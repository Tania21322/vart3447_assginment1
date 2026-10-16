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
using UnityEditor;
using UnityEngine;

namespace Meta.XR.MRUtilityKit.Tests.Editor
{
    /// <summary>
    /// Custom editor for MRUKTestsSettings ScriptableObject.
    /// Provides an optimized, user-friendly interface for configuring MRUK test settings
    /// with performance considerations and proper error handling.
    /// </summary>
    [CustomEditor(typeof(MRUKTestsSettings))]
    public sealed class MRUKTestsSettingsEditor : UnityEditor.Editor
    {
        #region Constants

        private const int ButtonHeight = 25;
        private const int SectionSpacing = 10;
        private const int SubSectionSpacing = 5;
        private const string HelpBoxStyle = "helpbox";

        #endregion

        #region Cached Properties

        private SerializedProperty _sceneSettingsProperty;
        private SerializedProperty _enableWorldLockProperty;
        private SerializedProperty _enableNotificationsProperty;
        private SerializedProperty _autoOpenOnFailureProperty;

        #endregion

        #region Unity Lifecycle

        /// <summary>
        /// Called when the editor is enabled. Caches serialized properties for performance.
        /// </summary>
        private void OnEnable()
        {
            try
            {
                CacheSerializedProperties();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error caching serialized properties in MRUKTestsSettingsEditor: {ex.Message}");
            }
        }

