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

namespace Meta.XR.FovSimulator
{
    [AddComponentMenu("")]
    internal sealed class FovSimulator : MonoBehaviour
    {
        private const float PlaneZ = 0.03f;
        private const float QuadDistance = 2f;
        private const float MaskHalfAngleDegrees = 65f;
        private const int RenderTextureSize = 2048;
        private const int DynamicFrameCount = 4;
        private const int MainCameraLookupIntervalFrames = 30;
        private const int AutoIpdRefreshIntervalFrames = 30;
        private const int DefaultCompositionDepth = -1000;
        private const string OverlayShaderResource = "fov_simulation_overlay";
        private const string OverlayShaderName = "qxr/fov_simulation_overlay";
        private const float DefaultTopFovDegrees = 25f;
        private const float DefaultBottomFovDegrees = 41f;
        private const float DefaultInnerFovDegrees = 35f;
        private const float DefaultOuterFovDegrees = 35f;
        private const float DefaultTopOuterFrameDegrees = 41.5f;
        private const float DefaultBottomOuterFrameDegrees = 44.55f;
        private const float DefaultInnerOuterFrameDegrees = 48.9f;
        private const float DefaultOuterOuterFrameDegrees = 51.7f;
        private const float DefaultIpdMillimeters = 62.5f;
        private const float MinIpdMillimeters = 50f;
        private const float MaxIpdMillimeters = 75f;
        private const float MinValidDeviceIpdMeters = 0.04f;
        private const float MaxValidDeviceIpdMeters = 0.085f;
        private const float MaxHalfAngleDegrees = 65f;
        private const float DefaultCornerRadius = 0.12f;
        private const float MaxCornerRadiusRatio = 0.5f;

        private float _topFovDegrees = DefaultTopFovDegrees;
        private float _bottomFovDegrees = DefaultBottomFovDegrees;
        private float _innerFovDegrees = DefaultInnerFovDegrees;
        private float _outerFovDegrees = DefaultOuterFovDegrees;
        private float _topOuterFrameDegrees = DefaultTopOuterFrameDegrees;
        private float _bottomOuterFrameDegrees = DefaultBottomOuterFrameDegrees;
        private float _innerOuterFrameDegrees = DefaultInnerOuterFrameDegrees;
        private float _outerOuterFrameDegrees = DefaultOuterOuterFrameDegrees;
        private float _alpha = 1f;
        private float _cornerRadius = DefaultCornerRadius;
        private bool _outerFrameEnabled;
        private bool _autoIpd = true;
        private float _manualIpdMillimeters = DefaultIpdMillimeters;
        private int _compositionDepth = DefaultCompositionDepth;

        private GameObject _overlayObject;
        private OVROverlay _overlay;
        private RenderTexture _leftRenderTexture;
        private RenderTexture _rightRenderTexture;
        private Material _leftMaterial;
        private Material _rightMaterial;
        private int _dynamicFrames;
        private Camera _cachedMainCamera;
        private int _nextMainCameraLookupFrame;
        private float _lastAppliedIpdMeters = float.NaN;
        private int _nextAutoIpdRefreshFrame;

        private static readonly int InnerBoundsId = Shader.PropertyToID("_InnerBounds");
        private static readonly int OuterBoundsId = Shader.PropertyToID("_OuterBounds");
        private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
        private static readonly int IsRightId = Shader.PropertyToID("_IsRight");
        private static readonly int EnableOuterFrameId = Shader.PropertyToID("_EnableOuterFrame");
        private static readonly int EyeOffsetXId = Shader.PropertyToID("_EyeOffsetX");
        private static readonly int UvScaleId = Shader.PropertyToID("_UvScale");
        private static readonly int CornerRadiusId = Shader.PropertyToID("_CornerRadius");

        internal bool OverlayActive
        {
            get => _overlayObject;
            set
            {
                if (value)
                {
                    Build();
                }
                else
                {
                    Teardown();
                }
            }
        }

        internal float TopFovDegrees
        {
            get => _topFovDegrees;
            set { _topFovDegrees = ClampHalfAngle(value, nameof(TopFovDegrees)); RefreshMask(); }
        }

        internal float BottomFovDegrees
        {
            get => _bottomFovDegrees;
            set { _bottomFovDegrees = ClampHalfAngle(value, nameof(BottomFovDegrees)); RefreshMask(); }
        }

        internal float InnerFovDegrees
        {
            get => _innerFovDegrees;
            set { _innerFovDegrees = ClampHalfAngle(value, nameof(InnerFovDegrees)); RefreshMask(); }
        }

