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
using System.Runtime.InteropServices;
using System.Text;

namespace Oculus.Interaction.Telemetry
{
    internal static class ISDKEngineTelemetryNative
    {
        private const string DllName = "ISDKEngineTelemetry";
        // Buffer size for StringBuilder out-parameters in native calls.
        // Must match or exceed the maximum output size from EngineTelemetry.h.
        // Native API currently writes at most ~2KB for consent markdown text.
        private const int TextBufferSize = 4096;

        // Must match the bundled ISDKEngineTelemetry version.
        // If this falls out of sync with the deployed DLL, Init() logs a warning.
        public const string ExpectedNativeVersion = "1.207.0";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr engineTelemetry_GetVersion();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void engineTelemetry_Init();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern void engineTelemetry_ShutDown();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_SetDeveloperTelemetryConsent(int consent);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_SendEvent(
            string eventName, string param, string source);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_SendUnifiedEvent(
            int isEssential,
            string productType,
            string eventName,
            string eventMetadataJson,
            string projectName,
            string eventEntrypoint,
            string projectGuid,
            string eventType,
            string eventTarget,
            string errorMsg,
            int isInternalBuild,
            int batchMode,
            ulong machineOculusUserId,
            int isRuntime,
            string eventStatus);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_AddCustomMetadata(
            string metadataName, string metadataParam);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_OnEditorShutdown();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_SaveUnifiedConsent(
            int toolId, int consentValue);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_SaveUnifiedConsentWithOlderVersion(
            int toolId, int consentValue, int consentVersion);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_GetUnifiedConsent(int toolId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_ShouldShowTelemetryConsentWindow(int toolId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_IsConsentSettingsChangeEnabled(int toolId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_ShouldShowTelemetryNotification(int toolId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_GetConsentSettingsChangeText(
            StringBuilder consentSettingsChangeText);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_SetNotificationShown(int toolId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_GetConsentTitle(StringBuilder consentTitle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_GetConsentMarkdownText(
            StringBuilder consentMarkdownText);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_GetConsentNotificationMarkdownText(
            string consentChangeLocationMarkdown,
            StringBuilder consentNotificationMarkdownText);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_QplMarkerStart(
            int markerId, int instanceKey, long timestampMs);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_QplMarkerEnd(
            int markerId, short actionId, int instanceKey, long timestampMs);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_QplMarkerPoint(
            int markerId, string name, int instanceKey, long timestampMs);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_QplMarkerAnnotation(
            int markerId, string annotationKey, string annotationValue, int instanceKey);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_QplCreateMarkerHandle(
            string name, out int nameHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_QplDestroyMarkerHandle(int nameHandle);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_QplSetConsent(int qplConsent);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int engineTelemetry_GetMachineID(StringBuilder machineId);

        private static bool _initialized;

        // telemetryBool constants
        public const int TelemetryBool_False = 0;
        public const int TelemetryBool_True = 1;

        // optionalTelemetryBool constants
        public const int OptionalTelemetryBool_False = 0;
        public const int OptionalTelemetryBool_True = 1;
        public const int OptionalTelemetryBool_NotSet = 2;

        // telemetryOptionalBool constants (return values from GetUnifiedConsent)
        public const int TelemetryOptionalBool_False = 0;
        public const int TelemetryOptionalBool_True = 1;
        public const int TelemetryOptionalBool_Unknown = 2;

        public static void Init()
        {
            if (_initialized)
            {
                return;
            }

            engineTelemetry_Init();
            _initialized = true;

            var nativeVersion = GetNativeVersion();
            if (nativeVersion != null && nativeVersion != ExpectedNativeVersion)
            {
                UnityEngine.Debug.LogWarning(
                    $"[Interaction SDK] Telemetry native library version mismatch " +
                    $"(found {nativeVersion}, expected {ExpectedNativeVersion}). " +
                    "Please update the Meta Interaction SDK package to the latest version.");
            }
        }

        public static string GetNativeVersion()
        {
            try
            {
                var ptr = engineTelemetry_GetVersion();
                return ptr != IntPtr.Zero ? Marshal.PtrToStringAnsi(ptr) : null;
            }
            catch (EntryPointNotFoundException)
            {
                return null;
            }
        }

        public static void ShutDown()
        {
            if (!_initialized)
            {
                return;
            }

            try
            {
                engineTelemetry_ShutDown();
            }
            catch (Exception)
            {
                // Suppress errors during shutdown — process may already be tearing down
            }
            finally
            {
                _initialized = false;
            }
        }

        public static bool SendUnifiedEvent(
            bool isEssential,
            string productType,
            string eventName,
            string eventMetadataJson,
            string projectName = null,
            string eventEntrypoint = null,
            string projectGuid = null,
            string eventType = null,
            string eventTarget = null,
            string errorMsg = null,
            int isInternalBuild = OptionalTelemetryBool_NotSet,
            int batchMode = OptionalTelemetryBool_NotSet,
            ulong machineOculusUserId = 0,
            int isRuntime = OptionalTelemetryBool_NotSet,
            string eventStatus = null)
        {
            if (batchMode == OptionalTelemetryBool_NotSet)
            {
                batchMode = UnityEngine.Application.isBatchMode
                    ? TelemetryBool_True
                    : TelemetryBool_False;
            }

            return engineTelemetry_SendUnifiedEvent(
                isEssential ? TelemetryBool_True : TelemetryBool_False,
                productType,
                eventName,
                eventMetadataJson,
                projectName,
                eventEntrypoint,
                projectGuid,
                eventType,
                eventTarget,
                errorMsg,
                isInternalBuild,
                batchMode,
                machineOculusUserId,
                isRuntime,
                eventStatus) == TelemetryBool_True;
        }

        public static bool ShouldShowTelemetryNotification(int toolId)
        {
            return engineTelemetry_ShouldShowTelemetryNotification(toolId) == TelemetryBool_True;
        }

        public static bool SetNotificationShown(int toolId)
        {
            return engineTelemetry_SetNotificationShown(toolId) == TelemetryBool_True;
        }

        public static string GetConsentNotificationMarkdownText(string consentChangeLocationMarkdown)
        {
            var sb = new StringBuilder(TextBufferSize);
            int result = engineTelemetry_GetConsentNotificationMarkdownText(consentChangeLocationMarkdown, sb);
            return result == TelemetryBool_True ? sb.ToString() : null;
        }

        public static string GetConsentTitle()
        {
            var sb = new StringBuilder(TextBufferSize);
            int result = engineTelemetry_GetConsentTitle(sb);
            return result == TelemetryBool_True ? sb.ToString() : null;
        }

        public static string GetMachineID()
        {
            var sb = new StringBuilder(TextBufferSize);
            int result = engineTelemetry_GetMachineID(sb);
            return result == TelemetryBool_True ? sb.ToString() : null;
        }

        public static bool SaveUnifiedConsent(int toolId, int consentValue)
        {
            try
            {
                return engineTelemetry_SaveUnifiedConsent(toolId, consentValue) == TelemetryBool_True;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }

        public static int GetUnifiedConsent(int toolId)
        {
            try
            {
                return engineTelemetry_GetUnifiedConsent(toolId);
            }
            catch (DllNotFoundException)
            {
                return TelemetryOptionalBool_Unknown;
            }
        }

        public static bool ShouldShowTelemetryConsentWindow(int toolId)
        {
            try
            {
                return engineTelemetry_ShouldShowTelemetryConsentWindow(toolId) == TelemetryBool_True;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }

        public static bool IsConsentSettingsChangeEnabled(int toolId)
        {
            try
            {
                return engineTelemetry_IsConsentSettingsChangeEnabled(toolId) == TelemetryBool_True;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }

        public static string GetConsentSettingsChangeText()
        {
            try
            {
                var sb = new StringBuilder(TextBufferSize);
                int result = engineTelemetry_GetConsentSettingsChangeText(sb);
                return result == TelemetryBool_True ? sb.ToString() : null;
            }
            catch (DllNotFoundException)
            {
                return null;
            }
        }

        public static string GetConsentMarkdownText()
        {
            try
            {
                var sb = new StringBuilder(TextBufferSize);
                int result = engineTelemetry_GetConsentMarkdownText(sb);
                return result == TelemetryBool_True ? sb.ToString() : null;
            }
            catch (DllNotFoundException)
            {
                return null;
            }
        }
    }
}
