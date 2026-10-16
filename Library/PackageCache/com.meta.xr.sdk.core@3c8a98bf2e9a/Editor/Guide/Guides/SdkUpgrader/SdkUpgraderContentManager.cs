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
using System.Threading.Tasks;
using Meta.XR.Editor.RemoteContent;
using Meta.XR.Editor.ToolingSupport;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Guides.Editor.SdkUpgrader
{
    /// <summary>
    /// Remote-hosted copy + distribution config for the SDK Upgrade Assistant's "Upgrade with AI"
    /// section (the skill install commands, the MetaVR CLI version that ships the skill installer, the
    /// GitHub URL, and the AI prompt). Mirrors <c>WelcomeContentManager</c>: a version-gated variants
    /// blob served by the Building Blocks Content CMS via <see cref="RemoteJsonContentDownloader"/>,
    /// field-merged over the compiled default so a partial blob is safe. Until a real
    /// <see cref="ContentId"/> is configured, the compiled default ships. Internal builds can point it
    /// at a hand-authored local file (see the SDK Upgrader content debug menu) to preview edits.
    /// </summary>
    internal static class SdkUpgraderContentManager
    {
        #region JSON structs (wire format)

        // A list of version-scoped content variants. The client downloads the whole blob and selects
        // the variant whose SDK window contains the running SDK, so one blob can serve many versions.
        [Serializable]
        internal struct SdkUpgraderBlob
        {
            public int schemaVersion;
            public SdkUpgraderContent[] variants;
        }

        [Serializable]
        internal struct SdkUpgraderContent
        {
            // SDK-version window for this variant (minor version, e.g. 207). The client picks the
            // applicable variant with the highest minSdkVersion. minSdkVersion 0 = no floor;
            // maxSdkVersion 0 = no cap.
            public int minSdkVersion;
            public int maxSdkVersion;

            // "Upgrade with AI" section copy.
            public string heading;
            public string intro;

            // Option A — MetaVR CLI. The install command is platform-dependent (curl on macOS/Linux,
            // PowerShell on Windows); the window picks the right one for the running editor.
            public string cliOptionLabel;
            public string cliInstallCliLabel;          // step label for installing the metavr CLI (not-installed path)
            public string cliInstallCliCommand;        // macOS/Linux (curl one-liner)
            public string cliInstallCliCommandWindows; // Windows (PowerShell one-liner)
            public string cliInstallSkillLabel;        // step label for installing the skill (step 2)
            public string cliInstallSkillCommand;      // e.g. "metavr skill install sdk-upgrader"
            public string cliUpgradeLabel;             // step label shown when an install is below cliMinVersion
            public string cliUpgradeCommand;           // e.g. "metavr update"
            public string cliMinVersion;          // minimum metavr CLI version required (blank = don't check)
            public string cliDocUrl;              // public docs for the metavr CLI (opened by the "?" help affordance)
            public string cliTooltip;             // 2-sentence explanation shown on hovering the "?"

            // Remote kill-switch: when true the metavr CLI skill path is hidden and only the
            // GitHub-manual + prompt path remains. Hide-sense (default false = shown) because
            // JsonUtility can't distinguish an omitted JSON bool from an explicit false, so a blob
            // that omits it leaves the CLI path visible; remote content must set it true to hide.
            public bool hideMetaVrCliSkillOption;

            // Option B — GitHub.
            public string githubOptionLabel;
            public string githubOptionDescription;
            public string githubUrl;

            // Run step.
            public string runLabel;
            public string promptTemplate;         // supports {from} and {to} placeholders
        }

        #endregion

        #region Default (compiled) content

        // The compiled fallback, used when no remote/override variant applies, and the base that
        // remote/override content is field-merged over. Version window kept at 0 so it reads as
        // "always applies".
        private static readonly SdkUpgraderContent DefaultContent = new()
        {
            minSdkVersion = 0,
            maxSdkVersion = 0,
            heading = "Upgrade with AI Skill",
            intro =
                "The SDK Upgrade Skill is curated by Meta. It knows every version's breaking changes " +
                "and teaches your AI agent to migrate them for you, automatically.",
            cliOptionLabel = "Install the skill with metavr CLI",
            cliInstallCliLabel = "Install the metavr CLI",
            // Curl/PowerShell one-liners are the MetaVR CLI team's recommended install, replacing the
            // deprecated `npm install`. Served from developers.meta.com/horizon/install-cli.
            cliInstallCliCommand = "curl -fsSL https://developers.meta.com/horizon/install-cli | bash",
            cliInstallCliCommandWindows = "iwr -useb https://developers.meta.com/horizon/install-cli/windows/ | iex",
            cliInstallSkillLabel = "Install the skill",
            cliInstallSkillCommand = "metavr skill install sdk-upgrader",
            cliUpgradeLabel = "Upgrade the metavr CLI",
            cliUpgradeCommand = "metavr update",
            cliMinVersion = "1.5.0",
            cliDocUrl = "https://developers.meta.com/horizon/install-cli/",
            cliTooltip =
                "The metavr CLI is Meta's command-line tool for XR development workflows. It installs " +
                "and runs the SDK Upgrade Skill along with other Meta XR automations.",
            githubOptionLabel = "Install the skill manually",
            githubOptionDescription = "Get the skill from GitHub and follow the manual setup steps.",
            githubUrl = "https://github.com/meta-quest/agentic-tools/tree/main/skills/hz-api-upgrade",
            runLabel = "Run this prompt referencing the skill in your AI agent",
            promptTemplate =
                "Use the SDK Upgrade Skill to upgrade my Unity project's Meta XR SDK from v{from} to v{to}. " +
                "Migrate all breaking changes and fix any compile errors."
        };

        #endregion

        // Content ID for the remote blob, uploaded via the Building Blocks Content CMS and served by the
        // remote content service. 0 = no remote content configured; the compiled default is used until a
        // real id is set here. static readonly (not const) so the `ContentId == 0` guards aren't
        // constant-folded into unreachable-code (CS0162) errors when a real id is set.
        private static readonly ulong ContentId = 27540541578960671;
        private const int SupportedSchemaVersion = 1;

        public static SdkUpgraderContent Content { get; private set; } = DefaultContent;

        public static event Action OnContentChanged;

        /// <summary>The AI prompt with the {from}/{to} placeholders resolved to concrete versions.</summary>
        public static string UpgradePrompt(int fromVersion, int toVersion) =>
            (Content.promptTemplate ?? DefaultContent.promptTemplate)
                .Replace("{from}", fromVersion.ToString())
                .Replace("{to}", toVersion.ToString());

        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            if (ContentId == 0) return;
            FetchRemoteContent();
        }

        private static async void FetchRemoteContent() => await FetchAndApply();

        // Downloads the remote blob, resolves the variant for the running SDK, and applies it on
        // success — leaving current content in place on any failure. Wrapped in try/catch because it
        // runs fire-and-forget on editor load (async void, from an [InitializeOnLoadMethod]).
        private static async Task<bool> FetchAndApply(bool clearCache = false)
        {
            try
            {
                // Bail when the running SDK version is unknown rather than coercing to 0: a 0 floor
                // would make every floorless variant apply for an unidentified editor.
                var sdkVersion = ToolUsage.GetSdkVersion();
                if (sdkVersion == null) return false;

                var downloader = new RemoteJsonContentDownloader("sdk_upgrader_content.json", ContentId)
                    .WithCacheDuration(TimeSpan.FromHours(6));
                if (clearCache) downloader.ClearCache();

                var result = await downloader.Fetch();
                if (!result.IsSuccess) return false;
                if (!TryResolve(result.Content, sdkVersion.Value, out var content)) return false;

                Content = content;
                OnContentChanged?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SdkUpgrader] Remote content fetch failed: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Parse the blob and resolve the variant for the given SDK version, field-merged over the
        /// compiled default. Returns false (leaving current content in place) when the JSON is
        /// malformed, the schema is unsupported, or no variant's SDK window contains the version.
        /// </summary>
        internal static bool TryResolve(string json, int sdkVersion, out SdkUpgraderContent content)
        {
            content = default;

            SdkUpgraderBlob blob;
            try
            {
                blob = JsonUtility.FromJson<SdkUpgraderBlob>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SdkUpgrader] Failed to parse content JSON: {e.Message}");
                return false;
            }

            // A missing/zero schemaVersion (e.g. an empty "{}" blob) is treated as malformed and
            // ignored rather than blanking the section.
            if (blob.schemaVersion < 1 || blob.schemaVersion > SupportedSchemaVersion) return false;

            var variant = SelectVariant(blob.variants, sdkVersion);
            if (variant == null) return false;

            content = Normalize(variant.Value);
            return true;
        }

        /// <summary>
        /// Whether a variant's SDK window includes the given SDK minor version.
        /// minSdkVersion 0 = no floor; maxSdkVersion 0 = no cap.
        /// </summary>
        internal static bool AppliesToSdk(SdkUpgraderContent variant, int sdkVersion)
        {
            if (sdkVersion < variant.minSdkVersion) return false;
            if (variant.maxSdkVersion > 0 && sdkVersion > variant.maxSdkVersion) return false;
            return true;
        }

        /// <summary>
        /// Pick the applicable variant with the highest minSdkVersion (the most specific match), or
        /// null when none applies. On a tie the first in array order wins, so blob authors should not
        /// ship two variants with the same floor.
        /// </summary>
        internal static SdkUpgraderContent? SelectVariant(SdkUpgraderContent[] variants, int sdkVersion)
        {
            SdkUpgraderContent? best = null;
            if (variants == null) return null;

            foreach (var variant in variants)
            {
                if (!AppliesToSdk(variant, sdkVersion)) continue;
                if (best == null || variant.minSdkVersion > best.Value.minSdkVersion)
                {
                    best = variant;
                }
            }

            return best;
        }

        // Field-level merge over DefaultContent: a variant only needs to carry the deltas it wants to
        // change; every string it omits or leaves blank (null/empty/whitespace) falls back to the
        // compiled default. cliMinVersion is intentionally NOT coalesced — an empty value is the valid
        // "don't show a minimum version" state.
        internal static SdkUpgraderContent Normalize(SdkUpgraderContent content)
        {
            content.heading = Coalesce(content.heading, DefaultContent.heading);
            content.intro = Coalesce(content.intro, DefaultContent.intro);
            content.cliOptionLabel = Coalesce(content.cliOptionLabel, DefaultContent.cliOptionLabel);
            content.cliInstallCliLabel = Coalesce(content.cliInstallCliLabel, DefaultContent.cliInstallCliLabel);
            content.cliInstallCliCommand = Coalesce(content.cliInstallCliCommand, DefaultContent.cliInstallCliCommand);
            content.cliInstallCliCommandWindows = Coalesce(content.cliInstallCliCommandWindows, DefaultContent.cliInstallCliCommandWindows);
            content.cliInstallSkillLabel = Coalesce(content.cliInstallSkillLabel, DefaultContent.cliInstallSkillLabel);
            content.cliInstallSkillCommand = Coalesce(content.cliInstallSkillCommand, DefaultContent.cliInstallSkillCommand);
            content.cliUpgradeLabel = Coalesce(content.cliUpgradeLabel, DefaultContent.cliUpgradeLabel);
            content.cliUpgradeCommand = Coalesce(content.cliUpgradeCommand, DefaultContent.cliUpgradeCommand);
            content.cliDocUrl = Coalesce(content.cliDocUrl, DefaultContent.cliDocUrl);
            content.cliTooltip = Coalesce(content.cliTooltip, DefaultContent.cliTooltip);
            content.githubOptionLabel = Coalesce(content.githubOptionLabel, DefaultContent.githubOptionLabel);
            content.githubOptionDescription = Coalesce(content.githubOptionDescription, DefaultContent.githubOptionDescription);
            content.githubUrl = Coalesce(content.githubUrl, DefaultContent.githubUrl);
            content.runLabel = Coalesce(content.runLabel, DefaultContent.runLabel);
            content.promptTemplate = Coalesce(content.promptTemplate, DefaultContent.promptTemplate);
            return content;
        }

        // Whitespace-only counts as absent (a blank remote value is a content mistake, not an intentional
        // blanking) — intentionally stricter than WelcomeContentManager's IsNullOrEmpty.
        private static string Coalesce(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;

    }
}
