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
using System.Collections.Generic;
using UnityEngine;

namespace Oculus.Interaction
{
    /// <summary>
    /// 3D hover affordance for ISDK Interactables. Paints a soft, world-space radial spotlight
    /// onto the surface of the source mesh at the pointer hit location. Reacts to any
    /// <see cref="IPointable"/> source — gaze, ray, hand-grab, etc. — and supports two
    /// simultaneous spotlights so left/right interactors can both light the object at once.
    /// </summary>
    [Experimental]
    public class InteractableGazeHover3DVisual : MonoBehaviour
    {
        [SerializeField, Interface(typeof(IPointable))]
        [Tooltip("IPointable sources whose pointer events will drive this affordance. Add every interactable on the object (RayInteractable, GazeInteractable, etc.) — events are demuxed by pointer identifier so multiple interactables on the same object work as expected.")]
        private List<UnityEngine.Object> _pointables;
        private readonly List<IPointable> _resolvedPointables = new List<IPointable>();

        [SerializeField]
        [Tooltip("Renderer that draws the hover effect. Toggled on/off as the affordance becomes active/inactive.")]
        private Renderer _renderer;

        [SerializeField]
        [Tooltip("MaterialPropertyBlockEditor that owns the MaterialPropertyBlock written to by this visual. Must reference the same renderer.")]
        private MaterialPropertyBlockEditor _editor;

        [Tooltip("The spotlight gradient properties.")]
        [SerializeField]
        private HoverSpotlightGradient _gradientProperties = new HoverSpotlightGradient()
        {
            Radius = 0.0f,
            Falloff = 0.5f,
            Power = 3.0f,
            Color = new Color(1.0f, 1.0f, 1.0f, 0.15f),
            Dither = 4.0f
        };
        public HoverSpotlightGradient GradientProperties
        {
            get => _gradientProperties;
            set => _gradientProperties = value;
        }

        [Tooltip("The timing and intensity properties of the spotlight.")]
        [SerializeField]
        private HoverSpotlightPointer _pointerProperties = new HoverSpotlightPointer()
        {
            StateTransitionDuration = 0.15f,
            DwellFadeDuration = 1.15f,
            DwellFadeValue = 0.8f,
        };

        public HoverSpotlightPointer PointerProperties
        {
            get => _pointerProperties;
            set => _pointerProperties = value;
        }

        [Tooltip("Exponential smoothing factor applied to the spotlight follow position. Higher = snappier follow, lower = more lag. 0 disables smoothing entirely.")]
        [SerializeField]
        private float _positionSmoothingFactor = 5f;
        public float PositionSmoothingFactor
        {
            get => _positionSmoothingFactor;
            set => _positionSmoothingFactor = value;
        }

        // Time-provider plumbing for deterministic stepping in tests. Returns the delta to
        // apply to the next Drive() call. Defaults to Time.deltaTime so the standard
        // LateUpdate path is unchanged at runtime.
        private static readonly Func<float> s_defaultDeltaProvider = () => Time.deltaTime;
        private Func<float> _deltaTimeProvider = s_defaultDeltaProvider;

        // Cached shader property IDs.
        private static readonly int s_centerID = Shader.PropertyToID("_Center");
        private static readonly int s_center2ID = Shader.PropertyToID("_Center2");
        private static readonly int s_intensityID = Shader.PropertyToID("_Intensity");
        private static readonly int s_intensity2ID = Shader.PropertyToID("_Intensity2");

        // We support up to two simultaneous pointer identifiers (e.g. left + right hand, or eye gaze + controller ray)
        private List<GazeHover3DPointerState> _slots;

        // Cached delegate to keep IPointable.WhenPointerEventRaised subscribe/unsubscribe
        // allocation-free (one delegate instance, reused for every pointable).
        private Action<PointerEvent> _pointerEventHandler;

        private bool _rendererEnabled = true;

        private bool _subscribed;

        protected bool _started = false;

        protected virtual void Awake()
        {
            _pointerEventHandler = HandlePointerEventRaised;
            ResolvePointables();
            _slots = new List<GazeHover3DPointerState>()
            {
                new GazeHover3DPointerState(),
                new GazeHover3DPointerState()
            };
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);

            this.AssertCollectionField(_resolvedPointables, nameof(_pointables));
            this.AssertField(_renderer, nameof(_renderer));
            this.AssertField(_editor, nameof(_editor));

            SetRendererEnabled(false);

            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            SubscribeToPointables();
        }

        protected virtual void OnDisable()
        {
            UnsubscribeFromPointables();

            // Reset state so we don't resume mid-tween if disabled while hovering.
            foreach (var slot in _slots) slot.Reset();
            SetRendererEnabled(false);
        }

        private void SubscribeToPointables()
        {
            if (_subscribed || _pointerEventHandler == null)
            {
                return;
            }
            for (int i = 0; i < _resolvedPointables.Count; i++)
            {
                _resolvedPointables[i].WhenPointerEventRaised += _pointerEventHandler;
            }
            _subscribed = true;
        }

