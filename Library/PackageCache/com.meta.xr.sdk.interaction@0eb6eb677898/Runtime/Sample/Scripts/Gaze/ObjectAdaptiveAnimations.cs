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

using System.Reflection;
using UnityEngine;

namespace Oculus.Interaction.Gaze.Samples
{
    /// <summary>
    /// Adjusts <see cref="InteractableColorVisual"/> hover/select color transition timings at runtime,
    /// enabling slower transitions for gaze and snappier ones for direct interactions (poke/ray/grab).
    /// </summary>
    /// <remarks>
    /// Listens for hover events and adjusts ColorTime values based on whether a <see cref="GazeInteractor"/>
    /// is hovering. Unlike Unity's Animator, <see cref="InteractableColorVisual"/> allows runtime timing
    /// modifications. Timings persist through unhover to prevent visual artifacts.
    /// For Canvas UI, use <see cref="CanvasAdaptiveAnimations"/> instead.
    /// </remarks>
    [Experimental]
    public class ObjectAdaptiveAnimations : MonoBehaviour
    {
        [Header("Gaze Timings")]
        [SerializeField, Tooltip("Color transition time when gaze enters hover state.")]
        private float _gazeHoverTime = 0.3f;

        [SerializeField, Tooltip("Color transition time when gaze triggers selection.")]
        private float _gazeSelectTime = 0.25f;

        [SerializeField, Tooltip("Color transition time when gaze returns to normal state.")]
        private float _gazeNormalTime = 0.4f;

        [Header("Direct Timings")]
        [SerializeField, Tooltip("Color transition time when poke/ray/grab enters hover state.")]
        private float _directHoverTime = 0.1f;

        [SerializeField, Tooltip("Color transition time when poke/ray/grab triggers selection.")]
        private float _directSelectTime = 0.1f;

        [SerializeField, Tooltip("Color transition time when poke/ray/grab returns to normal state.")]
        private float _directNormalTime = 0.1f;

        [SerializeField, Interface(typeof(IInteractableView))]
        [Tooltip("The interactable to monitor for hover events.")]
        private UnityEngine.Object _interactable;
        private IInteractableView Interactable;

        [SerializeField]
        [Tooltip("The color visual whose timings will be adjusted.")]
        private InteractableColorVisual _colorVisual;

        private InteractableColorVisual.ColorState _hoverState;
        private InteractableColorVisual.ColorState _selectState;
        private InteractableColorVisual.ColorState _normalState;
        private bool _isGazeMode = false;
        private bool _started;

        protected virtual void Awake()
        {
            if (Interactable == null)
            {
                Interactable = _interactable as IInteractableView;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertField(Interactable, nameof(Interactable));
            this.AssertField(_colorVisual, nameof(_colorVisual));

            CacheColorStates();
            SetTimings(useGaze: false);

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                Interactable.WhenInteractorViewAdded += OnInteractorAdded;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                Interactable.WhenInteractorViewAdded -= OnInteractorAdded;
            }
        }

        private void CacheColorStates()
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;

            var hoverField = typeof(InteractableColorVisual).GetField("_hoverColorState", flags);
            var selectField = typeof(InteractableColorVisual).GetField("_selectColorState", flags);
            var normalField = typeof(InteractableColorVisual).GetField("_normalColorState", flags);

            _hoverState = hoverField?.GetValue(_colorVisual) as InteractableColorVisual.ColorState;
            _selectState = selectField?.GetValue(_colorVisual) as InteractableColorVisual.ColorState;
            _normalState = normalField?.GetValue(_colorVisual) as InteractableColorVisual.ColorState;
        }

        private void OnInteractorAdded(IInteractorView interactor)
        {
            if (interactor == null) return;

            bool isGazeInteractor = interactor is GazeInteractor;

            if (isGazeInteractor != _isGazeMode)
            {
                SetTimings(isGazeInteractor);
            }
        }

        private void SetTimings(bool useGaze)
        {
            _isGazeMode = useGaze;

            if (_hoverState != null)
            {
                _hoverState.ColorTime = useGaze ? _gazeHoverTime : _directHoverTime;
            }

            if (_selectState != null)
            {
                _selectState.ColorTime = useGaze ? _gazeSelectTime : _directSelectTime;
            }

            if (_normalState != null)
            {
                _normalState.ColorTime = useGaze ? _gazeNormalTime : _directNormalTime;
            }
        }

        #region Inject

        /// <summary>
        /// Sets all required dependencies for this component.
        /// </summary>
        public void InjectAllObjectAdaptiveAnimations(
            IInteractableView interactable,
            InteractableColorVisual colorVisual)
        {
            InjectInteractable(interactable);
            InjectColorVisual(colorVisual);
        }

        /// <summary>
        /// Sets the interactable to monitor for hover events.
        /// </summary>
        public void InjectInteractable(IInteractableView interactable)
        {
            _interactable = interactable as UnityEngine.Object;
            Interactable = interactable;
        }

        /// <summary>
        /// Sets the color visual whose timings will be adjusted.
        /// </summary>
        public void InjectColorVisual(InteractableColorVisual colorVisual)
        {
            _colorVisual = colorVisual;
        }

        #endregion
    }
}
