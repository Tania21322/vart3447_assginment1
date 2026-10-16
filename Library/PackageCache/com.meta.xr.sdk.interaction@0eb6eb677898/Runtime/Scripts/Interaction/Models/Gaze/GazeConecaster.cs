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
using Oculus.Interaction.Input;
using UnityEngine.Pool;

namespace Oculus.Interaction
{
    [Experimental]
    public class GazeConecaster : Conecaster,
        GazeInteractor.IGazeCandidateProvider,
        ITimeConsumer
    {
        [SerializeField]
        [Interface(typeof(IGaze))]
        private UnityEngine.Object _gaze;
        private IGaze Gaze;

        [SerializeField]
        [Min(0f)]
        [Tooltip("The timespan over which the interactor will analyze gaze when " +
            "selecting a candidate. Increasing this value increases \"dwell time.\"")]
        private float _dwellTimespanSeconds = 0.2f;

        [SerializeField]
        [Tooltip("Draws an axis gizmo at the consensus' hit point.")]
        private bool _enableDebugVisuals = false;

        /// <summary>
        /// The timespan over which the interactor will analyze gaze when
        /// selecting a candidate. Increasing this value increases "dwell time."
        /// </summary>
        public float DwellTimespanSeconds
        {
            get => _dwellTimespanSeconds;
            set => _dwellTimespanSeconds = Mathf.Max(0, value);
        }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        public void SetTimeProvider(Func<float> timeProvider) =>
            _timeProvider = timeProvider;

        private (GazeInteractable interactable, ConecastResult conecastResult)? _consensus;
        private ConsensusFilter<(GazeInteractable, ConecastResult)> _consensusFilter;

        private Pose _eyePose = Pose.identity;
        private bool _needsUpdate = false;
        private Func<float> _timeProvider = () => Time.time;

        #region IGazeCandidateProvider

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        bool GazeInteractor.IGazeCandidateProvider.TryGetCandidate(
            out GazeInteractable candidate,
            out Pose eyePoseWorld,
            out Vector3 hitPointWorld,
            out Vector3 hitNormalWorld,
            Predicate<GazeInteractable> filter)
        {
            if (_needsUpdate)
            {
                UpdateConecaster(filter);
                _needsUpdate = false;
            }

            if (_consensus.HasValue &&
                _consensus.Value.interactable != null &&
                _consensus.Value.conecastResult.IsValid)
            {
                var consensus = _consensus.Value;
                candidate = consensus.interactable;
                eyePoseWorld = consensus.conecastResult.OriginPoseWorld;
                hitPointWorld = consensus.conecastResult.Hit.HitPointWorld;
                hitNormalWorld = consensus.conecastResult.Hit.HitNormalWorld;
                return true;
            }
            else
            {
                candidate = null;
                eyePoseWorld = Pose.identity;
                hitPointWorld = Vector3.zero;
                hitNormalWorld = Vector3.zero;
                return false;
            }
        }

        #endregion IGazeCandidateProvider

        protected override void Awake()
        {
            base.Awake();
            _consensusFilter = new(() => DwellTimespanSeconds);
            if (Gaze == null)
            {
                Gaze = _gaze as IGaze;
            }
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, base.Start);
            this.AssertField(Gaze, nameof(Gaze));
            this.EndStart(ref _started);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_started)
            {
                Gaze.WhenUpdated += OnGazeDataUpdated;
                OnGazeDataUpdated();
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            if (_started)
            {
                Gaze.WhenUpdated -= OnGazeDataUpdated;
            }
        }

        private void OnGazeDataUpdated()
        {
            if (Gaze.TryGetWorldPose(out _eyePose))
            {
                _needsUpdate = true;
            }
        }

        private void UpdateConecaster(Predicate<GazeInteractable> filter)
        {
            ListPool<GazeConecastTarget>.Get(out var targets);
            foreach (var interactable in GazeInteractable.Registry.List())
            {
                var target = GazeConecastTarget.GetPooled();
                target.GazeInteractable = interactable;
                targets.Add(target);
            }

            Conecast(_eyePose, targets);

            bool hasValidResult = TryGetBestResult(out var bestResult);
            _consensusFilter.AddResult(
                hasValidResult ? bestResult.conecastResult.TargetId : null,
                hasValidResult ? bestResult : default,
                _timeProvider());
            _consensus = _consensusFilter.GetConsensus();

            foreach (var target in targets)
            {
                GazeConecastTarget.ReleasePooled(target);
            }
            ListPool<GazeConecastTarget>.Release(targets);

            if (_enableDebugVisuals && _consensus.HasValue &&
                _consensus.Value.conecastResult.IsValid)
            {
                ConecastHit hit = _consensus.Value.conecastResult.Hit;
                DebugGizmos.LineWidth = 0.005f;
                DebugGizmos.Color = Color.white;
                DebugGizmos.DrawPoint(hit.HitPointWorld);
                DebugGizmos.DrawAxis(
                    hit.HitPointWorld,
                    Quaternion.LookRotation(hit.HitNormalWorld), 0.01f);
            }

            bool TryGetBestResult(out (GazeInteractable interactable,
                ConecastResult conecastResult) bestResult)
            {
                // Iterate through the results, and walk up the hierarchy, trying
                // to get GazeInteractables for each ancestor. If we find an ancestor
                // linked with a GazeInteractable, return that GazeInteractable along with
                // the original (best) result. If no ancestor has a GazeInteractable,
                // move to the next-best result from the conecast result collection.

                var conecastResults = GetResults();

                foreach (var conecastResult in conecastResults)
                {
                    var ancestor = conecastResult; // Start with the initial result.
                    do
                    {
                        if (TryGetInstanceFromUniqueId(ancestor, out GazeInteractable interactable) &&
                           (filter == null || filter(interactable)))
                        {
                            bestResult = (interactable, conecastResult);
                            return true;
                        }
                    } while (TryGetParent(ancestor, out ancestor));
                }

                bestResult = default;
                return false;
            }
        }

        private static bool TryGetInstanceFromUniqueId<TObj>(
            in ConecastResult result, out TObj obj)
            where TObj : class
        {
            obj = null;
            return UniqueIdentifier.TryGetInstanceFromIdentifier(
                Context.Global.GetInstance(), checked((int)result.TargetId), out obj);
        }

        #region Inject

        public void InjectAllGazeConecaster(IGaze gaze)
        {
            InjectGaze(gaze);
        }

        public void InjectGaze(IGaze gaze)
        {
            _gaze = gaze as UnityEngine.Object;
            Gaze = gaze;
        }

        #endregion Inject
    }
}
