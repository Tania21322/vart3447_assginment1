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
using System.IO;
using System.Linq;
using UnityEngine;

namespace Meta.XR.Guides.Editor.SdkUpgrader
{
    // MetaVR CLI detection: whether the `metavr` binary is installed, resolved without relying on the
    // shell PATH (the Unity editor GUI on macOS does not inherit it).
    internal static partial class SdkUpgraderData
    {
        /// <summary>Install state of the metavr CLI relative to a required minimum version.</summary>
        internal enum MetaVrCliState
        {
            NotInstalled,
            Outdated, // installed, but below the required minimum version
            Ready,    // installed and at/above the minimum (or no minimum required)
        }

        /// <summary>
        /// The metavr CLI's install state relative to <paramref name="minVersion"/> (blank/unparseable =
        /// any install counts as Ready). The CLI installs under <c>&lt;home&gt;/.metavr</c> (home = $HOME /
        /// %USERPROFILE%, overridable by METAVR_HOME/HZDB_HOME); we detect the <c>metavr</c> binary there
        /// rather than scanning PATH, because the Unity editor (a GUI app on macOS) does not inherit the
        /// shell PATH the CLI's bin sits on. The internal debug menu can override this via
        /// <see cref="MetaVrCliStateOverride"/>.
        /// </summary>
        internal static MetaVrCliState GetMetaVrCliState(string minVersion)
        {
            if (MetaVrCliStateOverride.HasValue)
            {
                return MetaVrCliStateOverride.Value;
            }

            if (FindMetaVrCliBinary() == null)
            {
                return MetaVrCliState.NotInstalled;
            }

            // Only flag "outdated" when we can parse BOTH a required minimum and the installed version;
            // an unknown installed version (e.g. `--version` unavailable) counts as Ready so a probe
            // failure never nags the user to upgrade.
            var min = ParseCliVersion(minVersion);
            if (min != null)
            {
                var installed = InstalledCliVersion();
                if (installed != null && installed < min)
                {
                    return MetaVrCliState.Outdated;
                }
            }

            return MetaVrCliState.Ready;
        }

