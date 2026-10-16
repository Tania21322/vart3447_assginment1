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

using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Hand pinch gesture for push-to-talk when using hand tracking.
    /// Maps to <see cref="OVRPlugin.HandFingerPinch"/> flags.
    /// </summary>
    internal enum HandPinchGesture
    {
        [InspectorName("Middle Finger Pinch")] MiddleFingerPinch,
        [InspectorName("Ring Finger Pinch")] RingFingerPinch,
        [InspectorName("Pinky Finger Pinch")] PinkyFingerPinch,
        [InspectorName("Index Finger Pinch (same as UI click — not recommended)")] IndexFingerPinch,
    }

    /// <summary>
    /// Runtime configuration settings for ImmersiveDebugger.DevAgent.
    /// Manages connection settings for the AgentBridge remote server and push-to-talk configuration.
    /// </summary>
    internal class RuntimeSettings : OVRRuntimeAssetsBase
    {
        private const string InstanceAssetName = "DevAgentSettings";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            _instance = null;
        }

        private static RuntimeSettings _instance;
        internal static RuntimeSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    LoadAsset(out RuntimeSettings settings, InstanceAssetName);
                    _instance = settings;
                }

                return _instance;
            }
        }

        [Header("Enable")]
        [Tooltip("Enable the AI Assistant panel in Immersive Debugger.")]
        [SerializeField] private bool enabled = false;

        internal bool Enabled
        {
            get => enabled;
            set => enabled = value;
        }

        [Header("AgentBridge Remote Server")]
        [Tooltip("Fallback IP address of the Unity Editor running AgentBridge. Runtime clients try ADB reverse first.")]
        [SerializeField] private string serverAddress = "";
        internal string ServerAddress
        {
            get => serverAddress;
            set => serverAddress = value;
        }

        [Tooltip("Port number for the AgentBridge Remote Server.")]
        [SerializeField] private int serverPort = 48735;
        internal int ServerPort
        {
            get => serverPort;
            set => serverPort = value;
        }

        internal bool HasValidConnectionSettings => serverPort > 0;

        [Header("Voice Input")]
        [Tooltip("Use the Meta Voice SDK (Wit.ai) for voice input instead of on-device System speech " +
            "recognition. Off by default (System speech is used). Requires the com.meta.xr.sdk.voice package.")]
        [SerializeField] private bool useVoiceSdkForInput = false;
        internal bool UseVoiceSdkForInput
        {
            get => useVoiceSdkForInput;
            set => useVoiceSdkForInput = value;
        }

        [Tooltip("Optional: Assign your own WitConfiguration asset (Assets > Create > Wit > Configuration) " +
            "for higher rate limits and custom intents/entities. If left empty, a built-in demo Wit.ai app " +
            "is used automatically — it supports basic speech-to-text but has limited request quotas and " +
            "no custom voice commands. Create a free app at https://wit.ai for more heavy usage.")]
        [SerializeField] private ScriptableObject witConfiguration;
        internal ScriptableObject WitConfiguration
        {
            get => witConfiguration;
            set => witConfiguration = value;
        }

        [Tooltip("Optional: paste a Wit.ai Client Access Token (from your app's Settings at https://wit.ai) to " +
            "use your own Wit app for dictation instead of the shared demo token — higher request quota. " +
            "Ignored if a WitConfiguration asset is assigned above.")]
        [SerializeField] private string witClientAccessToken = "";
        internal string WitClientAccessToken
        {
            get => witClientAccessToken;
            set => witClientAccessToken = value;
        }

        [Tooltip("Controller button used for push-to-talk voice input (when using controllers).")]
        [SerializeField] private OVRInput.Button pushToTalkButton = OVRInput.Button.One;
        internal OVRInput.Button PushToTalkButton
        {
            get => pushToTalkButton;
            set => pushToTalkButton = value;
        }

        [Tooltip("Hand pinch gesture used for push-to-talk voice input (when using hand tracking). " +
            "Middle Finger Pinch is recommended to avoid conflict with the index pinch used for UI interaction.")]
        [SerializeField] private HandPinchGesture handPushToTalkGesture = HandPinchGesture.MiddleFingerPinch;
        internal HandPinchGesture HandPushToTalkGesture
        {
            get => handPushToTalkGesture;
            set => handPushToTalkGesture = value;
        }

        [Tooltip("Enable delayed release for better dictation timing.")]
        [SerializeField] private bool enableDelayedRelease = true;
        internal bool EnableDelayedRelease
        {
            get => enableDelayedRelease;
            set => enableDelayedRelease = value;
        }

        [Tooltip("Delay in seconds before releasing push-to-talk.")]
        [SerializeField] private float releaseDelay = 0.8f;
        internal float ReleaseDelay
        {
            get => releaseDelay;
            set => releaseDelay = value;
        }

        [Header("AgentBridge Authentication")]
        [Tooltip("Access token for connecting to the Remote Agent Server.")]
        [SerializeField] private string accessToken = "";
        internal string AccessToken => accessToken;

        /// <summary>
        /// Sets the access token.
        /// </summary>
        internal void SetAccessToken(string token)
        {
            accessToken = token;
        }
    }
}
