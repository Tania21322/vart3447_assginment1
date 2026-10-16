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
using Meta.XR.MRUtilityKit.Extensions;
using Meta.XR.Util;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    /// <summary>
    /// A mask that randomly samples a probability from another mask and returns 1 if the sampled value is greater than or equal to the threshold.
    /// </summary>
    [Feature(Feature.Scene)]
    [Obsolete("SceneDecorator is deprecated and will be removed in a future version.")]
    public class StochasticMask : Mask2D
    {
        [SerializeField]
        public CompositeMaskAdd.MaskLayer probabilitySource;

        /// <summary>
        /// This method applies the stochastic mask to the given candidate.
        /// It first generates an affine transformation based on the provided parameters,
        /// and then applies this transformation to the local position of the candidate.
        /// The resulting position is used to sample the probability source mask,
        /// and a random value is compared against it to return either 1 or 0.
        /// </summary>
        /// <param name="c">The candidate to apply the mask to.</param>
        /// <returns>1 if the random value is less than the sampled probability, otherwise 0.</returns>
        public override float SampleMask(Candidate c)
        {
            var affineTransform = GenerateAffineTransform(offsetX, offsetY, rotation, scaleX, scaleY, shearX, shearY);
            var tuv = Float3X3.Multiply(affineTransform, Vector3Extensions.FromVector2AndZ(c.localPos, 1f));
            tuv /= tuv.z;
            c.localPos = new Vector2(tuv.x, tuv.y);

            return Random.value < probabilitySource.SampleMask(c) ? 1f : 0f;
        }

        /// <summary>
        /// Not used on this mask
        /// </summary>
        /// <param name="c">The candidate</param>
        /// <returns>true</returns>
        public override bool Check(Candidate c)
        {
            return true;
        }
    }
}
