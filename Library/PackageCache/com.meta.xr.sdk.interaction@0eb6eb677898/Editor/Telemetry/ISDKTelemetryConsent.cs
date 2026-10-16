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
using Oculus.Interaction.Telemetry;

namespace Oculus.Interaction.Editor.Telemetry
{
    internal static class ISDKTelemetryConsent
    {
        private const string ConsentKey = "ISDKTelemetry.ConsentEnabled";

        // Shared ISDK tool ID for telemetry consent
        public const int ToolId = 1;

        public static bool HasSetConsent => EditorPrefs.HasKey(ConsentKey);

        public static bool TelemetryEnabled
        {
            get => EditorPrefs.GetBool(ConsentKey, false);
            private set => EditorPrefs.SetBool(ConsentKey, value);
        }

        public static void SetConsent(bool enabled)
        {
            TelemetryEnabled = enabled;
            ISDKEngineTelemetryNative.SaveUnifiedConsent(ToolId,
                enabled ? ISDKEngineTelemetryNative.TelemetryBool_True
                        : ISDKEngineTelemetryNative.TelemetryBool_False);
        }

        public static int GetNativeConsentState()
        {
            return ISDKEngineTelemetryNative.GetUnifiedConsent(ToolId);
        }
    }
}

#endif
