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

/// <summary>
/// Custom editor for MRUKAnchor components that provides a specialized inspector interface
/// to display anchor properties and runtime information.
/// </summary>
[CustomEditor(typeof(MRUKAnchor))]
public class MRUKAnchorEditor : Editor
{
    private SerializedProperty _planeBoundary2DProperty;
    private SerializedProperty _childAnchorsProperty;
    private SerializedProperty _meshProperty;
    private SerializedProperty _parentAnchorProperty;
    private SerializedProperty _roomAnchor;

    private void OnEnable()
    {
        _planeBoundary2DProperty =
            serializedObject.FindProperty($"<{nameof(MRUKAnchor.PlaneBoundary2D)}>k__BackingField");
        _childAnchorsProperty = serializedObject.FindProperty($"<{nameof(MRUKAnchor.ChildAnchors)}>k__BackingField");
        _meshProperty = serializedObject.FindProperty("_mesh");
        _parentAnchorProperty = serializedObject.FindProperty($"<{nameof(MRUKAnchor.ParentAnchor)}>k__BackingField");
        _roomAnchor = serializedObject.FindProperty($"<{nameof(MRUKAnchor.Room)}>k__BackingField");
    }

    /// <summary>
    /// Draws the custom inspector GUI for MRUKAnchor components, displaying anchor properties
    /// and runtime information in a read-only format during play mode.
    /// </summary>
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        if (!Application.isPlaying)
        {
            return;
        }

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.ObjectField("Script", MonoScript.FromMonoBehaviour((MonoBehaviour)target), GetType(),
                false);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Info", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            var anchor = (MRUKAnchor)target;
            EditorGUILayout.LabelField("Anchor Label", anchor.Label.ToString());
            EditorGUILayout.LabelField("Plane Rect",
                anchor.PlaneRect.HasValue ? anchor.PlaneRect.Value.ToString() : "None");

            EditorGUILayout.LabelField("Volume Bounds",
                anchor.VolumeBounds.HasValue ? anchor.VolumeBounds.Value.ToString() : "None");

            EditorGUILayout.PropertyField(_planeBoundary2DProperty, new GUIContent("Plane Boundary 2D"));

            EditorGUILayout.LabelField("OVR Anchor", anchor.Anchor.ToString());
            if (anchor.Room != null)
            {
                EditorGUILayout.PropertyField(_roomAnchor, new GUIContent("Room"));
            }
            else
            {
                EditorGUILayout.LabelField("Room", "None");
            }

            EditorGUILayout.PropertyField(_meshProperty, new GUIContent("Mesh"));
            if (anchor.ParentAnchor != null)
            {
                EditorGUILayout.PropertyField(_parentAnchorProperty, new GUIContent("Parent Anchor"));
            }
            else
            {
                EditorGUILayout.LabelField("Parent Anchor", "None");
            }

            EditorGUILayout.PropertyField(_childAnchorsProperty, new GUIContent("Child Anchors"));
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
