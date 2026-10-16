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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Meta.XR.Guides.Editor.SdkUpgrader;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Meta.XR.Guides.Editor.Welcome
{
    internal static class MetaVrCliInstaller
    {
        private const string InstallerUrl = "https://developers.meta.com/horizon/install-cli/";
        private const int InstallTimeoutMs = 5 * 60 * 1000;

        private static SynchronizationContext _mainThread;

        internal static event Action StateChanged;

        internal static bool IsInstalling { get; private set; }

        [InitializeOnLoadMethod]
        private static void InitOnMainThread() => _mainThread = SynchronizationContext.Current;

        internal static bool SupportsDirectInstall(RuntimePlatform platform) =>
            platform is RuntimePlatform.OSXEditor or RuntimePlatform.WindowsEditor;

        internal static bool TryCreateProcess(
            RuntimePlatform platform,
            out ProcessStartInfo startInfo)
        {
            if (platform == RuntimePlatform.OSXEditor)
            {
                startInfo = new ProcessStartInfo(
                    "/bin/sh",
                    $"-c \"curl -fsSL --proto '=https' --proto-redir '=https' {InstallerUrl} | /bin/sh\"");
            }
            else if (platform == RuntimePlatform.WindowsEditor)
            {
                startInfo = new ProcessStartInfo(
                    "powershell.exe",
                    $"-NoProfile -NonInteractive -ExecutionPolicy Bypass -Command \"Invoke-RestMethod -UseBasicParsing '{InstallerUrl}windows/' | Invoke-Expression\"");
            }
            else
            {
                startInfo = null;
                return false;
            }

            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            return true;
        }

        internal static bool Install()
        {
            if (IsInstalling || !TryCreateProcess(Application.platform, out var startInfo))
            {
                return false;
            }

            IsInstalling = true;
            _mainThread ??= SynchronizationContext.Current;
            StateChanged?.Invoke();

            _ = Task.Run(() => Run(startInfo)).ContinueWith(task =>
            {
                var result = task.IsFaulted
                    ? (false, task.Exception?.GetBaseException().Message ?? "Unknown installer failure")
                    : task.Result;
                OnMainThread(() => Complete(result.Item1, result.Item2));
            }, TaskScheduler.Default);
            return true;
        }

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

        private static (bool success, string error) Run(ProcessStartInfo startInfo)
        {
            using var process = Process.Start(startInfo);
            if (process == null)
            {
                return (false, "The installer process could not be started.");
            }

            var stdout = process.StandardOutput.ReadToEndAsync();
            var stderr = process.StandardError.ReadToEndAsync();
            if (!process.WaitForExit(InstallTimeoutMs))
            {
                try { process.Kill(); } catch { }
                return (false, "The installer did not finish within five minutes.");
            }

            var output = stdout.GetAwaiter().GetResult();
            var error = stderr.GetAwaiter().GetResult();
            return process.ExitCode == 0
                ? (true, null)
                : (false, FirstNonEmpty(error, output, $"Installer exited with code {process.ExitCode}."));
        }

        private static void Complete(bool processSucceeded, string error)
        {
            IsInstalling = false;
            SdkUpgraderData.ResetCliVersionProbe();

            if (!processSucceeded)
            {
                Debug.LogError($"[Welcome] Meta VR CLI installation failed: {error}");
            }
            else if (SdkUpgraderData.FindMetaVrCliBinary() == null)
            {
                Debug.LogError(
                    "[Welcome] Meta VR CLI installer completed, but the metavr binary could not be found. " +
                    "Open the Meta VR CLI documentation for manual installation steps.");
            }
            else
            {
                Debug.Log("[Welcome] Meta VR CLI installed successfully.");
            }

            StateChanged?.Invoke();
        }

        private static string FirstNonEmpty(params string[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var trimmed = value.Trim();
                    return trimmed.Length <= 1000 ? trimmed : trimmed.Substring(0, 1000) + "…";
                }
            }
            return "Unknown installer failure.";
        }
    }
}
