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

using Oculus.Interaction;
using Oculus.Interaction.Editor.QuickActions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.BuildingBlocks.Editor
{
    internal class GazeInteractionInstallationRoutine : InstallationRoutine
    {
        protected override bool UsesPrefab => false;

        public enum InteractableVariant
        {
            GrabObject,
            UICanvas,
        }

        [SerializeField]
        [Variant(Description = "Select the type of Gaze interactable you want to create:\n" +
            "- Grab Object: Look and pinch at an object to grab it and move it around.\n" +
            "- UI Canvas: Look and pinch at a Canvas to select the buttons.")]
        public InteractableVariant variant = InteractableVariant.UICanvas;

        public override async Task<List<GameObject>> InstallAsync(BlockData blockData, GameObject selectedObject)
        {
            if (!InteractableInjectors.TryGetValue(variant, out GazeCreationData creationData))
            {
                throw new KeyNotFoundException(nameof(variant));
            }

            if (selectedObject == null)
            {
                selectedObject = await creationData.dummyCreator();
                if (selectedObject != null)
                {
                    Undo.RegisterCreatedObjectUndo(selectedObject, $"Create {selectedObject.name}");
                }
            }

            if (selectedObject == null)
            {
                throw new ArgumentNullException(nameof(selectedObject));
            }

            List<GameObject> blocks = creationData.interactionCreator(selectedObject).ToList();
            blocks.Where(block => block.GetComponent<GazeInteractable>() != null).ToList()
                .ForEach(block => block.name = $"{Utils.BlockPublicTag} {name}");
            selectedObject = blocks.First();
            Undo.RegisterFullObjectHierarchyUndo(selectedObject, $"Installing {nameof(GazeWizard)} on {selectedObject.name}");

            return new List<GameObject> { selectedObject };
        }

        internal override IReadOnlyCollection<InstallationStepInfo> GetInstallationSteps(VariantsSelection selection)
        {
            var installationSteps = new List<InstallationStepInfo>();
            installationSteps.AddRange(base.GetInstallationSteps(selection));
            if (InteractableInjectors.TryGetValue(variant, out GazeCreationData creationData) && creationData != null)
            {
                installationSteps.Add(new InstallationStepInfo(null, $"Run <b>{creationData.defaultName}</b> on the target object."));
            }
            return installationSteps;
        }

        private class GazeCreationData
        {
            public string defaultName;

            public delegate IEnumerable<GameObject> CreateInteractionDelegate(GameObject root);
            public delegate Task<GameObject> CreateDummyDelegate();

            public CreateInteractionDelegate interactionCreator;
            public CreateDummyDelegate dummyCreator;

            public GazeCreationData(string defaultName,
                CreateInteractionDelegate interactionCreator, CreateDummyDelegate dummyCreator)
            {
                this.defaultName = defaultName;
                this.interactionCreator = interactionCreator;
                this.dummyCreator = dummyCreator;
            }
        }

        private static readonly Dictionary<InteractableVariant, GazeCreationData>
           InteractableInjectors = new Dictionary<InteractableVariant, GazeCreationData>
           {
                {
                    InteractableVariant.GrabObject,
                    new GazeCreationData("Gaze grab",
                        (selectedObject) => QuickActionsWizard.CreateWithDefaults<GazeWizard>(selectedObject, true, (wizard) =>
                        {
                            wizard.InjectOptionalDeviceTypes(DeviceTypes.Hands);
                            wizard.InjectOptionalEnableRayFallback(true);
                        }),
                        async ()=> //dummy creator
                        {
                            var cubeBlockData = Utils.GetBlockData(BlockDataIds.Cube);
                            var cubeBlockObjects = await cubeBlockData.InstallWithDependencies();
                            return cubeBlockObjects.First();
                        })
                },
                {
                    InteractableVariant.UICanvas,
                    new GazeCreationData("Gaze UI",
                        (selectedObject) => QuickActionsWizard.CreateWithDefaults<GazeCanvasWizard>(selectedObject, true, (wizard) =>
                        {
                            wizard.FixMissingPointableCanvasModule();
                            wizard.InjectOptionalDeviceTypes(DeviceTypes.Hands);
                            wizard.InjectOptionalEnableRayFallback(true);
                            wizard.InjectOptionalAddToChildCanvases(true);

                        }),
                        async () => //dummy creator
                        {
                            await Task.CompletedTask;
                            GameObject dummy = new GameObject("Dummy Canvas");
                            return dummy;
                        })
                }
           };
    }
}
