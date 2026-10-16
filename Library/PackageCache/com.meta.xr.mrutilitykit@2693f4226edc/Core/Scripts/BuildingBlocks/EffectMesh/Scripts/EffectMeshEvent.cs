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
using UnityEngine.Events;

namespace Meta.XR.MRUtilityKit.BuildingBlocks
{
    /// <summary>
    /// A helper class to receive Global Mesh load completion event.
    /// </summary>
    /// <remarks>
    /// This Unity component is part of Effect Mesh Building Blocks. Subscribe to this event to be notified when the Global Mesh is loaded.
    /// The <see cref="MeshFilter"/> in the event payload contains the loaded mesh data.
    /// For more information on the Effect Mesh, see [Effect Mesh](https://developer.oculus.com/documentation/unity/unity-scene-build-mixed-reality/#scene-mesh) in the MRUK documentation.
    /// </remarks>
    [RequireComponent(typeof(EffectMesh))]
    public class EffectMeshEvent : MonoBehaviour
    {
        /// <summary>
        /// An event to trigger when the Global Mesh loads successfully.
        /// </summary>
        /// <remarks>
        /// In the event payload, <see cref="MeshFilter"/>, is the component that will contain the mesh data.
        /// </remarks>
        public UnityEvent<MeshFilter> OnGlobalMeshLoadComplete;

        private EffectMesh _effectMesh;

        private void Awake()
        {
            _effectMesh = GetComponent<EffectMesh>();
        }

        private void OnEnable()
        {
            _effectMesh.OnMeshLoadedComplete += HandleMeshLoaded;
        }

        private void OnDisable()
        {
            _effectMesh.OnMeshLoadedComplete -= HandleMeshLoaded;
        }

        private void HandleMeshLoaded()
        {
            foreach (var effectMeshObj in _effectMesh.EffectMeshObjects)
            {
                if (effectMeshObj.Key.Label == MRUKAnchor.SceneLabels.GLOBAL_MESH)
                {
                    var meshFilter = effectMeshObj.Value.effectMeshGO.GetComponent<MeshFilter>();
                    OnGlobalMeshLoadComplete?.Invoke(meshFilter);
                    break;
                }
            }
        }
    }
}
