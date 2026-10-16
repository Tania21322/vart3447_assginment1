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

using UnityEngine;
#if USE_OPENXR
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features.Interactions;
#endif

namespace Oculus.Interaction.Input
{
    [Experimental]
    public class UnityXREyeTrackingActiveState : MonoBehaviour, IActiveState
    {
        private bool? _isEyeTrackingSupported = null;

        public bool Active => _isEyeTrackingSupported ??
            (_isEyeTrackingSupported = CheckEyeTrackingSupported()).Value;

        private const string EXTENSION_STRING = "XR_EXT_eye_gaze_interaction";

        private static bool CheckEyeTrackingSupported()
        {
#if USE_OPENXR
            if (!OpenXRRuntime.IsExtensionEnabled(EXTENSION_STRING))
            {
                Debug.Log($"{EXTENSION_STRING} is not enabled. Eye tracking is not supported.");
                return false;
            }

            var openXrSettings = OpenXRSettings.Instance;
            if (openXrSettings != null)
            {
                var eyeGazeInteractionFeature = openXrSettings.GetFeature<EyeGazeInteraction>();
                return eyeGazeInteractionFeature != null && eyeGazeInteractionFeature.enabled;
            }
            else
            {
                Debug.LogWarning("OpenXR Settings is not initialized");
            }
#endif
            Debug.Log("Eye tracking is not supported.");
            return false;
        }
    }
}
