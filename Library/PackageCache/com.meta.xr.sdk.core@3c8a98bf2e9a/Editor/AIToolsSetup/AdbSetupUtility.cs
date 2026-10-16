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
using Meta.XR.AI.AgentBridge;

namespace Meta.XR.Editor
{
    /// <summary>
    /// Helpers for the ADB actions the AI Tools run against connected headsets:
    /// <list type="bullet">
    /// <item><b>adb reverse</b> for the AgentBridge Remote Server (device → this host): the in-headset
    /// assistant connects out to the Editor's inference server at <c>127.0.0.1</c>.</item>
    /// <item><b>adb forward</b> for the Meta XR Operator MCP server (this host → device): the desktop MCP
    /// proxy reaches the on-device OpenXR agentic-tool endpoint at <c>127.0.0.1</c>.</item>
    /// <item><b>setprop</b> / <b>am broadcast</b> for the device switches Meta XR Operator testing needs:
    /// experimental features, front-loaded screen-capture consent, and the proximity-sensor override.</item>
    /// </list>
    /// Every action is applied to all authorized devices.
    /// </summary>
    /// <remarks>
    /// These methods shell out to <c>adb</c> and are safe to call off the main thread. Resolve the Android SDK
    /// path (<c>OVRConfig.Instance.GetAndroidSDKPath()</c>) and the port lists (<see cref="ReversePorts"/> reads
    /// EditorPrefs) on the main thread first, then pass them in.
    /// </remarks>
    internal static class AdbSetupUtility
    {
        /// <summary>The Meta XR Operator MCP server port (mirrors the SSE endpoint at http://localhost:8720/sse).</summary>
        internal const int MetaXROperatorPort = 8720;

        /// <summary>
        /// Horizon OS v205+ gates part of the Meta XR Operator tool set behind experimental features.
        /// </summary>
        internal const string ExperimentalEnabledProp = "debug.oculus.experimentalEnabled";

        /// <summary>
        /// Makes the API layer request Android MediaProjection consent when the XR session starts, instead of
        /// on the first screen-capture tool call.
        /// </summary>
        internal const string RequestCapturePermissionProp = "debug.meta_xr_operator.request_capture_permission";

        /// <summary>
        /// Power-manager broadcast that makes the headset behave as if worn, so the OpenXR runtime keeps
        /// running while the device sits on a desk.
        /// </summary>
        internal const string ProximityCloseAction = "com.oculus.vrpowermanager.prox_close";

        /// <summary>
        /// Power-manager broadcast that hands proximity detection back to the physical sensor, undoing
        /// <see cref="ProximityCloseAction"/>.
        /// </summary>
        internal const string ProximityAutomationDisableAction =
            "com.oculus.vrpowermanager.automation_disable";

        /// <summary>Outcome of running one ADB action across the authorized devices.</summary>
        internal readonly struct DeviceResult
        {
            internal DeviceResult(bool adbAvailable, int authorizedDeviceCount, bool hasUnauthorizedDevice,
                int succeededCount, int failureCount, string failureMessage)
            {
                AdbAvailable = adbAvailable;
                AuthorizedDeviceCount = authorizedDeviceCount;
                HasUnauthorizedDevice = hasUnauthorizedDevice;
                SucceededCount = succeededCount;
                FailureCount = failureCount;
                FailureMessage = failureMessage;
            }

            internal bool AdbAvailable { get; }
            internal int AuthorizedDeviceCount { get; }
            internal bool HasUnauthorizedDevice { get; }
            internal int SucceededCount { get; }
            internal int FailureCount { get; }

            /// <summary>What the first failing device reported, or <c>null</c> when nothing failed.</summary>
            internal string FailureMessage { get; }

            internal bool Ok => AdbAvailable && AuthorizedDeviceCount > 0 && FailureCount == 0;
        }

        /// <summary>
        /// Ports that must be reachable <b>from</b> the device via <c>adb reverse</c> (device → host): the
        /// AgentBridge Remote Server. Reads <see cref="RemoteAgentSettings"/> so call it on the main thread.
        /// </summary>
        internal static IReadOnlyList<int> ReversePorts()
        {
            var ports = new List<int>();
            var agentPort = RemoteAgentSettings.Port.Value;
            if (agentPort > 0)
            {
                ports.Add(agentPort);
            }

            return ports;
        }

        /// <summary>
        /// Ports that must be reachable <b>on</b> the device via <c>adb forward</c> (host → device): the
        /// Meta XR Operator MCP server.
        /// </summary>
        internal static IReadOnlyList<int> ForwardPorts() => new[] { MetaXROperatorPort };

