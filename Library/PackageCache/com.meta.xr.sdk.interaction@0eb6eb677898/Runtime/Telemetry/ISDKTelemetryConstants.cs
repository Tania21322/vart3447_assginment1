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

namespace Oculus.Interaction.Telemetry
{
    internal static class ISDKTelemetryConstants
    {
        internal static class Package
        {
            internal static class EventName
            {
                public const string Imported = "ISDK_PACKAGE_IMPORTED";
            }

            internal static class Annotation
            {
                public const string PackageVersion = "package_version";
                public const string UnityVersion = "unity_version";
                public const string ProcessorType = "processor_type";
                public const string SdkConfiguration = "sdk_configuration";
            }
        }

        internal static class Session
        {
            internal static class EventName
            {
                public const string Started = "ISDK_INTERACTION_SESSION_START";
            }

            internal static class Annotation
            {
                public const string SessionDuration = "session_duration";
            }
        }

        internal static class Interactor
        {
            internal static class EventName
            {
                public const string Used = "ISDK_INTERACTOR_USED";
            }

            internal static class Annotation
            {
                public const string InteractorType = "interactor_type";
                public const string InteractableType = "interactable_type";
            }
        }
    }
}
