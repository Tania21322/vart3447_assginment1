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
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Oculus.Interaction.Input;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Locomotion;
using InputModality = Oculus.Interaction.Editor.QuickActions.InteractorTemplate.InputModality;
using Object = UnityEngine.Object;
using UnityEditor;

namespace Oculus.Interaction.Editor.QuickActions
{
    [Flags]
    internal enum InteractorTypes
    {
        None = 0,
        Poke = 1 << 0,
        Grab = 1 << 1,
        Ray = 1 << 2,
        DistanceGrab = 1 << 3,
        Teleport = 1 << 4,
        All = (1 << 5) - 1,
        //Experimental types will be hidden by the UI drawer
        TouchGrab = 1 << 5,
        Gaze = 1 << 6,
    }

    [Flags]
    internal enum DeviceTypes
    {
        None = 0,
        Hands = 1 << 0,
        Controllers = 1 << 1,
        ControllerDrivenHands = 1 << 2,
        All = (1 << 3) - 1,
    }

    internal static class InteractorUtils
    {
        public const string HAND_INTERACTOR_PARENT_NAME = "HandInteractors";
        public const string CONTROLLER_INTERACTOR_PARENT_NAME = "ControllerInteractors";
        public const string CONTROLLERHAND_INTERACTOR_PARENT_NAME = "ControllerHandInteractors";

        private static readonly Dictionary<InteractorTypes, Type> _handTypeLookup = new()
        {
            [InteractorTypes.Poke] = typeof(PokeInteractor),
            [InteractorTypes.Grab] = typeof(HandGrabInteractor),
            [InteractorTypes.Ray] = typeof(RayInteractor),
            [InteractorTypes.DistanceGrab] = typeof(DistanceHandGrabInteractor),
            [InteractorTypes.Teleport] = typeof(TeleportInteractor),
            [InteractorTypes.TouchGrab] = typeof(TouchHandGrabInteractor),
            [InteractorTypes.Gaze] = typeof(GazeInteractor)
        };

        private static readonly Dictionary<InteractorTypes, Type> _controllerTypeLookup = new()
        {
            [InteractorTypes.Poke] = typeof(PokeInteractor),
            [InteractorTypes.Grab] = typeof(GrabInteractor),
            [InteractorTypes.Ray] = typeof(RayInteractor),
            [InteractorTypes.DistanceGrab] = typeof(DistanceGrabInteractor),
            [InteractorTypes.Teleport] = typeof(TeleportInteractor),
            [InteractorTypes.Gaze] = typeof(GazeInteractor)
        };

        private static readonly Dictionary<InteractorTypes, Type> _controllerHandTypeLookup = new()
        {
            [InteractorTypes.Poke] = typeof(PokeInteractor),
            [InteractorTypes.Grab] = typeof(HandGrabInteractor),
            [InteractorTypes.Ray] = typeof(RayInteractor),
            [InteractorTypes.DistanceGrab] = typeof(DistanceHandGrabInteractor),
            [InteractorTypes.Teleport] = typeof(TeleportInteractor),
            [InteractorTypes.TouchGrab] = typeof(TouchHandGrabInteractor),
            [InteractorTypes.Gaze] = typeof(GazeInteractor)
        };

        private static readonly Dictionary<InputModality, string> _modalityGroupNames = new()
        {
            [InputModality.Hand] = "Hand",
            [InputModality.Controller] = "Controller",
            [InputModality.HandAndController] = "Controller and Hand",
            [InputModality.HandAndNoController] = "Hand and No Controller",
            [InputModality.ControllerAndNoHand] = "Controller and No Hand",
            [InputModality.Any] = ""
        };

        /// <summary>
        /// Get the Type of a Hand interactor associated with an
        /// <see cref="InteractorTypes"/> value
        /// </summary>
        public static bool TryGetTypeForHandInteractor(
            InteractorTypes interactorType, out Type type)
        {
            return _handTypeLookup.TryGetValue(interactorType, out type);
        }

        /// <summary>
        /// Get the Type of a Controller interactor associated with an
        /// <see cref="InteractorTypes"/> value
        /// </summary>
        public static bool TryGetTypeForControllerInteractor(
            InteractorTypes interactorType, out Type type)
        {
            return _controllerTypeLookup.TryGetValue(interactorType, out type);
        }

