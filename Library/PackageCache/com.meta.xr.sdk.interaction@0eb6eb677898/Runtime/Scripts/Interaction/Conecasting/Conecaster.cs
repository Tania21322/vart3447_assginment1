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
using UnityEngine;
using UnityEngine.Pool;

namespace Oculus.Interaction
{
    [Experimental]
    public abstract class Conecaster : MonoBehaviour
    {
        protected readonly struct ConecastResult
        {
            /// <summary>
            /// Is this a valid result, where default structs
            /// or structs created the the paramaterless constructor
            /// will return false. True otherwise.
            /// </summary>
            public readonly bool IsValid;

            /// <summary>
            /// The ID representing the target of the
            /// conecast from <see cref="IConecastTarget.Id"/>
            /// </summary>
            public readonly ulong RootId;

            /// <summary>
            /// The ID representing the target of the
            /// conecast from <see cref="IConecastTarget.Id"/>
            /// </summary>
            public readonly ulong TargetId;

            /// <summary>
            /// The hit data for the conecast.
            /// </summary>
            public readonly ConecastHit Hit;

            /// <summary>
            /// The source pose of the conecast.
            /// </summary>
            public readonly Pose OriginPoseWorld;

            public ConecastResult(
                ulong rootId,
                ulong targetId,
                ConecastHit hit,
                Pose originPoseWorld)
            {
                IsValid = true;
                RootId = rootId;
                TargetId = targetId;
                Hit = hit;
                OriginPoseWorld = originPoseWorld;
            }
        }

        private class ConecastResultInternal
        {
            private static readonly ObjectPool<ConecastResultInternal> _pool = new(
                createFunc: () => new(),
                actionOnRelease: (o) => { o.Target = null; o.Root = null; });

            public static ConecastResultInternal GetPooled(
                IConecastTarget root,
                IConecastTarget target,
                ConecastHit hit,
                Pose originPoseWorld)
            {
                var result = _pool.Get();
                result.Root = root;
                result.Target = target;
                result._hit = hit;
                result._originPoseWorld = originPoseWorld;
                return result;
            }

            public static void ReleasePooled(ConecastResultInternal obj) => _pool.Release(obj);
            public IConecastTarget Target { get; private set; }
            public IConecastTarget Root { get; private set; }
            public ref readonly ConecastHit Hit => ref _hit;
            public ref readonly Pose OriginPoseWorld => ref _originPoseWorld;

            private ConecastHit _hit;
            private Pose _originPoseWorld;

            private ConecastResultInternal() { }

            public ConecastResult GetConecastResult()
            {
                return new ConecastResult(
                    Root.Id,
                    Target.Id,
                    Hit,
                    OriginPoseWorld);
            }
        }

        [SerializeField, Tooltip("The maximum acceptable angular offset " +
            "between the gaze ray and a candidate interactable.")]
        [Min(0f)]
        private float _coneAngle = 2f;

        [SerializeField, Tooltip("(Meters, World) The length of the cone " +
            "as measured from the origin along the gaze ray.")]
        [Min(0f)]
        private float _coneLength = 10f;

        [SerializeField]
        private ConecastResultComparer _defaultComparer = new();

        /// <summary>
        /// The maximum acceptable angular offset between the gaze ray and a candidate interactable.
        /// </summary>
        public float ConeAngle { get => _coneAngle; set => _coneAngle = value; }

        /// <summary>
        /// (Meters, World) The length of the cone as measured from the origin along the gaze ray.
        /// </summary>
        public float ConeLength { get => _coneLength; set => _coneLength = value; }

        /// <summary>
        /// The default comparer used to sort the results of a conecast, if no
        /// override comparer is provided through <see cref="SetOverrideComparer()"/>
        /// </summary>
        public ConecastResultComparer DefaultComparer => _defaultComparer;

        /// <summary>
        /// If provided, this custom comparer will override the <see cref="DefaultComparer"/>
        /// used to sort the results of a conecast.
        /// </summary>
        public void SetOverrideComparer(IComparer<ConecastHit> overrideComparer) =>
            _overrideComparer = overrideComparer;

        private int _numResults = 0;
        private ConecastResult[] _results = new ConecastResult[64];
        private IComparer<ConecastHit> _overrideComparer;
        private Comparison<ConecastResultInternal> _cachedComparison;
        private Dictionary<ulong, ulong> _childIdToParentId = new();
        private Dictionary<ulong, int> _targetIdToIndex = new();
        protected bool _started = false;

