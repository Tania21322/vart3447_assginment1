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
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This interactor is driven by eye tracking and allows selecting and moving selected objects
    /// </summary>
    [Experimental]
    public class GazeInteractor : PointerInteractor<GazeInteractor, GazeInteractable>
    {
        public interface IGazeCandidateProvider
        {
            /// <summary>
            /// Provides a <see cref="GazeInteractable"/> candidate for interaction.
            /// </summary>
            /// <param name="candidate">The candidate <see cref="GazeInteractable"/></param>
            /// <param name="eyePoseWorld">The eye pose in world space at the time of the hit test</param>
            /// <param name="hitPointWorld">The hit point of the hit test on the object's surface</param>
            /// <param name="hitNormalWorld">The normal of the object's surface at <paramref name="hitPointWorld"/></param>
            /// <returns>True if a valid candidate was hit.</returns>
            bool TryGetCandidate(
                out GazeInteractable candidate,
                out Pose eyePoseWorld,
                out Vector3 hitPointWorld,
                out Vector3 hitNormalWorld,
            Predicate<GazeInteractable> filter = null);
        }

        public class GazeCandidateProperties : ICandidatePosition
        {
            public Vector3 CandidatePosition { get; }
            public GazeCandidateProperties(Vector3 candidatePosition) => CandidatePosition = candidatePosition;
        }

        [SerializeField, Tooltip("During selection, the pose delta of this transform is used to offset " +
                                 "the pose of generated pointer events.")]
        private Transform _pointerTransform = null;
        public Transform PointerTransform => _pointerTransform;

        [SerializeField, Interface(typeof(ISelector)), Tooltip("The \"select\" mechanism to listen to for \"selecting\" gazeable objects in the scene.")]
        private UnityEngine.Object _selector = null;

        [SerializeField, Interface(typeof(IGazeCandidateProvider))]
        [Tooltip("The hit-test mechanism (e.g raycaster, conecaster, etc.) used to determine which interactable receives the interaction event.")]
        private UnityEngine.Object _candidateProvider = null;
        public IGazeCandidateProvider CandidateProvider = null;

        /// <summary>
        /// The world pose of the gaze origin
        /// </summary>
        public Pose WorldPose { get; private set; }

        /// <summary>
        /// The movement provided by the actively selected interactable (can be null)
        /// </summary>
        private IMovement _selectionMovement = null;

        /// <summary>
        /// The pose delta (in world space) from <see cref="_pointerTransform"/> to
        /// <see cref="WorldPose"/> when selection is first triggered
        /// </summary>
        private Pose _pointerFromGazeSelectionDelta = Pose.identity;

        /// <summary>
        /// The position/rotation (world space) at the intersection between this interactor and its candidate interactable.
        /// </summary>
        /// <remarks>This is independent of where we render the cursor.</remarks>
        private Pose _interactionPose = Pose.identity;

        /// <summary>
        /// The current intersection point (world space) of this interactor and its candidate interactable.
        /// Will be <see cref="Vector3.zero"/> if there is no interaction candidate.
        /// </summary>
        /// <remarks>Effectively the "cursor" position of this interactor.</remarks>
        public Vector3 CandidateHitPoint { get; private set; }

        /// <summary>
        /// The current normal vector (world space) of the <see cref="CandidateHitPoint"/>.
        /// Will be <see cref="Vector3.zero"/> if there is no interaction candidate.
        /// </summary>
        public Vector3 CandidateHitNormal { get; private set; }

        /// <summary>
        /// The <see cref="ICandidatePosition"/> implementation used to compare interaction candidates.
        /// </summary>
        public override object CandidateProperties => new GazeCandidateProperties(CandidateHitPoint);

        protected override void Awake()
        {
            base.Awake();
            if (Selector == null)
            {
                Selector = _selector as ISelector;
            }
            if (CandidateProvider == null)
            {
                CandidateProvider = _candidateProvider as IGazeCandidateProvider;
            }
            if (CandidateFilter == null)
            {
                CandidateFilter = interactable => CanSelect(interactable) &&
                    interactable.CanBeSelectedBy(this);
            }
            _nativeId = 0x47617a6549746f72;
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.AssertField(Selector, nameof(Selector));
            this.AssertField(_pointerTransform, nameof(_pointerTransform));
            this.AssertField(CandidateProvider, nameof(CandidateProvider));
            this.EndStart(ref _started);
        }

        internal int TiebreakCandidates(GazeInteractable a, GazeInteractable b) => ComputeCandidateTiebreaker(a, b);

        protected override int ComputeCandidateTiebreaker(GazeInteractable a, GazeInteractable b)
        {
            int result = base.ComputeCandidateTiebreaker(a, b);
            return result != 0 ? result : a.TiebreakerScore.CompareTo(b.TiebreakerScore);
        }

        private Predicate<GazeInteractable> CandidateFilter;

        protected override GazeInteractable ComputeCandidate()
        {
            if (CandidateProvider.TryGetCandidate(out GazeInteractable candidate,
                out Pose eyePoseWorld,
                out Vector3 hitPointWorld,
                out Vector3 hitNormalWorld,
                CandidateFilter))
            {
                WorldPose = eyePoseWorld;
                CandidateHitPoint = hitPointWorld;
                CandidateHitNormal = hitNormalWorld;
                _interactionPose = new Pose(hitPointWorld, Quaternion.LookRotation(hitNormalWorld));
                return candidate;
            }
            WorldPose = default;
            CandidateHitPoint = default;
            CandidateHitNormal = default;
            _interactionPose = default;
            return null;
        }

        protected override void InteractableSelected(GazeInteractable interactable)
        {
            if (interactable != null)
            {
                // By flipping the normal here, we reduce the target rotation delta when the movement is applied.
                // Without flipping, a simple lerp from->to poses, for example, would flip the candidate such that the hit point
                // would face the same direction as the ray. With flipping, the lerp from->to better preserves the candidate's
                // general forward vector while still moving in the expected direction.
                Pose backHitPose = new Pose(CandidateHitPoint, Quaternion.LookRotation(-_interactionPose.forward));

                // Cache the delta between the pointer and gaze ray for subsequent raycasting
                Pose offsetEyePose = new Pose(WorldPose.position, Quaternion.LookRotation(_interactionPose.position - WorldPose.position));
                _pointerFromGazeSelectionDelta = PoseUtils.Delta(_pointerTransform.GetPose(), offsetEyePose);

                // Generate a movement from the flipped selection pose to (initially) the current selection pointer pose
                _selectionMovement = interactable.GenerateMovement(from: backHitPose, to: _pointerTransform.GetPose());
                if (_selectionMovement != null)
                {
                    _interactionPose = _selectionMovement.Pose;
                }
            }

            base.InteractableSelected(interactable);
        }

        protected override void InteractableUnselected(GazeInteractable interactable)
        {
            if (_selectionMovement != null)
            {
                _selectionMovement.StopAndSetPose(_selectionMovement.Pose);
            }

            base.InteractableUnselected(interactable);

            _selectionMovement = null;
        }

        protected override void DoNormalUpdate() => SyncCandidatePoseToInteractionPose();
        protected override void DoHoverUpdate() => SyncCandidatePoseToInteractionPose();
        protected override void DoSelectUpdate()
        {
            if (_selectionMovement != null)
            {
                _selectionMovement.UpdateTarget(_pointerTransform.GetPose());
                _selectionMovement.Tick();
                _interactionPose = _selectionMovement.Pose;

                SyncCandidatePoseToInteractionPose();

                return;
            }

            Pose rayPose = PoseUtils.Multiply(_pointerTransform.GetPose(), _pointerFromGazeSelectionDelta);
            Ray ray = new Ray(rayPose.position, rayPose.forward);

            Surfaces.ISurface surface = null;

            if (HasCandidate)
            {
                // If the candidate has a selection surface assigned, we want to use that
                // for raycasting here instead of the default surface.
                surface = Candidate.SelectionSurface != null ? Candidate.SelectionSurface : Candidate.Surface;
            }

            if (surface != null && surface.Raycast(ray, out Surfaces.SurfaceHit hit))
            {
                _interactionPose = new Pose(hit.Point, Quaternion.LookRotation(hit.Normal));
                SyncCandidatePoseToInteractionPose();
            }
            else
            {
                // No candidate and/or surface, zero out hit point and normal
                CandidateHitPoint = Vector3.zero;
                CandidateHitNormal = Vector3.zero;
            }
        }

        protected override Pose ComputePointerPose()
        {
            return HasCandidate ? _interactionPose : Pose.identity;
        }

        private void SyncCandidatePoseToInteractionPose()
        {
            CandidateHitPoint = _interactionPose.position;
            CandidateHitNormal = _interactionPose.forward;
        }

        #region Inject
        public void InjectAllGazeInteractor(ISelector selector, Transform relativeOrigin, IGazeCandidateProvider hitTester)
        {
            InjectSelector(selector);
            InjectPointerTransform(relativeOrigin);
            InjectHitTester(hitTester);
        }

        public void InjectSelector(ISelector selector)
        {
            _selector = selector as UnityEngine.Object;
            Selector = selector;
        }

        public void InjectPointerTransform(Transform pointerTransform)
        {
            _pointerTransform = pointerTransform;
        }

        public void InjectHitTester(IGazeCandidateProvider hitTester)
        {
            _candidateProvider = hitTester as UnityEngine.Object;
            CandidateProvider = hitTester;
        }
        #endregion
    }
}
