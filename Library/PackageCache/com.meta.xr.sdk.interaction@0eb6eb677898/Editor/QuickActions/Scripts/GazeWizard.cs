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

using Oculus.Interaction.Surfaces;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor.QuickActions
{
    internal class GazeWizard : QuickActionsWizard
    {
        private const string MENU_NAME = MENU_FOLDER +
            "Add Gaze Grab Interaction";

        [MenuItem(MENU_NAME, priority = MenuOrder.GAZE + 1)]
        private static void OpenWizard()
        {
            ShowWindow<GazeWizard>(Selection.gameObjects[0]);
        }

        [MenuItem(MENU_NAME, true)]
        static bool Validate()
        {
            return Selection.gameObjects.Length == 1;
        }

        private static readonly Template _gazeHoverEffectTemplate =
            new Template("ISDK_GazeHoverEffect", "a0e1b1c1d1e1f100000000000000000b");

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
        [Tooltip("When enabled, Ray interactors will automatically fallback to interacting with this object " +
            "when gaze tracking is unavailable. This adds an ActiveStateTagSetFilter to Ray interactors and " +
            "creates a RayInteractable with ISDK_Gaze_Fallback tag.")]
        private bool _enableRayFallback = true;

        [SerializeField]
        [Tooltip("The transform to be moved when grabbing the object.")]
        [WizardDependency(FindMethod = nameof(FindTransform), FixMethod = nameof(FixTransform))]
        private Transform _targetTransform;

        [SerializeField]
        [Tooltip("The rigidbody representing the physics object that will be moved.")]
        [WizardDependency(FindMethod = nameof(FindRigidbody), FixMethod = nameof(FixRigidbody))]
        private Rigidbody _rigidbody;

        [SerializeField, Interface(typeof(IPointableElement))]
        [Tooltip("The grabbable that will receive the Interactable events and move the object.")]
        [WizardDependency(FindMethod = nameof(FindGrabbable), FixMethod = nameof(FixGrabbable))]
        private UnityEngine.Object _grabbable;
        private IPointableElement Grabbable { get; set; }

        [SerializeField]
        [InspectorName("Gaze Surface")]
        [Tooltip("This surface will be used for hit testing the gaze interaction. " +
            "If a collider should be used for hit testing instead of an ISurface, " +
            "leave this null and assign a collider to the 'Gaze Collider' field.")]
        [WizardDependency(Category = Category.Optional,
            FindMethod = nameof(FindSurface))]
        [Interface(typeof(ISurface))]
        private UnityEngine.Object _surface;

        [SerializeField]
        [InspectorName("Gaze Collider")]
        [Tooltip("If present, this collider will be used for gaze hit tests.")]
        [WizardDependency(Category = Category.Optional,
            FindMethod = nameof(FindCollider), FixMethod = nameof(FixCollider))]
        [ConditionalHide(nameof(_surface), null)]
        private Collider _collider;

        [SerializeField]
        [Tooltip("An optional movement provider that determines how the object moves when selected.")]
        [WizardDependency(Category = Category.Optional,
            FindMethod = nameof(FindMovementProvider))]
        [Interface(typeof(IMovementProvider))]
        private UnityEngine.Object _movementProvider;

        [SerializeField]
        [InspectorName("Source Mesh")]
        [Tooltip("If present, a Gaze Hover 3D spotlight effect will be painted on this mesh.")]
        [WizardDependency(Category = Category.Optional,
            FindMethod = nameof(FindMeshFilter))]
        private MeshFilter _meshFilter;

        #endregion Fields

        private void FindTransform()
        {
            _targetTransform = Target.GetComponent<Transform>();
        }

        private void FixTransform()
        {
            FindTransform();
        }

        private void FindRigidbody()
        {
            _rigidbody = Target.GetComponent<Rigidbody>();
        }

        private void FindGrabbable()
        {
            Grabbable = Target.GetComponent<Grabbable>();
            if (Grabbable == null)
            {
                Grabbable = Target.GetComponent<IPointableElement>();
            }
            _grabbable = Grabbable as UnityEngine.Object;
        }

        private void FixGrabbable()
        {
            Transform target = _rigidbody != null ? _rigidbody.transform : Target.transform;
            Grabbable grabbable = AddComponent<Grabbable>(Target);
            grabbable.InjectOptionalTargetTransform(target);
            FindGrabbable();
        }

        private void FixRigidbody()
        {
            _rigidbody = AddComponent<Rigidbody>(Target);
            _rigidbody.useGravity = false;
            _rigidbody.isKinematic = true;
        }

        private void FindSurface()
        {
            _surface = Target.GetComponent<ISurface>() as UnityEngine.Object;
        }

        private void FindCollider()
        {
            _collider = Target.GetComponentInChildren<Collider>();
        }

        private void FixCollider()
        {
            _collider = Utils.GenerateCollider(Target);
        }

        private void FindMovementProvider()
        {
            _movementProvider = Target.GetComponent<IMovementProvider>() as UnityEngine.Object;
        }

        private void FindMeshFilter()
        {
            _meshFilter = Target.GetComponentInChildren<MeshFilter>();
        }

        protected override void Create()
        {
            GameObject obj = Templates.CreateFromTemplate(
                Target.transform, Templates.GazeInteractable);

            Transform transform = obj.transform;
            transform.localPosition = Vector3.zero;
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;

            var gazeInteractable = obj.GetComponent<GazeInteractable>();

            ISurface surface = _surface as ISurface;
            if (surface == null)
            {
                var colliderSurface = AddComponent<ColliderSurface>(obj);
                colliderSurface.InjectCollider(_collider);
                surface = colliderSurface;
            }

            gazeInteractable.InjectSurface(surface);

            IMovementProvider movementProvider = _movementProvider as IMovementProvider;
            if (movementProvider == null)
            {
                MoveFromTargetProvider moveProvider = AddComponent<MoveFromTargetProvider>(obj);
                movementProvider = moveProvider;
            }
            IPointableElement pointable = Grabbable;

            gazeInteractable.InjectOptionalMovementProvider(movementProvider);
            gazeInteractable.InjectOptionalPointableElement(pointable);
            gazeInteractable.InjectOptionalMultiGrabScalingSupport(true);

            var gazeInteractors = InteractorUtils.AddInteractorsToRig(
                InteractorTypes.Gaze, _deviceTypes);

            foreach (var interactor in gazeInteractors)
            {
                UnityObjectAddedBroadcaster.HandleObjectWasAdded(interactor);
            }

            if (_enableRayFallback)
            {
                IActiveState gazeActiveState = InteractorUtils.GetGazeActiveState();
                ConfigureRayFallback(obj, gazeActiveState);
            }

            CreateGazeHoverEffect(gazeInteractable);
        }

        /// <summary>
        /// Optionally adds a Gaze Hover 3D spotlight effect painted on <see cref="_meshFilter"/>,
        /// wired to react to the newly created <paramref name="gazeInteractable"/>. Skips silently
        /// when no valid source mesh is available, as the effect is optional.
        /// </summary>
        private void CreateGazeHoverEffect(GazeInteractable gazeInteractable)
        {
            if (_meshFilter == null || _meshFilter.sharedMesh == null)
            {
                return;
            }

            // Two-step parenting: instantiate under the MeshFilter so the effect inherits the
            // source mesh's world transform, then re-parent under Target (worldPositionStays)
            // so the effect lives where the designer authored it.
            GameObject instance = Templates.CreateFromTemplate(_meshFilter.transform, _gazeHoverEffectTemplate);
            instance.transform.SetParent(_meshFilter.transform);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localScale = Vector3.one;
            instance.transform.localRotation = Quaternion.identity;

            MeshFilter meshFilter = instance.GetComponent<MeshFilter>();
            meshFilter.sharedMesh = _meshFilter.sharedMesh;

            InteractableGazeHover3DVisual visual = instance.GetComponent<InteractableGazeHover3DVisual>();

            // The created GazeInteractable is itself an IPointable and is the sole driver of
            // the effect. RecordObject so the assignment is replayed on redo (the whole wizard
            // Create collapses into one undo group).
            Undo.RecordObject(visual, "Set Gaze Hover 3D Pointable");
            visual.InjectPointables(new List<IPointable> { gazeInteractable });

            EditorUtility.SetDirty(visual);
        }

        /// <summary>
        /// Configures Ray fallback by adding a fallback tag to existing RayInteractable or creating a new one.
        /// Searches for existing RayInteractable in target/parent/children hierarchy.
        /// If found, adds TagSet to make it work as fallback. If not found, creates a new RayInteractable with default settings.
        /// </summary>
        private void ConfigureRayFallback(GameObject gazeInteractableObj, IActiveState gazeActiveState)
        {
            // Find existing RayInteractable in hierarchy
            RayInteractable existingRayInteractable = Target.GetComponent<RayInteractable>();
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
                var objects = CreateWithDefaults<RayGrabWizard>(Target, true, (wizard) =>
                {
                    wizard.InjectOptionalDeviceTypes(_deviceTypes);
                    wizard.InjectOptionalTargetTransform(_targetTransform);
                    wizard.InjectOptionalRigidbody(_rigidbody);
                    wizard.InjectOptionalGrabbable(Grabbable);
                    wizard.InjectOptionalCollider(_collider);
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

            if (_collider == null && _surface == null)
            {
                void FindOrFixCollider()
                {
                    FindCollider();
                    if (_collider == null)
                    {
                        FixCollider();
                    }
                }
                result = result.Append(new MessageData(MessageType.Error,
                    "A Collider or a Surface must be provided for gaze hit testing. " +
                    "Assign either an ISurface or a Collider to the appropriate fields, or " +
                    "press the Fix button to generate a collider.",
                    new ButtonData("Fix", FindOrFixCollider)));
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

        public void InjectOptionalTargetTransform(Transform targetTransform)
        {
            _targetTransform = targetTransform;
        }

        public void InjectOptionalRigidbody(Rigidbody rigidbody)
        {
            _rigidbody = rigidbody;
        }

        public void InjectOptionalGrabbable(IPointableElement grabbable)
        {
            _grabbable = grabbable as UnityEngine.Object;
            Grabbable = grabbable;
        }

        public void InjectOptionalSurface(ISurface surface)
        {
            _surface = surface as UnityEngine.Object;
        }

        public void InjectOptionalCollider(Collider collider)
        {
            _collider = collider;
        }

        public void InjectOptionalMovementProvider(IMovementProvider movementProvider)
        {
            _movementProvider = movementProvider as UnityEngine.Object;
        }

        public void InjectOptionalMeshFilter(MeshFilter meshFilter)
        {
            _meshFilter = meshFilter;
        }

        #endregion
    }
}
