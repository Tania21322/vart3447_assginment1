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
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Oculus.Interaction
{
    /// <summary>
    /// Attach this alongside a Selectable component within a canvas to override the default
    /// canvas target search behavior. The Selectable will only use Graphic
    /// component(s) it references - other child graphic components will be ignored.
    /// </summary>
    [Experimental]
    [RequireComponent(typeof(Selectable))]
    public class ConecastableCanvasTarget : MonoBehaviour
    {
        [Tooltip("Provide a list of Graphic components to use as the target for this Selectable. " +
            "These Graphics must be children of the Selectable, and if none are provided, the " +
            "Selectable will be ignored by ConecastableCanvas.")]
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private Graphic[] _graphics;

        private HashSet<Graphic> _graphicSet;
        private HashSet<Graphic> GraphicSet => _graphicSet == null ?
            _graphicSet = new HashSet<Graphic>(_graphics) : _graphicSet;

        public bool ContainsGraphic(Graphic graphic) => GraphicSet.Contains(graphic);

        protected virtual void Start()
        {
            foreach (var graphic in _graphics)
            {
                Assert.IsTrue(graphic.transform.IsChildOf(transform),
                    "Provided graphics must be children of the Selectable.");
            }
        }

        #region Inject

        public void InjectOptionalGraphics(Graphic[] graphics)
        {
            _graphics = graphics;
        }

        #endregion
    }
}