        /// <summary>
        /// Runs <c>adb reverse tcp:port tcp:port</c> for each port on every authorized device (device → host).
        /// Blocking; call off the main thread. Used for the AgentBridge Remote Server.
        /// </summary>
        internal static DeviceResult ConfigureReverse(string androidSdkPath, IReadOnlyList<int> ports)
            => Configure(androidSdkPath, ports, reverse: true);

        /// <summary>
        /// Runs <c>adb forward tcp:port tcp:port</c> for each port on every authorized device (host → device).
        /// Blocking; call off the main thread. Used for the Meta XR Operator MCP server.
        /// </summary>
        internal static DeviceResult ConfigureForward(string androidSdkPath, IReadOnlyList<int> ports)
            => Configure(androidSdkPath, ports, reverse: false);

        /// <summary>
        /// Runs <c>adb shell setprop debug.oculus.experimentalEnabled 1</c> on every authorized device, so
        /// Meta XR Operator exposes its full tool set on Horizon OS v205+. Blocking; call off the main thread.
        /// </summary>
        internal static DeviceResult EnableExperimentalFeatures(string androidSdkPath)
            => RunOnDevices(androidSdkPath,
                (adb, serial) => SetProp(adb, serial, ExperimentalEnabledProp, "1"));

        /// <summary>
        /// Runs <c>adb shell setprop debug.meta_xr_operator.request_capture_permission 1</c> on every authorized
        /// device, so the screen-capture consent dialog is shown at session start rather than on the first
        /// capture tool call. Blocking; call off the main thread.
        /// </summary>
        internal static DeviceResult RequestCapturePermissionOnStart(string androidSdkPath)
            => RunOnDevices(androidSdkPath,
                (adb, serial) => SetProp(adb, serial, RequestCapturePermissionProp, "1"));

        /// <summary>
        /// Broadcasts <c>com.oculus.vrpowermanager.prox_close</c> to every authorized device, keeping the
        /// headset awake while it is off the user's face. Blocking; call off the main thread.
        /// </summary>
        internal static DeviceResult DisableProximitySensor(string androidSdkPath)
            => RunOnDevices(androidSdkPath, (adb, serial) => Broadcast(adb, serial, ProximityCloseAction));

        /// <summary>
        /// Drops the <c>adb forward</c> tunnels for <paramref name="ports"/> on every authorized device,
        /// releasing the host ports. Blocking; call off the main thread.
        /// </summary>
        internal static DeviceResult RemoveForward(string androidSdkPath, IReadOnlyList<int> ports)
            => RunOnDevices(androidSdkPath, (adb, serial) =>
            {
                foreach (var port in ports)
                {
                    var portString = $"tcp:{port}";
                    var exitCode = adb.RunCommand(
                        new[] { "-s", serial, "forward", "--remove", portString },
                        null, out _, out var stderr);
                    if (exitCode != 0 && !IsListenerAlreadyRemoved(stderr, portString))
                    {
                        return $"adb forward --remove {portString} exited {exitCode}: {Summarize(stderr)}";
                    }
                }

                return null;
            });

        /// <summary>Turns experimental features back off on every authorized device.</summary>
        internal static DeviceResult DisableExperimentalFeatures(string androidSdkPath)
            => RunOnDevices(androidSdkPath,
                (adb, serial) => SetProp(adb, serial, ExperimentalEnabledProp, "0"));

        /// <summary>
        /// Stops front-loading the screen-capture consent dialog, so it returns to appearing on the first
        /// capture tool call.
        /// </summary>
        internal static DeviceResult ClearCapturePermissionOnStart(string androidSdkPath)
            => RunOnDevices(androidSdkPath,
                (adb, serial) => SetProp(adb, serial, RequestCapturePermissionProp, "0"));

        /// <summary>
        /// Hands proximity detection back to the physical sensor, so the headset sleeps normally again.
        /// </summary>
        internal static DeviceResult RestoreProximitySensor(string androidSdkPath)
            => RunOnDevices(androidSdkPath,
                (adb, serial) => Broadcast(adb, serial, ProximityAutomationDisableAction));

        private static DeviceResult Configure(string androidSdkPath, IReadOnlyList<int> ports, bool reverse)
            => RunOnDevices(androidSdkPath, (adb, serial) =>
            {
                foreach (var port in ports)
                {
                    var exitCode = reverse
                        ? adb.ReversePort(serial, port, null)
                        : ForwardPort(adb, serial, port);
                    if (exitCode != 0)
                    {
                        return $"adb {(reverse ? "reverse" : "forward")} tcp:{port} exited {exitCode}";
                    }
                }

                return null;
            });

