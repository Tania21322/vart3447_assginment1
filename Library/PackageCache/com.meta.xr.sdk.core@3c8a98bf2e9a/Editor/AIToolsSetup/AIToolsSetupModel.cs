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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Meta.XR.AI.AgentBridge;
using Meta.XR.Editor.Id;
using UnityEditor;
using UnityEngine;
using AgentBridgeSettings = Meta.XR.AI.AgentBridge.Settings;

namespace Meta.XR.Editor
{
    internal class AIToolsSetupModel
    {
        internal enum StepState
        {
            Incomplete,
            Processing,
            Complete,
            Error
        }

        private const int ProcessTimeoutMs = 15000;

        // Generous next to ProcessTimeoutMs: a cold `adb` start-server plus a device round trip is slow,
        // and each headset setup step may run this once per connected device.
        private const int DeviceCommandTimeoutMs = 60000;
        private const int VerifyTimeoutMs = 30000;
        private const int StatusRefreshDelayMs = 2000;

        // --- Step state ---

        internal StepState Step1State => ComputeStep1State();
        internal StepState Step1BridgeState { get; private set; } = StepState.Incomplete;
        internal StepState Step2State { get; private set; } = StepState.Incomplete;
        internal StepState Step3State { get; private set; } = StepState.Incomplete;

        // Windows Firewall sub-step (Windows-only; tracked independently so it never blocks Step 1 on
        // other platforms or for users who haven't set it up yet).
        internal StepState FirewallState { get; private set; } = StepState.Incomplete;
        internal string FirewallError { get; private set; }

        /// <summary>State of a single one-click ADB action, rendered as a sub-step inside a product card.</summary>
        internal class DeviceAction
        {
            internal StepState State { get; set; } = StepState.Incomplete;
            internal string Error { get; set; }
            internal string Detail { get; set; }
        }

        // AgentBridge card: `adb reverse` sub-step (device -> host) so the in-headset assistant reaches the
        // Remote Agent Server at 127.0.0.1. Re-runnable; also configured automatically on server start.
        internal DeviceAction AdbReverse { get; } = new();

        // XR Operator card: the device setup a Quest APK build needs, run as one action. HeadsetSetup is
        // the overall state behind the button; the steps below are its checklist, in run order.
        // `adb forward` (host -> device) lets the desktop MCP proxy reach the on-device Meta XR Operator
        // tool endpoint (port 8720) at 127.0.0.1; the rest are device switches the tool set needs. The
        // whole thing is re-runnable: none of it survives a headset reboot.
        internal DeviceAction HeadsetSetup { get; } = new();
        internal DeviceAction HeadsetUndo { get; } = new();
        internal DeviceAction AdbForward { get; } = new();
        internal DeviceAction ExperimentalFeatures { get; } = new();
        internal DeviceAction CapturePermission { get; } = new();
        internal DeviceAction ProximitySensor { get; } = new();

        /// <summary>The <see cref="SetUpHeadsetAsync"/> steps, in the order they run.</summary>
        internal IReadOnlyList<DeviceAction> HeadsetSetupSteps { get; }

        internal string Step1Error => Step1BridgeError ?? FirstPrerequisiteError();
        internal string Step1BridgeError { get; private set; }
        internal string Step2Error { get; private set; }
        internal string Step3Error { get; private set; }

        internal string Step2SuccessDetail { get; set; }
        internal string Step3SuccessDetail { get; private set; }

        // --- Step 2 per-line state (bridge = Editor tools, runtime = Runtime tools) ---

        internal StepState Step2BridgeState { get; private set; } = StepState.Incomplete;
        internal string Step2BridgeError { get; private set; }
        internal string Step2BridgeSuccessDetail { get; private set; }

        internal StepState Step2RuntimeState { get; private set; } = StepState.Incomplete;
        internal string Step2RuntimeError { get; private set; }
        internal string Step2RuntimeSuccessDetail { get; private set; }

        // > 0 while one or more connection verifications are in flight (drives the "checking"
        // spinner). A counter rather than a bool so an outgoing (cancelled) verify's cleanup
        // can't clear the flag while a newer verify is still running.
        private int _verifyingCount;
        internal bool IsVerifying => _verifyingCount > 0;

        /// <summary>
        /// True when both products in the Install &amp; Setup step are connected (their MCP servers
        /// are registered/verified). Drives the "completed full setup" telemetry on close.
        /// </summary>
        internal bool IsFullSetupComplete =>
            Step2RuntimeState == StepState.Complete;

        // --- Prerequisite (sub-step A of Step 1) tracking, keyed by provider id ---

        internal class PrerequisiteStatus
        {
            public StepState State;
            public string Error;
            public bool UpdateAvailable;
        }

        private readonly Dictionary<string, PrerequisiteStatus> _prerequisiteStates = new();
        internal IReadOnlyDictionary<string, PrerequisiteStatus> PrerequisiteStates => _prerequisiteStates;

        /// <summary>
        /// Raised by a provider when an async prerequisite probe lands, so open windows re-derive
        /// their step state. Mirrors <see cref="WindowsFirewallUtility.StatusChanged"/>: providers
        /// cannot call the window directly because the assembly dependency runs the other way
        /// (providers reference AIToolsSetup, not vice versa).
        /// </summary>
        internal static event Action ProviderStateChanged;

        /// <summary>Raises <see cref="ProviderStateChanged"/>. Must be called on the main thread.</summary>
        internal static void NotifyProviderStateChanged() => ProviderStateChanged?.Invoke();

        // --- Settings ---

        internal string SelectedServiceId { get; set; }
        internal bool ShowAdvancedSettings { get; set; }

        internal enum InstallCheckState { Unknown, Checking, Installed, NotInstalled }

        internal InstallCheckState SelectedServiceInstallState { get; private set; } = InstallCheckState.Unknown;
        internal string SelectedServiceInstallMessage { get; private set; } = string.Empty;

        internal bool SelectedServiceIsKnownNotInstalled =>
            SelectedServiceInstallState == InstallCheckState.NotInstalled;

