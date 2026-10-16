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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.Settings;
using Meta.XR.Json;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Service for OpenAI Codex CLI integration in Unity Editor.
    /// Uses a one-shot process-per-prompt model with streaming JSON output.
    /// </summary>
    [RegisterAIService(ServiceId, "OpenAI Codex", Priority = 30, ExecutableName = "codex", SkillsSubPath = ".codex/skills")]
    public class CodexService : AIServiceBase, IServiceSettingsUI, IServiceValidation, IAIServiceSessionResume, IServiceCommandLineArguments, IServiceCommandLineArgumentsValidation
    {
        public const string ServiceId = "codex";

        private const int ValidationTimeoutSeconds = 10;

        public static class CodexSettings
        {
            private static readonly IIdentified Owner = new CodexDescriptor();

            internal static readonly UserString ExecutablePath = new UserString
            {
                Uid = nameof(ExecutablePath),
                Owner = Owner,
                Default = "",
                Label = "Executable Path",
                Tooltip = "Optional path to Codex CLI executable. Leave empty to use PATH environment variable.",
                SendTelemetry = false
            };

            internal static readonly UserString AdditionalArguments = new UserString
            {
                Uid = nameof(AdditionalArguments),
                Owner = Owner,
                Default = "",
                Label = "Additional Arguments",
                Tooltip = "Arguments appended to the Codex CLI command.",
                SendTelemetry = false
            };

            private class CodexDescriptor : IIdentified
            {
                public string Id => "AgentBridge.Codex";
            }
        }

        private readonly SemaphoreSlim _executionSemaphore = new(1, 1);
        private Process? _currentProcess;
        private readonly object _processLock = new();
        private bool _cancellationRequested;
        private bool _disposed;
        private ValidationResult _currentValidationResult = ValidationResult.Unknown();
        private readonly SemaphoreSlim _validationSemaphore = new(1, 1);

        public override string ServiceName => "OpenAI Codex";
        public override bool HasActiveSession => !string.IsNullOrEmpty(ConversationManager.GetSessionId());
        public ValidationResult CurrentValidationResult => CommandLineArguments.ResolveValidationResult(
            ServiceId,
            CodexSettings.ExecutablePath.Value,
            AdditionalCommandLineArguments,
            _currentValidationResult);
        public string AdditionalCommandLineArguments
        {
            get => CodexSettings.AdditionalArguments.Value;
            set
            {
                if (value == CodexSettings.AdditionalArguments.Value)
                {
                    return;
                }

                CodexSettings.AdditionalArguments.SetValue(value);
                _currentValidationResult = ValidationResult.Unknown();
            }
        }

        /// <inheritdoc />
        public bool CanResumeSession => !string.IsNullOrEmpty(ConversationManager.GetSessionId());

        public override async Task ProcessUserInputAsync(
            string userInput,
            CallerIdentity? caller,
            List<ImageAttachment>? images = null,
            CancellationToken cancellationToken = default,
            string? systemPrompt = null)
        {
            await _executionSemaphore.WaitAsync(cancellationToken);
            try
            {
                Log.Info($"Processing user input through Codex: {userInput}");

                ConversationManager.IsActive = true;
                ConversationManager.ClearError();
                ConversationManager.AddMessage("user", userInput, caller: caller);

                await ExecuteCodexAsync(userInput, systemPrompt, caller);

                ConversationManager.ClearError();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                ConversationManager.SetError($"{ex.Message} ({ex.GetType().Name})");
            }
            finally
            {
                ConversationManager.IsActive = false;
                _executionSemaphore.Release();
            }
        }

        private async Task ExecuteCodexAsync(string userInput, string? systemPrompt, CallerIdentity? caller)
        {
            var executable = ResolveExecutable(CodexSettings.ExecutablePath.Value, "codex");
            Log.Info($"Executing Codex (caller: {caller?.Id ?? "default"})");

            // Codex has no dedicated system-prompt flag, so prepend it to the prompt when present.
            var prompt = string.IsNullOrEmpty(systemPrompt) ? userInput : $"{systemPrompt}\n\n{userInput}";

            // `codex exec` writes only the final assistant message to this file, which avoids parsing the
            // human-oriented transcript on stdout. Created inside the try so the finally always deletes it
            // (and clears _currentProcess) even if construction below throws.
            string? lastMessagePath = null;
            Process? process = null;
            try
            {
                lastMessagePath = Path.GetTempFileName();

                var processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    // Redirect stdin only so we can close it immediately: `codex exec` reads stdin whenever it's
                    // an open pipe and blocks on EOF, so we hand it an empty, already-closed stdin and let the
                    // prompt argument drive the run.
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };

                ConfigureExecutable(processStartInfo, executable);
                ApplyLoginShellPath(processStartInfo);

                AddExecArguments(processStartInfo, lastMessagePath);
                CommandLineArguments.AppendTo(processStartInfo, AdditionalCommandLineArguments);
                // End-of-options separator: the prompt can begin with "---" (the skill's YAML
                // front-matter), which codex's arg parser would otherwise reject as a flag.
                processStartInfo.ArgumentList.Add("--");
                processStartInfo.ArgumentList.Add(prompt);

                _cancellationRequested = false;

                process = new Process { StartInfo = processStartInfo };
                lock (_processLock) { _currentProcess = process; }

                var stdoutBuilder = new StringBuilder();
                var stderrBuilder = new StringBuilder();

                process.ErrorDataReceived += (_, e) => { if (e.Data != null) stderrBuilder.AppendLine(e.Data); };

                process.Start();
                // Close stdin right away (EOF) so codex uses the prompt arg instead of waiting on input.
                try { process.StandardInput.Close(); } catch { }
                process.BeginErrorReadLine();

                // Drain stdout so a full pipe buffer can't deadlock the process; kept only as a fallback.
                var readTask = Task.Run(() =>
                {
                    try
                    {
                        string? line;
                        while ((line = process.StandardOutput.ReadLine()) != null)
                        {
                            if (_cancellationRequested) break;
                            stdoutBuilder.AppendLine(line);
                            ExtractCodexUsage(line, caller);
                            EmitCodexProgress(line, caller);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"Error reading Codex output: {ex.Message}");
                    }
                });

                var timeout = TimeSpan.FromMinutes(10);
                var completed = await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));

                if (!completed)
                {
                    try { process.Kill(); } catch { }
                    throw new TimeoutException("Codex CLI timed out after 10 minutes");
                }

                await Task.WhenAny(readTask, Task.Delay(1000));

                if (_cancellationRequested) return;

                // Prefer the clean final-message file; fall back to raw stdout.
                var answer = "";
                try { if (File.Exists(lastMessagePath)) answer = File.ReadAllText(lastMessagePath).Trim(); } catch { }
                // With --json, stdout is JSONL — extract the agent message from it rather
                // than surfacing raw JSONL if the --output-last-message file is empty.
                if (string.IsNullOrEmpty(answer)) answer = ExtractCodexAnswerFromJson(stdoutBuilder.ToString());

                if (!string.IsNullOrEmpty(answer))
                {
                    await AddAssistantMessageOnMainThreadAsync(answer, caller);
                }
                else if (process.ExitCode != 0)
                {
                    var stderr = stderrBuilder.ToString().Trim();
                    throw new Exception($"Codex exited with code {process.ExitCode}: {stderr}");
                }
                else
                {
                    Log.Warning($"Codex produced no output (exit 0). stderr: {stderrBuilder.ToString().Trim()}");
                    await AddAssistantMessageOnMainThreadAsync("(Codex returned no output.)", caller);
                }
            }
            finally
            {
                // Clear the current-process handle on every exit path (including the timeout throw above),
                // not just the happy path, so a dangling reference can't linger after the run ends.
                lock (_processLock) { _currentProcess = null; }
                process?.Dispose();
                try { if (lastMessagePath != null && File.Exists(lastMessagePath)) File.Delete(lastMessagePath); } catch { }
            }
        }

        private static void AddExecArguments(ProcessStartInfo processStartInfo, string lastMessagePath)
        {
            // `exec` is the non-interactive runner; a bare `codex <prompt>` forwards to the interactive TUI.
            processStartInfo.ArgumentList.Add("exec");
            processStartInfo.ArgumentList.Add("--skip-git-repo-check");
            processStartInfo.ArgumentList.Add("--sandbox");
            processStartInfo.ArgumentList.Add("read-only");
            processStartInfo.ArgumentList.Add("--color");
            processStartInfo.ArgumentList.Add("never");
            // Stream JSONL events to stdout so we can read the terminal `turn.completed`
            // token usage; the clean answer still comes from --output-last-message.
            processStartInfo.ArgumentList.Add("--json");
            processStartInfo.ArgumentList.Add("--output-last-message");
            processStartInfo.ArgumentList.Add(lastMessagePath);
        }

        /// <summary>
        /// Add the assistant message on the main thread and await its commit. Codex is a one-shot
        /// runner that produces its whole answer at the very end, so — unlike streaming providers
        /// whose earlier dispatches the editor update loop has already drained — there is a single
        /// trailing dispatch here. If we merely queued it and returned, the caller could read the
        /// conversation history before the queued add ran, surfacing a spurious "AI response was
        /// empty". Awaiting a TaskCompletionSource yields (rather than blocking like
        /// ExecuteOnMainThreadSync), so it cannot deadlock the main thread.
        /// </summary>
        private static Task AddAssistantMessageOnMainThreadAsync(string content, CallerIdentity? caller)
        {
            var tcs = new TaskCompletionSource<bool>();
            MainThreadDispatcher.ExecuteOnMainThread(() =>
            {
                try { ConversationManager.AddMessage("assistant", content, caller: caller); }
                finally { tcs.TrySetResult(true); }
            });
            return tcs.Task;
        }

        /// <summary>
        /// Turn codex's intermediate `--json` events (turn start, command executions,
        /// reasoning) into `tool_use` conversation messages so the caller's live status
        /// line reflects progress instead of sitting at "Starting…" for the whole
        /// single-turn run. Mirrors the streaming updates ClaudeCodeService emits.
        /// </summary>
        private void EmitCodexProgress(string line, CallerIdentity? caller)
        {
            try
            {
                if (string.IsNullOrEmpty(line)) return;

                var json = JsonObject.Parse(line);
                var type = json["type"]?.ToString();

                // Capture the codex session/thread id so the results screen can offer a
                // "codex resume <id>" command (parity with Claude/Gemini).
                if (type == "thread.started")
                {
                    var threadId = json["thread_id"]?.ToString();
                    if (!string.IsNullOrEmpty(threadId))
                    {
                        MainThreadDispatcher.ExecuteOnMainThread(() =>
                        {
                            ConversationManager.SetSessionId(threadId!);
                            ConversationManager.SetSessionIdForCaller(caller, threadId!);
                        });
                    }
                    return;
                }

                string? toolName = null;
                string? status = null;

                if (type == "turn.started")
                {
                    toolName = "Analyzing";
                    status = "Analyzing your project…";
                }
                else if (type == "item.started")
                {
                    // Emit progress only on item.started, not item.completed: codex sends both
                    // for every item, so acting on both would produce a duplicate progress line.
                    var item = json["item"] as JsonObject;
                    switch (item?["type"]?.ToString())
                    {
                        case "command_execution":
                            toolName = "Bash";
                            status = CleanCodexCommand(item?["command"]?.ToString());
                            break;
                        case "reasoning":
                            toolName = "Analyzing";
                            status = "Reasoning about your project…";
                            break;
                    }
                }

                if (toolName == null || string.IsNullOrEmpty(status)) return;

                var tName = toolName;
                var tStatus = status!;
                MainThreadDispatcher.ExecuteOnMainThread(() =>
                {
                    ConversationManager.AddMessage(new ConversationMessage
                    {
                        MessageId = Guid.NewGuid().ToString(),
                        MessageType = "tool_use",
                        ToolName = tName,
                        Content = $"[{tName}] ({tStatus})",
                        CallerId = caller?.Id ?? string.Empty,
                        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                        // Discrete one-shot progress events, not an updating stream — so consumers
                        // that consolidate streaming stubs treat each as its own line.
                        IsStreaming = false,
                        IsDelta = false
                    });
                });
            }
            catch
            {
                // Progress is best-effort; ignore parse errors.
            }
        }

        /// <summary>
        /// Strip the `/bin/bash -lc '…'` wrapper codex puts around shell commands so the
        /// status line shows just the command. Falls back to a generic label.
        /// </summary>
        private static string CleanCodexCommand(string? command)
        {
            if (string.IsNullOrEmpty(command)) return "Running analysis command…";
            var cmd = command!.Trim();
            const string prefix = "/bin/bash -lc ";
            if (cmd.StartsWith(prefix, StringComparison.Ordinal))
            {
                cmd = cmd.Substring(prefix.Length).Trim();
                if (cmd.Length >= 2 && (cmd[0] == '\'' || cmd[0] == '"') && cmd[cmd.Length - 1] == cmd[0])
                {
                    cmd = cmd.Substring(1, cmd.Length - 2);
                }
            }
            return cmd;
        }

        /// <summary>
        /// Parse a `codex exec --json` line for the terminal `turn.completed` usage event
        /// and accumulate token usage for the caller. Non-usage lines are ignored.
        /// </summary>
        private void ExtractCodexUsage(string line, CallerIdentity? caller)
        {
            try
            {
                if (string.IsNullOrEmpty(line)
                    || line.IndexOf("turn.completed", StringComparison.Ordinal) < 0)
                {
                    return;
                }

                var json = JsonObject.Parse(line);
                if (json["type"]?.ToString() != "turn.completed")
                {
                    return;
                }

                var usage = json["usage"] as JsonObject;
                if (usage == null)
                {
                    return;
                }

                long inputTotal = usage["input_tokens"]?.ToObject<long?>() ?? 0;
                long output = usage["output_tokens"]?.ToObject<long?>() ?? 0;
                long cacheRead = usage["cached_input_tokens"]?.ToObject<long?>() ?? 0;
                // codex's `input_tokens` INCLUDES the cached (reused-context) tokens, so a large
                // agentic run that re-sends the growing context each step reports a huge input.
                // Store only the fresh (non-cached) input so the total reflects tokens actually
                // consumed — matching Claude's convention (its `input_tokens` already excludes cache
                // reads, which it tracks separately) and codex's own headline usage report.
                long input = System.Math.Max(0, inputTotal - cacheRead);
                if (input == 0 && output == 0 && cacheRead == 0)
                {
                    return;
                }

                MainThreadDispatcher.ExecuteOnMainThread(() =>
                {
                    ConversationManager.AddUsageForCaller(caller, input, output, cacheRead, 0, 0);
                });
            }
            catch
            {
                // Ignore parsing errors for usage extraction
            }
        }

        /// <summary>
        /// Fallback answer extraction from `codex exec --json` JSONL: the last
        /// agent-message text, used only when the --output-last-message file is empty.
        /// </summary>
        private static string ExtractCodexAnswerFromJson(string stdout)
        {
            var answer = "";
            if (string.IsNullOrEmpty(stdout))
            {
                return answer;
            }

            foreach (var raw in stdout.Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line[0] != '{')
                {
                    continue;
                }

                try
                {
                    var json = JsonObject.Parse(line);
                    var type = json["type"]?.ToString();
                    if (type == "item.completed")
                    {
                        var item = json["item"] as JsonObject;
                        if (item != null && item["type"]?.ToString() == "agent_message")
                        {
                            var text = item["text"]?.ToString();
                            if (!string.IsNullOrEmpty(text)) answer = text!;
                        }
                    }
                    else if (type == "agent_message")
                    {
                        var text = json["text"]?.ToString();
                        if (!string.IsNullOrEmpty(text)) answer = text!;
                    }
                }
                catch
                {
                    // Skip malformed lines
                }
            }

            return answer.Trim();
        }

        public override void ClearSession()
        {
            Log.Info("Clearing Codex session");
            ConversationManager.Clear();
        }

        /// <inheritdoc />
        public override string? GetResumeCommand(string sessionId)
        {
            var executable = ResolveExecutable(CodexSettings.ExecutablePath.Value, "codex");
            return $"{executable} resume \"{sessionId}\"";
        }

        public override async Task CancelCurrentOperationAsync()
        {
            Process? processToKill;

            lock (_processLock)
            {
                processToKill = _currentProcess;
                if (IsProcessRunning(processToKill))
                    _cancellationRequested = true;
                else
                    processToKill = null;
            }

            if (processToKill != null)
            {
                try
                {
                    Log.Info("Cancelling Codex operation");
                    processToKill.Kill();
                    await Task.Run(() => processToKill.WaitForExit(5000));
                }
                catch (Exception ex)
                {
                    Log.Warning($"Error killing Codex process: {ex.Message}");
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _executionSemaphore?.Dispose();
                _validationSemaphore.Dispose();

                lock (_processLock)
                {
                    if (_currentProcess != null)
                    {
                        try
                        {
                            if (IsProcessRunning(_currentProcess))
                            {
                                _currentProcess.Kill();
                            }
                            _currentProcess.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Log.Warning($"Error disposing process: {ex.Message}");
                        }
                        _currentProcess = null;
                    }
                }
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        #region IServiceSettingsUI Implementation

        public void DrawSettingsUI()
        {
            UnityEditor.EditorGUILayout.LabelField("Codex Settings", UnityEditor.EditorStyles.boldLabel);
            UnityEditor.EditorGUILayout.LabelField("Executable Path", CodexSettings.ExecutablePath.Value);
        }

        public void DrawSettingsUI(Origins origins, IIdentified originData)
        {
            UnityEditor.EditorGUILayout.LabelField("Codex Settings", UnityEditor.EditorStyles.boldLabel);
            CodexSettings.ExecutablePath.DrawForGUI(origins, originData, OnExecutablePathChanged);
        }

        public void ResetSettingsToDefaults()
        {
            CodexSettings.ExecutablePath.Reset();
            CodexSettings.AdditionalArguments.Reset();
            OnExecutablePathChanged();
        }

        private void OnExecutablePathChanged()
        {
            ClearResolvedExecutablePath();
            _currentValidationResult = ValidationResult.Unknown();
            CommandLineArguments.InvalidateValidationResult(ServiceId);
        }

        #endregion

        #region IServiceValidation Implementation

        public Task<ValidationResult> ValidateConfigurationAsync()
        {
            return ValidateConfigurationAsync(false);
        }

        public async Task<ValidationResult> ValidateCommandLineArgumentsAsync()
        {
            var result = await ValidateConfigurationAsync(true);
            return CommandLineArguments.RecordValidationResult(
                ServiceId,
                CodexSettings.ExecutablePath.Value,
                AdditionalCommandLineArguments,
                result);
        }

        private async Task<ValidationResult> ValidateConfigurationAsync(bool validateCommandLineArguments)
        {
            if (!await _validationSemaphore.WaitAsync(CommandLineArguments.ValidationQueueTimeoutMilliseconds))
            {
                return CommandLineArguments.ValidationAlreadyInProgress();
            }
            try
            {
                _currentValidationResult = ValidationResult.Validating();
                var configuredPath = CodexSettings.ExecutablePath.Value;

                if (!CommandLineArguments.TryParse(
                        AdditionalCommandLineArguments,
                        out var additionalArguments,
                        out var argumentError))
                {
                    _currentValidationResult = ValidationResult.Invalid(argumentError);
                    return _currentValidationResult;
                }

                if (!string.IsNullOrEmpty(configuredPath) && !File.Exists(configuredPath))
                {
                    _currentValidationResult = ValidationResult.Invalid($"Executable not found: {configuredPath}");
                    return _currentValidationResult;
                }

                if (validateCommandLineArguments && additionalArguments.Count > 0)
                {
                    string? smokeLastMessagePath = null;
                    try
                    {
                        smokeLastMessagePath = Path.GetTempFileName();
                        var smokeTest = new ProcessStartInfo
                        {
                            UseShellExecute = false,
                            RedirectStandardInput = true,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            CreateNoWindow = true
                        };
                        ConfigureExecutable(smokeTest, ResolveExecutable(configuredPath, "codex"));
                        AddExecArguments(smokeTest, smokeLastMessagePath);
                        foreach (var argument in additionalArguments)
                        {
                            smokeTest.ArgumentList.Add(argument);
                        }
                        smokeTest.ArgumentList.Add("--");
                        smokeTest.ArgumentList.Add(CommandLineArguments.ValidationPrompt);
                        ApplyLoginShellPath(smokeTest);

                        _currentValidationResult = await CommandLineArguments.RunSmokeTestAsync(
                            smokeTest,
                            "Codex");
                        return _currentValidationResult;
                    }
                    finally
                    {
                        try
                        {
                            if (smokeLastMessagePath != null && File.Exists(smokeLastMessagePath))
                            {
                                File.Delete(smokeLastMessagePath);
                            }
                        }
                        catch { }
                    }
                }

                var processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                ConfigureExecutable(processStartInfo, ResolveExecutable(configuredPath, "codex"));
                processStartInfo.ArgumentList.Add("--version");

                ApplyLoginShellPath(processStartInfo);

                using var process = new Process { StartInfo = processStartInfo };
                var outputBuilder = new StringBuilder();
                var errorBuilder = new StringBuilder();

                process.Start();

                var readTask = Task.Run(() =>
                {
                    try
                    {
                        var output = process.StandardOutput.ReadToEnd();
                        if (!string.IsNullOrEmpty(output))
                            outputBuilder.Append(output);
                    }
                    catch { }
                });
                var errorTask = Task.Run(() =>
                {
                    try { errorBuilder.Append(process.StandardError.ReadToEnd()); }
                    catch { }
                });

                var waitTask = Task.Run(() =>
                {
                    try { return process.WaitForExit(ValidationTimeoutSeconds * 1000); }
                    catch { return false; }
                });

                var completed = await waitTask;

                if (!completed)
                {
                    CommandLineArguments.KillProcessTree(process);
                    _currentValidationResult = ValidationResult.Error("Codex CLI timed out");
                    return _currentValidationResult;
                }

                await Task.WhenAny(Task.WhenAll(readTask, errorTask), Task.Delay(500));

                if (process.ExitCode != 0)
                {
                    _currentValidationResult = ValidationResult.Invalid(CommandLineArguments.GetValidationError(
                        errorBuilder.ToString(),
                        "Codex CLI not found or not configured"));
                    return _currentValidationResult;
                }

                var version = outputBuilder.ToString().Trim();
                if (string.IsNullOrEmpty(version)) version = "unknown version";
                _currentValidationResult = ValidationResult.Valid($"Codex CLI available ({version})");
            }
            catch (System.ComponentModel.Win32Exception)
            {
                _currentValidationResult = ValidationResult.Invalid("Codex CLI not found. Install OpenAI Codex or set the executable path.");
            }
            catch (Exception ex)
            {
                _currentValidationResult = ValidationResult.Error($"Validation error: {ex.Message}");
            }
            finally
            {
                _validationSemaphore.Release();
            }

            return _currentValidationResult;
        }

        #endregion
    }
}
