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

#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meta.XR.AI.AgentBridge.Acp;
using Meta.XR.Json;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Implemented by CLI-backed services that accept user-configured launch arguments.
    /// </summary>
    public interface IServiceCommandLineArguments
    {
        /// <summary>Arguments appended to the service's normal command.</summary>
        string AdditionalCommandLineArguments { get; set; }
    }

    /// <summary>
    /// Optionally implemented by services that can validate their configured launch arguments.
    /// </summary>
    public interface IServiceCommandLineArgumentsValidation
    {
        /// <summary>
        /// Validates the service's complete launch command with its configured arguments.
        /// Providers may use one bounded request through their normal headless or ACP launch mode.
        /// </summary>
        Task<ValidationResult> ValidateCommandLineArgumentsAsync();
    }

    internal static class CommandLineArguments
    {
        internal const string ValidationPrompt = "Reply with exactly OK without using tools.";
        internal const int ValidationTimeoutSeconds = 30;
        internal const int ValidationQueueTimeoutMilliseconds = 5000;
        private const string ValidationInProgressMessage = "Command-line argument validation is already in progress";
        private static readonly ConcurrentDictionary<string, CachedValidationResult> ValidationResults = new();
        private static readonly MethodInfo? KillProcessTreeMethod = typeof(Process).GetMethod(
            nameof(Process.Kill),
            BindingFlags.Instance | BindingFlags.Public,
            null,
            new[] { typeof(bool) },
            null);

        private readonly struct CachedValidationResult
        {
            internal string ExecutablePath { get; }
            internal string Arguments { get; }
            internal ValidationResult Result { get; }

            internal CachedValidationResult(
                string executablePath,
                string arguments,
                ValidationResult result)
            {
                ExecutablePath = executablePath;
                Arguments = arguments;
                Result = result;
            }
        }

        internal static ValidationResult RecordValidationResult(
            string serviceId,
            string executablePath,
            string commandLine,
            ValidationResult result)
        {
            if (result.Status == ValidationStatus.Error &&
                result.Message == ValidationInProgressMessage)
            {
                return result;
            }

            if (string.IsNullOrWhiteSpace(commandLine))
            {
                ValidationResults.TryRemove(serviceId, out _);
            }
            else
            {
                ValidationResults[serviceId] = new CachedValidationResult(
                    executablePath,
                    commandLine,
                    result);
            }

            return result;
        }

        internal static ValidationResult ValidationAlreadyInProgress()
        {
            return ValidationResult.Error(ValidationInProgressMessage);
        }

        internal static ValidationResult ResolveValidationResult(
            string serviceId,
            string executablePath,
            string commandLine,
            ValidationResult configurationResult)
        {
            if (string.IsNullOrWhiteSpace(commandLine) ||
                configurationResult.Status == ValidationStatus.Validating)
            {
                return configurationResult;
            }

            if (ValidationResults.TryGetValue(serviceId, out var cached))
            {
                if (cached.Arguments == commandLine &&
                    cached.ExecutablePath == executablePath)
                {
                    return cached.Result;
                }

                if (cached.Arguments == commandLine)
                {
                    return ValidationResult.Unknown();
                }
            }

            if (configurationResult.Status == ValidationStatus.Invalid ||
                configurationResult.Status == ValidationStatus.Error)
            {
                return configurationResult;
            }

            return new ValidationResult(
                ValidationStatus.Unknown,
                "Additional arguments have not been validated");
        }

        internal static void InvalidateValidationResult(string serviceId)
        {
            ValidationResults.TryRemove(serviceId, out _);
        }

        internal static void ClearValidationResultsForTesting()
        {
            ValidationResults.Clear();
        }

        internal static IReadOnlyList<string> Parse(string commandLine)
        {
            if (!TryParse(commandLine, out var arguments, out var error))
            {
                throw new FormatException(error);
            }

            return arguments;
        }

        internal static bool TryParse(
            string commandLine,
            out IReadOnlyList<string> arguments,
            out string error)
        {
            var parsed = new List<string>();
            var current = new StringBuilder();
            var quote = '\0';
            var hasToken = false;

            for (var index = 0; index < commandLine.Length; index++)
            {
                var character = commandLine[index];

                if (character == '\\' && quote != '\'')
                {
                    var hasNext = index + 1 < commandLine.Length;
                    var next = hasNext ? commandLine[index + 1] : '\0';
                    var escapesDelimiter = quote == '\0'
                        ? hasNext && (next == '\'' || next == '"')
                        : hasNext && next == quote;

                    if (escapesDelimiter)
                    {
                        current.Append(next);
                        index++;
                    }
                    else
                    {
                        current.Append(character);
                    }

                    hasToken = true;
                    continue;
                }

                if (quote != '\0')
                {
                    if (character == quote)
                    {
                        quote = '\0';
                    }
                    else
                    {
                        current.Append(character);
                    }

                    hasToken = true;
                    continue;
                }

                if (character == '\'' || character == '"')
                {
                    quote = character;
                    hasToken = true;
                }
                else if (char.IsWhiteSpace(character))
                {
                    if (hasToken)
                    {
                        parsed.Add(current.ToString());
                        current.Clear();
                        hasToken = false;
                    }
                }
                else
                {
                    current.Append(character);
                    hasToken = true;
                }
            }

            if (quote != '\0')
            {
                arguments = Array.Empty<string>();
                error = "Additional arguments contain an unterminated quote.";
                return false;
            }

            if (hasToken)
            {
                parsed.Add(current.ToString());
            }

            arguments = parsed;
            error = string.Empty;
            return true;
        }

        internal static void AppendTo(ProcessStartInfo processStartInfo, string commandLine)
        {
            if (!TryParse(commandLine, out var arguments, out var error))
            {
                throw new InvalidOperationException($"Invalid additional arguments: {error}");
            }

            foreach (var argument in arguments)
            {
                processStartInfo.ArgumentList.Add(argument);
            }
        }

        internal static string GetValidationError(string standardError, string fallback)
        {
            var lines = standardError.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.TrimStart().StartsWith("error:", StringComparison.OrdinalIgnoreCase))
                {
                    return line.Trim();
                }
            }

            return lines.Length > 0 ? lines[0].Trim() : fallback;
        }

        internal static async Task<ValidationResult> RunSmokeTestAsync(
            ProcessStartInfo processStartInfo,
            string serviceName,
            string? standardInput = null)
        {
            processStartInfo.StandardOutputEncoding = Encoding.UTF8;
            processStartInfo.StandardErrorEncoding = Encoding.UTF8;
            using var process = new Process { StartInfo = processStartInfo };
            process.Start();

            var outputTask = Task.Run(() => process.StandardOutput.ReadToEnd());
            var errorTask = Task.Run(() => process.StandardError.ReadToEnd());

            if (processStartInfo.RedirectStandardInput)
            {
                try
                {
                    if (!string.IsNullOrEmpty(standardInput))
                    {
                        await process.StandardInput.WriteAsync(standardInput);
                    }
                    process.StandardInput.Close();
                }
                catch (Exception ex) when (
                    (ex is IOException || ex is InvalidOperationException) && process.HasExited)
                {
                    var streamError = await GetStreamReadErrorAsync(outputTask, errorTask);
                    if (streamError != null)
                    {
                        return ValidationResult.Error($"{serviceName} validation output failed: {streamError}");
                    }
                    return ValidationResult.Invalid(GetProcessError(
                        outputTask.Result,
                        errorTask.Result,
                        $"{serviceName} rejected the additional arguments"));
                }
            }

            var completed = await Task.Run(() => process.WaitForExit(ValidationTimeoutSeconds * 1000));

            if (!completed)
            {
                KillProcessTree(process);
                var drainTask = Task.WhenAll(outputTask, errorTask);
                if (await Task.WhenAny(drainTask, Task.Delay(2000)) == drainTask)
                {
                    await ObserveFailureAsync(drainTask);
                }
                else
                {
                    _ = ObserveFailureAsync(drainTask);
                }
                return ValidationResult.Error($"{serviceName} validation timed out");
            }

            var outputError = await GetStreamReadErrorAsync(outputTask, errorTask);
            if (outputError != null)
            {
                return ValidationResult.Error($"{serviceName} validation output failed: {outputError}");
            }
            if (process.ExitCode == 0)
            {
                return ValidationResult.Valid($"{serviceName} accepted the additional arguments");
            }

            var output = outputTask.Result;
            var error = errorTask.Result;
            return ValidationResult.Invalid(GetProcessError(
                output,
                error,
                $"{serviceName} rejected the additional arguments"));
        }

        internal static async Task<ValidationResult> RunAcpSmokeTestAsync(
            string executablePath,
            string? environmentPath,
            string acpFlag,
            IReadOnlyList<string> additionalArguments,
            string workingDirectory,
            string serviceName)
        {
            var client = new AcpClient(
                executablePath,
                environmentPath,
                acpFlag,
                additionalArguments);
            var cancellation = new CancellationTokenSource();
            var timeoutCancellation = new CancellationTokenSource();
            var disposeClient = true;

            try
            {
                var smokeTestTask = RunAcpSmokeTestAsync(
                    client,
                    workingDirectory,
                    cancellation.Token);
                var timeoutTask = Task.Delay(
                    ValidationTimeoutSeconds * 1000,
                    timeoutCancellation.Token);
                var completedTask = await Task.WhenAny(smokeTestTask, timeoutTask);
                timeoutCancellation.Cancel();
                await ObserveFailureAsync(timeoutTask);
                if (completedTask != smokeTestTask)
                {
                    cancellation.Cancel();
                    var cleanupTask = ObserveFailureAsync(smokeTestTask);
                    if (await Task.WhenAny(cleanupTask, Task.Delay(2000)) != cleanupTask)
                    {
                        client.Abort();
                        if (await Task.WhenAny(cleanupTask, Task.Delay(2000)) != cleanupTask)
                        {
                            disposeClient = false;
                            _ = DisposeAfterTaskAsync(cleanupTask, client);
                        }
                    }
                    return ValidationResult.Error($"{serviceName} validation timed out");
                }

                await smokeTestTask;
                return ValidationResult.Valid($"{serviceName} accepted the additional arguments");
            }
            catch (AcpException ex)
            {
                return ValidationResult.Invalid(ex.Message);
            }
            catch (Exception ex)
            {
                return ValidationResult.Error($"{serviceName} validation failed: {ex.Message}");
            }
            finally
            {
                timeoutCancellation.Cancel();
                timeoutCancellation.Dispose();
                cancellation.Dispose();
                if (disposeClient)
                {
                    client.Dispose();
                }
            }
        }

        private static async Task RunAcpSmokeTestAsync(
            AcpClient client,
            string workingDirectory,
            CancellationToken cancellationToken)
        {
            await client.InitializeAsync(
                "unity-agent-bridge-validation",
                "1.0.0",
                cancellationToken);
            var session = await client.NewSessionAsync(workingDirectory, cancellationToken);
            await client.PromptAsync(
                session.SessionId,
                new List<ContentBlock> { new TextContentBlock { Text = ValidationPrompt } },
                cancellationToken);
        }

        internal static void KillProcessTree(Process process)
        {
            try
            {
                if (KillProcessTreeMethod != null)
                {
                    KillProcessTreeMethod.Invoke(process, new object[] { true });
                }
                else
                {
                    KillProcessTreeWithPlatformFallback(process);
                }
            }
            catch
            {
                Log.Warning("Process tree termination failed; using the platform fallback");
                KillProcessTreeWithPlatformFallback(process);
            }

            try { process.WaitForExit(2000); } catch { }
        }

        private static void KillProcessTreeWithPlatformFallback(Process process)
        {
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    RunProcess("taskkill", "/PID", process.Id.ToString(), "/T", "/F");
                }
                else
                {
                    KillUnixDescendants(process.Id, new HashSet<int>());
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Process tree fallback failed: {ex.Message}");
            }

            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Validation process termination failed: {ex.Message}");
            }
        }

        private static void KillUnixDescendants(int parentProcessId, HashSet<int> visited)
        {
            if (!visited.Add(parentProcessId))
            {
                return;
            }

            foreach (var childProcessId in GetUnixChildProcessIds(parentProcessId))
            {
                KillUnixDescendants(childProcessId, visited);
                try
                {
                    using var childProcess = Process.GetProcessById(childProcessId);
                    if (!childProcess.HasExited)
                    {
                        childProcess.Kill();
                        childProcess.WaitForExit(2000);
                    }
                }
                catch { }
            }
        }

        private static IReadOnlyList<int> GetUnixChildProcessIds(int parentProcessId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxChildProcessIds(parentProcessId);
            }

            var childProcessIds = new List<int>();
            using var pgrep = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "pgrep",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            pgrep.StartInfo.ArgumentList.Add("-P");
            pgrep.StartInfo.ArgumentList.Add(parentProcessId.ToString());
            try
            {
                pgrep.Start();
            }
            catch (Win32Exception ex)
            {
                Log.Warning($"Unable to enumerate child processes because pgrep could not start: {ex.Message}");
                return childProcessIds;
            }
            var output = pgrep.StandardOutput.ReadToEnd();
            pgrep.WaitForExit(2000);

            foreach (var line in output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (int.TryParse(line, out var childProcessId))
                {
                    childProcessIds.Add(childProcessId);
                }
            }

            return childProcessIds;
        }

        private static IReadOnlyList<int> GetLinuxChildProcessIds(int parentProcessId)
        {
            var childProcessIds = new HashSet<int>();
            var taskDirectory = $"/proc/{parentProcessId}/task";
            string[] threadDirectories;
            try
            {
                threadDirectories = Directory.GetDirectories(taskDirectory);
            }
            catch (IOException)
            {
                return new List<int>();
            }
            catch (UnauthorizedAccessException)
            {
                return new List<int>();
            }

            foreach (var threadDirectory in threadDirectories)
            {
                var childrenPath = Path.Combine(threadDirectory, "children");
                string children;
                try
                {
                    if (!File.Exists(childrenPath))
                    {
                        continue;
                    }
                    children = File.ReadAllText(childrenPath);
                }
                catch (IOException) { continue; }
                catch (UnauthorizedAccessException) { continue; }

                foreach (var value in children.Split(
                             new[] { ' ', '\t', '\r', '\n' },
                             StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(value, out var childProcessId))
                    {
                        childProcessIds.Add(childProcessId);
                    }
                }
            }

            return new List<int>(childProcessIds);
        }

        private static void RunProcess(string executable, params string[] arguments)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = executable,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            process.Start();
            process.WaitForExit(2000);
        }

        private static async Task ObserveFailureAsync(Task task)
        {
            try { await task; }
            catch { }
        }

        private static async Task DisposeAfterTaskAsync(Task task, IDisposable disposable)
        {
            await ObserveFailureAsync(task);
            try { disposable.Dispose(); }
            catch (Exception ex) { Log.Warning($"Deferred validation cleanup failed: {ex.Message}"); }
        }

        private static async Task<string?> GetStreamReadErrorAsync(
            Task<string> outputTask,
            Task<string> errorTask)
        {
            try
            {
                await Task.WhenAll(outputTask, errorTask);
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        internal static string GetProcessError(string standardOutput, string standardError, string fallback)
        {
            string? structuredError = null;
            foreach (var line in standardOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                try
                {
                    var json = JsonObject.Parse(line);
                    var type = json["type"]?.Value<string>();
                    var message = json["message"]?.Value<string>();
                    if (string.Equals(type, "error", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrEmpty(message))
                    {
                        structuredError ??= message;
                    }
                }
                catch { }
            }

            if (structuredError != null)
            {
                return structuredError;
            }

            return GetValidationError(standardError, fallback);
        }
    }
}
