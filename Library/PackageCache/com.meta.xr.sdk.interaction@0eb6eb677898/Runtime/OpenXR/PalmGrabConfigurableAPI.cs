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
using Oculus.Interaction.Input;
using Oculus.Interaction.PoseDetection;

namespace Oculus.Interaction.GrabAPI
{
    /// <summary>
    /// This Finger API uses the curl value of the fingers to detect if they are grabbing
    /// </summary>
    public class PalmGrabConfigurableAPI : MonoBehaviour, IFingerAPI
    {
        [SerializeField, Range(0f, 1f)]
        [Tooltip("The normalized grab strength threshold at which a finger is considered to start grabbing.")]
        private float _startThreshold = 0.9f;
        /// <summary>
        /// The normalized grab strength threshold at which a finger is considered to start grabbing.
        /// </summary>
        public float StartThreshold
        {
            get => _startThreshold;
            set => _startThreshold = value;
        }

        [SerializeField, Range(0f, 1f)]
        [Tooltip("The normalized grab strength threshold at which a finger is considered to release the grab.")]
        private float _releaseThreshold = 0.85f;
        /// <summary>
        /// The normalized grab strength threshold at which a finger is considered to release the grab.
        /// </summary>
        public float ReleaseThreshold
        {
            get => _releaseThreshold;
            set => _releaseThreshold = value;
        }

        [Section("Curl ranges")]
        [SerializeField]
        [Tooltip("The curl angle range (min, max) used to normalize the thumb grab strength.")]
        private Vector2 _thumbRange = _defaultCurlRanges[(int)HandFinger.Thumb];
        /// <summary>
        /// The curl angle range (min, max) used to normalize the thumb grab strength.
        /// </summary>
        public Vector2 ThumbRange
        {
            get => _thumbRange;
            set
            {
                _thumbRange = value;
                SetCurlRange(HandFinger.Thumb, value);
            }
        }

        [Section("Curl ranges")]
        [SerializeField]
        [Tooltip("The curl angle range (min, max) used to normalize the index finger grab strength.")]
        private Vector2 _indexRange = _defaultCurlRanges[(int)HandFinger.Index];
        /// <summary>
        /// The curl angle range (min, max) used to normalize the index finger grab strength.
        /// </summary>
        public Vector2 IndexRange
        {
            get => _indexRange;
            set
            {
                _indexRange = value;
                SetCurlRange(HandFinger.Index, value);
            }
        }

        [Section("Curl ranges")]
        [SerializeField]
        [Tooltip("The curl angle range (min, max) used to normalize the middle finger grab strength.")]
        private Vector2 _middleRange = _defaultCurlRanges[(int)HandFinger.Middle];
        /// <summary>
        /// The curl angle range (min, max) used to normalize the middle finger grab strength.
        /// </summary>
        public Vector2 MiddleRange
        {
            get => _middleRange;
            set
            {
                _middleRange = value;
                SetCurlRange(HandFinger.Middle, value);
            }
        }

        [Section("Curl ranges")]
        [SerializeField]
        [Tooltip("The curl angle range (min, max) used to normalize the ring finger grab strength.")]
        private Vector2 _ringRange = _defaultCurlRanges[(int)HandFinger.Ring];
        /// <summary>
        /// The curl angle range (min, max) used to normalize the ring finger grab strength.
        /// </summary>
        public Vector2 RingRange
        {
            get => _ringRange;
            set
            {
                _ringRange = value;
                SetCurlRange(HandFinger.Ring, value);
            }
        }

        [Section("Curl ranges")]
        [SerializeField]
        [Tooltip("The curl angle range (min, max) used to normalize the pinky finger grab strength.")]
        private Vector2 _pinkyRange = _defaultCurlRanges[(int)HandFinger.Pinky];
        /// <summary>
        /// The curl angle range (min, max) used to normalize the pinky finger grab strength.
        /// </summary>
        public Vector2 PinkyRange
        {
            get => _pinkyRange;
            set
            {
                _pinkyRange = value;
                SetCurlRange(HandFinger.Pinky, value);
            }
        }

        private static readonly Vector3 _poseVolumeOffset = new Vector3(0.07f, -0.03f, 0.0f);
        private static readonly Vector2[] _defaultCurlRanges = new Vector2[]
        {
            new Vector2(190f, 220f),
            new Vector2(180f, 250f),
            new Vector2(180f, 250f),
            new Vector2(180f, 250f),
            new Vector2(180f, 245f),
        };