        // Services whose IServiceValidation does NOT reflect CLI installation and/or is slow, so they
        // must not drive the install-detection warning. Devmate's validation probes a local HTTP bridge
        // (VS Code extension) with a multi-second timeout — "Invalid" means the bridge is down, not that
        // a CLI is missing — so we rely on the launch-time "is it installed and in your PATH?" error instead.
        private static readonly HashSet<string> InstallDetectionExcludedServiceIds =
            new() { "devmate" };

        internal static bool IsInstallDetectionExcluded(string serviceId) =>
            !string.IsNullOrEmpty(serviceId) && InstallDetectionExcludedServiceIds.Contains(serviceId);

        private CancellationTokenSource _cancellation;

        // Monotonic guard for CheckSelectedServiceInstalledAsync. Kept SEPARATE from _cancellation
        // (shared by VerifyConnectionAsync/SetupProvidersAsync) so the install check and connection
        // verify don't cancel each other. A check discards its result if the generation advanced
        // (newer check started, or the selected service changed) before it writes state.
        private int _installCheckGeneration;
        private Action _onStateChanged;

        internal AIToolsSetupModel()
        {
            HeadsetSetupSteps = new[]
            {
                AdbForward, ExperimentalFeatures, CapturePermission, ProximitySensor
            };
            SelectedServiceId = AgentBridgeSettings.SelectedServiceId.Value;
            RefreshStepStates();
        }

        internal void SetStateChangedCallback(Action callback)
        {
            _onStateChanged = callback;
        }

        internal void SyncSelectedServiceFromSettings()
        {
            var settingValue = AgentBridgeSettings.SelectedServiceId.Value;
            if (SelectedServiceId != settingValue)
            {
                OnSelectedServiceChanged(settingValue);
            }
        }

        internal void RefreshStepStates()
        {
            SyncSelectedServiceFromSettings();

            Step1BridgeState = AgentBridgeSettings.IsEnabled
                ? StepState.Complete
                : StepState.Incomplete;

            // Initialize / refresh prerequisite states from each provider
            foreach (var provider in AIToolsSetupRegistry.GetProviders())
            {
                var prereq = provider.GetPrerequisiteInstall();
                if (prereq == null)
                {
                    _prerequisiteStates.Remove(provider.Id);
                    continue;
                }

                // Don't clobber an in-flight Processing or sticky Error state.
                if (_prerequisiteStates.TryGetValue(provider.Id, out var existing)
                    && (existing.State == StepState.Processing || existing.State == StepState.Error))
                {
                    continue;
                }

                var installed = false;
                try { installed = prereq.IsInstalled?.Invoke() ?? false; }
                catch { installed = false; }

                // Only meaningful when installed: is the on-disk copy stale vs the SDK-bundled binary?
                var updateAvailable = false;
                if (installed)
                {
                    try { updateAvailable = prereq.NeedsUpdate?.Invoke() ?? false; }
                    catch { updateAvailable = false; }
                }

                _prerequisiteStates[provider.Id] = new PrerequisiteStatus
                {
                    State = installed ? StepState.Complete : StepState.Incomplete,
                    Error = null,
                    UpdateAvailable = updateAvailable
                };
            }

            // Runtime tools come from the Meta XR Operator provider; mirror its connection state.
            Step2State = Step2RuntimeState;
            if (Step2State == StepState.Complete)
            {
                Step2SuccessDetail = string.Format(
                    AIToolsSetupStrings.ConnectionStatus.AssistantConnectedFormat,
                    GetSelectedServiceDisplayName());
            }

            Step3State = Step2State == StepState.Complete ? StepState.Complete : StepState.Incomplete;
            if (Step3State == StepState.Complete)
            {
                Step3SuccessDetail = AIToolsSetupStrings.ConnectionStatus.ConnectionVerified;
            }

            RefreshFirewallState();
            RefreshProjectSetupState();
        }

        // --- Windows Firewall sub-step ---

        /// <summary>
        /// Re-derives <see cref="FirewallState"/> from the shared firewall cache. Called when the rule is
        /// changed outside the wizard (e.g. the Meta/Internal debug menu) so the open window stays in sync.
        /// </summary>
        internal void SyncFirewallState() => RefreshFirewallState();

        private void RefreshFirewallState()
        {
            // Not applicable off Windows — treat as satisfied so it never renders as pending.
            if (!IsWindows())
            {
                FirewallState = StepState.Complete;
                return;
            }

            // Don't clobber an in-flight configure or a sticky error from a previous attempt.
            if (FirewallState == StepState.Processing || FirewallState == StepState.Error)
            {
                return;
            }

            if (WindowsFirewallUtility.CachedStatus == FirewallRuleStatus.Unknown)
            {
                // Kick off a one-shot async check; the UI refreshes when it completes.
                FirewallState = StepState.Processing;
                WindowsFirewallUtility.RefreshStatus(() =>
                {
                    MapFirewallStateFromCache();
                    _onStateChanged?.Invoke();
                });
                return;
            }

            MapFirewallStateFromCache();
        }

        private void MapFirewallStateFromCache()
        {
            FirewallState = WindowsFirewallUtility.CachedStatus switch
            {
                FirewallRuleStatus.Configured => StepState.Complete,
                FirewallRuleStatus.NotSupported => StepState.Complete,
                FirewallRuleStatus.Checking => StepState.Processing,
                _ => StepState.Incomplete,
            };
        }

        internal async Task EnsureFirewallAsync()
        {
            if (FirewallState == StepState.Processing)
            {
                return;
            }

            FirewallState = StepState.Processing;
            FirewallError = null;
            _onStateChanged?.Invoke();

            try
            {
                var (success, error) = await WindowsFirewallUtility.EnsureInboundRuleAsync(
                    WindowsFirewallUtility.BridgePorts());

                if (success)
                {
                    FirewallState = StepState.Complete;
                    FirewallError = null;
                }
                else
                {
                    FirewallState = StepState.Error;
                    FirewallError = error ?? AIToolsSetupStrings.Firewall.NotConfigured;
                }
            }
            catch (Exception e)
            {
                FirewallState = StepState.Error;
                FirewallError = e.Message;
            }
            finally
            {
                _onStateChanged?.Invoke();
            }
        }

