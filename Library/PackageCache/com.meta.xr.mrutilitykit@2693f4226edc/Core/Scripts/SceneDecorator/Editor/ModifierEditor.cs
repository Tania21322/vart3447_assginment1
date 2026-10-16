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

#if UNITY_EDITOR
using System;
using Meta.XR.Util;
using UnityEditor;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    /// <summary>
    /// Custom editor for the Modifier class that provides a specialized inspector interface.
    /// </summary>
    [CustomEditor(typeof(Modifier), true)]
    [Feature(Feature.Scene)]
    [Obsolete("SceneDecorator is deprecated and will be removed in a future version.")]
    public class ModifierEditor : UnityEditor.Editor
    {
        /// <summary>
        /// Renders the custom inspector GUI for the Modifier component.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            using (var check = new EditorGUI.ChangeCheckScope())
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Name"));

                if (check.changed)
                {
                    serializedObject.ApplyModifiedProperties();
                }
            }

            DrawDefaultInspector();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
