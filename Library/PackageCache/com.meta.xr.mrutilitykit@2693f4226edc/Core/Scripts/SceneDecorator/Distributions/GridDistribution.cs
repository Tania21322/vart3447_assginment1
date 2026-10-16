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
    /// A distribution that places a grid of decorations
    /// </summary>
    [Serializable]
    [Feature(Feature.Scene)]
    [Obsolete("SceneDecorator is deprecated and will be removed in a future version.")]
    public class GridDistribution : SceneDecorator.IDistribution
    {
        [SerializeField] private float _spacingX = 1f;
        [SerializeField] private float _spacingY = 1f;

        /// <summary>
        /// Distribute in a grid pattern
        /// </summary>
        /// <param name="sceneDecorator">The decorator</param>
        /// <param name="sceneAnchor">The SceneAnchor</param>
        /// <param name="sceneDecoration">The SceneDecoration</param>
        public void Distribute(SceneDecorator sceneDecorator, MRUKAnchor sceneAnchor, SceneDecoration sceneDecoration)
        {
            Vector3 anchorScale = Vector3.one;
            if (sceneAnchor.PlaneRect.HasValue)
            {
                anchorScale = new Vector3(sceneAnchor.PlaneRect.Value.width, sceneAnchor.PlaneRect.Value.height, 1);
            }

            if (sceneAnchor.VolumeBounds.HasValue)
            {
                anchorScale = sceneAnchor.VolumeBounds.Value.size;
            }

            Vector2 res = new Vector2(
                Mathf.Max(Mathf.Ceil(anchorScale.x / _spacingX), 1),
                Mathf.Max(Mathf.Ceil(anchorScale.y / _spacingY), 1));

            Vector2 gridStep = anchorScale / res;
            for (int gridX = 0; gridX < res.x; ++gridX)
            {
                for (int gridY = 0; gridY < res.y; ++gridY)
                {
                    var gridPosition = new Vector2(gridX, gridY) * gridStep;
                    var localPosition = new Vector2(gridPosition.x - anchorScale.x / 2,
                        gridPosition.y - anchorScale.y / 2);
                    var normalizedPosition = new Vector2(gridPosition.x / anchorScale.x,
                        gridPosition.y / anchorScale.y);
                    sceneDecorator.GenerateOn(localPosition, normalizedPosition, sceneAnchor, sceneDecoration);
                }
            }
        }
    }
}
