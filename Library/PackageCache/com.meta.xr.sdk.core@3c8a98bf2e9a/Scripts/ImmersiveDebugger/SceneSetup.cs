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
using Meta.XR.FovSimulator;
using Meta.XR.ImmersiveDebugger.Gizmo;
using Meta.XR.ImmersiveDebugger.Manager;
using Meta.XR.ImmersiveDebugger.UserInterface;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;
using FovSimulatorComponent = Meta.XR.FovSimulator.FovSimulator;
using Object = UnityEngine.Object;

namespace Meta.XR.ImmersiveDebugger
{
    internal static class SceneSetup
    {
        [RuntimeInitializeOnLoadMethod]
        private static void OnLoad()
        {
            if (!RuntimeSettings.Instance.ImmersiveDebuggerEnabled)
            {
                return;
            }
            if (RuntimeSettings.Instance.EnableOnlyInDebugBuild)
            {
#if !DEBUG
                return;
#endif // DEBUG
            }
            SetupImmersiveDebugger();
        }

        /// <summary>
        ///  Setup the scene with canvas and ImmersiveDebuggerManager, if not existed
        /// </summary>
        internal static void SetupImmersiveDebugger()
        {
            GizmoTypesRegistry.InitGizmos();

            GameObject manager = new GameObject("ImmersiveDebuggerManager");
            manager.AddComponent<DebugManager>();
            AttachFovSimulatorIfInspected(manager);
            // The Meta XR Operator tool binder is added by the DevAgent AI-Assistant setup
            // (LLMDialogPanelRegistrar), gated on the AI Assistant being enabled — not here.

            GameObject interfaceObject = new GameObject("ImmersiveDebuggerInterface");
            interfaceObject.transform.SetParent(manager.transform);
            interfaceObject.AddComponent<DebugInterface>();

            var customConfigType = Type.GetType(RuntimeSettings.Instance.CustomIntegrationConfigClassName);
            if (RuntimeSettings.Instance.UseCustomIntegrationConfig &&
                customConfigType != null)
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(customConfigType) &&
                    customConfigType.IsSubclassOf(typeof(CustomIntegrationConfigBase)))
                {
                    manager.AddComponent(customConfigType);
                }
                else
                {
                    Debug.LogWarning("CustomIntegrationConfig file is not an valid type");
                }
            }

            Object.DontDestroyOnLoad(manager);
        }

        internal static void AttachFovSimulatorIfInspected(GameObject owner)
        {
            var assets = RuntimeSettings.Instance.InspectedDataAssets;
            var enabled = RuntimeSettings.Instance.InspectedDataEnabled;
            for (var i = 0; i < assets.Count; i++)
            {
                if (i >= enabled.Count || !enabled[i] || assets[i] == null)
                {
                    continue;
                }

                foreach (var inspectedMember in assets[i].InspectedMembers)
                {
                    inspectedMember.Initialize();
                    if (!inspectedMember.Valid ||
                        inspectedMember.MemberInfo.DeclaringType != typeof(FovSimulatorComponent))
                    {
                        continue;
                    }

                    FovSimulatorLoader.ApplyConfiguredSetting();
                    if (Object.FindAnyObjectByType<FovSimulatorComponent>(FindObjectsInactive.Include) == null)
                    {
                        FovSimulatorComponent simulator = owner.AddComponent<FovSimulatorComponent>();
                        simulator.enabled = false;
                    }
                    return;
                }
            }
        }
    }
}
