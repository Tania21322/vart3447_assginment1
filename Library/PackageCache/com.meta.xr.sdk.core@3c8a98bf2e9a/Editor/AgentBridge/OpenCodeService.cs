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
using Meta.XR.AI.AgentBridge.Acp;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.Settings;
using UnityEngine;

namespace Meta.XR.AI.AgentBridge
{
    /// <summary>
    /// Service for OpenCode integration via ACP (Agent Client Protocol).
    /// Launches `opencode acp` and communicates over a bidirectional JSON-RPC 2.0
    /// protocol over stdio, keeping the subprocess alive across prompts within a session.
    /// </summary>
    [RegisterAIService(ServiceId, "OpenCode", Priority = 40, ExecutableName = "opencode", SkillsSubPath = ".opencode/skills")]
    public class OpenCodeService : AIServiceBase, IServiceSettingsUI, IServiceValidation, IAIServiceSessionResume, IServiceCommandLineArguments, IServiceCommandLineArgumentsValidation
    {
        /// <summary>
        /// The unique service identifier for OpenCode.
        /// </summary>
        public const string ServiceId = "opencode";

        /// <summary>
        /// Settings specific to the OpenCode service.
        /// </summary>
        public static class OpenCodeSettings
        {
            private static readonly IIdentified Owner = new OpenCodeDescriptor();

            internal static readonly UserString ExecutablePath = new UserString
            {
                Uid = nameof(ExecutablePath),
                Owner = Owner,
                Default = "",
                Label = "Executable Path",
                Tooltip = "Optional path to OpenCode executable. Leave empty to use PATH environment variable.",
                SendTelemetry = false
            };

            internal static readonly UserString AdditionalArguments = new UserString
            {
                Uid = nameof(AdditionalArguments),
                Owner = Owner,
                Default = "",
                Label = "Additional Arguments",
                Tooltip = "Arguments appended to the OpenCode CLI command.",
                SendTelemetry = false
            };

            private class OpenCodeDescriptor : IIdentified
            {
                public string Id => "AgentBridge.OpenCode";
            }
        }

        private readonly SemaphoreSlim _executionSemaphore = new(1, 1);
        private AcpClient? _acpClient;
        private string? _sessionId;
        private bool _isFirstPromptInSession = true;
        private bool _disposed;
        private int _restartAcpClientOnNextPrompt;
        private ValidationResult _currentValidationResult = ValidationResult.Unknown();
        private readonly SemaphoreSlim _validationSemaphore = new(1, 1);
        // Caller of the in-flight prompt. ACP usage_updates arrive on a background
        // event with no caller, so we stash it here to attribute usage correctly.
        // volatile: written on the calling thread but read on the ACP receive-loop
        // background thread in HandleSessionUpdate, so we need cross-thread visibility.
        private volatile CallerIdentity? _currentCaller;

        /// <inheritdoc/>
        public override string ServiceName => "OpenCode";

        /// <inheritdoc/>
        public override bool HasActiveSession => !string.IsNullOrEmpty(_sessionId);

        /// <inheritdoc />
        public ValidationResult CurrentValidationResult => CommandLineArguments.ResolveValidationResult(
            ServiceId,
            OpenCodeSettings.ExecutablePath.Value,
            AdditionalCommandLineArguments,
            _currentValidationResult);

        /// <inheritdoc />
        public string AdditionalCommandLineArguments
        {
            get => OpenCodeSettings.AdditionalArguments.Value;
            set
            {
                if (value == OpenCodeSettings.AdditionalArguments.Value)
                {
                    return;
                }

                OpenCodeSettings.AdditionalArguments.SetValue(value);
                _currentValidationResult = ValidationResult.Unknown();
                ScheduleAcpClientRestart();
            }
        }

        /// <inheritdoc />
        public bool CanResumeSession => !string.IsNullOrEmpty(_sessionId);

