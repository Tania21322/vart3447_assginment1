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
using UnityEngine.Serialization;

namespace Oculus.Interaction.Input
{
    [Experimental]
    public class EyeGazeRef : MonoBehaviour, IGaze, IActiveState
    {
        [SerializeField, Interface(typeof(IGaze))]
        private UnityEngine.Object _gaze;

        internal IGaze Gaze { get; set; }

        private void Awake()
        {
            if (Gaze == null)
            {
                Gaze = _gaze as IGaze;
            }
        }

        void Start()
        {
            this.AssertField(_gaze, nameof(_gaze));
        }

        public bool TryGetWorldPose(out Pose worldPose)
        {
            return Gaze.TryGetWorldPose(out worldPose);
        }

        public event Action WhenUpdated
        {
            add => Gaze.WhenUpdated += value;
            remove => Gaze.WhenUpdated -= value;
        }

        public bool IsTrackedDataValid { get => Gaze.IsTrackedDataValid; }

        public bool Active { get => IsTrackedDataValid; }

        #region Inject

        public void InjectAllEyeGazeRef(IGaze gaze)
        {
            InjectGaze(gaze);
        }

        public void InjectGaze(IGaze gaze)
        {
            _gaze = gaze as UnityEngine.Object;
            Gaze = gaze;
        }
        #endregion

    }
}
