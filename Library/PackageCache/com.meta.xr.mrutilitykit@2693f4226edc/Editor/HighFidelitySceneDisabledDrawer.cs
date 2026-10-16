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

[CustomPropertyDrawer(typeof(HighFidelitySceneDisabledAttribute))]
public class HighFidelitySceneDisabledDrawer : PropertyDrawer
{
    private const string _warning =
        "High Fidelity scene (V2) is currently disabled. This option has no effect; V1 will always be loaded.";

    private const float _spacing = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        if (property.propertyType == SerializedPropertyType.Boolean && property.boolValue)
        {
            property.boolValue = false;
        }

        var warningContent = new GUIContent(_warning, EditorGUIUtility.IconContent("console.warnicon").image);
        var warningHeight = GetWarningHeight(position.width);
        var warningRect = new Rect(position.x, position.y, position.width, warningHeight);
        EditorGUI.LabelField(warningRect, warningContent, EditorStyles.helpBox);

        var toggleRect = new Rect(position.x, position.y + warningHeight + _spacing, position.width,
            EditorGUIUtility.singleLineHeight);
        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.PropertyField(toggleRect, property, label);
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return GetWarningHeight(EditorGUIUtility.currentViewWidth) + _spacing + EditorGUIUtility.singleLineHeight;
    }

    private float GetWarningHeight(float width)
    {
        var warningContent = new GUIContent(_warning, EditorGUIUtility.IconContent("console.warnicon").image);
        return Mathf.Max(EditorGUIUtility.singleLineHeight * 2f,
            EditorStyles.helpBox.CalcHeight(warningContent, width));
    }
}
