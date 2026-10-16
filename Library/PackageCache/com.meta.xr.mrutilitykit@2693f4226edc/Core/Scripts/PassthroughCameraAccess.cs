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
using System.Collections;
using System.Runtime.InteropServices;
using Meta.XR.MRUtilityKit;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using Meta.XR.Telemetry;
using Debug = UnityEngine.Debug;

namespace Meta.XR
{
    /// <summary>
    /// Provides access to the Passthrough Camera Access API.<br/>
    /// This component requires the "horizonos.permission.HEADSET_CAMERA" permission to be present in AndroidManifest.xml:
    /// <code>&lt;uses-permission android:name="horizonos.permission.HEADSET_CAMERA" /&gt;</code>
    /// See documentation: https://developers.meta.com/horizon/documentation/unity/unity-pca-overview/
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class PassthroughCameraAccess : MonoBehaviour
    {
        private const string CameraPermission = OVRPermissionsRequester.PassthroughCameraAccessPermission; // Required to access the Passthrough Camera API in Horizon OS v74 and above.
        private const string NoPermissionMessage = nameof(PassthroughCameraAccess) + " doesn't have the required camera permission: " + CameraPermission + ". Please verify that the permission is granted before using this API.";

        /// <summary>
        /// Possible positions of the camera (Left or Right).
        /// </summary>
        public enum CameraPositionType { Left, Right }
        /// <summary>Requested camera position. To access both left and right camera simultaneously, create two instances of <see cref="PassthroughCameraAccess"/> with different <see cref="PassthroughCameraAccess.CameraPosition"/></summary>
        [SerializeField] public CameraPositionType CameraPosition;
        /// <summary>The requested resolution of the camera. If the requested resolution is not present in <see cref="GetSupportedResolutions"/>, these rules are applied in order:<br/>
        /// - Tries to pick the first smaller resolution with the same aspect ratio.<br/>
        /// - Tries to pick the first smaller resolution.<br/>
        /// - Picks the smallest supported resolution.<br/></summary>
        [SerializeField] public Vector2Int RequestedResolution = new Vector2Int(1280, 960);
        [Tooltip("Maximum framerate for the camera stream (frames per second). The actual framerate may vary based on lighting conditions and the current workload.")]
        [SerializeField] private int _maxFramerate = 60;
        [Tooltip("If set, PassthroughCameraAccess will assign its internal Texture2D into this Material each frame.")]
        [SerializeField] public Material TargetMaterial;

        [Tooltip("(Optional) The name of the texture property to update. If blank, uses _MainTex.")]
        [SerializeField] private string _texturePropertyName = "";

        private int _texturePropertyID;
        private static readonly PassthroughCameraAccess[] _instances = new PassthroughCameraAccess[2];
        private Texture _texture;
        private int _currentCameraIndex;

        private bool? _isPlaying;
        private int _lastUpdatedImageFrame;
        private bool _wasPlayingBeforePause;
        private long _timestampNsMonotonic; // same time base as XrTime
        private NativeArray<Color32> _colorsBuffer;
        private bool _hasPermission;

        /// <summary>
        /// Maximum framerate for the camera stream (frames per second). The actual framerate may vary based on lighting conditions and the current workload.
        /// This property can only be changed when the component is disabled.
        /// </summary>
        public int MaxFramerate
        {
            get => _maxFramerate;
            set
            {
                if (enabled)
                {
                    Debug.LogError($"{nameof(MaxFramerate)} can only be changed while {nameof(PassthroughCameraAccess)} is not running. Please disable the component before setting {nameof(MaxFramerate)}.");
                }
                else
                {
                    _maxFramerate = Mathf.Clamp(value, 1, int.MaxValue);
                }
            }
        }

        /// <summary>
        /// The name of the texture property to update. If changed at runtime,
        /// we recache the shader property ID so that Material.SetTexture(...) uses the new name.
        /// </summary>
        public string TexturePropertyName
        {
            get => _texturePropertyName;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value), "TexturePropertyName cannot be null.");
                }

                if (_texturePropertyName == value)
                {
                    return;
                }

                _texturePropertyName = value;
                _texturePropertyID = Shader.PropertyToID(string.IsNullOrEmpty(value) ? "_MainTex" : value);
            }
        }

        /// <summary>Indicates whether the component is enabled and has received the camera image at least once.</summary>
        public bool IsPlaying => _isPlaying == true;

        /// <summary>Timestamp associated with the latest camera image.</summary>
        public DateTime Timestamp { get; private set; }

        /// <summary>The current resolution of the camera. May be different from the <see cref="RequestedResolution"/>.</summary>
        public Vector2Int CurrentResolution { get; private set; }

        /// <summary>The static intrinsic parameters of the sensor. These parameters become available after the <see cref="PassthroughCameraAccess"/> is enabled and never change after that.</summary>
        public CameraIntrinsics Intrinsics { get; private set; }

        /// <summary>
        /// Retrieves color data of the latest camera image. Use this method to process camera images on CPU.<br/>
        /// Do not cache, modify or dispose the contents of the returned native array.<br/>
        /// NOTE: this method is expensive, consider using a non-blocking version of <see cref="AsyncGPUReadback"/> instead.
        /// </summary>
        /// <returns>Native array that contains all pixels of the captured image.</returns>
        public NativeArray<Color32> GetColors()
        {
            if (!ValidateState())
            {
                return default;
            }
            if (!_colorsBuffer.IsCreated)
            {
                _colorsBuffer = new NativeArray<Color32>(CurrentResolution.x * CurrentResolution.y, Allocator.Persistent);
            }
            AsyncGPUReadback.RequestIntoNativeArray(ref _colorsBuffer, _texture).WaitForCompletion();
            return _colorsBuffer;
        }

        /// <summary>Retrieves GPU texture of the latest camera image. Use this method to access camera images on GPU.<br/>
        /// The texture is updated in render thread before the frame is displayed. This means that performing blocking operations such as Graphics.Blit() will pick the texture from the previous frame. To access the latest texture on CPU, use <see cref="AsyncGPUReadback"/> instead.</summary>
        /// <returns>Texture with the latest camera image.</returns>
        public Texture GetTexture()
        {
            if (!ValidateState())
            {
                return null;
            }
            return _texture;
        }

        private bool ValidateState()
        {
            if (!enabled)
            {
                Debug.LogError(nameof(PassthroughCameraAccess) + " is not enabled. Please enable the component before accessing this API.", this);
                return false;
            }
            if (!_hasPermission)
            {
                Debug.LogError(NoPermissionMessage, this);
                return false;
            }
            return true;
        }

        /// <summary>Returns 'true' if the camera texture was updated this frame.</summary>
        public bool IsUpdatedThisFrame => _lastUpdatedImageFrame == Time.frameCount;

        /// <summary>Returns true if the current headset supports Passthrough Camera Access.</summary>
        public static bool IsSupported
        {
            get
            {
                var headset = OVRPlugin.GetSystemHeadsetType();
#if UNITY_EDITOR
                if (Application.isEditor)
                {
                    return headset is OVRPlugin.SystemHeadset.Meta_Link_Quest_3 or OVRPlugin.SystemHeadset.Meta_Link_Quest_3S or OVRPlugin.SystemHeadset.None
                    ;
                }
#endif

                if (headset is not (OVRPlugin.SystemHeadset.Meta_Quest_3 or OVRPlugin.SystemHeadset.Meta_Quest_3S
                ))
                {
                    return false;
                }

                using var vrosClass = new AndroidJavaClass("vros.os.VrosBuild");
                var osVersion = vrosClass.CallStatic<int>("getSdkVersion");
                const int minSupportedVersion = 74;
                return osVersion >= minSupportedVersion;
            }
        }

        private void Update()
        {
            if (!_isPlaying.HasValue)
            {
                return;
            }

            long timestampMicroseconds = 0;
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                unsafe
                {
                    byte* buffer = MRUKNativeFuncs.CameraAcquireLatestCpuImage != null
                        ? MRUKNativeFuncs.CameraAcquireLatestCpuImage(_currentCameraIndex, ref timestampMicroseconds, ref _timestampNsMonotonic)
                        : null;
                    if (buffer == null)
                    {
                        return;
                    }
                    try
                    {
                        var tex2d = _texture as Texture2D;
                        Assert.IsNotNull(tex2d);
                        tex2d.LoadRawTextureData((IntPtr)buffer, CurrentResolution.x * CurrentResolution.y * sizeof(Color32));
                        tex2d.Apply();
                    }
                    finally
                    {
                        MRUKNativeFuncs.CameraReleaseLatestCpuImage?.Invoke(_currentCameraIndex);
                    }
                }
            }
            else
