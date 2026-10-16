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

using Oculus.Interaction.Surfaces;
using UnityEngine;

namespace Oculus.Interaction
{
    public static class SurfaceExtensions
    {
        public static bool TryConecast(this ISurface surface,
            Cone cone, out Vector3 hitPoint, out Vector3 hitNormal)
        {
            Ray ray = cone.Ray;
            float maxDistance = cone.Length;

            // If cone angle is 0, treat as a raycast
            if (Mathf.Approximately(cone.RadiusAngle, 0f))
            {
                bool raycastResult = surface.Raycast(ray,
                    out SurfaceHit raycastHit, maxDistance);

                if (raycastResult)
                {
                    hitPoint = raycastHit.Point;
                    hitNormal = raycastHit.Normal;
                    return true;
                }
                else
                {
                    hitPoint = default;
                    hitNormal = default;
                    return false;
                }
            }

            // Conecast against the surface directly

            if (surface.Raycast(ray, out SurfaceHit hit, maxDistance) &&
                cone.Contains(hit.Point))
            {
                hitPoint = hit.Point;
                hitNormal = hit.Normal;
                return true;
            }

            if (surface is ISurfacePatch patch)
            {
                // If the surface isn't directly hit, but the surface is a patch
                // of a larger surface, conecast against the larger surface, and
                // use the closes point within the cone on the original surface patch.

                if (patch.BackingSurface.Raycast(ray, out hit, maxDistance) &&
                    patch.ClosestSurfacePoint(hit.Point, out SurfaceHit patchHit) &&
                    cone.Contains(patchHit.Point))

                {
                    hitPoint = patchHit.Point;
                    hitNormal = patchHit.Normal;
                    return true;
                }
            }
            else
            {
                // If the surface is not a patch, find the surface's closest point
                // to the cone ray, then find the closest point on the surface to that point.

                Vector3 GetNearestPointToGazeRay(Vector3 objectPos)
                {
                    return cone.Origin + Vector3.Project(
                        objectPos - cone.Origin, cone.Ray.direction);
                }

                if (surface.ClosestSurfacePoint(cone.Origin, out SurfaceHit nearestToCone) &&
                    surface.ClosestSurfacePoint(GetNearestPointToGazeRay(nearestToCone.Point),
                        out SurfaceHit nearestToGazeRay) &&
                    cone.Contains(nearestToGazeRay.Point))
                {
                    hitPoint = nearestToGazeRay.Point;
                    hitNormal = nearestToGazeRay.Normal;
                    return true;
                }
            }

            // didn't hit anything within the cone volume
            hitPoint = default;
            hitNormal = default;
            return false;
        }
    }
}
