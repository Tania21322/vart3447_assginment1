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

using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

namespace Oculus.Interaction.Editor.QuickActions
{
    /// <summary>
    /// Draws a mask field for InteractorTypes, ignoring the composite
    /// InteractorTypes.None and InteractorTypes.All in favor of Unity's own
    /// Nothing/Everything entries. Standard types plus Gaze are selectable;
    /// other experimental values remain hidden.
    /// </summary>
    [CustomPropertyDrawer(typeof(InteractorTypes))]
    public class InteractorTypesDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InteractorTypes[] visibleValues = Enum.GetValues(typeof(InteractorTypes))
                .Cast<InteractorTypes>()
                .Where(value => (value > InteractorTypes.None && value < InteractorTypes.All)
                    || value == InteractorTypes.Gaze)
                .ToArray();
            string[] options = visibleValues.Select(value => value.ToString()).ToArray();

            // MaskField works on option indices, so map the stored flags to/from them.
            int mask = 0;
            for (int i = 0; i < visibleValues.Length; i++)
            {
                if ((property.intValue & (int)visibleValues[i]) != 0)
                {
                    mask |= 1 << i;
                }
            }

            mask = EditorGUI.MaskField(position, label, mask, options);

            int result = 0;
            for (int i = 0; i < visibleValues.Length; i++)
            {
                if ((mask & (1 << i)) != 0)
                {
                    result |= (int)visibleValues[i];
                }
            }
            property.intValue = result;
        }
    }
}
