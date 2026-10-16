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

namespace Meta.XR.MRUtilityKit.BuildingBlocks
{
    /// <summary>
    /// Provides functionality to point at a position in the physical environment and place the Target prefab at that location using raycasting.
    /// </summary>
    public class PointAndLocate : SpaceLocator
    {
        [Tooltip("Assign a Transform to use that as raycast origin")]
        [SerializeField]
        internal Transform _raycastOrigin;

        /// <summary>
        /// Transform to use that as raycast origin
        /// </summary>
        protected override Transform RaycastOrigin => _raycastOrigin;

        /// <summary>
        /// Casts a ray from the <see cref="RaycastOrigin"/> into the physical environment and attempts to place the Target object at the hit location.
        /// </summary>
        public void Locate() => TryLocateSpace(out _);

        /// <summary>
        /// Creates and returns a ray for raycasting operations using the position and forward direction of the RaycastOrigin transform.
        /// </summary>
        /// <returns>A Ray starting from the RaycastOrigin position and pointing in its forward direction.</returns>
        protected internal override Ray GetRaycastRay() => new(RaycastOrigin.position, RaycastOrigin.forward);
    }
}
