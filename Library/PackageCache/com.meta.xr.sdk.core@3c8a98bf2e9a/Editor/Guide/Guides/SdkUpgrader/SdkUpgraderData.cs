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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meta.XR.Editor.ToolingSupport;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace Meta.XR.Guides.Editor.SdkUpgrader
{
    /// <summary>
    /// One SDK version's aggregated release-notes summary as shown in the SDK Upgrade Guide.
    /// Highlights are prefixed with the package short-name when more than one installed SDK
    /// contributes notes for that version.
    /// </summary>
    internal readonly struct ReleaseNoteEntry
    {
        public readonly int Version;
        public readonly string DateLabel;
        public readonly string[] Highlights;
        public readonly bool IsLatest;
        public readonly bool BreakingChanges;

        // Changelog slug of the primary package that contributed this version's notes; used to build
        // the per-version "Full release notes" link. Null when unknown (link falls back to the hub).
        public readonly string Slug;

        // Short names of the SDK(s) whose changelogs contributed this version's notes (e.g. "Core",
        // "Interaction"); rendered as per-SDK pills so each version's changes are attributed to their SDK.
        public readonly string[] Sdks;

        public ReleaseNoteEntry(
            int version,
            string dateLabel,
            string[] highlights,
            string slug = null,
            string[] sdks = null,
            bool isLatest = false,
            bool breakingChanges = false)
        {
            Version = version;
            DateLabel = dateLabel;
            Highlights = highlights;
            Slug = slug;
            Sdks = sdks ?? Array.Empty<string>();
            IsLatest = isLatest;
            BreakingChanges = breakingChanges;
        }

        public string VersionLabel => $"Version {Version}";
    }

    /// <summary>An installed Meta XR SDK package and whether an update is available for it.</summary>
    internal readonly struct InstalledSdkInfo
    {
        public readonly string DisplayName;
        public readonly string PackageId;
        public readonly int CurrentVersion;
        public readonly bool HasUpdate;

        public InstalledSdkInfo(string displayName, string packageId, int currentVersion, bool hasUpdate)
        {
            DisplayName = displayName;
            PackageId = packageId;
            CurrentVersion = currentVersion;
            HasUpdate = hasUpdate;
        }
    }

    /// <summary>
    /// Provides the data shown by <see cref="SdkUpgraderWindow"/>: which Meta XR SDKs are installed,
    /// the latest available version, and per-version release-notes summaries.
    ///
    /// Installed packages + their versions come from the Unity Package Manager; if the SDK is embedded
    /// in the project (not a UPM package), it falls back to the OVRPlugin version via <see cref="ToolUsage"/>.
    /// Release notes come from the public Dev Center changelog endpoint
    /// (https://developers.meta.com/horizon/changelog/package/{slug}), fetched per installed package via
    /// <see cref="RemoteJsonContentDownloader"/>. Per-version highlights prefer the AI-generated
    /// <c>release_notes_summary</c> when the endpoint provides it, then the "What's New" section, then the
    /// first bullet points.
    ///
    /// Loading is asynchronous; <see cref="OnChanged"/> fires (on the main thread) when data arrives so
    /// the window can rebuild. Internal builds can override the state via the SDK Upgrader debug menu.
    /// </summary>
    internal static partial class SdkUpgraderData
    {
        /// <summary>A Meta XR SDK package: its UPM id, Dev Center changelog slug, and display names.</summary>
        internal readonly struct KnownPackage
        {
            public readonly string UpmId;
            public readonly string Slug;
            public readonly string DisplayName;
            public readonly string ShortName;

            public KnownPackage(string upmId, string slug, string displayName, string shortName)
            {
                UpmId = upmId;
                Slug = slug;
                DisplayName = displayName;
                ShortName = shortName;
            }
        }

        // UPM id -> Dev Center changelog slug (each package's canonical name on the public changelog).
        // Core is identified by CoreUpmId (not array position) for the current/latest signal + embedded fallback.
        internal static readonly KnownPackage[] KnownPackages =
        {
            new KnownPackage(CoreUpmId, "meta-xr-core-sdk", "Meta XR Core SDK", "Core"),
            new KnownPackage("com.meta.xr.sdk.interaction", "meta-xr-interaction-sdk", "Meta XR Interaction SDK", "Interaction"),
            new KnownPackage("com.meta.xr.sdk.interaction.ovr", "meta-xr-interaction-sdk-ovr-integration", "Interaction SDK (OVR)", "Interaction OVR"),
            new KnownPackage("com.meta.xr.sdk.platform", "meta-xr-platform-sdk", "Meta XR Platform SDK", "Platform"),
            new KnownPackage("com.meta.xr.sdk.audio", "meta-xr-audio-sdk", "Meta XR Audio SDK", "Audio"),
            new KnownPackage("com.meta.xr.sdk.voice", "meta-voice-sdk", "Meta Voice SDK", "Voice"),
            new KnownPackage("com.meta.xr.sdk.haptics", "meta-haptics-sdk-unity", "Meta Haptics SDK", "Haptics"),
            new KnownPackage("com.meta.xr.mrutilitykit", "meta-xr-mr-utility-kit-upm", "MR Utility Kit", "MRUK"),
            new KnownPackage("com.meta.xr.sdk.all", "meta-xr-sdk-all-in-one-upm", "Meta XR All-in-One SDK", "All-in-One"),
        };

        // Core is the primary SDK — its version drives the current/latest signal and it is the embedded
        // (OVRPlugin) fallback. Identify it by id, never by position in KnownPackages.
        internal const string CoreUpmId = "com.meta.xr.sdk.core";

        private static KnownPackage CorePackage => KnownPackages.First(k => k.UpmId == CoreUpmId);

        private const string AgentBridgePackageId = "com.meta.xr.ai.agentbridge";

        // Human-facing release-notes hub (fallback target when a per-version link can't be built).
        public const string ReleaseNotesBaseUrl = "https://developers.meta.com/horizon/release-notes/";

        // Per-package, per-version notes page — the same target the release-notes hub links each entry
        // to: {DownloadsPackageBaseUrl}{slug}/{major}.0?view=full_width.
        private const string DownloadsPackageBaseUrl = "https://developers.meta.com/horizon/downloads/package/";

        /// <summary>
        /// The human-facing "Full release notes" URL for a release entry: the per-version notes page of
        /// the primary package that contributed it. Falls back to the release-notes hub when the entry
        /// carries no package slug.
        /// </summary>
        public static string FullReleaseNotesUrl(ReleaseNoteEntry release) =>
            string.IsNullOrEmpty(release.Slug)
                ? ReleaseNotesBaseUrl
                : $"{DownloadsPackageBaseUrl}{release.Slug}/{release.Version}.0?view=full_width";

        // Used only when no installed version can be resolved (e.g. running from in-project source
        // with no OVRPlugin and no UPM package). Real installs resolve a real version.
        private const int FallbackCurrentVersion = 65;

        public static event Action OnChanged;

        private static bool _loading;
        private static bool _loaded;

        // Coalescing flags for reloads requested while a load is in flight. Main-thread-only access
        // (Refresh/ForceRefresh/DrainPendingReload all run on the editor main thread), so no locking needed.
        private static bool _reloadRequested;    // a refresh arrived mid-load; run one more load once the current finishes
        private static bool _clearCacheOnReload; // the queued reload should also drop caches first (from ForceRefresh)

        // Editor main-thread context (captured in InitOnMainThread, with a Refresh-time fallback) so
        // load completions marshal back safely.
        private static SynchronizationContext _mainThread;
        private static bool _agentBridgeInstalled;
        internal static List<InstalledPackage> _installed = new();

        // Key "{canonicalName}@{major}" -> AI summary from the release-notes hub.
        internal static Dictionary<string, string> _hubSummaries = new();

        // Key "{canonicalName}@{major}" -> release date label ("MMM yyyy") from the hub's change_date.
        internal static Dictionary<string, string> _hubDates = new();

        public static bool IsLoading => _loading;
        public static bool IsLoaded => _loaded;

        internal struct VersionNotes
        {
            public string Summary;
            public string Raw;
        }

        internal sealed class InstalledPackage
        {
            public KnownPackage Package;
            public int CurrentVersion;
            public int LatestVersion;
            public Dictionary<int, VersionNotes> NotesByMajor = new();
        }

        #region Public data surface (read by the window)

        public static int CurrentVersion
        {
            get
            {
                // Debug: pretend the project is N versions behind the REAL latest, so the guide renders
                // the real release notes/dates for the latest N versions (see SdkUpgraderDebugMenu).
                if (SimVersionsBehind >= 0 && RealLatestVersion > 0)
                {
                    return RealLatestVersion - SimVersionsBehind;
                }

                // Prefer the UPM-installed Core package version (authoritative when Core ships as a
                // package); fall back to the embedded OVRPlugin version only when Core is not a resolved
                // UPM package, matching how LoadAsync treats OVRPlugin as the embedded fallback.
                var core = _installed.FirstOrDefault(p => p.Package.UpmId == CoreUpmId);
                if (core != null && core.CurrentVersion > 0)
                {
                    return core.CurrentVersion;
                }

                var pluginVersion = ToolUsage.GetSdkVersion();
                if (pluginVersion.HasValue)
                {
                    return pluginVersion.Value;
                }

                var anyInstalled = _installed.FirstOrDefault(p => p.CurrentVersion > 0);
                return anyInstalled?.CurrentVersion ?? FallbackCurrentVersion;
            }
        }

        private static int RealLatestVersion
        {
            get
            {
                var core = _installed.FirstOrDefault(package => package.Package.UpmId == CoreUpmId);
                return core?.LatestVersion
                    ?? _installed.Select(package => package.LatestVersion).DefaultIfEmpty(0).Max();
            }
        }

        public static int LatestVersion
        {
            get
            {
                var latest = RealLatestVersion;
                return latest > CurrentVersion ? latest : CurrentVersion;
            }
        }

        public static bool IsUpdateAvailable => LatestVersion > CurrentVersion;

        public static int VersionsBehind
        {
            get
            {
                var current = CurrentVersion;
                return _installed
                    .SelectMany(p => p.NotesByMajor.Keys)
                    .Where(major => major > current)
                    .Distinct()
                    .Count();
            }
        }

        public static bool AiBridgeConnected => AiBridgeOverride ?? _agentBridgeInstalled;

        public static IReadOnlyList<InstalledSdkInfo> InstalledSdks
        {
            get
            {
                var latest = LatestVersion;
                return _installed
                    .Select(p =>
                    {
                        var current = p.CurrentVersion > 0 ? p.CurrentVersion : CurrentVersion;
                        return new InstalledSdkInfo(p.Package.DisplayName, p.Package.UpmId, current, latest > current);
                    })
                    .ToList();
            }
        }

        public static ReleaseNoteEntry[] Releases
        {
            get
            {
                return BuildReleasesFromInstalled();
            }
        }

        #endregion

        #region Loading

        // Unity runs [InitializeOnLoadMethod] on the editor main thread after each domain reload, so this
        // is where we capture the main-thread SynchronizationContext and pre-resolve the HTTP cache dir
        // (ComputeHttpCacheDir reads Application.dataPath, which is main-thread-only) before any threadpool
        // continuation can need them.
        [InitializeOnLoadMethod]
        private static void InitOnMainThread()
        {
            _mainThread = SynchronizationContext.Current;
            _httpCacheDir = ComputeHttpCacheDir();
        }

        public static void EnsureLoaded()
        {
            if (_loaded || _loading)
            {
                return;
            }

            Refresh();
        }

        public static void Refresh()
        {
            // Guard against concurrent loads: a second Refresh while one is in flight would let the two
            // completion callbacks interleave and clobber each other's results.
            if (_loading)
            {
                // A load is already in flight; queue exactly one more to run when it finishes.
                _reloadRequested = true;
                return;
            }

            // Captured here (always the editor main thread) so completions can marshal back even when the
            // fetch continuations resume on a threadpool thread.
            _mainThread ??= SynchronizationContext.Current;
            _loading = true;
            _ = RunLoadAsync();
            NotifyChanged();
        }

        // Wraps LoadAsync so a fault can't strand the UI in the loading state: the fire-and-forget discard
        // would otherwise swallow the exception and leave _loading stuck true.
        private static async Task RunLoadAsync()
        {
            try
            {
                await LoadAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SdkUpgrader] Load failed: {e.Message}");
                OnMainThread(() =>
                {
                    _loading = false;
                    NotifyChanged();
                    DrainPendingReload();
                });
            }
        }

        // Runs the action on the editor main thread — via the SynchronizationContext captured in Refresh
        // when available (fetch continuations may resume on a threadpool thread), else via delayCall.
        private static void OnMainThread(Action action)
        {
            if (_mainThread != null)
            {
                _mainThread.Post(_ => action(), null);
            }
            else
            {
                EditorApplication.delayCall += () => action();
            }
        }

        private static void NotifyChanged()
        {
            try
            {
                OnChanged?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SdkUpgrader] OnChanged subscriber threw: {e.Message}");
            }
        }

        // Runs a reload queued (by Refresh/ForceRefresh) while a load was in flight. Called on the main thread
        // at the end of each load, after _loading has been reset.
        private static void DrainPendingReload()
        {
            if (!_reloadRequested)
            {
                return;
            }

            _reloadRequested = false;
            if (_clearCacheOnReload)
            {
                _clearCacheOnReload = false;
                ClearHttpCache();
                ResetCliVersionProbe();
            }

            Refresh();
        }

        // Debug/manual refresh: drop the on-disk response cache so the next load re-downloads.
        public static void ForceRefresh()
        {
            if (_loading)
            {
                // Defer: let the in-flight load finish, then drop caches and reload once.
                _clearCacheOnReload = true;
                _reloadRequested = true;
                return;
            }

            ClearHttpCache();
            ResetCliVersionProbe();
            Refresh();
        }

        private static async Task LoadAsync()
        {
            // Client.List must be kicked off on the main thread (this runs before the first await).
            var installedVersions = new Dictionary<string, int>();
            var agentBridgeInstalled = false;
            try
            {
                var listRequest = Client.List(offlineMode: true, includeIndirectDependencies: true);
                // Bound the poll so a stuck request can't strand _loading; the ~10s cap (200 × 50ms)
                // times out into the Status != Success path below, degrading to embedded detection.
                const int maxAttempts = 200;
                var attempts = 0;
                while (!listRequest.IsCompleted)
                {
                    if (++attempts > maxAttempts)
                    {
                        Debug.LogWarning("[SdkUpgrader] Package list timed out; falling back to embedded detection.");
                        // Client.List returns a polled ListRequest (not a Task) with no public cancel API,
                        // so there's nothing to await/cancel and no unobserved-exception risk: we simply
                        // abandon it — it completes and is GC'd harmlessly — and fall through to the
                        // Status != Success path below (embedded detection).
                        break;
                    }

                    await Task.Delay(50);
                }

                if (listRequest.Status == StatusCode.Success && listRequest.Result != null)
                {
                    foreach (var package in listRequest.Result)
                    {
                        installedVersions[package.name] = ParseMajor(package.version);
                    }
                    agentBridgeInstalled = installedVersions.ContainsKey(AgentBridgePackageId);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SdkUpgrader] Failed to list packages: {e.Message}");
            }

            var embeddedCoreVersion = installedVersions.ContainsKey(CoreUpmId)
                ? null
                : ToolUsage.GetSdkVersion();
            var toLoad = ResolvePackagesToLoad(installedVersions, embeddedCoreVersion);

            var hubTask = FetchHubSummariesAsync(toLoad.Select(t => t.pkg.Slug).ToList());

            var results = new List<InstalledPackage>();
            await Task.WhenAll(toLoad.Select(async entry =>
            {
                var notes = await FetchChangelogAsync(entry.pkg.Slug);
                // The newest version we actually have release notes for (changelog max), NOT inflated to
                // the installed version. An internal/dev build can sit ahead of public (e.g. 208 installed
                // vs 203 published); clamping here keeps the guide — and the debug "N behind" simulation —
                // targeting a version that has notes + AI summaries. The public LatestVersion getter still
                // re-guards against reporting a value below the installed version.
                var latest = notes.Keys.DefaultIfEmpty(entry.current).Max();
                var installedPackage = new InstalledPackage
                {
                    Package = entry.pkg,
                    CurrentVersion = entry.current,
                    LatestVersion = latest,
                    NotesByMajor = notes
                };
                lock (results)
                {
                    results.Add(installedPackage);
                }
            }));

            var (hubSummaries, hubDates) = await hubTask;

            // Fetch continuations may resume off the main thread; marshal the state swap + notification
            // back onto the editor tick before touching shared state / UIToolkit.
            OnMainThread(() =>
            {
                // Preserve the KnownPackages order for stable display.
                _installed = results
                    .OrderBy(p => Array.FindIndex(KnownPackages, k => k.UpmId == p.Package.UpmId))
                    .ToList();
                _hubSummaries = hubSummaries;
                _hubDates = hubDates;
                _agentBridgeInstalled = agentBridgeInstalled;
                _loaded = true;
                _loading = false;
                NotifyChanged();
                DrainPendingReload();
            });
        }

        internal static List<(KnownPackage pkg, int current)> ResolvePackagesToLoad(
            IReadOnlyDictionary<string, int> installedVersions,
            int? embeddedCoreVersion)
        {
            var toLoad = KnownPackages
                .Where(package => installedVersions.ContainsKey(package.UpmId))
                .Select(package => (pkg: package, current: installedVersions[package.UpmId]))
                .ToList();

            if (toLoad.All(entry => entry.pkg.UpmId != CoreUpmId) && embeddedCoreVersion.HasValue)
            {
                toLoad.Add((CorePackage, embeddedCoreVersion.Value));
            }

            return toLoad
                .OrderBy(entry => Array.FindIndex(
                    KnownPackages,
                    package => package.UpmId == entry.pkg.UpmId))
                .ToList();
        }

        #endregion

        #region Debug overrides (driven by SdkUpgraderDebugMenu under OVR_INTERNAL_CODE)

        private const string SimKey = "MetaXR.SdkUpgrader.SimVersionsBehind";
        private const string AiKey = "MetaXR.SdkUpgrader.AiBridgeOverride"; // -1 unset, 0 false, 1 true
        private const string CliKey = "MetaXR.SdkUpgrader.MetaVrCliOverride"; // -1 unset, else MetaVrCliState value

        // >= 0 forces "update available N versions behind" with mock releases; -1 = use real data.
        internal static int SimVersionsBehind
        {
            get => SessionState.GetInt(SimKey, -1);
            set
            {
                SessionState.SetInt(SimKey, value);
                NotifyChanged();
            }
        }

        internal static bool? AiBridgeOverride
        {
            get
            {
                var v = SessionState.GetInt(AiKey, -1);
                return v < 0 ? (bool?)null : v == 1;
            }
            set
            {
                SessionState.SetInt(AiKey, value.HasValue ? (value.Value ? 1 : 0) : -1);
                NotifyChanged();
            }
        }

        // null = use real detection; else force a specific metavr CLI state (debug menu).
        internal static MetaVrCliState? MetaVrCliStateOverride
        {
            get
            {
                var v = SessionState.GetInt(CliKey, -1);
                return v < 0 ? (MetaVrCliState?)null : (MetaVrCliState)v;
            }
            set
            {
                SessionState.SetInt(CliKey, value.HasValue ? (int)value.Value : -1);
                ResetCliVersionProbe();
                NotifyChanged();
            }
        }

        internal static bool HasActiveOverrides =>
            SimVersionsBehind >= 0 || AiBridgeOverride.HasValue || MetaVrCliStateOverride.HasValue;

        internal static void ClearOverrides()
        {
            SessionState.EraseInt(SimKey);
            SessionState.EraseInt(AiKey);
            SessionState.EraseInt(CliKey);
            NotifyChanged();
        }


        #endregion
    }
}
