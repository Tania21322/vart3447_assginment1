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
using System.Linq;
using Oculus.Interaction.Editor.QuickActions;
using UnityEngine;

namespace Oculus.Interaction.OVR.Editor.QuickActions
{
    /// <summary>
    /// Public API for programmatically creating OVR interaction rigs.
    /// </summary>
    public static class OVRQuickActionsAPI
    {
        /// <summary>
        /// Creates the OVR Comprehensive Interaction Rig under the OVRCameraRig.
        /// If no OVRCameraRig exists, one is created automatically.
        /// Performs all wizard setup including disabling duplicate hand visuals
        /// and creating editable interactor variants.
        /// </summary>
        /// <param name="generateAsEditableCopy">When true, creates an editable copy of
        /// the interactor prefab instead of a prefab instance. Defaults to true.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddOVRInteractionRig(
            bool generateAsEditableCopy = true)
        {
            return QuickActionsWizard.CreateWithDefaults<OVRComprehensiveInteractionRigWizard>(null,
                injections: w =>
                {
#if UNITY_2022_1_OR_NEWER
                    w.InjectOptionalGenerateAsEditableCopy(generateAsEditableCopy);
#endif
                }).ToList();
        }
    }
}