#endif
            {
                if (!MRUKNativeFuncs.CameraGetLatestImage(_currentCameraIndex, ref timestampMicroseconds, ref _timestampNsMonotonic))
                {
                    return;
                }
                PcaDebugLog("GL.IssuePluginEvent");
                GL.IssuePluginEvent(Marshal.GetFunctionPointerForDelegate(MRUKNativeFuncs.CameraUpdateNativeTexture), _currentCameraIndex);
                GL.InvalidateState(); // Needed because native code modifies render state
            }

            _isPlaying = true;
            const long ticksPerMicrosecond = 10;
            Timestamp = DateTime.UnixEpoch.AddTicks(timestampMicroseconds * ticksPerMicrosecond);
            _lastUpdatedImageFrame = Time.frameCount;

            if (TargetMaterial)
            {
                if (TargetMaterial.HasProperty(_texturePropertyID))
                {
                    TargetMaterial.SetTexture(_texturePropertyID, _texture);
                }
                else
                {
                    Debug.LogWarning($"Texture property '{_texturePropertyName}' not found on the material for GameObject {gameObject.name}.", this);
                }
            }
        }

        private void Awake()
        {
            if (_texturePropertyName == null)
            {
                throw new InvalidOperationException("_texturePropertyName cannot be null.");
            }

            _texturePropertyID = Shader.PropertyToID(string.IsNullOrEmpty(_texturePropertyName) ? "_MainTex" : _texturePropertyName);
        }

        private void Start()
        {
            var unifiedEvent = new UnifiedEventData(TelemetryConstants.EventName.LoadPassthroughCameraAccess);
            unifiedEvent.SendMRUKEvent();
        }

        private void OnEnable()
        {
            _hasPermission = Permission.HasUserAuthorizedPermission(CameraPermission);
            if (_hasPermission)
            {
                TryPlayOrDisable();
            }
            else
            {
                Debug.LogWarning(nameof(PassthroughCameraAccess) + " doesn't have the required camera permission: " + CameraPermission +
                                 ". Waiting for permission before enabling the camera...\n" +
                                 "To request camera permission, AndroidManifest.xml should contain this entry:\n" +
                                 "<uses-permission android:name=\"horizonos.permission.HEADSET_CAMERA\" />");
                StartCoroutine(WaitForPermissionsAndPlay());
            }
        }

        private void OnValidate()
        {
            _maxFramerate = Mathf.Clamp(_maxFramerate, 1, int.MaxValue);
            RequestedResolution.Clamp(new Vector2Int(1, 1), new Vector2Int(int.MaxValue, int.MaxValue));
        }

        private IEnumerator WaitForPermissionsAndPlay()
        {
            while (true)
            {
                _hasPermission = Permission.HasUserAuthorizedPermission(CameraPermission);
                if (_hasPermission)
                {
                    break;
                }
                yield return null;
            }
            TryPlayOrDisable();
        }

        private void OnDisable()
        {
            StopCoroutine(WaitForPermissionsAndPlay());
            if (_isPlaying.HasValue)
            {
                Stop();
                if (_texture != null)
                {
                    Destroy(_texture);
                    _texture = null;
                }
                CurrentResolution = Vector2Int.zero;
            }
            if (_colorsBuffer.IsCreated)
            {
                _colorsBuffer.Dispose();
            }
        }

        private void OnApplicationFocus(bool isFocused)
        {
            if (isFocused)
            {
                MRUKNativeFuncs.CameraOnApplicationFocused?.Invoke();
            }
        }

        private void OnApplicationPause(bool isPaused)
        {
            PcaDebugLog($"OnApplicationPause {isPaused} {_currentCameraIndex}, CurrentResolution:{CurrentResolution}");
            if (isPaused)
            {
                if (_isPlaying.HasValue)
                {
                    _wasPlayingBeforePause = true;
                    Stop();
                }
            }
            else if (_wasPlayingBeforePause)
            {
                _wasPlayingBeforePause = false;
                Play(CurrentResolution);
            }
        }

        private void TryPlayOrDisable()
        {
            if (!Play(RequestedResolution))
            {
                enabled = false;
            }
        }

        private bool Play(Vector2Int resolution)
        {
            Assert.IsFalse(_isPlaying.HasValue);
            _currentCameraIndex = GetCameraIndex(CameraPosition);
            if (_instances[_currentCameraIndex] != null)
            {
                Debug.LogError($"Only one instance of {nameof(PassthroughCameraAccess)} is allowed per camera position ({_currentCameraIndex}). Please ensure you're not creating two instances with the same '{nameof(CameraPosition)}'.", this);
                return false;
            }
            int w = resolution.x;
            int h = resolution.y;
            MRUKNativeFuncs.MrukCameraIntrinsics intrinsics = default;
            if (!MRUKNativeFuncs.CameraPlay(_currentCameraIndex, ref w, ref h, ref intrinsics, _maxFramerate))
            {
                if (w == -1 || h == -1)
                {
                    Debug.LogError($"Requested resolution {resolution} is too small.", this);
                }
                else
                {
#if UNITY_EDITOR
                    IssueTracker.TrackError(IssueTracker.SDK.MRUK, "mruk-passthrough-camera-play-failed",
                        $"{nameof(PassthroughCameraAccess)} failed to play camera at index {_currentCameraIndex}. " +
                        $"Requires: 'Meta Horizon Link' v85 (or newer) or 'Meta XR Simulator' v85 (or newer), " +
                        $"and Quest 3 or Quest 3S headset running HzOS v85 (or newer).");
#else
                    Debug.LogError($"{nameof(PassthroughCameraAccess)} failed to play camera at index {_currentCameraIndex}.", this);
#endif
                }
                return false;
            }
            PcaDebugLog($"Play() {_currentCameraIndex} {w}x{h}");
            _instances[_currentCameraIndex] = this;

            if (resolution != Vector2Int.zero && resolution != new Vector2Int(w, h))
            {
                Debug.LogWarning($"Requested resolution {resolution} is not supported, using ({w}, {h}) instead.");
            }
            Assert.IsFalse(_isPlaying.HasValue);
            _isPlaying = false;
            CurrentResolution = new Vector2Int(w, h);
            var lensTranslation = intrinsics.lensTranslation;
            var lensRotation = intrinsics.lensRotation;
            Intrinsics = new CameraIntrinsics
            {
                FocalLength = intrinsics.focalLength,
                PrincipalPoint = intrinsics.principalPoint,
                SensorResolution = intrinsics.sensorResolution,
                LensOffset = new Pose(MRUK.FlipZ(lensTranslation), Quaternion.Inverse(new Quaternion(-lensRotation[0], -lensRotation[1], lensRotation[2], lensRotation[3])) * Quaternion.Euler(180, 0, 0))
            };

            if (_texture == null)
            {
#if UNITY_EDITOR
                if (Application.isEditor)
                {
                    _texture = new Texture2D(CurrentResolution.x, CurrentResolution.y, TextureFormat.RGBA32, false);
                    return true;
                }
#endif
                bool isLinear = QualitySettings.activeColorSpace == ColorSpace.Linear;
                var format = isLinear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                var rt = new RenderTexture(CurrentResolution.x, CurrentResolution.y, 0, format)
                {
                    useMipMap = false,
                    autoGenerateMips = false
                };
                rt.Create();
                _texture = rt;
            }
            MRUKNativeFuncs.CameraSetNativeTexture(_currentCameraIndex, _texture.GetNativeTexturePtr());
            return true;
        }

        private void Stop()
        {
            PcaDebugLog($"Stop() {_currentCameraIndex}");
            _instances[_currentCameraIndex] = null;
            if (MRUKNativeFuncs.CameraStop != null)
            {
                MRUKNativeFuncs.CameraStop(_currentCameraIndex);
                GL.IssuePluginEvent(Marshal.GetFunctionPointerForDelegate(MRUKNativeFuncs.CameraUpdateNativeTexture), _currentCameraIndex);
            }
            _isPlaying = null;
            Timestamp = default;
            Intrinsics = default;
            PcaDebugLog("GL.IssuePluginEvent() after CameraStop()");
        }

        private static int GetCameraIndex(CameraPositionType cameraPosition)
        {
            switch (cameraPosition)
            {
                case CameraPositionType.Left:
                    return 0;
                case CameraPositionType.Right:
                    return 1;
                default: throw new Exception($"Invalid camera position: {cameraPosition}.");
            }
        }

        /// <summary>Retrieves supported resolutions of the given camera.</summary>
        /// <param name="cameraPosition">The position of the camera (Left or Right).</param>
        /// <returns>An array containing all supported resolutions.</returns>
        public static unsafe Vector2Int[] GetSupportedResolutions(CameraPositionType cameraPosition)
        {
            if (!Permission.HasUserAuthorizedPermission(CameraPermission))
            {
                Debug.LogError(NoPermissionMessage);
                return Array.Empty<Vector2Int>();
            }
            int cameraIndex = GetCameraIndex(cameraPosition);
            int len = 0;
            Vector2Int* buffer = MRUKNativeFuncs.CameraGetSupportedResolutions(cameraIndex, ref len);
            var result = new Vector2Int[len];
            for (uint i = 0; i < len; ++i)
            {
                result[i] = buffer[i];
            }
            MRUKNativeFuncs.CameraFreeSupportedResolutions(buffer);
            return result;
        }

        /// <summary>Returns a world-space ray going from camera through a viewport point.</summary>
        /// <param name="viewportPoint">Viewport-space is normalized and relative to the camera. The bottom-left of the camera is (0,0); the top-right is (1,1).</param>
        /// <param name="cameraPose">Optional camera pose that should be used for calculation. For example, you can cache <see cref="GetCameraPose"/>, do a long-running image processing, then use the cached camera pose with this method.</param>
        /// <returns>World-space ray.</returns>
        public Ray ViewportPointToRay(Vector2 viewportPoint, Pose? cameraPose = null)
        {
            if (!ValidateIsPlaying())
            {
                return default;
            }
            var camPose = cameraPose ?? GetCameraPose();
            var direction = camPose.rotation * ViewportPointToLocalRay(viewportPoint).direction;
            return new Ray(camPose.position, direction);
        }

        private Ray ViewportPointToLocalRay(Vector2 viewportPoint)
        {
            var intrinsics = Intrinsics;
            var sensorCropRegion = CalcSensorCropRegion();
            var principalPoint = intrinsics.PrincipalPoint;
            var focalLength = intrinsics.FocalLength;
            var directionInCamera = new Vector3
            {
                x = (sensorCropRegion.x + sensorCropRegion.width * viewportPoint.x - principalPoint.x) / focalLength.x,
                y = (sensorCropRegion.y + sensorCropRegion.height * viewportPoint.y - principalPoint.y) / focalLength.y,
                z = 1
            };

            return new Ray(Vector3.zero, directionInCamera);
        }

        /// <summary>Transforms <paramref name="worldPosition"/> from world-space into viewport-space.</summary>
        /// <param name="worldPosition">A world-space position.</param>
        /// <param name="cameraPose">Optional camera pose that should be used for calculation. For example, you can cache <see cref="GetCameraPose"/>, do a long-running image processing, then use the cached camera pose with this method.</param>
        /// <returns>Viewport-space coordinate. Viewport-space is normalized and relative to the camera. The bottom-left of the camera is (0,0); the top-right is (1,1).</returns>
        public Vector2 WorldToViewportPoint(Vector3 worldPosition, Pose? cameraPose = null)
        {
            if (!ValidateIsPlaying())
            {
                return default;
            }
            Pose camPose = cameraPose ?? GetCameraPose();
            Vector3 positionInCameraSpace = Quaternion.Inverse(camPose.rotation) * (worldPosition - camPose.position);
            var intrinsics = Intrinsics;
            var focalLength = intrinsics.FocalLength;
            var principalPoint = intrinsics.PrincipalPoint;
            var sensorPoint = new Vector2(
                (positionInCameraSpace.x / positionInCameraSpace.z) * focalLength.x + principalPoint.x,
                (positionInCameraSpace.y / positionInCameraSpace.z) * focalLength.y + principalPoint.y);
            var sensorCropRegion = CalcSensorCropRegion();
            return new Vector2(
                (sensorPoint.x - sensorCropRegion.x) / sensorCropRegion.width,
                (sensorPoint.y - sensorCropRegion.y) / sensorCropRegion.height);
        }

        private Rect CalcSensorCropRegion()
        {
            var sensorResolution = (Vector2)Intrinsics.SensorResolution;
            var currentResolution = (Vector2)CurrentResolution;
            Vector2 scaleFactor = currentResolution / sensorResolution;
            scaleFactor /= Mathf.Max(scaleFactor.x, scaleFactor.y);
            return new Rect(
                sensorResolution.x * (1f - scaleFactor.x) * 0.5f,
                sensorResolution.y * (1f - scaleFactor.y) * 0.5f,
                sensorResolution.x * scaleFactor.x,
                sensorResolution.y * scaleFactor.y);
        }

        /// <returns>Camera's world-space pose at <see cref="Timestamp"/>.</returns>
        public Pose GetCameraPose()
        {
            if (!ValidateIsPlaying())
            {
                return default;
            }
            if (MRUKNativeFuncs.GetHeadsetPoseAtTime == null)
            {
                return default;
            }
            Vector3 pos = default;
            Quaternion rot = default;
            MRUKNativeFuncs.GetHeadsetPoseAtTime(_timestampNsMonotonic, ref pos, ref rot);
            var headPose = MRUK.FlipZ(new Pose(pos, rot));
            var lensOffset = Intrinsics.LensOffset;
            return new Pose(headPose.position + headPose.rotation * lensOffset.position,
                headPose.rotation * lensOffset.rotation);
        }

        private bool ValidateIsPlaying()
        {
            if (IsPlaying)
            {
                return true;
            }
            Debug.LogError(nameof(PassthroughCameraAccess) + " is not playing. Please check the '" + nameof(IsPlaying) + "' before calling this API.", this);
            return false;
        }

        /// <summary>The static intrinsic parameters of the sensor.</summary>
        public struct CameraIntrinsics
        {
            /// <summary>The focal length of the camera.</summary>
            public Vector2 FocalLength;
            /// <summary>The principal point of the camera.</summary>
            public Vector2 PrincipalPoint;
            /// <summary>The sensor resolution of the camera.</summary>
            public Vector2Int SensorResolution;
            /// <summary>The translation and orientation of the camera sensor relative to the headset.</summary>
            public Pose LensOffset;
        }

        [System.Diagnostics.Conditional("DEBUG_PASSTHROUGH_CAMERA_ACCESS")]
        private void PcaDebugLog(object msg)
        {
            Debug.LogWarning($"frame:[{Time.frameCount}] PassthroughCamera {_currentCameraIndex}: {msg}");
        }
    }
}