        /// <summary>
        /// Get the Type of a Controller Driven Hand interactor associated with an
        /// <see cref="InteractorTypes"/> value
        /// </summary>
        public static bool TryGetTypeForControllerHandInteractor(
            InteractorTypes interactorType, out Type type)
        {
            return _controllerHandTypeLookup.TryGetValue(interactorType, out type);
        }

        /// <summary>
        /// Get the Type of am interactor associated with an
        /// <see cref="InteractorTypes"/> value for a specific
        /// <see cref="DeviceTypes"/> device.
        /// </summary>
        public static bool TryGetTypeForInteractor(DeviceTypes device,
             InteractorTypes interactorType, out Type type)
        {
            switch (device)
            {
                case DeviceTypes.Hands:
                    return TryGetTypeForHandInteractor(interactorType, out type);
                case DeviceTypes.Controllers:
                    return TryGetTypeForControllerInteractor(interactorType, out type);
                case DeviceTypes.ControllerDrivenHands:
                    return TryGetTypeForControllerHandInteractor(interactorType, out type);
                default:
                    type = default;
                    return false;
            }
        }

        /// <summary>
        /// Find the default interactor group and transform holder that serves as a parent to
        /// the ISDK interactors
        /// </summary>
        public static bool TryFindInteractorsGroup<TData>(DataSource<TData> dataSource, out InteractorGroup group, out Transform holder)
            where TData : class, ICopyFrom<TData>, new()
        {
            //search down the hierarchy
            group = dataSource.GetComponentInChildren<InteractorGroup>();
            if (group != null)
            {
                holder = group.transform;
                return true;
            }

            //search up the hierarchy
            group = dataSource.GetComponentInParent<InteractorGroup>();
            if (group != null)
            {
                holder = group.transform;
                return true;
            }

            //search for potential children of the next step in the DataStack
            Hmd hmd = GetHmd();
            if (hmd == null)
            {
                holder = null;
                return false;
            }
            Transform root = hmd.transform.parent;
            if (root == null)
            {
                holder = null;
                return false;
            }

            foreach (Transform child in root.transform)
            {
                if (child.TryGetComponent(out DataModifier<TData> modifier)
                    && modifier.ModifyDataFromSource == dataSource as IDataSource<TData>)
                {
                    if (TryFindInteractorsGroup(modifier, out group, out holder))
                    {
                        return true;
                    }
                }
            }

            //if everything else fails. Look for the names right under the data source
            foreach (Transform child in dataSource.transform)
            {
                if (child.gameObject.name.StartsWith(HAND_INTERACTOR_PARENT_NAME) ||
                    child.gameObject.name.StartsWith(CONTROLLER_INTERACTOR_PARENT_NAME) ||
                    child.gameObject.name.StartsWith(CONTROLLERHAND_INTERACTOR_PARENT_NAME))
                {
                    group = null;
                    holder = child;
                    return true;
                }
            }

            holder = null;
            return false;
        }