        /// <summary>
        /// Process user input through OpenCode via ACP.
        /// </summary>
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
                if (Interlocked.Exchange(ref _restartAcpClientOnNextPrompt, 0) == 1)
                {
                    RestartAcpClient();
                }

                Log.Info($"Processing user input through OpenCode: {userInput}");
                _currentCaller = caller;
                if (images != null && images.Count > 0)
                {
                    Log.Info($"Processing with {images.Count} image(s)");
                }

                ConversationManager.IsActive = true;
                ConversationManager.ClearError();

                // Add the user's input to the conversation history
                ConversationManager.AddMessage("user", userInput, caller: _currentCaller);

                // Ensure ACP client is connected and session exists
                await EnsureAcpClientAsync();

                if (_acpClient == null || _sessionId == null)
                {
                    throw new InvalidOperationException("Failed to initialize ACP client or session");
                }

                // Build prompt content blocks
                var promptBlocks = new List<ContentBlock>();

                // Include system prompt as first content block on first prompt in session
                if (systemPrompt != null && _isFirstPromptInSession)
                {
                    promptBlocks.Add(new TextContentBlock { Text = $"[System]: {systemPrompt}" });
                }

                // Add images as native base64 content blocks (no temp files needed)
                if (images != null)
                {
                    foreach (var img in images)
                    {
                        if (string.IsNullOrEmpty(img.Data))
                        {
                            continue;
                        }
                        promptBlocks.Add(new ImageContentBlock
                        {
                            Data = img.Data,
                            MimeType = img.MediaType
                        });
                    }
                }

                // Add user input text
                promptBlocks.Add(new TextContentBlock { Text = userInput });

                // Send prompt — streaming updates arrive via OnSessionUpdate event
                await _acpClient.PromptAsync(_sessionId, promptBlocks);

                // Only mark the system prompt as sent once PromptAsync succeeds, so a throw here
                // doesn't skip the system prompt on the next retry within the same session.
                _isFirstPromptInSession = false;

