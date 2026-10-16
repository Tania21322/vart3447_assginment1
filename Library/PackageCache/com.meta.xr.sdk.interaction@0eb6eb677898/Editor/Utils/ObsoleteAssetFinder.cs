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
using UnityEngine.SceneManagement;

namespace Oculus.Interaction.Editor
{
    public struct SceneGameObjectWithMaterials
    {
        public string SceneName;
        public string ObjectPath;
        public List<string> MaterialNames;
    }

    public struct PrefabMaterialUse
    {
        public string PrefabPath;
        public string ObjectPath;
        public List<string> MaterialNames;
    }

    public class ObsoleteAssetFinder
    {
        public const string OBSOLETE_TAG = "Meta_isdk_obsolete";

        public static List<Material> FindObsoleteMaterials()
        {
            var obsoleteMaterials = new List<Material>();

            string[] guids = AssetDatabase.FindAssets($"l:{OBSOLETE_TAG} t:Material");
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material != null)
                {
                    obsoleteMaterials.Add(material);
                }
            }

            return obsoleteMaterials;
        }

        public static List<PrefabMaterialUse> FindPrefabAssetsWithMaterials(List<Material> materials)
        {
            var prefabsWithObsoleteMaterials = new List<PrefabMaterialUse>();
            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");

            foreach (var prefabGuid in prefabGuids)
            {
                string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                foreach (var materialUse in FindPrefabMaterialUses(prefabPath, materials))
                {
                    prefabsWithObsoleteMaterials.Add(materialUse);
                }
            }

            return prefabsWithObsoleteMaterials;
        }

        public static IEnumerable<PrefabMaterialUse> FindPrefabMaterialUses(string prefabPath, List<Material> materials)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                yield break;
            }

            var renderers = prefab.GetComponentsInChildren<Renderer>(true);

            foreach (var renderer in renderers)
            {
                PrefabMaterialUse materialUse = new();
                foreach (var material in renderer.sharedMaterials)
                {
                    if (materials.Contains(material))
                    {
                        if (materialUse.MaterialNames == null)
                        {
                            materialUse.PrefabPath = prefabPath;
                            materialUse.MaterialNames = new List<string>();
                            materialUse.ObjectPath = GetGameObjectPath(renderer.gameObject);
                        }
                        materialUse.MaterialNames.Add(material.name);
                    }
                }

                if (materialUse.MaterialNames != null)
                {
                    yield return materialUse;
                }

            }
        }

        public static void LogPrefabsWithObsoleteMaterials(List<PrefabMaterialUse> prefabs)
        {
            foreach (var prefab in prefabs)
            {
                Debug.LogWarning($"[ISDK] Scene '{prefab.PrefabPath}' has GameObject '{prefab.ObjectPath}' using obsolete materials: {string.Join(", ", prefab.MaterialNames)}");
            }
        }

        public static List<SceneGameObjectWithMaterials> FindSceneGameObjectsWithMaterials(Scene scene, List<Material> materials)
        {
            var findings = new List<SceneGameObjectWithMaterials>();
            var rootObjects = scene.GetRootGameObjects();

            foreach (var rootObject in rootObjects)
            {
                var renderers = rootObject.GetComponentsInChildren<Renderer>(true);
                foreach (var renderer in renderers)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(renderer.gameObject))
                    {
                        var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderer.gameObject);
                        var prefabLabels = AssetDatabase.GetLabels(prefab);

                        if (prefab != null && ArrayUtility.Contains(prefabLabels, OBSOLETE_TAG))
                        {
                            // Skip if this prefab is marked as obsolete. Use of obsolete prefabs is noted elsewhere.
                            continue;
                        }
                    }

                    var foundMaterials = new List<string>();
                    foreach (var material in renderer.sharedMaterials)
                    {
                        if (materials.Contains(material))
                        {
                            foundMaterials.Add($"'{material.name}'");
                        }
                    }

                    if (foundMaterials.Count > 0)
                    {
                        findings.Add(new SceneGameObjectWithMaterials
                        {
                            SceneName = scene.name,
                            ObjectPath = GetGameObjectPath(renderer.gameObject),
                            MaterialNames = foundMaterials
                        });
                    }
                }
            }

            return findings;
        }

        public static void LogSceneGameObjectsWithObsoleteMaterials(List<SceneGameObjectWithMaterials> findings)
        {
            foreach (var finding in findings)
            {
                Debug.LogWarning($"[ISDK] Scene '{finding.SceneName}' has GameObject '{finding.ObjectPath}' using obsolete materials: {string.Join(", ", finding.MaterialNames)}");
            }
        }

        private static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
