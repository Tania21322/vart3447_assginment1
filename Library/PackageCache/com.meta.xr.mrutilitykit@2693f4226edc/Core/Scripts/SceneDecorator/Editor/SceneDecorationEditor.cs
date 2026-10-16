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
using System.Collections.ObjectModel;
using Meta.XR.Util;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Meta.XR.MRUtilityKit.SceneDecorator
{
    /// <summary>
    /// Custom editor for SceneDecoration components that provides UI for managing masks and modifiers.
    /// </summary>
    [CustomEditor(typeof(SceneDecoration))]
    [Feature(Feature.Scene)]
    [Obsolete("SceneDecorator is deprecated and will be removed in a future version.")]
    public class SceneDecorationEditor : UnityEditor.Editor
    {
        private static readonly Type[] MaskTypes = new Type[]
        {
            typeof(AnchorComponentDistanceMask),
            typeof(AnchorDistanceMask),
            typeof(CellularNoiseMask),
            typeof(ColliderMask),
            typeof(CompositeMaskAdd),
            typeof(CompositeMaskAvg),
            typeof(CompositeMaskMax),
            typeof(CompositeMaskMin),
            typeof(CompositeMaskMul),
            typeof(ConstantMask),
            typeof(CookieMask),
            typeof(HeightMask),
            typeof(InsideCurrentRoomMask),
            typeof(NotInsideMask),
            typeof(RandomMask),
            typeof(RayDistanceMask),
            typeof(SimplexNoiseMask),
            typeof(SlopeMask),
            typeof(SpaceMapGPUMask),
            typeof(StochasticMask)
        };

        private static readonly Type[] ModifierTypes = new Type[]
        {
            typeof(DontDestroyOnLoadModifier),
            typeof(KeepUprightWithAnchorModifier),
            typeof(KeepUprightWithSurfaceModifier),
            typeof(RotationModifier),
            typeof(RotationModifierSpaceMap),
            typeof(ScaleModifier),
            typeof(ScaleUniformModifier)
        };

        private static readonly string[] ExcludedProperties = new string[] { "masks", "modifiers" };

        private bool _masksVisible;
        private bool _modifiersVisible;

        private GenericMenu _maskAddMenu;
        private GenericMenu _modifierAddMenu;

        private ReorderableList _maskList;
        private ReorderableList _modifierList;

        private void OnEnable()
        {
            _maskAddMenu = new GenericMenu();
            foreach (Type maskType in MaskTypes)
            {
                _maskAddMenu.AddItem(new GUIContent(maskType.Name), false, CreateMask, maskType);
            }

            _modifierAddMenu = new GenericMenu();
            foreach (Type modifierType in ModifierTypes)
            {
                _modifierAddMenu.AddItem(new GUIContent(modifierType.Name), false, CreateModifier, modifierType);
            }

            SerializedProperty arrayProp = serializedObject.FindProperty("masks");
            _maskList = new ReorderableList(arrayProp.serializedObject, arrayProp, true, false, true, true)
            {
                multiSelect = true,
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.x += 10;
                    rect.width -= 10;
                    EditorGUI.PropertyField(rect, _maskList.serializedProperty.GetArrayElementAtIndex(index));
                },
                elementHeightCallback = (int index) =>
                {
                    return EditorGUI.GetPropertyHeight(_maskList.serializedProperty.GetArrayElementAtIndex(index));
                },
                onAddCallback = (ReorderableList list) =>
                {
                    _maskAddMenu.ShowAsContext();
                },
                onRemoveCallback = (ReorderableList list) =>
                {
                    ReadOnlyCollection<int> deleteIndices = list.selectedIndices.Count > 0 ? list.selectedIndices : new ReadOnlyCollection<int>(new int[] { list.index });

                    foreach (int index in deleteIndices)
                    {
                        DeleteSubAsset(_maskList.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue);
                    }

                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                }
            };

            arrayProp = serializedObject.FindProperty("modifiers");
            _modifierList = new ReorderableList(arrayProp.serializedObject, arrayProp, true, false, true, true)
            {
                multiSelect = true,
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.x += 10;
                    rect.width -= 10;
                    EditorGUI.PropertyField(rect, _modifierList.serializedProperty.GetArrayElementAtIndex(index));
                },
                elementHeightCallback = (int index) =>
                {
                    return EditorGUI.GetPropertyHeight(_modifierList.serializedProperty.GetArrayElementAtIndex(index));
                },
                onAddCallback = (ReorderableList list) =>
                {
                    _modifierAddMenu.ShowAsContext();
                },
                onRemoveCallback = (ReorderableList list) =>
                {
                    ReadOnlyCollection<int> deleteIndices = list.selectedIndices.Count > 0 ? list.selectedIndices : new ReadOnlyCollection<int>(new int[] { list.index });

                    foreach (int index in deleteIndices)
                    {
                        DeleteSubAsset(_modifierList.serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue);
                    }

                    ReorderableList.defaultBehaviours.DoRemoveButton(list);
                }
            };
        }

        /// <summary>
        /// Renders the custom inspector GUI for SceneDecoration components.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, ExcludedProperties);

            _masksVisible = EditorGUILayout.Foldout(_masksVisible, "Masks", true);
            if (_masksVisible)
            {
                _maskList.DoLayoutList();
            }

            _modifiersVisible = EditorGUILayout.Foldout(_modifiersVisible, "Modifiers", true);
            if (_modifiersVisible)
            {
                _modifierList.DoLayoutList();
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void CreateMask(object userData)
        {
            Undo.RecordObjects(serializedObject.targetObjects, "Create Mask");

            Type maskType = (Type)userData;
            Mask mask = (Mask)ScriptableObject.CreateInstance(maskType);
            mask.name = maskType.Name;
            EditorUtility.SetDirty(mask);

            foreach (Object obj in serializedObject.targetObjects)
            {
                SceneDecoration sceneDecoration = (SceneDecoration)obj;
                Array.Resize(ref sceneDecoration.masks, sceneDecoration.masks.Length + 1);
                sceneDecoration.masks[sceneDecoration.masks.Length - 1] = mask;

                AssetDatabase.AddObjectToAsset(mask, obj);
                EditorUtility.SetDirty(obj);
            }

            AssetDatabase.SaveAssets();
        }

        private void CreateModifier(object userData)
        {
            Undo.RecordObjects(serializedObject.targetObjects, "Create Modifier");

            Type modifierType = (Type)userData;
            Modifier modifier = (Modifier)ScriptableObject.CreateInstance(modifierType);
            modifier.name = modifierType.Name;
            EditorUtility.SetDirty(modifier);

            foreach (Object obj in serializedObject.targetObjects)
            {
                SceneDecoration sceneDecoration = (SceneDecoration)obj;
                Array.Resize(ref sceneDecoration.modifiers, sceneDecoration.modifiers.Length + 1);
                sceneDecoration.modifiers[sceneDecoration.modifiers.Length - 1] = modifier;

                AssetDatabase.AddObjectToAsset(modifier, obj);
                EditorUtility.SetDirty(obj);
            }

            AssetDatabase.SaveAssets();
        }

        private void DeleteSubAsset(Object subAsset)
        {
            Undo.DestroyObjectImmediate(subAsset);

            foreach (Object obj in serializedObject.targetObjects)
            {
                EditorUtility.SetDirty(obj);
            }

            AssetDatabase.SaveAssets();
        }
    }
}
#endif
