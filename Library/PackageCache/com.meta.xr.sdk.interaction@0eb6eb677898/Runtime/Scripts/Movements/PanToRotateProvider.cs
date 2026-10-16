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
    /// This provider is rotation-only, and rotates the Source along the
    /// movement delta of the Target pose.
    /// </summary>
    [Experimental]
    public class PanToRotateProvider : MonoBehaviour, IMovementProvider
    {
        [SerializeField, Min(0f)]
        [Tooltip("The amount of rotation relative to target motion, where " +
            "a value of 1 results in a 360 degree source rotation per unit " +
            "of hand motion.")]
        private float _sensitivity = 1f;

        public IMovement CreateMovement()
        {
            return new PanToRotate() { Sensitivity = _sensitivity };
        }
    }

    public class PanToRotate : IMovement
    {
        /// <summary>
        /// The amount of rotation relative to target motion, where a
        /// value of 1 results in a 360 degree source rotation per
        /// unit of hand motion.
        /// </summary>
        public float Sensitivity { get; set; } = 1f;

        public Pose Pose => _current;
        public bool Stopped => true;

        private Pose _current = Pose.identity;
        private Pose _originalTarget;
        private Pose _currentTarget;
        private Pose _originalSource;

        public void MoveTo(Pose target)
        {
            _originalTarget = _currentTarget = target;
        }

        public void UpdateTarget(Pose target)
        {
            Vector3 deltaPos = target.position - _currentTarget.position;
            Vector3 originalTargetToCurrent = _current.position - _originalTarget.position;
            Vector3 axis = Vector3.Cross(deltaPos, originalTargetToCurrent);
            float angle = deltaPos.magnitude * (Sensitivity * 360);
            angle *= 1 - Mathf.Abs(Vector3.Dot(originalTargetToCurrent.normalized, deltaPos.normalized));
            Quaternion rotationDelta = Quaternion.AngleAxis(angle, axis);
            _current.rotation = rotationDelta * _originalSource.rotation;
            _originalSource = _current;
            _currentTarget = target;
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
