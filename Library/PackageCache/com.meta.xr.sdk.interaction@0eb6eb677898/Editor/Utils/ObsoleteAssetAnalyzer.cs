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

#if UNITY_PROJECT_AUDITOR

using System;
using System.Collections.Generic;
using Unity.ProjectAuditor.Editor;
using Unity.ProjectAuditor.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor
{
    internal class ObsoleteAssetAnalyzer : AssetsModuleAnalyzer
    {
        private List<Material> _obsoleteMaterials = new ();

        internal static readonly Descriptor k_ObsoleteMaterialInPrefabDescriptor = new(
            "ISDK0001",
            "[ISDK] Obsolete Material in prefab",
            Areas.Quality,
            "The material used by this prefab is obsolete, and may be removed in a future version. A new material exists that should be used instead.",
            "Replace the obsolete material(s) with the current recommended material(s). In many cases, the new material will be located within an FBX file, next to the deprecated material."
        )
        {
            DefaultSeverity = Severity.Warning
        };

        public override void Initialize(Action<Descriptor> registerDescriptor)
        {
            registerDescriptor(k_ObsoleteMaterialInPrefabDescriptor);

            _obsoleteMaterials = ObsoleteAssetFinder.FindObsoleteMaterials();
        }

        public override IEnumerable<ReportItem> Analyze(AssetAnalysisContext context)
        {
            if (_obsoleteMaterials.Count == 0)
            {
                yield break;
            }

            // Use AssetDatabase to check if the asset at path (context.AssetPath) is a prefab. If not, skip it.
            if (!context.AssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                yield break;
            }

            // Double-check the asset type without fully loading it
            var assetType = AssetDatabase.GetMainAssetTypeAtPath(context.AssetPath);
            if (assetType != typeof(GameObject))
            {
                yield break;
            }

            if (context.AssetPath == "Assets/Samples/Asset Deprecation/Cube.prefab")
            {
                Debug.Log("Checking asset: " + context.AssetPath);
            }

            // Load the prefab asset and check if it uses any obsolete materials
            var materialUses =
                ObsoleteAssetFinder.FindPrefabMaterialUses(context.AssetPath, _obsoleteMaterials);
            foreach (var materialUse in materialUses)
            {
                foreach (var material in materialUse.MaterialNames)
                {
                    var issue = context.CreateIssue(
                        IssueCategory.AssetIssue,
                        k_ObsoleteMaterialInPrefabDescriptor.Id
                    );
                    issue.WithLocation(context.AssetPath);
                    issue.WithDescription(
                        $"Object '{materialUse.ObjectPath}' uses obsolete material: {material}.");
                    yield return issue;
                }
            }
        }
    }
}

#endif