                // Clear error on success
                ConversationManager.ClearError();
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogException(ex);
                ConversationManager.SetError($"{ex.Message} ({ex.GetType().Name})");
            }
            finally
            {
                // Clear so a late ACP event (e.g. a usage_update arriving after this prompt
                // returns) isn't misattributed to this caller on the next turn.
                _currentCaller = null;
                ConversationManager.IsActive = false;
                _executionSemaphore.Release();
            }
        }

        /// <summary>
        /// Clear the current session. Kills the subprocess and resets state.
        /// </summary>
        public override void ClearSession()
        {
            Log.Info("Clearing OpenCode session");

            Interlocked.Exchange(ref _restartAcpClientOnNextPrompt, 0);
            RestartAcpClient();
            ConversationManager.Clear();
        }

        private void RestartAcpClient()
        {
            if (_acpClient != null)
            {
                _acpClient.OnSessionUpdate -= HandleSessionUpdate;
            }
            _acpClient?.Dispose();
            _acpClient = null;
            _sessionId = null;
            _isFirstPromptInSession = true;
        }

        private void ScheduleAcpClientRestart()
        {
            Interlocked.Exchange(ref _restartAcpClientOnNextPrompt, 1);
        }

        /// <summary>
        /// Cancel the current operation by sending a cancel notification and killing the process.
        /// </summary>
        public override async Task CancelCurrentOperationAsync()
        {
            // Capture the client/session we're cancelling. While we await below, a new scan
            // (e.g. the user navigates Back then Next mid-scan) can replace _acpClient; guarding
            // on this reference keeps us from disposing a client a later scan created. (T280100936)
            var client = _acpClient;
            var sessionId = _sessionId;
            if (client == null || sessionId == null)
            {
                return;
            }

            try
            {
                Log.Info("Cancelling OpenCode operation");
                client.Cancel(sessionId);

                // Give a moment for the cancel to take effect
                await Task.Delay(500);
            }
            catch (Exception ex)
            {
                Log.Warning($"Error sending cancel notification: {ex.Message}");
            }

            // Dispose the client we cancelled (cleanup), but only clear the shared fields if a
            // new scan hasn't already swapped in its own client while we awaited — otherwise we'd
            // strand the returning scan's session, which is the T280100936 failure.
            try
            {
                if (client.IsRunning)
                {
                    client.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"Error disposing ACP client during cancel: {ex.Message}");
            }

            // Guard the unsubscribe: if it threw, it would bypass the ReferenceEquals
            // state-clearing block below and leave the shared fields dangling.
            try { client.OnSessionUpdate -= HandleSessionUpdate; }
            catch (Exception ex) { Log.Warning($"Error unsubscribing from ACP client during cancel: {ex.Message}"); }

            if (ReferenceEquals(_acpClient, client))
            {
                _acpClient = null;
                _sessionId = null;
                _isFirstPromptInSession = true;
            }
        }

        /// <summary>
        /// Ensure the ACP client is initialized with an active session.
        /// Creates the subprocess and performs the initialize handshake if needed.
        /// </summary>
        private async Task EnsureAcpClientAsync()
        {
            // If client is alive and session exists, reuse it
            if (_acpClient != null && _acpClient.IsRunning && _sessionId != null)
            {
                return;
            }

            // Clean up any dead client
            if (_acpClient != null)
            {
                _acpClient.OnSessionUpdate -= HandleSessionUpdate;
                _acpClient.Dispose();
                _acpClient = null;
                _sessionId = null;
            }

            var openCodeExecutable = GetOpenCodeExecutable();

            // OpenCode exposes ACP via the `opencode acp` subcommand (a single launch token), so the
            // AcpClient's flag argument is "acp" rather than a "--flag" like Gemini's "--experimental-acp".
            if (!CommandLineArguments.TryParse(
                    AdditionalCommandLineArguments,
                    out var additionalArguments,
                    out var argumentError))
            {
                throw new InvalidOperationException($"Invalid additional arguments: {argumentError}");
            }

            _acpClient = new AcpClient(
                openCodeExecutable,
                GetLoginShellPath(),
                "acp",
                additionalArguments);
            _acpClient.OnSessionUpdate += HandleSessionUpdate;

            Log.Info("Initializing ACP connection to OpenCode");
            await _acpClient.InitializeAsync("unity-agent-bridge", "1.0.0");

            var projectPath = GetProjectPath();
            var session = await _acpClient.NewSessionAsync(projectPath);
            _sessionId = session.SessionId;
            _isFirstPromptInSession = true;

            var sessionCaller = _currentCaller;
            MainThreadDispatcher.ExecuteOnMainThread(() =>
            {
                ConversationManager.SetSessionId(_sessionId);
                // Also record per-caller so the results screen's resume banner
                // (which reads GetSessionIdForCaller) can offer a resume command.
                ConversationManager.SetSessionIdForCaller(sessionCaller, _sessionId);
                Log.Info($"OpenCode session created: {_sessionId}");
            });
        }

        /// <summary>
        /// Handle ACP session/update notifications by converting them to ConversationMessages.
        /// Called from the ACP receive loop background thread — all work is dispatched
        /// to the main thread since both the parser and ConversationManager use Unity APIs.
        /// </summary>
        private void HandleSessionUpdate(SessionUpdateParams update)
        {
            MainThreadDispatcher.ExecuteOnMainThread(() =>
            {
                // Ignore updates from stale sessions
                if (update.SessionId != null && update.SessionId != _sessionId)
                {
                    return;
                }

                // Snapshot the caller once so this event's usage and message are attributed
                // to a consistent caller even if the field changes mid-handler.
                var caller = _currentCaller;

                var conversationInfo = AcpParser.GetConversationContentFromAcp(update);
                if (conversationInfo?.Usage != null)
                {
                    var u = conversationInfo.Usage;
                    ConversationManager.AddUsageForCaller(
                        caller, u.InputTokens, u.OutputTokens,
                        u.CacheReadTokens, u.CacheCreationTokens, u.CostUsd);
                }
                if (conversationInfo == null || !conversationInfo.HasContent)
                {
                    return;
                }

                var message = new ConversationMessage
                {
                    MessageId = conversationInfo.MessageId ?? Guid.NewGuid().ToString(),
                    MessageType = conversationInfo.MessageType,
                    Content = conversationInfo.Content,
                    ToolName = conversationInfo.ToolName ?? string.Empty,
                    Method = conversationInfo.Method ?? string.Empty,
                    Target = conversationInfo.Target ?? string.Empty,
                    Rationale = conversationInfo.Rationale ?? string.Empty,
                    CallerId = caller?.Id ?? string.Empty,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    IsStreaming = conversationInfo.IsStreaming,
                    IsDelta = conversationInfo.IsDelta
                };

                ConversationManager.AddMessage(message);
            });
        }

        private string GetOpenCodeExecutable()
        {
            return ResolveExecutable(OpenCodeSettings.ExecutablePath.Value, "opencode");
        }

        /// <summary>
        /// Get the Unity project root path for the ACP session working directory.
        /// </summary>
        private static string GetProjectPath()
        {
            // Application.dataPath returns "ProjectPath/Assets", go up one level
            var assetsPath = Application.dataPath;
            return Directory.GetParent(assetsPath)?.FullName ?? assetsPath;
        }

        /// <inheritdoc />
        public override string? GetResumeCommand(string sessionId)
        {
            var executable = GetOpenCodeExecutable();
            return $"{executable} --session {sessionId}";
        }

        /// <summary>
        /// Dispose of resources used by the service.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _executionSemaphore?.Dispose();
                _validationSemaphore.Dispose();

                if (_acpClient != null)
                {
                    _acpClient.OnSessionUpdate -= HandleSessionUpdate;
                    _acpClient.Dispose();
                    _acpClient = null;
                }

                _sessionId = null;
            }

            _disposed = true;
            base.Dispose(disposing);
        }

        #region IServiceSettingsUI Implementation

        /// <summary>
        /// Render OpenCode-specific settings UI (simple version for 3P compatibility).
        /// </summary>
        public void DrawSettingsUI()
        {
            UnityEditor.EditorGUILayout.LabelField("OpenCode Settings", UnityEditor.EditorStyles.boldLabel);
            UnityEditor.EditorGUILayout.LabelField("Executable Path", OpenCodeSettings.ExecutablePath.Value);
        }

        /// <summary>
        /// Render OpenCode-specific settings UI (full version with Meta settings infrastructure).
        /// </summary>
        public void DrawSettingsUI(Origins origins, IIdentified originData)
        {
            UnityEditor.EditorGUILayout.LabelField("OpenCode Settings", UnityEditor.EditorStyles.boldLabel);
            OpenCodeSettings.ExecutablePath.DrawForGUI(origins, originData, OnExecutablePathChanged);
        }

        /// <summary>
        /// Reset OpenCode settings to defaults.
        /// </summary>
        public void ResetSettingsToDefaults()
        {
            var launchConfigurationChanged =
                !string.IsNullOrEmpty(OpenCodeSettings.ExecutablePath.Value) ||
                !string.IsNullOrEmpty(AdditionalCommandLineArguments);
            OpenCodeSettings.ExecutablePath.Reset();
            OpenCodeSettings.AdditionalArguments.Reset();
            InvalidateLaunchConfiguration();
            if (launchConfigurationChanged)
            {
                ScheduleAcpClientRestart();
            }
        }

        private void OnExecutablePathChanged()
        {
            InvalidateLaunchConfiguration();
            ScheduleAcpClientRestart();
        }

        private void InvalidateLaunchConfiguration()
        {
            ClearResolvedExecutablePath();
            _currentValidationResult = ValidationResult.Unknown();
            CommandLineArguments.InvalidateValidationResult(ServiceId);
        }

        #endregion

        #region IServiceValidation Implementation

        /// <inheritdoc />
        public Task<ValidationResult> ValidateConfigurationAsync()
        {
            return ValidateConfigurationAsync(false);
        }

        /// <inheritdoc />
        public async Task<ValidationResult> ValidateCommandLineArgumentsAsync()
        {
            var result = await ValidateConfigurationAsync(true);
            return CommandLineArguments.RecordValidationResult(
                ServiceId,
                OpenCodeSettings.ExecutablePath.Value,
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
                var openCodeExecutable = GetOpenCodeExecutable();

                if (!CommandLineArguments.TryParse(
                        AdditionalCommandLineArguments,
                        out var additionalArguments,
                        out var argumentError))
                {
                    _currentValidationResult = ValidationResult.Invalid(argumentError);
                    return _currentValidationResult;
                }

                // Check if the executable exists (if a specific path is provided)
                var configuredPath = OpenCodeSettings.ExecutablePath.Value;
                if (!string.IsNullOrEmpty(configuredPath) && !File.Exists(configuredPath))
                {
                    _currentValidationResult = ValidationResult.Invalid($"Executable not found: {configuredPath}");
                    return _currentValidationResult;
                }

                if (validateCommandLineArguments && additionalArguments.Count > 0)
                {
                    _currentValidationResult = await CommandLineArguments.RunAcpSmokeTestAsync(
                        openCodeExecutable,
                        GetLoginShellPath(),
                        "acp",
                        additionalArguments,
                        GetProjectPath(),
                        "OpenCode");
                    return _currentValidationResult;
                }

                // Try to run "opencode --version" to verify the CLI is available.
                // Routed through ConfigureExecutable so that on Windows, where npm installs OpenCode
                // as a .cmd shim that CreateProcess cannot launch directly, the probe runs via
                // cmd.exe /c instead of throwing a Win32Exception (a false "OpenCode not found").
                var processStartInfo = new ProcessStartInfo
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                ConfigureExecutable(processStartInfo, openCodeExecutable);
                processStartInfo.ArgumentList.Add("--version");

                // Set login shell PATH so the executable and its dependencies are found
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
                        {
                            outputBuilder.Append(output);
                        }
                    }
                    catch
                    {
                        // Ignore read errors
                    }
                });
                var errorTask = Task.Run(() =>
                {
                    try { errorBuilder.Append(process.StandardError.ReadToEnd()); }
                    catch { }
                });

                var waitTask = Task.Run(() =>
                {
                    try
                    {
                        return process.WaitForExit(5000);
                    }
                    catch
                    {
                        return false;
                    }
                });

                var completed = await waitTask;

                if (!completed)
                {
                    CommandLineArguments.KillProcessTree(process);
                    _currentValidationResult = ValidationResult.Error("OpenCode CLI timed out");
                    return _currentValidationResult;
                }

                // Wait a short time for read task to complete after process exits
                await Task.WhenAny(Task.WhenAll(readTask, errorTask), Task.Delay(500));

                if (process.ExitCode == 0)
                {
                    var version = outputBuilder.ToString().Trim();
                    if (string.IsNullOrEmpty(version))
                    {
                        version = "unknown version";
                    }
                    _currentValidationResult = ValidationResult.Valid($"OpenCode CLI available ({version})");
                }
                else
                {
                    _currentValidationResult = ValidationResult.Invalid(CommandLineArguments.GetValidationError(
                        errorBuilder.ToString(),
                        "OpenCode CLI not found or not configured"));
                }
            }
            catch (System.ComponentModel.Win32Exception)
            {
                _currentValidationResult = ValidationResult.Invalid("OpenCode not found. Install OpenCode or set the executable path.");
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
