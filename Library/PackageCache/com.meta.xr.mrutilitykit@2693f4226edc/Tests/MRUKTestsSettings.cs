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
using Meta.XR.Editor.Callbacks;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
using Meta.XR.Editor.Id;
using Meta.XR.MRUtilityKit.Tests.Editor;
#endif

namespace Meta.XR.MRUtilityKit.Tests
{
    /// <summary>
    /// MRUK Tests Settings ScriptableObject following the ImmersiveDebugger pattern.
    /// This file will be placed under the "Assets/Resources" folder and can be accessed at runtime.
    /// Provides configuration settings for MRUK automated testing framework with performance optimizations.
    /// </summary>
    public sealed class MRUKTestsSettings : OVRRuntimeAssetsBase
    {
        private const string InstanceAssetName = "MRUKTestsSettings";

        // Base paths for loading embedded test resources from the package.
        private const string TestJsonBasePath = "Packages/com.meta.xr.mrutilitykit/Core/Rooms/JSON";

        private const string TestPrefabBasePath = "Packages/com.meta.xr.mrutilitykit/Core/Rooms/Prefabs";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init() // reset static fields in case of domain reload disabled
        {
            _instance = null;
        }

        private static MRUKTestsSettings _instance;

        public static MRUKTestsSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (!InitializeOnLoad.EditorReady)
                    {
                        _instance = Resources.Load<MRUKTestsSettings>("MRUKTestsSettings");
                    }

