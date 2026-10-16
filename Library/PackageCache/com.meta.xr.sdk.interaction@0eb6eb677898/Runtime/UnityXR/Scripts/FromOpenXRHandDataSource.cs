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

using Oculus.Interaction.GrabAPI;
using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace Oculus.Interaction.Input.UnityXR
{
    /// <summary>
    ///   <para>Accepts OpenXR Hand Data returning an ISDK compatible HandDataAsset.</para>
    /// </summary>
    public abstract class FromOpenXRHandDataSource : DataSource<HandDataAsset>
    {
#if ISDK_OPENXR_HAND
// class should only exist if !ISDK_OPENXR_HAND
#else
        [Serializable]
        public class OpenXRHandDataAsset : ICopyFrom<OpenXRHandDataAsset>
        {
            public static class Constants
            {
                public const int NUM_HAND_JOINTS = 26;
                public const int NUM_FINGERS = 5;
            }

            [Flags]
            public enum JointTrackingState
            {
                None = 0,
                Radius = 1,
                Pose = 2,
                LinearVelocity = 4,
                AngularVelocity = 8,
                WillNeverBeValid = 16
            }

            [Flags]
            [Preserve]
            public enum AimFlagsFB : ulong
            {
                None = 0,
                Computed = 1,
                Valid = 2,
                IndexPinching = 4,
                MiddlePinching = 8,
                RingPinching = 16, // 0x0000000000000010
                LittlePinching = 32, // 0x0000000000000020
                SystemGesture = 64, // 0x0000000000000040
                DominantHand = 128, // 0x0000000000000080
                MenuPressed = 256, // 0x0000000000000100
            }

            public bool IsDataValid;
            public bool IsConnected;
            public bool IsTracked;

            // XR_EXT_hand_tracking
            public Pose Root;
            public PoseOrigin RootPoseOrigin;
            public JointTrackingState[] JointStates = new JointTrackingState[Constants.NUM_HAND_JOINTS];
            public Pose[] JointPoses = new Pose[Constants.NUM_HAND_JOINTS];
            public float[] JointRadiuses = new float[Constants.NUM_HAND_JOINTS];
            public Vector3[] JointAngularVelocities = new Vector3[Constants.NUM_HAND_JOINTS];
            public Vector3[] JointLinearVelocities = new Vector3[Constants.NUM_HAND_JOINTS];

            // XR_FB_hand_tracking_aim
            public AimFlagsFB AimFlags;
            public float[] FingerPinchStrength = new float[Constants.NUM_FINGERS];
            public Pose PointerPose;
            public PoseOrigin PointerPoseOrigin;

            public HandDataSourceConfig Config = new();
            public void CopyFrom(OpenXRHandDataAsset source)
            {
                IsDataValid = source.IsDataValid;
                IsConnected = source.IsConnected;
                IsTracked = source.IsTracked;
                AimFlags = source.AimFlags;
                Config = source.Config;
                CopyPosesFrom(source);
            }

            private void CopyPosesFrom(OpenXRHandDataAsset source)
            {
                Root = source.Root;
                RootPoseOrigin = source.RootPoseOrigin;
                Array.Copy(source.JointStates, JointStates, Constants.NUM_HAND_JOINTS);
                Array.Copy(source.JointPoses, JointPoses, Constants.NUM_HAND_JOINTS);
                Array.Copy(source.JointRadiuses, JointRadiuses, Constants.NUM_HAND_JOINTS);
                Array.Copy(source.JointAngularVelocities, JointAngularVelocities, Constants.NUM_HAND_JOINTS);
                Array.Copy(source.JointLinearVelocities, JointLinearVelocities, Constants.NUM_HAND_JOINTS);

                Array.Copy(source.FingerPinchStrength, FingerPinchStrength, FingerPinchStrength.Length);
                PointerPose = source.PointerPose;
                PointerPoseOrigin = source.PointerPoseOrigin;
                Config = source.Config;
            }
        }

        protected abstract OpenXRHandDataAsset OpenXRData { get; }
#endif
        private readonly static float DefaultSkeletonIndexMagnitude = HandSkeleton.DefaultLeftSkeleton[(int)HandJointId.HandIndex2].pose.position
            .magnitude;

        private const float PressThreshold = 0.8f;
        static readonly Vector3 TrackedRemoteAimOffset = new(0.0f, 0.0f, -0.055f);


#if ISDK_OPENXR_HAND
        protected readonly HandDataAsset _dataAsset = new();
#else
        private readonly HandDataAsset _dataAsset = new();
#endif

        // Meta Hand Aim Mocking
#if ISDK_OPENXR_HAND
        protected bool _shouldMockHandTrackingAim = false;
        private HandDataAssetHand _handDataAssetHand;

        [Tooltip("Optional pinch detection API. If not provided, a default PinchGrabConfigurableAPI will be created.")]
        [SerializeField, Interface(typeof(IFingerAPI)), Optional]
        private UnityEngine.Object _pinchAPI;
        private IFingerAPI PinchAPI { get; set; }
#endif

        protected virtual void Awake()
        {
#if ISDK_OPENXR_HAND
            if (PinchAPI == null)
            {
                PinchAPI = _pinchAPI as IFingerAPI;
            }
#endif
        }

        protected override void Start()
        {
            this.BeginStart(ref _started, () => base.Start());
            this.EndStart(ref _started);
        }

        protected override void UpdateData()
        {
#if ISDK_OPENXR_HAND
            // Legacy local rotations
            for (int i = 0; i < Constants.NUM_HAND_JOINTS; i++)
            {
                int parent = (int)HandJointUtils.JointParentList[i];
#pragma warning disable 0618
                _dataAsset.Joints[i] = parent < 0 ? Quaternion.identity :
                    Quaternion.Inverse(_dataAsset.JointPoses[parent].rotation) *
                    _dataAsset.JointPoses[i].rotation;
#pragma warning restore 0618
            }

            UpdateHandScale(
                _dataAsset.JointPoses[(int)HandJointId.HandIndex1].position, // IndexProximal
                _dataAsset.JointPoses[(int)HandJointId.HandIndex2].position); // IndexIntermediate

            // if XR_FB_hand_tracking_aim is unavailable
            if (_dataAsset.IsDataValidAndConnected && _shouldMockHandTrackingAim)
            {
                PopulateMockHandTrackingAim(_dataAsset.JointPoses[0]);
            }
#else
            var openXRData = OpenXRData;
            _dataAsset.CopyFrom(openXRData);

            UpdateHandScale(
                OpenXRData.JointPoses[7].position, // IndexProximal
                OpenXRData.JointPoses[8].position); // IndexIntermediate

            // if XR_FB_hand_tracking_aim is unavailable
            if (_dataAsset.IsDataValidAndConnected && openXRData.AimFlags == OpenXRHandDataAsset.AimFlagsFB.None)
            {
                PopulateMockHandTrackingAim(openXRData.JointPoses[0]);
            }
#endif
        }

        private void UpdateHandScale(Vector3 indexProximal, Vector3 indexIntermediate)
        {
            // calculate scale comparing Index Proximal -> Intermediate distance
            var indexDistance = Vector3.Distance(
                indexProximal,
                indexIntermediate);
            _dataAsset.HandScale = indexDistance / DefaultSkeletonIndexMagnitude;
#if ISDK_OPENXR_HAND
            // normalize joint poses
            var unscaleFactor = 1 / _dataAsset.HandScale;
            for (int i = 0; i < Constants.NUM_HAND_JOINTS; i++)
            {
                _dataAsset.JointPoses[i].position *= unscaleFactor;
            }
#endif
        }

        private void PopulateMockHandTrackingAim(Pose xrPalmPose)
        {
            _dataAsset.PointerPose =
            xrPalmPose.GetTransformedBy(new Pose(TrackedRemoteAimOffset, Quaternion.identity));
            _dataAsset.PointerPoseOrigin = PoseOrigin.SyntheticPose;
            _dataAsset.IsDominantHand = _dataAsset.Config.Handedness == Handedness.Right;

#if ISDK_OPENXR_HAND
            if(PinchAPI == null)
            {
                PinchAPI = this.gameObject.AddComponent<PinchGrabConfigurableAPI>();
            }
            if (_handDataAssetHand == null)
            {
                _handDataAssetHand = new HandDataAssetHand();
            }
            _handDataAssetHand.Update(_dataAsset);
            PinchAPI.Update(_handDataAssetHand);
#endif
            PopulateMockHandTrackingAimFinger(HandFinger.Index);
            PopulateMockHandTrackingAimFinger(HandFinger.Middle);
            PopulateMockHandTrackingAimFinger(HandFinger.Ring);
            PopulateMockHandTrackingAimFinger(HandFinger.Pinky);
        }

        private void PopulateMockHandTrackingAimFinger(HandFinger finger)
        {
            var fingerIndex = (int)finger;

#if ISDK_OPENXR_HAND
            _dataAsset.FingerPinchStrength[fingerIndex] =
                PinchAPI.GetFingerGrabScore(finger);
#else
            _dataAsset.FingerPinchStrength[fingerIndex] = 0.0f;
#endif
            _dataAsset.IsFingerPinching[fingerIndex] =
                _dataAsset.FingerPinchStrength[fingerIndex] > PressThreshold;
        }

        protected override HandDataAsset DataAsset => _dataAsset;

        #region Inject

#if ISDK_OPENXR_HAND
        /// <summary>
        /// Injects an optional custom <see cref="IFingerAPI"/> implementation for pinch detection.
        /// If not provided, a default <see cref="PinchGrabConfigurableAPI"/> will be created at runtime.
        /// </summary>
        /// <param name="fingerPinchAPI">The custom pinch <see cref="IFingerAPI"/> to inject.</param>
        public void InjectOptionalFingerPinchAPI(IFingerAPI fingerPinchAPI)
        {
            _pinchAPI = fingerPinchAPI as UnityEngine.Object;
            PinchAPI = fingerPinchAPI;
        }
#endif

        #endregion

#if ISDK_OPENXR_HAND
        private class HandDataAssetHand : IHand
        {
            private HandDataAsset _asset;
            private HandJointCache _jointPosesCache;
            private int _dataVersion;

            public void Update(HandDataAsset asset)
            {
                _asset = asset;
                _dataVersion++;
                if (_jointPosesCache == null && asset.IsDataValidAndConnected)
                {
                    _jointPosesCache = new HandJointCache();
                }
                _jointPosesCache?.Update(asset, _dataVersion);
            }

            public Handedness Handedness => _asset.Config.Handedness;
            public bool IsConnected => _asset.IsDataValidAndConnected;
            public bool IsHighConfidence => _asset.IsHighConfidence;
            public bool IsDominantHand => _asset.IsDominantHand;
            public float Scale => _asset.HandScale;
            public bool IsPointerPoseValid => _asset.PointerPoseOrigin != PoseOrigin.None;
            public bool IsTrackedDataValid => _asset.IsTracked;
            public int CurrentDataVersion => _dataVersion;

            public event Action WhenHandUpdated { add { } remove { } }

            public bool GetFingerIsPinching(HandFinger finger)
            {
                return _asset.IsFingerPinching[(int)finger];
            }

            public bool GetIndexFingerIsPinching()
            {
                return GetFingerIsPinching(HandFinger.Index);
            }

            public bool GetPointerPose(out Pose pose)
            {
                pose = _asset.PointerPose;
                return IsPointerPoseValid;
            }

            public bool GetJointPose(HandJointId handJointId, out Pose pose)
            {
                pose = Pose.identity;
                if (!IsTrackedDataValid || _jointPosesCache == null)
                {
                    return false;
                }
                pose = _jointPosesCache.GetWorldJointPose(handJointId);
                return true;
            }

            public bool GetJointPoseLocal(HandJointId handJointId, out Pose pose)
            {
                pose = Pose.identity;
                if (!GetJointPosesLocal(out var localJointPoses))
                {
                    return false;
                }
                pose = localJointPoses[(int)handJointId];
                return true;
            }

            public bool GetJointPosesLocal(out ReadOnlyHandJointPoses localJointPoses)
            {
                if (!IsTrackedDataValid || _jointPosesCache == null)
                {
                    localJointPoses = ReadOnlyHandJointPoses.Empty;
                    return false;
                }
                return _jointPosesCache.GetAllLocalPoses(out localJointPoses);
            }

            public bool GetJointPoseFromWrist(HandJointId handJointId, out Pose pose)
            {
                pose = Pose.identity;
                if (!GetJointPosesFromWrist(out var jointPosesFromWrist))
                {
                    return false;
                }
                pose = jointPosesFromWrist[(int)handJointId];
                return true;
            }

            public bool GetJointPosesFromWrist(out ReadOnlyHandJointPoses jointPosesFromWrist)
            {
                if (!IsTrackedDataValid || _jointPosesCache == null)
                {
                    jointPosesFromWrist = ReadOnlyHandJointPoses.Empty;
                    return false;
                }
                return _jointPosesCache.GetAllPosesFromWrist(out jointPosesFromWrist);
            }

            public bool GetPalmPoseLocal(out Pose pose)
            {
                pose = _asset.Root;
                return IsTrackedDataValid;
            }

            public bool GetFingerIsHighConfidence(HandFinger finger)
            {
                return _asset.IsFingerHighConfidence[(int)finger];
            }

            public float GetFingerPinchStrength(HandFinger finger)
            {
                return _asset.FingerPinchStrength[(int)finger];
            }

            public bool GetRootPose(out Pose pose)
            {
                pose = _asset.Root;
                return IsTrackedDataValid;
            }
        }
#endif
    }
}
