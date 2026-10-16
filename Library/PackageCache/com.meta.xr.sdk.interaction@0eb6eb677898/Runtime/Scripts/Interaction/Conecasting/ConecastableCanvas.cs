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

using Oculus.Interaction.Surfaces;
using Oculus.Interaction.UnityCanvas;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Oculus.Interaction
{
    [Experimental]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    public class ConecastableCanvas : MonoBehaviour
    {
        private class Registry : HashSet<ConecastableCanvas> { }

        /// <summary>
        /// Will find all child targets under the provided root <see cref="ConecastableCanvas"/>.
        /// </summary>
        public static bool TryGetTargetsForRootCanvas(ConecastableCanvas root, IList<IConecastTarget> results)
        {
            var registry = Context.Global.GetInstance().GetOrCreateSingleton<Registry>();
            results.Clear();
            foreach (var conecastableCanvas in registry)
            {
                if (conecastableCanvas.transform.IsChildOf(root.transform))
                {
                    conecastableCanvas.TryGetConecastTargets(results);
                }
            }
            return results.Count > 0;
        }

        [SerializeField]
        [Tooltip("If true, this component will automatically make valid graphics " +
            "with the provided canvas cone-castable. If false, ConecastableCanvasTargets " +
            "must be explicitly added to Selectables.")]
        private bool _autoGenerateTargets = true;

        [SerializeField, Optional]
        [Interface(typeof(IComparer<GameObject>))]
        [Tooltip("If two elements are considered equal by the conecasting " +
            "algorithm, this comparer can be used to tiebreak them.")]
        private UnityEngine.Object _tiebreakComparer;
        private IComparer<GameObject> TiebreakComparer = null;

        [Header("Mesh Canvas Rendering")]
        [SerializeField]
        [Tooltip("If true, this component will inherit mesh rendering dependencies from " +
            "its parent ConecastableCanvas, if one is present.")]
        private bool _inheritMeshFromParent = true;

        [SerializeField]
        [Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("If the canvas is rendered onto a CanvasMesh, provide the " +
            "mesh here. The corresponding surface must also be provided.")]
        private CanvasMesh _canvasMesh;

        [SerializeField]
        [Interface(typeof(ISurface))]
        [Optional(OptionalAttribute.Flag.DontHide)]
        [Tooltip("If the canvas is rendered onto a CanvasMesh, provide the " +
            "corresponding surface here, which will be used for hit testing.")]
        private UnityEngine.Object _surface;
        private ISurface Surface;

        [Header("Debug")]
        [SerializeField]
        private bool _enableDebugVisuals = false;

        /// <summary>
        /// If true, mesh and surface dependencies will be inherited from the
        /// parent <see cref="ConecastableCanvas"/>, if one is present.
        /// </summary>
        public bool InheritMeshFromParent
        {
            get => _inheritMeshFromParent;
            set => _inheritMeshFromParent = value;
        }

        private ConecastableCanvas _parent;
        private Context _context;
        private Canvas _canvas;
        private bool _needsUpdate = false;
        private Comparison<UIConecastTarget> _tiebreakComparison;
        private List<UIConecastTarget> _targets = new();
        private HashSet<Graphic> _ignoredGraphics = new();
        private Dictionary<Graphic, UIConecastTarget> _graphicToTarget =
            new Dictionary<Graphic, UIConecastTarget>();

        protected bool _started = false;

        protected virtual void Awake()
        {
            _canvas = GetComponent<Canvas>();
            _context = Context.Global.GetInstance();
            _tiebreakComparison = TiebreakTargets;
            if (TiebreakComparer == null)
            {
                TiebreakComparer = _tiebreakComparer as IComparer<GameObject>;
            }
            if (Surface == null)
            {
                Surface = _surface as ISurface;
            }
        }

        protected virtual void Start()
        {
            this.BeginStart(ref _started);
            this.EndStart(ref _started);
        }

        protected virtual void OnEnable()
        {
            if (_started)
            {
                _context.GetOrCreateSingleton<Registry>().Add(this);
                Canvas.willRenderCanvases += OnWillRenderCanvases;
                UIConecastTarget.NotifyParentChanged += OnParentChanged;
                UIConecastTarget.NotifyTargetDestroyed += OnTargetDestroyed;
            }
        }

        protected virtual void OnDisable()
        {
            if (_started)
            {
                _context.GetOrCreateSingleton<Registry>().Remove(this);
                Canvas.willRenderCanvases -= OnWillRenderCanvases;
                UIConecastTarget.NotifyParentChanged -= OnParentChanged;
                UIConecastTarget.NotifyTargetDestroyed -= OnTargetDestroyed;
                ClearCaches();
            }
        }

        private void OnParentChanged(UIConecastTarget obj)
        {
            // Remove graphic from caches as it may need to be
            // ignored, no longer ignored, or removed from the
            // associated canvas entirely.
            _ignoredGraphics.Remove(obj.Graphic);
            _graphicToTarget.Remove(obj.Graphic);
            _needsUpdate = true;
        }

        private void OnTargetDestroyed(UIConecastTarget obj)
        {
            // Nuclear option, but a safer one. Clear all caches
            // when any target is destroyed, as references may be
            // invalid or null.
            ClearCaches();
            _needsUpdate = true;
        }

        private void ClearCaches()
        {
            _targets.Clear();
            _ignoredGraphics.Clear();
            _graphicToTarget.Clear();
        }

        private void OnWillRenderCanvases()
        {
            _needsUpdate = true;
        }

        private ISurface GetSurface() => _inheritMeshFromParent && _parent != null ?
            _parent.GetSurface() : Surface;

        private CanvasMesh GetCanvasMesh() => _inheritMeshFromParent && _parent != null ?
            _parent.GetCanvasMesh() : _canvasMesh;

        private void TryGetConecastTargets(IList<IConecastTarget> results)
        {
            if (!_started || !isActiveAndEnabled)
            {
                return;
            }

            UpdateTargets();

            foreach (var target in _targets)
            {
                if (target != null && target.IsValid)
                {
                    results.Add(target);
                }
            }
        }

        protected virtual void LateUpdate()
        {
            if (_enableDebugVisuals)
            {
                UpdateTargets();
                foreach (var target in _targets)
                {
                    if (target != null && target.IsValid)
                    {
                        target.DrawDebugVisual();
                    }
                }
            }
        }

        private void UpdateTargets()
        {
            if (!_needsUpdate)
            {
                return;
            }
            _needsUpdate = false;

            _parent = transform.parent?.GetComponentInParent<ConecastableCanvas>();

            _targets.Clear();
            var graphics = GraphicRegistry.GetRaycastableGraphicsForCanvas(_canvas);

            if (graphics == null || graphics.Count == 0)
            {
                return;
            }

            for (int i = 0; i < graphics.Count; i++)
            {
                var graphic = graphics[i];

                if (_ignoredGraphics.Contains(graphic))
                {
                    continue;
                }

                if (!_graphicToTarget.TryGetValue(graphic, out var target))
                {
                    // Ignore graphic if they do not have a Selectable parent.
                    var sel = graphic.GetComponentInParent<Selectable>(false);
                    if (sel == null)
                    {
                        _ignoredGraphics.Add(graphic);
                        continue;
                    }

                    // If graphics are explicitly provided for a selectable, ignore other child graphics.
                    if (sel.TryGetComponent<ConecastableCanvasTarget>(out var canvasTarget) &&
                        !canvasTarget.ContainsGraphic(graphic))
                    {
                        _ignoredGraphics.Add(graphic);
                        continue;
                    }

                    // If UIConecastTarget already exists, re-use it.
                    if (!graphic.gameObject.TryGetComponent(out target))
                    {
                        // Ignore targets not manually set if auto-generation is disabled.
                        if (!_autoGenerateTargets && canvasTarget == null)
                        {
                            _ignoredGraphics.Add(graphic);
                            continue;
                        }
                        target = graphic.gameObject.AddComponent<UIConecastTarget>();
                    }

                    target.Canvas = _canvas;
                    target.TiebreakComparison = _tiebreakComparison;

                    // Inherit from the provided component
                    target.GetCanvasMesh = GetCanvasMesh;
                    target.GetSurface = GetSurface;

                    _graphicToTarget.Add(graphic, target);
                }

                target.UpdateTarget();
                _targets.Add(target);
            }
        }

        private int TiebreakTargets(UIConecastTarget a, UIConecastTarget b)
        {
            if (TiebreakComparer != null)
            {
                return TiebreakComparer.Compare(a.gameObject, b.gameObject);
            }
            if (a.Graphic.canvas == b.Graphic.canvas)
            {
                return a.Graphic.depth.CompareTo(b.Graphic.depth);
            }
            else
            {
                int result = a.Graphic.canvas.sortingLayerID
                    .CompareTo(b.Graphic.canvas.sortingLayerID);

                if (result == 0)
                {
                    result = a.Graphic.canvas.sortingOrder.CompareTo(
                        b.Graphic.canvas.sortingOrder);
                }
                return result;
            }
        }

        #region Inject

        public void InjectOptionalCanvasMesh(CanvasMesh mesh)
        {
            _canvasMesh = mesh;
        }

        public void InjectOptionalSurface(ISurface surface)
        {
            _surface = surface as UnityEngine.Object;
            Surface = surface;
        }

        public void InjectOptionalTiebreakComparer(IComparer<GameObject> comparer)
        {
            _tiebreakComparer = comparer as UnityEngine.Object;
            TiebreakComparer = comparer;
        }

        #endregion

        private class UIConecastTarget : UIBehaviour, IClippable, IConecastTarget
        {
            private const float MIN_ALPHA_VALUE = 0.001f;

            public static event Action<UIConecastTarget> NotifyParentChanged = delegate { };
            public static event Action<UIConecastTarget> NotifyTargetDestroyed = delegate { };

            public Graphic Graphic => _graphic != null ?
                _graphic : (_graphic = GetComponent<Graphic>());

            public RectTransform rectTransform => transform as RectTransform;
            public bool IsValid => _isValid;

            public Canvas Canvas { get; set; }
            public Func<CanvasMesh> GetCanvasMesh { get; set; }
            public Func<ISurface> GetSurface { get; set; }
            public Comparison<UIConecastTarget> TiebreakComparison { get; set; }

            private Selectable Selectable => _selectable != null ?
                _selectable : (_selectable = GetComponentInParent<Selectable>());

            private UniqueIdentifier Identifier => _identifier != null ?
                _identifier : (_identifier = UniqueIdentifier.Generate(Context.Global.GetInstance(), gameObject));

            private UniqueIdentifier _identifier;
            private bool _isValid = false;
            private bool _shouldClip;
            private Rect _clipRect;
            private Graphic _graphic;
            private Selectable _selectable;
            private RectMask2D _parentMask;

            public void UpdateTarget()
            {
                _isValid = CheckValid();
            }

            private bool CheckValid()
            {
                return Graphic != null
                    && Selectable != null
                    && Graphic.isActiveAndEnabled
                    && Graphic.raycastTarget
                    && Graphic.canvasRenderer != null
                    && !Graphic.canvasRenderer.cull
                    && Selectable.interactable
                    && Graphic.canvasRenderer.GetInheritedAlpha() > MIN_ALPHA_VALUE;
            }

            private void UpdateClipParent()
            {
                var newParent = (Graphic is MaskableGraphic maskable && maskable.maskable && IsActive()) ?
                    MaskUtilities.GetRectMaskForClippable(this) : null;

                // if the new parent is different OR is now inactive
                if (_parentMask != null && (newParent != _parentMask || !newParent.IsActive()))
                {
                    _parentMask.RemoveClippable(this);
                }

                // don't re-add it if the newparent is inactive
                if (newParent != null && newParent.IsActive())
                    newParent.AddClippable(this);

                _parentMask = newParent;
            }

            #region IClippable

            void IClippable.RecalculateClipping() => UpdateClipParent();
            void IClippable.Cull(Rect clipRect, bool validRect) { }
            void IClippable.SetClipSoftness(Vector2 clipSoftness) { }

            void IClippable.SetClipRect(Rect clipRect, bool validRect)
            {
                _shouldClip = _parentMask != null && validRect;
                _clipRect = clipRect;
            }

            #endregion

            protected override void OnCanvasHierarchyChanged()
            {
                base.OnCanvasHierarchyChanged();
                UpdateClipParent();
                NotifyParentChanged(this);
            }

            protected override void OnTransformParentChanged()
            {
                base.OnTransformParentChanged();
                UpdateClipParent();
                NotifyParentChanged(this);
            }

            protected override void OnRectTransformDimensionsChange()
            {
                base.OnRectTransformDimensionsChange();
                UpdateClipParent();
            }

            protected override void OnEnable()
            {
                base.OnEnable();
                UpdateClipParent();
            }

            protected override void OnDisable()
            {
                base.OnDisable();
                UpdateClipParent();
            }

            protected override void OnDestroy()
            {
                base.OnDestroy();
                NotifyTargetDestroyed(this);
            }

            #region IConecastTarget

            ulong IConecastTarget.Id => (ulong)Identifier.ID;

            int IConecastTarget.TiebreakWith(IConecastTarget other)
            {
                if (other is UIConecastTarget otherTarget)
                {
                    return TiebreakComparison != null ?
                        TiebreakComparison(this, otherTarget) : 0;
                }
                return 0;
            }

            bool IConecastTarget.TryConecast(in Cone cone, out Vector3 hitPoint, out Vector3 hitNormal)
            {
                if (!_isValid)
                {
                    hitPoint = default;
                    hitNormal = default;
                    return false;
                }

                Ray ray = cone.Ray;

                Vector3 ClampAndClipLocalPoint(Vector3 localPoint)
                {
                    const float epsilon = 1f; // pixels from edge

                    // Clamp the local point to the rect of the element
                    Vector3 clampedLocal = new Vector3(
                        Mathf.Clamp(localPoint.x, rectTransform.rect.xMin + epsilon, rectTransform.rect.xMax - epsilon),
                        Mathf.Clamp(localPoint.y, rectTransform.rect.yMin + epsilon, rectTransform.rect.yMax - epsilon),
                        0);

                    if (_shouldClip)
                    {
                        // If the rect is clipped, clamp the point (in canvas space) to the clip rect.
                        // This only works properly if the rect has a cardinal rotation (factor of 90 degrees)
                        Vector3 inCanvasSpace = Canvas.rootCanvas.transform.InverseTransformPoint(rectTransform.TransformPoint(clampedLocal));
                        Vector3 clampedCanvas = new Vector3(
                            Mathf.Clamp(inCanvasSpace.x, _clipRect.xMin + epsilon, _clipRect.xMax - epsilon),
                            Mathf.Clamp(inCanvasSpace.y, _clipRect.yMin + epsilon, _clipRect.yMax - epsilon),
                            0);
                        clampedLocal = rectTransform.InverseTransformPoint(Canvas.rootCanvas.transform.TransformPoint(clampedCanvas));
                    }

                    return clampedLocal;
                }

                var canvasMesh = GetCanvasMesh();
                var surface = GetSurface();

                if (canvasMesh != null && surface != null)
                {
                    // Raycast against canvas mesh. Handles curved surfaces.
                    if (surface.Raycast(ray, out SurfaceHit hit))
                    {
                        Vector3 hitPointToCanvasSpace = canvasMesh.ImposterToCanvasTransformPoint(hit.Point);
                        Vector3 localPoint = rectTransform.InverseTransformPoint(hitPointToCanvasSpace);
                        Vector3 clamped = ClampAndClipLocalPoint(localPoint);
                        hitPoint = canvasMesh.CanvasToImposterTransformPoint(rectTransform.TransformPoint(clamped));
                        hitNormal = hit.Normal;
                        return cone.Contains(hitPoint);
                    }
                }
                else
                {
                    // Raycast against canvas rect. Handles normal non-mesh canvases.
                    hitNormal = rectTransform.forward * -Mathf.Sign(Vector3.Dot(ray.direction, rectTransform.forward));
                    Plane plane = new Plane(hitNormal, rectTransform.position);
                    if (plane.Raycast(ray, out float hitDistance))
                    {
                        Vector3 worldPoint = ray.GetPoint(hitDistance);
                        Vector3 localPoint = rectTransform.InverseTransformPoint(worldPoint);
                        Vector3 clamped = ClampAndClipLocalPoint(localPoint);
                        hitPoint = rectTransform.TransformPoint(clamped);
                        return cone.Contains(hitPoint);
                    }
                }

                hitPoint = default;
                hitNormal = default;
                return false;
            }

            #endregion IConecastable

            private readonly Vector3[] _debugCornerArray = new Vector3[4];
            public void DrawDebugVisual()
            {
                if (!IsValid)
                {
                    return;
                }

                bool CheckHasCardinalRotation()
                {
                    Vector3 deltaEuler = (rectTransform.rotation *
                        Quaternion.Inverse(Canvas.rootCanvas.transform.rotation)).eulerAngles;
                    for (int i = 0; i < 3; ++i)
                    {
                        // Close enough to cardinal, 
                        if (Mathf.Abs(deltaEuler[i] % 90) > 0.01f)
                        {
                            return false;
                        }
                    }
                    return true;
                }

                void UpdateWorldCorners(bool clip)
                {
                    rectTransform.GetWorldCorners(_debugCornerArray);

                    if (clip)
                    {
                        for (int i = 0; i < 4; i++)
                        {
                            Vector3 worldCorner = _debugCornerArray[i];
                            // If the rect is clipped, clamp the point (in canvas space) to the clip rect.
                            Vector3 inCanvasSpace = Canvas.rootCanvas.transform.InverseTransformPoint(worldCorner);
                            Vector3 clampedCanvas = new Vector3(
                                Mathf.Clamp(inCanvasSpace.x, _clipRect.xMin, _clipRect.xMax),
                                Mathf.Clamp(inCanvasSpace.y, _clipRect.yMin, _clipRect.yMax),
                                0);
                            _debugCornerArray[i] = Canvas.rootCanvas.transform.TransformPoint(clampedCanvas);
                        }
                    }
                }

                bool hasValidRotation = CheckHasCardinalRotation();
                bool shouldClipDebugRect = _shouldClip && hasValidRotation;

                UpdateWorldCorners(shouldClipDebugRect);

                DebugGizmos.Color = hasValidRotation ? Color.green : Color.red;
                DebugGizmos.LineWidth = 0.002f;
                for (int i = 0; i < 4; ++i)
                {
                    DebugGizmos.DrawLine(_debugCornerArray[i], _debugCornerArray[(i + 1) % 4]);
                }
            }

            public bool TryGetChildren(IList<IConecastTarget> children) => false;
        }
    }
}
