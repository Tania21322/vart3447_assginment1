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
using System;
using System.Collections.Generic;

namespace Oculus.Interaction
{
    /// <summary>
    /// Defines a cone with a convex base.
    /// </summary>
    public readonly struct Cone
    {
        /// <summary>
        /// The origin point of the cone.
        /// </summary>
        public readonly Vector3 Origin;

        /// <summary>
        /// The direction from the <see cref="Origin"/> to the center base of the cone.
        /// </summary>
        public readonly Vector3 Direction;

        /// <summary>
        /// The angular offset (in degrees) between the surface of the cone (excluding the base) and its center.
        /// </summary>
        /// <remarks>
        /// Value should be in range [0, 90).
        /// </remarks>
        public readonly float RadiusAngle;

        /// <summary>
        /// The distance (euclidean, meters) from the cone origin to the base of the cone.
        /// </summary>
        /// <remarks>
        /// The cone base is convex, not flat, such that every
        /// point on the base is equidistant to the origin.
        /// </remarks>
        public readonly float Length;

        /// <summary>
        /// The ray from the origin to the the center base of the cone.
        /// </summary>
        public Ray Ray => new Ray(Origin, Direction);

        /// <summary>
        /// Does the cone contain the given point?
        /// </summary>
        public bool Contains(Vector3 point)
        {
            Vector3 originToPoint = point - Origin;

            // Technically we shouldn't need to normalize the second vector, but this Vector3.Angle method
            // has a precision-related bug https://discussions.unity.com/t/a-question-about-vector3-angle/555586
            float degrees = Vector3.Angle(Direction, originToPoint.normalized);

            float sqDistance = originToPoint.sqrMagnitude;
            return sqDistance <= (Length * Length) && degrees <= RadiusAngle;
        }

        public Cone(Ray ray, float radiusAngle, float length) :
            this(ray.origin, ray.direction, radiusAngle, length)
        { }

        public Cone(Vector3 origin, Vector3 direction, float radiusAngle, float length)
        {
            Origin = origin;
            Direction = direction;
            RadiusAngle = radiusAngle;
            Length = length;
        }
    }

    /// <summary>
    /// An object in the scene that can be conecasted by <see cref="Conecaster"/>.
    /// </summary>
    public interface IConecastTarget
    {
        /// <summary>
        /// The unique ID representing this target.
        /// This should be unique among <see cref="IConecastTarget"/>s, meaning multiple
        /// targets should not have the same value for this field. If multiple targets do
        /// share the same <see cref="Id"/>, the behavior is undefined.
        /// </summary>
        ulong Id { get; }

        /// <summary>
        /// A conecast is defined here as the closest point on the object's surface to the
        /// <see cref="Cone.Ray"/>, where that point is within the cone's volume.
        /// </summary>
        /// <param name="cone">Only elements within this cone volume should be considered
        /// as valid results. </param>
        /// <param name="hitPoint">The surface point on the object nearest the cone ray.</param>
        /// <param name="surfaceNormal">The normal of the object's surface
        /// at the <see cref="hitPoint"/></param>
        /// <returns>True if closest point on object is within cone volume.</returns>
        bool TryConecast(in Cone cone, out Vector3 hitPoint, out Vector3 surfaceNormal);

        /// <summary>
        /// If a successful conecast is performed on this <see cref="IConecastTarget"/>,
        /// where <see cref="TryConecast"/> returns true, this method will be called to
        /// fetch child targets for further conecasting.
        /// </summary>
        /// <param name="children">An empty list should be provided here to be populated
        /// with child targets, if they exist.</param>
        /// <returns>True if any child targets are added to the list.</returns>
        bool TryGetChildren(IList<IConecastTarget> children);

        /// <summary>
        /// Optional. In the event the conecaster cannot determine the best target,
        /// this can be implemented to return a comparison value. See the
        /// <see cref="IComparable"/> interface for information on the return value.
        /// </summary>
        int TiebreakWith(IConecastTarget other) => 0;
    }

    public readonly struct ConecastHit
    {
        public readonly Cone Cone;
        public readonly Vector3 HitPointWorld;
        public readonly Vector3 HitNormalWorld;
        public readonly float DegreesFromConeOrigin;
        public readonly float DistanceFromConeOrigin;

        public ConecastHit(Cone cone, Vector3 hitPoint, Vector3 hitNormal)
        {
            Cone = cone;
            HitPointWorld = hitPoint;
            HitNormalWorld = hitNormal;

            // Technically we shouldn't need to normalize the second vector, but this Vector3.Angle method
            // has a precision-related bug https://discussions.unity.com/t/a-question-about-vector3-angle/555586
            DegreesFromConeOrigin = Vector3.Angle(cone.Direction, (hitPoint - cone.Origin).normalized);
            DistanceFromConeOrigin = Vector3.Distance(cone.Origin, hitPoint);
        }
    }
}
