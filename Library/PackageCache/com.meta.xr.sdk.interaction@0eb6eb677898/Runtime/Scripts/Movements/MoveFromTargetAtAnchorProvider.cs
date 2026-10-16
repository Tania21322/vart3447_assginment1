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
    /// Moves the selected interactable anchored to the Interactor at the hit point.
    /// </summary>
    public class MoveFromTargetAtAnchorProvider : MonoBehaviour, IMovementProvider
    {
        public IMovement CreateMovement()
        {
            return new MoveFromTargetAtAnchor();
        }
    }

    public class MoveFromTargetAtAnchor : IMovement
    {
        public Pose Pose { get; private set; } = Pose.identity;
        public bool Stopped => true;
        private Pose _originalSource = Pose.identity;
        private Pose _offset = Pose.identity;

        public void StopMovement()
        {
        }

        public void MoveTo(Pose target)
        {
            _offset = PoseUtils.Delta(target, _originalSource);
            UpdateTarget(target);
        }

        public void UpdateTarget(Pose target)
        {
            Pose = PoseUtils.Multiply(target, _offset);
        }

        public void StopAndSetPose(Pose source)
        {
            Pose = _originalSource = source;
        }

        public void Tick()
        {
        }
    }
}
