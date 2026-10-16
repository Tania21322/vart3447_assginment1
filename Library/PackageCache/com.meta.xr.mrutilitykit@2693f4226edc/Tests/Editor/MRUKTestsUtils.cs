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
using System.Linq;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.Settings;
using Meta.XR.Editor.ToolingSupport;
using Meta.XR.Editor.UserInterface;
using UnityEditor;
using UnityEngine;
using static Meta.XR.Editor.UserInterface.Styles.Contents;
using static Meta.XR.Editor.UserInterface.Styles.Colors;
using RLDSStyles = Meta.XR.Editor.UserInterface.RLDS.Styles;
using RLDSButton = Meta.XR.Editor.UserInterface.RLDS.Button;
using RLDSSeparator = Meta.XR.Editor.UserInterface.RLDS.Separator;

namespace Meta.XR.MRUtilityKit.Tests.Editor
{
    /// <summary>
    /// Simple array setting that uses Unity's standard PropertyField for array handling.
    /// Integrates with Meta XR Editor Settings framework while providing reliable array UI.
    /// </summary>
    /// <typeparam name="T">The type of Unity objects in the array. Must inherit from UnityEngine.Object.</typeparam>
    internal sealed class UnityArraySetting<T> : CustomSetting<T[]>
        where T : UnityEngine.Object
    {
        /// <summary>
        /// Draws the GUI using Unity's standard PropertyField with array support.
        /// </summary>
        /// <param name="origin">The origin of the GUI drawing request.</param>
        /// <param name="originData">Additional data about the origin.</param>
        /// <param name="callback">Optional callback to invoke after changes.</param>
        protected override void DrawForGUIImplementation(Origins origin, IIdentified originData, Action callback = null)
        {
            var settings = MRUKTestsSettings.Instance;
            if (settings == null) return;

            using var serializedObject = new SerializedObject(settings);
            serializedObject.Update();

            // Find the nested property in SceneSettings based on the setting's label
            var sceneSettingsProperty = serializedObject.FindProperty("_sceneSettings");
            if (sceneSettingsProperty == null)
            {
                EditorGUILayout.HelpBox("SceneSettings property not found. The settings may not be properly initialized.", MessageType.Warning);
                return;
            }

            var propertyName = Label == "Room Prefabs" ? "RoomPrefabs" : "SceneJsons";
            var arrayProperty = sceneSettingsProperty.FindPropertyRelative(propertyName);

            if (arrayProperty != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(arrayProperty, Content, true);

                if (EditorGUI.EndChangeCheck())
                {
                    if (serializedObject.ApplyModifiedProperties())
                    {
                        EditorUtility.SetDirty(settings);
                        // Update our cached value
                        Value = Get();
                        callback?.Invoke();
                    }
                }

                // Show helpful information
                if (arrayProperty.arraySize == 0)
                {
                    EditorGUILayout.HelpBox($"No {Label.ToLower()} configured. Add items to enable testing with different configurations.", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.LabelField($"Total: {arrayProperty.arraySize} item{(arrayProperty.arraySize == 1 ? "" : "s")}", EditorStyles.miniLabel);
                }
            }
            else
            {
                EditorGUILayout.HelpBox($"Array property '{propertyName}' not found in SceneSettings. The MRUK.MRUKSettings structure may not be properly serialized.", MessageType.Warning);
            }
        }
    }

    /// <summary>
    /// Utility class for MRUK Tests Settings integration with Meta XR Editor tooling.
    /// Provides optimized GUI components and settings management for the MRUK testing framework.
    /// </summary>
    [InitializeOnLoad]
    internal static class MRUKTestsUtils
    {
        #region Constants

        private const string PublicName = "MRUK Tests Settings";
        private const string DocumentationUrl = "https://developers.meta.com/horizon/documentation/unity/unity-mr-utility-kit-overview/";

        #endregion

        #region Static Fields

        /// <summary>
        /// Comprehensive description of the MRUK Tests Settings tool functionality.
        /// </summary>
        private static readonly string Description =
            "Configure settings for MRUK (Mixed Reality Utility Kit) automated testing framework." +
            "\n\nThis tool allows you to:" +
            "\n• Configure room prefabs and scene JSONs for testing" +
            "\n• Set up test notifications and reporting preferences" +
            "\n• Manage test execution parameters and data sources" +
            "\n• View and analyze test results through the integrated report viewer";

        /// <summary>
        /// Icon category for MRUK Tests related textures.
        /// Uses the MRUK Tests assembly definition to resolve the icon path.
        /// </summary>
        private static readonly TextureContent.Category MRUKTestsIcons = new("Icon", pathKey: "meta.xr.mrutilitykit.tests");

        /// <summary>
        /// Status icon for the MRUK Tests Settings tool.
        /// </summary>
        private static readonly TextureContent StatusIcon =
            TextureContent.CreateContent("ovr_icon_mruk_stf.png", MRUKTestsIcons, $"Open {PublicName}");

        /// <summary>
        /// Tool descriptor for integration with Meta XR Editor tooling system.
        /// </summary>
        private static readonly ToolDescriptor ToolDescriptor = new ToolDescriptor
        {
            Name = PublicName,
            MenuDescription = "MRUK test configuration",
            Description = Description,
            Color = Meta.XR.Editor.UserInterface.Utils.HexToColor("#4285f4"),
            Icon = StatusIcon,
            InfoTextDelegate = ComputeInfoText,
            PillIcon = ComputePillIcon,
            OnClickDelegate = OnStatusMenuClick,
            Order = 15,
            Experimental = true,
            CanBeNew = false,
            AddToStatusMenu = false,
            OnProjectSettingsGUI = OnProjectSettingsGUI,
            Documentation = new List<Documentation>()
            {
                new Documentation()
                {
                    Title = "Mixed Reality Utility Kit",
                    Url = DocumentationUrl
                }
            }
        };

        /// <summary>
        /// Setting for managing room prefabs array using Unity's standard array UI.
        /// </summary>
        private static readonly UnityArraySetting<GameObject> RoomPrefabsSetting = new UnityArraySetting<GameObject>()
        {
            Uid = nameof(RoomPrefabsSetting),
            Owner = ToolDescriptor,
            Get = () => MRUKTestsSettings.Instance.SceneSettings.RoomPrefabs,
            Set = (val) =>
            {
                var settings = MRUKTestsSettings.Instance;
                settings.SceneSettings.RoomPrefabs = val ?? Array.Empty<GameObject>();
                UnityEditor.EditorUtility.SetDirty(settings);
            },
            Label = "Room Prefabs",
            Tooltip = "GameObject prefabs representing different room configurations for testing",
            SendTelemetry = false
        };

        /// <summary>
        /// Setting for managing scene JSONs array using Unity's standard array UI.
        /// </summary>
        private static readonly UnityArraySetting<TextAsset> SceneJsonsSetting = new UnityArraySetting<TextAsset>()
        {
            Uid = nameof(SceneJsonsSetting),
            Owner = ToolDescriptor,
            Get = () => MRUKTestsSettings.Instance.SceneSettings.SceneJsons.ToArray(),
            Set = (val) =>
            {
                var settings = MRUKTestsSettings.Instance;
                settings.SceneSettings.SceneJsons = val ?? Array.Empty<TextAsset>();
                UnityEditor.EditorUtility.SetDirty(settings);
            },
            Label = "Scene JSONs",
            Tooltip = "JSON files containing scene data for testing different room configurations",
            SendTelemetry = false
        };

        /// <summary>
        /// Setting for managing test reports data persistence using Meta XR Editor Settings framework.
        /// </summary>
        public static readonly UserString TestReportsSetting = new UserString()
        {
            Uid = "TestReports",
            Owner = ToolDescriptor,
            OldKey = "MRUKTestReports",
            Label = "Test Reports Data",
            Tooltip = "Serialized test reports data for persistence across Unity sessions",
            Default = "",
            SendTelemetry = false
        };

        #endregion

        #region Public Methods

        /// <summary>
        /// Creates a section header style with proper theming support.
        /// </summary>
        /// <returns>A GUIStyle configured for section headers with appropriate text color.</returns>
        private static GUIStyle CreateSectionHeaderStyle()
        {
            var style = RLDSStyles.Typography.Heading3.ToGUIStyle();
            style.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            return style;
        }

        /// <summary>
        /// Draws a section separator with consistent spacing.
        /// </summary>
        private static void DrawSectionSeparator()
        {
            GUILayout.Space(RLDSStyles.Spacing.SpaceMD);
            new RLDSSeparator().Draw();
            GUILayout.Space(RLDSStyles.Spacing.SpaceMD);
        }

        /// <summary>
        /// Computes the pill icon for the tool status display.
        /// </summary>
        /// <returns>A tuple containing the icon, color, and visibility state.</returns>
        private static (TextureContent, Color?, bool) ComputePillIcon()
        {
            try
            {
                return MRUKTestsSettings.Instance.EnableNotifications
                    ? (CheckIcon, XR.Editor.UserInterface.Styles.Colors.SuccessColor, false)
                    : (null, XR.Editor.UserInterface.Styles.Colors.DisabledColor, false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error computing pill icon: {ex.Message}");
                return (null, XR.Editor.UserInterface.Styles.Colors.ErrorColor, false);
            }
        }

        /// <summary>
        /// Computes the info text for the tool status display.
        /// </summary>
        /// <returns>A tuple containing the status text and color.</returns>
        private static (string, Color?) ComputeInfoText()
        {
            try
            {
                var settings = MRUKTestsSettings.Instance;
                var roomCount = settings.SceneSettings.RoomPrefabs.Length;
                var jsonCount = settings.SceneSettings.SceneJsons.Length;
                var totalScenes = roomCount + jsonCount;

                if (totalScenes > 0)
                {
                    return ($"{totalScenes} test scene{(totalScenes == 1 ? "" : "s")} configured",
                           XR.Editor.UserInterface.Styles.Colors.SuccessColor);
                }

                return ("No test scenes configured", XR.Editor.UserInterface.Styles.Colors.WarningColor);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error computing info text: {ex.Message}");
                return ("Error loading settings", XR.Editor.UserInterface.Styles.Colors.ErrorColor);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Handles clicks on the status menu item.
        /// </summary>
        /// <param name="origin">The origin of the click event.</param>
        private static void OnStatusMenuClick(Origins origin)
        {
            try
            {
                ToolDescriptor.OpenProjectSettings(origin);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error opening project settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Draws the project settings GUI with optimized layout and error handling.
        /// </summary>
        /// <param name="origin">The origin of the GUI request.</param>
        /// <param name="searchContext">The search context for filtering settings.</param>
        private static void OnProjectSettingsGUI(Origins origin, string searchContext)
        {
            try
            {
                var settings = MRUKTestsSettings.Instance;
                if (settings == null)
                {
                    EditorGUILayout.HelpBox("Failed to load MRUK Tests Settings. Please check the console for errors.",
                                          MessageType.Error);
                    return;
                }

                using var serializedObject = new SerializedObject(settings);
                serializedObject.Update();

                DrawTestConfigurationSection(serializedObject);
                DrawRoomConfigurationSection(serializedObject);
                DrawTestAssetsSection(origin);
                DrawTestReportingSection(serializedObject);
                DrawActionsSection(settings, serializedObject);

                // Apply all changes at once for better performance
                if (serializedObject.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(settings);
                }
            }
            catch (ExitGUIException)
            {
                // ExitGUIException is expected when Unity's PropertyField handles array operations - just rethrow it
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error drawing project settings GUI: {ex.Message}");
                EditorGUILayout.HelpBox($"An error occurred while drawing the settings: {ex.Message}",
                                      MessageType.Error);
            }
        }

        /// <summary>
        /// Draws the test configuration section of the settings GUI.
        /// </summary>
        /// <param name="serializedObject">The serialized object for the settings.</param>
        private static void DrawTestConfigurationSection(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Test Configuration", CreateSectionHeaderStyle());
            GUILayout.Space(RLDSStyles.Spacing.SpaceXS);

            var sceneSettingsProp = serializedObject.FindProperty("_sceneSettings");
            if (sceneSettingsProp != null)
            {
                var dataSourceProp = sceneSettingsProp.FindPropertyRelative("DataSource");
                if (dataSourceProp != null)
                {
                    var content = new GUIContent("Data Source", "Source of scene data for testing");
                    var enumValue = (MRUK.SceneDataSource)dataSourceProp.intValue;
                    enumValue = (MRUK.SceneDataSource)EditorGUILayout.EnumPopup(content, enumValue);
                    dataSourceProp.intValue = (int)enumValue;
                }
                else
                {
                    EditorGUILayout.HelpBox("DataSource property not found in SceneSettings.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("SceneSettings property not found. Make sure the settings are properly initialized.", MessageType.Warning);
            }

            GUILayout.Space(RLDSStyles.Spacing.Space2XS);

            if (sceneSettingsProp != null)
            {
                var loadSceneOnStartupProp = sceneSettingsProp.FindPropertyRelative("LoadSceneOnStartup");
                if (loadSceneOnStartupProp != null)
                {
                    var content = new GUIContent("Load Scene on Startup", "Automatically load scene data when tests start");
                    loadSceneOnStartupProp.boolValue = EditorGUILayout.Toggle(content, loadSceneOnStartupProp.boolValue);
                }
                else
                {
                    EditorGUILayout.HelpBox("LoadSceneOnStartup property not found in SceneSettings.", MessageType.Warning);
                }

                GUILayout.Space(RLDSStyles.Spacing.Space2XS);

                var enableHighFidelitySceneProp = sceneSettingsProp.FindPropertyRelative("EnableHighFidelityScene");
                if (enableHighFidelitySceneProp != null)
                {
                    var content = new GUIContent("Enable High Fidelity Scene", "Enable high fidelity scene rendering during tests");
                    enableHighFidelitySceneProp.boolValue = EditorGUILayout.Toggle(content, enableHighFidelitySceneProp.boolValue);
                }
                else
                {
                    EditorGUILayout.HelpBox("EnableHighFidelityScene property not found in SceneSettings.", MessageType.Warning);
                }
            }

            GUILayout.Space(RLDSStyles.Spacing.Space2XS);

            DrawPropertyField(serializedObject, nameof(MRUKTestsSettings.enableWorldLock), "Enable World Lock",
                            "Enable world lock functionality during tests", PropertyType.Toggle);

            DrawSectionSeparator();
        }

        /// <summary>
        /// Draws the room configuration section of the settings GUI.
        /// </summary>
        /// <param name="serializedObject">The serialized object for the settings.</param>
        private static void DrawRoomConfigurationSection(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Room Configuration", CreateSectionHeaderStyle());
            GUILayout.Space(RLDSStyles.Spacing.SpaceXS);

            var sceneSettingsProp = serializedObject.FindProperty("_sceneSettings");
            if (sceneSettingsProp != null)
            {
                var roomIndexProp = sceneSettingsProp.FindPropertyRelative("RoomIndex");
                if (roomIndexProp != null)
                {
                    var content = new GUIContent("Room Index", "The starting index of the scene room");
                    roomIndexProp.intValue = EditorGUILayout.IntField(content, roomIndexProp.intValue);
                }
                else
                {
                    EditorGUILayout.HelpBox("RoomIndex property not found in SceneSettings.", MessageType.Warning);
                }

                GUILayout.Space(RLDSStyles.Spacing.Space2XS);

                var seatWidthProp = sceneSettingsProp.FindPropertyRelative("SeatWidth");
                if (seatWidthProp != null)
                {
                    var content = new GUIContent("Seat Width",
                        "The width of a seat. Used to calculate seat positions with the COUCH label.");
                    seatWidthProp.floatValue = EditorGUILayout.FloatField(content, seatWidthProp.floatValue);
                }
                else
                {
                    EditorGUILayout.HelpBox("SeatWidth property not found in SceneSettings.", MessageType.Warning);
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "SceneSettings property not found. Make sure the settings are properly initialized.",
                    MessageType.Warning);
            }

            DrawSectionSeparator();
        }

        /// <summary>
        /// Draws the test assets section of the settings GUI.
        /// </summary>
        /// <param name="origin">The origin of the GUI request.</param>
        private static void DrawTestAssetsSection(Origins origin)
        {
            EditorGUILayout.LabelField("Test Assets", CreateSectionHeaderStyle());
            GUILayout.Space(RLDSStyles.Spacing.SpaceXS);

            RoomPrefabsSetting.DrawForGUI(origin, ToolDescriptor);
            GUILayout.Space(RLDSStyles.Spacing.SpaceXS);

            SceneJsonsSetting.DrawForGUI(origin, ToolDescriptor);

            DrawSectionSeparator();
        }

        /// <summary>
        /// Draws the test reporting section of the settings GUI.
        /// </summary>
        /// <param name="serializedObject">The serialized object for the settings.</param>
        private static void DrawTestReportingSection(SerializedObject serializedObject)
        {
            EditorGUILayout.LabelField("Test Reporting", CreateSectionHeaderStyle());
            GUILayout.Space(RLDSStyles.Spacing.SpaceXS);

            DrawPropertyField(serializedObject, nameof(MRUKTestsSettings.enableNotifications), "Enable Notifications",
                            "Show notifications when tests complete", PropertyType.Toggle);

            GUILayout.Space(RLDSStyles.Spacing.Space2XS);

            DrawPropertyField(serializedObject, nameof(MRUKTestsSettings.autoOpenOnFailure), "Auto-open on Failure",
                            "Automatically open the test report window when tests fail", PropertyType.Toggle);

            DrawSectionSeparator();
        }

        /// <summary>
        /// Draws the actions section of the settings GUI.
        /// </summary>
        /// <param name="settings">The settings instance.</param>
        /// <param name="serializedObject">The serialized object for the settings.</param>
        private static void DrawActionsSection(MRUKTestsSettings settings, SerializedObject serializedObject)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(RLDSStyles.Spacing.SpaceSM);

                var initButton = new RLDSButton(new ActionLinkDescription
                {
                    Content = new GUIContent("Initialize from MRUK Prefab", "Load test configuration from the MRUK prefab"),
                    Action = () =>
                    {
                        if (settings.TryInitializeFromMrukPrefab())
                        {
                            serializedObject.Update();
                            GUI.changed = true;
                            EditorUtility.SetDirty(settings);
                        }
                    }
                }, RLDSStyles.Buttons.Primary);
                initButton.Draw();

                GUILayout.Space(RLDSStyles.Spacing.SpaceXS);

                var resetButton = new RLDSButton(new ActionLinkDescription
                {
                    Content = new GUIContent("Reset to Defaults", "Reset all settings to their default values"),
                    Action = () =>
                    {
                        if (EditorUtility.DisplayDialog("Reset to Defaults",
                            "This will reset all test settings to embedded defaults. This action cannot be undone.",
                            "Reset", "Cancel"))
                        {
                            ResetToDefaults(settings, serializedObject);
                        }
                    }
                }, RLDSStyles.Buttons.Primary);
                resetButton.Draw();

                GUILayout.Space(RLDSStyles.Spacing.SpaceSM);
            }

            GUILayout.Space(RLDSStyles.Spacing.SpaceXS);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(RLDSStyles.Spacing.SpaceSM);

                var openReportButton = new RLDSButton(new ActionLinkDescription
                {
                    Content = new GUIContent("Open Test Report Window", "View and analyze test results"),
                    Action = MRUKTestReportWindow.ShowWindow
                }, RLDSStyles.Buttons.Primary);
                openReportButton.Draw();

                GUILayout.Space(RLDSStyles.Spacing.SpaceSM);
            }
        }

        /// <summary>
        /// Enumeration for different property field types.
        /// </summary>
        private enum PropertyType
        {
            Toggle,
            IntField,
            FloatField,
            Enum
        }

        /// <summary>
        /// Draws a property field with consistent styling and error handling.
        /// </summary>
        /// <param name="serializedObject">The serialized object containing the property.</param>
        /// <param name="propertyName">The name of the property to draw.</param>
        /// <param name="label">The display label for the property.</param>
        /// <param name="tooltip">The tooltip text for the property.</param>
        /// <param name="propertyType">The type of property field to draw.</param>
        private static void DrawPropertyField(SerializedObject serializedObject, string propertyName,
                                            string label, string tooltip, PropertyType propertyType)
        {
            var property = serializedObject.FindProperty(propertyName);
            if (property == null)
            {
                EditorGUILayout.HelpBox($"Property '{propertyName}' not found.", MessageType.Warning);
                return;
            }

            var content = new GUIContent(label, tooltip);

            switch (propertyType)
            {
                case PropertyType.Toggle:
                    property.boolValue = EditorGUILayout.Toggle(content, property.boolValue);
                    break;

                case PropertyType.IntField:
                    property.intValue = EditorGUILayout.IntField(content, property.intValue);
                    break;

                case PropertyType.FloatField:
                    property.floatValue = EditorGUILayout.FloatField(content, property.floatValue);
                    break;

                case PropertyType.Enum:
                    var enumValue = (MRUK.SceneDataSource)property.intValue;
                    enumValue = (MRUK.SceneDataSource)EditorGUILayout.EnumPopup(content, enumValue);
                    property.intValue = (int)enumValue;
                    break;

                default:
                    EditorGUILayout.PropertyField(property, content);
                    break;
            }
        }

        /// <summary>
        /// Resets settings to embedded defaults.
        /// </summary>
        /// <param name="settings">The settings instance to reset.</param>
        /// <param name="serializedObject">The serialized object for the settings.</param>
        private static void ResetToDefaults(MRUKTestsSettings settings, SerializedObject serializedObject)
        {
            MRUKTestsSettings.ApplyDefaultsToSettings(settings);
            serializedObject.Update(); // Refresh after external changes
            GUI.changed = true;
            EditorUtility.SetDirty(settings);

            Debug.Log("Successfully reset MRUK test settings to embedded defaults.");
        }

        #endregion
    }
}
