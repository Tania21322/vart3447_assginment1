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

using Oculus.Interaction.Input;
using System;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// This class demonstrates how to do custom movement mapping in the form of "coronary movement," where the movements provided
    /// are based on the estimated position of the user's heart/core. This should only be used in conjunction with a
    /// <see cref="GazeMultiGrab"/> component as it depends on assumptions which may not hold independent of that class.
    /// </summary>
    /// <remarks>
    /// Coronary movement --- movement which evaluates input relative to the position of the user's heart/core --- is provided here
    /// as an example of arbitrary motion remapping, where the movement provided is not directly from the user's inputs, but is
    /// calculated therefrom while considering other factors as well.
    /// </remarks>
    [Experimental]
    public class CoronaryMovementProvider : MonoBehaviour, IMovementProvider
    {
        [SerializeField, Interface(typeof(IHmd))]
        private MonoBehaviour _hmd;

        [SerializeField]
        [Tooltip("The offset (in meters) behind the HMD position at which to estimate the position of the occipital region of the skull.")]
        private float _occipitalOffset = 0.1f;

        [SerializeField]
        [Tooltip("The offset (in meters) below the occipital region of the skull at which to estimate the position of the heart.")]
        private float _coronaryOffset = 0.4f;

        [SerializeField]
        [Tooltip("A power which will be applied to the depth ratio when moving something toward or away from the user, speeding (when above 1) or slowing (when below 1) that motion.")]
        private float _depthRemappingFactor = 1.3f;

        [SerializeField]
        [Tooltip("Toggles whether rotation is set by billboarding or by target rotation.")]
        private bool _billboardToFaceUser = false;

        [SerializeField]
        [Tooltip("Offsets the pitch of the rotation set by billboarding.")]
        private float _billboardPitchOffset = 20f;


        /// <summary>
        /// Implementation of <see cref="IMovementProvider.CreateMovement"/>; for details, please refer to the related documentation
        /// provided for that interface.
        /// </summary>
        public IMovement CreateMovement()
        {
            _hmd ??= FindAnyObjectByType<Hmd>();
            return new CoronaryMovement(_hmd as IHmd, _occipitalOffset, _coronaryOffset, _depthRemappingFactor, _billboardToFaceUser, _billboardPitchOffset);
        }

        /// <summary>
        /// Sets the optional <see cref="IHmd"/> used to estimate the user's heart/core position. Supports the
        /// Interaction SDK dependency injection pattern; when unset, the provider falls back to the first
        /// <see cref="Hmd"/> found in the scene.
        /// </summary>
        public void InjectOptionalHmd(IHmd hmd)
        {
            _hmd = hmd as MonoBehaviour;
        }

        private class CoronaryMovement : IMovement
        {
            public Pose Pose => _currentPose;
            public bool Stopped => true;

            private IHmd _hmd;
            private float _occipitalOffset;
            private float _coronaryOffset;
            private float _depthRempapingFactor;
            private bool _billboard;
            private float _billboardPitch;

            private Pose _originalPose;
            private Pose _currentPose;

            private Pose _target;

            private Vector3 _originalCorePoint;
            private Vector3 _originalInteractionPoint;
            private Quaternion _inverseOriginalInteractionOrientation;

            /// <summary>
            /// The "core point" is the estimated position of the heart. It is calculated from the HMD position minus the occipital offset
            /// (to reach a point of the head which generally doesn't move as much, contrasted with the face, as the head looks around) minus
            /// the coronary offset (to reach a position more-or-less near the heart).
            /// </summary>
            /// <returns>The estimated position of the heart.</returns>
            private Vector3 getCorePoint()
            {
                _hmd.TryGetRootPose(out var pose);
                var occipitalToThirdEyeDirection = pose.forward;
                // NOTE: This assumes user is standing up, would need body estimates to deal with lying down.
                var coreToOccipitalDirection = Vector3.up;
                return pose.position - _occipitalOffset * occipitalToThirdEyeDirection - _coronaryOffset * coreToOccipitalDirection;
            }

            /// <summary>
            /// The "interaction point" is the position representing the user's control over the movement. <see cref="GazeMultiGrab"/> provides
            /// this position as a "target," which is NOT the position toward which the motion is expected to tend, but is merely an input
            /// value used to inspire the provided motion.
            /// </summary>
            /// <returns>The point in space which represents the user's control of the interaction</returns>
            private Vector3 getInteractionPoint()
            {
                return _target.position;
            }

            /// <summary>
            /// The "interaction orientation" is the rotation representing the user's control over the movement. <see cref="GazeMultiGrab"/>
            /// provides this orientation as a "target," which is NOT the orientation toward which the motion is expected to tend, but is merely
            /// an input value used to inspire the provided motion.
            /// </summary>
            /// <returns>The orientation which represents the user's control of the interaction</returns>
            private Quaternion getInteractionOrientation()
            {
                return _target.rotation;
            }

            /// <summary>
            /// Creates a new CoronaryMovement instance.
            /// </summary>
            /// <param name="hmd">The HMD from which heart/core position estimation will begin.</param>
            /// <param name="occipitalOffset">The distance (in meters) behind the HMD at which to estimate the occipital region of the skull.</param>
            /// <param name="coronaryOffset">The distance (in meters) below the occipital region of the skull at which to estimate the position of the heart.</param>
            /// <param name="depthRemappingFactor">A factor which can be used to speed or slow movement toward or away from the user.</param>
            /// <param name="billboard">Toggles whether orientation is set by billboarding or by target rotation</param>
            /// <param name="billboardPitch">Offsets the billboarded orientation by the provided pitch</param>
            public CoronaryMovement(IHmd hmd, float occipitalOffset, float coronaryOffset, float depthRemappingFactor, bool billboard, float billboardPitch)
            {
                _hmd = hmd;
                _occipitalOffset = occipitalOffset;
                _coronaryOffset = coronaryOffset;
                _depthRempapingFactor = depthRemappingFactor;
                _billboard = billboard;
                _billboardPitch = billboardPitch;
            }

            /// <summary>
            /// Signals the beginning of a period of movement. Despite its name (and breaking from the specified contract of
            /// <see cref="IMovement.MoveTo(Pose)"/>), the <paramref name="target"/> provided here is NOT a destination to which this movement
            /// should tend, nor is it related to the current position of what will be moved. Rather, <paramref name="target"/> is an "interaction
            /// pose" used to represent the user's intent in controlling the procedural animation encapsulated in this <see cref="IMovement"/>.
            /// This usage assumes that <see cref="StopAndSetPose(Pose)"/> has been invoked prior to the invocation of this method, as that is the
            /// only way the actual position of the thing being moved (as opposed to the interaction point inspiring the movement) is ever
            /// communicated to this <see cref="IMovement"/> instance.
            /// </summary>
            /// <param name="target">The "interaction pose" representing the user's intent in controlling this <see cref="IMovement"/></param>
            public void MoveTo(Pose target)
            {
                _target = target;

                _originalCorePoint = getCorePoint();
                _originalInteractionPoint = getInteractionPoint();
                _inverseOriginalInteractionOrientation = Quaternion.Inverse(getInteractionOrientation());
            }

            /// <summary>
            /// Updates the "interaction pose" being used to control movement; for details, see the documentation for <see cref="MoveTo(Pose)"/>.
            /// </summary>
            /// <param name="target">The new "interaction pose"</param>
            public void UpdateTarget(Pose target)
            {
                _target = target;
            }

            /// <summary>
            /// Updates this instance's "current pose" (which is NOT the "target" which is functioning as an "interaction pose," as discussed in
            /// <see cref="MoveTo(Pose)"/>, but is instead the actual position and orientation of the thing to be moved) to a new value. In the
            /// usage pattern leveraged by <see cref="GazeMultiGrab"/>, this MUST be called with the most up-to-date spatial information before
            /// calling <see cref="MoveTo(Pose)"/>.
            /// </summary>
            /// <param name="pose">The pose to be assessed as the current position of the thing being moved</param>
            public void StopAndSetPose(Pose pose)
            {
                _currentPose = _originalPose = pose;
            }

            /// <summary>
            /// Leverages the "interaction pose" information (set by <see cref="MoveTo(Pose)"/> and <see cref="UpdateTarget(Pose)"/>) along with
            /// the "current pose" information (originally set by <see cref="StopAndSetPose(Pose)"/> and updated by <see cref="Tick"/> since)
            /// to calculate a new "current pose," the value of which will be retrievable via <see cref="CoronaryMovement.Pose"/>.
            /// </summary>
            public void Tick()
            {
                var currentCorePoint = getCorePoint();
                var currentInteractionPoint = getInteractionPoint();

                // All the specialized arithmetic performed for CoronaryMovement is implemented in this method. For efficiency, some of this
                // (which depends only on "original" values) could be done in MoveTo() and cached, but since this is illustrative code, it's
                // all kept here to improve readability.

                // There are two states relevant to coronary movement: the "original" state and the "current" state. "Original" state is
                // the state from which movement began (for example, the moment a user gaze-grabbed something) and is designated by an
                // invocation of MoveTo() (which is StopAndSetPose() MUST be called with the most up-to-date values before MoveTo() codifies the
                // "original" state). "Current" state is the state at the present moment --- where the user's head is right now, where the
                // "interaction pose" is right now, etc.

                // Suppose the user gazes at something, then grabs it by "pinching" with their head which is off axis somewhere. At that moment,
                // an imaginary rigid "beam" is created from the user's heart to the grabbed object, with an imaginary "handle on a stick" coming
                // off that beam and going to the user's hand. From this moment on, the user's moving hand will move the "handle on a stick,"
                // which levers the beam to move the object. At a high level, this is what coronary movement means.

                // The "beam" connecting the user's heart to the moved object.
                var originalTargetDelta = _originalPose.position - _originalCorePoint;

                // The arithmetic behind coronary movement is computed in three phases: yaw, pitch, and depth. Yaw is computed first. Consider
                // the line from the user's heart to the user's hand; what is the angle between the "original" state of that line and the
                // "current" state? That angle is the yaw, and it is experienced identically by the "handle on a stick" and the "beam" moving
                // the object.
                var originalInteractionDir = (_originalInteractionPoint - _originalCorePoint).normalized;
                var originalInteractionDirXZ = new Vector3(originalInteractionDir.x, 0f, originalInteractionDir.z).normalized;
                var originalYaw = Mathf.Atan2(originalInteractionDirXZ.z, originalInteractionDirXZ.x);

                var currentDir = (currentInteractionPoint - currentCorePoint).normalized;
                var currentDirXZ = new Vector3(currentDir.x, 0f, currentDir.z).normalized;
                var currentYaw = Mathf.Atan2(currentDirXZ.z, currentDirXZ.x);

                // The rotation required to get from the original yaw to the current yaw.
                var currentYawRotation = Quaternion.AngleAxis(Mathf.Rad2Deg * (originalYaw - currentYaw), Vector3.up);

                var currentTargetDelta = currentYawRotation * originalTargetDelta;

                // Once yaw is computed, it can be used to compute pitch. This is necessary because the pitch in question isn't simply the
                // angle between heart-hand line and the XZ plane; calculating pitch that way would subject the moved object to undesired
                // "tilt" when the hand was raised or lowered off-axis. Instead, pitch must be calculated along the axis of the "beam," which
                // means the current beam's direction must be known, which requires knowing the yaw. Once that is known, the "original" and
                // "current" hand positions can be projected into the plane of the "beam" and pitch can be calculated.
                var originalTargetDir = originalTargetDelta.normalized;
                var originalRightDir = Vector3.Cross(originalTargetDir, Vector3.up);
                var originalPlane = new Plane(originalRightDir, 0f);
                var projectionToOriginalPlane = originalPlane.ClosestPointOnPlane(_originalInteractionPoint - _originalCorePoint);
                var originalPitch = Mathf.Acos(projectionToOriginalPlane.normalized.y);

                var currentRightDir = currentYawRotation * originalRightDir;
                var currentPlane = new Plane(currentRightDir, 0f);
                var projectionToCurrentPlane = currentPlane.ClosestPointOnPlane(currentInteractionPoint - currentCorePoint);
                var currentPitch = Mathf.Acos(projectionToCurrentPlane.normalized.y);

                // The rotation required to get from the original pitch to the current pitch.
                var currentPitchRotation = Quaternion.AngleAxis(Mathf.Rad2Deg * (originalPitch - currentPitch), currentRightDir);

                currentTargetDelta = currentPitchRotation * currentTargetDelta;

                // Finally, depth --- the degree to which the "current" "beam" should grow or shrink relative to its "original" length ---
                // can be calculated. Strictly speaking, pitch is not needed in order to calculate depth, but the planes use to compute
                // pitch are reused here. Depth, like pitch, is calculated along the "beam" axis, specifically as a ratio of the magnitudes
                // of the hand positions projected into the "beam" axis planes. Colloquially, depth is controlled by "how far away from the heart
                // the hand is along the beam axis." This value is then adjusted using the _depthRemappingFactor before being used to scale the
                // "beam."
                var originalInteractionDepth = Vector3.Magnitude(projectionToOriginalPlane);
                var currentInteractionDepth = Vector3.Magnitude(projectionToCurrentPlane);
                var depthRatio = currentInteractionDepth / originalInteractionDepth;
                currentTargetDelta *= Mathf.Pow(depthRatio, _depthRempapingFactor);

                _currentPose.position = currentCorePoint + currentTargetDelta;

                // Independently of position, set rotation one of two ways, depending on whether or not billboarding is enabled.
                if (_billboard)
                {
                    _currentPose.rotation =
                        Quaternion.LookRotation((currentCorePoint - _currentPose.position).normalized) *
                        Quaternion.AngleAxis(-_billboardPitch, Vector3.right) *
                        Quaternion.AngleAxis(180f, Vector3.up);
                }
                else
                {
                    _currentPose.rotation = getInteractionOrientation() * _inverseOriginalInteractionOrientation * _originalPose.rotation;
                }
            }
        }
    }
}
