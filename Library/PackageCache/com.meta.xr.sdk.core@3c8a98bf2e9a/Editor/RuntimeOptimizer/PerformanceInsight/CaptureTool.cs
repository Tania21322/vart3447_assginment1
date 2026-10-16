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
using Unity.Profiling;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text;
using RO = Meta.XR.RuntimeOptimizer.Core;
using Meta.XR.RuntimeOptimizer.Editor;

namespace Meta.XR.RuntimeOptimizer.Editor.PerformanceInsight
{
    static class CaptureTool
    {
        /// <summary>Upper bound on any single ADB call issued by the Runtime Optimizer.</summary>
        /// <remarks>
        /// Most of these calls run on the editor's main thread, so a command that never returns
        /// freezes the whole editor with no recovery short of killing the process. The bound is set
        /// far above what any command here needs -- the longest is the capture itself, which runs for
        /// the trace duration (3s) -- so a healthy device never reaches it. Raising the capture
        /// duration past this value would require raising this too.
        /// </remarks>
        private const int kAdbCommandTimeoutMs = 30000;

        /// <summary>Tighter bound for the <c>ovrgpuprofiler</c> calls.</summary>
        /// <remarks>
        /// These complete in well under a second, but they talk to gpuprofserver, which is the part
        /// that has been observed to wedge. Since they sit directly behind button clicks, they get a
        /// bound tight enough that a wedged daemon costs a noticeable pause rather than half a minute.
        /// </remarks>
        private const int kGpuProfilerCommandTimeoutMs = 10000;

        /// <summary>Seconds of render stage trace run to start the stream before a capture.</summary>
        /// <remarks>
        /// The shortest duration the tool's <c>-t=N</c> form accepts. The capture does not consume
        /// this trace -- it only has to run and finish -- so there is nothing to gain from a longer
        /// one, and every extra second is added to the wait behind the Analyze button.
        /// </remarks>
        private const int kRenderStagePrimeSeconds = 1;

        /// <summary>How long to wait for init to report the GPU profiling service running.</summary>
        /// <remarks>
        /// init only has to fork an already-installed service, so this is generous. It bounds the
        /// cold path; the warm path returns on the first poll without ever sleeping.
        /// </remarks>
        private const int kGpuServiceReadyTimeoutMs = 5000;

        /// <summary>Gap between service-state polls, in milliseconds.</summary>
        private const int kGpuServiceReadyPollMs = 250;

        /// <summary>PID of an app process that started before detailed profiling was enabled.</summary>
        /// <remarks>
        /// Detailed profiling is injected into a process at Vulkan/GL init, so it "only applies to
        /// applications started after this mode is started". A process that was up before the enable
        /// therefore renders un-instrumented for its whole lifetime: not just the capture that armed
        /// the device, but every later one too, even though the device reads as armed by then.
        /// Remembering the PID is what keeps the diagnosis attached to the process it describes; it
        /// stops applying the moment the app is relaunched under a new PID, which
        /// <see cref="LaunchApp"/> makes explicit by clearing this rather than relying on the new
        /// process happening to get a different number.
        ///
        /// Measured on a Phoenix (HzOS 10000) with the capture config below, counting
        /// `surface#...MSAA` slices on the `Gpu (<pkg>)` track: app launched after the enable, 355
        /// slices; app already running when the enable ran, 0. `ovrgpuprofiler -e` reported success
        /// both times.
        /// </remarks>
        static private int unprofiledAppPid = -1;

        /// <summary>Bound for the <c>adb pull</c> transfers.</summary>
        /// <remarks>
        /// Unlike the other commands these scale with file size and link speed -- a trace can be tens
        /// of megabytes -- so they get a bound generous enough that a slow but working transfer is
        /// never killed, while still being finite.
        /// </remarks>
        private const int kFileTransferTimeoutMs = 300000;

        static private OVRADBTool adbTool = CreateAdbTool();
        static private Thread captureThread = null;
        static private List<string> connectedDevices = new List<string>();
        private const string outFile = "/data/misc/perfetto-traces/trace";
        static private System.Object lockObj = new System.Object();
        static private int metricProcessWorkInQueue = 0;
        static public int lastKnownPID = -1;

        static private string LowOverheadCaptureConfig = @"
buffers {
  size_kb: 4096
  fill_policy: DISCARD
}
}
buffers {
  size_kb: 43008
  fill_policy: DISCARD
}
buffers {
  size_kb: 262144
  fill_policy: DISCARD
}
data_sources {
  config {
    name: ""linux.process_stats""
    target_buffer: 0
    process_stats_config {
      scan_all_processes_on_start: false
    }
  }
}
data_sources {
  config {
    name: ""linux.ftrace""
    target_buffer: 1
    ftrace_config {
      ftrace_events: ""sched/sched_switch""
      ftrace_events: ""power/suspend_resume""
      ftrace_events: ""sched/sched_wakeup""
      ftrace_events: ""sched/sched_wakeup_new""
      ftrace_events: ""sched/sched_waking""
      ftrace_events: ""sched/sched_process_exit""
      ftrace_events: ""sched/sched_process_free""
      ftrace_events: ""task/task_newtask""
      ftrace_events: ""task/task_rename""
      ftrace_events: ""ftrace/print""
      buffer_size_kb: 8192
      drain_period_ms: 10
    }
  }
  producer_name_filter: ""{bundle_name}""
}
data_sources {
  config {
    name: ""track_event""
    target_buffer: 2
  }
}
trigger_config {
    trigger_mode: STOP_TRACING
    triggers {
        name: ""perf_optimizer_auto_stop""
        stop_delay_ms: 0
    }
    trigger_timeout_ms: 100000
  }
";

