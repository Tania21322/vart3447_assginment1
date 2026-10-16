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

namespace Oculus.Interaction
{
    [Serializable]
    public struct HoverSpotlightGradient
    {
        [Tooltip("Distance, in world space, where the intensity of the spotlight starts falling off")]
        /// <summary>Distance, in world space coords, where the intensity of the spotlight starts falling off.</summary>
        public float Radius;
        [Tooltip("Distance, in world space, where the intensity of the spotlight goes from max intensity to zero")]
        /// <summary>Distance, in world space, where the intensity of the spotlight goes from max intensity to zero.</summary>
        public float Falloff;
        [Tooltip("Power of the gradient falloff")]
        /// <summary>Power of the gradient falloff.</summary>
        public float Power;
        [Tooltip("Color of the gradient. The alpha component is used to dim the intensity")]
        /// <summary>Color of the gradient. The alpha component is used to dim the intensity.</summary>
        public Color Color;
        [Tooltip("Gradient dither intensity.")]
        /// <summary>Gradient dither intensity.</summary>
        public float Dither;

        private static readonly int s_radiusID = Shader.PropertyToID("_Radius");
        private static readonly int s_falloffID = Shader.PropertyToID("_Falloff");
        private static readonly int s_powerID = Shader.PropertyToID("_Power");
        private static readonly int s_colorID = Shader.PropertyToID("_Color");
        private static readonly int s_ditherID = Shader.PropertyToID("_DitheringAmount");

        public void ApplyToPropertyBlock(MaterialPropertyBlock block)
        {
            if (block == null) return;
            block.SetFloat(s_radiusID, Radius);
            block.SetFloat(s_falloffID, Falloff);
            block.SetFloat(s_powerID, Power);
            block.SetColor(s_colorID, Color);
            block.SetFloat(s_ditherID, Dither);
        }
    }
    [Serializable]
    public struct HoverSpotlightPointer
    {
        [Tooltip("Length, in seconds, over which the spotlight transitions from one state to the next")]
        [Range(0.0f, 3.0f)]
        /// <summary>Length, in seconds, over which the spotlight transitions from one state to the next.</summary>
        public float StateTransitionDuration;
        [Tooltip("Length, in seconds, over which the spotlight intensity dims down once the user has been hovering for a while. 0 disables the dwell fade.")]
        [Range(0.0f, 5.0f)]
        /// <summary>Length, in seconds, over which the spotlight intensity dims down once the user has been hovering for a while. 0 disables the dwell fade.</summary>
        public float DwellFadeDuration;
        [Tooltip("Final intensity multiplier reached after the dwell fade is complete (1 = intensity of 1.0, 0.5 = half intensity at the end).")]
        [Range(0.0f, 1.0f)]
        /// <summary>Final intensity multiplier reached after the dwell fade is complete (1 = intensity of 1.0, 0.5 = half intensity at the end).</summary>
        public float DwellFadeValue;
    }

    /// <summary>
    /// Per-pointer runtime state for one spotlight in the
    /// <see cref="InteractableGazeHover3DVisual"/> effect.
    ///
    /// Encapsulates the state machine driven by <see cref="PointerEvent"/>s,
    /// the dwell dimmer that softens the spotlight after the user has been hovering for a while,
    /// the per-state intensity tween, and the exponentially-smoothed gradient center.
    /// </summary>
    internal class GazeHover3DPointerState
    {
        static float EaseInOutSine(float param) => -(Mathf.Cos(Mathf.PI * param) - 1.0f) / 2.0f;

        /// <summary>High-level state of a single spotlight.</summary>
        public enum State
        {
            None = 0,
            Hover = 1,
            Press = 2,
            FadingOut = 3,
        }

        /// <summary>Identifier of the pointer represented by this hover pointer.</summary>
        public int? PointerId;

        /// <summary>The current high-level state of the spotlight.</summary>
        public State CurrentState;

        /// <summary>
        /// Smoothed world-space gradient center that the layer shaders sample. Lags behind
        /// <see cref="TargetPosition"/> by a few frames to hide the discrete steps inherent to
        /// raycast hit points.
        /// </summary>
        public Vector3 SmoothedPosition;

        /// <summary>The latest raw world-space hit point received from the pointer.</summary>
        public Vector3 TargetPosition;

        /// <summary>Last computed intensity value.</summary>
        public float Intensity;

        /// <summary>
        /// Time, in seconds, since the last state transition. Used to drive the per-state transitions.
        /// </summary>
        public float StateTimer;

        /// <summary>
        /// Intensity at the start of the current state transition. Captured so that the tween
        /// can interpolate smoothly out of whatever value the previous state had reached.
        /// </summary>
        public float PrevStateIntensity;