        // Resolves the MetaVR CLI install root (<home>/.metavr), mirroring the CLI's own resolution:
        // METAVR_HOME / HZDB_HOME env, else the user home. Robust to Unity's Mono occasionally returning
        // an empty SpecialFolder.UserProfile by also trying the HOME / USERPROFILE env vars.
        internal static string MetaVrHome()
        {
            // An env var SET to empty ("") comes back as "" (not null), so ?? wouldn't fall through;
            // treat blank the same as unset at each step so an empty METAVR_HOME still tries HZDB_HOME,
            // etc. Order: METAVR_HOME -> HZDB_HOME -> user profile -> HOME -> USERPROFILE.
            var home = Environment.GetEnvironmentVariable("METAVR_HOME");
            if (string.IsNullOrWhiteSpace(home))
            {
                home = Environment.GetEnvironmentVariable("HZDB_HOME");
            }
            if (string.IsNullOrWhiteSpace(home))
            {
                home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            if (string.IsNullOrWhiteSpace(home))
            {
                home = Environment.GetEnvironmentVariable("HOME");
            }
            if (string.IsNullOrWhiteSpace(home))
            {
                home = Environment.GetEnvironmentVariable("USERPROFILE");
            }
            return string.IsNullOrWhiteSpace(home) ? null : Path.Combine(home, ".metavr");
        }

        // Fixed install locations checked directly, because the Unity editor GUI on macOS does not
        // inherit the shell PATH; these cover Homebrew (Apple Silicon / Intel) and manual installs.
        private static readonly string[] KnownUnixBinDirs =
        {
            "/opt/homebrew/bin",   // Homebrew (Apple Silicon)
            "/usr/local/bin",      // Homebrew (Intel) / manual
        };

        // Returns the metavr binary path if found, else null. Checks, in order: the .metavr install root
        // (curl bin/ + npm cache), the known fixed bin dirs, then PATH.
        internal static string FindMetaVrCliBinary()
        {
            try
            {
                var exe = Application.platform == RuntimePlatform.WindowsEditor ? "metavr.exe" : "metavr";

                var metavrDir = MetaVrHome();
                if (metavrDir != null && Directory.Exists(metavrDir))
                {
                    var binPath = Path.Combine(metavrDir, "bin", exe);
                    if (File.Exists(binPath))
                    {
                        return binPath;
                    }

                    var npmBinaries = Path.Combine(metavrDir, "npm", "binaries");
                    if (Directory.Exists(npmBinaries))
                    {
                        var hit = Directory.EnumerateFiles(npmBinaries, exe, SearchOption.AllDirectories).FirstOrDefault();
                        if (hit != null)
                        {
                            return hit;
                        }
                    }
                }

                if (Application.platform != RuntimePlatform.WindowsEditor)
                {
                    foreach (var dir in KnownUnixBinDirs)
                    {
                        var candidate = Path.Combine(dir, exe);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                }

                var pathVar = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(pathVar))
                {
                    foreach (var dir in pathVar.Split(Path.PathSeparator))
                    {
                        if (string.IsNullOrEmpty(dir))
                        {
                            continue;
                        }

                        var candidate = Path.Combine(dir, exe);
                        if (File.Exists(candidate))
                        {
                            return candidate;
                        }
                    }
                }

                return null;
            }
            catch
            {
                // Any probe failure (an unreadable dir during npm-cache enumeration, an invalid PATH
                // entry, etc.) degrades to "not found" rather than aborting detection.
                return null;
            }
        }

        // Guards the probe fields so concurrent callers can't double-probe (spawning duplicate
        // `metavr --version` processes) or read a half-updated value.
        private static readonly object _cliProbeLock = new();
        private static bool _cliVersionProbed;
        private static Version _cliVersion;
        // Bumped by every reset; a probe that started before a reset captures the epoch and refuses to
        // publish its now-stale result, so a reset landing mid-probe isn't overwritten by the pre-reset value.
        private static int _cliProbeEpoch;

        // Cached for the session — a re-probe would re-exec the CLI. Reset when the debug override
        // changes or the data force-refreshes (see ResetCliVersionProbe).
        private static Version InstalledCliVersion()
        {
            // Fast path: return the cached probe result. The check stays inside the lock so we never
            // read a torn (half-published) value.
            int epoch;
            lock (_cliProbeLock)
            {
                if (_cliVersionProbed)
                {
                    return _cliVersion;
                }

                epoch = _cliProbeEpoch;
            }

            // Probe OUTSIDE the lock so the up-to-~4s FindMetaVrCliBinary + DetectMetaVrCliVersion never
            // stalls a concurrent caller (e.g. ResetCliVersionProbe from the debug menu). Tradeoff: two
            // racing first-callers could each probe once — a rare, harmless extra `metavr --version` in
            // editor-only code — but the lock is no longer held for the probe's duration.
            var binary = FindMetaVrCliBinary();
            var version = binary == null ? null : DetectMetaVrCliVersion(binary);

            // Publish, but honor another thread's result if it won the race and published first, and
            // discard ours if a reset bumped the epoch mid-probe (so the reset isn't silently overwritten
            // — the next caller re-probes for the post-reset state instead).
            lock (_cliProbeLock)
            {
                if (!_cliVersionProbed && epoch == _cliProbeEpoch)
                {
                    _cliVersion = version;
                    _cliVersionProbed = true;
                }
                return _cliVersion;
            }
        }

        internal static void ResetCliVersionProbe()
        {
            lock (_cliProbeLock)
            {
                _cliVersionProbed = false;
                _cliVersion = null;
                _cliProbeEpoch++;
            }
        }

        // Runs `{binary} --version` and parses the reported version. Returns null on any failure, so the
        // caller treats the CLI as an unknown-but-present version (i.e. not "outdated").
        private static Version DetectMetaVrCliVersion(string binaryPath)
        {
            try
            {
                var psi = new System.Diagnostics.ProcessStartInfo(binaryPath, "--version")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using var proc = System.Diagnostics.Process.Start(psi);
                if (proc == null)
                {
                    return null;
                }

                // Drain the pipes asynchronously so a large/slow writer can't deadlock us (a synchronous
                // ReadToEnd would block before WaitForExit could time out), and kill the process if it
                // overruns the 3s budget so we never leave an orphan.
                var stdout = proc.StandardOutput.ReadToEndAsync();
                var stderr = proc.StandardError.ReadToEndAsync();
                if (!proc.WaitForExit(3000))
                {
                    try { proc.Kill(); proc.WaitForExit(1000); } catch { /* best-effort cleanup */ }
                    // Observe (without blocking) the still-pending reads: killing the process closes the
                    // pipes, which can fault these tasks; left unobserved they'd surface later as a
                    // TaskScheduler.UnobservedTaskException. Don't Wait/Result them — that would block.
                    stdout.ContinueWith(t => { _ = t.Exception; }, System.Threading.Tasks.TaskScheduler.Default);
                    stderr.ContinueWith(t => { _ = t.Exception; }, System.Threading.Tasks.TaskScheduler.Default);
                    return null;
                }

                // GetAwaiter().GetResult() unwraps a faulted read (rather than wrapping it in an
                // AggregateException as .Result would); either way the outer catch degrades to null.
                return ParseCliVersion($"{stdout.GetAwaiter().GetResult()}\n{stderr.GetAwaiter().GetResult()}");
            }
            catch
            {
                return null;
            }
        }

        // Extracts the first "major.minor[.patch]" from arbitrary text (e.g. "metavr 1.5.0", "v1.5").
        internal static Version ParseCliVersion(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var match = System.Text.RegularExpressions.Regex.Match(text, @"(\d+)\.(\d+)(?:\.(\d+))?");
            if (!match.Success)
            {
                return null;
            }

            // TryParse (not Parse) so an out-of-Int32-range component fails closed to null like other
            // unparseable input, rather than throwing out of GetMetaVrCliState.
            if (!int.TryParse(match.Groups[1].Value, out var major)
                || !int.TryParse(match.Groups[2].Value, out var minor))
            {
                return null;
            }

            var patch = match.Groups[3].Success && int.TryParse(match.Groups[3].Value, out var p) ? p : 0;
            return new Version(major, minor, patch);
        }
    }
}
