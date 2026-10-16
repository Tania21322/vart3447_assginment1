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

namespace Meta.XR.MRUtilityKit.Extensions
{
    internal static class Vector2Extensions
    {
        /// <summary>
        /// Returns a new Vector2 with the floor of each component.
        /// </summary>
        /// <param name="a">The Vector2 to apply the floor operation to.</param>
        /// <returns>A new Vector2 with the floor of each component.</returns>
        internal static Vector2 Floor(this Vector2 a)
        {
            return new Vector2(Mathf.Floor(a.x), Mathf.Floor(a.y));
        }

        /// <summary>
        /// Returns the fractional part of each component.
        /// </summary>
        /// <param name="a">The Vector2 to get the fractional parts from.</param>
        /// <returns>A new Vector2 containing the fractional part of each component.</returns>
        internal static Vector2 Frac(this Vector2 a)
        {
            return new Vector2(a.x - Mathf.Floor(a.x), a.y - Mathf.Floor(a.y));
        }

        /// <summary>
        /// Adds a scalar value to both components of the Vector2.
        /// </summary>
        /// <param name="a">The Vector2 to add to.</param>
        /// <param name="b">The scalar value to add to both components.</param>
        /// <returns>A new Vector2 with the scalar value added to both components.</returns>
        internal static Vector2 Add(this Vector2 a, float b)
        {
            return new Vector2(a.x + b, a.y + b);
        }

        /// <summary>
        /// Returns the absolute value of each component.
        /// </summary>
        /// <param name="a">The Vector2 to get the absolute values from.</param>
        /// <returns>A new Vector2 with the absolute value of each component.</returns>
        internal static Vector2 Abs(this Vector2 a)
        {
            return new Vector2(Mathf.Abs(a.x), Mathf.Abs(a.y));
        }
    }
}