        // --- Per-card ADB sub-steps ---

        /// <summary>
        /// Configures <c>adb reverse</c> for the AgentBridge Remote Server port(s) on every authorized device
        /// (device -> host), so a Quest build's in-headset assistant can reach the server at <c>127.0.0.1</c>.
        /// </summary>
        internal Task EnsureAgentBridgeReverseAsync()
        {
            // Resolve the SDK path and ports on the main thread: ReversePorts() reads EditorPrefs
            // (RemoteAgentSettings.Port), which throws off the main thread.
            var sdkPath = OVRConfig.Instance.GetAndroidSDKPath();
            var ports = AdbSetupUtility.ReversePorts();
            return RunDeviceActionAsync(
                AdbReverse,
                () => AdbSetupUtility.ConfigureReverse(sdkPath, ports),
                result => string.Format(
                    AIToolsSetupStrings.Device.ReverseConfiguredFormat,
                    result.AuthorizedDeviceCount, string.Join(", ", ports)));
        }

        /// <summary>
        /// Runs every on-device step a Quest APK build needs, in order, across all authorized devices:
        /// <c>adb forward</c> for the Meta XR Operator port, the experimental-features and screen-capture
        /// system properties, then the proximity-sensor override.
        /// </summary>
        /// <remarks>
        /// Steps run sequentially and each publishes its own state as it goes, so the card's checklist
        /// shows what is running now and which step failed. A failing step does not stop the rest: they
        /// are independent, and stopping early would hide a second, unrelated failure.
        /// </remarks>
        internal async Task SetUpHeadsetAsync()
        {
            if (IsHeadsetActionRunning)
            {
                return;
            }

            var sdkPath = OVRConfig.Instance.GetAndroidSDKPath();
            var forwardPorts = AdbSetupUtility.ForwardPorts();

            BeginHeadsetAction(HeadsetSetup, HeadsetUndo);

            var results = new[]
            {
                await RunHeadsetStepAsync(
                    AdbForward, () => AdbSetupUtility.ConfigureForward(sdkPath, forwardPorts)),
                await RunHeadsetStepAsync(
                    ExperimentalFeatures, () => AdbSetupUtility.EnableExperimentalFeatures(sdkPath)),
                await RunHeadsetStepAsync(
                    CapturePermission, () => AdbSetupUtility.RequestCapturePermissionOnStart(sdkPath)),
                await RunHeadsetStepAsync(
                    ProximitySensor, () => AdbSetupUtility.DisableProximitySensor(sdkPath)),
            };

            FinishHeadsetAction(HeadsetSetup, results, AIToolsSetupStrings.Device.SetUpCompleteFormat);
            _onStateChanged?.Invoke();
        }

        /// <summary>
        /// Puts every setting <see cref="SetUpHeadsetAsync"/> applied back the way it was: drops the port
        /// forward, returns both system properties to <c>0</c>, and hands the proximity sensor back to
        /// hardware.
        /// </summary>
        /// <remarks>
        /// The steps reuse the same checklist rows as setup so the run shows live progress. On success the
        /// rows reset to pending, because a green "Forward MCP server port" next to an undone tunnel would
        /// claim the opposite of what is true on the device.
        /// </remarks>
        internal async Task UndoHeadsetSetupAsync()
        {
            if (IsHeadsetActionRunning)
            {
                return;
            }

            var sdkPath = OVRConfig.Instance.GetAndroidSDKPath();
            var forwardPorts = AdbSetupUtility.ForwardPorts();

            BeginHeadsetAction(HeadsetUndo, HeadsetSetup);

            var results = new[]
            {
                await RunHeadsetStepAsync(
                    AdbForward, () => AdbSetupUtility.RemoveForward(sdkPath, forwardPorts)),
                await RunHeadsetStepAsync(
                    ExperimentalFeatures, () => AdbSetupUtility.DisableExperimentalFeatures(sdkPath)),
                await RunHeadsetStepAsync(
                    CapturePermission, () => AdbSetupUtility.ClearCapturePermissionOnStart(sdkPath)),
                await RunHeadsetStepAsync(
                    ProximitySensor, () => AdbSetupUtility.RestoreProximitySensor(sdkPath)),
            };

            FinishHeadsetAction(HeadsetUndo, results, AIToolsSetupStrings.Device.UndoCompleteFormat);
            if (HeadsetUndo.State == StepState.Complete)
            {
                ResetHeadsetSteps();
            }

            _onStateChanged?.Invoke();
        }

        /// <summary>True while either headset action is mid-run; both buttons stay disabled until it ends.</summary>
        internal bool IsHeadsetActionRunning =>
            HeadsetSetup.State == StepState.Processing || HeadsetUndo.State == StepState.Processing;

        // Setup and undo share one status line, so starting either clears the other's result.
        private void BeginHeadsetAction(DeviceAction action, DeviceAction other)
        {
            action.State = StepState.Processing;
            action.Error = null;
            action.Detail = null;

            other.State = StepState.Incomplete;
            other.Error = null;
            other.Detail = null;

            ResetHeadsetSteps();
            _onStateChanged?.Invoke();
        }

        private void FinishHeadsetAction(
            DeviceAction action, IReadOnlyList<AdbSetupUtility.DeviceResult> results, string successFormat)
        {
            var failed = HeadsetSetupSteps.FirstOrDefault(step => step.State == StepState.Error);
            if (failed == null)
            {
                action.State = StepState.Complete;
                action.Error = null;
                action.Detail = string.Format(
                    successFormat, results.Max(result => result.AuthorizedDeviceCount));
            }
            else
            {
                action.State = StepState.Error;
                action.Error = failed.Error;
                action.Detail = null;
            }
        }

        private void ResetHeadsetSteps()
        {
            foreach (var step in HeadsetSetupSteps)
            {
                step.State = StepState.Incomplete;
                step.Error = null;
                step.Detail = null;
            }
        }

