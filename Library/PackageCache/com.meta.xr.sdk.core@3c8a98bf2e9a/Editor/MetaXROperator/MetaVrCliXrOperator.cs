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
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Meta.XR.AI.AgentBridge;
using Meta.XR.Guides.Editor.SdkUpgrader;
using Meta.XR.Guides.Editor.Welcome;
using Meta.XR.Json;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Editor
{
    /// <summary>
    /// Sets up Meta XR Operator through the Meta VR CLI (<c>metavr</c>): installing the
    /// <c>xroperator</c> tool and registering metavr's own MCP server with the selected AI
    /// coding assistant. XR Operator's tools reach the assistant by federating into that
    /// server, so the registered server name is <c>metavr</c>, not <c>meta-xr-operator</c>.
    /// </summary>
    internal static partial class MetaVrCliXrOperator
    {
        internal const string XrOperatorToolAlias = "xroperator";
        internal const string MetavrServerName = "metavr";
        internal const string LegacyServerName = "meta-xr-operator";

        // The CLI's name on PATH. Same spelling as MetavrServerName but a distinct concept: this is
        // the executable to invoke, that is the MCP server it registers.
        internal const string MetavrCommandName = "metavr";

        /// <summary>
        /// The <c>metavr mcp install</c> agent name for a panel service id, or null when the
        /// service has no metavr agent (devmate, plus anything unmapped) and the caller must
        /// fall back to <see cref="BuildFallbackMcpAddArgs"/>.
        /// </summary>
        internal static string MetaVrAgentForService(string serviceId) => serviceId switch
        {
            ClaudeCodeService.ServiceId => "claude-code",
            GeminiCliService.ServiceId => "gemini-cli",
            CodexService.ServiceId => "codex",
            OpenCodeService.ServiceId => "open-code",
            _ => null,
        };

        // claude-code needs --execute to actually run `claude mcp add ...`; it is print-only
        // otherwise. The file-based agents (gemini-cli/codex/open-code) write their config
        // directly and only need -y to stay non-interactive.
        internal static string BuildMcpInstallArgs(string metavrAgent) =>
            metavrAgent == "claude-code"
                ? $"mcp install {metavrAgent} --execute -y"
                : $"mcp install {metavrAgent} -y";

        /// <summary>
        /// Registers metavr's stdio MCP server through the agent's own <c>mcp add</c>, for agents
        /// that metavr has no subcommand for. The caller supplies <paramref name="metavrPathToken"/>
        /// already quoted.
        /// </summary>
        internal static string BuildFallbackMcpAddArgs(string metavrPathToken) =>
            $"mcp add {MetavrServerName} -- {metavrPathToken} mcp server --update";

        internal static string BuildRemoveLegacyEntryArgs() => $"mcp remove {LegacyServerName}";

        internal static string BuildToolsInstallArgs() => $"tools install {XrOperatorToolAlias}";

        internal static string BuildToolsUpdateArgs() => $"tools update {XrOperatorToolAlias}";

        /// <summary>
        /// The command that actually advances the tool, given whether it is already installed.
        /// The two are not interchangeable in either direction: <c>tools install</c> on an
        /// already-installed but stale tool exits 0 <em>without</em> upgrading (it only prints
        /// "Run <c>metavr tools update</c>"), and <c>tools update</c> exits 1 on a tool that was
        /// never installed.
        /// </summary>
        internal static string BuildToolsActionArgs(bool installed) =>
            installed ? BuildToolsUpdateArgs() : BuildToolsInstallArgs();

        // `tools list --json`, not `tools info <alias> --json`: only the list form carries
        // metavr's own `update_available` verdict. `tools info` returns just the raw
        // installed_version/latest_version strings, and `xroperator status` ignores --json
        // entirely and reports no versions at all.
        internal static string BuildToolsListJsonArgs() => "tools list --json";

        /// <summary>
        /// Reads the xroperator entry out of <c>metavr tools list --json</c>.
        /// <paramref name="json"/> anything unexpected yields <c>found: false</c>, which callers
        /// must treat as a failed probe (keep the last known state) rather than as an
        /// authoritative "not installed".
        /// </summary>
        internal static (bool found, bool installed, bool updateAvailable)
            ParseXrOperatorFromToolsList(string json)
        {
            if (string.IsNullOrEmpty(json)) return default;
            try
            {
                if (!(JsonNode.Parse(json) is JsonArray tools)) return default;

                foreach (var tool in tools)
                {
                    if (tool?["alias"]?.Value<string>() != XrOperatorToolAlias) continue;

                    // Symmetry with the alias / update_available handling: an entry whose
                    // `installed` field is absent or not a boolean is an unexpected shape, so
                    // report it as not-found (a failed probe that keeps the last known state)
                    // rather than as an authoritative "not installed".
                    var installedNode = tool["installed"];
                    if (installedNode == null || installedNode.Type != JsonNodeType.Boolean)
                        return default;
                    var installed = installedNode.Value<bool>();

                    // Prefer metavr's own verdict: it owns the comparison, and some tools report
                    // versions this side could not parse (platform-utils ships an HTML-escaped
                    // CRLF in latest_version). Fall back to comparing versions only when the field
                    // is genuinely absent, so a future CLI dropping it degrades the signal rather
                    // than silently killing it.
                    var verdict = tool["update_available"];
                    var updateAvailable = verdict != null && verdict.Type == JsonNodeType.Boolean
                        ? verdict.Value<bool>()
                        : IsOutOfDate(tool["installed_version"], tool["latest_version"]);

                    // An absent tool is an install, not an update.
                    return (true, installed, installed && updateAvailable);
                }
            }
            catch
            {
                // Malformed payload: fall through to the not-found default.
            }

            return default;
        }

        /// <summary>
        /// Version-comparison fallback for when <c>update_available</c> is missing. Unknown or
        /// unparseable versions read as "up to date" so the panel never offers an update it
        /// cannot justify.
        /// </summary>
        private static bool IsOutOfDate(JsonNode installedVersion, JsonNode latestVersion)
        {
            var installed = ParseVersionNode(installedVersion);
            var latest = ParseVersionNode(latestVersion);
            return installed != null && latest != null && installed < latest;
        }

        private static Version ParseVersionNode(JsonNode node)
        {
            try
            {
                return SdkUpgraderData.ParseCliVersion(node?.Value<string>());
            }
            catch
            {
                return null;
            }
        }

        // Some CLIs wrap list output in ANSI color codes; strip them so the line anchor below works.
        private static readonly Regex AnsiEscapeRegex = new(@"\x1b\[[0-9;]*[A-Za-z]");

        // Anchored at line start so a mid-line path mention of "metavr" isn't read as a registered
        // entry. "meta-xr-operator" must NOT match — that is the legacy (DirectSSE) server.
        private static readonly Regex MetavrListEntryRegex =
            new(@"(?m)^[\s✓✗⚠•\-\*]*metavr(?:\s|:|$)");

        /// <summary>
        /// True when the federated <c>metavr</c> MCP server appears as a registered entry in an
        /// <c>mcp list</c> output.
        /// </summary>
        internal static bool MetavrAppearsInMcpList(string output)
        {
            if (string.IsNullOrEmpty(output)) return false;
            var clean = AnsiEscapeRegex.Replace(output, string.Empty);
            return MetavrListEntryRegex.IsMatch(clean);
        }

        // --- Detection (async probe) and install ---

        // `metavr tools info` measures ~1.5s because it resolves `latest_version` over the NETWORK:
        // `metavr --version` (no network) returns in ~0.15s, the version string is not baked into
        // the CLI binary, and the local tools manifest carries only the installed version. That is
        // what makes update detection trustworthy — but it also means the probe can be slow or fail
        // when offline, so it runs on a background task (never in CreateGUI) and a failed probe is
        // recorded as "unknown" rather than "not installed".
        private const int ProbeTimeoutMs = 10000;
        private const int InstallTimeoutMs = 5 * 60 * 1000;

        // The SDK Upgrader's floor for the metavr CLI. It gates the `sdk-upgrader` SKILL, not
        // `tools install`, so it must never pre-block an install — a 1.3.x CLI installs xroperator
        // fine. It is only used to turn a raw install failure into an actionable message.
        private const string SdkUpgraderCliMinVersion = "1.5.0";

        // Last authoritative result, persisted so a reopened panel renders the right state at once
        // instead of flashing a wrong one for the duration of the probe. There is no cheap on-disk
        // seed available: the tool install root is the platform config dir (%APPDATA%/metavr on
        // Windows), which is NOT SdkUpgraderData.MetaVrHome() (~/.metavr). BOTH halves of the state
        // are persisted — persisting only "installed" is what used to make the Update button
        // disappear and reappear on every panel focus.
        private const string InstalledPrefKey = "Meta.XR.MetaXROperator.MetaVrCli.XrOperatorInstalled";
        private const string UpdateAvailablePrefKey =
            "Meta.XR.MetaXROperator.MetaVrCli.XrOperatorUpdateAvailable";

        // How long a resolved CLI path / probe result is trusted before a panel refresh re-checks.
        // Short enough that installing the CLI in a terminal shows up in seconds; long enough that
        // a single UI build (several descriptor queries) does at most one rescan.
        private const double CacheTtlSeconds = 10;

        private static readonly object _probeLock = new();
        private static double _lastResolveTime = double.NegativeInfinity;
        private static string _metavrPath;
        private static bool _metavrPathProbed;
        private static bool _probeInFlight;
        private static bool _seeded;
        private static bool _xrOperatorInstalled;
        private static bool _xrOperatorUpdateAvailable;

        // Whether the cached tool state is worth rendering at all — set by the first successful
        // probe (or the persisted seed) and NEVER cleared by staleness. Staleness is tracked
        // separately by _lastProbeTime, which only decides when to re-probe. Conflating the two is
        // what made a stale-but-known state render as "definitely no update available".
        private static bool _hasKnownState;
        private static double _lastProbeTime = double.NegativeInfinity;
        // Bumped by every reset; a probe that started earlier captures the epoch and refuses to
        // publish its now-stale result.
        private static int _probeEpoch;

        /// <summary>
        /// Cached metavr binary path. <see cref="SdkUpgraderData.FindMetaVrCliBinary"/> does a
        /// recursive scan of the npm cache plus a full PATH walk on every call and is not cached
        /// upstream; the panel would otherwise call it several times per UI build.
        /// </summary>
        internal static string FindMetaVrCli()
        {
            lock (_probeLock)
            {
                if (_metavrPathProbed) return _metavrPath;
            }

            // Resolved on the calling (main) thread: FindMetaVrCliBinary reads Application.platform.
            var path = SdkUpgraderData.FindMetaVrCliBinary();
            var now = EditorApplication.timeSinceStartup;
            lock (_probeLock)
            {
                _metavrPath = path;
                _metavrPathProbed = true;
                _lastResolveTime = now;
                return _metavrPath;
            }
        }

        /// <summary>
        /// Drops the cached state once it is older than <see cref="CacheTtlSeconds"/>, so a CLI or
        /// tool installed from a terminal is picked up without restarting Unity. Called at the top
        /// of the provider's descriptor build (i.e. on every panel refresh), so the TTL is what
        /// keeps the expensive <see cref="SdkUpgraderData.FindMetaVrCliBinary"/> rescan and the
        /// ~1.5s `tools info` probe from running several times per UI build. Main thread only.
        /// </summary>
        internal static void InvalidateIfStale()
        {
            lock (_probeLock)
            {
                if (EditorApplication.timeSinceStartup - _lastResolveTime < CacheTtlSeconds) return;

                // Stale-while-revalidate. Only the CLI path is dropped (cheap to re-resolve, and
                // re-resolving is how a CLI installed or removed outside the editor gets noticed);
                // the tool state is deliberately left in place so the card keeps rendering the last
                // known answer while the ~1.5s probe revalidates it in the background. Clearing it
                // here instead made every focus flip Update -> Installed -> Update.
                _metavrPathProbed = false;
                _metavrPath = null;
            }
        }

        /// <summary>True when the metavr CLI binary can be located.</summary>
        internal static bool IsMetaVrCliInstalled() => FindMetaVrCli() != null;

        /// <summary>
        /// Whether the <c>xroperator</c> tool is installed. Never blocks: returns the last known
        /// value (persisted across sessions) and kicks off a background probe when the cached
        /// value isn't authoritative yet. Must be called from the main thread.
        /// </summary>
        internal static bool IsXrOperatorInstalled()
        {
            var metavr = FindMetaVrCli();
            if (metavr == null) return false;

            lock (_probeLock)
            {
                if (!_seeded)
                {
                    _xrOperatorInstalled = EditorPrefs.GetBool(InstalledPrefKey, false);
                    _xrOperatorUpdateAvailable = EditorPrefs.GetBool(UpdateAvailablePrefKey, false);
                    // A persisted result is a real previous answer, so it is renderable straight
                    // away; the probe below revalidates it.
                    _hasKnownState = EditorPrefs.HasKey(InstalledPrefKey);
                    _seeded = true;
                }
            }

            // Throttled internally, so calling this on every descriptor query is cheap.
            EnsureProbeStarted(metavr, AIToolsSetupModel.GetCurrentProjectRoot());

            lock (_probeLock)
            {
                return _xrOperatorInstalled;
            }
        }

        /// <summary>
        /// The Update-button decision, split out so it can be table-tested. An update is only
        /// meaningful for a tool that is actually installed, and only when we have a real answer
        /// about it — a state we have never successfully probed must never surface one.
        /// </summary>
        /// <remarks>
        /// Takes "do we have a known state", NOT "did the current probe cycle just succeed". A
        /// known-but-stale state still renders; it is corrected in place when the next probe lands.
        /// </remarks>
        internal static bool ShouldOfferUpdate(
            bool hasKnownState, bool installed, bool updateAvailable) =>
            hasKnownState && installed && updateAvailable;

        /// <summary>
        /// True when metavr reports an update available for the installed xroperator. Deliberately
        /// does not trigger a probe — the wizard only asks this once IsInstalled() is true.
        /// </summary>
        internal static bool XrOperatorNeedsUpdate()
        {
            lock (_probeLock)
            {
                return ShouldOfferUpdate(
                    _hasKnownState, _xrOperatorInstalled, _xrOperatorUpdateAvailable);
            }
        }

        /// <summary>
        /// Forces the next query to re-resolve the CLI and re-probe, for when we know something
        /// changed (an install/update finished, or the CLI installer ran). The last known tool
        /// state is intentionally kept so the card doesn't flash a wrong value in the meantime.
        /// </summary>
        internal static void ResetProbe()
        {
            lock (_probeLock)
            {
                _metavrPathProbed = false;
                _metavrPath = null;
                _lastProbeTime = double.NegativeInfinity;
                _probeEpoch++;
            }
        }

        /// <summary>
        /// Whether a background probe should start now. Pure so the throttling rules are testable:
        /// never overlap probes, always probe when nothing is known yet, otherwise re-probe only
        /// once the cached answer is older than the TTL.
        /// </summary>
        internal static bool ShouldStartProbe(
            bool probeInFlight, bool hasKnownState, double secondsSinceLastProbe, double ttlSeconds)
            => !probeInFlight && (!hasKnownState || secondsSinceLastProbe >= ttlSeconds);

        private static void EnsureProbeStarted(string metavr, string projectRoot)
        {
            int epoch;
            lock (_probeLock)
            {
                if (!ShouldStartProbe(
                        _probeInFlight,
                        _hasKnownState,
                        EditorApplication.timeSinceStartup - _lastProbeTime,
                        CacheTtlSeconds))
                {
                    return;
                }

                _probeInFlight = true;
                epoch = _probeEpoch;
            }

            _ = Task.Run(() => RunProbeAsync(metavr, projectRoot, epoch));
        }

        private static async Task RunProbeAsync(string metavr, string projectRoot, int epoch)
        {
            var probe = await ProbeXrOperatorAsync(metavr, projectRoot);

            // Publish on the main thread: EditorApplication.timeSinceStartup, EditorPrefs and the
            // panel rebuild are all main-thread only.
            EditorApplication.delayCall += () => PublishProbeResult(probe, epoch);
        }

        private static void PublishProbeResult(
            (bool probeSucceeded, bool installed, bool updateAvailable) probe, int epoch)
        {
            bool changed;
            lock (_probeLock)
            {
                _probeInFlight = false;

                // A reset landed mid-probe: discard rather than overwrite the post-reset state,
                // and leave _lastProbeTime alone so the next query starts a fresh probe.
                if (epoch != _probeEpoch) return;

                // Stamp even on failure, so an offline editor retries on the TTL instead of
                // spawning a probe on every descriptor query.
                _lastProbeTime = EditorApplication.timeSinceStartup;

                // Only latch on a SUCCESSFUL probe, so a transient failure keeps the last known
                // answer and is never remembered as "not installed" or "no update".
                if (!probe.probeSucceeded) return;

                changed = !_hasKnownState
                    || _xrOperatorInstalled != probe.installed
                    || _xrOperatorUpdateAvailable != probe.updateAvailable;
                _xrOperatorInstalled = probe.installed;
                _xrOperatorUpdateAvailable = probe.updateAvailable;
                _hasKnownState = true;
                _seeded = true;
            }

            EditorPrefs.SetBool(InstalledPrefKey, probe.installed);
            EditorPrefs.SetBool(UpdateAvailablePrefKey, probe.updateAvailable);
            if (changed) AIToolsSetupModel.NotifyProviderStateChanged();
        }

        /// <summary>
        /// Turns one <c>tools list --json</c> invocation into a probe result. Pure, so the rule
        /// that matters here is testable: a probe that did not produce a usable answer must report
        /// <c>probeSucceeded: false</c> and never be mistaken for an authoritative "not installed"
        /// or "no update" — those would wrongly flip the card and hide the Update button.
        /// </summary>
        internal static (bool probeSucceeded, bool installed, bool updateAvailable)
            InterpretProbeOutput(int exitCode, bool timedOut, string stdout)
        {
            if (timedOut || exitCode != 0) return default;

            // Exit 0 with no usable xroperator entry means the output shape is not what we
            // expect, which is a failed probe — not an authoritative "not installed".
            var entry = ParseXrOperatorFromToolsList(stdout);
            if (!entry.found) return default;

            return (true, entry.installed, entry.updateAvailable);
        }

        private static async Task<(bool probeSucceeded, bool installed, bool updateAvailable)>
            ProbeXrOperatorAsync(string metavr, string projectRoot)
        {
            try
            {
                var r = await AIToolsSetupModel.RunProcessAsync(
                    metavr, BuildToolsListJsonArgs(), CancellationToken.None,
                    ProbeTimeoutMs, projectRoot);

                return InterpretProbeOutput(r.exitCode, r.timedOut, r.stdout);
            }
            catch
            {
                // RunProcessAsync throws when the binary vanished between resolution and launch.
                // Treat as a failed probe, never as "not installed".
                return default;
            }
        }

        /// <summary>State A action: open the public Meta VR CLI prerequisites page.</summary>
        internal static void OpenMetaVrCliDownload() =>
            Application.OpenURL(AIToolsSetupStrings.Links.MetaVrCliDownloadUrl);

        // --- State A: direct Meta VR CLI install (gated exactly like the Welcome card) ---

        /// <summary>
        /// The State-A CTA decision, split out so it can be table-tested: offer a real "Install"
        /// only when the remote content enables it AND the running editor has an installer.
        /// Mirrors <c>WelcomeSettings.XrTools.ResolveMetaVrCliCta</c>.
        /// </summary>
        internal static bool ShouldOfferDirectCliInstall(
            bool remoteFlagEnabled, bool platformSupported) =>
            remoteFlagEnabled && platformSupported;

        /// <summary>
        /// Whether Step 1 should offer to install the Meta VR CLI itself rather than link to its
        /// prerequisites page. Reads the same remote flag and the same platform check the Welcome
        /// card uses, so the two entry points cannot disagree.
        /// </summary>
        /// <remarks>
        /// Deliberately does NOT mirror the Welcome card's <c>OVR_INTERNAL_CODE</c> widening of
        /// <c>supportsDirectInstall</c> via <c>SdkUpgraderData.MetaVrCliStateOverride</c>: that
        /// override exists so internal developers can preview the card's CTA text, and honoring it
        /// here would surface a functional Install button on a platform where
        /// <c>MetaVrCliInstaller</c> has no installer and the click could only fail.
        /// </remarks>
        internal static bool IsDirectCliInstallAvailable() =>
            ShouldOfferDirectCliInstall(
                WelcomeSettings.XrTools.IsMetaVrCliDirectInstallEnabled(),
                MetaVrCliInstaller.SupportsDirectInstall(Application.platform));

        /// <summary>
        /// State A action when <see cref="IsDirectCliInstallAvailable"/>: run the Welcome card's
        /// Meta VR CLI installer and then continue straight into the XR Operator install, so one
        /// click satisfies the whole prerequisite. Both halves are needed because the step reports
        /// Complete off <see cref="IsXrOperatorInstalled"/>, not off the CLI being present.
        /// </summary>
        internal static async Task<(bool success, string error)> InstallMetaVrCliAsync(
            CancellationToken token)
        {
            if (!IsDirectCliInstallAvailable())
                return (false, AIToolsSetupStrings.Errors.MetaVrCliDirectInstallUnavailable);

            var finished = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);

            void OnInstallerStateChanged()
            {
                // Install() raises this synchronously on start (IsInstalling still true) and again
                // from Complete(); only the second one ends the wait.
                if (!MetaVrCliInstaller.IsInstalling) finished.TrySetResult(true);
            }

            MetaVrCliInstaller.StateChanged += OnInstallerStateChanged;
            try
            {
                // Subscribed before the check so a completion can't slip through the gap. If the
                // Welcome card already has an install running, join it rather than starting a
                // second one (Install() would refuse anyway).
                if (!MetaVrCliInstaller.IsInstalling && !MetaVrCliInstaller.Install())
                    return (false, AIToolsSetupStrings.Errors.MetaVrCliInstallFailed);

                using (token.Register(() => finished.TrySetResult(false)))
                {
                    await finished.Task;
                }
            }
            finally
            {
                MetaVrCliInstaller.StateChanged -= OnInstallerStateChanged;
            }

            if (token.IsCancellationRequested) return (false, null);

            // MetaVrCliInstaller reports failures to the Console rather than to its caller, so the
            // binary appearing on disk is the only success signal available here.
            ResetProbe();
            if (FindMetaVrCli() == null)
                return (false, AIToolsSetupStrings.Errors.MetaVrCliInstallFailed);

            return await InstallXrOperatorAsync(token);
        }

        /// <summary>
        /// State B action and the Update action: <c>metavr tools install xroperator</c> or
        /// <c>metavr tools update xroperator</c>, whichever matches the tool's current state. This
        /// invokes the user's already-installed first-party CLI to fetch a Meta-distributed tool;
        /// it never downloads and executes a script.
        /// </summary>
        internal static async Task<(bool success, string error)> InstallXrOperatorAsync(
            CancellationToken token)
        {
            var metavr = FindMetaVrCli();
            if (metavr == null)
                return (false, AIToolsSetupStrings.Errors.MetaVrCliMissingForInstall);

            var projectRoot = AIToolsSetupModel.GetCurrentProjectRoot();

            // The wizard routes both its Install and its Update button here, so the command has to
            // be chosen from the tool's actual state (see BuildToolsActionArgs — install is a
            // silent no-op on a stale tool, update is a hard error on a missing one). Re-probed
            // rather than read from the cache: this runs on a click, where one ~1.5s probe is cheap
            // next to the download that follows, and a stale flag picks a command that fails in a
            // way the user cannot act on. A failed probe falls back to the last known value.
            var before = await ProbeXrOperatorAsync(metavr, projectRoot);
            bool installed;
            if (before.probeSucceeded)
            {
                installed = before.installed;
            }
            else
            {
                lock (_probeLock) installed = _xrOperatorInstalled;
            }

            var r = await AIToolsSetupModel.RunProcessAsync(
                metavr, BuildToolsActionArgs(installed), token, InstallTimeoutMs, projectRoot);

            ResetProbe();

            if (token.IsCancellationRequested) return (false, null);

            if (r.timedOut || r.exitCode != 0)
            {
                // An out-of-date CLI is a common cause and the raw CLI text rarely says so.
                if (SdkUpgraderData.GetMetaVrCliState(SdkUpgraderCliMinVersion)
                    == SdkUpgraderData.MetaVrCliState.Outdated)
                {
                    return (false, AIToolsSetupStrings.Errors.MetaVrCliOutdatedForInstall);
                }

                var detail = string.IsNullOrWhiteSpace(r.stderr) ? r.stdout : r.stderr;
                return (false, string.Format(
                    installed
                        ? AIToolsSetupStrings.Errors.XROperatorUpdateFailed
                        : AIToolsSetupStrings.Errors.XROperatorInstallFailed,
                    detail));
            }

            // Exit 0 is the CLI's success verdict. Re-probe so the card clears its Update state,
            // but don't let a probe hiccup fail the install. ResetProbe above already bumped the
            // epoch, so publish under the current one.
            var probe = await ProbeXrOperatorAsync(metavr, projectRoot);
            int currentEpoch;
            lock (_probeLock) currentEpoch = _probeEpoch;
            EditorApplication.delayCall += () => PublishProbeResult(probe, currentEpoch);

            return (true, null);
        }
    }
}
