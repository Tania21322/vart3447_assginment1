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

namespace Oculus.Interaction.Editor
{
    [CustomEditor(typeof(ConecastableCanvas))]
    public class ConecastableCanvasEditor : SimplifiedEditor
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            _editorDrawer.Draw("_inheritMeshFromParent", "_canvasMesh", "_surface", (inherit, canvasMesh, surface) =>
            {
                EditorGUILayout.PropertyField(inherit);
                var cc = target as ConecastableCanvas;
                var parent = cc.transform.parent == null ? null :
                             cc.transform.parent.GetComponentInParent<ConecastableCanvas>(true);
                // If set to inherit and there is a parent, hide the mesh and surface properties.
                if (inherit.boolValue && parent != null)
                {
                    EditorGUILayout.HelpBox($"Inheriting from {parent.gameObject.name}", MessageType.Info);
                    GUI.enabled = false;
                }

                EditorGUILayout.PropertyField(canvasMesh);
                EditorGUILayout.PropertyField(surface);
                GUI.enabled = true;
            });
        }
    }
}
