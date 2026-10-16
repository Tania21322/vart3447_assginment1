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
using System.Collections.Generic;
using Meta.XR.BuildingBlocks.Editor;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.MRUtilityKit.BuildingBlocks
{
    public class EffectMeshInstallationRoutine : InstallationRoutine
    {
        internal enum EffectMeshVariant
        {
            Default,
            GlobalMeshOnly
        }

        [SerializeField]
        [Variant(Behavior = VariantAttribute.VariantBehavior.Parameter,
             Description = "Initial configuration types for the Effect Mesh block.")]
        internal EffectMeshVariant EffectMeshTheme = EffectMeshVariant.Default;

        /// <summary>
        /// Installs the Effect Mesh building block by instantiating the appropriate prefab variant.
        /// </summary>
        /// <param name="block">The block data containing information about the building block to install.</param>
        /// <param name="selectedGameObject">The currently selected GameObject in the scene (not used in this implementation).</param>
        /// <returns>A list containing the instantiated Effect Mesh prefab GameObject.</returns>
        public override List<GameObject> Install(BlockData block, GameObject selectedGameObject)
        {
            var defaultPrefab = Prefab.transform.GetChild(0).gameObject;
            var globalMeshPrefab = Prefab.transform.GetChild(1).gameObject;
            var spawnedPrefab = EffectMeshTheme == EffectMeshVariant.Default
                ? Instantiate(defaultPrefab)
                : Instantiate(globalMeshPrefab);
            var prefabName = EffectMeshTheme == EffectMeshVariant.Default ? defaultPrefab.name : globalMeshPrefab.name;
            spawnedPrefab.name = $"{Utils.BlockPublicTag} {prefabName}";
            Undo.RegisterCreatedObjectUndo(spawnedPrefab, $"Install {prefabName}");
            return new List<GameObject> { spawnedPrefab };
        }

        internal override IReadOnlyCollection<InstallationStepInfo> GetInstallationSteps(VariantsSelection selection)
        {
            if (!UsesPrefab)
                return Array.Empty<InstallationStepInfo>();

            return new List<InstallationStepInfo>
            {
                new(null, "Choose your desired variant of the Effect Mesh prefab."),
                new(null, "Instantiates an appropriate Effect Mesh prefab."),
                new(null, $"Renames the instantiated prefab to <b>{Utils.BlockPublicTag} {TargetBlockData.BlockName} - $type</b>.")
            };
        }
    }
}