        private async Task<AdbSetupUtility.DeviceResult> RunHeadsetStepAsync(
            DeviceAction step, Func<AdbSetupUtility.DeviceResult> run)
        {
            step.State = StepState.Processing;
            step.Error = null;
            _onStateChanged?.Invoke();

            try
            {
                var (timedOut, result) = await RunDeviceCommandAsync(run);
                if (timedOut)
                {
                    step.State = StepState.Error;
                    step.Error = AIToolsSetupStrings.Device.TimedOut;
                    return default;
                }

                step.State = result.Ok ? StepState.Complete : StepState.Error;
                step.Error = result.Ok ? null : DescribeDeviceFailure(result);
                return result;
            }
            catch (Exception e)
            {
                step.State = StepState.Error;
                step.Error = e.Message;
                return default;
            }
            finally
            {
                _onStateChanged?.Invoke();
            }
        }

        /// <summary>
        /// Runs a blocking ADB call off the main thread, giving up after
        /// <see cref="DeviceCommandTimeoutMs"/>.
        /// </summary>
        /// <remarks>
        /// <c>OVRADBTool.RunCommand</c> waits on the adb process with no timeout and never kills it, so a
        /// wedged adb (offline device, stale server) would otherwise leave the action stuck in
        /// <see cref="StepState.Processing"/> — and the Processing guards below block every retry for the
        /// rest of the editor session. The abandoned task keeps running until adb finally returns; it only
        /// writes to state we discard, so letting it finish unobserved is safe.
        /// </remarks>
        private static async Task<(bool timedOut, AdbSetupUtility.DeviceResult result)> RunDeviceCommandAsync(
            Func<AdbSetupUtility.DeviceResult> run)
        {
            var work = Task.Run(run);
            var finished = await Task.WhenAny(work, Task.Delay(DeviceCommandTimeoutMs));
            return finished == work ? (false, await work) : (true, default);
        }

        private async Task RunDeviceActionAsync(
            DeviceAction action,
            Func<AdbSetupUtility.DeviceResult> run,
            Func<AdbSetupUtility.DeviceResult, string> describeSuccess)
        {
            if (action.State == StepState.Processing)
            {
                return;
            }

            action.State = StepState.Processing;
            action.Error = null;
            _onStateChanged?.Invoke();

            try
            {
                var (timedOut, result) = await RunDeviceCommandAsync(run);
                if (timedOut)
                {
                    action.State = StepState.Error;
                    action.Error = AIToolsSetupStrings.Device.TimedOut;
                    action.Detail = null;
                }
                else if (result.Ok)
                {
                    action.State = StepState.Complete;
                    action.Error = null;
                    action.Detail = describeSuccess(result);
                }
                else
                {
                    action.State = StepState.Error;
                    action.Error = DescribeDeviceFailure(result);
                    action.Detail = null;
                }
            }
            catch (Exception e)
            {
                action.State = StepState.Error;
                action.Error = e.Message;
                action.Detail = null;
            }
            finally
            {
                _onStateChanged?.Invoke();
            }
        }

        private static string DescribeDeviceFailure(AdbSetupUtility.DeviceResult result)
        {
            if (!result.AdbAvailable)
            {
                return AIToolsSetupStrings.Device.AdbUnavailable;
            }
            if (result.AuthorizedDeviceCount == 0)
            {
                return result.HasUnauthorizedDevice
                    ? AIToolsSetupStrings.Device.DeviceUnauthorized
                    : AIToolsSetupStrings.Device.NoDeviceDetected;
            }
            // Prefer what the device actually said: a bare failure count gives nothing to act on.
            return result.FailureMessage
                   ?? string.Format(AIToolsSetupStrings.Device.PartialFailureFormat, result.FailureCount);
        }

        // --- Project Setup Tool (final step) ---

        // Message substrings that identify Project Setup Tool rules belonging to the AI-tools features
        // this window sets up. OVRConfigurationTask carries no per-feature identifier (only a coarse
        // TaskGroup shared with unrelated rules), so the rule message is the only available signal.
        // Today only Meta XR Operator registers such rules; the Bridge/Debugger markers are
        // forward-looking so any rules they add later surface here automatically.
        private static readonly string[] AIToolRuleMarkers =
        {
            "Meta XR Operator",
            "AI Agent Bridge",
            "Immersive Debugger",
        };

        // Outstanding rules specific to this window's AI-tools features (Meta XR Operator, etc.). This
        // step only calls these out; other Project Setup Tool rules are the developer's to review via
        // the "Open Project Setup Tool" button.
        internal int OutstandingAIToolRuleCount { get; private set; }
        internal bool HasOutstandingAIToolRules => OutstandingAIToolRuleCount > 0;

        /// <summary>
        /// Re-counts outstanding AI-tools Project Setup Tool rules. Exposed so the window can refresh
        /// after the user fixes rules in the Project Setup Tool and returns to this window.
        /// </summary>
        internal void SyncProjectSetupState() => RefreshProjectSetupState();

        private void RefreshProjectSetupState()
        {
            var aiToolCount = 0;
            var seen = new HashSet<Hash128>();
            foreach (var buildTargetGroup in SetupRulePlatformsToCheck())
            {
                foreach (var task in OVRProjectSetup.GetTasks(buildTargetGroup))
                {
                    // A platform-agnostic task appears under every build target group; count it once.
                    if (!seen.Add(task.Uid))
                        continue;
                    if (!OVRProjectSetupStatus.IsOutstanding(task, buildTargetGroup))
                        continue;
                    // Only count rules the Project Setup Tool itself surfaces. Rules in groups hidden from
                    // that window (e.g. HandReadiness) are never shown there, so counting them would report
                    // outstanding rules the user cannot see or fix from the tool this step points them to.
                    if (!OVRProjectSetup.IsTaskVisibleInProjectSetupTool(task))
                        continue;
                    var message = task.Message?.GetValue(buildTargetGroup);
                    if (message != null && MatchesAIToolRule(message))
                        aiToolCount++;
                }
            }
            OutstandingAIToolRuleCount = aiToolCount;
        }

