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


namespace Meta.XR.Editor
{
    internal static class AIToolsSetupTelemetryConstants
    {
        internal static class FalcoEventName
        {
            public const string Opened = "ats_opened";
            public const string Closed = "ats_closed";
            public const string ServiceSelected = "ats_service_selected";
            public const string InstallCompleted = "ats_install_completed";
            public const string AutoSetupCompleted = "ats_auto_setup_completed";
            public const string VerifyCompleted = "ats_verify_completed";
        }

        internal static class AnnotationType
        {
            // Ambient (on every event via AddSetupContext)
            public const string SessionId = "session_id";
            public const string AgentBridgeEnabled = "agentbridge_enabled";
            public const string AgentBridgeProvider = "agentbridge_provider";
            public const string Step1State = "step1_state";
            public const string Step2State = "step2_state";
            public const string Step3State = "step3_state";

            // Common
            public const string Success = "success";
            public const string ErrorKind = "error_kind";
            public const string ErrorMessage = "error_message";
            public const string DurationMs = "duration_ms";
            public const string ServiceId = "service_id";
            public const string PreviousServiceId = "previous_service_id";
            public const string EntryPoint = "entry_point";
            public const string CompletedFullSetup = "completed_full_setup";
            public const string BridgeEnabled = "bridge_enabled";
            public const string CanAutoRegister = "can_auto_register";
            public const string Trigger = "trigger";
            public const string BridgeOk = "bridge_ok";
            public const string AgenticOk = "agentic_ok";
            public const string Subject = "subject";
            public const string ProviderId = "provider_id";
        }

        internal static class StepStateName
        {
            public const string Incomplete = "incomplete";
            public const string Processing = "processing";
            public const string Complete = "complete";
            public const string Error = "error";
        }

        internal static class EntryPoint
        {
            public const string Menu = "menu";
            public const string Preferences = "preferences";
            public const string Code = "code";
            public const string GuidedSetup = "guided_setup";
            public const string Unknown = "unknown";
        }

        internal static class VerifyTrigger
        {
            public const string Manual = "manual";
            public const string Auto = "auto";
        }

        internal static class ErrorKind
        {
            public const string BridgeEnableFailed = "bridge_enable_failed";
            public const string RegistrationFailed = "registration_failed";
            public const string BridgeUnreachable = "bridge_unreachable";
            public const string AgenticUnreachable = "agentic_unreachable";
            public const string ProxyInstallFailed = "proxy_install_failed";
            public const string Cancelled = "cancelled";
            public const string Unknown = "unknown";
        }

        internal static class Subject
        {
            public const string Bridge = "bridge";
            public const string Proxy = "proxy";
            public const string Providers = "providers";
        }
    }
}

