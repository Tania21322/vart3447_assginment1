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

using Meta.XR.MRUtilityKit;
using UnityEditor;
using UnityEngine;
[CustomEditor(typeof(MRUK))]
public class MRUKEditor : Editor
{
    private const float _spacing = 10;
    private string _currentRoomName = "...";
    private string _numberOfRooms = "...";
    public override void OnInspectorGUI()
    {
        var mruk = (MRUK)target;
        DrawDefaultInspector();
        EditorGUILayout.Space(_spacing);
        EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
        var mrukInitialized = Application.isPlaying && mruk.IsInitialized;
        _currentRoomName = mrukInitialized ? mruk.GetCurrentRoom()?.name ?? "No Room" : "...";
        _numberOfRooms = mrukInitialized ? mruk.Rooms.Count.ToString() : "...";
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Number of Rooms", _numberOfRooms);
        EditorGUILayout.LabelField("Current Room Loaded", _currentRoomName);
        EditorGUILayout.LabelField("Current Room Loaded Index",
            mrukInitialized ? mruk.currentRoomLoadedIndex.ToString() : "...");
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(_spacing);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear Scene", GUILayout.MinWidth(100)))
        {
            if (Utilities.EnsurePlayMode())
            {
                Utilities.ExecuteAction(() =>
                        mruk.ClearScene(),
                    "Scene successfully cleared."
                );
            }
        }

        // Button to load next scene
        if (GUILayout.Button("Load Next Scene", GUILayout.MinWidth(150)))
        {
            if (Utilities.EnsurePlayMode())
            {
                Utilities.ExecuteAction(() =>
                    {
                        mruk.currentRoomLoadedIndex++;
                        switch (mruk.SceneSettings.DataSource)
                        {
                            case MRUK.SceneDataSource.Prefab
                                or MRUK.SceneDataSource.DeviceWithPrefabFallback:
                            {
                                if (mruk.currentRoomLoadedIndex >= mruk.SceneSettings.RoomPrefabs.Length)
                                {
                                    mruk.currentRoomLoadedIndex = 0;
                                }
                                _ = mruk.LoadSceneFromPrefab(
                                    mruk.SceneSettings.RoomPrefabs[mruk.currentRoomLoadedIndex],
                                    true);
                                break;
                            }
                            case MRUK.SceneDataSource.Json
                                or MRUK.SceneDataSource.DeviceWithJsonFallback:
                            {
                                if (mruk.currentRoomLoadedIndex >= mruk.SceneSettings.SceneJsons.Length)
                                {
                                    mruk.currentRoomLoadedIndex = 0;
                                }
                                _ = mruk.LoadSceneFromJsonString(
                                    mruk.SceneSettings.SceneJsons[mruk.currentRoomLoadedIndex].text,
                                    true);
                                break;
                            }
                        }
                    },
                    "Scene successfully changed."
                );
            }
        }
        EditorGUILayout.EndHorizontal();
        // Apply changes to the serialized object
        if (GUI.changed)
        {
            EditorUtility.SetDirty(mruk);
        }
    }
}
