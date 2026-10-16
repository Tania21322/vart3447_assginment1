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
    internal class ISDKTelemetrySettingsWindow : EditorWindow
    {
        [MenuItem("Window/Meta/Interaction SDK/Telemetry Settings", false, 2302)]
        private static void ShowWindow()
        {
            var window = GetWindow<ISDKTelemetrySettingsWindow>("ISDK Telemetry Settings");
            window.minSize = new Vector2(400, 200);
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Data Collection Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(8);

            string privacyText = ISDKEngineTelemetryNative.GetConsentSettingsChangeText()
                ?? "Meta collects usage data to improve the Interaction SDK.";
            EditorGUILayout.HelpBox(privacyText, MessageType.Info);
            EditorGUILayout.Space(12);

            // Toggle is disabled when consent is enforced at the user level.
            bool canChange = ISDKEngineTelemetryNative.IsConsentSettingsChangeEnabled(ISDKTelemetryConsent.ToolId);
            EditorGUI.BeginDisabledGroup(!canChange);

            int nativeState = ISDKTelemetryConsent.GetNativeConsentState();
            bool current = nativeState == ISDKEngineTelemetryNative.TelemetryOptionalBool_Unknown
                ? ISDKTelemetryConsent.TelemetryEnabled
                : nativeState == ISDKEngineTelemetryNative.TelemetryOptionalBool_True;
            bool newVal = EditorGUILayout.Toggle("Allow usage data collection", current);
            if (newVal != current)
            {
                ISDKTelemetryConsent.SetConsent(newVal);
            }

            EditorGUI.EndDisabledGroup();

            if (!canChange)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.HelpBox(
                    "Consent is managed by Meta Quest Developer Hub. "
                    + "Change your preference there.", MessageType.Warning);
            }
        }
    }
}

#endif
