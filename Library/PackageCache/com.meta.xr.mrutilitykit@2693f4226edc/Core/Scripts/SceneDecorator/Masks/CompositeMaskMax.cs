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

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    /// <summary>
    /// A mask that combines multiple other masks and returns the maximum value of all
    /// </summary>
    [Feature(Feature.Scene)]
    [Obsolete("SceneDecorator is deprecated and will be removed in a future version.")]
    public class CompositeMaskMax : Mask2D
    {
        [SerializeField]
        private CompositeMaskAdd.MaskLayer[] maskLayers;

        /// <summary>
        /// This method applies the mask to the given candidate.
        /// It first generates an affine transformation based on the provided parameters,
        /// and then applies this transformation to the local position of the candidate.
        /// The resulting position is used to sample each layer in the mask, and the maximum value
        /// of all layers is returned. If there are no mask layers, it returns 0.
        /// </summary>
        /// <param name="c">The candidate to apply the mask to.</param>
        /// <returns>The maximum mask value for the given candidate.</returns>
        public override float SampleMask(Candidate c)
        {
            var affineTransform = GenerateAffineTransform(offsetX, offsetY, rotation, scaleX, scaleY, shearX, shearY);
            var tuv = Float3X3.Multiply(affineTransform, Vector3Extensions.FromVector2AndZ(c.localPos, 1f));
            tuv /= tuv.z;
            c.localPos = new Vector2(tuv.x, tuv.y);

            if (maskLayers.Length <= 0)
            {
                return 0f;
            }

            var value = float.NegativeInfinity;
            foreach (var layer in maskLayers)
            {
                var layerValue = layer.SampleMask(c);
                if (layerValue > value)
                {
                    value = layerValue;
                }
            }

            return value;
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
