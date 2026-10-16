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

using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Oculus.Interaction.Editor
{
    /// <summary>
    /// Checks for obsolete material usage in scenes and prefabs before building.
    /// </summary>
    public class ObsoleteCheckBuildTask : IPreprocessBuildWithReport
    {

        public int callbackOrder => 0;

        private System.Collections.Generic.List<Material> _obsoleteMaterials;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (_obsoleteMaterials == null)
            {
                _obsoleteMaterials = ObsoleteAssetFinder.FindObsoleteMaterials();
            }
            if (_obsoleteMaterials?.Count > 0)
            {
                var prefabs = ObsoleteAssetFinder.FindPrefabAssetsWithMaterials(_obsoleteMaterials);
                ObsoleteAssetFinder.LogPrefabsWithObsoleteMaterials(prefabs);
            }
        }
    }

    /// <summary>
    /// Checks for obsolete material usage in non-prefab GameObjects for each scene being built.
    /// </summary>
    internal class SceneBuildTasks : IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        private System.Collections.Generic.List<Material> _obsoleteMaterials;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            if (_obsoleteMaterials == null)
            {
                _obsoleteMaterials = ObsoleteAssetFinder.FindObsoleteMaterials();
            }

            if (_obsoleteMaterials?.Count > 0)
            {
                var findings = ObsoleteAssetFinder.FindSceneGameObjectsWithMaterials(scene, _obsoleteMaterials);
                ObsoleteAssetFinder.LogSceneGameObjectsWithObsoleteMaterials(findings);
            }
        }
    }
}
