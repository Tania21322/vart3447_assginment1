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
    /// This provider links the deltas from the Target pose to the Source pose,
    /// so the Source's transformations match the Target but in the Source's
    /// own local space. Whatever the target does, the source does.
    /// </summary>
    [Experimental]
    public class MatchTargetAtSourceProvider : MonoBehaviour, IMovementProvider
    {
        public IMovement CreateMovement()
        {
            return new MatchTargetAtSource();
        }
    }

    public class MatchTargetAtSource : IMovement
    {
        public Pose Pose => _current;
        public bool Stopped => true;

        private Pose _current = Pose.identity;
        private Pose _originalTarget;
        private Pose _originalSource;

        public void MoveTo(Pose target)
        {
            _originalTarget = target;
        }

        public void UpdateTarget(Pose target)
        {
            Vector3 deltaPos = target.position - _originalTarget.position;
            Quaternion deltaRot = target.rotation * Quaternion.Inverse(_originalTarget.rotation);
            _current.position = _originalSource.position + deltaPos;
            _current.rotation = deltaRot * _originalSource.rotation;
            _originalSource = _current;
            _originalTarget = target;
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