        // Sized against what the analysis consumes rather than what the GPU can produce.
        // MetricAPI bins frames with windows(2).take(200), so the capture only needs to clear
        // ~200 colour passes: 3000ms yields 199 and 64MB holds them, where the previous 256MB
        // collected ~500 and spent the surplus purely on capture and transfer time.
        //
        // Do not shorten the duration to save time. Measured on a Quest 3S, two runs each:
        //   1000ms -> 61 passes, GPU 15.03ms, 31.5s     2000ms -> 127 passes, GPU 14.84ms, 36.5s
        //   3000ms -> 199 passes, GPU 14.17ms, 36.4s
        // GPU reads systematically high on short windows -- runs agree with each other to 0.02%
        // while differing 5.7% across durations, so it is bias rather than noise -- and CPU
        // run-to-run spread is 7x worse at 1000ms. 3000ms costs the same wall time as 2000ms
        // because fixed perfetto setup dominates, so there is nothing to buy by going shorter.
        static private string captureConfig = @"
buffers {
  size_kb: 4096
  fill_policy: DISCARD
}
buffers {
  size_kb: 43008
  fill_policy: DISCARD
}
buffers {
  size_kb: 65536
  fill_policy: DISCARD
}
data_sources {
  config {
    name: ""linux.process_stats""
    target_buffer: 0
    process_stats_config {
      scan_all_processes_on_start: false
    }
  }
}
data_sources {
  config {
    name: ""linux.ftrace""
    target_buffer: 1
    ftrace_config {
      ftrace_events: ""sched/sched_switch""
      ftrace_events: ""power/suspend_resume""
      ftrace_events: ""task/task_newtask""
      ftrace_events: ""task/task_rename""
      ftrace_events: ""ftrace/print""
      atrace_apps: ""{bundle_name}""
      buffer_size_kb: 8192
      drain_period_ms: 10
      compact_sched {
       enabled: true
      }
    }
  }
}
data_sources {
  config {
    name: ""track_event""
    target_buffer: 2
    track_event_config {
      disabled_categories: ""*""
      disabled_categories: ""xr_runtime_server""
      disabled_categories: ""xr_runtime_client""
      disabled_categories: ""gpu_profiling_service""
      disabled_categories: ""gpu_profiling_service_low_frequency""
      disabled_categories: ""gpu_renderstage""
      disabled_categories: ""gpu_surface_workload""
      enabled_categories: ""gpu_profiling_service""
      enabled_categories: ""gpu_renderstage""
      enabled_categories: ""gpu_surface_workload""
      enabled_categories: ""vulkan_os_layer""
    }
  }
}
duration_ms: 3000
";

        static public string RemoveLastFolderFromPath(string path)
        {
            var lastSlashIndex = path.LastIndexOf('/');
            if (lastSlashIndex != -1)
            {
                return path.Substring(0, lastSlashIndex);
            }
            return path;
        }

        static private string CreateFolderIn(string path, string folderName)
        {
#if UNITY_EDITOR_OSX
        var wholePath = path + "/" + folderName;
#else
            var wholePath = path + "\\" + folderName;
#endif
            if (!Directory.Exists(wholePath))
            {
                Directory.CreateDirectory(wholePath);
            }
            return wholePath;
        }

        static public uint EpochTime()
        {
            DateTime epochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            uint currentEpochTime = (uint)(DateTime.UtcNow - epochStart).TotalSeconds;

            return currentEpochTime;
        }

        static public string GetAssetOutputFolderName()
        {
            return "Assets/#Meta";
        }

        static public string GetOutputDirectory()
        {
            string projectPath = RemoveLastFolderFromPath(Application.dataPath);
            string outdir = Path.Combine(projectPath, GetAssetOutputFolderName());
            if (!Directory.Exists(outdir))
            {
                Directory.CreateDirectory(outdir);
            }
            return outdir;
        }

