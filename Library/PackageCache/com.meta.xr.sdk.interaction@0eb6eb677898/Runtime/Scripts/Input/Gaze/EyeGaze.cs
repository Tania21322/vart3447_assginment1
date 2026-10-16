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

namespace Oculus.Interaction.Input
{
    [Experimental]
    public class EyeGaze : DataModifier<EyeGazeDataAsset>, IGaze, IActiveState
    {
        [Tooltip("If true, the camera pose from the source data asset " +
            "will replace the source eye pose data.")]
        [SerializeField]
        private bool _emulateGazeWithCameraPose = false;

        /// <summary>
        /// If true, the camera pose from the source data asset will replace
        /// the source eye pose data.
        /// </summary>
        public bool EmulateGazeWithCameraPose
        {
            get => _emulateGazeWithCameraPose;
            set => _emulateGazeWithCameraPose = value;
        }

        public bool Active { get => IsTrackedDataValid; }

        public event Action WhenUpdated = delegate { };

        public bool IsSyntheticPose
        {
            get => GetData().WorldPoseOrigin == PoseOrigin.SyntheticPose;
        }

        public bool IsTrackedDataValid
        {
            get => GetData().IsTracked;
        }

        protected override void Apply(EyeGazeDataAsset data)
        {
            if (_emulateGazeWithCameraPose)
            {
                data.IsTracked = true;
                data.WorldPose = data.CameraWorldPose;
                data.WorldPoseOrigin = PoseOrigin.SyntheticPose;
            }
        }

        public override void MarkInputDataRequiresUpdate()
        {
            base.MarkInputDataRequiresUpdate();

            if (Started)
            {
                WhenUpdated();
            }
        }

        public bool TryGetWorldPose(out Pose worldPose)
        {
            var data = GetData();
            worldPose = data.WorldPose;
            return data.IsTracked;
        }

        #region Inject

        #endregion
    }
}
