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
using UnityEngine.UI;

namespace Oculus.Interaction
{
    /// <summary>
    /// Reusable impulse-based <see cref="ScrollRect"/> velocity driver (e.g. for microgestures or
    /// thumbstick flicks). Same-direction impulses accumulate; an opposite impulse stops, clamps to
    /// bounds, then applies the new velocity; momentum is optionally cancelled when the owner's
    /// target changes away from the active <see cref="ScrollRect"/>. A plain C# class so it can be
    /// composed and unit tested in isolation; the owner calls <see cref="Tick"/> from its Update loop.
    /// </summary>
    internal class ScrollRectVelocityDriver
    {
        private ScrollRect _activeScrollRect;
        private Vector2 _lastDirection = Vector2.zero;

        /// <summary>If true, <see cref="Tick"/> zeroes momentum when the target leaves the active <see cref="ScrollRect"/> (or a nested child). Can be changed at runtime.</summary>
        public bool StopMomentumOnTargetChange { get; set; } = false;

        /// <summary>The <see cref="ScrollRect"/> most recently impulsed and possibly still carrying momentum; null when idle.</summary>
        public ScrollRect ActiveScrollRect => _activeScrollRect;

        /// <summary>Direction of the most recently applied impulse, used for reversal detection.</summary>
        public Vector2 LastDirection => _lastDirection;

        /// <summary>Applies a velocity impulse: accumulates on the same direction, stops + clamps then applies on reversal.</summary>
        public void ApplyImpulse(ScrollRect scrollRect, Vector2 velocityDelta, Vector2 direction)
        {
            if (scrollRect == null)
            {
                return;
            }

            bool isDirectionChange = _lastDirection != Vector2.zero
                && Vector2.Dot(_lastDirection, direction) < 0f;

            if (isDirectionChange)
            {
                scrollRect.StopMovement();
                ClampContentToBounds(scrollRect);
                scrollRect.velocity = velocityDelta;
            }
            else
            {
                scrollRect.velocity += velocityDelta;
            }

            _lastDirection = direction;
            _activeScrollRect = scrollRect;
        }

        /// <summary>Called each owner Update; kills momentum if the current target has changed away from the active <see cref="ScrollRect"/>.</summary>
        public void Tick(ScrollRect currentTargetScrollRect)
        {
            if (!StopMomentumOnTargetChange || _activeScrollRect == null)
            {
                return;
            }

            if (_activeScrollRect.velocity.sqrMagnitude > 1f)
            {
                if (!IsStillTargeting(currentTargetScrollRect))
                {
                    _activeScrollRect.velocity = Vector2.zero;
                    Reset();
                }
            }
            else
            {
                _activeScrollRect = null;
            }
        }

        /// <summary>Immediately cancels momentum on the active <see cref="ScrollRect"/> and resets state.</summary>
        public void CancelMomentum()
        {
            if (_activeScrollRect != null)
            {
                _activeScrollRect.velocity = Vector2.zero;
            }
            Reset();
        }

        /// <summary>Resets tracking state without modifying any <see cref="ScrollRect"/> velocity.</summary>
        public void Reset()
        {
            _activeScrollRect = null;
            _lastDirection = Vector2.zero;
        }

        /// <summary>Whether the target is still the active <see cref="ScrollRect"/> or a nested child of it.</summary>
        private bool IsStillTargeting(ScrollRect currentTarget)
        {
            if (currentTarget == null)
            {
                return false;
            }
            if (currentTarget == _activeScrollRect)
            {
                return true;
            }
            return _activeScrollRect != null &&
                   currentTarget.transform.IsChildOf(_activeScrollRect.transform);
        }

        private static void ClampContentToBounds(ScrollRect scrollRect)
        {
            if (scrollRect.content == null)
            {
                return;
            }

            Vector2 norm = scrollRect.normalizedPosition;
            Vector2 clamped = new Vector2(
                Mathf.Clamp01(norm.x),
                Mathf.Clamp01(norm.y)
            );

            if (norm != clamped)
            {
                scrollRect.normalizedPosition = clamped;
            }
        }
    }
}
