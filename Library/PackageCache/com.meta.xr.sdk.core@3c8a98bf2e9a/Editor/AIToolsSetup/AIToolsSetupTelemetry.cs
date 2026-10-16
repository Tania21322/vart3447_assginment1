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
using Meta.XR.Telemetry;
using UnityEditor;
using AgentBridgeSettings = Meta.XR.AI.AgentBridge.Settings;

namespace Meta.XR.Editor
{
    internal static class AIToolsSetupTelemetry
    {
        private const string PrefKeySessionId = "AIToolsSetup_TelemetrySessionId";

        internal static string SessionId { get; private set; }

        internal static AIToolsSetupModel ActiveModel { get; set; }

        internal static bool EnsureSessionId()
        {
            var existing = EditorPrefs.GetString(PrefKeySessionId, "");
            if (!string.IsNullOrEmpty(existing))
            {
                SessionId = existing;
                return false;
            }
            SessionId = Guid.NewGuid().ToString();
            EditorPrefs.SetString(PrefKeySessionId, SessionId);
            return true;
        }

        internal static void ResetSession()
        {
            SessionId = null;
            EditorPrefs.DeleteKey(PrefKeySessionId);
        }

        internal static UnifiedEventData AddSetupContext(this UnifiedEventData eventData)
        {
            eventData.productType = TelemetryProductType.Editor;

            if (!string.IsNullOrEmpty(SessionId))
            {
                eventData.SetMetadata(
                    AIToolsSetupTelemetryConstants.AnnotationType.SessionId, SessionId);
            }

            try
            {
                eventData.SetMetadata(
                    AIToolsSetupTelemetryConstants.AnnotationType.AgentBridgeEnabled,
                    AgentBridgeSettings.IsEnabled);

                if (AgentBridgeSettings.IsEnabled)
                {
                    var providerName = AgentBridgeSettings.SelectedServiceId.Value;
                    if (!string.IsNullOrEmpty(providerName))
                    {
                        eventData.SetMetadata(
                            AIToolsSetupTelemetryConstants.AnnotationType.AgentBridgeProvider,
                            providerName);
                    }
                }
            }
            catch
            {
                // AgentBridge settings can throw during early Editor init
            }

            var model = ActiveModel;
            if (model != null)
            {
                eventData.SetMetadata(
                    AIToolsSetupTelemetryConstants.AnnotationType.Step1State,
                    StepStateToString(model.Step1State));
                eventData.SetMetadata(
                    AIToolsSetupTelemetryConstants.AnnotationType.Step2State,
                    StepStateToString(model.Step2State));
                eventData.SetMetadata(
                    AIToolsSetupTelemetryConstants.AnnotationType.Step3State,
                    StepStateToString(model.Step3State));
            }

            return eventData;
        }

        public static void SendEvent(
            string eventName,
            Action<UnifiedEventData> configureEvent = null,
            bool isEssential = false)
        {
            try
            {
                var evt = new UnifiedEventData(eventName)
                {
                    isEssential = isEssential
                };
                evt.AddSetupContext();
                configureEvent?.Invoke(evt);
#if ATS_TELEMETRY_DEBUG
                UnityEngine.Debug.Log($"[ATS Telemetry] {eventName} | {evt.GetMetadata()}");
#endif
                evt.Send();
            }
            catch
            {
                // Telemetry errors must never surface to users
            }
        }


        internal static string StepStateToString(AIToolsSetupModel.StepState state)
        {
            return state switch
            {
                AIToolsSetupModel.StepState.Incomplete =>
                    AIToolsSetupTelemetryConstants.StepStateName.Incomplete,
                AIToolsSetupModel.StepState.Processing =>
                    AIToolsSetupTelemetryConstants.StepStateName.Processing,
                AIToolsSetupModel.StepState.Complete =>
                    AIToolsSetupTelemetryConstants.StepStateName.Complete,
                AIToolsSetupModel.StepState.Error =>
                    AIToolsSetupTelemetryConstants.StepStateName.Error,
                _ => AIToolsSetupTelemetryConstants.StepStateName.Incomplete
            };
        }
    }
}

