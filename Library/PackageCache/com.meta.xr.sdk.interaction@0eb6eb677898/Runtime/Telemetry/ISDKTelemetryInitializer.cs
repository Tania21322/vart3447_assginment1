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
using UnityEngine;

namespace Oculus.Interaction.Telemetry
{
    internal static class ISDKTelemetryInitializer
    {
        private static float _sessionStartTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            try
            {
                ISDKEngineTelemetryNative.Init();
                _sessionStartTime = Time.realtimeSinceStartup;
                Application.quitting += OnApplicationQuitting;
            }
            catch (DllNotFoundException)
            {
                // Only warn on platforms where the native binary is expected to exist.
                // macOS and Linux binaries are not yet shipped — silence is expected there.
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_ANDROID
                Debug.LogWarning("[ISDK Telemetry] ISDKEngineTelemetry native library not found. Telemetry disabled.");
#endif
            }
        }

        private static void OnApplicationQuitting()
        {
            try
            {
                var duration = Time.realtimeSinceStartup - _sessionStartTime;
                ISDKTelemetryRuntimeEvents.SendSessionDuration(duration);
                ISDKEngineTelemetryNative.ShutDown();
            }
            catch (Exception)
            {
                // Suppress errors during quit — process may already be tearing down
            }
        }
    }
}
