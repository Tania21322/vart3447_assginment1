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

using Meta.XR.AI.AgentBridge;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Integration MonoBehaviour that connects <see cref="RemoteAgentBridgeClient"/>
    /// with the <see cref="ConversationManager"/>. Routes voice input and conversation
    /// events through AgentBridge's HTTP remote client.
    ///
    /// Initialized by <see cref="DevAgentController"/> which provides the
    /// <see cref="ConversationManager"/> reference and optional injected client.
    /// </summary>
    internal class AgentBridgeIntegration : MonoBehaviour
    {
        // Unique per session instance so a new session starts a fresh server-side
        // conversation; stable across OnApplicationPause so resume keeps the conversation.
        private readonly string _callerId =
            "ImmersiveDebugger.DevAgent." + System.Guid.NewGuid().ToString("N");

        private const string DefaultSystemPrompt =
            "You are a Unity VR debugging assistant running inside the user's app on a Meta Quest " +
            "headset. You inspect, debug, visualize, and modify the user's live Unity scene. Prefer the " +
            "Meta XR Operator runtime tools for interacting with the running scene; other tools the user " +
            "has connected may also be used when they fit the request better.\n\n" +
            "Discovering tools: at first only a few CORE tools plus the meta-tool 'unity_load_tools' are " +
            "registered. Every other tool lives in a group you must load before you can call it. So when " +
            "a request needs a capability you do not yet have a tool for, do not say it is impossible - " +
            "instead:\n" +
            "1. Call 'unity_load_tools' with action 'list' to see all groups, what each does, and which " +
            "are loaded.\n" +
            "2. Call 'unity_load_tools' with action 'load' and the group id to register that group's tools.\n" +
            "3. Call the now-available tool. Treat the 'list' output as the source of truth for what exists.\n\n" +
            "Match the user's intent to a group id (verify against the list; ids may change):\n" +
            "- Find, inspect, or read a GameObject, component value, scene hierarchy, or health - CORE " +
            "(already loaded).\n" +
            "- 'Show me / where is / locate / highlight' an object - load 'visualization' and draw a " +
            "bounding box; prefer a visible highlight over only stating coordinates.\n" +
            "- Enable, show, or hide the Immersive Debugger panel, inspector, console, gizmos, opacity, " +
            "or head-follow - load 'debugger_ui'.\n" +
            "- Change a value, toggle a GameObject active, add or remove a component, or invoke a method " +
            "- load 'scene_mutation'.\n" +
            "- Read logs, errors, or warnings, or write a debug log - load 'diagnostics'.\n" +
            "- Find objects by component type, or find [DebugMember]-annotated members - load 'discovery'.\n\n" +
            "Prefer taking the action (load the right group, then call the tool) over describing what you " +
            "would do. When the user asks to see, show, find, or locate something in the scene, give " +
            "visual feedback - a bounding box or the debugger panel - alongside any text answer. Load a " +
            "group only when you need it; do not preload everything.";

        private const string AdbReverseServerAddress = "127.0.0.1";
        private static readonly TimeSpan PreferredConnectionTimeout = TimeSpan.FromSeconds(1.5);
        private static readonly TimeSpan FallbackConnectionTimeout = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan ConnectRetryBaseDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan ConnectRetryMaxDelay = TimeSpan.FromSeconds(30);

        private ConversationManager _manager;
        internal ConversationManager ConversationManager => _manager;

        private IRemoteAgentBridgeClient _client;
        internal IRemoteAgentBridgeClient Client => _client;

        private IDictationSource _dictationSource;
        private PushToTalkController _pushToTalkController;
        private bool _isLiveTranscriptionActive;
#if HAS_META_VOICE_SDK
        private VoiceSetupController _voiceSetupController;
#endif

        // Thinking stream state - tracks current streaming entry by MessageId
        private ConversationEntry _currentThinkingEntry;
        private string _currentThinkingMessageId = "";
        private string _accumulatedThinkingContent = "";

        // When true, the next reconnect or prompt must send a clear before proceeding.
        // Set when the user clears the conversation while the client is disconnected.
        internal bool _pendingClear;

        private CancellationTokenSource _autoConnectCts;

        #region Initialization

        /// <summary>
        /// Initialize the integration with a ConversationManager and optional client.
        /// Called by <see cref="DevAgentController"/> during its Awake().
        /// </summary>
        internal void Initialize(ConversationManager manager, IRemoteAgentBridgeClient client = null)
        {
            _manager = manager;
            _client = client;
        }

        /// <summary>
        /// Set the client for testing. Must be called before Start() runs.
        /// </summary>
        internal void SetClient(IRemoteAgentBridgeClient client)
        {
            _client = client;
        }

        #endregion

        #region Lifecycle

        private void Awake()
        {
            // Voice backend selection. Default is on-device System Intelligence ASR (Quest 3/3S);
            // the Voice SDK backend is used only when the user opts into it in settings. Either way
            // the panel still works via keyboard text input when no voice backend is available.
            var voiceSourceCreated = false;

#if HAS_META_VOICE_SDK
            // Cache the singleton once — reading RuntimeSettings.Instance in both the condition and
            // the body would risk a NullReferenceException if it were nulled between the two reads.
            var settings = RuntimeSettings.Instance;
            if (settings != null && settings.UseVoiceSdkForInput)
            {
                var witConfig = settings.WitConfiguration as Meta.WitAi.Data.Configuration.WitConfiguration;
                var clientToken = settings.WitClientAccessToken;
                _voiceSetupController = VoiceSetupController.CreateVoiceSetupAsChild(gameObject, witConfig, clientToken);
                if (_voiceSetupController != null)
                {
                    _voiceSetupController.OnDictationControllerReady += BindDictationSource;
                    voiceSourceCreated = true;
                }
            }
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!voiceSourceCreated && SiAsr.IsAvailable())
            {
                BindDictationSource(SiDictationController.CreateAsChild(gameObject));
                voiceSourceCreated = true;
            }
#endif

            if (!voiceSourceCreated)
            {
                Debug.Log("[AgentBridgeIntegration] No voice backend active; " +
                    "use keyboard text input to talk to the assistant.");
            }
        }

        private async void Start()
        {
            // Subscribe to conversation manager events
            if (_manager != null)
            {
                _manager.OnConversationHistoryCleared += OnConversationHistoryCleared;
                _manager.OnConversationCancelled += OnConversationCancelled;
            }

            SetupPushToTalk();

            // Create and connect the AgentBridge remote client (if not already injected)
            if (_client == null)
            {
                // Only attempt auto-connection when the AI Assistant is enabled in settings
                var settings = RuntimeSettings.Instance;
                if (settings == null || !settings.Enabled)
                {
                    return;
                }

                // Set initial status
                _manager?.SetConnectionStatus(ConversationManager.ConnectionStatus.Disconnected);

                await ConnectWithRetryAsync(settings);

                return;
            }

            SubscribeToClient(_client);

            // Set initial status
            _manager?.SetConnectionStatus(ConversationManager.ConnectionStatus.Disconnected);

            var connected = await _client.ConnectAsync();
            if (!connected)
            {
                Debug.LogWarning("[AgentBridgeIntegration] Could not connect to Remote Agent Server. " +
                                 $"Ensure the server is running at {_client.ServerUrl}:{_client.Port}");
            }
        }

        private void OnDestroy()
        {
            _autoConnectCts?.Cancel();
            _autoConnectCts?.Dispose();
            _autoConnectCts = null;

            if (_client != null)
            {
                UnsubscribeFromClient(_client);
                _client.Dispose();
                _client = null;
            }

            if (_pushToTalkController != null)
            {
                _pushToTalkController.OnButtonPressed -= OnInputButtonPressed;
                _pushToTalkController.OnButtonReleased -= OnInputButtonReleased;
            }

            if (_manager != null)
            {
                _manager.OnConversationHistoryCleared -= OnConversationHistoryCleared;
                _manager.OnConversationCancelled -= OnConversationCancelled;
            }

            if (_dictationSource != null)
            {
                _dictationSource.OnPartialTranscriptionUpdate -= OnPartialTranscriptionUpdate;
                _dictationSource.OnTranscriptionFinalized -= OnTranscriptionFinalized;
                _dictationSource.OnDictationError -= HandleDictationError;
            }

#if HAS_META_VOICE_SDK
            if (_voiceSetupController != null)
            {
                _voiceSetupController.OnDictationControllerReady -= BindDictationSource;
            }
#endif
        }

        internal void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                // Headset doffed mid-dictation — abandon the session so it doesn't stay stuck active
                // and a stale utterance isn't sent on resume. Done regardless of client state.
                CancelActiveDictation();
            }

            if (_client == null) return;

            if (pauseStatus)
            {
                // Headset doffed — disconnect to release resources and stop SSE stream
                Debug.Log("[AgentBridgeIntegration] Application paused, disconnecting from server");
                _client.Disconnect();
            }
            else
            {
                // Headset donned — reconnect to server
                Debug.Log("[AgentBridgeIntegration] Application resumed, reconnecting to server");
                _ = ReconnectAsync();
            }
        }

        private async System.Threading.Tasks.Task ReconnectAsync()
        {
            if (_client == null || _client.IsConnected) return;

            var connected = await _client.ConnectAsync();
            if (!connected)
            {
                Debug.LogWarning("[AgentBridgeIntegration] Failed to reconnect after resume");
            }
        }

        #endregion

        #region Client Connection

        private struct ConnectionEndpoint
        {
            public string Address;
            public int Port;
            public string Name;
            public bool Preferred;
        }

        /// <summary>
        /// Connect to the Remote Agent Server, retrying with exponential backoff until a connection
        /// succeeds or this component is destroyed. Each round re-probes every endpoint, so a tunnel
        /// or server that only comes up after the app started is still picked up.
        /// </summary>
        internal async Task ConnectWithRetryAsync(RuntimeSettings settings)
        {
            _autoConnectCts?.Cancel();
            _autoConnectCts?.Dispose();
            _autoConnectCts = new CancellationTokenSource();
            var cancellationToken = _autoConnectCts.Token;

            for (var attempt = 1; !cancellationToken.IsCancellationRequested; attempt++)
            {
                if (await ConnectWithFallbackAsync(settings))
                {
                    return;
                }

                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (attempt == 1)
                {
                    Debug.LogWarning("[AgentBridgeIntegration] Could not connect to Remote Agent Server. " +
                                     $"Tried {DescribeConnectionEndpoints(settings)}. Retrying in the background.");
                }

                var delaySeconds = Math.Min(
                    ConnectRetryBaseDelay.TotalSeconds * Math.Pow(2, attempt - 1),
                    ConnectRetryMaxDelay.TotalSeconds);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
        }

        private async Task<bool> ConnectWithFallbackAsync(RuntimeSettings settings)
        {
            foreach (var endpoint in GetConnectionEndpoints(settings))
            {
                var client = new RemoteAgentBridgeClient(endpoint.Address, endpoint.Port);
                if (!string.IsNullOrEmpty(settings.AccessToken))
                {
                    client.AccessToken = settings.AccessToken;
                }

                _client = client;
                SubscribeToClient(client);

                Debug.Log($"[AgentBridgeIntegration] Connecting to Remote Agent Server via {endpoint.Name} " +
                          $"at {endpoint.Address}:{endpoint.Port}");

                var connected = false;
                try
                {
                    using var timeout = new CancellationTokenSource(
                        endpoint.Preferred ? PreferredConnectionTimeout : FallbackConnectionTimeout);
                    connected = await client.ConnectAsync(timeout.Token);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[AgentBridgeIntegration] Connection attempt failed via {endpoint.Name}: {ex.Message}");
                }

                if (connected)
                {
                    return true;
                }

                // If OnDestroy tore this client down during the await, it is already unsubscribed and
                // disposed — don't touch it again (double-dispose) and stop connecting.
                if (!ReferenceEquals(_client, client))
                {
                    return false;
                }

                UnsubscribeFromClient(client);
                client.Dispose();
                _client = null;
            }

            return false;
        }

        private static List<ConnectionEndpoint> GetConnectionEndpoints(RuntimeSettings settings)
        {
            var endpoints = new List<ConnectionEndpoint>();
            if (settings == null || settings.ServerPort <= 0)
            {
                return endpoints;
            }

            endpoints.Add(new ConnectionEndpoint
            {
                Address = AdbReverseServerAddress,
                Port = settings.ServerPort,
                Name = "ADB reverse",
                Preferred = true
            });

            var serverAddress = (settings.ServerAddress ?? string.Empty).Trim();
            if (!string.IsNullOrEmpty(serverAddress) && !IsSameEndpoint(serverAddress, settings.ServerPort, endpoints))
            {
                endpoints.Add(new ConnectionEndpoint
                {
                    Address = serverAddress,
                    Port = settings.ServerPort,
                    Name = "network fallback",
                    Preferred = false
                });
            }

            return endpoints;
        }

        private static bool IsSameEndpoint(string address, int port, List<ConnectionEndpoint> endpoints)
        {
            foreach (var endpoint in endpoints)
            {
                if (endpoint.Port == port && string.Equals(endpoint.Address, address, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string DescribeConnectionEndpoints(RuntimeSettings settings)
        {
            var endpoints = GetConnectionEndpoints(settings);
            if (endpoints.Count == 0)
            {
                return "no valid connection endpoints";
            }

            var descriptions = new List<string>();
            foreach (var endpoint in endpoints)
            {
                descriptions.Add($"{endpoint.Name} {endpoint.Address}:{endpoint.Port}");
            }

            return string.Join(", ", descriptions);
        }

        private void SubscribeToClient(IRemoteAgentBridgeClient client)
        {
            client.OnMessageReceived += OnMessageReceived;
            client.OnProcessingStateChanged += OnProcessingStateChanged;
            client.OnConversationCleared += OnRemoteConversationCleared;
            client.OnConnectionStateChanged += OnConnectionStateChanged;
            client.OnErrorReceived += OnErrorReceived;
        }

        private void UnsubscribeFromClient(IRemoteAgentBridgeClient client)
        {
            client.OnMessageReceived -= OnMessageReceived;
            client.OnProcessingStateChanged -= OnProcessingStateChanged;
            client.OnConversationCleared -= OnRemoteConversationCleared;
            client.OnConnectionStateChanged -= OnConnectionStateChanged;
            client.OnErrorReceived -= OnErrorReceived;
        }

        #endregion

        #region Connection State

        private async void OnConnectionStateChanged(bool connected)
        {
            if (_manager == null) return;

            var status = connected
                ? ConversationManager.ConnectionStatus.Connected
                : ConversationManager.ConnectionStatus.Disconnected;

            _manager.SetConnectionStatus(status);

            if (connected && _pendingClear)
            {
                try
                {
                    await SendClearAsync();
                }
                catch (Exception ex)
                {
                    _pendingClear = true;
                    Debug.LogError($"[AgentBridgeIntegration] Failed to replay pending clear on reconnect: {ex.Message}");
                }
            }

            if (!connected && _manager.IsConversationActive)
            {
                FinalizeThinkingStream();
                _manager.AddSystemMessage("[Connection lost]");
                _manager.StopProcessing();
            }
        }

        private void OnErrorReceived(RemoteSseError error)
        {
            if (_manager == null) return;

            Debug.LogError($"[AgentBridgeIntegration] Server error: [{error.Code}] {error.Message}");
            FinalizeThinkingStream();
            _manager.AddSystemMessage($"[Error: {error.Message}]");
            _manager.StopProcessing();
        }

        #endregion

        #region Message Handling (from RemoteAgentBridgeClient)

        private void OnMessageReceived(ConversationMessage message)
        {
            if (_manager == null) return;

            switch (message.MessageType?.ToLower())
            {
                case "thinking":
                    HandleThinkingMessage(message);
                    break;
                case "tool_use":
                    FinalizeThinkingStream();
                    _manager.AddToolCallMessage(message.Content);
                    break;
                case "tool_result":
                    _manager.UpdateLastToolStatus(message.Content);
                    break;
                case "assistant":
                    if (message.IsStreaming)
                    {
                        HandleThinkingMessage(message);
                    }
                    else
                    {
                        // Only add a new message if there was no active stream to finalize.
                        // If we just finalized a stream, it already contains the accumulated content.
                        if (!FinalizeThinkingStream())
                        {
                            _manager.AddAssistantMessage(message.Content);
                        }
                    }
                    break;
                case "user":
                    // Ignore user messages from the server - they were already added locally
                    // before sending the prompt. Adding them again would cause duplicates.
                    break;
                default:
                    _manager.AddAssistantMessage(message.Content);
                    break;
            }
        }

        private void OnProcessingStateChanged(bool isProcessing)
        {
            if (isProcessing)
            {
                _manager?.StartProcessing();
            }
            else
            {
                FinalizeThinkingStream();
                _manager?.StopProcessing();
            }
        }

        private void OnRemoteConversationCleared()
        {
            // Server-side clear acknowledged — no additional action needed
        }

        /// <summary>
        /// Handle streaming thinking messages by properly tracking MessageId.
        /// When IsDelta is true, content is appended to accumulated content.
        /// When IsDelta is false, content replaces the accumulated content.
        /// </summary>
        private void HandleThinkingMessage(ConversationMessage message)
        {
            try
            {
                var messageId = message.MessageId ?? "";
                var content = message.Content ?? "";

                // Check if this is a new message stream (different MessageId)
                if (_currentThinkingEntry == null || _currentThinkingMessageId != messageId)
                {
                    // Finalize any existing stream first
                    FinalizeThinkingStream();

                    // Start a new streaming entry
                    _currentThinkingEntry = _manager.AddLiveTranscriptionEntry(ConversationEntry.MessageType.Assistant);
                    _currentThinkingMessageId = messageId;
                    _accumulatedThinkingContent = "";
                }

                // Update content based on IsDelta flag
                if (message.IsDelta)
                {
                    // Delta mode: APPEND content (incremental chunks)
                    _accumulatedThinkingContent += content;
                }
                else
                {
                    // Full text mode: REPLACE content (server sends full accumulated text)
                    _accumulatedThinkingContent = content;
                }

                _manager.UpdateLiveTranscriptionEntry(_accumulatedThinkingContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentBridgeIntegration] Error handling thinking message: {ex.Message}");
            }
        }

        private bool FinalizeThinkingStream()
        {
            if (_currentThinkingEntry != null)
            {
                _manager?.FinalizeLiveTranscriptionEntry(_accumulatedThinkingContent);
                _currentThinkingEntry = null;
                _currentThinkingMessageId = "";
                _accumulatedThinkingContent = "";
                return true;
            }
            return false;
        }

        #endregion

        #region Push-to-Talk / Voice

        private void SetupPushToTalk()
        {
            _pushToTalkController = FindFirstObjectByType<PushToTalkController>();
            if (_pushToTalkController == null)
            {
                _pushToTalkController = gameObject.AddComponent<PushToTalkController>();
            }

            var settings = RuntimeSettings.Instance;
            if (settings != null)
            {
                _pushToTalkController.InputButton = settings.PushToTalkButton;
                _pushToTalkController.HandGesture = settings.HandPushToTalkGesture;
            }

            _pushToTalkController.OnButtonPressed += OnInputButtonPressed;
            _pushToTalkController.OnButtonReleased += OnInputButtonReleased;
        }

        private void BindDictationSource(IDictationSource dictationController)
        {
            _dictationSource = dictationController;
            _dictationSource.OnPartialTranscriptionUpdate += OnPartialTranscriptionUpdate;
            _dictationSource.OnTranscriptionFinalized += OnTranscriptionFinalized;
            _dictationSource.OnDictationError += HandleDictationError;
        }

        private void OnInputButtonPressed()
        {
            // Don't start a live entry or flip the active flag until dictation can actually run,
            // otherwise an orphan entry is left behind and the flag stays stuck, no-op'ing future presses.
            if (_dictationSource == null)
            {
                Debug.LogWarning("[AgentBridgeIntegration] Cannot start dictation - DictationController is not ready yet.");
                return;
            }

            // The live-transcription slot is shared with the assistant's streaming reply, so starting
            // dictation while a response is in flight would overwrite that bubble. Ignore the press.
            if (_manager != null && _manager.IsConversationActive)
            {
                Debug.Log("[AgentBridgeIntegration] Ignoring push-to-talk while a response is in progress.");
                return;
            }

            if (_manager != null && !_isLiveTranscriptionActive)
            {
                _manager.AddLiveTranscriptionEntry();
            }
            // Track the push-to-talk session whenever dictation actually starts — even if _manager is
            // momentarily null and no live entry was added — so OnInputButtonReleased stays symmetric and
            // always stops a session a press began, never leaving dictation stuck active.
            _isLiveTranscriptionActive = true;

            _dictationSource.Toggle(true);

            _manager?.SetVoiceStatus(ConversationManager.VoiceStatus.Listening);
        }

        private void OnInputButtonReleased()
        {
            // If the press never started a dictation — it was ignored because a response was already in
            // progress, or the controller wasn't ready — there's nothing to finalize. Leave the voice
            // status untouched so it stays "Waiting" rather than falsely flipping to "Processing".
            if (!_isLiveTranscriptionActive)
            {
                return;
            }

            _manager?.SetVoiceStatus(ConversationManager.VoiceStatus.Processing);

            if (_dictationSource != null)
            {
                _dictationSource.Toggle(false);
            }
            else
            {
                Debug.LogWarning("[AgentBridgeIntegration] Cannot stop dictation - DictationController is not ready yet.");
            }
        }

        private void OnPartialTranscriptionUpdate(string partialText)
        {
            if (_manager != null && _isLiveTranscriptionActive)
            {
                _manager.UpdateLiveTranscriptionEntry(partialText);
            }
        }

        private void OnTranscriptionFinalized(string finalText)
        {
            if (_manager == null || !_isLiveTranscriptionActive) return;

            _manager.FinalizeLiveTranscriptionEntry(finalText);
            _isLiveTranscriptionActive = false;

            if (!string.IsNullOrEmpty(finalText) && finalText.Trim().Length > 0)
            {
                ProcessTranscription(finalText);
            }
            else
            {
                _manager.SetVoiceStatus(ConversationManager.VoiceStatus.Waiting);
            }
        }

        /// <summary>
        /// Abandon any in-progress dictation and drop its live-transcription entry. Used when the
        /// conversation is cleared or the headset is doffed mid-dictation so the voice state does not
        /// stay stuck active and an orphaned utterance is not sent.
        /// </summary>
        private void CancelActiveDictation()
        {
            _dictationSource?.Cancel();

            if (_isLiveTranscriptionActive)
            {
                _isLiveTranscriptionActive = false;
                _manager?.RemoveLiveTranscriptionEntry();
                _manager?.SetVoiceStatus(ConversationManager.VoiceStatus.Waiting);
            }
        }

        /// <summary>
        /// The dictation request failed (auth/quota/network). Drop the empty live entry, flag the
        /// voice status as Error (red pill), and surface the reason in the conversation so the failure
        /// isn't silent.
        /// </summary>
        private void HandleDictationError(string message)
        {
            if (_manager == null) return;

            if (_isLiveTranscriptionActive)
            {
                _isLiveTranscriptionActive = false;
                _manager.RemoveLiveTranscriptionEntry();
            }

            _manager.SetVoiceStatus(ConversationManager.VoiceStatus.Error);
            _manager.AddSystemMessage($"[Voice error: {message}]");
            Debug.LogError($"[AgentBridgeIntegration] Dictation failed: {message}");
        }

        #endregion

        #region Send to AgentBridge

        /// <summary>
        /// Sends a typed text message to AgentBridge, bypassing voice/PTT. Used by the panel's
        /// keyboard text-input path (and by Editor testing).
        /// </summary>
        /// <param name="message">The text message to send</param>
        internal void SendTextMessage(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                Debug.LogWarning("[AgentBridgeIntegration] Cannot send empty message");
                return;
            }

            _manager?.AddUserMessage(message);
            ProcessTranscription(message);
        }

        internal async void ProcessTranscription(string transcription)
        {
            try
            {
                if (string.IsNullOrEmpty(transcription))
                {
                    Debug.LogWarning("[AgentBridgeIntegration] Cannot process empty transcription");
                    _manager?.StopProcessing();
                    return;
                }

                if (_client == null || !_client.IsConnected)
                {
                    Debug.LogError("[AgentBridgeIntegration] Not connected to Remote Agent Server");
                    _manager?.AddSystemMessage("[Error: Not connected to AgentBridge server]");
                    _manager?.StopProcessing();
                    return;
                }

                if (_pendingClear)
                {
                    // Don't send the new prompt against stale server-side history if the clear failed.
                    var cleared = await SendClearAsync();
                    if (!cleared)
                    {
                        Debug.LogError("[AgentBridgeIntegration] Aborting prompt because the pending conversation clear failed.");
                        _manager?.AddSystemMessage("[Error: Could not clear the previous conversation]");
                        _manager?.StopProcessing();
                        return;
                    }
                }

                var request = new RemotePromptRequest
                {
                    Prompt = transcription,
                    CallerId = _callerId,
                    SystemPrompt = DefaultSystemPrompt
                };

                var (success, error) = await _client.SendPromptAsync(request);
                if (!success)
                {
                    Debug.LogError($"[AgentBridgeIntegration] Failed to send prompt: {error}");
                    _manager?.AddSystemMessage($"[Error: {error}]");
                    _manager?.StopProcessing();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentBridgeIntegration] Error in ProcessTranscription: {ex.Message}");
                _manager?.AddSystemMessage($"[Error: {ex.Message}]");
                _manager?.StopProcessing();
            }
            finally
            {
                _manager?.SetVoiceStatus(ConversationManager.VoiceStatus.Waiting);
            }
        }

        private async void OnConversationHistoryCleared()
        {
            try
            {
                // The cleared conversation removed the live-transcription entry; abandon any active
                // dictation so it doesn't keep the voice state stuck or send a now-orphaned utterance.
                CancelActiveDictation();

                if (_client == null || !_client.IsConnected)
                {
                    _pendingClear = true;
                    Debug.LogWarning("[AgentBridgeIntegration] Not connected; clear will be sent on reconnect");
                    return;
                }

                await SendClearAsync();
            }
            catch (Exception ex)
            {
                _pendingClear = true;
                Debug.LogError($"[AgentBridgeIntegration] Failed to clear conversation: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task<bool> SendClearAsync()
        {
            var request = new RemoteCallerRequest { CallerId = _callerId };
            var (success, error) = await _client.ClearConversationAsync(request);
            if (success)
            {
                _pendingClear = false;
            }
            else
            {
                _pendingClear = true;
                Debug.LogError($"[AgentBridgeIntegration] Failed to clear conversation: {error}");
            }
            return success;
        }

        internal async void OnConversationCancelled()
        {
            try
            {
                if (_client == null || !_client.IsConnected)
                {
                    Debug.LogError("[AgentBridgeIntegration] Not connected to Remote Agent Server");
                    return;
                }

                Debug.Log("[AgentBridgeIntegration] Sending cancellation request");
                var request = new RemoteCallerRequest { CallerId = _callerId };
                var (success, error) = await _client.CancelAsync(request);

                _manager?.AddSystemMessage(
                    success ? "[Interrupted by user]" : $"[Error: Failed to cancel - {error}]");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[AgentBridgeIntegration] Failed to send cancellation: {ex.Message}");
                _manager?.AddSystemMessage($"[Error: Cancellation failed - {ex.Message}]");
            }
        }

        #endregion
    }
}
