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
    /// A mask that samples the height of a candidate
    /// </summary>
    [Feature(Feature.Scene)]
    [Obsolete("SceneDecorator is deprecated and will be removed in a future version.")]
    public class HeightMask : Mask
    {
        /// <summary>
        /// Returns the height (y-coordinate) of the candidate's hit point
        /// </summary>
        /// <param name="candidate">Candidate containing hit information</param>
        /// <returns>The y-coordinate of the hit point</returns>
        public override float SampleMask(Candidate candidate)
        {
            return candidate.hit.point.y;
        }

        /// <summary>
        /// Validates whether the candidate is acceptable for height sampling.
        /// This mask does not perform any filtering and always accepts all candidates
        /// since it only samples height values without applying constraints.
        /// </summary>
        /// <param name="candidate">Candidate with the information from the distribution</param>
        /// <returns>Always returns true as this mask accepts all candidates</returns>
        public override bool Check(Candidate candidate)
        {
            return true;
        }
    }
}