        /// <summary>
        /// True if this slot still needs per-frame work meaning it is not in
        /// <see cref="State.None"/> (so the spotlight is visibly on)
        /// </summary>
        public bool IsActive() => CurrentState != State.None;

        public void HandlePointerEvent(PointerEvent evt)
        {
            var lastState = CurrentState;
            TargetPosition = evt.Pose.position;
            switch (evt.Type)
            {
                case PointerEventType.Select:
                    CurrentState = State.Press;
                    break;
                case PointerEventType.Hover:
                case PointerEventType.Unselect:
                    CurrentState = State.Hover;
                    if (evt.Type == PointerEventType.Hover && PointerId == null)
                    {
                        SmoothedPosition = TargetPosition;
                    }
                    PointerId = evt.Identifier;
                    break;
                case PointerEventType.Cancel:
                case PointerEventType.Unhover:
                    CurrentState = CurrentState == State.None ? State.None : State.FadingOut;
                    break;
                default:
                    break;
            }
            if (lastState != CurrentState)
            {
                PrevStateIntensity = Intensity;
                StateTimer = 0f;
            }
        }

        /// <summary>Update the intensity multiplier for this pointer, given the glow properties.</summary>
        public void UpdateIntensity(HoverSpotlightPointer props, float deltaTime)
        {
            StateTimer += deltaTime;
            // Stop the timer from going into infinity and ensures the following interpolations are stable
            var maxTargetDuration = Mathf.Max(props.DwellFadeDuration, props.StateTransitionDuration);
            StateTimer = Mathf.Clamp(StateTimer, 0.0f, maxTargetDuration);

            var targetIntensity = CurrentState switch
            {
                State.Hover or State.Press => 1.0f,
                _ => 0.0f
            };

            // Tween from the value we had at the moment of the last transition to the new target.
            float intensity = targetIntensity;
            if (props.StateTransitionDuration > 0.0f)
            {
                float normalized = Mathf.Clamp01(StateTimer / props.StateTransitionDuration);
                float curveValue = EaseInOutSine(normalized);
                intensity = Mathf.LerpUnclamped(PrevStateIntensity, targetIntensity, curveValue);
            }

            if (CurrentState == State.FadingOut && StateTimer >= props.StateTransitionDuration)
            {
                Reset();
                return;
            }

            // Apply the dwell fade only while we're in the Hover state. Press holds at full
            // intensity; Default has nothing to dim.
            if (CurrentState == State.Hover && props.DwellFadeDuration > 0f)
            {
                float dwellNormalized = Mathf.Clamp01(StateTimer / props.DwellFadeDuration);
                float dwellMultiplier = Mathf.Lerp(1f, props.DwellFadeValue, dwellNormalized);
                intensity *= dwellMultiplier;
            }

            Intensity = intensity;
        }

        /// <summary>Updates the internal state of the pointer based on the provided properties.</summary>
        public void Tick(HoverSpotlightPointer props, float positionSmoothingFactor, float deltaTime)
        {
            // Nothing to do when the pointer is not in use
            if (CurrentState == State.None) return;
            UpdatePosition(positionSmoothingFactor, deltaTime);
            UpdateIntensity(props, deltaTime);
        }

        /// <summary>Smoothly moves <see cref="SmoothedPosition"/> toward <see cref="TargetPosition"/>.</summary>
        /// <param name="smoothingFactor">Higher values = snappier follow. 0 = no smoothing.</param>
        public void UpdatePosition(float smoothingFactor, float deltaTime)
        {
            // Don't move the pointer while fading out
            if (CurrentState == State.FadingOut) return;

            // The spotlight should stay attached to the pointer when pressed
            if (smoothingFactor <= 0f || CurrentState == State.Press)
            {
                SmoothedPosition = TargetPosition;
                return;
            }

            // Exponential smoothing: position += (target - position) * (1 - exp(-k*dt))
            // This is frame-rate-independent and matches the prototype's Vector3SmoothingFilter.
            float t = Mathf.Exp(-smoothingFactor * deltaTime);
            SmoothedPosition = Vector3.LerpUnclamped(TargetPosition, SmoothedPosition, t);
        }

        /// <summary>Resets the state to a clean, inactive baseline.</summary>
        public void Reset()
        {
            PointerId = null;
            CurrentState = State.None;
            SmoothedPosition = Vector3.zero;
            TargetPosition = Vector3.zero;
            Intensity = 0.0f;
            StateTimer = 0.0f;
            PrevStateIntensity = 0f;
        }
    }
}