        // Meta XR Operator registers its rules for Standalone, but the active build target may be
        // Android (Quest); check both (plus platform-agnostic rules, which appear under any group).
        private static IEnumerable<BuildTargetGroup> SetupRulePlatformsToCheck()
        {
            var active = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            yield return active;
            if (active != BuildTargetGroup.Standalone)
                yield return BuildTargetGroup.Standalone;
        }

        private static bool MatchesAIToolRule(string message)
        {
            foreach (var marker in AIToolRuleMarkers)
            {
                if (message.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private StepState ComputeStep1State()
        {
            var anyProcessing = Step1BridgeState == StepState.Processing;
            var anyError = Step1BridgeState == StepState.Error;
            var allComplete = Step1BridgeState == StepState.Complete;

            foreach (var kvp in _prerequisiteStates)
            {
                switch (kvp.Value.State)
                {
                    case StepState.Processing: anyProcessing = true; break;
                    case StepState.Error: anyError = true; break;
                    case StepState.Complete: break;
                    default: allComplete = false; break;
                }
                if (kvp.Value.State != StepState.Complete)
                    allComplete = false;
            }

            if (anyProcessing) return StepState.Processing;
            if (anyError) return StepState.Error;
            if (allComplete) return StepState.Complete;
            return StepState.Incomplete;
        }

        private string FirstPrerequisiteError()
        {
            foreach (var kvp in _prerequisiteStates)
            {
                if (kvp.Value.Error != null)
                    return kvp.Value.Error;
            }
            return null;
        }

        // --- Step 1: Install AI Agent Bridge ---

        internal void InstallBridgeAsync()
        {
            if (Step1BridgeState == StepState.Processing)
                return;

            var sw = System.Diagnostics.Stopwatch.StartNew();

            Step1BridgeState = StepState.Processing;
            Step1BridgeError = null;
            _onStateChanged?.Invoke();

            try
            {
                // Use the full enable path (not a bare Enabled.SetValue) so installing from setup also
                // turns on Remote Agent auto-start and starts the Remote + MCP servers — a direct SetValue
                // bypasses OnEnabledChanged and would leave the servers stopped and auto-start off.
                AgentBridgeSettings.EnableAndInitialize();
                Step1BridgeState = StepState.Complete;
            }
            catch (OperationCanceledException)
            {
                Step1BridgeState = StepState.Incomplete;
            }
            catch (Exception e)
            {
                Step1BridgeState = StepState.Error;
                Step1BridgeError = e.Message;
            }
            finally
            {
                sw.Stop();
                AIToolsSetupTelemetry.SendEvent(
                    AIToolsSetupTelemetryConstants.FalcoEventName.InstallCompleted,
                    evt =>
                    {
                        var success = Step1BridgeState == StepState.Complete;
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.Success, success);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.DurationMs, sw.ElapsedMilliseconds);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.BridgeEnabled, AgentBridgeSettings.IsEnabled);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.Subject,
                            AIToolsSetupTelemetryConstants.Subject.Bridge);
                        if (!success)
                        {
                            var errorKind = Step1BridgeState == StepState.Incomplete
                                ? AIToolsSetupTelemetryConstants.ErrorKind.Cancelled
                                : AIToolsSetupTelemetryConstants.ErrorKind.BridgeEnableFailed;
                            evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ErrorKind, errorKind);
                            if (Step1BridgeError != null)
                                evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ErrorMessage, Step1BridgeError);
                        }
                    },
                    isEssential: true);
                _onStateChanged?.Invoke();
            }
        }

        // --- Step 1 (sub-step A): Install MCP Proxy (or any provider prerequisite) ---

        internal async Task InstallPrerequisiteAsync(string providerId)
        {
            var provider = AIToolsSetupRegistry.GetProvider(providerId);
            var prereq = provider?.GetPrerequisiteInstall();
            if (prereq == null || prereq.InstallAsync == null)
                return;

            if (_prerequisiteStates.TryGetValue(providerId, out var existing)
                && existing.State == StepState.Processing)
            {
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var status = new PrerequisiteStatus { State = StepState.Processing, Error = null };
            _prerequisiteStates[providerId] = status;

            var cts = new CancellationTokenSource();
            _cancellation = cts;
            var token = cts.Token;
            _onStateChanged?.Invoke();

            try
            {
                var result = await prereq.InstallAsync(token);
                if (token.IsCancellationRequested)
                {
                    status.State = StepState.Incomplete;
                }
                else if (result.success)
                {
                    status.State = StepState.Complete;
                    status.Error = null;
                }
                else
                {
                    status.State = StepState.Error;
                    status.Error = result.error ?? AIToolsSetupStrings.Errors.ProxyInstallFailed;
                }
            }
            catch (OperationCanceledException)
            {
                status.State = StepState.Incomplete;
            }
            catch (Exception e)
            {
                status.State = StepState.Error;
                status.Error = string.Format(
                    AIToolsSetupStrings.Errors.ProxyInstallExceptionFormat, e.Message);
            }
            finally
            {
                sw.Stop();
                AIToolsSetupTelemetry.SendEvent(
                    AIToolsSetupTelemetryConstants.FalcoEventName.InstallCompleted,
                    evt =>
                    {
                        var success = status.State == StepState.Complete;
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.Success, success);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.DurationMs, sw.ElapsedMilliseconds);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.Subject,
                            AIToolsSetupTelemetryConstants.Subject.Proxy);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ProviderId, providerId);
                        if (!success)
                        {
                            var errorKind = status.State == StepState.Incomplete
                                ? AIToolsSetupTelemetryConstants.ErrorKind.Cancelled
                                : AIToolsSetupTelemetryConstants.ErrorKind.ProxyInstallFailed;
                            evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ErrorKind, errorKind);
                            if (status.Error != null)
                                evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ErrorMessage, status.Error);
                        }
                    },
                    isEssential: true);
                cts.Dispose();
                if (ReferenceEquals(_cancellation, cts))
                    _cancellation = null;
                _onStateChanged?.Invoke();
            }
        }

        // --- Step 2: Connect AI coding assistant ---

        internal string GetSelectedServiceDisplayName()
        {
            var services = AIServiceRegistry.GetAllServices();
            foreach (var service in services)
            {
                if (service.Id == SelectedServiceId)
                    return service.DisplayName;
            }
            return SelectedServiceId;
        }

        internal string GetSelectedServiceExecutable()
        {
            var service = AIServiceRegistry.GetService(SelectedServiceId);
            return service?.ExecutableName ?? SelectedServiceId;
        }

        internal bool CanAutoSetup()
        {
            return ServiceSupportsAutoRegister(SelectedServiceId);
        }

        // True only when the agent supports auto-setup AND we haven't positively detected it as missing.
        internal bool CanRunSetup => CanAutoSetup() && !SelectedServiceIsKnownNotInstalled;

        // Whether the selected AI coding assistant can be auto-registered from its CLI (it declares an
        // executable). Lives here, independent of the editor-tools MCP layer, so the public XR Operator
        // setup path keeps working without the internal-only MCP registration classes.
        internal static bool ServiceSupportsAutoRegister(string serviceId)
        {
            var service = AIServiceRegistry.GetService(serviceId);
            return !string.IsNullOrEmpty(service?.ExecutableName);
        }

        // Detects whether the currently-selected agent's CLI is available, via the service's own
        // IServiceValidation. MUST be called only AFTER AgentBridgeSettings.SelectedServiceId is
        // persisted for the selection, because GetCurrentService() re-syncs off that setting — NOT
        // off this model's SelectedServiceId.
        internal async System.Threading.Tasks.Task CheckSelectedServiceInstalledAsync()
        {
            var generation = ++_installCheckGeneration;

            // Excluded services (e.g. Devmate) don't reflect CLI installation via their
            // IServiceValidation, so skip detection and treat them as installed.
            if (IsInstallDetectionExcluded(SelectedServiceId))
            {
                SelectedServiceInstallState = InstallCheckState.Installed;
                SelectedServiceInstallMessage = string.Empty;
                _onStateChanged?.Invoke();
                return;
            }

            SelectedServiceInstallState = InstallCheckState.Checking;
            SelectedServiceInstallMessage = string.Empty;
            _onStateChanged?.Invoke();

            try
            {
                AgentBridgeManager.EnsureServiceInitialized();
                var service = AgentBridgeManager.GetCurrentService();

                InstallCheckState resultState;
                string resultMessage = string.Empty;
                if (service is IServiceValidation validation)
                {
                    var result = await validation.ValidateConfigurationAsync();
                    // A newer check started (or the selection changed) while we awaited — discard this result.
                    if (generation != _installCheckGeneration)
                        return;
                    if (result.Status == ValidationStatus.Invalid)
                    {
                        resultState = InstallCheckState.NotInstalled;
                        resultMessage = !string.IsNullOrEmpty(result.Message)
                            ? result.Message
                            : string.Format(
                                AIToolsSetupStrings.Errors.AgentNotInstalledFormat,
                                GetSelectedServiceDisplayName());
                    }
                    else
                    {
                        resultState = InstallCheckState.Installed;
                    }
                }
                else
                {
                    resultState = InstallCheckState.Installed;
                }

                SelectedServiceInstallState = resultState;
                SelectedServiceInstallMessage = resultMessage;
            }
            catch (System.Exception)
            {
                if (generation != _installCheckGeneration)
                    return;
                SelectedServiceInstallState = InstallCheckState.Installed;
                SelectedServiceInstallMessage = string.Empty;
            }

            _onStateChanged?.Invoke();
        }

        internal void OnSelectedServiceChanged(string id)
        {
            // Cancel any in-flight verification for the previous service so its (possibly slower)
            // result can't land after the switch and overwrite the new service's status.
            CancelCurrentOperation();

            SelectedServiceId = id;

            // Abandon any in-flight install check for the previous agent (its stale result must not
            // land after the switch) and clear the install UI back to Unknown until the new agent's
            // check completes.
            _installCheckGeneration++;
            SelectedServiceInstallState = InstallCheckState.Unknown;
            SelectedServiceInstallMessage = string.Empty;

            // Reset the per-product connection states so each column returns to its default
            // (Run command + "Waiting for connection") for the newly selected assistant. A
            // silent re-verify (triggered by the window) re-detects status for the new service.
            Step2BridgeState = StepState.Incomplete;
            Step2BridgeError = null;
            Step2BridgeSuccessDetail = null;
            Step2RuntimeState = StepState.Incomplete;
            Step2RuntimeError = null;
            Step2RuntimeSuccessDetail = null;
        }

        internal async Task SetupProvidersAsync()
        {
            if (Step2RuntimeState == StepState.Processing
                || Step2BridgeState == StepState.Processing
                || !CanRunSetup)
            {
                _onStateChanged?.Invoke();
                return;
            }

            var sw = System.Diagnostics.Stopwatch.StartNew();

            var cts = new CancellationTokenSource();
            _cancellation = cts;
            var token = cts.Token;
            Step2RuntimeState = StepState.Processing;
            Step2RuntimeError = null;
            _onStateChanged?.Invoke();

            try
            {
                AgentBridgeSettings.SetSelectedService(SelectedServiceId, Origins.Menu);

                var ctx = new SetupContext
                {
                    ServiceId = SelectedServiceId,
                    ServiceExecutable = GetSelectedServiceExecutable()
                };

                foreach (var provider in AIToolsSetupRegistry.GetProvidersSorted())
                {
                    var actions = provider.GetSetupActions();
                    if (actions == null) continue;
                    foreach (var action in actions.OrderBy(a => a.Order))
                    {
                        await action.Execute(ctx, token);
                        if (token.IsCancellationRequested)
                        {
                            Step2RuntimeState = StepState.Incomplete;
                            return;
                        }
                    }
                }

                Step2RuntimeState = StepState.Complete;
                Step2RuntimeSuccessDetail = string.Format(
                    AIToolsSetupStrings.ConnectionStatus.RuntimeToolsRegisteredFormat,
                    GetSelectedServiceDisplayName());
            }
            catch (OperationCanceledException)
            {
                Step2RuntimeState = StepState.Incomplete;
            }
            catch (Exception e)
            {
                Step2RuntimeState = StepState.Error;
                Step2RuntimeError = string.Format(
                    AIToolsSetupStrings.Errors.SetupFailedFormat, e.Message);
            }
            finally
            {
                sw.Stop();
                AIToolsSetupTelemetry.SendEvent(
                    AIToolsSetupTelemetryConstants.FalcoEventName.AutoSetupCompleted,
                    evt =>
                    {
                        var success = Step2RuntimeState == StepState.Complete;
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ServiceId, SelectedServiceId);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.Success, success);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.DurationMs, sw.ElapsedMilliseconds);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.Subject,
                            AIToolsSetupTelemetryConstants.Subject.Providers);
                        if (!success)
                        {
                            var errorKind = Step2RuntimeState == StepState.Incomplete
                                ? AIToolsSetupTelemetryConstants.ErrorKind.Cancelled
                                : AIToolsSetupTelemetryConstants.ErrorKind.RegistrationFailed;
                            evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ErrorKind, errorKind);
                            if (Step2RuntimeError != null)
                                evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ErrorMessage, Step2RuntimeError);
                        }
                    },
                    isEssential: true);
                cts.Dispose();
                if (ReferenceEquals(_cancellation, cts))
                    _cancellation = null;
                _onStateChanged?.Invoke();
            }
        }

        // --- Step 3: Verify connection ---

        internal async Task VerifyConnectionAsync(bool silent = false)
        {
            if (!silent && Step3State == StepState.Processing)
                return;

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var trigger = silent
                ? AIToolsSetupTelemetryConstants.VerifyTrigger.Auto
                : AIToolsSetupTelemetryConstants.VerifyTrigger.Manual;

            var cts = new CancellationTokenSource();
            _cancellation = cts;
            var token = cts.Token;

            _verifyingCount++;
            if (!silent)
            {
                Step3State = StepState.Processing;
                Step3Error = null;
            }
            _onStateChanged?.Invoke();

            var verifyBridgeOk = false;
            var verifyAgenticOk = false;

            try
            {
                // The editor-tools MCP is internal-only; the AI Tools Setup flow verifies only the
                // Meta XR Operator provider connection.
                verifyBridgeOk = true;

                var exe = GetSelectedServiceExecutable();
                var allProvidersOk = true;
                foreach (var provider in AIToolsSetupRegistry.GetProviders())
                {
                    var ok = await provider.VerifyAsync(exe, token);
                    if (token.IsCancellationRequested) { if (!silent) Step3State = StepState.Incomplete; return; }
                    if (!ok) { allProvidersOk = false; break; }
                }
                verifyAgenticOk = allProvidersOk;

                // Bail before promoting if a switch/cancel landed after the last await — otherwise
                // a stale (e.g. previous-service) result could overwrite the current status.
                if (token.IsCancellationRequested)
                {
                    if (!silent) Step3State = StepState.Incomplete;
                    return;
                }

                // Drive the per-product column statuses independently so each card in the
                // Install & Setup step shows "Connection verified" as soon as its own MCP is
                // detected. Don't clobber an in-flight Run command (Processing).
                if (allProvidersOk && Step2RuntimeState != StepState.Processing)
                {
                    Step2RuntimeState = StepState.Complete;
                    Step2RuntimeSuccessDetail = string.Format(
                        AIToolsSetupStrings.ConnectionStatus.RuntimeToolsRegisteredFormat,
                        GetSelectedServiceDisplayName());
                }

                if (allProvidersOk)
                {
                    Step3State = StepState.Complete;
                    Step3SuccessDetail = AIToolsSetupStrings.ConnectionStatus.ConnectionVerified;

                    if (Step2State != StepState.Complete)
                    {
                        Step2State = StepState.Complete;
                        Step2SuccessDetail = string.Format(
                            AIToolsSetupStrings.ConnectionStatus.AssistantConnectedFormat,
                            GetSelectedServiceDisplayName());
                    }
                }
                else if (!silent)
                {
                    Step3State = StepState.Error;
                    Step3Error = string.Format(
                        AIToolsSetupStrings.Errors.NotRegisteredFormat,
                        GetSelectedServiceDisplayName());
                }
            }
            catch (OperationCanceledException)
            {
                if (!silent) Step3State = StepState.Incomplete;
            }
            catch (Exception e)
            {
                if (!silent)
                {
                    Step3State = StepState.Error;
                    Step3Error = string.Format(AIToolsSetupStrings.Errors.VerifyFailedFormat, e.Message);
                }
            }
            finally
            {
                sw.Stop();
                var verifySuccess = Step3State == StepState.Complete;
                AIToolsSetupTelemetry.SendEvent(
                    AIToolsSetupTelemetryConstants.FalcoEventName.VerifyCompleted,
                    evt =>
                    {
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.Success, verifySuccess);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.DurationMs, sw.ElapsedMilliseconds);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.Trigger, trigger);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.BridgeOk, verifyBridgeOk);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.AgenticOk, verifyAgenticOk);
                        if (!verifySuccess)
                        {
                            string errorKind;
                            if (Step3State == StepState.Incomplete)
                                errorKind = AIToolsSetupTelemetryConstants.ErrorKind.Cancelled;
                            else if (!verifyBridgeOk)
                                errorKind = AIToolsSetupTelemetryConstants.ErrorKind.BridgeUnreachable;
                            else if (!verifyAgenticOk)
                                errorKind = AIToolsSetupTelemetryConstants.ErrorKind.AgenticUnreachable;
                            else
                                errorKind = AIToolsSetupTelemetryConstants.ErrorKind.Unknown;
                            evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ErrorKind, errorKind);
                            if (Step3Error != null)
                                evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ErrorMessage, Step3Error);
                        }
                    },
                    isEssential: true);
                cts.Dispose();
                if (ReferenceEquals(_cancellation, cts))
                    _cancellation = null;
                _verifyingCount--;
                _onStateChanged?.Invoke();
            }
        }

        // --- Cancel ---

        internal void CancelCurrentOperation()
        {
            _cancellation?.Cancel();
        }

        // --- Platform utilities ---

        internal static bool IsWindows()
        {
            return Application.platform == RuntimePlatform.WindowsEditor;
        }

        internal static bool IsMac()
        {
            return Application.platform == RuntimePlatform.OSXEditor;
        }

        internal static string GetUserHome()
        {
            return IsWindows()
                ? Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
                : Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        }

        internal static string GetCurrentProjectRoot()
        {
            var assetsPath = Application.dataPath;
            return string.IsNullOrEmpty(assetsPath)
                ? null
                : Directory.GetParent(assetsPath)?.FullName;
        }

        internal static string ResolveExecutablePath(string fileName)
        {
            if (IsWindows() || fileName.Contains("/") || fileName.Contains(Path.DirectorySeparatorChar))
                return fileName;

            var shellPath = GetUserShellPath();
            if (shellPath == null)
                return fileName;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-l -c \"which {fileName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc == null) return fileName;
                var output = proc.StandardOutput.ReadToEnd().Trim();
                proc.WaitForExit(5000);
                if (proc.ExitCode == 0 && !string.IsNullOrEmpty(output) && File.Exists(output))
                    return output;
            }
            catch
            {
                // Fall through
            }

            return fileName;
        }

        private static readonly object _shellPathLock = new object();
        // volatile so the lock-free fast path below sees the fully-published value: the flag is
        // set LAST inside the lock, so observing it true guarantees _cachedUserShellPath is set.
        private static volatile bool _shellPathResolved;
        private static string _cachedUserShellPath;

        internal static string GetUserShellPath()
        {
            if (_shellPathResolved)
                return _cachedUserShellPath;

            // Called from background Task.Run (RunProcessAsync / ResolveExecutablePath). Serialize
            // the one-time resolve so a second caller can never observe the flag while
            // _cachedUserShellPath is still null (which would launch a child process with no
            // login-shell PATH on macOS/Linux).
            lock (_shellPathLock)
            {
                if (_shellPathResolved)
                    return _cachedUserShellPath;

                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "/bin/sh",
                        Arguments = "-l -c \"echo $PATH\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };
                    using var proc = Process.Start(psi);
                    if (proc != null)
                    {
                        var output = proc.StandardOutput.ReadToEnd().Trim();
                        proc.WaitForExit(5000);
                        if (proc.ExitCode == 0 && !string.IsNullOrEmpty(output))
                            _cachedUserShellPath = output;
                    }
                }
                catch
                {
                    // Fall through: leave _cachedUserShellPath null (best-effort PATH probe).
                }

                _shellPathResolved = true;
                return _cachedUserShellPath;
            }
        }

        // --- Process execution ---

        // Best-effort display name for the executable that failed to launch, resolved without an
        // instance (RunProcessAsync is static). Falls back to the file's base name.
        private static string GetServiceDisplayNameForExecutable(string fileName)
        {
            var baseName = Path.GetFileNameWithoutExtension(fileName);
            foreach (var s in AIServiceRegistry.GetAllServices())
            {
                if (string.Equals(s.ExecutableName, fileName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(s.ExecutableName, baseName, StringComparison.OrdinalIgnoreCase))
                    return s.DisplayName;
            }
            return baseName;
        }

        internal static Task<(int exitCode, string stdout, string stderr, bool timedOut)> RunProcessAsync(
            string fileName, string arguments, CancellationToken token = default, int timeoutMs = ProcessTimeoutMs,
            string workingDirectory = null)
        {
            return Task.Run(() =>
            {
                var resolvedFileName = ResolveExecutablePath(fileName);
                var startInfo = new ProcessStartInfo
                {
                    FileName = resolvedFileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                // Run from the Unity project root when provided so project-scoped MCP
                // registrations (Gemini's default scope; Claude's cwd-keyed "local" scope) are
                // written to and read from the same directory.
                if (!string.IsNullOrEmpty(workingDirectory))
                    startInfo.WorkingDirectory = workingDirectory;

                if (!IsWindows())
                {
                    var shellPath = GetUserShellPath();
                    if (shellPath != null)
                        startInfo.Environment["PATH"] = shellPath;
                }

                try
                {
                    using var process = Process.Start(startInfo);
                    if (process == null)
                        return (-1, "", "Failed to start process", false);

                    process.StandardInput.Close();

                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(token);
                    timeoutCts.CancelAfter(timeoutMs);

                    using var reg = timeoutCts.Token.Register(() =>
                    {
                        try { process.Kill(); } catch { /* already exited */ }
                    });

                    var stderrTask = process.StandardError.ReadToEndAsync();
                    var stdout = process.StandardOutput.ReadToEnd();
                    var stderr = stderrTask.GetAwaiter().GetResult();
                    process.WaitForExit(5000);

                    if (token.IsCancellationRequested)
                    {
                        token.ThrowIfCancellationRequested();
                    }

                    if (timeoutCts.IsCancellationRequested)
                    {
                        return (-1, stdout, $"Process timed out after {timeoutMs / 1000} seconds", true);
                    }

                    return (process.ExitCode, stdout, stderr, false);
                }
                catch (Win32Exception e) when (e.NativeErrorCode == 2)
                {
                    // ERROR_FILE_NOT_FOUND (Windows) == ENOENT (macOS/Linux) == 2. Covers Windows
                    // "The system cannot find the file specified" and macOS/Mono "No such file or directory"
                    // without matching localized message text. Rethrow so SetupProvidersAsync and
                    // VerifyConnectionAsync render it via their *Format paths.
                    throw new System.InvalidOperationException(
                        string.Format(
                            AIToolsSetupStrings.Errors.ClientNotFoundFormat,
                            GetServiceDisplayNameForExecutable(fileName)));
                }
            }, token);
        }
    }
}
