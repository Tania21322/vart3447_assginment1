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

namespace Oculus.Interaction
{
    /// <summary>
    /// This provider maintains the Source pose at a fixed offset from the Target pose,
    /// as if the Source was parented to the Target
    /// </summary>
    [Experimental]
    public class MaintainOffsetFromTargetProvider : MonoBehaviour, IMovementProvider
    {
        public IMovement CreateMovement()
        {
            return new MaintainOffsetFromTarget();
        }
    }

    public class MaintainOffsetFromTarget : IMovement
    {
        public Pose Pose => _current;
        public bool Stopped => true;

        private Pose _current = Pose.identity;
        private Pose _originalTarget;
        private Pose _originalSource;

        private Pose _offset = Pose.identity;

        public void MoveTo(Pose target)
        {
            _originalTarget = target;
            _offset = PoseUtils.Delta(_originalTarget, _originalSource);
        }

        public void UpdateTarget(Pose target)
        {
            _current = PoseUtils.Multiply(target, _offset);
        }

        public void StopAndSetPose(Pose source)
        {
            _current = _originalSource = source;
        }

        public void Tick()
        {
        }
    }
}
