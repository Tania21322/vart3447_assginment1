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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oculus.Interaction.Editor.QuickActions
{
    /// <summary>
    /// The type of teleport surface to create.
    /// </summary>
    public enum TeleportSurfaceType
    {
        /// <summary>A localized teleport point with optional snap behavior.</summary>
        Hotspot,
        /// <summary>A flat plane surface for teleportation.</summary>
        Plane,
        /// <summary>A NavMesh-based walkable area for teleportation.</summary>
        NavMesh,
        /// <summary>A physics layer-based surface for teleportation.</summary>
        PhysicsLayer,
    }

    /// <summary>
    /// Snap behavior for teleport hotspots.
    /// </summary>
    public enum TeleportHotspotSnap
    {
        /// <summary>No snapping; teleport to the exact target point.</summary>
        None,
        /// <summary>Snap the player's position to the hotspot.</summary>
        SnapPosition,
        /// <summary>Snap the player's position and rotation to the hotspot.</summary>
        SnapPositionAndRotation,
    }

    /// <summary>
    /// Types of interactors that can be added to an input device.
    /// This is a flags enum; combine values with bitwise OR to add multiple types.
    /// </summary>
    [System.Flags]
    public enum QuickActionInteractorTypes
    {
        /// <summary>No interactors.</summary>
        None = 0,
        /// <summary>Poke (direct touch) interactor.</summary>
        Poke = 1 << 0,
        /// <summary>Hand grab interactor.</summary>
        Grab = 1 << 1,
        /// <summary>Ray interactor for pointing.</summary>
        Ray = 1 << 2,
        /// <summary>Distance grab interactor.</summary>
        DistanceGrab = 1 << 3,
        /// <summary>Teleport interactor for locomotion.</summary>
        Teleport = 1 << 4,
        /// <summary>Gaze (eye tracking) interactor. Opt-in; not included in <see cref="All"/>.</summary>
        Gaze = 1 << 5,
        /// <summary>All standard interactor types.</summary>
        All = Poke | Grab | Ray | DistanceGrab | Teleport,
    }

    /// <summary>
    /// The distance grab behavior mode.
    /// </summary>
    public enum DistanceGrabMode
    {
        /// <summary>Grab relative to the hand position.</summary>
        AnchorAtHand,
        /// <summary>Pull the interactable to the hand.</summary>
        PullToHand,
        /// <summary>Manipulate the object in place from a distance.</summary>
        ManipulateInPlace,
    }

    /// <summary>
    /// Public API for programmatically invoking Interaction SDK Quick Actions.
    /// Each method runs the corresponding wizard headlessly with default settings
    /// and returns all GameObjects created during the operation.
    /// </summary>
    public static class QuickActionsAPI
    {
        /// <summary>
        /// Adds a grab interaction to the target GameObject.
        /// Sets up HandGrabInteractable with Rigidbody, Grabbable, and collider.
        /// </summary>
        /// <param name="target">The GameObject to add the grab interaction to.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddGrabInteraction(GameObject target)
        {
            return QuickActionsWizard.CreateWithDefaults<GrabWizard>(target).ToList();
        }

        /// <summary>
        /// Adds a distance grab interaction to the target GameObject.
        /// Creates a DistanceGrabInteractable with configurable grab behavior.
        /// </summary>
        /// <param name="target">The GameObject to add the distance grab interaction to.</param>
        /// <param name="mode">The distance grab behavior mode.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddDistanceGrabInteraction(
            GameObject target,
            DistanceGrabMode mode = DistanceGrabMode.PullToHand)
        {
            return QuickActionsWizard.CreateWithDefaults<DistanceGrabWizard>(target,
                injections: w => w.InjectMode(ToInternalMode(mode))).ToList();
        }

        /// <summary>
        /// Adds a ray grab interaction to the target GameObject.
        /// Creates a RayInteractable with Grabbable and auto-generated ColliderSurface.
        /// </summary>
        /// <param name="target">The GameObject to add the ray grab interaction to.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddRayGrabInteraction(GameObject target)
        {
            return QuickActionsWizard.CreateWithDefaults<RayGrabWizard>(target).ToList();
        }

        /// <summary>
        /// Adds a ray interaction to a Canvas.
        /// Creates a RayCanvasInteractable with GraphicRaycaster and ensures a
        /// PointableCanvasModule exists in the scene.
        /// </summary>
        /// <param name="target">The Canvas GameObject to add the ray interaction to.</param>
        /// <param name="fixPointableCanvasModule">When true (default), ensures a
        /// PointableCanvasModule exists in the scene, adding one if missing.
        /// Set to false when a PointableCanvasModule is already provided via a
        /// prefab or additive scene that is not currently loaded.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddRayCanvasInteraction(
            GameObject target,
            bool fixPointableCanvasModule = true)
        {
            return QuickActionsWizard.CreateWithDefaults<RayCanvasWizard>(target,
                injections: w =>
                {
                    if (fixPointableCanvasModule)
                    {
                        w.FixMissingPointableCanvasModule();
                    }
                }).ToList();
        }

        /// <summary>
        /// Adds a poke interaction to a Canvas.
        /// Creates a PokeCanvasInteractable for direct touch on UI elements and
        /// ensures a PointableCanvasModule exists in the scene.
        /// </summary>
        /// <param name="target">The Canvas GameObject to add the poke interaction to.</param>
        /// <param name="fixPointableCanvasModule">When true (default), ensures a
        /// PointableCanvasModule exists in the scene, adding one if missing.
        /// Set to false when a PointableCanvasModule is already provided via a
        /// prefab or additive scene that is not currently loaded.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddPokeCanvasInteraction(
            GameObject target,
            bool fixPointableCanvasModule = true)
        {
            return QuickActionsWizard.CreateWithDefaults<PokeCanvasWizard>(target,
                injections: w =>
                {
                    if (fixPointableCanvasModule)
                    {
                        w.FixMissingPointableCanvasModule();
                    }
                }).ToList();
        }

        /// <summary>
        /// Adds a teleport interaction to the target GameObject.
        /// Creates a TeleportInteractable with locomotion support.
        /// </summary>
        /// <param name="target">The GameObject to add the teleport interaction to.</param>
        /// <param name="surfaceType">The type of teleport surface to create.</param>
        /// <param name="hotspotSnap">Snap behavior for hotspot teleports. Only applies when
        /// surfaceType is Hotspot.</param>
        /// <param name="walkableArea">The NavMesh area name. Only applies when surfaceType
        /// is NavMesh.</param>
        /// <param name="layerMask">The physics layer mask. Only applies when surfaceType
        /// is PhysicsLayer.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddTeleportInteraction(
            GameObject target,
            TeleportSurfaceType surfaceType = TeleportSurfaceType.Hotspot,
            TeleportHotspotSnap hotspotSnap = TeleportHotspotSnap.SnapPosition,
            string walkableArea = "Walkable",
            int layerMask = -1)
        {
            return QuickActionsWizard.CreateWithDefaults<TeleportWizard>(target,
                injections: w =>
                {
                    switch (surfaceType)
                    {
                        case TeleportSurfaceType.Hotspot:
                            w.InjectOptionalHotspotType(null,
                                ToInternalHotspotSnap(hotspotSnap), null);
                            break;
                        case TeleportSurfaceType.Plane:
                            w.InjectOptionalPlaneType();
                            break;
                        case TeleportSurfaceType.NavMesh:
                            w.InjectOptionalNavMeshType(walkableArea);
                            break;
                        case TeleportSurfaceType.PhysicsLayer:
                            w.InjectOptionalPhysicsLayerType(layerMask);
                            break;
                    }
                }).ToList();
        }

        /// <summary>
        /// Adds interactors to a hand input device.
        /// </summary>
        /// <param name="target">The Hand GameObject to add interactors to.</param>
        /// <param name="interactorTypes">The types of interactors to add. Defaults to all
        /// standard types.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddHandInteractors(
            GameObject target,
            QuickActionInteractorTypes interactorTypes = QuickActionInteractorTypes.All)
        {
            return QuickActionsWizard.CreateWithDefaults<HandInteractorWizard>(target,
                injections: w => w.InjectInteractorTypes(
                    ToInternalInteractorTypes(interactorTypes))).ToList();
        }

        /// <summary>
        /// Adds interactors to a controller input device.
        /// </summary>
        /// <param name="target">The Controller GameObject to add interactors to.</param>
        /// <param name="interactorTypes">The types of interactors to add. Defaults to all
        /// standard types.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddControllerInteractors(
            GameObject target,
            QuickActionInteractorTypes interactorTypes = QuickActionInteractorTypes.All)
        {
            return QuickActionsWizard.CreateWithDefaults<ControllerInteractorWizard>(target,
                injections: w => w.InjectInteractorTypes(
                    ToInternalInteractorTypes(interactorTypes))).ToList();
        }

        /// <summary>
        /// Adds interactors to a controller-driven hand.
        /// </summary>
        /// <param name="target">The controller-driven Hand GameObject to add interactors to.</param>
        /// <param name="interactorTypes">The types of interactors to add. Defaults to all
        /// standard types.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddControllerHandInteractors(
            GameObject target,
            QuickActionInteractorTypes interactorTypes = QuickActionInteractorTypes.All)
        {
            return QuickActionsWizard.CreateWithDefaults<ControllerHandInteractorWizard>(target,
                injections: w => w.InjectInteractorTypes(
                    ToInternalInteractorTypes(interactorTypes))).ToList();
        }


        /// <summary>
        /// Adds a gaze grab interaction to the target GameObject.
        /// Sets up GazeInteractable with eye tracking and ray fallback.
        /// </summary>
        /// <param name="target">The GameObject to add the gaze interaction to.</param>
        /// <param name="enableRayFallback">Whether to create a parallel RayInteractable
        /// as a fallback when gaze tracking is unavailable.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddGazeInteraction(
            GameObject target,
            bool enableRayFallback = true)
        {
            return QuickActionsWizard.CreateWithDefaults<GazeWizard>(target,
                injections: w =>
                {
                    w.InjectOptionalEnableRayFallback(enableRayFallback);
                }).ToList();
        }

        /// <summary>
        /// Adds a gaze interaction to a Canvas.
        /// Creates a ConecastableCanvas with gaze support and ensures a
        /// PointableCanvasModule exists in the scene.
        /// </summary>
        /// <param name="target">The Canvas GameObject to add the gaze interaction to.</param>
        /// <param name="enableRayFallback">Whether to create a parallel RayInteractable
        /// as a fallback when gaze tracking is unavailable.</param>
        /// <param name="addToChildCanvases">Whether to also add gaze interaction to
        /// child Canvas elements.</param>
        /// <param name="fixPointableCanvasModule">When true (default), ensures a
        /// PointableCanvasModule exists in the scene, adding one if missing.
        /// Set to false when a PointableCanvasModule is already provided via a
        /// prefab or additive scene that is not currently loaded.</param>
        /// <returns>The root GameObjects created by the wizard.</returns>
        public static List<GameObject> AddGazeCanvasInteraction(
            GameObject target,
            bool enableRayFallback = true,
            bool addToChildCanvases = true,
            bool fixPointableCanvasModule = true)
        {
            return QuickActionsWizard.CreateWithDefaults<GazeCanvasWizard>(target,
                injections: w =>
                {
                    w.InjectOptionalEnableRayFallback(enableRayFallback);
                    w.InjectOptionalAddToChildCanvases(addToChildCanvases);
                    if (fixPointableCanvasModule)
                    {
                        w.FixMissingPointableCanvasModule();
                    }
                }).ToList();
        }


        private static DistanceGrabWizard.Mode ToInternalMode(DistanceGrabMode mode)
        {
            switch (mode)
            {
                case DistanceGrabMode.AnchorAtHand:
                    return DistanceGrabWizard.Mode.AnchorAtHand;
                case DistanceGrabMode.PullToHand:
                    return DistanceGrabWizard.Mode.InteractableToHand;
                case DistanceGrabMode.ManipulateInPlace:
                    return DistanceGrabWizard.Mode.HandToInteractable;
                default:
                    return DistanceGrabWizard.Mode.InteractableToHand;
            }
        }

        private static TeleportWizard.TeleportHotspotSnapType ToInternalHotspotSnap(TeleportHotspotSnap snap)
        {
            switch (snap)
            {
                case TeleportHotspotSnap.None:
                    return TeleportWizard.TeleportHotspotSnapType.None;
                case TeleportHotspotSnap.SnapPosition:
                    return TeleportWizard.TeleportHotspotSnapType.SnapPosition;
                case TeleportHotspotSnap.SnapPositionAndRotation:
                    return TeleportWizard.TeleportHotspotSnapType.SnapPositionAndRotation;
                default:
                    return TeleportWizard.TeleportHotspotSnapType.SnapPosition;
            }
        }

        private static InteractorTypes ToInternalInteractorTypes(
            QuickActionInteractorTypes types)
        {
            InteractorTypes result = InteractorTypes.None;
            if ((types & QuickActionInteractorTypes.Poke) != 0)
                result |= InteractorTypes.Poke;
            if ((types & QuickActionInteractorTypes.Grab) != 0)
                result |= InteractorTypes.Grab;
            if ((types & QuickActionInteractorTypes.Ray) != 0)
                result |= InteractorTypes.Ray;
            if ((types & QuickActionInteractorTypes.DistanceGrab) != 0)
                result |= InteractorTypes.DistanceGrab;
            if ((types & QuickActionInteractorTypes.Teleport) != 0)
                result |= InteractorTypes.Teleport;
            if ((types & QuickActionInteractorTypes.Gaze) != 0)
                result |= InteractorTypes.Gaze;
            return result;
        }
    }
}