        static public string GetMetricJsonOutputPath(string traceFilePath)
        {
            string originalFilePath = traceFilePath;
            // New file extension
            string newExtension = ".json";
            // Get the file name without the extension
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalFilePath);
            // Create the new file path with the new extension
            return Path.Combine(Path.GetDirectoryName(originalFilePath), $"{fileNameWithoutExtension}{newExtension}");
        }

        static private void StartServerCallback()
        {

        }

        static private void PerfettoCapture()
        {

        }

        static public bool isDetailedGPUServiceEnabled()
        {
            string outputString;
            string errorString;
            const string commandCapture = "shell getprop debug.egl.profiler";
            adbTool.RunCommand(new string[] { "-s", connectedDevices[0], commandCapture }, PerfettoCapture, out outputString, out errorString);
            RO.Util.DebugLog("adb egl.profiler: " + outputString + " error: " + errorString);

            if (outputString.Contains("1"))
            {
                return true;
            }
            return false;
        }

        /// <summary>True while the background capture thread is alive and issuing adb commands.</summary>
        /// <remarks>
        /// <c>OVRADBTool</c> keeps its stdout and stderr StringBuilders in instance fields that every
        /// <c>RunCommand</c> reassigns and then nulls out, so two threads sharing one tool overwrite
        /// each other's output. Main-thread pollers must consult this before touching adb, or they
        /// corrupt whatever the capture thread is reading.
        /// </remarks>
        static public bool IsCaptureThreadRunning()
        {
            return captureThread != null && captureThread.IsAlive;
        }

        static public void ValidateProcess(string bundleName)
        {
            // Return early if IssuePerfettoCapture is currently running
            if (IsCaptureThreadRunning())
            {
                return;
            }
            lastKnownPID = GetProcessPID(bundleName);
        }


        static public bool ValidateAdb()
        {
            // Return early if IssuePerfettoCapture is currently running
            if (IsCaptureThreadRunning())
            {
                return true;
            }

            if (!IsADBReady())
            {
                return false;
            }

            connectedDevices = adbTool.GetDevices();

            if (connectedDevices.Count > 0)
            {
                return true;
            }
            else
            {
                connectedDevices.Clear();
            }

            RO.Util.DebugLog("No device connected");
            return false;
        }

        static public bool ForwardPort(int port)
        {
            int exitCode = adbTool.ForwardPort(port, null);
            return exitCode == 0;
        }

        static public bool ReleasePort(int port)
        {
            int exitCode = adbTool.ReleasePort(port, null);
            return exitCode == 0;
        }

        static public int GetProcessPID(string bundleName)
        {
            string outputString = GetProcessInfo(bundleName);

            RO.Util.DebugLog($"[GetProcessPID] Searching for PID of '{bundleName}'");

            if (string.IsNullOrEmpty(outputString))
            {
                RO.Util.DebugLog($"[GetProcessPID] No output from GetProcessInfo - app not running");
                return -1;
            }

            RO.Util.DebugLog($"[GetProcessPID] Raw output: '{outputString}'");

            // Parse the output to extract PID
            string[] parts = outputString.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            RO.Util.DebugLog($"[GetProcessPID] Split into {parts.Length} parts");
            for (int i = 0; i < parts.Length && i < 5; i++)
            {
                RO.Util.DebugLog($"[GetProcessPID] Part[{i}]: '{parts[i]}'");
            }

            if (parts.Length >= 2)
            {
                int pid;
                if (int.TryParse(parts[1], out pid))
                {
                    RO.Util.DebugLog($"[GetProcessPID] Successfully extracted PID: {pid}");
                    return pid;
                }
                else
                {
                    RO.Util.DebugLog($"[GetProcessPID] Failed to parse PID from parts[1]: '{parts[1]}'");
                }
            }
            else
            {
                RO.Util.DebugLog($"[GetProcessPID] Insufficient parts in output (need at least 2, got {parts.Length})");
            }

            // Try to parse just the first part as PID (in case pidof returns just the PID)
            if (parts.Length >= 1)
            {
                int pid;
                if (int.TryParse(parts[0], out pid))
                {
                    RO.Util.DebugLog($"[GetProcessPID] Successfully extracted PID from first part: {pid}");
                    return pid;
                }
            }

            RO.Util.DebugLog($"[GetProcessPID] Failed to extract PID from output");
            return -1;
        }

        static public string GetProcessInfo(string bundleName)
        {
            string errorString = string.Empty;

            if (!IsADBReady() || connectedDevices.Count == 0)
            {
                RO.Util.DebugLog($"[GetProcessInfo] ADB not ready or no devices connected");
                return null;
            }

            string outputString;
            // Use pidof command which is more reliable for finding process by package name
            // Falls back to ps | grep if pidof is not available
            string command = $"shell \"pidof {bundleName} 2>/dev/null || ps -A | grep {bundleName}\"";

            RO.Util.DebugLog($"[GetProcessInfo] Executing command: {command}");

            adbTool.RunCommand(new string[] { "-s", connectedDevices[0], command }, PerfettoCapture, out outputString, out errorString);

            // Debug logging to help diagnose detection issues
            RO.Util.DebugLog($"[GetProcessInfo] Bundle: '{bundleName}', Output: '{outputString}', Error: '{errorString}'");

            return !string.IsNullOrEmpty(outputString) && string.IsNullOrEmpty(errorString) ? outputString : null;
        }

        static public int ConnectedDeviceCount()
        {
            return connectedDevices.Count;
        }

        static public string[] ConnectedDevices()
        {
            return connectedDevices.ToArray();
        }

        /// <summary>Resolves the Android SDK root without depending on the Android module's editor assembly.</summary>
        /// <remarks>
        /// <c>OVRConfig</c> resolves the path from EditorPrefs, the embedded playback engine and
        /// <c>ANDROID_SDK_ROOT</c>, using only <c>UnityEditor</c> APIs. That keeps this file compiling
        /// whether or not the Android module is installed, and working regardless of the active build
        /// target. The try/catch matters because this runs from a static field initializer, where an
        /// escaping exception would leave the whole type uninitialized.
        /// </remarks>
        static private string GetAndroidSdkRootPath()
        {
            try
            {
                return OVRConfig.GetAndroidSDKPathLocation(false) ?? string.Empty;
            }
            catch (Exception e)
            {
                RO.Util.DebugLog("Failed to resolve the Android SDK root: " + e.Message);
                return string.Empty;
            }
        }

        /// <summary>Reports whether an ADB executable could be located, without starting the server or logging.</summary>
        /// <remarks>
        /// Every device operation in the Runtime Optimizer goes through ADB, so when this returns false
        /// the window cannot do anything useful.
        /// </remarks>
        static public bool IsAdbAvailable()
        {
            return OVRADBTool.IsAndroidSdkRootValid(GetAndroidSdkRootPath());
        }

        static private OVRADBTool CreateAdbTool()
        {
            return new OVRADBTool(GetAndroidSdkRootPath())
            {
                defaultTimeoutMs = kAdbCommandTimeoutMs
            };
        }

        static public bool IsADBReady()
        {
            if (adbTool == null)
            {
                adbTool = CreateAdbTool();
            }
            int code = adbTool.StartServer(StartServerCallback);
            if (code != 0)
            {
                RO.Util.DebugLog("adb StartServer failed: " + code.ToString());
                RO.Util.DebugLog("Please check your Android sdk path in Preferences/External Tools");
                adbTool = null;
                return false;
            }
            return true;
        }

        static public bool KillApp(string packageName)
        {
            string outputString;
            string errorString;
            string command = "shell am force-stop " + packageName;

            int result = adbTool.RunCommand(new string[] { "-s", connectedDevices[0], command }, PerfettoCapture, out outputString, out errorString);

            if (result != 0 || !string.IsNullOrEmpty(errorString))
            {
                UnityEngine.Debug.LogError($"Failed to kill app: {packageName}. Error: {errorString}");
                return false;
            }

            RO.Util.DebugLog($"Successfully killed app: {packageName}");
            return true;
        }

        static public bool LaunchApp(string packageActivityPath)
        {
            if (!IsADBReady() || connectedDevices.Count == 0)
            {
                UnityEngine.Debug.LogError("ADB not ready or no devices connected");
                return false;
            }

            string outputString;
            string errorString;
            string command = "shell am start -n " + packageActivityPath;

            int result = adbTool.RunCommand(new string[] { "-s", connectedDevices[0], command }, PerfettoCapture, out outputString, out errorString);

            if (result != 0 || !string.IsNullOrEmpty(errorString))
            {
                UnityEngine.Debug.LogError($"Failed to launch app: {packageActivityPath}. Error: {errorString}");
                return false;
            }

            RO.Util.DebugLog($"Successfully launched app: {packageActivityPath}");

            // A relaunch retires the un-instrumented process that unprofiledAppPid describes: Init
            // arms and waits for the service before calling this, so the process starting here does
            // pick up detailed profiling. Clearing is also what bounds the record's lifetime -- it
            // is keyed on PID alone, and Android recycles PIDs, so a record left set for the whole
            // editor session would eventually match an unrelated, properly instrumented process and
            // warn about it. A relaunch driven from outside Runtime Optimizer still leaves the
            // record standing; the warning is advisory, so that is worth less than the complexity
            // of tracking process identity beyond the PID.
            unprofiledAppPid = -1;
            return true;
        }


        static public string GetExecutablePath(string bundleName)
        {
            if (string.IsNullOrEmpty(bundleName) || connectedDevices.Count == 0)
            {
                return "";
            }

            string outputString;
            string errorString;
            string command = $"shell cmd package resolve-activity --brief \"{bundleName}\"";

            adbTool.RunCommand(new string[] { "-s", connectedDevices[0], command }, PerfettoCapture, out outputString, out errorString);

            if (!string.IsNullOrEmpty(outputString))
            {
                string[] lines = outputString.Trim().Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    string activityPath = lines[1].Trim();
                    return activityPath;
                }
            }

            return "";
        }

        /// <param name="waitForGpuService">
        /// Block until the GPU profiling service is up before returning. Callers that are about to
        /// launch the app must pass true: detailed profiling reaches a process only at Vulkan/GL
        /// init, so an app launched while the service is still coming up renders un-instrumented
        /// for its whole lifetime and every capture taken against it reports `GPU: 0.00ms`. This is
        /// the first launch of a session, where the service is cold; later launches find it warm,
        /// which is why the symptom looks intermittent. Editor GUI handlers that are not about to
        /// launch leave it false, so a wedged device costs them nothing.
        /// </param>
        static public void Init(string packageName = "", bool shimLayer = false, bool waitForGpuService = false)
        {
            if (!IsADBReady())
            {
                return;
            }

            // connectedDevices is a cache that only the capture and the Update()-driven ValidateAdb
            // refresh. Arming reads it, and reading it stale is how the arm gets skipped for a whole
            // session on an editor that has not enumerated a device yet -- which leaves the app
            // launched below un-instrumented and every capture at GPU: 0.00ms.
            connectedDevices = adbTool.GetDevices();

            string outputString;
            string errorString;
            if (connectedDevices.Count > 0)
            {
                if (ArmGpuProfiler(packageName) && waitForGpuService)
                {
                    WaitForGpuProfilingService();
                }

                if (shimLayer == true)
                {
                    adbTool.RunCommand(new string[] { "-s", connectedDevices[0], "shell setprop debug.oculus.forceVkShimLayer 1" }, PerfettoCapture, out outputString, out errorString);
                    RO.Util.DebugLog("forceVkShimLayer: " + outputString);
                    adbTool.RunCommand(new string[] { "-s", connectedDevices[0], "shell setprop debug.oculus.vkshim.forceVkLoadingFunctionShimming 1" }, PerfettoCapture, out outputString, out errorString);
                    RO.Util.DebugLog("forceVkLoadingFunctionShimming: " + outputString);
                }
                else
                {
                    adbTool.RunCommand(new string[] { "-s", connectedDevices[0], "shell setprop debug.oculus.forceVkShimLayer 0" }, PerfettoCapture, out outputString, out errorString);
                    // RO.Util.DebugLog("forceVkShimLayer: " + outputString);
                }
            }
            else
            {
                RO.Util.DebugLog("adb no adb device found");
            }
        }

        static public void StopPerfettoCapture()
        {
            try
            {
                List<string> devices = adbTool.GetDevices();
                if (devices.Count > 0)
                {
                    int commandResult = adbTool.RunCommand(new string[] { "-s", devices[0], "shell trigger_perfetto perf_optimizer_auto_stop" }, PerfettoCapture, out string outputString, out string errorString);
                    RO.Util.DebugLog("StopPerfettoCapture: " + outputString + " error: " + errorString.ToString());
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning("Exception caught while stopping perfetto capture. Details  \r\n" + ex.Message.ToString() + "\r\n" + ex.StackTrace);
            }
        }

        /// <returns>False when no device could be resolved, so the caller skips pull and screenshot.</returns>
        static public bool IssuePerfettoCapture(bool screenshot, string packageName, bool lowOverhead = false)
        {
            RO.Util.DebugLog("Issue perfetto capture");

            if (adbTool == null)
            {
                UnityEngine.Debug.LogError("abd tool is not ready, did you call Init?");
                return false;
            }

            List<string> devices = adbTool.GetDevices();

            if (devices.Count == 0)
            {
                // Deliberately leaves connectedDevices alone. GetDevices cannot tell "no headset
                // attached" from "that adb call failed" -- it discards the exit code and returns an
                // empty list either way -- so publishing this result turns one bad round trip into
                // a phantom headset_disconnection that kills the capture from the editor's Update.
                UnityEngine.Debug.LogError(
                    "adb no adb device found; leaving the known device list untouched");
                return false;
            }

            connectedDevices = devices;
            PerfettoCaptureCommand(lowOverhead, packageName);
            return true;
        }

        private static void ClearPerfettoCapture()
        {
            string outputString;
            string errorString;
            // Directory exists, remove the file
            const string commandRemove = "shell rm " + outFile;
            adbTool.RunCommand(new string[] { "-s", connectedDevices[0], commandRemove }, PerfettoCapture, out outputString, out errorString);
            RO.Util.DebugLog("adb Perfetto device: " + outputString + " error: " + errorString.ToString());
            Thread.Sleep(2000);
        }

        /// <summary>Builds the adb shell command that arms GPU profiling for a package.</summary>
        /// <remarks>Split out from <see cref="ArmGpuProfiler"/> so the exact command is testable.</remarks>
        private static string BuildGpuProfilerArmCommand(string packageName)
        {
            // The optional target scopes detailed profiling to the app being analysed.
            string armTarget = string.IsNullOrEmpty(packageName) ? "" : $" {packageName}";
            return $"shell ovrgpuprofiler -e{armTarget}";
        }

        /// <summary>Builds the adb shell command that asks whether detailed profiling is on.</summary>
        /// <remarks>Split out from <see cref="ArmGpuProfiler"/> so the exact command is testable.</remarks>
        internal static string BuildGpuProfilerStatusCommand()
        {
            return "shell ovrgpuprofiler -i";
        }

        /// <summary>True when <c>ovrgpuprofiler -i</c> reported detailed profiling already off.</summary>
        /// <remarks>
        /// Measured output is <c>"Detailed GPU profiling is disabled"</c> or
        /// <c>"Detailed GPU profiling is enabled."</c> -- note the disabled form carries no full
        /// stop, so neither a trailing-period nor an exact-string match is safe. Matching on
        /// <c>"is disabled"</c> keeps the two apart without depending on that difference.
        ///
        /// Deliberately an affirmative test rather than the negation of "enabled". A query that
        /// timed out, lost the device, or ran against a build with no <c>ovrgpuprofiler</c> returns
        /// nothing at all, and "said nothing" is not "said disabled": reading it as disabled records
        /// a running app as un-instrumented and tells the user to relaunch for no reason.
        ///
        /// This is only used to decide whether an app that is already running could have picked up
        /// instrumentation; it is not a gate on the capture, because a capture with no GPU data is
        /// still worth taking for its CPU metrics.
        /// </remarks>
        internal static bool DetailedProfilingIsDisabled(string statusOutput)
        {
            return !string.IsNullOrEmpty(statusOutput) && statusOutput.Contains("is disabled");
        }

        /// <summary>
        /// Put the GPU driver in detailed profiling mode, starting the profiling service if it is
        /// not already up. Must run before any capture that reads render stages, and before the app
        /// being analysed is launched.
        /// </summary>
        /// <remarks>
        /// gpuprofserver produces every GPU track in a capture. `gpuprofilingservice.rc` declares
        /// its init service `disabled`, and starts it at boot only when
        /// `persist.vr.gpuprofilingservice` is set, so on a normal headset it is simply not running
        /// after a reboot. Perfetto never starts it -- naming `gpu.renderstages.oculus` in the
        /// capture config while the service is down still records zero GPU slices -- so a capture
        /// taken in that state reaches the user as `GPU: 0.00ms`.
        ///
        /// Enabling is necessary but not sufficient, and the ordering is the whole game: detailed
        /// profiling is injected into a process at Vulkan/GL init, so `-e` "only applies to
        /// applications started after this mode is started". <see cref="Init"/> issues this before
        /// launching the app, which is the call that matters. Arming next to the capture, as this
        /// also does, cannot rescue a process that is already running -- it only leaves the device
        /// ready for the next launch.
        ///
        /// The status read before the enable is what lets the capture path tell the user which of
        /// those two situations it is in, rather than reporting an unexplained missing metric.
        /// </remarks>
        private static bool ArmGpuProfiler(string packageName)
        {
            // adbTool is cleared by IsADBReady() when the server fails to start, and
            // connectedDevices is empty with no device attached. Arming is best-effort: skipping it
            // leaves the capture to behave exactly as it would have anyway.
            if (adbTool == null || !adbTool.isReady || connectedDevices.Count == 0)
            {
                return false;
            }

            string outputString;
            string errorString;

            // Only an answer that arrived and said "disabled" counts. Measured on a Quest 3 (HzOS
            // 52648620000000521): both real states exit 0 -- "Detailed GPU profiling is disabled"
            // and "Detailed GPU profiling is enabled." -- while a missing binary exits 127 and
            // prints nothing. So the exit code screens out transport and tooling failures, and the
            // predicate screens out an answer that parses as neither; treating either as "disabled"
            // would tell the user to relaunch an app that is in fact instrumented.
            int statusCode = adbTool.RunCommand(
                new string[] { "-s", connectedDevices[0], BuildGpuProfilerStatusCommand() },
                PerfettoCapture, out outputString, out errorString, null, kGpuProfilerCommandTimeoutMs);
            if (statusCode == 0 && DetailedProfilingIsDisabled(outputString) && lastKnownPID != -1)
            {
                unprofiledAppPid = lastKnownPID;
            }

            int code = adbTool.RunCommand(
                new string[] { "-s", connectedDevices[0], BuildGpuProfilerArmCommand(packageName) },
                PerfettoCapture, out outputString, out errorString, null, kGpuProfilerCommandTimeoutMs);

            RO.Util.DebugLog("adb ovrgpuprofiler : " + outputString);
            RO.Util.DebugLog("adb ovrgpuprofiler : " + errorString);
            if (code != 0)
            {
                RO.Util.DebugLogError("adb ovrgpuprofiler failed: " + code.ToString());
                return false;
            }

            return true;
        }

        /// <summary>True only when getprop reported exactly "running", not "restarting".</summary>
        /// <remarks>Split out from the poll loop so the state matching is testable.</remarks>
        private static bool ServiceStateIsRunning(string getpropOutput)
        {
            if (string.IsNullOrEmpty(getpropOutput))
            {
                return false;
            }

            foreach (var line in getpropOutput.Split('\n'))
            {
                if (line.Trim() == "running")
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Block until init reports the GPU profiling service running, so a capture never starts
        /// against a service that has not finished coming up.
        /// </summary>
        /// <remarks>
        /// Arming and the capture are two separate adb round trips, so nothing otherwise orders the
        /// service being up against the capture starting. Measured on a Quest 3S the service was
        /// always already running by the time `ovrgpuprofiler -e` returned, making this loop a
        /// single confirming getprop in the normal case, but a capture that loses this race yields
        /// a silent `GPU: 0.00ms` rather than an error, which is far worse than waiting.
        ///
        /// Only the capture path waits. <see cref="Init"/> arms from an editor GUI handler, where
        /// blocking the main thread for the readiness budget would wedge the editor.
        /// </remarks>
        private static void WaitForGpuProfilingService()
        {
            string outputString = null;
            string errorString;
            var deadline = DateTime.UtcNow.AddMilliseconds(kGpuServiceReadyTimeoutMs);

            while (true)
            {
                // The device can go away mid-wait; without this the poll would NRE or index past
                // the end of connectedDevices rather than reporting the missing GPU data.
                if (adbTool == null || !adbTool.isReady || connectedDevices.Count == 0)
                {
                    return;
                }

                // Bound the call by what is left of the readiness budget, otherwise one hung adb
                // round trip runs to kGpuProfilerCommandTimeoutMs and the 5s bound means nothing.
                // Checked before issuing, so the budget is never overrun by a whole extra poll.
                int remainingMs = (int)(deadline - DateTime.UtcNow).TotalMilliseconds;
                if (remainingMs <= 0)
                {
                    ReportGpuServiceNotReady(outputString);
                    return;
                }

                int code = adbTool.RunCommand(
                    new string[] { "-s", connectedDevices[0], "shell getprop init.svc.gpuprofserver" },
                    PerfettoCapture, out outputString, out errorString, null,
                    Math.Min(kGpuProfilerCommandTimeoutMs, remainingMs));

                if (code != 0)
                {
                    RO.Util.DebugLogError(
                        "Could not read init.svc.gpuprofserver (exit " + code + "); this capture " +
                        "may have no GPU data. " + errorString);
                    return;
                }

                // getprop exits 0 with nothing to say when the property does not exist, which is
                // what a build that does not run this service under init looks like. Waiting the
                // whole budget on every capture would not make it appear.
                if (string.IsNullOrWhiteSpace(outputString))
                {
                    return;
                }

                // init reports "restarting" while the service is coming back up, which contains
                // "running" -- a substring test would call that ready and race the capture.
                if (ServiceStateIsRunning(outputString))
                {
                    return;
                }

                if (DateTime.UtcNow >= deadline)
                {
                    ReportGpuServiceNotReady(outputString);
                    return;
                }

                Thread.Sleep(kGpuServiceReadyPollMs);
            }
        }

        /// <summary>Reads the gpuprofserver pid, or empty when it is not running.</summary>
        internal static string GpuServicePid()
        {
            if (adbTool == null || !adbTool.isReady || connectedDevices.Count == 0)
            {
                return string.Empty;
            }

            string outputString;
            string errorString;
            int code = adbTool.RunCommand(
                new string[] { "-s", connectedDevices[0], "shell pidof gpuprofserver" },
                PerfettoCapture, out outputString, out errorString, null, kGpuProfilerCommandTimeoutMs);

            return code == 0 && outputString != null ? outputString.Trim() : string.Empty;
        }

        /// <summary>True when a capture taken now cannot contain render stages for the app.</summary>
        /// <remarks>
        /// Split out from <see cref="ReportIfAppPredatesGpuProfiler"/> so the rule is testable. It
        /// holds for as long as the un-instrumented process lives, not only for the capture that
        /// armed the device: the device reads as armed from the second capture onward while that
        /// process stays exactly as un-instrumented as it was.
        /// </remarks>
        internal static bool CaptureCannotHaveGpuData(int unprofiledPid, int appPid)
        {
            return appPid != -1 && appPid == unprofiledPid;
        }

        /// <summary>
        /// Say plainly that this capture will have no GPU data, rather than letting it surface as a
        /// missing metric with no cause attached.
        /// </summary>
        private static void ReportIfAppPredatesGpuProfiler()
        {
            if (!CaptureCannotHaveGpuData(unprofiledAppPid, lastKnownPID))
            {
                return;
            }

            RO.Util.DebugLogError(
                "GPU profiling was not enabled on this device until now, and the app was already " +
                "running. Detailed profiling only reaches an app that starts after it is enabled, " +
                "so this capture will have no GPU or MSAA data. Relaunch the app to collect them.");
        }

        /// <summary>
        /// Runs a short render stage trace to completion so gpuprofserver starts streaming render
        /// stages into perfetto. Must finish before the capture opens, and must never overlap it.
        /// </summary>
        /// <remarks>
        /// Arming with <c>ovrgpuprofiler -e</c> only puts the GPU driver into detailed mode; on its
        /// own it does not make the service emit. <c>-t</c> is what starts the stream. While a
        /// <c>-t</c> is running it consumes that stream exclusively and perfetto still records
        /// nothing, so the ordering is the whole point: warm up, let it finish, then capture.
        ///
        /// Needed on Panther (Quest 3S, v207) even with the arm correctly ordered before app start,
        /// which <see cref="ReportIfAppPredatesGpuProfiler"/> verifies and which alone is enough on
        /// Phoenix. Measured on Panther with the capture config below, counting rows in
        /// trace_processor's gpu_slice table: arm before launch and no warm up, 0; the same capture
        /// with this call, 328k and 329k on back to back runs.
        ///
        /// Costs one <c>ovrgpuprofiler</c> round trip plus <see cref="kRenderStagePrimeSeconds"/>
        /// on the capture thread, and puts the app briefly under the Adreno GPU profiler layer,
        /// which is the layer that kills it in roughly one capture in eight (T247804164). That
        /// crash predates this call and happens during the capture regardless; what changes is that
        /// the app is now exposed for a second longer. A capture whose app dies is caught by
        /// <see cref="IsPackageForeground"/> and abandoned rather than filed as a bogus card.
        /// </remarks>
        /// <summary>Serialises render stage traces against each other and against a capture.</summary>
        /// <remarks>
        /// A running <c>-t</c> consumes the render stage stream exclusively: one spanning a capture
        /// leaves the trace with zero GPU slices, and a second <c>-t</c> started while one is in
        /// flight fails outright with "Failed to start render stage tracing". Both call sites take
        /// this, so priming can be issued freely without either hazard.
        /// </remarks>
        private static readonly object renderStagePrimeLock = new object();

        internal static void PrimeRenderStageStream()
        {
            lock (renderStagePrimeLock)
            {
                PrimeRenderStageStreamLocked();
            }
        }

        private static void PrimeRenderStageStreamLocked()
        {
            if (adbTool == null || !adbTool.isReady || connectedDevices.Count == 0)
            {
                return;
            }

            string outputString;
            string errorString;

            // -t=N is the duration form. A bare "-t N" is not: it parses as the flag with no
            // argument, silently runs the 0.1s default and leaves the N as a stray token.
            int code = adbTool.RunCommand(
                new string[] { "-s", connectedDevices[0], "shell ovrgpuprofiler -t=" + kRenderStagePrimeSeconds },
                PerfettoCapture, out outputString, out errorString, null, kGpuProfilerCommandTimeoutMs);

            RO.Util.DebugLog("adb ovrgpuprofiler -t : " + outputString);

            // The tool reports both of these on stdout and still exits 0. Each one means the
            // capture that follows will have no GPU data, so neither is worth swallowing.
            if (code != 0 ||
                string.IsNullOrEmpty(outputString) ||
                outputString.Contains("No render stage data") ||
                outputString.Contains("Failed to start render stage tracing"))
            {
                // A warning, not an error: the capture still runs and its CPU metrics are still
                // worth having. The common cause is benign -- a prime issued while another one is
                // in flight, or before the app has started rendering -- and the message only has
                // to be visible enough to explain a later GPU: 0.00ms.
                RO.Util.DebugLogWarning(
                    "Could not start the render stage stream (exit " + code + "); this capture will " +
                    "likely report 0.00ms GPU time. " + outputString + " " + errorString);
            }
        }

        private static void ReportGpuServiceNotReady(string lastState)
        {
            RO.Util.DebugLogError(
                "GPU profiling service is not running after " + kGpuServiceReadyTimeoutMs +
                "ms; this capture will have no GPU data. init.svc.gpuprofserver = " +
                (string.IsNullOrEmpty(lastState) ? "<empty>" : lastState.Trim()));
        }

        private static void PerfettoCaptureCommand(bool lowOverhead, string packageName)
        {
            ClearPerfettoCapture();

            // Only the full capture reads render stages; the low-overhead config has none. Init
            // arms the profiler too, but it can be skipped there, and this is the call site that
            // actually depends on it.
            if (!lowOverhead && ArmGpuProfiler(packageName))
            {
                WaitForGpuProfilingService();
                ReportIfAppPredatesGpuProfiler();
                PrimeRenderStageStream();
            }

            // Held across the capture as well as the prime: a render stage trace started by
            // another path while perfetto is recording would take the stream and leave this
            // trace with no GPU slices at all.
            lock (renderStagePrimeLock)
            {
                RunPerfettoCapture(lowOverhead, packageName);
            }
        }

        private static void RunPerfettoCapture(bool lowOverhead, string packageName)
        {
            string outputString;
            string errorString;

            const string commandCapture = "shell perfetto -c - --txt -o " + outFile;
            string finalConfig = captureConfig.Replace("{bundle_name}", packageName);
            string lowOverheadConfig = LowOverheadCaptureConfig.Replace("{bundle_name}", packageName);
            RO.Util.DebugLog("core sdk version 70+");

            int code2 = adbTool.RunCommand(new string[] { "-s", connectedDevices[0], commandCapture }, PerfettoCapture, out outputString, out errorString, lowOverhead ? lowOverheadConfig : finalConfig);
            RO.Util.DebugLog("adb Perfetto device: " + code2.ToString());
            RO.Util.DebugLog("adb Perfetto device: " + outputString + " error: " + errorString);

        }

        public static bool ASWDetectionCommand()
        {
            string outputString = "";
            string errorString;
            const string commandCapture = "shell logcat -d | grep ASW";
            connectedDevices = adbTool.GetDevices();


            adbTool.RunCommand(new string[] { "-s", connectedDevices[0], commandCapture }, PerfettoCapture, out outputString, out errorString);
            RO.Util.DebugLog("adb ASW: " + outputString + " error: " + errorString);

            if (outputString.Contains("ASW="))
            {
                return true;
            }
            return false;
        }

        public static bool ExecuteADBCommand(string[] command, out string output)
        {
            string error;
            output = string.Empty;

            // Deliberately silent on the success path: polling callers such as IsDeviceAsleep run
            // this from Update(), so logging each command, each argument and each result floods the
            // console several lines per editor tick and buries real warnings. Failures below carry
            // the command with them, so nothing diagnostic is lost.
            string commandStr = string.Join(" ", command);

            // adbTool is cleared by IsADBReady() whenever the server fails to start, so it can be
            // null here even though the field has an initializer.
            if (adbTool == null || !adbTool.isReady)
            {
                RO.Util.DebugLogError($"[ADB] OVRADBTool not ready, skipping: adb {commandStr}");
                return false;
            }

            int exitCode = adbTool.RunCommand(command, null, out output, out error);
            bool executed = exitCode == 0;

            if (!executed)
            {
                RO.Util.DebugLogError($"[ADB] adb {commandStr} failed with exit code {exitCode}. " +
                                      $"stdout: {output}; stderr: {error}");
            }
            else if (!string.IsNullOrEmpty(error))
            {
                RO.Util.DebugLogError($"[ADB] adb {commandStr} reported: {error}");
            }

            if (!executed)
            {
                output = error;
            }

            return executed;
        }

        public static void PullPerfettoCapture(string captureName)
        {
            RO.Util.DebugLog("Pulling Perfetto Capture");
            string outputString;
            string errorString;
            string outPath = GetOutputDirectory();
            string outFileName = captureName + ".ptrace";
            string finalPath = Path.Combine(outPath, outFileName);

            string tempFile = Path.Combine(Path.GetTempPath(), outFileName);
            string commandPull = string.Format("pull {0} {1}", outFile, tempFile);

            int exitCode = adbTool.RunCommand(new string[] { "-s", connectedDevices[0], commandPull }, PerfettoCapture, out outputString, out errorString, null, kFileTransferTimeoutMs);

            if (exitCode == 0 && File.Exists(tempFile))
            {
                try
                {
                    File.Copy(tempFile, finalPath, true);
                    File.Delete(tempFile);
                    RO.Util.DebugLog($"Perfetto trace pulled to: {finalPath}");
                }
                catch (System.Exception ex)
                {
                    RO.Util.DebugLogError($"Failed to copy trace from temp to project: {ex.Message}");
                }
            }
            else
            {
                RO.Util.DebugLogError($"ADB pull failed (exit code {exitCode}): {errorString}");
            }
        }

        /// <summary>Is the analysed app the activity the device is currently showing?</summary>
        /// <remarks>
        /// Returns true when the answer cannot be determined, so an adb hiccup degrades to the
        /// previous behaviour rather than failing captures that would have been fine.
        /// </remarks>
        static private bool IsPackageForeground(string packageName)
        {
            if (adbTool == null || !adbTool.isReady || string.IsNullOrEmpty(packageName))
            {
                return true;
            }

            string outputString = null;
            string errorString = null;
            int code;

            // The whole point of this helper is to fail open, so the adb calls are wrapped: an
            // exception escaping here would leave the capture thread's lambda without running
            // onFinishedScreenshot, which is worse than letting a doomed capture through.
            try
            {
                List<string> devices = adbTool.GetDevices();
                if (devices == null || devices.Count == 0)
                {
                    return true;
                }

                code = adbTool.RunCommand(
                    new string[] { "-s", devices[0],
                        "shell dumpsys activity activities | grep -m1 topResumedActivity" },
                    PerfettoCapture, out outputString, out errorString, null, kAdbCommandTimeoutMs);
            }
            catch (System.Exception ex)
            {
                RO.Util.DebugLog("Foreground check could not run, letting the capture through: " + ex.Message);
                return true;
            }

            RO.Util.DebugLog("adb topResumedActivity: " + outputString + " error: " + errorString);

            if (code != 0 || string.IsNullOrEmpty(outputString))
            {
                RO.Util.DebugLog(
                    "Foreground check inconclusive (exit " + code + "), letting the capture through");
                return true;
            }

            // dumpsys prints the component as `package/.Activity`, so anchoring on the separator
            // keeps `com.acme.game` from matching a resumed `com.acme.game.launcher`.
            return outputString.Contains(packageName + "/");
        }

        static private void IssueScreenShotCapture(string outName)
        {
            if (adbTool == null)
            {
                UnityEngine.Debug.LogError("adb not found");
                return;
            }
            try
            {
                List<string> devices = adbTool.GetDevices();
                if (devices == null || devices.Count == 0)
                {
                    UnityEngine.Debug.LogError("adb no adb device found");
                    return;
                }
                const string outFile = "/sdcard/insight.png";
                string outputString;
                string errorString;
                string outPath = GetOutputDirectory();
                // Capture the screen on the device.
                string commandCapture = string.Format("-s {0} shell screencap -p {1}", devices[0], outFile);
                int code = adbTool.RunCommand(new string[] { "-s", devices[0], commandCapture }, PerfettoCapture, out outputString, out errorString);
                if (code == 0)
                {
                    string outFileName = outName + ".png";
                    string finalOutFile = string.Format("\"{0}\"", Path.Combine(outPath, outFileName));
                    string commandPull = string.Format("pull {0} {1}", outFile, finalOutFile);
                    RO.Util.DebugLog("adb screencap pull: " + commandPull);
                    int pullCode = adbTool.RunCommand(new string[] { "-s", devices[0], commandPull }, PerfettoCapture, out outputString, out errorString, null, kFileTransferTimeoutMs);
                    if (pullCode != 0)
                    {
                        RO.Util.DebugLogError($"adb screencap pull failed (exit code {pullCode}): {errorString}");
                    }
                    RO.Util.DebugLog("adb screencap pull: " + (outputString ?? "null"));
                    RO.Util.DebugLog("adb screencap pull: " + (errorString ?? "null"));
                }
                else
                {
                    UnityEngine.Debug.LogError("adb screencap device: " + code.ToString());
                    UnityEngine.Debug.LogError("adb screencap device: " + (outputString ?? "null"));
                    UnityEngine.Debug.LogError("adb screencap device: " + (errorString ?? "null"));
                }
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogError("Exception caught while capturing screen on device. Details  \r\n" + ex.Message.ToString() + "\r\n" + ex.StackTrace);
            }
        }

        static public int GetAndroidOSVersion()
        {
            string output;
            if (CaptureTool.ExecuteADBCommand(new[] { "shell getprop ro.build.version.release" }, out output))
            {
                if (int.TryParse(output.Trim(), out int osVersion))
                {
                    return osVersion;
                }
                else
                {
                    RO.Util.DebugLog("Getting Android OS Version failed with error: " + output);
                }
            }
            return 0;
        }

        static public int GetHeadsetOSVersion()
        {
            string output;
            if (CaptureTool.ExecuteADBCommand(new[] { "shell getprop ro.vros.build.version" }, out output))
            {
                if (int.TryParse(output.Trim(), out int osVersion))
                {
                    return osVersion;
                }
                else
                {
                    RO.Util.DebugLog("Getting Quest OS Version failed with error: " + output);
                }
            }
            return 0;
        }

        /// <summary>
        /// Checks if the connected device is currently asleep (T241267036).
        /// Used by the async capture thread to detect device sleep and abort early,
        /// preventing blank screenshots from being captured.
        /// </summary>
        static public bool IsDeviceAsleep()
        {
            string output;
            if (ExecuteADBCommand(new[] { "shell dumpsys power | grep \"mWakefulness=\"" }, out output))
            {
                if (!string.IsNullOrEmpty(output) && output.Contains("Asleep"))
                {
                    return true;
                }
            }
            return false;
        }

        static public int CompareStringsAsHex(string filePath1, string filePath2)
        {
            string fileName1 = Path.GetFileNameWithoutExtension(filePath1);
            string fileName2 = Path.GetFileNameWithoutExtension(filePath2);
            bool valid1 = ulong.TryParse(fileName1, System.Globalization.NumberStyles.HexNumber, null, out ulong l1);
            bool valid2 = ulong.TryParse(fileName2, System.Globalization.NumberStyles.HexNumber, null, out ulong l2);
            if (!valid1 && !valid2) return string.Compare(fileName1, fileName2, StringComparison.Ordinal);
            if (!valid1) return 1;
            if (!valid2) return -1;
            return (int)(l2 - l1);
        }

        static public string[] GetCapturedList()
        {
            var outPath = GetOutputDirectory();
            // RO.Util.DebugLog("GetCapturedList: " + outPath);
            string[] dirs = Directory.GetFiles(outPath, "*.ptrace");
            Array.Sort(dirs, CompareStringsAsHex);
            return dirs;
        }

        static private void ProcessCaptureToJsonObjFunc(object filePathObj)
        {
            string filePath = (string)filePathObj;
            RO.Util.DebugLog("Async Loading: " + filePath);
            string finalInFile = string.Format("\"{0}\"", filePath);
            MetricAPI.GetCaptureMetric(finalInFile, GetMetricJsonOutputPath(filePath));
            lock (lockObj)
            {
                metricProcessWorkInQueue--;
            }
        }

        static public int CaptureToJsonObjAsyncCount()
        {
            return metricProcessWorkInQueue;
        }

        static public int CaptureToJsonAsyncTimeout()
        {
            lock (lockObj)
            {
                metricProcessWorkInQueue--;
            }
            return metricProcessWorkInQueue;
        }

        static public void ProcessCaptureToJsonObjAsync(string filePath)
        {
            lock (lockObj)
            {
                metricProcessWorkInQueue++;
                ThreadPool.QueueUserWorkItem(ProcessCaptureToJsonObjFunc, filePath);
            }
            RO.Util.DebugLog("Async ProcessCaptureToJsonObjAsync: " + filePath);
        }

        static public void IssuePerfettoCaptureAsync(bool screenshot, string packageName, string captureName, bool lowOverhead = false, Action onFirstExecute = null, Action onFinishedScreenshot = null)
        {
            if (captureThread != null && captureThread.IsAlive)
            {
                captureThread.Abort();
                captureThread.Join();
            }

            captureThread = new Thread(() =>
            {
                if (onFirstExecute != null)
                {
                    onFirstExecute();
                }
                // No device means no trace was written, so pulling one would read whatever the
                // previous capture left on the device -- and PullPerfettoCapture indexes
                // connectedDevices[0], which is empty in exactly this case.
                if (!IssuePerfettoCapture(screenshot, packageName, lowOverhead))
                {
                    if (onFinishedScreenshot != null)
                    {
                        onFinishedScreenshot();
                    }
                    return;
                }

                // Check if device went to sleep during Perfetto capture (T241267036)
                // If asleep, skip pull and screenshot to avoid capturing blank frames
                // Single check covers both pull and screenshot to avoid redundant ADB calls
                bool deviceAsleep = IsDeviceAsleep();
                if (deviceAsleep)
                {
                    RO.Util.DebugLogError("Device entered sleep mode during Perfetto capture - aborting to prevent blank frame");
                    if (onFinishedScreenshot != null)
                    {
                        onFinishedScreenshot();
                    }
                    return;
                }

                // Same reasoning as the sleep check: a capture of the wrong thing is worse than
                // no capture. screencap follows the display, so if the app is not on screen the
                // thumbnail and the trace describe the Home environment, not the app.
                if (!IsPackageForeground(packageName))
                {
                    RO.Util.DebugLogError(
                        "Aborting capture: " + packageName + " is not the foreground app, so the " +
                        "screenshot and trace would not be of the app being analysed");
                    if (onFinishedScreenshot != null)
                    {
                        onFinishedScreenshot();
                    }
                    return;
                }

                PullPerfettoCapture(captureName);
                if (screenshot)
                {
                    IssueScreenShotCapture(captureName);
                }
                if (onFinishedScreenshot != null)
                {
                    onFinishedScreenshot();
                }
            });
            captureThread.Start();
        }

        /// <summary>
        /// Captures only a screenshot without any Perfetto trace capture.
        /// This is useful for freeze frame scenarios where you only need a visual thumbnail.
        /// </summary>
        /// <returns>True if capture was started, false if a capture is already in progress</returns>
        static public void IssueScreenShotCaptureAsync(string captureName, Action onFirstExecute = null, Action onFinished = null)
        {
            if (captureThread != null && captureThread.IsAlive)
            {
                captureThread.Abort();
                captureThread.Join();
            }

            captureThread = new Thread(() =>
            {
                if (onFirstExecute != null)
                {
                    onFirstExecute();
                }

                // Check if device is asleep before taking screenshot (T241267036)
                if (IsDeviceAsleep())
                {
                    RO.Util.DebugLogError("Device is asleep - skipping screenshot capture to prevent blank frame");
                }
                else
                {
                    IssueScreenShotCapture(captureName);
                }

                if (onFinished != null)
                {
                    onFinished();
                }
            });
            captureThread.Start();
        }
        static public bool IsFileLocked(string filePath)
        {
            try
            {
                using (FileStream fileStream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                {
                    // If the file is not locked, the code above will reach here
                    return false; // File is not locked
                }
            }
            catch (IOException)
            {
                // Check if the exception is related to a file lock
                return true; // File is locked
            }
        }
    }
}
