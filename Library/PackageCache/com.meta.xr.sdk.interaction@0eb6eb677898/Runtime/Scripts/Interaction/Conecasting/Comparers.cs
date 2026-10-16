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
using System;
using System.Collections.Generic;

namespace Oculus.Interaction
{
    public enum ConecastComparerType
    {
        [Tooltip("Candidates will be sorted by angle, followed by distance.")]
        [InspectorName("Sort by angle, then distance")]
        AngleDistance,
        [Tooltip("Candidates will be sorted by distance, followed by angle.")]
        [InspectorName("Sort by distance, then angle")]
        DistanceAngle,
    }

    [Serializable]
    public class ConecastResultComparer : IComparer<ConecastHit>
    {
        [SerializeField]
        [Tooltip("The default comparer used to score interactables.")]
        private ConecastComparerType _comparerType = ConecastComparerType.AngleDistance;

        [SerializeField]
        [Tooltip("(Meters, World) The threshold below which distances to a surface " +
            "are treated as equal for the purposes of ranking.")]
        private float _equalDistanceThreshold = 0.001f;

        [SerializeField]
        [Tooltip("(Degrees, World) The threshold below which angular offsets from the gaze ray" +
                 "are treated as equal for the purposes of ranking.")]
        private float _equalAngleThreshold = 0.001f;

        /// <summary>
        /// Sets the default comparer used to score interactables.
        /// </summary>
        public ConecastComparerType ComparerType { get => _comparerType; set => _comparerType = value; }

        /// <summary>
        /// (Meters, World) The threshold below which distances to a surface are treated as equal for the purposes of ranking.
        /// </summary>
        public float EqualDistanceThreshold { get => _equalDistanceThreshold; set => _equalDistanceThreshold = value; }

        /// <summary>
        /// (Degrees, World) The threshold below which angular offsets from the gaze ray are treated as equal for the purposes of ranking.
        /// </summary>
        public float EqualAngleThreshold { get => _equalAngleThreshold; set => _equalAngleThreshold = value; }

        private DistanceAngleCompare _distanceAngle = new();
        private AngleDistanceCompare _angleDistance = new();

        public int Compare(ConecastHit x, ConecastHit y)
        {
            return GetComparer().Compare(x, y);
        }

        private IComparer<ConecastHit> GetComparer()
        {
            switch (_comparerType)
            {
                default:
                case ConecastComparerType.AngleDistance:
                    _angleDistance.EqualDistanceThreshold = EqualDistanceThreshold;
                    _angleDistance.EqualAngleThreshold = EqualAngleThreshold;
                    return _angleDistance;
                case ConecastComparerType.DistanceAngle:
                    _distanceAngle.EqualDistanceThreshold = EqualDistanceThreshold;
                    _distanceAngle.EqualAngleThreshold = EqualAngleThreshold;
                    return _distanceAngle;
            }
        }
    }

    public class AngleDistanceCompare : IComparer<ConecastHit>
    {
        public float EqualDistanceThreshold { get; set; }
        public float EqualAngleThreshold { get; set; }

        public int Compare(ConecastHit x, ConecastHit y)
        {
            float degreeDelta = Mathf.Abs(x.DegreesFromConeOrigin - y.DegreesFromConeOrigin);
            if (degreeDelta > EqualAngleThreshold)
            {
                return x.DegreesFromConeOrigin.CompareTo(y.DegreesFromConeOrigin);
            }

            float distanceDelta = Mathf.Abs(x.DistanceFromConeOrigin - y.DistanceFromConeOrigin);
            if (distanceDelta > EqualDistanceThreshold)
            {
                return x.DistanceFromConeOrigin.CompareTo(y.DistanceFromConeOrigin);
            }

            return 0;
        }
    }

    public class DistanceAngleCompare : IComparer<ConecastHit>
    {
        public float EqualDistanceThreshold { get; set; }
        public float EqualAngleThreshold { get; set; }

        public int Compare(ConecastHit x, ConecastHit y)
        {
            float distanceDelta = Mathf.Abs(x.DistanceFromConeOrigin - y.DistanceFromConeOrigin);
            if (distanceDelta > EqualDistanceThreshold)
            {
                return x.DistanceFromConeOrigin.CompareTo(y.DistanceFromConeOrigin);
            }

            float degreeDelta = Mathf.Abs(x.DegreesFromConeOrigin - y.DegreesFromConeOrigin);
            if (degreeDelta > EqualAngleThreshold)
            {
                return x.DegreesFromConeOrigin.CompareTo(y.DegreesFromConeOrigin);
            }

            return 0;
        }
    }
}
