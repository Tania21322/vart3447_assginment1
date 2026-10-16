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

namespace Oculus.Interaction.Editor.QuickActions
{
    /// <summary>
    /// Constants used by Gaze Quick Actions wizards and setup utilities.
    /// </summary>
    internal static class GazeQuickActionsConstants
    {
        /// <summary>
        /// Standard tag for interactables that should fallback to Ray when Gaze is unavailable.
        /// Add this tag to any RayInteractable's TagSet to opt into Gaze/Ray fallback behavior.
        /// </summary>
        public const string GAZE_RAY_FALLBACK_TAG = "ISDK_Gaze_Fallback";
    }
}
