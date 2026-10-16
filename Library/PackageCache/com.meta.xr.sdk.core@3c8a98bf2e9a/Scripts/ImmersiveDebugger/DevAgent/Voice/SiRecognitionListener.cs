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

#if UNITY_ANDROID && !UNITY_EDITOR
using System;
using System.Threading;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// C# implementation of the Android <c>android.speech.RecognitionListener</c> interface, bridging
    /// System Intelligence on-device ASR callbacks into Unity. Callbacks arrive on the Android main
    /// (Looper) thread; each is marshalled onto the Unity main thread via the captured
    /// <see cref="SynchronizationContext"/> before invoking the C# events.
    /// </summary>
    internal class SiRecognitionListener : AndroidJavaProxy
    {
        // Value of android.speech.SpeechRecognizer.RESULTS_RECOGNITION.
        private const string ResultsRecognitionKey = "results_recognition";

        private readonly SynchronizationContext _unityContext;

        internal event Action OnReady;
        internal event Action OnEndOfSpeech;
        internal event Action<float> OnRmsChanged;
        internal event Action<string> OnPartial;
        internal event Action<string> OnFinal;
        internal event Action<int> OnError;

        internal SiRecognitionListener(SynchronizationContext unityContext)
            : base("android.speech.RecognitionListener")
        {
            _unityContext = unityContext;
        }

        // Dispatch every interface call ourselves so primitive args (int/float), which the Java proxy
        // delivers boxed, are unwrapped explicitly rather than relying on typed-method matching.
        public override AndroidJavaObject Invoke(string methodName, AndroidJavaObject[] javaArgs)
        {
            switch (methodName)
            {
                case "onReadyForSpeech":
                    Post(() => OnReady?.Invoke());
                    break;
                case "onEndOfSpeech":
                    Post(() => OnEndOfSpeech?.Invoke());
                    break;
                case "onRmsChanged":
                    float rms = UnboxFloat(javaArgs);
                    Post(() => OnRmsChanged?.Invoke(rms));
                    break;
                case "onError":
                    int error = UnboxInt(javaArgs);
                    Post(() => OnError?.Invoke(error));
                    break;
                case "onPartialResults":
                    string partial = ExtractTopResult(javaArgs);
                    Post(() => OnPartial?.Invoke(partial));
                    break;
                case "onResults":
                    string final = ExtractTopResult(javaArgs);
                    Post(() => OnFinal?.Invoke(final));
                    break;
                // onBeginningOfSpeech / onBufferReceived / onEvent are unused.
            }

            return null;
        }

        private void Post(Action action)
        {
            if (_unityContext == null)
            {
                // Without a captured context we cannot marshal to the Unity main thread; drop the
                // callback rather than invoke on the Android Looper thread and corrupt Unity state.
                Debug.LogWarning("[SiAsr] No Unity SynchronizationContext captured; dropping recognition callback.");
                return;
            }
            _unityContext.Post(_ => action(), null);
        }

        private static int UnboxInt(AndroidJavaObject[] javaArgs)
        {
            return javaArgs != null && javaArgs.Length > 0 && javaArgs[0] != null
                ? javaArgs[0].Call<int>("intValue")
                : -1;
        }

        private static float UnboxFloat(AndroidJavaObject[] javaArgs)
        {
            return javaArgs != null && javaArgs.Length > 0 && javaArgs[0] != null
                ? javaArgs[0].Call<float>("floatValue")
                : 0f;
        }

        // Reads the highest-confidence hypothesis out of the RESULTS_RECOGNITION string list on the
        // Android thread — the Bundle must not be retained past the callback.
        private static string ExtractTopResult(AndroidJavaObject[] javaArgs)
        {
            if (javaArgs == null || javaArgs.Length == 0 || javaArgs[0] == null)
            {
                return string.Empty;
            }

            using var list = javaArgs[0].Call<AndroidJavaObject>("getStringArrayList", ResultsRecognitionKey);
            if (list == null || list.Call<int>("size") <= 0)
            {
                return string.Empty;
            }

            return list.Call<string>("get", 0);
        }
    }
}
#endif