                    LoadAsset(out MRUKTestsSettings settings, InstanceAssetName, OnCreateAsset);
                    _instance = settings;
                }
                return _instance;
            }
        }

        #region Settings Fields

        [Header("Basic Test Settings")]
        [SerializeField, Tooltip("Enable world lock functionality during tests")]
        internal bool enableWorldLock;



        [Header("Reporting")]
        [SerializeField, Tooltip("Show notifications when tests complete")]
        internal bool enableNotifications = true;
        [SerializeField, Tooltip("Automatically open the test report window when tests fail")]
        internal bool autoOpenOnFailure;

        // Public property accessors
        public bool EnableNotifications => enableNotifications;
        public bool AutoOpenOnFailure => autoOpenOnFailure;

        /// <summary>
        /// Simple event fired when new test reports have been saved to storage.
        /// </summary>
        public static event Action TestDataChanged;

        #endregion

        #region Properties

        // Simple property accessors without excessive validation
        public bool EnableWorldLock => enableWorldLock;

        #endregion

        #region Resource Management - Asset File + Embedded Fallback

        /// <summary>
        /// Gets room prefabs with fallback: asset file first, then embedded defaults.
        /// </summary>
        public GameObject[] GetRoomPrefabs()
        {
            // Priority 1: Use asset file configuration if available
            if (SceneSettings.RoomPrefabs != null && SceneSettings.RoomPrefabs.Length > 0)
            {
                return SceneSettings.RoomPrefabs;
            }

            // Priority 2: Fall back to embedded defaults
#if UNITY_EDITOR
            return LoadEmbeddedTestRoomPrefabs();
#else
            return Array.Empty<GameObject>();
#endif
        }

        /// <summary>
        /// Gets scene JSONs with fallback: asset file first, then embedded defaults.
        /// </summary>
        public TextAsset[] GetSceneJsons()
        {
            // Priority 1: Use asset file configuration if available
            if (SceneSettings.SceneJsons != null && SceneSettings.SceneJsons.Length > 0)
            {
                return SceneSettings.SceneJsons;
            }

            // Priority 2: Fall back to embedded defaults
#if UNITY_EDITOR
            return LoadEmbeddedTestSceneJsons();
#else
            return Array.Empty<TextAsset>();
#endif
        }

        /// <summary>
        /// Initializes the settings from the MRUK prefab (useful in editor).
        /// </summary>
        public bool TryInitializeFromMrukPrefab()
        {
#if UNITY_EDITOR
            try
            {
                var mrukPrefab = GetMrukPrefab();
                if (mrukPrefab?.GetComponent<MRUK>() is { } mrukInstance)
                {
                    SceneSettings.RoomPrefabs = mrukInstance.SceneSettings.RoomPrefabs;
                    SceneSettings.SceneJsons = mrukInstance.SceneSettings.SceneJsons;
                    EditorUtility.SetDirty(this);
                    Debug.Log("Successfully initialized MRUK test settings from MRUK prefab.");
                    return true;
                }
                Debug.LogWarning("MRUK prefab not found or invalid.");
                return false;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize from MRUK prefab: {ex.Message}");
                return false;
            }
#else
            return false;
#endif
        }


        #endregion

        [SerializeField]
        [Tooltip("Scene settings containing room prefabs and scene JSONs for testing")]
        private MRUK.MRUKSettings _sceneSettings;

        /// <summary>
        /// Provides access to current settings as MRUK.MRUKSettings
        /// </summary>
        public MRUK.MRUKSettings SceneSettings
        {
            get
            {
                if (_sceneSettings == null)
                {
                    _sceneSettings = new MRUK.MRUKSettings
                    {
                        DataSource = MRUK.SceneDataSource.Prefab,
                        RoomIndex = 0,
                        RoomPrefabs = Array.Empty<GameObject>(),
                        SceneJsons = Array.Empty<TextAsset>(),
                        LoadSceneOnStartup = true,
                        SeatWidth = 0.5f,
                        EnableHighFidelityScene = false
                    };
                }

                return _sceneSettings;
            }
            set => _sceneSettings = value;
        }

        /// <summary>
        /// Provides access to MRUK prefab
        /// </summary>
        public static GameObject MrukPrefab => GetMrukPrefab();

        /// <summary>
        /// Legacy compatibility - provides access to world lock setting
        /// </summary>
        public bool WorldLockEnabled => EnableWorldLock;

        public UnityEvent SceneLoadedEvent { get; } = new();
        public UnityEvent<MRUKRoom> RoomCreatedEvent { get; } = new();
        public UnityEvent<MRUKRoom> RoomUpdatedEvent { get; } = new();
        public UnityEvent<MRUKRoom> RoomRemovedEvent { get; } = new();

        /// <summary>
        /// Validates that a MRUKTestsSettings instance has usable configuration.
        /// </summary>
        /// <param name="settings">Settings instance to validate</param>
        /// <returns>True if the settings have valid test resources</returns>
        public static bool HasValidConfiguration(MRUKTestsSettings settings)
        {
            if (settings == null)
            {
                return false;
            }

            var hasRoomPrefabs = settings.SceneSettings.RoomPrefabs != null &&
                                 settings.SceneSettings.RoomPrefabs.Length > 0;
            var hasSceneJsons = settings.SceneSettings.SceneJsons != null &&
                                settings.SceneSettings.SceneJsons.Length > 0;

            // Need at least one type of test resource
            return hasRoomPrefabs || hasSceneJsons;
        }

        /// <summary>
        /// Creates a new MRUKTestsSettings instance with embedded default values.
        /// This method is used by OVRRuntimeAssetsBase CreateAsset callback and tests.
        /// </summary>
        /// <returns>A fully configured MRUKTestsSettings instance suitable for testing.</returns>
        public static MRUKTestsSettings CreateDefaultConfiguration()
        {
            var settings = CreateInstance<MRUKTestsSettings>();
            ApplyDefaultsToSettings(settings);
            return settings;
        }

        /// <summary>
        /// Applies default configuration values to the settings.
        /// </summary>
        internal static void ApplyDefaultsToSettings(MRUKTestsSettings settings)
        {
            if (settings == null) return;

            try
            {
                // Set basic test configuration
                settings.SceneSettings.DataSource = MRUK.SceneDataSource.Prefab;
                settings.SceneSettings.LoadSceneOnStartup = true;
                settings.enableWorldLock = true;
                settings.SceneSettings.RoomIndex = 0;
                settings.SceneSettings.SeatWidth = 0.5f;
                // Load embedded resources
                settings.SceneSettings.RoomPrefabs = LoadEmbeddedTestRoomPrefabs();
                settings.SceneSettings.SceneJsons = LoadEmbeddedTestSceneJsons();

                Debug.Log("Applied embedded defaults to MRUKTestsSettings");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to apply defaults to MRUKTestsSettings: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads embedded test room prefabs from the package.
        /// Falls back gracefully if prefabs are not available.
        /// </summary>
        /// <returns>Array of test room prefabs, or empty array if none found.</returns>
        private static GameObject[] LoadEmbeddedTestRoomPrefabs()
        {
            var prefabs = new List<GameObject>();

            try
            {
                // Search recursively in the TestPrefabBasePath for all GameObject prefabs
                var prefabGuids = AssetDatabase.FindAssets("t:GameObject", new[] { TestPrefabBasePath });

                foreach (var guid in prefabGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);

                    // Only load .prefab files to avoid loading other GameObjects
                    if (!path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null)
                    {
                        continue;
                    }

                    prefabs.Add(prefab);
                    var fileName = System.IO.Path.GetFileName(path);
                    Debug.Log($"Loaded test prefab: {fileName} from {path}");
                }

                Debug.Log($"Loaded {prefabs.Count} default test room prefabs from {TestPrefabBasePath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load embedded test room prefabs: {ex.Message}");
            }

            return prefabs.ToArray();
        }

        /// <summary>
        /// Loads embedded test scene JSONs from the package.
        /// Falls back gracefully if JSONs are not available.
        /// </summary>
        /// <returns>Array of test scene JSONs, or empty array if none found.</returns>
        private static TextAsset[] LoadEmbeddedTestSceneJsons()
        {
            var jsons = new List<TextAsset>();

            try
            {
                var additionalJsonGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { TestJsonBasePath });
                foreach (var guid in additionalJsonGuids)
                {
                    var path = AssetDatabase.GUIDToAssetPath(guid);
                    if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var jsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                    if (jsonAsset != null && !jsons.Contains(jsonAsset))
                    {
                        jsons.Add(jsonAsset);
                    }
                }

                Debug.Log($"Loaded {jsons.Count} default test scene JSONs");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load embedded test scene JSONs: {ex.Message}");
            }


            return jsons.ToArray();
        }


        /// <summary>
        /// Saves test results and triggers notifications. Called from test execution.
        /// </summary>
        public static void SaveTestResults(Dictionary<string, List<string>> testFailures, List<TestResult> allResults)
        {
            try
            {
                var reportData = CreateTestReportData(testFailures, allResults);
                SaveTestReport(reportData);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save test results: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the test report data to persistent storage and triggers notifications.
        /// This method is thread-safe and can be called from background test runners.
        /// </summary>
        /// <param name="reportData">The test report data to save</param>
        public static void SaveTestReport(TestReport reportData)
        {
            if (reportData == null)
            {
                Debug.LogWarning("Cannot save null test report data");
                return;
            }

#if UNITY_EDITOR
            try
            {
                var existingReports = LoadExistingReports();
                existingReports.Insert(0, reportData);

                // Keep only the most recent reports (limit to prevent unlimited growth)
                const int maxReports = 50;
                if (existingReports.Count > maxReports)
                {
                    existingReports.RemoveRange(maxReports, existingReports.Count - maxReports);
                }

                // Save to Meta XR Editor Settings
                var reportList = new TestReportCollection { Reports = existingReports };
                var json = JsonUtility.ToJson(reportList, true);
                MRUKTestsUtils.TestReportsSetting.SetValue(json, Origins.ProjectSettings);

                // Trigger the event to notify other systems (like editor windows)
                TestDataChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save test report data: {ex.Message}\n{ex.StackTrace}");
            }
#endif
        }
        /// <summary>
        /// Loads existing test reports from Meta XR Editor Settings.
        /// </summary>
        /// <returns>List of existing test report data</returns>
        private static List<TestReport> LoadExistingReports()
        {
#if UNITY_EDITOR
            try
            {
                var json = MRUKTestsUtils.TestReportsSetting.Value;
                if (!string.IsNullOrEmpty(json))
                {
                    var reportList = JsonUtility.FromJson<TestReportCollection>(json);
                    return reportList?.Reports ?? new List<TestReport>();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load existing test reports: {ex.Message}");
            }
#endif
            return new List<TestReport>();
        }

        /// <summary>
        /// Creates a TestReport object from raw test results using reflection.
        /// </summary>
        /// <param name="testFailures">Dictionary of test failures</param>
        /// <param name="allResults">List of all test results</param>
        /// <returns>Processed TestReport object</returns>
        private static TestReport CreateTestReportData(Dictionary<string, List<string>> testFailures,
            List<TestResult> allResults)
        {
            var reportData = new TestReport
            {
                Timestamp = DateTime.Now,
                TestMethodName = "Unknown Test"
            };

            // Extract test method name from the first result
            if (allResults.Count > 0)
            {
                try
                {
                    var firstResult = allResults[0];
                    var resultType = firstResult.GetType();
                    var testNameProp = resultType.GetProperty("TestName");
                    reportData.TestMethodName = testNameProp?.GetValue(firstResult) as string ?? "Unknown Test";
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to extract test method name: {ex.Message}");
                    reportData.TestMethodName = "Unknown Test";
                }
            }

            ProcessTestResultsForReport(allResults, reportData);

            return reportData;
        }

        /// <summary>
        /// Processes test results and populates room statistics and other data.
        /// </summary>
        /// <param name="allResults">List of all test results</param>
        /// <param name="reportData">Report data to populate</param>
        private static void ProcessTestResultsForReport(List<TestResult> allResults, TestReport reportData)
        {
            foreach (var testResult in allResults)
            {
                if (testResult != null)
                {
                    reportData.AddResult(testResult);
                }
            }
        }

        /// <summary>
        /// Extracts TestResult from a test result object using reflection.
        /// </summary>
        /// <param name="resultObj">The test result object</param>
        /// <returns>TestResult if successful, null otherwise</returns>
        private static TestResult ExtractTestResultFromObject(object resultObj)
        {
            var resultType = resultObj.GetType();
            var roomNameProp = resultType.GetProperty("RoomName");
            var passedProp = resultType.GetProperty("Passed");
            var testNameProp = resultType.GetProperty("TestName");
            var errorMessageProp = resultType.GetProperty("ErrorMessage");
            var isConfigurationErrorProp = resultType.GetProperty("IsConfigurationError");

            if (roomNameProp == null || passedProp == null || testNameProp == null)
                return null;

            var roomName = roomNameProp.GetValue(resultObj) as string;
            var testName = testNameProp.GetValue(resultObj) as string;

            if (string.IsNullOrEmpty(roomName) || string.IsNullOrEmpty(testName))
                return null;

            return new TestResult
            {
                TestName = testName,
                RoomName = roomName,
                Passed = (bool)passedProp.GetValue(resultObj),
                ErrorMessage = errorMessageProp?.GetValue(resultObj) as string ?? string.Empty,
                IsConfigurationError = isConfigurationErrorProp != null && (bool)isConfigurationErrorProp.GetValue(resultObj)
            };
        }

        #region Helper methods

        private static GameObject GetMrukPrefab()
        {
#if UNITY_EDITOR
            return AssetDatabase.LoadAssetAtPath<GameObject>(
                "Packages/com.meta.xr.mrutilitykit/Core/Tools/MRUK.prefab");
#else
            return null;
#endif
        }

        /// <summary>
        /// Called when creating a new asset instance.
        /// Initializes default values using embedded test defaults instead of external prefabs.
        /// </summary>
        /// <param name="settings">The newly created settings instance. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when settings are null.</exception>
        private static void OnCreateAsset(MRUKTestsSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            try
            {
                // Initialize with embedded test defaults instead of MRUK prefab
                ApplyDefaultsToSettings(settings);

                Debug.LogWarning(
                    "A new MRUKTestsSettings instance has been created and initialized with defaults values." +
                    "If this was not intentional make sure to create an MRUKTestsSettings asset in you project's Resources folder.");

#if UNITY_EDITOR
                EditorUtility.SetDirty(settings);
#endif
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to initialize MRUKTestsSettings: {ex.Message}");
            }
        }

        /// <summary>
        /// Compares two lists for equality, handling null values safely.
        /// </summary>
        /// <typeparam name="T">The type of elements in the lists.</typeparam>
        /// <param name="list1">The first list to compare.</param>
        /// <param name="list2">The second list to compare.</param>
        /// <returns>True if the lists are equal; otherwise, false.</returns>
        private static bool AreListsEqual<T>(IList<T> list1, IList<T> list2) where T : class
        {
            if (list1 == null && list2 == null) return true;
            if (list1 == null || list2 == null) return false;
            if (list1.Count != list2.Count) return false;

            for (var i = 0; i < list1.Count; i++)
            {
                if (!ReferenceEquals(list1[i], list2[i]))
                    return false;
            }

            return true;
        }

        #endregion
    }
}