        private void UnsubscribeFromPointables()
        {
            if (!_subscribed)
            {
                return;
            }
            for (int i = 0; i < _resolvedPointables.Count; i++)
            {
                _resolvedPointables[i].WhenPointerEventRaised -= _pointerEventHandler;
            }
            _subscribed = false;
        }

        /// <summary>
        /// Rebuilds <see cref="_resolvedPointables"/> from the serialized
        /// <see cref="_pointables"/> list. Filters out nulls and entries that don't
        /// actually implement <see cref="IPointable"/>.
        /// </summary>
        private void ResolvePointables()
        {
            _resolvedPointables.Clear();
            if (_pointables == null)
            {
                return;
            }
            for (int i = 0; i < _pointables.Count; i++)
            {
                if (_pointables[i] is IPointable pointable)
                {
                    _resolvedPointables.Add(pointable);
                }
            }
        }

        private void HandlePointerEventRaised(PointerEvent evt)
        {
            // Look for the slot the event points to
            foreach (var slot in _slots)
            {
                if (slot.PointerId == null) continue;
                if (slot.PointerId.Value != evt.Identifier) continue;
                slot.HandlePointerEvent(evt);
                return;
            }
            // Look for a free slot to add this new event
            if (evt.Type != PointerEventType.Hover) return;
            foreach (var slot in _slots)
            {
                if (slot.PointerId != null) continue;
                slot.HandlePointerEvent(evt);
                return;
            }
            // Both slots are in use, so nothing happens
        }

        protected virtual void LateUpdate()
        {
            Drive();
        }

        /// <summary>
        /// Advances the visual one frame. Called from <see cref="LateUpdate"/> at runtime.
        /// </summary>
        /// <remarks>
        /// At runtime, do not call manually — <see cref="LateUpdate"/> already drives this
        /// once per frame. Calling externally would double-step the simulation.
        /// </remarks>
        internal void Drive()
        {
            bool anyActive = false;
            float dt = _deltaTimeProvider();
            foreach (var slot in _slots)
            {
                slot.Tick(_pointerProperties, _positionSmoothingFactor, dt);
                anyActive |= slot.IsActive();
            }

            if (anyActive != _rendererEnabled) SetRendererEnabled(anyActive);
            if (anyActive) ApplyGradientToBlock();
        }

        /// <summary>
        /// Writes the two gradient centers + intensities to the editor's MaterialPropertyBlock
        /// and pushes it to the renderer. Allocation-free hot path.
        /// </summary>
        private void ApplyGradientToBlock()
        {
            if (_editor == null)
            {
                return;
            }
            MaterialPropertyBlock block = _editor.MaterialPropertyBlock;
            block.SetVector(s_centerID, _slots[0].SmoothedPosition);
            block.SetFloat(s_intensityID, _slots[0].Intensity);
            block.SetVector(s_center2ID, _slots[1].SmoothedPosition);
            block.SetFloat(s_intensity2ID, _slots[1].Intensity);
            _gradientProperties.ApplyToPropertyBlock(block);

            // Push explicitly so the MPB reaches the renderer regardless of the editor's
            // _updateEveryFrame toggle.
            _editor.UpdateMaterialPropertyBlock();
        }

        private void SetRendererEnabled(bool enabled)
        {
            _rendererEnabled = enabled;
            if (_renderer != null && _renderer.enabled != enabled)
            {
                _renderer.enabled = enabled;
            }
        }

        internal GazeHover3DPointerState GetPointerState(int slot) => _slots[slot];
        internal int? GetSlotId(int slot) => _slots[slot].PointerId;

        #region Inject

        public void InjectAllInteractableGazeHover3DVisual(
            IList<IPointable> pointables,
            Renderer renderer,
            MaterialPropertyBlockEditor editor)
        {
            InjectPointables(pointables);
            InjectRenderer(renderer);
            InjectEditor(editor);
        }

        public void InjectPointables(IList<IPointable> pointables)
        {
            // If we're currently subscribed, refresh the subscription set: unsubscribe the
            // old _resolvedPointables before mutating, re-resolve, then resubscribe so the
            // new pointables take over without dropping events or routing through stale
            // references.
            bool wasSubscribed = _subscribed;
            if (wasSubscribed)
            {
                UnsubscribeFromPointables();
            }

            if (_pointables == null)
            {
                _pointables = new List<UnityEngine.Object>(pointables?.Count ?? 0);
            }
            else
            {
                _pointables.Clear();
            }
            if (pointables != null)
            {
                for (int i = 0; i < pointables.Count; i++)
                {
                    _pointables.Add(pointables[i] as UnityEngine.Object);
                }
            }
            ResolvePointables();

            if (wasSubscribed)
            {
                SubscribeToPointables();
            }
        }

        public void InjectRenderer(Renderer renderer)
        {
            _renderer = renderer;
        }

        public void InjectEditor(MaterialPropertyBlockEditor editor)
        {
            _editor = editor;
        }

        /// <summary>
        /// Swaps the time provider used by <see cref="Drive"/> for the per-frame delta.
        /// Defaults to <see cref="Time.deltaTime"/>
        public void SetTimeProvider(Func<float> deltaTimeProvider)
        {
            _deltaTimeProvider = deltaTimeProvider ?? s_defaultDeltaProvider;
        }

        #endregion
    }
}
