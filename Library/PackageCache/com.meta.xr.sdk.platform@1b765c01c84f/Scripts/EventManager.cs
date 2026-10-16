// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

#if USING_META_CORE_SDK_203_OR_NEWER && UNITY_EDITOR
using System;
using Meta.XR.Telemetry;
using UnityEditor;
#elif USING_META_CORE_SDK_77_OR_NEWER && UNITY_EDITOR
using System;
using UnityEditor;
#endif

[assembly: InternalsVisibleTo("Oculus.Platform.Editor")]
namespace Oculus.Platform
{
    internal class EventManager
    {
        private static string projectName;
        private static string projectGUID;

        internal static string ToPascalCase(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase))
                return snakeCase;

            var parts = snakeCase.Split('_');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 0)
                {
                    parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1);
                }
            }
            return string.Join("", parts);
        }

        internal static void SendUnifiedEvent(
            bool isEssential,
            string productType,
            string eventName,
            Dictionary<string, object> metadata = null,
            string event_entrypoint = "",
            string event_type = "",
            string event_target = "",
            string error_msg = "")
        {
#if USING_META_CORE_SDK_203_OR_NEWER && UNITY_EDITOR
            SendUnifiedEventNew(isEssential, eventName, metadata, event_entrypoint, event_type, event_target, error_msg);
#elif USING_META_CORE_SDK_77_OR_NEWER && UNITY_EDITOR
            SendUnifiedEventLegacy(isEssential, eventName, metadata, event_entrypoint, event_type, event_target, error_msg);
#endif
        }

#if USING_META_CORE_SDK_203_OR_NEWER && UNITY_EDITOR
        private static void SendUnifiedEventNew(
            bool isEssential,
            string eventName,
            Dictionary<string, object> metadata,
            string event_entrypoint,
            string event_type,
            string event_target,
            string error_msg)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                projectName = PlayerSettings.productName;
            }

            if (string.IsNullOrEmpty(projectGUID))
            {
                projectGUID = PlayerSettings.productGUID.ToString();
            }

            var eventData = new UnifiedEventData(eventName)
            {
                isEssential = isEssential,
                productType = TelemetryProductType.PlatformSdk,
                project_name = projectName,
                project_guid = projectGUID,
                entrypoint = event_entrypoint,
                type = event_type,
                target = event_target,
                error_msg = error_msg,
            };

            // Set metadata using typed SetMetadata methods
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    SetMetadataValue(ref eventData, kvp.Key, kvp.Value);
                }
            }

            eventData.Send();
        }

        private static void SetMetadataValue(ref UnifiedEventData eventData, string key, object value)
        {
            switch (value)
            {
                case string s:
                    eventData.SetMetadata(key, s);
                    break;
                case int i:
                    eventData.SetMetadata(key, i);
                    break;
                case float f:
                    eventData.SetMetadata(key, f);
                    break;
                case double d:
                    eventData.SetMetadata(key, d);
                    break;
                case bool b:
                    eventData.SetMetadata(key, b);
                    break;
                case long l:
                    eventData.SetMetadata(key, l);
                    break;
                default:
                    eventData.SetMetadata(key, value?.ToString() ?? "");
                    break;
            }
        }
#elif USING_META_CORE_SDK_77_OR_NEWER && UNITY_EDITOR
        private static void SendUnifiedEventLegacy(
            bool isEssential,
            string eventName,
            Dictionary<string, object> metadata,
            string event_entrypoint,
            string event_type,
            string event_target,
            string error_msg)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                projectName = PlayerSettings.productName;
            }

            if (string.IsNullOrEmpty(projectGUID))
            {
                projectGUID = PlayerSettings.productGUID.ToString();
            }

            var eventData = new OVRPlugin.UnifiedEventData(eventName)
            {
                isEssential = isEssential ? OVRPlugin.Bool.True : OVRPlugin.Bool.False,
                productType = OVRPlugin.ProductType.PlatformSdk,
                project_name = projectName,
                project_guid = projectGUID,
                entrypoint = event_entrypoint,
                type = event_type,
                target = event_target,
                error_msg = error_msg,
            };

            // Set metadata using typed SetMetadata methods
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    SetMetadataValueLegacy(ref eventData, kvp.Key, kvp.Value);
                }
            }

            eventData.Send();
        }

        private static void SetMetadataValueLegacy(ref OVRPlugin.UnifiedEventData eventData, string key, object value)
        {
            switch (value)
            {
                case string s:
                    eventData.SetMetadata(key, s);
                    break;
                case int i:
                    eventData.SetMetadata(key, i);
                    break;
                case float f:
                    eventData.SetMetadata(key, f);
                    break;
                case double d:
                    eventData.SetMetadata(key, d);
                    break;
                case bool b:
                    eventData.SetMetadata(key, b);
                    break;
                case long l:
                    eventData.SetMetadata(key, l);
                    break;
                default:
                    eventData.SetMetadata(key, value?.ToString() ?? "");
                    break;
            }
        }
#endif
    }
}