        private Vector3 _poseVolumeCenterOffset = Vector3.zero;
        private FingerShapes _fingerShapes = new FingerShapes();
        private FingerGrabData[] _fingersGrabData = new FingerGrabData[]
        {
            new FingerGrabData(HandFinger.Thumb, _defaultCurlRanges[(int)HandFinger.Thumb]),
            new FingerGrabData(HandFinger.Index, _defaultCurlRanges[(int)HandFinger.Index]),
            new FingerGrabData(HandFinger.Middle, _defaultCurlRanges[(int)HandFinger.Middle]),
            new FingerGrabData(HandFinger.Ring, _defaultCurlRanges[(int)HandFinger.Ring]),
            new FingerGrabData(HandFinger.Pinky, _defaultCurlRanges[(int)HandFinger.Pinky])
        };

        private class FingerGrabData
        {
            private readonly HandFinger _fingerID;
            private Vector2 _curlNormalizationParams;

            public float GrabStrength;
            public bool IsGrabbing;
            public bool IsGrabbingChanged { get; private set; }

            public FingerGrabData(HandFinger fingerId, Vector2 curlRange)
            {
                _fingerID = fingerId;
                SetCurlRange(curlRange);
            }

            public void SetCurlRange(Vector2 curlRange)
            {
                _curlNormalizationParams = new Vector2(curlRange.x, 1f / (curlRange.y - curlRange.x));
            }

            public void UpdateGrabStrength(IHand hand, FingerShapes fingerShapes)
            {
                float curlAngle = fingerShapes.GetCurlValue(_fingerID, hand);
                if (_fingerID != HandFinger.Thumb)
                {
                    curlAngle = (curlAngle * 2 + fingerShapes.GetFlexionValue(_fingerID, hand)) / 3f;
                }
                GrabStrength = Mathf.Clamp01((curlAngle - _curlNormalizationParams.x) * _curlNormalizationParams.y);
            }

            public void UpdateIsGrabbing(float startThreshold, float releaseThreshold)
            {
                if (GrabStrength > startThreshold)
                {
                    if (!IsGrabbing)
                    {
                        IsGrabbing = true;
                        IsGrabbingChanged = true;
                    }
                    return;
                }

                if (GrabStrength < releaseThreshold)
                {
                    if (IsGrabbing)
                    {
                        IsGrabbing = false;
                        IsGrabbingChanged = true;
                    }
                }
            }

            public void ClearState()
            {
                IsGrabbingChanged = false;
            }
        }

        protected virtual void Awake()
        {
            SetCurlRange(HandFinger.Thumb, ThumbRange);
            SetCurlRange(HandFinger.Index, IndexRange);
            SetCurlRange(HandFinger.Middle, MiddleRange);
            SetCurlRange(HandFinger.Ring, RingRange);
            SetCurlRange(HandFinger.Pinky, PinkyRange);
        }

        bool IFingerAPI.GetFingerIsGrabbing(HandFinger finger)
        {
            return _fingersGrabData[(int)finger].IsGrabbing;
        }

        bool IFingerAPI.GetFingerIsGrabbingChanged(HandFinger finger, bool targetGrabState)
        {
            return _fingersGrabData[(int)finger].IsGrabbingChanged &&
                   _fingersGrabData[(int)finger].IsGrabbing == targetGrabState;
        }

        float IFingerAPI.GetFingerGrabScore(HandFinger finger)
        {
            return _fingersGrabData[(int)finger].GrabStrength;
        }

        void IFingerAPI.Update(IHand hand)
        {
            ClearState();

            if (hand == null || !hand.IsTrackedDataValid)
            {
                return;
            }

            UpdateVolumeCenter(hand);

            for (int i = 0; i < Constants.NUM_FINGERS; ++i)
            {
                _fingersGrabData[i].UpdateGrabStrength(hand, _fingerShapes);
                _fingersGrabData[i].UpdateIsGrabbing(StartThreshold, ReleaseThreshold);
            }
        }

        Vector3 IFingerAPI.GetWristOffsetLocal()
        {
            return _poseVolumeCenterOffset;
        }

        private void SetCurlRange(HandFinger finger, Vector2 range)
        {
            _fingersGrabData[(int)finger].SetCurlRange(range);
        }

        private void UpdateVolumeCenter(IHand hand)
        {
            _poseVolumeCenterOffset = hand.Handedness == Handedness.Left
                ? Constants.LeftDistal * _poseVolumeOffset.x
                    + Constants.LeftDorsal * _poseVolumeOffset.y
                    + Constants.LeftThumbSide * _poseVolumeOffset.z
                : Constants.RightDistal * _poseVolumeOffset.x
                    + Constants.RightDorsal * _poseVolumeOffset.y
                    + Constants.RightThumbSide * _poseVolumeOffset.z;
        }

        private void ClearState()
        {
            for (int i = 0; i < Constants.NUM_FINGERS; ++i)
            {
                _fingersGrabData[i].ClearState();
            }
        }
    }
}
