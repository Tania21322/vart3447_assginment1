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
    /// Enables scrolling of UI <see cref="ScrollRect"/>s in response to discrete directional
    /// scroll commands from any source, coupled to a single interactor via
    /// <see cref="IInteractorView"/>. Callers drive scrolling by invoking the public
    /// <see cref="Scroll(Vector2)"/> method (or the <see cref="ScrollUp"/>/<see cref="ScrollDown"/>/
    /// <see cref="ScrollLeft"/>/<see cref="ScrollRight"/> convenience wrappers) - for example from a
    /// microgesture event wrapper, keyboard input, or on-screen buttons. While the interactor is
    /// hovering it tracks the hovered UI selectable via
    /// <see cref="PointableCanvasModule.WhenObjectHovered"/> /
    /// <see cref="PointableCanvasModule.WhenObjectUnhovered"/> (matched by
    /// <see cref="IInteractorView.Identifier"/>), resolves the hovered <see cref="ScrollRect"/>
    /// immediately, and applies scroll impulses to it. Momentum/velocity physics is delegated to a
    /// reusable <see cref="ScrollRectVelocityDriver"/>.
    /// </summary>
    [Experimental]
    public class DiscreteScrollInputProvider : MonoBehaviour
    {
        #region Serialized Fields

        [Header("Dependencies")]
        [SerializeField, Interface(typeof(IInteractorView))]
        [Tooltip("The interactor whose current interactable determines the scroll target.")]
        private UnityEngine.Object _interactor;

        [Header("Scroll Settings")]
        [SerializeField]
        [Tooltip("The speed at which scrolling occurs. Multiplies viewport size to determine velocity.")]
        private float _scrollSpeed = 2f;

        [Header("Behavior")]
        [SerializeField]
        [Tooltip("If true, scrolling stops immediately when the interactor's target changes.")]
        private bool _stopMomentumOnTargetChange = false;

        #endregion

        #region Properties

        /// <summary>
        /// The speed at which scrolling occurs. Multiplies viewport size to determine velocity.
        /// </summary>
        public float ScrollSpeed
        {
            get => _scrollSpeed;
            set => _scrollSpeed = value;
        }

        /// <summary>
        /// Whether scrolling stops immediately when the interactor's target changes.
        /// </summary>
        public bool StopMomentumOnTargetChange
        {
            get => _stopMomentumOnTargetChange;
            set
            {
                _stopMomentumOnTargetChange = value;
                if (_velocityDriver != null)
                {
                    _velocityDriver.StopMomentumOnTargetChange = value;
                }
            }
        }

        /// <summary>
        /// The <see cref="ScrollRect"/> currently tracked from the interactor's hovered target,
        /// or <c>null</c> when there is no valid target.
        /// </summary>
        public ScrollRect CurrentScrollRect => _currentScrollRect;

        #endregion

        private IInteractorView InteractorView { get; set; }
        private ScrollRectVelocityDriver _velocityDriver;
        private ScrollRect _currentScrollRect;
        protected bool _started = false;

        private GameObject _lastHoveredGO;
        private ScrollRect _dirCacheInnermost;
        private ScrollRect _dirCacheHorizontal;
        private ScrollRect _dirCacheVertical;
        private bool _dirCacheHorizontalComputed;
        private bool _dirCacheVerticalComputed;

        #region Unity Lifecycle

        protected virtual void Awake()
        {
            if (InteractorView == null)
            {
                InteractorView = _interactor as IInteractorView;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.AssertField(InteractorView, nameof(_interactor));
            _velocityDriver = new ScrollRectVelocityDriver
            {
                StopMomentumOnTargetChange = _stopMomentumOnTargetChange
            };
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                InteractorView.WhenStateChanged += HandleInteractorStateChanged;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                InteractorView.WhenStateChanged -= HandleInteractorStateChanged;
                UnsubscribeHoverTracking();
                ClearTarget();
                _velocityDriver?.CancelMomentum();
            }
        }

        protected virtual void Update()
        {
            if (InteractorView.State != InteractorState.Disabled)
            {
                _velocityDriver?.Tick(_currentScrollRect);
            }
        }

        #endregion

        #region Target Resolution (Interactor-Coupled)

        private void HandleInteractorStateChanged(InteractorStateChangeArgs args)
        {
            if (args.PreviousState == InteractorState.Normal &&
                args.NewState == InteractorState.Hover)
            {
                SubscribeHoverTracking();
            }
            else if (args.PreviousState == InteractorState.Hover &&
                     args.NewState == InteractorState.Normal)
            {
                UnsubscribeHoverTracking();
                ClearTarget();
            }
        }

        private void SubscribeHoverTracking()
        {
            PointableCanvasModule.WhenObjectHovered += HandleObjectHovered;
            PointableCanvasModule.WhenObjectUnhovered += HandleObjectUnhovered;
        }

        private void UnsubscribeHoverTracking()
        {
            PointableCanvasModule.WhenObjectHovered -= HandleObjectHovered;
            PointableCanvasModule.WhenObjectUnhovered -= HandleObjectUnhovered;
        }

        private void HandleObjectHovered(PointableCanvasEventArgs args)
        {
            if (!args.PointerId.HasValue || args.PointerId.Value != InteractorView.Identifier)
            {
                return;
            }
            ResolveScrollRect(args.Hovered);
        }

        private void HandleObjectUnhovered(PointableCanvasEventArgs args)
        {
            if (!args.PointerId.HasValue || args.PointerId.Value != InteractorView.Identifier)
            {
                return;
            }
            ClearTarget();
        }

        private void ClearTarget()
        {
            _currentScrollRect = null;
            _lastHoveredGO = null;
        }

        /// <summary>Resolves the target <see cref="ScrollRect"/> from the hovered GameObject.</summary>
        private void ResolveScrollRect(GameObject hoveredGO)
        {
            if (hoveredGO == _lastHoveredGO)
            {
                return;
            }
            _lastHoveredGO = hoveredGO;

            if (hoveredGO == null)
            {
                _currentScrollRect = null;
                return;
            }

            _currentScrollRect = hoveredGO.GetComponentInParent<ScrollRect>();
        }

        #endregion

        #region Scroll Input

        /// <summary>Scrolls the tracked <see cref="ScrollRect"/> content upward.</summary>
        public void ScrollUp() => Scroll(Vector2.up);

        /// <summary>Scrolls the tracked <see cref="ScrollRect"/> content downward.</summary>
        public void ScrollDown() => Scroll(Vector2.down);

        /// <summary>Scrolls the tracked <see cref="ScrollRect"/> content left.</summary>
        public void ScrollLeft() => Scroll(Vector2.left);

        /// <summary>Scrolls the tracked <see cref="ScrollRect"/> content right.</summary>
        public void ScrollRight() => Scroll(Vector2.right);

        /// <summary>
        /// Applies a discrete scroll impulse to the currently tracked <see cref="ScrollRect"/> in
        /// the given direction. <see cref="Vector2.up"/> produces positive <c>velocity.y</c> (content
        /// scrolls up); the other cardinal directions follow the same convention. Non-cardinal
        /// directions are resolved by their dominant axis. A zero direction is treated as a stop
        /// signal, immediately cancelling any in-flight momentum. No-ops when there is no valid
        /// tracked target.
        /// </summary>
        public void Scroll(Vector2 direction)
        {
            if (direction == Vector2.zero)
            {
                _velocityDriver?.CancelMomentum();
                return;
            }

            if (_currentScrollRect == null ||
                !_currentScrollRect.enabled ||
                !_currentScrollRect.gameObject.activeInHierarchy)
            {
                return;
            }

            // Resolve the ScrollRect for this swipe axis, bubbling up to a parent if needed.
            ScrollRect targetRect = ResolveDirectionTarget(direction);
            if (targetRect == null)
            {
                return;
            }

            float viewportDim = GetViewportDimension(targetRect, direction);
            Vector2 velocityDelta = direction * (viewportDim * _scrollSpeed);

            _velocityDriver.ApplyImpulse(targetRect, velocityDelta, direction);
        }

        #endregion

        #region Static Helpers

        private static bool IsHorizontal(Vector2 direction)
        {
            return Mathf.Abs(direction.x) > Mathf.Abs(direction.y);
        }

        private static float GetViewportDimension(ScrollRect scrollRect, Vector2 direction)
        {
            if (scrollRect.viewport)
            {
                return IsHorizontal(direction)
                    ? scrollRect.viewport.rect.width
                    : scrollRect.viewport.rect.height;
            }
            if (scrollRect.transform is RectTransform rt)
            {
                return IsHorizontal(direction)
                    ? rt.rect.width
                    : rt.rect.height;
            }
            return 500f;
        }

        /// <summary>Walks up from the innermost hovered <see cref="ScrollRect"/> to the first that supports the swipe axis.</summary>
        private static ScrollRect FindScrollRectForDirection(ScrollRect innermost, Vector2 direction)
        {
            ScrollRect current = innermost;
            bool horizontal = IsHorizontal(direction);
            while (current)
            {
                if ((horizontal && current.horizontal) ||
                    (!horizontal && current.vertical))
                {
                    return current;
                }

                Transform parent = current.transform.parent;
                current = parent ? parent.GetComponentInParent<ScrollRect>() : null;
            }
            return null;
        }

        /// <summary>Per-axis cached <see cref="FindScrollRectForDirection"/>, keyed on the current <see cref="ScrollRect"/>.</summary>
        private ScrollRect ResolveDirectionTarget(Vector2 direction)
        {
            if (_currentScrollRect != _dirCacheInnermost)
            {
                _dirCacheInnermost = _currentScrollRect;
                _dirCacheHorizontalComputed = false;
                _dirCacheVerticalComputed = false;
            }

            if (IsHorizontal(direction))
            {
                if (!_dirCacheHorizontalComputed)
                {
                    _dirCacheHorizontal = FindScrollRectForDirection(_currentScrollRect, direction);
                    _dirCacheHorizontalComputed = true;
                }
                return _dirCacheHorizontal;
            }

            if (!_dirCacheVerticalComputed)
            {
                _dirCacheVertical = FindScrollRectForDirection(_currentScrollRect, direction);
                _dirCacheVerticalComputed = true;
            }
            return _dirCacheVertical;
        }

        #endregion

        #region Inject

        public void InjectAllDiscreteScrollInputProvider(IInteractorView interactorView)
        {
            InjectInteractorView(interactorView);
        }

        public void InjectInteractorView(IInteractorView interactorView)
        {
            _interactor = interactorView as UnityEngine.Object;
            InteractorView = interactorView;
        }

        #endregion
    }
}
