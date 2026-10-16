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

using UnityEditor;
using UnityEngine;
using Oculus.Interaction.Telemetry;

namespace Oculus.Interaction.Editor.Telemetry
{
    [InitializeOnLoad]
    internal static class ISDKTelemetryEditorEvents
    {
        private const string SessionStateKey = "ISDKTelemetry_PackageImported";

        static ISDKTelemetryEditorEvents()
        {
            if (SessionState.GetBool(SessionStateKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionStateKey, true);
            SendPackageImportedEvent();
        }

        private static void SendPackageImportedEvent()
        {
            try
            {
                ISDKEngineTelemetryNative.Init();

                var metadataJson = ISDKTelemetryUtils.BuildMetadataJson(
                    ISDKTelemetryConstants.Package.Annotation.PackageVersion, GetPackageVersion(),
                    ISDKTelemetryConstants.Package.Annotation.UnityVersion, Application.unityVersion,
                    ISDKTelemetryConstants.Package.Annotation.ProcessorType, SystemInfo.processorType,
                    ISDKTelemetryConstants.Package.Annotation.SdkConfiguration, GetSdkConfiguration());

                ISDKEngineTelemetryNative.SendUnifiedEvent(
                    isEssential: true,
                    productType: "InteractionSdk",
                    eventName: ISDKTelemetryConstants.Package.EventName.Imported,
                    eventMetadataJson: metadataJson,
                    projectGuid: Application.identifier ?? string.Empty,
                    isRuntime: ISDKEngineTelemetryNative.TelemetryBool_False);
            }
            catch (System.DllNotFoundException)
            {
                // ISDKEngineTelemetry DLL not present — telemetry silently disabled
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ISDK Telemetry] Failed to send package imported event: {e.Message}");
            }
        }

        private static string GetSdkConfiguration()
        {
#if HAS_META_XR_SDK_CORE
            return "isdk_with_core_sdk";
#else
            return "isdk_essentials_only";
#endif
        }

        private static string GetPackageVersion()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                typeof(ISDKTelemetryEditorEvents).Assembly);
            return packageInfo?.version ?? "unknown";
        }
    }
}
