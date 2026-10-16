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

namespace Meta.XR.FovSimulator
{
    [FilePath("Library/Meta/FovSimulationEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    internal sealed class FovSimulationEditorSettings : ScriptableSingleton<FovSimulationEditorSettings>
    {
        [SerializeField] private bool fovSimulationEnabled;

        internal bool FovSimulationEnabled
        {
            get => fovSimulationEnabled;
            set
            {
                if (fovSimulationEnabled == value)
                {
                    return;
                }
                fovSimulationEnabled = value;
                Save(saveAsText: true);
            }
        }
    }

    [InitializeOnLoad]
    internal static class FovSimulationEditorController
    {
        private const double MissingCameraWarningDelaySeconds = 10.0;
        private const double MissingCameraWarningIntervalSeconds = 30.0;
        private static double _missingCameraWarningTime;

        static FovSimulationEditorController()
        {
            FovSimulatorLoader.EditorSettingApplier = ApplySettingBeforeDebugger;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update -= ApplyWhenReady;
            if (EditorApplication.isPlaying && Enabled)
            {
                ScheduleApply();
            }
        }

        internal static bool Enabled => FovSimulationEditorSettings.instance.FovSimulationEnabled;

        internal static void SetEnabled(bool enabled, string source)
        {
            if (Enabled == enabled)
            {
                return;
            }

            FovSimulationEditorSettings.instance.FovSimulationEnabled = enabled;
            OVRFovSimulationTelemetry.SendSettingChanged(source, enabled);
            if (EditorApplication.isPlaying)
            {
                if (enabled)
                {
                    ScheduleApply();
                }
                else
                {
                    CancelScheduledApply();
                    FovSimulatorLoader.Apply(false, OVRFovSimulationTelemetry.EditorEnvironment);
                }
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                if (Enabled)
                {
                    ScheduleApply();
                }
                else
                {
                    FovSimulatorLoader.Apply(false, OVRFovSimulationTelemetry.EditorEnvironment);
                }
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                CancelScheduledApply();
            }
        }

        private static void ScheduleApply()
        {
            EditorApplication.update -= ApplyWhenReady;
            EditorApplication.update += ApplyWhenReady;
            _missingCameraWarningTime = EditorApplication.timeSinceStartup + MissingCameraWarningDelaySeconds;
            ApplyWhenReady();
        }

        private static void ApplySettingBeforeDebugger()
        {
            if (EditorApplication.isPlaying)
            {
                FovSimulatorLoader.Apply(Enabled, OVRFovSimulationTelemetry.EditorEnvironment);
            }
        }

        private static void ApplyWhenReady()
        {
            if (!EditorApplication.isPlaying || !Enabled)
            {
                CancelScheduledApply();
                return;
            }
            if (Camera.main == null)
            {
                if (EditorApplication.timeSinceStartup >= _missingCameraWarningTime)
                {
                    Debug.LogWarning(
                        "[FovSimulator] Waiting for a MainCamera before enabling FoV simulation.");
                    _missingCameraWarningTime =
                        EditorApplication.timeSinceStartup + MissingCameraWarningIntervalSeconds;
                }
                return;
            }

            CancelScheduledApply();
            FovSimulatorLoader.Apply(true, OVRFovSimulationTelemetry.EditorEnvironment);
        }

        private static void CancelScheduledApply()
        {
            EditorApplication.update -= ApplyWhenReady;
            _missingCameraWarningTime = 0.0;
        }
    }
}
