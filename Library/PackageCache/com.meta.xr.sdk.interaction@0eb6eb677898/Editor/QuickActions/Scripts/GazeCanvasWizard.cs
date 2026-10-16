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
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using Oculus.Interaction.Surfaces;

namespace Oculus.Interaction.Editor.QuickActions
{
    internal class GazeCanvasWizard : QuickActionsWizard
    {
        private const string MENU_NAME = MENU_FOLDER +
            "Add Gaze Interaction to Canvas";

        [MenuItem(MENU_NAME, priority = MenuOrder.GAZE)]
        private static void OpenWizard()
        {
            ShowWindow<GazeCanvasWizard>(Selection.gameObjects[0]);
        }

        [MenuItem(MENU_NAME, true)]
        static bool Validate()
        {
            return Selection.gameObjects.Length == 1;
        }

        #region Fields

        [SerializeField]
        [DeviceType, WizardSetting]
        [InspectorName("Add Required Interactor(s)")]
        [Tooltip("The interactors required for the new interactable will be " +
            "added for the device types selected here, if not already present.")]
        private DeviceTypes _deviceTypes = DeviceTypes.Hands;

        [SerializeField]
        [WizardSetting]
        [InspectorName("Enable Ray Fallback")]
        [Tooltip("When enabled, Ray interactors will automatically fallback to interacting with this canvas " +
            "when gaze tracking is unavailable. This adds an ActiveStateTagSetFilter to Ray interactors and " +
            "creates a RayInteractable with ISDK_Gaze_Fallback tag.")]
        private bool _enableRayFallback = true;

        [SerializeField]
        [Tooltip("The canvas to make Gaze interactable.")]
        [WizardDependency(ReadOnly = true,
            FindMethod = nameof(FindCanvas),
            FixMethod = nameof(FixCanvas))]
        private Canvas _canvas;

        [SerializeField]
        [WizardSetting]
        [InspectorName("Conecast on Child Canvases")]
        [Tooltip("When enabled, canvases that are children of the provided Canvas will also " +
            "be made conecastable.")]
        private bool _addToChildCanvases = true;

        #endregion Fields

        private void FindCanvas()
        {
            _canvas = Target.GetComponent<Canvas>();
        }

        private void FixCanvas()
        {
            _canvas = AddComponent<Canvas>(Target);
            _canvas.renderMode = RenderMode.WorldSpace;
        }

        protected override void Create()
        {
            GameObject obj = Templates.CreateFromTemplate(
                Target.transform, Templates.GazeCanvasInteractable);

            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;

            if (_canvas.GetComponent<GraphicRaycaster>() == null)
            {
                AddComponent<GraphicRaycaster>(_canvas.gameObject);
            }

            PointableCanvas pointableCanvas = obj.GetComponent<PointableCanvas>();
            pointableCanvas.InjectCanvas(_canvas);

            ConecastableCanvas AddConecastableCanvas(Canvas canvas)
            {
                ConecastableCanvas conecastableCanvas = canvas.GetComponent<ConecastableCanvas>();
                if (conecastableCanvas == null)
                {
                    conecastableCanvas = AddComponent<ConecastableCanvas>(canvas.gameObject);
                }
                return conecastableCanvas;
            }

            var rootConecastableCanvas = AddConecastableCanvas(_canvas);
            if (_addToChildCanvases)
            {
                foreach (var childCanvas in _canvas.GetComponentsInChildren<Canvas>())
                {
                    AddConecastableCanvas(childCanvas);
                }
            }

            GazeInteractable gazeInteractable = obj.GetComponent<GazeInteractable>();
            if (gazeInteractable != null)
            {
                gazeInteractable.InjectOptionalRootCanvas(rootConecastableCanvas);
            }

            var gazeInteractors = InteractorUtils.AddInteractorsToRig(
                InteractorTypes.Gaze, _deviceTypes);

            if (_enableRayFallback)
            {
                IActiveState gazeActiveState = InteractorUtils.GetGazeActiveState();
                ConfigureRayFallback(obj, gazeActiveState);
            }
        }