        /// <summary>
        /// Get all the interactors in <see cref="InteractorGroup.Interactors"/>,
        /// using reflection to get the backing list due to the public property
        /// only being populated at runtime.
        /// </summary>
        /// <param name="group"></param>
        /// <returns>The <see cref="IInteractor"/>s within the group</returns>
        public static IEnumerable<IInteractor> GetInteractorsFromGroup(InteractorGroup group)
        {
            FieldInfo field = group.GetType().GetField("_interactors",
                BindingFlags.NonPublic | BindingFlags.Instance);
            var curValue = field.GetValue(group) as List<Object>;
            return curValue == null ? Enumerable.Empty<IInteractor>() : curValue
                .Where(o => o != null)
                .ToList().ConvertAll(o => o as IInteractor);
        }
        /// <summary>
        /// Looks for an specific interactor type and input modality within a hierarchy
        /// </summary>
        /// <param name="root">Root of the hierarchy to look for interactors</param>
        /// <param name="deviceTypes">Device using the interactor</param>
        /// <param name="interactorTypes">Interactor type being searched</param>
        /// <param name="inputModality">Input modality expected for the interactor</param>
        /// <returns>True if the interactor with the input modality exists within the hierarchy</returns>
        public static bool HasInteractorWithModality(Transform root,
            DeviceTypes deviceTypes, InteractorTypes interactorTypes, InputModality inputModality)
        {
            if (!TryGetTypeForInteractor(deviceTypes, interactorTypes, out Type iType))
            {
                return false;
            }

            bool found = false;
            foreach (Component component in root.GetComponentsInChildren(iType, true))
            {
                bool hasHand = component.TryGetComponent<IHand>(out _);
                bool hasController = component.TryGetComponent<IController>(out _);
                switch (inputModality)
                {
                    case InputModality.Hand:
                    case InputModality.HandAndNoController:
                        found |= (hasHand && !hasController); break;
                    case InputModality.Controller:
                    case InputModality.ControllerAndNoHand:
                        found |= (hasController && !hasHand); break;
                    case InputModality.HandAndController:
                        found |= (hasHand && hasController); break;
                }
                if (found)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get any Hand <see cref="InteractorTypes"/> that may
        /// already exist under a Transform.
        /// </summary>
        public static InteractorTypes GetExistingHandInteractors(Transform root)
        {
            InteractorTypes result = 0;
            foreach (InteractorTypes type in Enum.GetValues(typeof(InteractorTypes)))
            {
                //the type must be a hand interactor type
                if (!TryGetTypeForHandInteractor(type, out Type iType))
                {
                    continue;
                }

                bool found = false;

                //the component must exist and have a hand and not a controller reference
                foreach (Component component in root.GetComponentsInChildren(iType, true))
                {
                    if (component.TryGetComponent<IHand>(out _)
                        && !component.TryGetComponent<IController>(out _))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    result |= type;
                }

            }
            return result;
        }

        /// <summary>
        /// Get any Controller <see cref="InteractorTypes"/> that may
        /// already exist under a Transform.
        /// </summary>
        public static InteractorTypes GetExistingControllerInteractors(Transform root)
        {
            InteractorTypes result = 0;
            foreach (InteractorTypes type in Enum.GetValues(typeof(InteractorTypes)))
            {
                //the type must be a hand interactor type
                if (!TryGetTypeForControllerInteractor(type, out Type iType))
                {
                    continue;
                }

                //the component must exist and have a controller reference and not a hand reference
                bool found = false;
                foreach (Component component in root.GetComponentsInChildren(iType, true))
                {
                    if (component.TryGetComponent<IController>(out _)
                        && !component.TryGetComponent<IHand>(out _))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    result |= type;
                }
            }
            return result;
        }

        /// <summary>
        /// Get any Controller Driven Hand <see cref="InteractorTypes"/> that may
        /// already exist under a Transform.
        /// </summary>
        public static InteractorTypes GetExistingControllerHandInteractors(Transform root)
        {
            InteractorTypes result = 0;
            foreach (InteractorTypes type in Enum.GetValues(typeof(InteractorTypes)))
            {
                //the type must be a hand interactor type
                if (!TryGetTypeForControllerHandInteractor(type, out Type iType))
                {
                    continue;
                }

                //the component must exist and have a controller and Hand reference
                bool found = false;
                foreach (Component component in root.GetComponentsInChildren(iType, true))
                {
                    if (component.TryGetComponent<IController>(out _)
                        && component.TryGetComponent<IHand>(out _))
                    {
                        found = true;
                        break;
                    }
                }

                if (found)
                {
                    result |= type;
                }
            }
            return result;
        }

        /// <summary>
        /// Verify that rig contains correct object structure for auto-adding
        /// interactors to Controllers
        /// </summary>
        public static bool CanAddControllerInteractorsToRig()
        {
            return GetHmd() != null
                && GetControllers().Any();
        }

        /// <summary>
        /// Verify that rig contains correct object structure for auto-adding
        /// interactors to Hands
        /// </summary>
        public static bool CanAddHandInteractorsToRig()
        {
            return GetHmd() != null
                && GetHands().Any();
        }

        /// <summary>
        /// Verify that rig contains correct object structure for auto-adding
        /// interactors to Controller Driven Hands
        /// </summary>
        public static bool CanAddControllerHandInteractorsToRig()
        {
            return GetHmd() != null
                && GetControllerHands().Any();
        }

        /// <summary>
        /// Add a set of interactors to a rig within the provided devices.
        /// </summary>
        /// <param name="interactors">The interactors to add</param>
        /// <param name="devices">The devices to add the interactors to</param>
        /// <returns>A collection of any added objects</returns>
        public static IEnumerable<GameObject> AddInteractorsToRig(
            InteractorTypes interactors, DeviceTypes devices)
        {
            List<GameObject> newInteractors = new List<GameObject>();

            if (devices.HasFlag(DeviceTypes.Hands)
                && CanAddHandInteractorsToRig())
            {
                foreach (var hand in GetHands())
                {
                    if (TryFindInteractorsGroup(hand, out InteractorGroup group, out Transform holder))
                    {
                        var result = AddInteractorsToHand(
                            interactors, hand, GetHmd(), holder, group);
                        newInteractors.AddRange(result);
                    }
                }
            }

            if (devices.HasFlag(DeviceTypes.Controllers)
                && CanAddControllerInteractorsToRig())
            {
                foreach (var controller in GetControllers())
                {
                    if (TryFindInteractorsGroup(controller, out InteractorGroup group, out Transform holder))
                    {
                        var result = AddInteractorsToController(
                            interactors, controller, GetHmd(), holder, group);
                        newInteractors.AddRange(result);
                    }
                }
            }

            if (devices.HasFlag(DeviceTypes.ControllerDrivenHands)
                && CanAddControllerHandInteractorsToRig())
            {
                foreach (var controllerHand in GetControllerHands())
                {
                    if (TryFindInteractorsGroup(controllerHand.Item1, out InteractorGroup group, out Transform holder))
                    {
                        var result = AddInteractorsToControllerHand(
                            interactors, controllerHand.Item1, controllerHand.Item2, GetHmd(), holder, group);
                        newInteractors.AddRange(result);
                    }
                }
            }

            return newInteractors;
        }

        /// <summary>
        /// Adds interactor(s) to a Hand
        /// </summary>
        /// <param name="types">The interactor types to add</param>
        /// <returns>A collection of the added interactors</returns>
        public static IEnumerable<GameObject> AddInteractorsToHand(InteractorTypes types,
            Hand hand, Hmd hmd, Transform parentTransform, InteractorGroup group = null)
        {
            List<GameObject> newInteractors = new List<GameObject>();
            foreach (InteractorTypes interactor in Enum.GetValues(typeof(InteractorTypes)))
            {
                if (types.HasFlag(interactor) &&
                    Templates.TryGetHandInteractorTemplate(interactor, out var template) &&
                    !HasInteractorWithModality(parentTransform, DeviceTypes.Hands, interactor, template.Modality))
                {
                    GameObject newInteractor = AddInteractor(template, hmd, parentTransform, group);
                    newInteractor.GetComponent<HandRef>().InjectHand(hand);

                    if (interactor == InteractorTypes.Gaze)
                    {
                        WireGazeInteractor(newInteractor);
                    }

                    newInteractors.Add(newInteractor);
                }
            }
            return newInteractors;
        }

        /// <summary>
        /// Adds interactor(s) to a Controller
        /// </summary>
        /// <param name="types">The interactor types to add</param>
        /// <returns>A collection of the added interactors</returns>
        public static IEnumerable<GameObject> AddInteractorsToController(InteractorTypes types,
            Controller controller, Hmd hmd, Transform parentTransform, InteractorGroup group = null)
        {
            List<GameObject> newInteractors = new List<GameObject>();
            foreach (InteractorTypes interactor in Enum.GetValues(typeof(InteractorTypes)))
            {
                if (types.HasFlag(interactor) &&
                    Templates.TryGetControllerInteractorTemplate(interactor, out var template) &&
                    !HasInteractorWithModality(parentTransform, DeviceTypes.Controllers, interactor, template.Modality))
                {
                    GameObject newInteractor = AddInteractor(template, hmd, parentTransform, group);
                    newInteractor.GetComponent<ControllerRef>().InjectController(controller);

                    if (interactor == InteractorTypes.Gaze)
                    {
                        WireGazeInteractor(newInteractor);
                    }

                    newInteractors.Add(newInteractor);
                }
            }
            return newInteractors;
        }

        /// <summary>
        /// Adds interactor(s) to a Controller Driven Hand
        /// </summary>
        /// <param name="types">The interactor types to add</param>
        /// <returns>A collection of the added interactors</returns>
        public static IEnumerable<GameObject> AddInteractorsToControllerHand(InteractorTypes types,
            Controller controller, Hand hand, Hmd hmd, Transform parentTransform, InteractorGroup group = null)
        {
            List<GameObject> newInteractors = new List<GameObject>();
            foreach (InteractorTypes interactor in Enum.GetValues(typeof(InteractorTypes)))
            {
                if (types.HasFlag(interactor) &&
                    Templates.TryGetControllerHandInteractorTemplate(interactor, out var template) &&
                    !HasInteractorWithModality(parentTransform, DeviceTypes.ControllerDrivenHands, interactor, template.Modality))
                {
                    GameObject newInteractor = AddInteractor(template, hmd, parentTransform, group);
                    if (newInteractor.TryGetComponent(out ControllerRef controllerRef))
                    {
                        controllerRef.InjectController(controller);
                    }
                    if (newInteractor.TryGetComponent(out HandRef handRef))
                    {
                        handRef.InjectHand(hand);
                    }

                    if (interactor == InteractorTypes.Gaze)
                    {
                        WireGazeInteractor(newInteractor);
                    }

                    newInteractors.Add(newInteractor);
                }
            }
            return newInteractors;
        }

        /// <summary>
        /// Wires a newly created gaze interactor to the scene's eye-gaze system,
        /// creating that system if it does not yet exist. Component look-ups are
        /// guarded so this is a no-op for any component the interactor lacks.
        /// </summary>
        private static void WireGazeInteractor(GameObject newInteractor)
        {
            EyeGaze eyeGaze = CreateEyeGazeSystem();
            if (eyeGaze != null && newInteractor.TryGetComponent<EyeGazeRef>(out var gazeRef))
            {
                gazeRef.InjectAllEyeGazeRef(eyeGaze);
            }
            GazeConecaster gazeConecaster = GetGazeConecaster();
            if (gazeConecaster != null && newInteractor.TryGetComponent<GazeInteractor>(out var gazeInteractor))
            {
                gazeInteractor.InjectHitTester(gazeConecaster);
            }
        }

        internal static GameObject AddInteractor(InteractorTemplate template, Hmd hmd,
            Transform parentTransform, InteractorGroup group = null)
        {
            parentTransform = GetModalityGroup(template.Modality, parentTransform);
            var newInteractorGo = Templates.CreateFromTemplate(parentTransform, template);
            newInteractorGo.GetComponent<HmdRef>()?.InjectHmd(hmd);
            var newInteractor = newInteractorGo.GetComponent<IInteractor>();
            if (group != null)
            {
                var currentInteractors = GetInteractorsFromGroup(group);
                group.InjectInteractors(currentInteractors.Append(newInteractor).ToList());
                EditorUtility.SetDirty(group); // List will not persist if not set dirty
            }
            return newInteractorGo;
        }

        internal static Transform GetModalityGroup(InputModality modality, Transform parentTransform)
        {
            if (modality == InputModality.Any)
            {
                return parentTransform;
            }

            Transform holder = parentTransform.Find(_modalityGroupNames[modality]);
            if (holder != null)
            {
                return holder;
            }

            return parentTransform;
        }

        /// <summary>
        /// Find Hand components in the scene, ignoring derived types.
        /// </summary>
        public static IEnumerable<Hand> GetHands()
        {
            //get all devices (exact type)
            IEnumerable<Hand> devices = Object.FindObjectsByType<Hand>(FindObjectsSortMode.None)
                .Where(device => device.GetType() == typeof(Hand));

            //get just the final devices of the data stack and the hierarchy
            var devices2 = devices.Where(device => !devices.Any(other =>
                    other.ModifyDataFromSource == device as IDataSource<HandDataAsset>));

            //remove by hierarchy
            var devices3 = devices2.Where(device => !devices2.Any(other =>
                    other != device && other.GetComponentsInChildren<Hand>().Contains(device)));

            //old rig does not have HMD ref at the Input entry level
            var devices4 = devices3.Where(device =>
                device.TryGetComponent<HmdRef>(out _) ==
                device.TryGetComponent<Controller>(out _));

            return devices4;
        }

        /// <summary>
        /// Find Controller components in the scene, ignoring derived types.
        /// </summary>
        public static IEnumerable<Controller> GetControllers()
        {
            IEnumerable<Controller> controllers = Object.FindObjectsByType<Controller>(FindObjectsSortMode.None)
                .Where(device => device.GetType() == typeof(Controller));
            //get just the final devices of the data stack
            var devices2 = controllers.Where(device => !controllers.Any(other =>
                    other.ModifyDataFromSource == device as IDataSource<ControllerDataAsset>));

            //old rig does not have HMD ref at the Input entry level
            var devices3 = devices2.Where(device =>
                device.TryGetComponent<HmdRef>(out _) ==
                device.TryGetComponent<Hand>(out _));

            return devices3;
        }

        /// <summary>
        /// Find Controller Driven Hands components in the scene, ignoring derived types.
        /// </summary>
        public static IEnumerable<(Controller, Hand)> GetControllerHands()
        {
            IEnumerable<(Controller, Hand)> devices = Object.FindObjectsByType<Controller>(FindObjectsSortMode.None)
                .Where(device => device.GetType() == typeof(Controller))
                .Where(device => device.TryGetComponent(out Hand hand) && hand.GetType() == typeof(Hand))
                .Select(device => (device, device.GetComponent<Hand>()));

            //get just the final device of the stack
            var devices2 = devices.Where(controllerHand => !devices.Any(otherControllerHand =>
                    otherControllerHand.Item2.ModifyDataFromSource == controllerHand.Item2 as IDataSource<HandDataAsset>));

            return devices2;
        }

        /// <summary>
        /// Find the HMD component in the scene, ignoring derived types.
        /// </summary>
        public static Hmd GetHmd()
        {
            return Object.FindObjectsByType<Hmd>(FindObjectsSortMode.None)
                .Where(h => h.GetType() == typeof(Hmd))
                .FirstOrDefault();
        }

        /// <summary>
        /// Find existing EyeGaze component in the scene.
        /// </summary>
        public static EyeGaze GetEyeGaze()
        {
            return Object.FindObjectsByType<EyeGaze>(FindObjectsSortMode.None)
                .Where(e => e.GetType() == typeof(EyeGaze))
                .FirstOrDefault();
        }

        /// <summary>
        /// Find existing GazeConecaster component in the scene.
        /// </summary>
        public static GazeConecaster GetGazeConecaster()
        {
            return Object.FindObjectsByType<GazeConecaster>(FindObjectsSortMode.None)
                .Where(e => e.GetType() == typeof(GazeConecaster))
                .FirstOrDefault();
        }

        /// <summary>
        /// Create the eye gaze system from template if it doesn't already exist.
        /// </summary>
        /// <returns>The existing or newly created EyeGaze component</returns>
        public static EyeGaze CreateEyeGazeSystem()
        {
            static bool CheckIsOVRRig(Hmd hmd)
            {
                var ovrRigType = Type.GetType("OVRCameraRig, Oculus.VR");
                return ovrRigType != null && hmd.GetComponentInParent(ovrRigType) != null;
            }

            EyeGaze existingEyeGaze = GetEyeGaze();
            if (existingEyeGaze != null)
            {
                return existingEyeGaze;
            }

            Hmd hmd = GetHmd();
            if (hmd == null)
            {
                Debug.LogError("Cannot create EyeGaze system: HMD not found in scene.");
                return null;
            }

            Template eyeGazeTemplate = CheckIsOVRRig(hmd) ?
                new Template("OVREyeGaze", "fa67d7a229b4f944ebe85912157d8d30") :
                new Template("UnityXREyeGaze", "541c85193db7fbb438ff13729bff184c");

            string assetPath = AssetDatabase.GUIDToAssetPath(eyeGazeTemplate.AssetGUID);
            Object templateAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (templateAsset == null)
            {
                Debug.LogError("Cannot create EyeGaze system: EyeGaze template asset not found.");
                return null;
            }

            GameObject eyeGazeObject = Templates.CreateFromTemplate(hmd.transform.parent, eyeGazeTemplate);
            eyeGazeObject.transform.SetSiblingIndex(hmd.transform.GetSiblingIndex() + 1);

            EyeGaze eyeGaze = eyeGazeObject.GetComponent<EyeGaze>();
            GameObject conecasterGO = new GameObject("GazeConecaster");
            conecasterGO.transform.SetParent(eyeGazeObject.transform, false);
            GazeConecaster gazeConecaster = conecasterGO.AddComponent<GazeConecaster>();

            if (eyeGaze != null)
            {
                gazeConecaster.InjectGaze(eyeGaze);
            }

            UnityObjectAddedBroadcaster.HandleObjectWasAdded(eyeGazeObject);
            return eyeGaze;
        }

        /// <summary>
        /// Find or create the GazeActiveState component in the scene.
        /// </summary>
        /// <returns>The existing GazeActiveState component, or null if not found</returns>
        public static IActiveState GetGazeActiveState()
        {
            EyeGaze eyeGaze = GetEyeGaze();
            if (eyeGaze == null)
            {
                return null;
            }
            return eyeGaze;
        }

        /// <summary>
        /// Configures RayInteractors with ActiveStateTagSetFilter to enable Gaze/Ray fallback.
        /// When gaze tracking is active, Ray interactors will be blocked from interacting with
        /// objects tagged with GAZE_RAY_FALLBACK_TAG. When gaze tracking becomes unavailable,
        /// Ray interactors can interact with those objects.
        /// </summary>
        /// <param name="gazeActiveState">The GazeActiveState to monitor</param>
        /// <param name="deviceTypes">Device types to configure (Hands, Controllers, ControllerDrivenHands)</param>
        public static void ConfigureRayInteractorsForGazeFallback(
            IActiveState gazeActiveState, DeviceTypes deviceTypes)
        {
            if (gazeActiveState == null)
            {
                Debug.LogWarning("Cannot configure Ray interactors: GazeActiveState is null");
                return;
            }

            List<RayInteractor> rayInteractors = new List<RayInteractor>();

            if (deviceTypes.HasFlag(DeviceTypes.Hands))
            {
                foreach (var hand in GetHands())
                {
                    if (TryFindInteractorsGroup(hand, out InteractorGroup group, out Transform holder))
                    {
                        rayInteractors.AddRange(holder.GetComponentsInChildren<RayInteractor>(true)
                            .Where(r => r.GetComponent<IHand>() != null && r.GetComponent<IController>() == null));
                    }
                }
            }

            if (deviceTypes.HasFlag(DeviceTypes.Controllers))
            {
                foreach (var controller in GetControllers())
                {
                    if (TryFindInteractorsGroup(controller, out InteractorGroup group, out Transform holder))
                    {
                        rayInteractors.AddRange(holder.GetComponentsInChildren<RayInteractor>(true)
                            .Where(r => r.GetComponent<IController>() != null && r.GetComponent<IHand>() == null));
                    }
                }
            }

            if (deviceTypes.HasFlag(DeviceTypes.ControllerDrivenHands))
            {
                foreach (var controllerHand in GetControllerHands())
                {
                    if (TryFindInteractorsGroup(controllerHand.Item1, out InteractorGroup group, out Transform holder))
                    {
                        rayInteractors.AddRange(holder.GetComponentsInChildren<RayInteractor>(true)
                            .Where(r => r.GetComponent<IController>() != null && r.GetComponent<IHand>() != null));
                    }
                }
            }

            // Add ActiveStateTagSetFilter to each Ray interactor
            foreach (RayInteractor rayInteractor in rayInteractors)
            {
                // Check if ActiveStateTagSetFilter already exists
                ActiveStateTagSetFilter filter = rayInteractor.GetComponent<ActiveStateTagSetFilter>();
                if (filter == null)
                {
                    filter = rayInteractor.gameObject.AddComponent<ActiveStateTagSetFilter>();
                    filter.InjectAllActiveStateTagSetFilter(gazeActiveState);
                    filter.Tag = GazeQuickActionsConstants.GAZE_RAY_FALLBACK_TAG;
                    rayInteractor.InjectOptionalInteractableFilters(new List<IGameObjectFilter> { filter });
                    Undo.RegisterCreatedObjectUndo(filter, "Add ActiveStateTagSetFilter to RayInteractor");
                }
            }
        }

        /// <summary>
        /// GetComponent but only matches the base (non-derived) type.
        /// </summary>
        /// <typeparam name="TComponent"></typeparam>
        /// <param name="root">The root transform to search from</param>
        /// <param name="includeChildren">Search down the hierarchy</param>
        /// <param name="includeParents">Search up the hierarchy</param>
        /// <returns></returns>
        public static TComponent GetBaseComponent<TComponent>(Transform root,
            bool includeChildren = false, bool includeParents = false)
            where TComponent : Component
        {
            IEnumerable<TComponent> components =
                root.GetComponents<TComponent>();
            if (includeChildren)
            {
                components = components.Union(
                    root.GetComponentsInChildren<TComponent>());
            }
            if (includeParents)
            {
                components = components.Union(
                    root.GetComponentsInParent<TComponent>());
            }
            return components
                .Where(h => h.GetType() == typeof(TComponent))
                .FirstOrDefault();
        }
    }
}
