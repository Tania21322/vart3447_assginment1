/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;

namespace Meta.XR.Guides.Editor.SdkUpgrader
{
    // Release-notes data pipeline: fetch (Dev Center changelog + scoped release-notes hub) with an on-disk
    // TTL cache, parse the JSON / SSR payloads, and aggregate them into per-(SDK, version) entries.
    internal static partial class SdkUpgraderData
    {
        private const int MaxReleasesShown = 6;
        private const int MaxVersionsFetched = 20;
        private const int MaxHighlightsPerVersion = 6;
        private const string ChangelogBaseUrl = "https://developers.meta.com/horizon/changelog/package/";

        // Public Dev Center release-notes hub. Its SSR HTML embeds a Relay payload whose changelog-entry
        // nodes carry the AI-generated `summary`, exposed tokenlessly in the release-notes page's SSR
        // Relay payload. The `path` query param scopes that payload to one package's canonical_name (==
        // our Slug), returning THAT package's recent versions each with its own summary. We fetch it per
        // installed package: the unscoped feed (path="") is only the ~10 globally-most-recent entries, so
        // any given package readily ages off it (e.g. Core once newer non-Unity products publish).
        // TODO: replace with a first-class Dev Center changelog API that emits the summary field directly.
        private const string ReleaseNotesHubPathUrl =
            "https://developers.meta.com/horizon/release-notes/?path=";

        private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(20) };

        #region Fetch

        private static async Task<Dictionary<int, VersionNotes>> FetchChangelogAsync(string slug)
        {
            var notes = new Dictionary<int, VersionNotes>();
            try
            {
                // System.Net.Http is thread-agnostic, so this is safe from the async continuation.
                // Trailing slash before the query avoids a 301 redirect (the endpoint canonicalizes to
                // .../package/{slug}/?max_versions=N).
                var url = $"{ChangelogBaseUrl}{slug}/?max_versions={MaxVersionsFetched}";
                var json = await GetStringUtf8Async(url);
                ParseChangelog(json, notes);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SdkUpgrader] Failed to fetch changelog for {slug}: {e.Message}");
            }

