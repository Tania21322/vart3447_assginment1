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
    /// A mask that randomly samples a value between 0 and 1.
    /// </summary>
    [Feature(Feature.Scene)]
    [Obsolete("SceneDecorator is deprecated and will be removed in a future version.")]
    public class RandomMask : Mask
    {
        /// <summary>
        /// Returns a random value between 0 and 1.
        /// </summary>
        /// <param name="candidate">Candidate with the information from the distribution</param>
        /// <returns>A random float value between 0 and 1</returns>
        public override float SampleMask(Candidate candidate)
        {
            return UnityEngine.Random.value;
        }

        /// <summary>
        /// Checks if the candidate is valid for this mask.
        /// </summary>
        /// <param name="candidate">Candidate with the information from the distribution</param>
        /// <returns>Always returns true as all candidates are valid for random sampling</returns>
        public override bool Check(Candidate candidate)
        {
            return true;
        }
    }
}