        /// <summary>
        /// Get the <see cref="ConecastResult"/> representing the root
        /// <see cref="IConecastTarget"/> used in the conecast.
        /// <para></para>
        /// This should only be called with <see cref="ConecastResult"/>s obtained
        /// from <see cref="GetResults()"/> within the same frame.
        /// </summary>
        /// <param name="source">The result to retrieve the root for.</param>
        /// <returns>The root. If not found, will return the source.</returns>
        protected ConecastResult GetRoot(in ConecastResult source)
        {
            var current = source;
            while (TryGetParent(current, out var parent))
            {
                current = parent;
            }
            return current;
        }

        /// <summary>
        /// Get the <see cref="ConecastResult"/> representing the parent
        /// <see cref="IConecastTarget"/> of the given <see cref="ConecastResult"/>.
        /// <para></para>
        /// This should only be called with <see cref="ConecastResult"/>s obtained
        /// from <see cref="GetResults()"/> within the same frame.
        /// </summary>
        /// <param name="source">The result to retrieve the parent for.</param>
        /// <param name="parent">The parent of the provided source.</param>
        /// <returns>True if a parent was found, false if the source is a root target.</returns>
        protected bool TryGetParent(in ConecastResult source, out ConecastResult parent)
        {
            if (_childIdToParentId.TryGetValue(source.TargetId, out ulong parentId) &&
                _targetIdToIndex.TryGetValue(parentId, out int parentIndex))
            {
                parent = _results[parentIndex];
                return true;
            }
            parent = default;
            return false;
        }

        /// <summary>
        /// Get the results of the last <see cref="Conecast"/> call. Results are sorted from
        /// best to worst, so index 0 represents the closest target to the cone ray, unless
        /// overridden by custom comparers (see <see cref="SetOverrideComparer"/>).
        /// </summary>
        /// <returns>The sorted collection of <see cref="ConecastResult"/>s</returns>
        protected ReadOnlySpan<ConecastResult> GetResults()
        {
            return new ReadOnlySpan<ConecastResult>(_results, 0, _numResults);
        }

        protected void Conecast(Pose worldPose, IReadOnlyList<IConecastTarget> rootConecastables)
        {
            _childIdToParentId.Clear();
            _targetIdToIndex.Clear();

            ListPool<ConecastResultInternal>.Get(out var hitTestResults);

            var cone = new Cone(worldPose.position, worldPose.forward, _coneAngle, _coneLength);

            for (int i = 0; i < rootConecastables.Count; ++i)
            {
                var root = rootConecastables[i];

                ConecastRecursive(root);

                void ConecastRecursive(IConecastTarget target)
                {
                    if (target.TryConecast(cone, out var hitPoint, out var hitNormal) &&
                        cone.Contains(hitPoint))
                    {
                        ConecastHit hit = new ConecastHit(cone, hitPoint, hitNormal);
                        ListPool<IConecastTarget>.Get(out var children);
                        if (target.TryGetChildren(children))
                        {
                            foreach (var child in children)
                            {
                                _childIdToParentId[child.Id] = target.Id;
                                ConecastRecursive(child);
                            }
                        }
                        hitTestResults.Add(ConecastResultInternal.GetPooled(root, target, hit, worldPose));
                        ListPool<IConecastTarget>.Release(children);
                    }
                }
            }

            hitTestResults.Sort(_cachedComparison);

            _numResults = hitTestResults.Count;
            if (_results.Length < _numResults)
            {
                _results = new ConecastResult[_numResults * 2];
            }

            for (int i = 0; i < _numResults; ++i)
            {
                _results[i] = hitTestResults[i].GetConecastResult();
                _targetIdToIndex[_results[i].TargetId] = i;
                ConecastResultInternal.ReleasePooled(hitTestResults[i]);
            }
            ListPool<ConecastResultInternal>.Release(hitTestResults);
        }

