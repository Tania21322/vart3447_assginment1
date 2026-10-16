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

using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Oculus.Interaction.Telemetry
{
    internal static class ISDKTelemetryRuntimeEvents
    {
        private static readonly Dictionary<string, float> _lastEventTime = new Dictionary<string, float>();
        private const float THROTTLE_SECONDS = 5f;

        /// <summary>
        /// Fire ISDK_INTERACTOR_USED when an interactor enters Select state.
        /// Throttled: max once per 5s per unique interactor+interactable pair.
        /// </summary>
        public static void SendInteractorUsed(string interactorType, string interactableType)
        {
            var key = string.Concat(interactorType, "_", interactableType);
            if (_lastEventTime.TryGetValue(key, out var lastTime)
                && Time.unscaledTime - lastTime < THROTTLE_SECONDS)
            {
                return;
            }
            _lastEventTime[key] = Time.unscaledTime;

            try
            {
                var metadataJson = ISDKTelemetryUtils.BuildMetadataJson(
                    ISDKTelemetryConstants.Interactor.Annotation.InteractorType, interactorType,
                    ISDKTelemetryConstants.Interactor.Annotation.InteractableType, interactableType);

                ISDKEngineTelemetryNative.SendUnifiedEvent(
                    isEssential: true,
                    productType: "InteractionSdk",
                    eventName: ISDKTelemetryConstants.Interactor.EventName.Used,
                    eventMetadataJson: metadataJson,
                    projectGuid: Application.identifier ?? string.Empty,
                    isRuntime: ISDKEngineTelemetryNative.TelemetryBool_True);
            }
            catch (System.DllNotFoundException)
            {
                // ISDKEngineTelemetry DLL not present — telemetry silently disabled
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ISDK Telemetry] Failed to send interactor used event: {e.Message}");
            }
        }

        /// <summary>
        /// Fire ISDK_INTERACTION_SESSION_START with duration on app quit.
        /// </summary>
        public static void SendSessionDuration(float sessionDurationSeconds)
        {
            try
            {
                var metadataJson = ISDKTelemetryUtils.BuildMetadataJson(
                    ISDKTelemetryConstants.Session.Annotation.SessionDuration,
                    sessionDurationSeconds.ToString("F1", CultureInfo.InvariantCulture));

                ISDKEngineTelemetryNative.SendUnifiedEvent(
                    isEssential: true,
                    productType: "InteractionSdk",
                    eventName: ISDKTelemetryConstants.Session.EventName.Started,
                    eventMetadataJson: metadataJson,
                    projectGuid: Application.identifier ?? string.Empty,
                    isRuntime: ISDKEngineTelemetryNative.TelemetryBool_True);
            }
            catch (System.DllNotFoundException)
            {
                // ISDKEngineTelemetry DLL not present — telemetry silently disabled
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ISDK Telemetry] Failed to send session event: {e.Message}");
            }
        }
    }
}