            return notes;
        }

        private static async Task<(Dictionary<string, string> summaries, Dictionary<string, string> dates)>
            FetchHubSummariesAsync(IReadOnlyList<string> slugs)
        {
            var summaries = new Dictionary<string, string>();
            var dates = new Dictionary<string, string>();

            // Fetch each package's scoped release-notes payload in parallel; the continuations may resume
            // on different threads, so merge under a lock.
            await Task.WhenAll(slugs.Distinct().Select(async slug =>
            {
                var (s, d) = await FetchPackageSummariesAsync(slug);
                lock (summaries)
                {
                    foreach (var kvp in s)
                    {
                        summaries[kvp.Key] = kvp.Value;
                    }

                    foreach (var kvp in d)
                    {
                        dates[kvp.Key] = kvp.Value;
                    }
                }
            }));

            return (summaries, dates);
        }

        // Keyed by "{canonical_name}@{major}" (canonical_name == the requested slug for scoped payloads,
        // but ?path is a prefix match, so a request can also return closely-named siblings — harmless
        // since we key on each node's own canonical_name).
        private static async Task<(Dictionary<string, string> summaries, Dictionary<string, string> dates)>
            FetchPackageSummariesAsync(string slug)
        {
            var summaries = new Dictionary<string, string>();
            var dates = new Dictionary<string, string>();
            try
            {
                var html = await GetStringUtf8Async($"{ReleaseNotesHubPathUrl}{slug}");
                var edgesArray = ExtractEdgesArray(html);
                if (string.IsNullOrEmpty(edgesArray))
                {
                    return (summaries, dates);
                }

                var parsed = JsonUtility.FromJson<HubEdges>($"{{\"edges\":{edgesArray}}}");
                if (parsed.edges == null)
                {
                    return (summaries, dates);
                }

                foreach (var edge in parsed.edges)
                {
                    var node = edge.node;
                    if (node.status != "PUBLISHED"
                        || string.IsNullOrEmpty(node.canonical_name)
                        || !int.TryParse((node.version ?? string.Empty).Split('.')[0], out var major))
                    {
                        continue;
                    }

                    var key = $"{node.canonical_name}@{major}";
                    if (!string.IsNullOrWhiteSpace(node.summary) && !summaries.ContainsKey(key))
                    {
                        summaries[key] = node.summary;
                    }

                    // change_date is a GraphQLTime scalar = Unix epoch seconds.
                    // Guard the conversion so one out-of-range value (e.g. accidentally in milliseconds,
                    // or a sentinel) can't throw and discard every sibling entry for this package.
                    if (node.change_date > 0 && !dates.ContainsKey(key))
                    {
                        try
                        {
                            dates[key] = DateTimeOffset.FromUnixTimeSeconds(node.change_date)
                                .UtcDateTime.ToString("MMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Leave the date unset for this entry rather than dropping the whole package.
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SdkUpgrader] Failed to fetch release-notes summaries for {slug}: {e.Message}");
            }

            return (summaries, dates);
        }

        #endregion

        #region On-disk response cache

        // Release-notes content changes rarely, but the editor reloads its domain constantly; cache each
        // response on disk (the scoped hub pages are ~500KB each) so those reloads don't re-download it. A
        // newer version simply surfaces once the entry ages past the TTL; force-refresh drops the cache.
        private static readonly TimeSpan HttpCacheTtl = TimeSpan.FromHours(6);
        // Pre-resolved on the main thread by InitOnMainThread (ComputeHttpCacheDir reads
        // Application.dataPath, which is main-thread-only). Reads go through HttpCacheDir, which lazily
        // resolves it should a threadpool continuation touch the cache before that runs.
        private static string _httpCacheDir;

        private static string HttpCacheDir => _httpCacheDir ??= ComputeHttpCacheDir();

        // Under Library/ (git-ignored, per-machine): the natural home for a regenerable cache.
        private static string ComputeHttpCacheDir() =>
            Path.Combine(
                Path.GetDirectoryName(Application.dataPath) ?? ".",
                "Library", "MetaXR", "SdkUpgraderHttpCache");

        // HttpClient.GetStringAsync decodes the body using the response's Content-Type charset; the
        // Dev Center sometimes returns a charset .NET rejects ("invalid character set"), which throws.
        // Read the raw bytes and decode as UTF-8 ourselves (these endpoints are UTF-8), stripping a
        // leading BOM so JSON/HTML parsing isn't thrown off.
        private static async Task<string> GetStringUtf8Async(string url)
        {
            var cachePath = HttpCachePath(url);
            if (cachePath != null)
            {
                try
                {
                    if (File.Exists(cachePath)
                        && DateTime.UtcNow - File.GetLastWriteTimeUtc(cachePath) < HttpCacheTtl)
                    {
                        return File.ReadAllText(cachePath);
                    }
                }
                catch (Exception)
                {
                    // Unreadable cache entry: fall through to a fresh download.
                }
            }

            var bytes = await _http.GetByteArrayAsync(url);
            var text = System.Text.Encoding.UTF8.GetString(bytes).TrimStart('\uFEFF');
            WriteHttpCache(cachePath, text);
            return text;
        }

        private static string HttpCachePath(string url)
        {
            if (string.IsNullOrEmpty(HttpCacheDir))
            {
                return null;
            }

            using var md5 = System.Security.Cryptography.MD5.Create();
            var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(url));
            return Path.Combine(HttpCacheDir, $"{string.Concat(hash.Select(b => b.ToString("x2")))}.txt");
        }

        private static void WriteHttpCache(string cachePath, string text)
        {
            if (cachePath == null)
            {
                return;
            }

            string tmp = null;
            try
            {
                Directory.CreateDirectory(HttpCacheDir);
                // Write to a unique temp file then atomically swap it in, so concurrent writers to the
                // same cache path can't corrupt each other. File.Replace atomically overwrites an
                // existing entry; File.Move covers the first write. (File.Move's overwrite overload
                // isn't available on this runtime.)
                tmp = $"{cachePath}.{Guid.NewGuid():N}.tmp";
                File.WriteAllText(tmp, text);
                try
                {
                    // Move-first, with no File.Exists precheck (that would be a TOCTOU race): Move succeeds
                    // on the first write; if the entry already exists — or a concurrent writer just created
                    // it — Move throws and we replace it atomically instead.
                    File.Move(tmp, cachePath);
                }
                catch (IOException)
                {
                    try
                    {
                        File.Replace(tmp, cachePath, null);
                    }
                    catch (IOException)
                    {
                        // Lost the race and the replace failed; the existing entry is valid, so drop our temp.
                        try { File.Delete(tmp); } catch { /* ignore */ }
                    }
                }
            }
            catch (Exception e)
            {
                // A non-IOException swap failure (e.g. UnauthorizedAccessException) or a failed
                // WriteAllText can orphan the temp; drop it so temps don't accumulate.
                if (tmp != null)
                {
                    try { File.Delete(tmp); } catch { /* ignore */ }
                }
                Debug.LogWarning($"[SdkUpgrader] Failed to write response cache: {e.Message}");
            }
        }

        private static void ClearHttpCache()
        {
            try
            {
                if (!string.IsNullOrEmpty(HttpCacheDir) && Directory.Exists(HttpCacheDir))
                {
                    Directory.Delete(HttpCacheDir, recursive: true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SdkUpgrader] Failed to clear response cache: {e.Message}");
            }
        }

        #endregion

        #region Parse

        // Extracts the `release_notes.edges` JSON array embedded in the hub's SSR HTML by
        // brace-balancing from the marker (a regex can't handle the nested/escaped payload).
        internal static string ExtractEdgesArray(string html)
        {
            const string marker = "\"release_notes\":{\"edges\":[";
            var markerIndex = html.IndexOf(marker, StringComparison.Ordinal);
            if (markerIndex < 0)
            {
                return null;
            }

            var start = markerIndex + marker.Length - 1; // at the opening '['
            var depth = 0;
            var inString = false;
            var escaped = false;
            for (var i = start; i < html.Length; i++)
            {
                var c = html[i];
                if (inString)
                {
                    if (escaped) escaped = false;
                    else if (c == '\\') escaped = true;
                    else if (c == '"') inString = false;
                }
                else if (c == '"')
                {
                    inString = true;
                }
                else if (c == '[')
                {
                    depth++;
                }
                else if (c == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        return html.Substring(start, i - start + 1);
                    }
                }
            }

            return null;
        }

        [Serializable]
        private struct HubEdges
        {
            public HubEdge[] edges;
        }

        [Serializable]
        private struct HubEdge
        {
            public HubNode node;
        }

        [Serializable]
        private struct HubNode
        {
            public string summary;
            public string version;
            public string canonical_name;
            public string status;
            public long change_date;
        }

        internal static void ParseChangelog(string json, Dictionary<int, VersionNotes> notes)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            try
            {
                // The endpoint returns a bare JSON array; wrap it so JsonUtility can parse it.
                var parsed = JsonUtility.FromJson<ChangelogWrapper>($"{{\"items\":{json}}}");
                if (parsed.items == null)
                {
                    return;
                }

                foreach (var item in parsed.items)
                {
                    if (!int.TryParse((item.version ?? string.Empty).Split('.')[0], out var major))
                    {
                        continue;
                    }

                    // Keep the first entry seen per major. The changelog endpoint returns entries
                    // newest-first, so the first seen is the newest (highest minor/patch) for that major.
                    if (!notes.ContainsKey(major))
                    {
                        notes[major] = new VersionNotes
                        {
                            Summary = item.release_notes_summary,
                            Raw = item.release_notes
                        };
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SdkUpgrader] Failed to parse changelog JSON: {e.Message}");
            }
        }

        [Serializable]
        private struct ChangelogWrapper
        {
            public ChangelogItem[] items;
        }

        [Serializable]
        private struct ChangelogItem
        {
            public string version;
            public string release_notes;
            public string release_notes_summary;
        }

        #endregion

        #region Aggregation

        internal static ReleaseNoteEntry[] BuildReleasesFromInstalled()
        {
            if (_installed.Count == 0)
            {
                return Array.Empty<ReleaseNoteEntry>();
            }

            var current = CurrentVersion;
            var latest = LatestVersion;

            // One entry PER (SDK package, version) — NOT merged — so each card is attributed to a single
            // SDK and its "Full release notes" link points to that SDK's own page. (A merged card could
            // only carry one slug, e.g. Core, which would mislabel Interaction's notes.)
            var entries = new List<ReleaseNoteEntry>();
            foreach (var package in _installed)
            {
                foreach (var kvp in package.NotesByMajor)
                {
                    var major = kvp.Key;
                    if (major <= current)
                    {
                        continue;
                    }

                    // Prefer this package's AI summary from the release-notes hub (keyed by canonical
                    // name + major); fall back to its changelog "What's New" / bullets.
                    var summaryKey = $"{package.Package.Slug}@{major}";
                    var highlights =
                        _hubSummaries.TryGetValue(summaryKey, out var aiSummary)
                        && !string.IsNullOrWhiteSpace(aiSummary)
                            ? SplitToLines(aiSummary).Take(MaxHighlightsPerVersion).ToArray()
                            : BuildHighlights(kvp.Value, MaxHighlightsPerVersion);
                    if (highlights.Length == 0)
                    {
                        continue;
                    }

                    entries.Add(new ReleaseNoteEntry(
                        major,
                        _hubDates.TryGetValue(summaryKey, out var date) ? date : string.Empty,
                        highlights,
                        slug: package.Package.Slug,
                        sdks: new[] { package.Package.ShortName },
                        isLatest: major == latest,
                        breakingChanges: DetectBreaking(kvp.Value.Raw)));
                }
            }

            // Newest versions first (capped by distinct version count so multi-SDK projects stay
            // bounded); within a version, keep KnownPackages order (Core first).
            var majorsShown = entries
                .Select(e => e.Version)
                .Distinct()
                .OrderByDescending(v => v)
                .Take(MaxReleasesShown)
                .ToHashSet();

            return entries
                .Where(e => majorsShown.Contains(e.Version))
                .OrderByDescending(e => e.Version)
                .ThenBy(e =>
                {
                    // FindIndex returns -1 for an unknown SDK; map that to sort-last (int.MaxValue)
                    // to match the empty-Sdks branch, so unknowns never sort ahead of Core (index 0).
                    var idx = e.Sdks.Length > 0
                        ? Array.FindIndex(KnownPackages, k => k.ShortName == e.Sdks[0])
                        : -1;
                    return idx < 0 ? int.MaxValue : idx;
                })
                .ToArray();
        }

        internal static string[] BuildHighlights(VersionNotes notes, int max)
        {
            // Prefer the AI summary, then the "What's New" section, then the first bullet points.
            if (!string.IsNullOrWhiteSpace(notes.Summary))
            {
                var lines = SplitToLines(notes.Summary);
                if (lines.Length > 0)
                {
                    return lines.Take(max).ToArray();
                }
            }

            var raw = notes.Raw ?? string.Empty;
            var section = ExtractSectionText(raw, "What's New");
            var source = string.IsNullOrWhiteSpace(section) ? raw : section;

            var bullets = Regex.Matches(source, @"(?m)^\s*[-*]\s+(.+)$")
                .Select(m => CleanMarkdown(m.Groups[1].Value))
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Take(max)
                .ToArray();

            if (bullets.Length > 0)
            {
                return bullets;
            }

            var firstLine = SplitToLines(source).FirstOrDefault();
            return string.IsNullOrWhiteSpace(firstLine) ? Array.Empty<string>() : new[] { firstLine };
        }

        internal static string[] SplitToLines(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return Array.Empty<string>();
            }

            return text.Split('\n')
                .Select(line => CleanMarkdown(line.TrimStart('-', '*', ' ', '\t')))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToArray();
        }

        private static string ExtractSectionText(string raw, string sectionTitle)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return string.Empty;
            }

            var pattern = $@"(?is)#{{1,6}}\s*\**\s*{Regex.Escape(sectionTitle)}\s*\**\s*\n(.*?)(?=\n#{{1,6}}\s|\z)";
            var match = Regex.Match(raw, pattern);
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private static string CleanMarkdown(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return string.Empty;
            }

            s = Regex.Replace(s, @"\[([^\]]+)\]\([^)]*\)", "$1"); // [label](url) -> label
            s = s.Replace("**", string.Empty).Replace("`", string.Empty).Replace("__", string.Empty);
            return s.Trim();
        }

        internal static bool DetectBreaking(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return false;
            }

            return raw.IndexOf("breaking", StringComparison.OrdinalIgnoreCase) >= 0
                   || raw.IndexOf("deprecat", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        internal static int ParseMajor(string version)
        {
            if (string.IsNullOrEmpty(version))
            {
                return 0;
            }

            return int.TryParse(version.Split('.')[0], out var major) ? major : 0;
        }

        #endregion
    }
}
