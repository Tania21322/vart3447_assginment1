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
using UnityEngine.Events;

namespace Oculus.Interaction.Locomotion
{
    /// <summary>
    /// Converts <see cref="LocomotionEvent"/> into UnityEvents for different
    /// locomotion types (translation, rotation, invalid target), allowing
    /// audio and visual feedback to be wired up in the Inspector.
    /// </summary>
    public class LocomotionHandlerUnityEventWrapper : MonoBehaviour, ILocomotionEventHandler
    {
        /// <summary>
        /// Optional context used to resolve locomotion actions from <see cref="LocomotionActionsBroadcaster"/>.
        /// </summary>
        [Tooltip("Optional context used to resolve locomotion actions from LocomotionActionsBroadcaster.")]
        [SerializeField, Optional]
        private Context _context = null;

        /// <summary>
        /// Raised when an absolute translation locomotion event is received.
        /// </summary>
        [Tooltip("Raised when an absolute translation locomotion event is received.")]
        [SerializeField]
        private UnityEvent _whenAbsoluteTranslation;

        /// <summary>
        /// Raised when a relative translation locomotion event is received.
        /// </summary>
        [Tooltip("Raised when a relative translation locomotion event is received.")]
        [SerializeField]
        private UnityEvent _whenRelativeTranslation;

        /// <summary>
        /// Raised when a locomotion event targets an invalid location.
        /// </summary>
        [Tooltip("Raised when a locomotion event targets an invalid location.")]
        [SerializeField]
        private UnityEvent _whenInvalidTarget;

        /// <summary>
        /// Raised when an absolute rotation locomotion event is received.
        /// </summary>
        [Tooltip("Raised when an absolute rotation locomotion event is received.")]
        [SerializeField]
        private UnityEvent _whenAbsoluteRotation;

        /// <summary>
        /// Raised when a relative rotation to the left is received.
        /// </summary>
        [Tooltip("Raised when a relative rotation to the left is received.")]
        [SerializeField]
        private UnityEvent _whenRelativeRotationLeft;

        /// <summary>
        /// Raised when a relative rotation to the right is received.
        /// </summary>
        [Tooltip("Raised when a relative rotation to the right is received.")]
        [SerializeField]
        private UnityEvent _whenRelativeRotationRight;

        /// <summary>
        /// Unity event invoked when the incoming <see cref="LocomotionEvent"/> has a
        /// <see cref="LocomotionEvent.TranslationType.Absolute"/> or
        /// <see cref="LocomotionEvent.TranslationType.AbsoluteEyeLevel"/> translation.
        /// </summary>
        public UnityEvent WhenAbsoluteTranslation => _whenAbsoluteTranslation;

        /// <summary>
        /// Unity event invoked when the incoming <see cref="LocomotionEvent"/> has a
        /// <see cref="LocomotionEvent.TranslationType.Relative"/> translation.
        /// </summary>
        public UnityEvent WhenRelativeTranslation => _whenRelativeTranslation;

        /// <summary>
        /// Unity event invoked when the incoming <see cref="LocomotionEvent"/> has no translation
        /// or rotation and the resolved <see cref="LocomotionActionsBroadcaster.LocomotionAction"/>
        /// is <see cref="LocomotionActionsBroadcaster.LocomotionAction.InvalidTarget"/>.
        /// </summary>
        public UnityEvent WhenInvalidTarget => _whenInvalidTarget;

        /// <summary>
        /// Unity event invoked when the incoming <see cref="LocomotionEvent"/> has a
        /// <see cref="LocomotionEvent.RotationType.Absolute"/> rotation.
        /// </summary>
        public UnityEvent WhenAbsoluteRotation => _whenAbsoluteRotation;

        /// <summary>
        /// Unity event invoked when the incoming <see cref="LocomotionEvent"/> has a
        /// <see cref="LocomotionEvent.RotationType.Relative"/> rotation turning to the left.
        /// </summary>
        public UnityEvent WhenRelativeRotationLeft => _whenRelativeRotationLeft;

        /// <summary>
        /// Unity event invoked when the incoming <see cref="LocomotionEvent"/> has a
        /// <see cref="LocomotionEvent.RotationType.Relative"/> rotation turning to the right.
        /// </summary>
        public UnityEvent WhenRelativeRotationRight => _whenRelativeRotationRight;

        /// <summary>
        /// Raised after a <see cref="LocomotionEvent"/> has been successfully handled,
        /// providing the original event and the resulting <see cref="Pose"/>.
        /// </summary>
        public event Action<LocomotionEvent, Pose> WhenLocomotionEventHandled = delegate { };

        /// <summary>
        /// Processes an incoming <see cref="LocomotionEvent"/> and invokes the corresponding
        /// UnityEvents based on the event's translation and rotation types.
        /// </summary>
        /// <param name="locomotionEvent">The locomotion event to handle.</param>
        public void HandleLocomotionEvent(LocomotionEvent locomotionEvent)
        {
            bool handled = false;
            if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Absolute
                || locomotionEvent.Translation == LocomotionEvent.TranslationType.AbsoluteEyeLevel)
            {
                _whenAbsoluteTranslation.Invoke();
                handled = true;
            }
            else if (locomotionEvent.Translation == LocomotionEvent.TranslationType.Relative)
            {
                _whenRelativeTranslation.Invoke();
                handled = true;
            }
            else if (locomotionEvent.Translation == LocomotionEvent.TranslationType.None
                && locomotionEvent.Rotation == LocomotionEvent.RotationType.None
                && LocomotionActionsBroadcaster.TryGetLocomotionActions(locomotionEvent, out var action, _context)
                && action == LocomotionActionsBroadcaster.LocomotionAction.InvalidTarget)
            {
                _whenInvalidTarget.Invoke();
                handled = true;
            }

            if (locomotionEvent.Rotation == LocomotionEvent.RotationType.Absolute)
            {
                _whenAbsoluteRotation.Invoke();
                handled = true;
            }
            else if (locomotionEvent.Rotation == LocomotionEvent.RotationType.Relative)
            {
                float angle = locomotionEvent.Pose.rotation.y * locomotionEvent.Pose.rotation.w;
                if (angle < 0f)
                {
                    _whenRelativeRotationLeft.Invoke();
                }
                else if (angle > 0f)
                {
                    _whenRelativeRotationRight.Invoke();
                }
                handled = true;
            }

            if (handled)
            {
                WhenLocomotionEventHandled(locomotionEvent, Pose.identity);
            }
        }

        #region Inject

        /// <summary>
        /// Sets the optional <see cref="Context"/> for a dynamically instantiated
        /// <see cref="LocomotionHandlerUnityEventWrapper"/>. This method exists to support
        /// Interaction SDK's dependency injection pattern and is not needed for typical
        /// Unity Editor-based usage.
        /// </summary>
        /// <param name="context">The <see cref="Context"/> used to resolve locomotion actions.</param>
        public void InjectOptionalContext(Context context)
        {
            _context = context;
        }

        #endregion
    }
}
