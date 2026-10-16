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

#if !HAS_META_XR_SDK_CORE

using UnityEditor;
using UnityEngine;
using Oculus.Interaction.Telemetry;

namespace Oculus.Interaction.Editor.Telemetry
{
    [InitializeOnLoad]
    internal static class ISDKTelemetryPopupDisplayer
    {
        private static bool _hasChecked;

        static ISDKTelemetryPopupDisplayer()
        {
            EditorApplication.update += OnEditorUpdate;
        }

        private static void OnEditorUpdate()
        {
            if (_hasChecked)
            {
                return;
            }

            _hasChecked = true;
            EditorApplication.update -= OnEditorUpdate;

            if (Application.isBatchMode)
            {
                return;
            }

            try
            {
                // Path 1: no consent on this machine yet, show consent popup.
                if (ISDKEngineTelemetryNative.ShouldShowTelemetryConsentWindow(ISDKTelemetryConsent.ToolId))
                {
                    ISDKTelemetryConsentWindow.Show();
                    return;
                }

                // Path 2: consent already set by another tool, show non-blocking notification.
                if (ISDKEngineTelemetryNative.ShouldShowTelemetryNotification(ISDKTelemetryConsent.ToolId))
                {
                    ShowNotification();
                }
            }
            catch (System.DllNotFoundException)
            {
                Debug.LogWarning(
                    "[ISDK Telemetry] ISDKEngineTelemetry native library not found. " +
                    "Consent UI will not be shown.");
            }
        }

        private static void ShowNotification()
        {
            string markdownText;
            try
            {
                markdownText = ISDKEngineTelemetryNative.GetConsentNotificationMarkdownText(
                    "Meta > Interaction SDK > Telemetry Settings");
            }
            catch (System.Exception)
            {
                markdownText = null;
            }

            if (string.IsNullOrEmpty(markdownText))
            {
                markdownText = "Meta collects essential usage data to improve the " +
                    "Interaction SDK experience. No personal data is collected.";
            }

            ISDKTelemetryNotificationWindow.Show(markdownText, () =>
            {
                try
                {
                    ISDKEngineTelemetryNative.SetNotificationShown(ISDKTelemetryConsent.ToolId);
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[ISDK Telemetry] Failed to mark notification as shown: {e.Message}");
                }
            });
        }
    }
}

#endif