        /// <summary>
        /// Configures Ray fallback by adding a fallback tag to existing RayInteractable or creating a new one.
        /// Searches for existing RayInteractable in target/parent/children hierarchy.
        /// If found, adds TagSet to make it work as fallback. If not found, creates a new RayInteractable with default settings.
        /// </summary>
        private void ConfigureRayFallback(GameObject gazeInteractableObj, IActiveState gazeActiveState)
        {
            PointableCanvas pointableCanvas = gazeInteractableObj.GetComponent<PointableCanvas>();
            RayInteractable existingRayInteractable = null;

            if (pointableCanvas != null)
            {
                existingRayInteractable = pointableCanvas.Canvas.GetComponent<RayInteractable>();
            }

            if (existingRayInteractable == null)
            {
                existingRayInteractable = Target.GetComponent<RayInteractable>();
            }
            if (existingRayInteractable == null)
            {
                existingRayInteractable = Target.GetComponentInChildren<RayInteractable>(true);
            }
            if (existingRayInteractable == null)
            {
                existingRayInteractable = Target.GetComponentInParent<RayInteractable>();
            }

            GameObject rayInteractableObj;

            if (existingRayInteractable != null)
            {
                rayInteractableObj = existingRayInteractable.gameObject;
            }
            else
            {
                var objects = CreateWithDefaults<RayCanvasWizard>(Target, true, (wizard) =>
                {
                    wizard.InjectOptionalDeviceTypes(_deviceTypes);
                    wizard.InjectOptionalCanvas(_canvas);
                });
                rayInteractableObj = objects.First();

            }

            TagSet tagSet = rayInteractableObj.GetComponent<TagSet>();
            if (tagSet == null)
            {
                tagSet = rayInteractableObj.AddComponent<TagSet>();
            }

            if (!tagSet.ContainsTag(GazeQuickActionsConstants.GAZE_RAY_FALLBACK_TAG))
            {
                SerializedObject tagSetObj = new SerializedObject(tagSet);
                SerializedProperty tagsProperty = tagSetObj.FindProperty("_tags");
                tagsProperty.InsertArrayElementAtIndex(tagsProperty.arraySize);
                SerializedProperty newTagProperty = tagsProperty.GetArrayElementAtIndex(tagsProperty.arraySize - 1);
                newTagProperty.stringValue = GazeQuickActionsConstants.GAZE_RAY_FALLBACK_TAG;
                tagSetObj.ApplyModifiedProperties();
            }

            if (gazeActiveState != null)
            {
                InteractorUtils.ConfigureRayInteractorsForGazeFallback(gazeActiveState, _deviceTypes);
            }
        }

        protected override IEnumerable<MessageData> GetMessages()
        {
            var result = Enumerable.Empty<MessageData>();

            result = result.Concat(Messages
                .MissingPointableCanvasModule<GazeInteractor>());

            if (Target.GetComponent<Canvas>() == null)
            {
                result = result.Append(new MessageData(MessageType.Error,
                    "The target object must have a Canvas attached."));
            }
            else if (_canvas != null && _canvas.renderMode != RenderMode.WorldSpace)
            {
                result = result.Append(new MessageData(MessageType.Error,
                    "The provided canvas must be in World space.",
                    new ButtonData("Fix", () => _canvas.renderMode = RenderMode.WorldSpace)));
            }

            return result;
        }

        #region Injects

        public void InjectOptionalDeviceTypes(DeviceTypes deviceTypes)
        {
            _deviceTypes = deviceTypes;
        }

        public void InjectOptionalEnableRayFallback(bool enableRayFallback)
        {
            _enableRayFallback = enableRayFallback;
        }

        public void InjectOptionalCanvas(Canvas canvas)
        {
            _canvas = canvas;
        }

        public void InjectOptionalAddToChildCanvases(bool addToChildCanvases)
        {
            _addToChildCanvases = addToChildCanvases;
        }

        #endregion
    }
}
