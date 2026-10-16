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

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(GazeInteractable)), CanEditMultipleObjects]
    public class GazeInteractableEditor : SimplifiedEditor
    {
        private List<SerializedProperty> _pointableElementProperties = new List<SerializedProperty>();
        private List<SerializedProperty> _movementProviderProperties = new List<SerializedProperty>();

        private static readonly string SingleWarningMsg = $"It looks like you want to use this {nameof(GazeInteractable)} for grab with an {nameof(IGrabbable)}, " +
                                                          $"but you did not specify an {nameof(IMovementProvider)}. Without it your grab interaction might not behave as expected.";

        private static readonly string MultipleWarningMsg = $"It looks like you want to use {nameof(GazeInteractable)}s for grab with an {nameof(IGrabbable)}, " +
                                                            $"but you did not specify an {nameof(IMovementProvider)} for one or more of the selected {nameof(GazeInteractable)}s." +
                                                            $" Without it your grab interactions might not behave as expected.";

        protected override void OnEnable()
        {
            base.OnEnable();

            _pointableElementProperties.Clear();
            _movementProviderProperties.Clear();

            foreach (Object interactable in targets)
            {
                SerializedObject so = new SerializedObject(interactable); ;
                _pointableElementProperties.Add(so.FindProperty("_pointableElement"));
                _movementProviderProperties.Add(so.FindProperty("_movementProvider"));
            }
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DrawGrabbableMovementWarning();
        }

        private void DrawGrabbableMovementWarning()
        {
            int numWarnings = GetWarningCount();

            if (numWarnings == 0)
            {
                return;
            }

            string buttonLabel = numWarnings > 1 ? "Fix All" : "Fix";
            string warningMsg = _pointableElementProperties.Count > 1 ? MultipleWarningMsg : SingleWarningMsg;

            GUILayout.BeginHorizontal();
            EditorGUILayout.HelpBox(warningMsg, MessageType.Warning);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(buttonLabel, GUILayout.MaxWidth(100), GUILayout.MaxHeight(40)))
            {
                FixAllWarnings();
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        private int GetWarningCount()
        {
            int warnings = 0;

            int count = _pointableElementProperties.Count;
            for (int i = 0; i < count; ++i)
            {
                SerializedProperty pointableProp = _pointableElementProperties[i];
                SerializedProperty movementProp = _movementProviderProperties[i];

                // Update so this serialized representation of the object
                // is in sync with any updates assigned in editor
                movementProp.serializedObject.Update();

                bool isUsedForGrab = pointableProp.objectReferenceValue is IGrabbable;
                bool hasMovementProvider = (movementProp.objectReferenceValue as IMovementProvider) != null;

                if (isUsedForGrab && !hasMovementProvider)
                {
                    warnings++;
                }
            }

            return warnings;
        }

        private void FixAllWarnings()
        {
            int count = _pointableElementProperties.Count;
            for (int i = 0; i < count; ++i)
            {
                SerializedProperty movementProp = _movementProviderProperties[i];

                bool isUsedForGrab = _pointableElementProperties[i].objectReferenceValue is IGrabbable;
                bool hasMovementProvider = (movementProp.objectReferenceValue as IMovementProvider) != null;

                if (isUsedForGrab && !hasMovementProvider)
                {
                    GazeInteractable interactable = movementProp.serializedObject.targetObject as GazeInteractable;

                    // Create a new component and wire up the reference
                    MoveFromTargetAtAnchorProvider movementProvider = Undo.AddComponent<MoveFromTargetAtAnchorProvider>(interactable.gameObject);
                    movementProp.objectReferenceValue = movementProvider;
                    movementProp.serializedObject.ApplyModifiedProperties();
                }
            }
        }
    }
}
