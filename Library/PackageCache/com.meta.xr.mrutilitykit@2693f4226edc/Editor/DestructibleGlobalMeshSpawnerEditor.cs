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
using UnityEngine;
using UnityEditor;

namespace Meta.XR.MRUtilityKit
{
    [CustomEditor(typeof(DestructibleGlobalMeshSpawner))]
    public class DestructibleGlobalMeshSpawnerEditor : UnityEditor.Editor
    {
        private SerializedProperty _createOnRoomLoadProp;
        private SerializedProperty _reserveSpaceProp;
        private SerializedProperty _reservedTopProp;
        private SerializedProperty _reservedBottomProp;
        private SerializedProperty _globalMeshMaterialProp;
        private SerializedProperty _pointsPerUnitXProp;
        private SerializedProperty _pointsPerUnitYProp;
        private SerializedProperty _pointsPerUnitZProp;
        private SerializedProperty _onDestructibleMeshCreatedProp;

        private void OnEnable()
        {
            _createOnRoomLoadProp = serializedObject.FindProperty("CreateOnRoomLoaded");
            _onDestructibleMeshCreatedProp = serializedObject.FindProperty("OnDestructibleMeshCreated");
            _reserveSpaceProp = serializedObject.FindProperty("_reserveSpace");
            _reservedTopProp = serializedObject.FindProperty("_reservedTop");
            _reservedBottomProp = serializedObject.FindProperty("_reservedBottom");
            _globalMeshMaterialProp = serializedObject.FindProperty("_globalMeshMaterial");
            _pointsPerUnitXProp = serializedObject.FindProperty("_pointsPerUnitX");
            _pointsPerUnitYProp = serializedObject.FindProperty("_pointsPerUnitY");
            _pointsPerUnitZProp = serializedObject.FindProperty("_pointsPerUnitZ");
        }

        public override void OnInspectorGUI()
        {
            var spawner = (DestructibleGlobalMeshSpawner)target;
            if (!spawner)
            {
                return;
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(),
                    false);
            }

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_createOnRoomLoadProp,
                new GUIContent("Create On Room Loaded",
                    "Determines the rooms where the global mesh will be spawned when the scene data will be loaded."));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_onDestructibleMeshCreatedProp,
                new GUIContent("On Destructible Mesh Created",
                    "Event fired when the global mesh has been segmented and all of the segments have been instantiated."));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_globalMeshMaterialProp,
                new GUIContent("Global Mesh Material", "The material used for the destructible global mesh."));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_pointsPerUnitXProp,
                new GUIContent("Points Per Unit X",
                    "Specifies the number of points per unit along the X-axis for the destructible mesh. This setting affects the density and detail of the mesh, influencing both visual quality and performance."));
            EditorGUILayout.PropertyField(_pointsPerUnitYProp, new GUIContent("Points Per Unit Y",
                "Specifies the number of points per unit along the Y-axis for the destructible mesh. This setting affects the density and detail of the mesh, influencing both visual quality and performance."));
            EditorGUILayout.PropertyField(_pointsPerUnitZProp, new GUIContent("Points Per Unit Z",
                "Specifies the number of points per unit along the Z-axis for the destructible mesh. This setting affects the density and detail of the mesh, influencing both visual quality and performance."));
            EditorGUILayout.Space();

            EditorGUILayout.PropertyField(_reserveSpaceProp,
                new GUIContent("Reserve Space", "Whether to keep some reserved un-destructible space (defined in meters)."));
            if (spawner.ReserveSpace)
            {
                EditorGUILayout.PropertyField(_reservedTopProp,
                    new GUIContent("Reserved Top",
                        "The reserved space at the top of the mesh in which a mesh cannot be destructed."));
                EditorGUILayout.PropertyField(_reservedBottomProp,
                    new GUIContent("Reserved Bottom",
                        "The reserved space at the bottom of the mesh in which a mesh cannot be destructed."));

                if (_reservedBottomProp.floatValue <= 0 || _reservedTopProp.floatValue <= 0)
                {
                    EditorGUILayout.HelpBox("Reserved portions should always be greater than zero.",
                        MessageType.Warning);
                }
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("Spawn Destructible Global Mesh"))
            {
                if (Utilities.EnsurePlayMode())
                {
                    Utilities.ExecuteAction(
                        () => spawner.AddDestructibleGlobalMesh(MRUK.Instance.GetCurrentRoom()),
                        "Destructible meshes spawned.");
                }
            }

            if (GUILayout.Button("Clear Spawned Mesh"))
            {
                if (Utilities.EnsurePlayMode())
                {
                    Utilities.ExecuteAction(() => spawner.RemoveDestructibleGlobalMesh(),
                        "Spawned destructible meshes cleared.");
                }
            }

            if (spawner.PointsPerUnitX <= 0 || spawner.PointsPerUnitY <= 0 || spawner.PointsPerUnitZ <= 0)
            {
                EditorGUILayout.HelpBox("Points Per Unit should be greater than 0 for X, Y, and Z axes.",
                    MessageType.Warning);
            }

            serializedObject.ApplyModifiedProperties(); // Write back modified values and handle undo/redo
        }

    }
}
