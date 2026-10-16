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
using Meta.XR.Util;
using UnityEngine;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    /// <summary>
    /// A mask that returns true if the position is inside of a room.
    /// </summary>
    [Feature(Feature.Scene)]
    [Obsolete("SceneDecorator is deprecated and will be removed in a future version.")]
    public class InsideCurrentRoomMask : Mask
    {
        /// <summary>
        /// Returns a constant value since this mask only performs boolean checks.
        /// The actual filtering logic is implemented in the Check method.
        /// </summary>
        /// <param name="candidate">Candidate with the information from the distribution</param>
        /// <returns>Always returns 0 as this mask is used for boolean filtering only</returns>
        public override float SampleMask(Candidate candidate)
        {
            return 0;
        }

        /// <summary>
        /// Validates whether the candidate's hit point is located inside the current room.
        /// This method performs the primary filtering logic for this mask.
        /// </summary>
        /// <param name="candidate">Candidate with the information from the distribution</param>
        /// <returns>True if the candidate's hit point is inside the current room, false otherwise</returns>
        public override bool Check(Candidate candidate)
        {
            var room = MRUK.Instance.GetCurrentRoom();
            if (room == null)
            {
                return false;
            }
            return room.IsPositionInRoom(candidate.hit.point);
        }
    }
}