        internal float OuterFovDegrees
        {
            get => _outerFovDegrees;
            set { _outerFovDegrees = ClampHalfAngle(value, nameof(OuterFovDegrees)); RefreshMask(); }
        }

        internal float Alpha
        {
            get => _alpha;
            set
            {
                ThrowIfNaN(value, nameof(Alpha));
                _alpha = Mathf.Clamp01(value);
                RefreshMask();
            }
        }

        internal float CornerRadius
        {
            get => _cornerRadius;
            set
            {
                ThrowIfNaN(value, nameof(CornerRadius));
                _cornerRadius = Mathf.Clamp(value, 0f, MaxCornerRadiusRatio);
                RefreshMask();
            }
        }

        internal bool AutoIpd
        {
            get => _autoIpd;
            set { _autoIpd = value; RefreshMask(); }
        }

        internal float Ipd
        {
            get => ResolveIpdMeters() * 1000f;
            set
            {
                ThrowIfNaN(value, nameof(Ipd));
                _manualIpdMillimeters = Mathf.Clamp(value, MinIpdMillimeters, MaxIpdMillimeters);
                // A manually supplied IPD must take precedence over the device value.
                _autoIpd = false;
                RefreshMask();
            }
        }

        internal bool OuterFrameEnabled
        {
            get => _outerFrameEnabled;
            set { _outerFrameEnabled = value; RefreshMask(); }
        }

        internal float TopOuterFrameDegrees
        {
            get => _topOuterFrameDegrees;
            set { _topOuterFrameDegrees = ClampHalfAngle(value, nameof(TopOuterFrameDegrees)); RefreshMask(); }
        }

        internal float BottomOuterFrameDegrees
        {
            get => _bottomOuterFrameDegrees;
            set { _bottomOuterFrameDegrees = ClampHalfAngle(value, nameof(BottomOuterFrameDegrees)); RefreshMask(); }
        }

        internal float InnerOuterFrameDegrees
        {
            get => _innerOuterFrameDegrees;
            set { _innerOuterFrameDegrees = ClampHalfAngle(value, nameof(InnerOuterFrameDegrees)); RefreshMask(); }
        }

        internal float OuterOuterFrameDegrees
        {
            get => _outerOuterFrameDegrees;
            set { _outerOuterFrameDegrees = ClampHalfAngle(value, nameof(OuterOuterFrameDegrees)); RefreshMask(); }
        }

        internal int CompositionDepth
        {
            get => _compositionDepth;
            set
            {
                _compositionDepth = value;
                if (_overlay != null)
                {
                    _overlay.compositionDepth = value;
                }
            }
        }

        internal void ResetToDefaults()
        {
            _topFovDegrees = DefaultTopFovDegrees;
            _bottomFovDegrees = DefaultBottomFovDegrees;
            _innerFovDegrees = DefaultInnerFovDegrees;
            _outerFovDegrees = DefaultOuterFovDegrees;
            _topOuterFrameDegrees = DefaultTopOuterFrameDegrees;
            _bottomOuterFrameDegrees = DefaultBottomOuterFrameDegrees;
            _innerOuterFrameDegrees = DefaultInnerOuterFrameDegrees;
            _outerOuterFrameDegrees = DefaultOuterOuterFrameDegrees;
            _alpha = 1f;
            _cornerRadius = DefaultCornerRadius;
            _outerFrameEnabled = false;
            _autoIpd = true;
            _manualIpdMillimeters = DefaultIpdMillimeters;
            CompositionDepth = DefaultCompositionDepth;
            RefreshMask();
        }

        private void Awake()
        {
            enabled = false;
        }

        private void OnDestroy()
        {
            Teardown();
        }