        protected virtual void Awake()
        {
            _cachedComparison = CompareResults;
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable() { }

        protected virtual void OnDisable() { }

        /// <summary>
        /// Comparison of <see cref="ConecastResultInternal"/>s where lower is considered better.
        /// </summary>
        /// <returns>Comparison result where lower is better.</returns>
        private int CompareResults(ConecastResultInternal a, ConecastResultInternal b)
        {
            bool IsDescendantOf(IConecastTarget child, IConecastTarget parent)
            {
                ulong currentId = child.Id;
                while (_childIdToParentId.TryGetValue(currentId, out ulong currentParent))
                {
                    currentId = currentParent;
                    if (currentId == parent.Id)
                    {
                        return true;
                    }
                }
                return false;
            }

            // A child element should always take precedence over its ancestor.
            int result = IsDescendantOf(a.Target, b.Target) ? -1 :
                IsDescendantOf(b.Target, a.Target) ? 1 : 0;

            // After sorting by parent-child relationship, compare conecast hits.
            var comparer = _overrideComparer == null ? _defaultComparer : _overrideComparer;
            if (result == 0) result = comparer.Compare(a.Hit, b.Hit);

            // If the results are still equal, targets should attempt tiebreaking with one another.
            if (result == 0) result = b.Target.TiebreakWith(a.Target);

            return result;
        }

        protected class ConsensusFilter<TData>
            where TData : struct
        {
            private Dictionary<ulong, List<(float, TData)>> _targetToResults;
            private List<(float, TData)> _nullResults;
            private HashSet<ulong> _emptyTargets;
            private TData? _currentConsensus = null;
            private float _currentConsensusTimespan;
            private float _mostRecentTimestamp = -1f;
            private readonly Func<float> _getTimespan;

            /// <summary>
            /// Creates a new consensus filter that pre-allocates memory for targets and data points.
            /// </summary>
            /// <param name="getTimespan">Func that returns a time span. Represents the
            /// length of time to "dwell" on a target before considering it valid.</param>
            /// <param name="preWarmTargetCount">To avoid runtime allocations due to internal
            /// data structures growing, you may provide a count representing the maximumum number
            /// of unique targets anticipated in the provided time span.
            /// </param>
            /// <param name="preWarmSamplesCount">To avoid runtime allocations due to internal
            /// data structures growing, you may provide a count representing the maximum number
            /// of samples added via <see cref="AddResult"/> in the provided time span.
            /// </param>
            public ConsensusFilter(Func<float> getTimespan,
                int preWarmTargetCount, int preWarmSamplesCount)
            {
                _getTimespan = getTimespan;

                _targetToResults = new(preWarmTargetCount);
                _nullResults = new(preWarmTargetCount);
                _emptyTargets = new(preWarmTargetCount);

                for (int i = 0; i < preWarmTargetCount; ++i)
                {
                    ListPool<(float, TData)>.Release(new(preWarmSamplesCount));
                }
            }

            /// <summary>
            /// Creates a new consensus filter with a default allocation of memory for targets and data points.
            /// </summary>
            /// <param name="getTimespan">Func that returns a time span. Represents the
            /// length of time to "dwell" on a target before considering it valid.</param>
            public ConsensusFilter(Func<float> getTimespan) : this(getTimespan, 16, 32) { }

            public void AddResult(ulong? target, TData result, float timestamp)
            {
                _mostRecentTimestamp = timestamp;
                if (!target.HasValue)
                {
                    _nullResults.Add(new(timestamp, result));
                }
                else
                {
                    if (!_targetToResults.TryGetValue(target.Value, out var results))
                    {
                        results = ListPool<(float, TData)>.Get();
                        _targetToResults.Add(target.Value, results);
                    }
                    results.Add(new(timestamp, result));
                }
                _currentConsensus = null;
            }

            public TData? GetConsensus()
            {
                float newTimespan = _getTimespan();
                if (!Mathf.Approximately(newTimespan, _currentConsensusTimespan))
                {
                    _currentConsensus = null;
                }
                if (_currentConsensus == null)
                {
                    CalculateConsensus(newTimespan);
                }
                return _currentConsensus;
            }

            private void CalculateConsensus(float timespan)
            {
                _currentConsensusTimespan = timespan;
                _currentConsensus = null;
                int consensusCount = -1;
                float removalThreshold = _mostRecentTimestamp - timespan;

                void UpdateTargetAndResults(ulong? target, List<(float, TData)> results)
                {
                    int toRemoveCount;
                    for (toRemoveCount = 0; toRemoveCount < results.Count && results[toRemoveCount].Item1 < removalThreshold; ++toRemoveCount) ;
                    if (target.HasValue && toRemoveCount == results.Count)
                    {
                        _emptyTargets.Add(target.Value);
                    }
                    else
                    {
                        results.RemoveRange(0, toRemoveCount);

                        if (results.Count > consensusCount)
                        {
                            consensusCount = results.Count;
                            _currentConsensus = results[consensusCount - 1].Item2;
                        }
                    }
                }

                foreach (var pair in _targetToResults)
                {
                    UpdateTargetAndResults(pair.Key, pair.Value);
                }
                UpdateTargetAndResults(null, _nullResults);

                foreach (var target in _emptyTargets)
                {
                    if (_targetToResults.TryGetValue(target, out var results))
                    {
                        _targetToResults.Remove(target);
                        ListPool<(float, TData)>.Release(results);
                    }
                }
                _emptyTargets.Clear();
            }
        }
    }
}
