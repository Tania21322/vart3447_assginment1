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
    /// Base class for all masks that are 2D
    /// </summary>
    [Feature(Feature.Scene)]
    [Obsolete("SceneDecorator is deprecated and will be removed in a future version.")]
    public abstract class Mask2D : Mask
    {
        /// <summary>x offset</summary>
        [SerializeField]
        public float offsetX;

        /// <summary>y offset</summary>
        [SerializeField]
        public float offsetY;

        /// <summary>rotation in degrees</summary>
        [SerializeField]
        public float rotation;

        /// <summary>scale in x direction</summary>
        [SerializeField]
        public float scaleX = 1f;

        /// <summary>scale in y direction</summary>
        [SerializeField]
        public float scaleY = 1f;

        /// <summary>shear in x direction</summary>
        [SerializeField]
        public float shearX;

        /// <summary>shear in y direction</summary>
        [SerializeField]
        public float shearY;

        private static Float3X3 GenerateAffineTransform(Vector2 position, float rotation, Vector2 scale, Vector2 shear)
        {
            var rotationRadians = Mathf.Deg2Rad * rotation;
            var cosValue = Mathf.Cos(rotationRadians);
            var mat = new Float3X3(scale.x, 0f, 0f, 0f, scale.y, 0f, 0f, 0f, 1f);

            mat = Float3X3.Multiply(new Float3X3(1f, shear.x, 0f, shear.y, 1f, 0f, 0f, 0f, 1f), mat);

            var sinValue = Mathf.Sin(rotationRadians);
            mat = Float3X3.Multiply(new Float3X3(cosValue, -sinValue, 0f, sinValue, cosValue, 0f, 0f, 0f, 1f), mat);
            mat = Float3X3.Multiply(new Float3X3(1f, 0f, position.x, 0f, 1f, position.y, 0f, 0f, 1f), mat);

            return mat;
        }

        internal static Float3X3 GenerateAffineTransform(float positionX, float positionY, float rotation, float scaleX, float scaleY, float shearX, float shearY)
        {
            return GenerateAffineTransform(new Vector2(positionX, positionY), rotation, new Vector2(scaleX, scaleY),
                new Vector2(shearX, shearY));
        }
    }
}
