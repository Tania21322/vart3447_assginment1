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


using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Meta.XR.MRUtilityKit.Tests
{
    /// <summary>
    /// Example test class demonstrating how to use the new configuration resolver
    /// system to create tests that work reliably across different projects.
    /// </summary>
    public class MRUKTestsVerification
    {
        /// <summary>
        /// Example test that uses the default configuration resolver.
        /// This test will work in any project because it uses the embedded defaults.
        /// </summary>
        [UnityTest]
        public IEnumerator ExampleTest_WithDefaultConfiguration()
        {
            var testSettings = MRUKTestsSettings.CreateDefaultConfiguration();

            Debug.Log(
                $"Test using configuration with {testSettings.SceneSettings.RoomPrefabs.Length} room prefabs " +
                $"and {testSettings.SceneSettings.SceneJsons.Length} scene JSONs. Data source: {testSettings.SceneSettings.DataSource}");

            yield return null;
            Debug.Log("ExampleTest_WithDefaultConfiguration completed successfully");
        }

        /// <summary>
        /// Example test that uses specific Core room prefabs and JSONs.
        /// Demonstrates loading all bedroom prefabs (10 rooms) and all Core JSONs (8 rooms).
        /// </summary>
        [UnityTest]
        public IEnumerator ExampleTest_WithSpecificCoreRooms()
        {
#if UNITY_EDITOR
            // Create a custom settings instance
            var settings = ScriptableObject.CreateInstance<MRUKTestsSettings>();

            // Load all bedroom prefabs from Core/Rooms/Prefabs/Bedroom
            var bedroomPrefabs = LoadBedroomPrefabs();
            Debug.Log($"Loaded {bedroomPrefabs.Count} bedroom prefabs");

            // Load all JSONs from Core/Rooms/JSON
            var coreJsons = LoadCoreJsons();
            Debug.Log($"Loaded {coreJsons.Count} core JSON files");

            // Set the loaded resources directly
            settings.SceneSettings.RoomPrefabs = bedroomPrefabs.ToArray();
            settings.SceneSettings.SceneJsons = coreJsons.ToArray();

            // Ensure scene settings are initialized before setting data source
            var sceneSettings = settings.SceneSettings;
            sceneSettings.DataSource = MRUK.SceneDataSource.Prefab;

            // Verify the settings have the expected resources
            Assert.AreEqual(10, settings.SceneSettings.RoomPrefabs.Length, "Should have 10 bedroom prefabs");
            Assert.AreEqual(8, settings.SceneSettings.SceneJsons.Length, "Should have 8 core JSON files");

            yield return null;

            // Cleanup
            Object.DestroyImmediate(settings);

            Debug.Log("ExampleTest_WithSpecificCoreRooms completed successfully");
#else
            yield return null;
            Assert.Inconclusive("This test requires Unity Editor");
#endif
        }

#if UNITY_EDITOR
        /// <summary>
        /// Helper method to load all bedroom prefabs from Core/Rooms/Prefabs/Bedroom.
        /// </summary>
        /// <returns>List of bedroom prefab GameObjects</returns>
        private static List<GameObject> LoadBedroomPrefabs()
        {
            var bedroomPrefabs = new List<GameObject>();

            try
            {
                var bedroomPath = "Packages/com.meta.xr.mrutilitykit/Core/Rooms/Prefabs/Bedroom";
                var prefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { bedroomPath });

                foreach (var guid in prefabGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        if (prefab != null)
                        {
                            bedroomPrefabs.Add(prefab);
                            Debug.Log($"Loaded bedroom prefab: {System.IO.Path.GetFileName(path)}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to load bedroom prefabs: {ex.Message}");
            }

            return bedroomPrefabs;
        }

        /// <summary>
        /// Helper method to load all JSON files from Core/Rooms/JSON.
        /// </summary>
        /// <returns>List of core JSON TextAssets</returns>
        private static List<TextAsset> LoadCoreJsons()
        {
            var coreJsons = new List<TextAsset>();

            try
            {
                var jsonPath = "Packages/com.meta.xr.mrutilitykit/Core/Rooms/JSON";
                var jsonGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { jsonPath });

                foreach (var guid in jsonGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (path.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase))
                    {
                        var jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                        if (jsonAsset != null)
                        {
                            coreJsons.Add(jsonAsset);
                            Debug.Log($"Loaded core JSON: {System.IO.Path.GetFileName(path)}");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Failed to load core JSONs: {ex.Message}");
            }

            return coreJsons;
        }
#endif
    }
}