        // Runs `perDevice` once per authorized device — it returns null on success, or a short reason for
        // the failure — and reports how many devices were reachable, how many calls failed, and what the
        // first failure said.
        private static DeviceResult RunOnDevices(string androidSdkPath, Func<OVRADBTool, string, string> perDevice)
        {
            var adb = new OVRADBTool(androidSdkPath);
            if (!adb.isReady)
            {
                return new DeviceResult(false, 0, false, 0, 0, null);
            }

            var authorized = new List<string>();
            var hasUnauthorized = false;
            foreach (var kvp in adb.GetDevicesWithStatus())
            {
                if (kvp.Value == "device")
                {
                    authorized.Add(kvp.Key);
                }
                else
                {
                    hasUnauthorized = true;
                }
            }

            var succeeded = 0;
            var failures = 0;
            string firstFailure = null;
            foreach (var serial in authorized)
            {
                var failure = perDevice(adb, serial);
                if (failure == null)
                {
                    succeeded++;
                }
                else
                {
                    failures++;
                    firstFailure ??= $"{serial}: {failure}";
                }
            }

            return new DeviceResult(true, authorized.Count, hasUnauthorized, succeeded, failures, firstFailure);
        }

        // Per-device `adb -s <serial> forward tcp:port tcp:port` (host → device). OVRADBTool.ForwardPort has no
        // device-targeted overload, so drive its generic runner directly to scope the tunnel to one serial.
        private static int ForwardPort(OVRADBTool adb, string serial, int port)
        {
            var portString = $"tcp:{port}";
            return adb.RunCommand(
                new[] { "-s", serial, "forward", portString, portString }, null, out _, out _);
        }

        // `adb shell setprop` reports a rejected write (read-only property, insufficient permissions) by
        // printing to stdout while still exiting 0, so an exit-code-only check reports success when nothing
        // changed on the device. Treat any stdout as failure — the same rule OVRExperimentalCheck applies to
        // this very property — then read the value back so the caller's "done" really means done.
        private static string SetProp(OVRADBTool adb, string serial, string property, string expectedValue)
        {
            var exitCode = adb.RunCommand(
                new[] { "-s", serial, "shell", "setprop", property, expectedValue },
                null, out var stdout, out var stderr);

            if (exitCode != 0)
            {
                return $"setprop {property} exited {exitCode}: {Summarize(stderr)}";
            }
            if (!string.IsNullOrWhiteSpace(stdout))
            {
                return $"setprop {property} rejected: {Summarize(stdout)}";
            }

            if (!adb.TryGetSystemProperty(serial, property, string.Empty, out var readBack)
                || readBack?.Trim() != expectedValue)
            {
                return $"{property} still reads '{Summarize(readBack)}' after setprop";
            }

            return null;
        }

        // `am broadcast` always prints to stdout ("Broadcast completed: result=0"), so unlike setprop the
        // output is not a failure signal — the result code inside it is.
        private static string Broadcast(OVRADBTool adb, string serial, string action)
        {
            var exitCode = adb.RunCommand(
                new[] { "-s", serial, "shell", "am", "broadcast", "-a", action },
                null, out var stdout, out var stderr);

            if (exitCode != 0)
            {
                return $"am broadcast {action} exited {exitCode}: {Summarize(stderr)}";
            }
            if (stdout?.Contains("result=0") != true)
            {
                return $"am broadcast {action} reported: {Summarize(stdout)}";
            }

            return null;
        }

        // `adb forward --remove` exits non-zero when the tunnel is already gone, which is exactly the end
        // state undo wants, so that one case is success. Match it narrowly: "device '<serial>' not found"
        // carries the same "not found" text but means the headset dropped and nothing was touched — and
        // unlike the setprop steps there is no read-back here that would catch the difference afterwards
        // (a departed device has no forwards to list either).
        private static bool IsListenerAlreadyRemoved(string stderr, string portString)
        {
            if (string.IsNullOrEmpty(stderr)
                || stderr.IndexOf("not found", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return false;
            }

            // Accept either wording adb has used for the empty-listener case; both name the tunnel,
            // whereas the device-missing message names only the serial.
            return stderr.IndexOf("listener", StringComparison.OrdinalIgnoreCase) >= 0
                   || stderr.IndexOf(portString, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Collapse adb output to a single short line: these strings land in a one-line status row.
        private static string Summarize(string output)
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                return "no output";
            }

            var collapsed = System.Text.RegularExpressions.Regex.Replace(output, @"\s+", " ").Trim();
            return collapsed.Length <= 120 ? collapsed : collapsed.Substring(0, 117) + "...";
        }
    }
}