        /// <summary>
        /// Draws the custom inspector GUI with optimized layout and error handling.
        /// </summary>
        public override void OnInspectorGUI()
        {
            if (target == null)
            {
                EditorGUILayout.HelpBox("Target MRUKTestsSettings is null.", MessageType.Error);
                return;
            }

            try
            {
                serializedObject.Update();

                DrawTestConfigurationSection();
                DrawRoomConfigurationSection();
                DrawTestAssetsSection();
                DrawTestReportingSection();
                DrawActionsSection();

                // Apply changes and mark dirty if needed
                if (serializedObject.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(target);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error drawing MRUKTestsSettings inspector: {ex.Message}");
                EditorGUILayout.HelpBox($"An error occurred while drawing the inspector: {ex.Message}",
                                      MessageType.Error);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Caches all serialized properties for performance optimization.
        /// </summary>
        private void CacheSerializedProperties()
        {
            _sceneSettingsProperty = serializedObject.FindProperty("_sceneSettings");
            _enableWorldLockProperty = serializedObject.FindProperty("enableWorldLock");
            _enableNotificationsProperty = serializedObject.FindProperty("enableNotifications");
            _autoOpenOnFailureProperty = serializedObject.FindProperty("autoOpenOnFailure");
        }

        /// <summary>
        /// Draws the test configuration section with proper validation.
        /// </summary>
        private void DrawTestConfigurationSection()
        {
            DrawSectionHeader("Test Configuration");

            // Draw properties from SceneSettings
            if (_sceneSettingsProperty != null)
            {
                var dataSourceProp = _sceneSettingsProperty.FindPropertyRelative("dataSource");
                if (dataSourceProp != null)
                {
                    var content = new GUIContent("Data Source", "Source of scene data for testing");
                    var enumValue = (MRUK.SceneDataSource)dataSourceProp.intValue;
                    enumValue = (MRUK.SceneDataSource)EditorGUILayout.EnumPopup(content, enumValue);
                    dataSourceProp.intValue = (int)enumValue;
                }

                var loadSceneOnStartupProp = _sceneSettingsProperty.FindPropertyRelative("LoadSceneOnStartup");
                if (loadSceneOnStartupProp != null)
                {
                    var content = new GUIContent("Load Scene on Startup",
                        "Automatically load scene data when tests start");
                    loadSceneOnStartupProp.boolValue =
                        EditorGUILayout.Toggle(content, loadSceneOnStartupProp.boolValue);
                }

                var enableHighFidelitySceneProp =
                    _sceneSettingsProperty.FindPropertyRelative("EnableHighFidelityScene");
                if (enableHighFidelitySceneProp != null)
                {
                    var content = new GUIContent("Enable High Fidelity Scene",
                        "Enable high fidelity scene rendering during tests");
                    enableHighFidelitySceneProp.boolValue =
                        EditorGUILayout.Toggle(content, enableHighFidelitySceneProp.boolValue);
                }
            }

            DrawPropertyFieldSafe(_enableWorldLockProperty, "Enable World Lock",
                                "Enable world lock functionality during tests");

            EditorGUILayout.Space(SectionSpacing);
        }

        /// <summary>
        /// Draws the room configuration section with validation.
        /// </summary>
        private void DrawRoomConfigurationSection()
        {
            DrawSectionHeader("Room Configuration");

            // Draw properties from SceneSettings
            if (_sceneSettingsProperty != null)
            {
                var roomIndexProp = _sceneSettingsProperty.FindPropertyRelative("RoomIndex");
                if (roomIndexProp != null)
                {
                    var content = new GUIContent("Room Index", "Index of the room to use for testing");
                    roomIndexProp.intValue = EditorGUILayout.IntField(content, roomIndexProp.intValue);

                    // Add validation for room index
                    if (roomIndexProp.intValue < 0)
                    {
                        EditorGUILayout.HelpBox("Room index cannot be negative.", MessageType.Warning);
                    }
                }

                var seatWidthProp = _sceneSettingsProperty.FindPropertyRelative("SeatWidth");
                if (seatWidthProp != null)
                {
                    var content = new GUIContent("Seat Width", "Width of seats in meters for testing");
                    seatWidthProp.floatValue = EditorGUILayout.FloatField(content, seatWidthProp.floatValue);

                    // Add validation for seat width
                    if (seatWidthProp.floatValue < 0.1f || seatWidthProp.floatValue > 2.0f)
                    {
                        EditorGUILayout.HelpBox("Seat width should be between 0.1 and 2.0 meters.",
                            MessageType.Warning);
                    }
                }
            }

            EditorGUILayout.Space(SectionSpacing);
        }

        /// <summary>
        /// Draws the test assets section with array management.
        /// </summary>
        private void DrawTestAssetsSection()
        {
            DrawSectionHeader("Test Assets");

            // Draw arrays from SceneSettings
            if (_sceneSettingsProperty != null)
            {
                // Room Prefabs
                DrawSubSectionHeader("Room Prefabs");
                var roomPrefabsProp = _sceneSettingsProperty.FindPropertyRelative("RoomPrefabs");
                DrawArrayPropertySafe(roomPrefabsProp, "Room prefab configurations for testing");

                EditorGUILayout.Space(SubSectionSpacing);

                // Scene JSONs
                DrawSubSectionHeader("Scene JSONs");
                var sceneJsonsProp = _sceneSettingsProperty.FindPropertyRelative("SceneJsons");
                DrawArrayPropertySafe(sceneJsonsProp, "JSON files containing scene data for testing");
            }

            EditorGUILayout.Space(SectionSpacing);
        }
        /// <summary>
        /// Draws the test reporting section with notification settings.
        /// </summary>
        private void DrawTestReportingSection()
        {
            DrawSectionHeader("Test Reporting");

            DrawPropertyFieldSafe(_enableNotificationsProperty, "Enable Notifications",
                                "Show notifications when tests complete");
            DrawPropertyFieldSafe(_autoOpenOnFailureProperty, "Auto-open on Failure",
                                "Automatically open the test report window when tests fail");
            EditorGUILayout.Space(SectionSpacing);
        }

        /// <summary>
        /// Draws the actions section with initialization and utility buttons.
        /// </summary>
        private void DrawActionsSection()
        {
            DrawSectionHeader("Actions");

            var settings = target as MRUKTestsSettings;
            if (settings == null)
            {
                EditorGUILayout.HelpBox("Settings target is null.", MessageType.Error);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Initialize from MRUK Prefab", GUILayout.Height(ButtonHeight)))
                {
                    HandleInitializeFromMrukPrefab(settings);
                }

                if (GUILayout.Button("Open Test Report Window", GUILayout.Height(ButtonHeight)))
                {
                    HandleOpenTestReportWindow();
                }
            }

            // Add help text for actions
            EditorGUILayout.Space(SubSectionSpacing);
            using (new EditorGUILayout.VerticalScope(HelpBoxStyle))
            {
                EditorGUILayout.LabelField("Actions Help:", EditorStyles.miniBoldLabel);
                EditorGUILayout.LabelField("• Initialize from MRUK Prefab: Automatically configure room prefabs and scene JSONs from the MRUK package", EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.LabelField("• Open Test Report Window: View detailed test execution results and statistics", EditorStyles.wordWrappedMiniLabel);
            }
        }

        /// <summary>
        /// Draws a section header with consistent styling.
        /// </summary>
        /// <param name="title">The title of the section.</param>
        private static void DrawSectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            EditorGUILayout.Space(SubSectionSpacing);
        }

        /// <summary>
        /// Draws a sub-section header with consistent styling.
        /// </summary>
        /// <param name="title">The title of the sub-section.</param>
        private static void DrawSubSectionHeader(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.miniBoldLabel);
        }

        /// <summary>
        /// Safely draws a property field with error handling.
        /// </summary>
        /// <param name="property">The serialized property to draw.</param>
        /// <param name="label">The display label for the property.</param>
        /// <param name="tooltip">The tooltip text for the property.</param>
        private static void DrawPropertyFieldSafe(SerializedProperty property, string label, string tooltip)
        {
            if (property == null)
            {
                EditorGUILayout.HelpBox($"Property '{label}' not found.", MessageType.Warning);
                return;
            }

            try
            {
                EditorGUILayout.PropertyField(property, new GUIContent(label, tooltip));
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error drawing property '{label}': {ex.Message}");
                EditorGUILayout.HelpBox($"Error displaying {label}", MessageType.Error);
            }
        }

        /// <summary>
        /// Safely draws an array property with error handling and validation.
        /// </summary>
        /// <param name="arrayProperty">The array property to draw.</param>
        /// <param name="tooltip">The tooltip text for the array.</param>
        private static void DrawArrayPropertySafe(SerializedProperty arrayProperty, string tooltip)
        {
            if (arrayProperty == null)
            {
                EditorGUILayout.HelpBox("Array property not found.", MessageType.Warning);
                return;
            }

            try
            {
                EditorGUILayout.PropertyField(arrayProperty, new GUIContent(arrayProperty.displayName, tooltip), true);

                // Show array size information
                if (arrayProperty.isArray)
                {
                    var arraySize = arrayProperty.arraySize;
                    if (arraySize == 0)
                    {
                        EditorGUILayout.HelpBox($"No {arrayProperty.displayName.ToLower()} configured. Add items to enable testing with different configurations.",
                                              MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Total: {arraySize} item{(arraySize == 1 ? "" : "s")}", EditorStyles.miniLabel);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error drawing array property '{arrayProperty.displayName}': {ex.Message}");
                EditorGUILayout.HelpBox($"Error displaying {arrayProperty.displayName}", MessageType.Error);
            }
        }

        /// <summary>
        /// Handles the initialization from MRUK prefab with proper error handling.
        /// </summary>
        /// <param name="settings">The settings instance to initialize.</param>
        private void HandleInitializeFromMrukPrefab(MRUKTestsSettings settings)
        {
            try
            {
                if (settings.TryInitializeFromMrukPrefab())
                {
                    serializedObject.Update(); // Refresh after external changes
                    EditorUtility.SetDirty(settings);

                    // Show success message
                    Debug.Log("Successfully initialized MRUK test settings from MRUK prefab.");
                }
                else
                {
                    EditorUtility.DisplayDialog("Initialization Failed",
                        "Failed to initialize from MRUK prefab. Please check the console for details.", "OK");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error initializing from MRUK prefab: {ex.Message}");
                EditorUtility.DisplayDialog("Initialization Error",
                    $"An error occurred during initialization: {ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Handles opening the test report window with error handling.
        /// </summary>
        private static void HandleOpenTestReportWindow()
        {
            MRUKTestReportWindow.ShowWindow();
        }
        #endregion
    }
}