        private void OnDisable()
        {
            if (_overlay != null)
            {
                _overlay.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (_overlay != null)
            {
                EnsureOverlayCamera(ResolveMainCamera());
            }
        }

        private void Build()
        {
            if (_overlayObject)
            {
                return;
            }
            if (_leftRenderTexture != null ||
                _rightRenderTexture != null ||
                _leftMaterial != null ||
                _rightMaterial != null)
            {
                Teardown();
            }

            Camera camera = Camera.main;
            if (camera == null)
            {
                enabled = true;
                _cachedMainCamera = null;
                _nextMainCameraLookupFrame = Time.frameCount;
                return;
            }
            _cachedMainCamera = camera;
            _nextMainCameraLookupFrame = Time.frameCount + MainCameraLookupIntervalFrames;

            Shader shader = Resources.Load<Shader>(OverlayShaderResource);
            if (shader == null)
            {
                shader = Shader.Find(OverlayShaderName);
            }
            if (shader == null)
            {
                Debug.LogError(
                    $"[FovSimulator] Shader resource '{OverlayShaderResource}' and shader '{OverlayShaderName}' were not found.",
                    this);
                enabled = false;
                return;
            }

            try
            {
                _leftRenderTexture = CreateRenderTexture("FovOverlayMask_Left");
                _rightRenderTexture = CreateRenderTexture("FovOverlayMask_Right");
                _leftMaterial = new Material(shader);
                _leftMaterial.SetFloat(IsRightId, 0f);
                _rightMaterial = new Material(shader);
                _rightMaterial.SetFloat(IsRightId, 1f);

                _overlayObject = new GameObject("FovSimulatorOverlay (Runtime)");
                AttachOverlayToCamera(camera);

                float tanHalf = Mathf.Tan(MaskHalfAngleDegrees * Mathf.Deg2Rad);
                float quadSize = 2f * QuadDistance * tanHalf;
                _overlayObject.transform.localScale = new Vector3(quadSize, quadSize, 1f);
                float uvScale = 2f * tanHalf * PlaneZ;
                _leftMaterial.SetFloat(UvScaleId, uvScale);
                _rightMaterial.SetFloat(UvScaleId, uvScale);

                _overlay = _overlayObject.AddComponent<OVROverlay>();
                _overlay.currentOverlayType = OVROverlay.OverlayType.Overlay;
                _overlay.currentOverlayShape = OVROverlay.OverlayShape.Quad;
                _overlay.noDepthBufferTesting = true;
                _overlay.isDynamic = false;
                _overlay.isAlphaPremultiplied = false;
                _overlay.compositionDepth = _compositionDepth;
                _overlay.textures[0] = _leftRenderTexture;
                _overlay.textures[1] = _rightRenderTexture;

                enabled = true;
                RefreshMask();
            }
            catch (System.Exception exception)
            {
                Teardown();
                Debug.LogException(exception, this);
            }
        }

        private void Teardown()
        {
            if (_overlay != null)
            {
                _overlay.enabled = false;
                if (_overlay.textures != null)
                {
                    if (_overlay.textures.Length > 0)
                    {
                        _overlay.textures[0] = null;
                    }
                    if (_overlay.textures.Length > 1)
                    {
                        _overlay.textures[1] = null;
                    }
                }
            }
            if (_overlayObject != null)
            {
                Destroy(_overlayObject);
            }
            ReleaseRenderTexture(ref _leftRenderTexture);
            ReleaseRenderTexture(ref _rightRenderTexture);
            DestroyObject(ref _leftMaterial);
            DestroyObject(ref _rightMaterial);

            _overlayObject = null;
            _overlay = null;
            _dynamicFrames = 0;
            _cachedMainCamera = null;
            _nextMainCameraLookupFrame = 0;
            _lastAppliedIpdMeters = float.NaN;
            _nextAutoIpdRefreshFrame = 0;
            enabled = false;
        }

        private static RenderTexture CreateRenderTexture(string textureName)
        {
            var texture = new RenderTexture(RenderTextureSize, RenderTextureSize, 0, RenderTextureFormat.ARGB32)
            {
                name = textureName,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };
            if (!texture.Create() || !texture.IsCreated())
            {
                Destroy(texture);
                throw new System.InvalidOperationException(
                    $"Failed to create render texture '{textureName}'.");
            }
            return texture;
        }

        private static void ReleaseRenderTexture(ref RenderTexture texture)
        {
            if (texture == null)
            {
                return;
            }
            texture.Release();
            Destroy(texture);
            texture = null;
        }

        private static void DestroyObject<T>(ref T value) where T : Object
        {
            if (value != null)
            {
                Destroy(value);
                value = null;
            }
        }

        private void RefreshMask()
        {
            if (_overlayObject == null ||
                _leftMaterial == null ||
                _rightMaterial == null ||
                _leftRenderTexture == null ||
                _rightRenderTexture == null)
            {
                return;
            }

            float ipdMeters = ResolveIpdMeters();
            _lastAppliedIpdMeters = ipdMeters;
            _nextAutoIpdRefreshFrame = Time.frameCount + AutoIpdRefreshIntervalFrames;
            ApplyMaskParameters(_leftMaterial, isRight: false, ipdMeters: ipdMeters);
            ApplyMaskParameters(_rightMaterial, isRight: true, ipdMeters: ipdMeters);
            Graphics.Blit(null, _leftRenderTexture, _leftMaterial);
            Graphics.Blit(null, _rightRenderTexture, _rightMaterial);

            MarkMaskDynamic();
        }

        private void MarkMaskDynamic()
        {
            if (!enabled)
            {
                return;
            }

            _dynamicFrames = DynamicFrameCount;
            if (_overlay != null)
            {
                _overlay.isDynamic = true;
            }
        }

        private void Update()
        {
            UpdateOverlayState();
        }

        internal void UpdateOverlayState()
        {
            if (_dynamicFrames > 0)
            {
                _dynamicFrames--;
                if (_dynamicFrames == 0 && _overlay != null)
                {
                    _overlay.isDynamic = false;
                }
            }

            EnsureOverlayCamera(ResolveMainCamera());
            RefreshAutoIpdIfNeeded();
        }

        private Camera ResolveMainCamera()
        {
            if (IsUsableMainCamera(_cachedMainCamera) &&
                Time.frameCount < _nextMainCameraLookupFrame)
            {
                return _cachedMainCamera;
            }

            _cachedMainCamera = Camera.main;
            _nextMainCameraLookupFrame = Time.frameCount + MainCameraLookupIntervalFrames;
            return _cachedMainCamera;
        }

        private static bool IsUsableMainCamera(Camera camera) =>
            camera != null && camera.isActiveAndEnabled && camera.CompareTag("MainCamera");

        private void EnsureOverlayCamera(Camera camera)
        {
            if (camera == null)
            {
                if (_overlay != null)
                {
                    _overlay.enabled = false;
                }
                return;
            }

            if (_overlayObject == null)
            {
                Build();
                return;
            }

            if (_overlayObject.transform.parent != camera.transform)
            {
                AttachOverlayToCamera(camera);
            }
            if (_overlay != null)
            {
                _overlay.enabled = true;
            }
        }

        private void RefreshAutoIpdIfNeeded()
        {
            if (!_autoIpd || _overlayObject == null ||
                Time.frameCount < _nextAutoIpdRefreshFrame)
            {
                return;
            }

            float ipdMeters = ResolveIpdMeters();
            _nextAutoIpdRefreshFrame = Time.frameCount + AutoIpdRefreshIntervalFrames;
            if (!Mathf.Approximately(ipdMeters, _lastAppliedIpdMeters))
            {
                RefreshMask();
            }
        }

        private void AttachOverlayToCamera(Camera camera)
        {
            _overlayObject.transform.SetParent(camera.transform, false);
            _overlayObject.transform.localPosition = new Vector3(0f, 0f, QuadDistance);
            _overlayObject.transform.localRotation = Quaternion.identity;
        }

        private float ResolveIpdMeters()
        {
            float ipdMeters = _autoIpd ? OVRPlugin.ipd : _manualIpdMillimeters / 1000f;
            if (float.IsNaN(ipdMeters) ||
                ipdMeters < MinValidDeviceIpdMeters ||
                ipdMeters > MaxValidDeviceIpdMeters)
            {
                ipdMeters = DefaultIpdMillimeters / 1000f;
            }
            return ipdMeters;
        }

        private void ApplyMaskParameters(Material material, bool isRight, float ipdMeters)
        {
            material.SetVector(InnerBoundsId, CreateBounds(
                _outerFovDegrees,
                _innerFovDegrees,
                _bottomFovDegrees,
                _topFovDegrees));
            material.SetVector(OuterBoundsId, CreateBounds(
                _outerOuterFrameDegrees,
                _innerOuterFrameDegrees,
                _bottomOuterFrameDegrees,
                _topOuterFrameDegrees));
            material.SetFloat(AlphaId, _alpha);
            material.SetFloat(CornerRadiusId, _cornerRadius);
            material.SetFloat(EnableOuterFrameId, _outerFrameEnabled ? 1f : 0f);

            float eyeOffset = ipdMeters * 0.5f * (PlaneZ / QuadDistance);
            material.SetFloat(EyeOffsetXId, isRight ? -eyeOffset : eyeOffset);
        }

        private static Vector4 CreateBounds(
            float leftDegrees,
            float rightDegrees,
            float bottomDegrees,
            float topDegrees)
        {
            return new Vector4(
                -PlaneZ * Mathf.Tan(leftDegrees * Mathf.Deg2Rad),
                PlaneZ * Mathf.Tan(rightDegrees * Mathf.Deg2Rad),
                -PlaneZ * Mathf.Tan(bottomDegrees * Mathf.Deg2Rad),
                PlaneZ * Mathf.Tan(topDegrees * Mathf.Deg2Rad));
        }

        private static float ClampHalfAngle(float value, string parameterName)
        {
            ThrowIfNaN(value, parameterName);
            return Mathf.Clamp(value, 0f, MaxHalfAngleDegrees);
        }

        private static void ThrowIfNaN(float value, string parameterName)
        {
            if (float.IsNaN(value))
            {
                throw new System.ArgumentException("Value cannot be NaN.", parameterName);
            }
        }
    }
}
