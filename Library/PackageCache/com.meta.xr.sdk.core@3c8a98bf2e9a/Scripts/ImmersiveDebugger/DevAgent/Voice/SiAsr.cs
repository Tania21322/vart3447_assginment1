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
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace Meta.XR.ImmersiveDebugger.DevAgent
{
    /// <summary>
    /// Thin bridge over the standard Android <c>SpeechRecognizer</c> API pinned to the on-device
    /// System Intelligence recognizer (<c>com.oculus.systemintelligence</c>). All recognizer
    /// lifecycle calls must run on the Android main (UI) thread — use <see cref="RunOnUiThread"/>.
    /// Availability is device- and OS-gated: it is only present on Quest 3 / 3S, which is the signal
    /// used to fall back to the Voice SDK path on other headsets.
    /// </summary>
    internal static class SiAsr
    {
        internal const string SiPackage = "com.oculus.systemintelligence";
        internal const string SiRecognitionServiceClass =
            "com.oculus.systemintelligence.android.speech.OnDeviceRecognitionService";

        /// <summary>
        /// Whether on-device speech recognition is available on this headset. False on Quest 2 /
        /// Quest Pro (no HTP) and in the Editor.
        /// </summary>
        internal static bool IsAvailable()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using var activity = GetActivity();
                if (activity == null)
                {
                    return false;
                }

                using var recognizerClass = new AndroidJavaClass("android.speech.SpeechRecognizer");
                return recognizerClass.CallStatic<bool>("isOnDeviceRecognitionAvailable", activity);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SiAsr] Availability check failed: {e.Message}");
                return false;
            }
#else
            return false;
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        // Android platform constants (values, so no reflection into RecognizerIntent is needed).
        private const string ActionRecognizeSpeech = "android.speech.action.RECOGNIZE_SPEECH";
        private const string ExtraLanguageModel = "android.speech.extra.LANGUAGE_MODEL";
        private const string LanguageModelFreeForm = "free_form";
        private const string ExtraPartialResults = "android.speech.extra.PARTIAL_RESULTS";
        private const string ExtraPreferOffline = "android.speech.extra.PREFER_OFFLINE";
        private const string ExtraLanguage = "android.speech.extra.LANGUAGE";

        internal static AndroidJavaObject GetActivity()
        {
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            return player.GetStatic<AndroidJavaObject>("currentActivity");
        }

        /// <summary>Posts <paramref name="action"/> onto the Android main (UI) thread.</summary>
        internal static void RunOnUiThread(AndroidJavaObject activity, Action action)
        {
            activity.Call("runOnUiThread", new AndroidJavaRunnable(action));
        }

        /// <summary>
        /// Creates a SpeechRecognizer pinned to the System Intelligence on-device service. Must be
        /// called on the Android UI thread. Uses the ComponentName overload (API 31+); the two-arg
        /// createOnDeviceSpeechRecognizer is API 35+ and unavailable on Quest 3S (API 34).
        /// </summary>
        internal static AndroidJavaObject CreateSiRecognizer(AndroidJavaObject context)
        {
            using var recognizerClass = new AndroidJavaClass("android.speech.SpeechRecognizer");
            using var componentName =
                new AndroidJavaObject("android.content.ComponentName", SiPackage, SiRecognitionServiceClass);
            return recognizerClass.CallStatic<AndroidJavaObject>("createSpeechRecognizer", context, componentName);
        }

        /// <summary>
        /// Builds a free-form, offline-preferred recognition intent that captures from the device mic.
        /// </summary>
        internal static AndroidJavaObject BuildMicIntent(string language, bool partialResults)
        {
            var intent = new AndroidJavaObject("android.content.Intent", ActionRecognizeSpeech);
            PutStringExtra(intent, ExtraLanguageModel, LanguageModelFreeForm);
            PutBoolExtra(intent, ExtraPartialResults, partialResults);
            PutBoolExtra(intent, ExtraPreferOffline, true);
            if (!string.IsNullOrEmpty(language))
            {
                PutStringExtra(intent, ExtraLanguage, language);
            }

            return intent;
        }

        internal static bool HasMicPermission()
        {
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
        }

        internal static void RequestMicPermission()
        {
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
        }

        // putExtra returns the Intent for chaining; the generic Call is required so JNI resolves the
        // correct (non-void) signature. Dispose the returned self-reference.
        private static void PutStringExtra(AndroidJavaObject intent, string key, string value)
        {
            using var _ = intent.Call<AndroidJavaObject>("putExtra", key, value);
        }

        private static void PutBoolExtra(AndroidJavaObject intent, string key, bool value)
        {
            using var _ = intent.Call<AndroidJavaObject>("putExtra", key, value);
        }
#endif
    }
}
