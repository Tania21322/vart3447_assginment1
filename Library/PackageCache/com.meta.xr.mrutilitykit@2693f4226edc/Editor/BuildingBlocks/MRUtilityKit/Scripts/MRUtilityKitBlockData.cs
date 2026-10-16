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

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// Block data implementation for MRUtilityKit building blocks that handles installation
    /// and configuration of MRUtilityKit components in the Unity scene.
    /// </summary>
    public class MRUtilityKitBlockData : Meta.XR.BuildingBlocks.Editor.BlockData
    {
        /// <summary>
        /// Installs the MRUtilityKit building block with special handling for existing MRUK instances.
        /// If an existing MRUK instance is found, it will be reused instead of creating a new one.
        /// In Unity 2021, prefab instances will be unpacked to allow modification.
        /// </summary>
        /// <param name="selectedGameObject">The currently selected GameObject in the scene.</param>
        /// <returns>A list containing the GameObject with the MRUK component, either existing or newly created.</returns>
        protected override List<GameObject> InstallRoutine(GameObject selectedGameObject)
        {
            var existingMRUK = FindAnyObjectByType<MRUK>();

            if (existingMRUK == null)
            {
                return base.InstallRoutine(selectedGameObject);
            }

#if UNITY_2021
            if (PrefabUtility.GetPrefabInstanceStatus(existingMRUK.gameObject) != PrefabInstanceStatus.NotAPrefab)
            {
                PrefabUtility.UnpackPrefabInstance(existingMRUK.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            }
#endif

            return new List<GameObject> { existingMRUK.gameObject };
        }
    }
}
