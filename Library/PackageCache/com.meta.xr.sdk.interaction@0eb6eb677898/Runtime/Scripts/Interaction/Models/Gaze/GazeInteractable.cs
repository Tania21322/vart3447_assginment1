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

using UnityEngine;
using Oculus.Interaction.Surfaces;
using UnityEngine.Serialization;
using System.ComponentModel;

namespace Oculus.Interaction
{
    [Experimental]
    public class GazeInteractable : PointerInteractable<GazeInteractor, GazeInteractable>
    {
        [SerializeField, Interface(typeof(ISurface)), FormerlySerializedAs("_surfacePatch"), Tooltip("The gazeable surface area.")]
        private Object _surface;
        public ISurface Surface { get; private set; }

        /// <summary>
        /// Defines an override surface for the interactor to raycast against while the interactable is being selected.
        /// </summary>
        /// <remarks>
        /// A common assignment would be the unclipped version of <see cref="Surface"/>, if applicable.
        /// </remarks>
        [Tooltip("Defines an override surface for the interactor to raycast against while the interactable is being selected.")]
        [SerializeField, Optional, Interface(typeof(ISurface))]
        private UnityEngine.Object _selectionSurface = null;
        public ISurface SelectionSurface { get; private set; }

        /// <summary>
        /// An <see cref="IMovementProvider"/> that modifies how the interactable moves while selected.
        /// </summary>
        [Tooltip("An IMovementProvider that determines how the interactable moves when selected.")]
        [SerializeField, Optional, Interface(typeof(IMovementProvider))]
        private UnityEngine.Object _movementProvider;
        private IMovementProvider MovementProvider = null;

        [SerializeField, Tooltip("The priority level used by the interactor to determine which interactable should receive" +
            " a particular interaction event when all other factors (e.g. proximity) are equal. Higher value => takes priority.")]
        private float _tiebreakerScore;
        public float TiebreakerScore { get => _tiebreakerScore; set => _tiebreakerScore = value; }

        [Tooltip("If provided, selectable canvas graphics will be conecasted.")]
        [SerializeField, Optional(OptionalAttribute.Flag.DontHide)]
        private ConecastableCanvas _rootCanvas;
        public ConecastableCanvas RootCanvas => _rootCanvas;

        [SerializeField]
        [InspectorName("MultiGrab scaling support")]
        [Tooltip("When enabled, the GazeInteractable internally wraps its PointableElement in a " +
            "MultiGrabPointableElement so multi-handed grabs pivot and scale about this transform for any " +
            "movement provider. Disable to drive the PointableElement directly with no multi-grab scaling.")]
        private bool _multiGrabScalingSupport = true;

        private MultiGrabPointableElement _multiGrab;

        protected override void Awake()
        {
            base.Awake();
            if (Surface == null)
            {
                Surface = _surface as ISurface;
            }
            if (SelectionSurface == null)
            {
                SelectionSurface = _selectionSurface as ISurface;
            }
            if (MovementProvider == null)
            {
                MovementProvider = _movementProvider as IMovementProvider;
            }
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Surface, nameof(Surface));
            if (_multiGrabScalingSupport && PointableElement != null && _multiGrab == null)
            {
                _multiGrab = new MultiGrabPointableElement(PointableElement, this, transform, TryGetOriginPose);
                PointableElement = _multiGrab;
            }
            this.EndStart(ref _started);
        }

        // Resolves a selecting interactor's finger-tip (origin) pose from its GazeInteractor PointerTransform.
        private static bool TryGetOriginPose(IInteractorView interactorView, out Pose pose)
        {
            if (interactorView is GazeInteractor gazeInteractor && gazeInteractor.PointerTransform != null)
            {
                pose = gazeInteractor.PointerTransform.GetPose();
                return true;
            }
            pose = Pose.identity;
            return false;
        }

        protected virtual void OnDestroy()
        {
            if (_multiGrab != null)
            {
                _multiGrab.Dispose();
                _multiGrab = null;
            }
        }

        /// <summary>
        /// Generates movement to move the <see cref="GazeInteractable"/> from its current position to the target position.
        /// </summary>
        /// <param name="to">The target position.</param>
        /// <param name="from">The current position.</param>
        /// <returns>Returns the created movement.</returns>
        public IMovement GenerateMovement(in Pose from, in Pose to)
        {
            if (MovementProvider == null)
            {
                return null;
            }

            IMovement movement = MovementProvider.CreateMovement();
            movement.StopAndSetPose(from);
            movement.MoveTo(to);
            return movement;
        }

        #region Inject
        public void InjectAllGazeInteractable(ISurface surface)
        {
            InjectSurface(surface);
        }

        public void InjectSurface(ISurface surface)
        {
            Surface = surface;
            _surface = surface as MonoBehaviour;
        }

        /// <summary>
        /// Sets a movement provider for a dynamically instantiated GameObject.
        /// </summary>
        public void InjectOptionalMovementProvider(IMovementProvider provider)
        {
            _movementProvider = provider as UnityEngine.Object;
            MovementProvider = provider;
        }

        public void InjectOptionalRootCanvas(ConecastableCanvas conecastableCanvas)
        {
            _rootCanvas = conecastableCanvas;
        }

        /// <summary>
        /// Sets whether the interactable internally wraps its <see cref="PointerInteractable.PointableElement"/>
        /// in a <see cref="MultiGrabPointableElement"/> for multi-grab scaling. Must be set before
        /// <see cref="Awake"/> runs for dynamically instantiated GameObjects.
        /// </summary>
        public void InjectOptionalMultiGrabScalingSupport(bool enabled)
        {
            _multiGrabScalingSupport = enabled;
        }

        #endregion
    }
}
