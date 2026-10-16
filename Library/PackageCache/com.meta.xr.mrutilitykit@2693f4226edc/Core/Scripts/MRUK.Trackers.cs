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
using Meta.XR.Telemetry;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.Events;

namespace Meta.XR.MRUtilityKit
{
    partial class MRUK
    {
        /// <summary>
        /// A unique identifier for a trackable.
        /// Depending on the origin of the trackable either EntityId or space will be zero.
        /// This id helps to create a unique identifier for a trackable.
        /// </summary>
        private readonly struct TrackableKey : IEquatable<TrackableKey>
        {
            /// <summary>
            /// Set in case the trackable originated from ext_spatial_entity
            /// </summary>
            private readonly ulong _entityId;

            /// <summary>
            /// Set in case the trackable originated from fb_spatial_entity_query
            /// </summary>
            private readonly ulong _space;

            /// <summary>
            /// Creates a new TrackableKey with the specified space and entity identifiers.
            /// </summary>
            /// <param name="space">The tracking space identifier.</param>
            /// <param name="entityId">The entity id.</param>
            public TrackableKey(ulong space, ulong entityId)
            {
                _space = space;
                _entityId = entityId;
            }

            /// <summary>
            /// Determines whether this TrackableKey is equal to another TrackableKey.
            /// </summary>
            /// <param name="other">The TrackableKey to compare with this instance.</param>
            /// <returns>True if both the Space and EntityId match; otherwise, false.</returns>
            public bool Equals(TrackableKey other)
            {
                return _space == other._space && _entityId == other._entityId;
            }

            /// <summary>
            /// Determines whether this TrackableKey is equal to another object.
            /// </summary>
            /// <param name="obj">The object to compare with this instance.</param>
            /// <returns>True if the object is a TrackableKey and both the Space and EntityId match; otherwise, false.</returns>
            public override bool Equals(object obj)
            {
                return obj is TrackableKey other && Equals(other);
            }

            /// <summary>
            /// Returns a hash code for this TrackableKey.
            /// </summary>
            /// <returns>A hash code based on both the space and EntityId values.</returns>
            public override int GetHashCode()
            {
                return HashCode.Combine(_space, _entityId);
            }

            /// <summary>
            /// Determines whether two TrackableKey instances are equal.
            /// </summary>
            /// <param name="left">The first TrackableKey to compare.</param>
            /// <param name="right">The second TrackableKey to compare.</param>
            /// <returns>True if both TrackableKey instances are equal; otherwise, false.</returns>
            public static bool operator ==(TrackableKey left, TrackableKey right)
            {
                return left.Equals(right);
            }

            /// <summary>
            /// Determines whether two TrackableKey instances are not equal.
            /// </summary>
            /// <param name="left">The first TrackableKey to compare.</param>
            /// <param name="right">The second TrackableKey to compare.</param>
            /// <returns>True if the TrackableKey instances are not equal; otherwise, false.</returns>
            public static bool operator !=(TrackableKey left, TrackableKey right)
            {
                return !left.Equals(right);
            }

            /// <summary>
            /// Creates a string representation of this <see cref="TrackableKey"/>.
            /// </summary>
            /// <remarks>
            /// This is intended for debugging purposes.
            /// </remarks>
            /// <returns>Returns a string representation of this <see cref="TrackableKey"/>.</returns>
            public override string ToString()
            {
                return $"Space: {_space}, EntityId: {_entityId}";
            }
        }

        partial class MRUKSettings
        {
            /// <summary>
            /// The requested configuration of the tracking service.
            /// </summary>
            /// <remarks>
            /// This property represents the requested tracker configuration (which types of trackables to track). It is possible that some
            /// configuration settings may not be satisfied (for example, due to lack of device support). <see cref="MRUK.TrackerConfiguration"/>
            /// represents the true state of the system.
            /// </remarks>
            [field: SerializeField, Tooltip("Settings related to trackables that are detectable in the environment at runtime.")]
            public OVRAnchor.TrackerConfiguration TrackerConfiguration { get; set; }

            /// <summary>
            /// Invoked when a newly detected trackable has been localized.
            /// </summary>
            /// <remarks>
            /// When a new <see cref="OVRAnchor"/> has been detected and localized, a new `GameObject` with a <see cref="MRUKTrackable"/> is created
            /// to represent it. Its transform is set, and then this event is invoked.
            ///
            /// Subscribe to this event to add additional child GameObjects or further customize the behavior.
            ///
            /// <example>
            /// This example shows how to create a MonoBehaviour that instantiates a custom prefab:
            /// <code><![CDATA[
            /// class MyCustomManager : MonoBehaviour
            /// {
            ///     public GameObject Prefab;
            ///
            ///     public void OnTrackableAdded(MRUKTrackable trackable)
            ///     {
            ///         Instantiate(Prefab, trackable.transform);
            ///     }
            /// }
            /// ]]></code>
            /// </example>
            /// </remarks>
            [field: SerializeField, Tooltip("Invoked after a newly detected anchor has been localized.")]
            public UnityEvent<MRUKTrackable> TrackableAdded { get; private set; } = new();

            /// <summary>
            /// Invoked when an existing trackable is no longer detected by the runtime.
            /// </summary>
            /// <remarks>
            /// When an anchor is removed, no action is taken by default. The <see cref="MRUKTrackable"/>, if any, is not destroyed or deactivated.
            /// Subscribe to this event to change this behavior.
            ///
            /// Once this event has been invoked, the <see cref="MRUKTrackable"/>'s anchor (<see cref="MRUKTrackable.Anchor"/>) is no longer valid.
            /// </remarks>
            [field: SerializeField, Tooltip("The event is invoked when an anchor is removed.")]
            public UnityEvent<MRUKTrackable> TrackableRemoved { get; private set; } = new();
        }

        /// <summary>
        /// The current configuration for the tracking service.
        /// </summary>
        /// <remarks>
        /// To request a particular configuration, set the desired values in <see cref="MRUKSettings.TrackerConfiguration"/>.
        /// This property represents the true state of the system.
        /// This may differ from what was requested with <see cref="MRUKSettings.TrackerConfiguration"/> if, for example, some types of trackables are not supported on the current device.
        /// </remarks>
        public OVRAnchor.TrackerConfiguration TrackerConfiguration { get; private set; }

        /// <summary>
        /// Whether QR code tracking is supported.
        /// </summary>
        /// <remarks>
        /// If QR code tracking is supported, you can enable it by setting the <see cref="OVRAnchor.TrackerConfiguration.QRCodeTrackingEnabled"/>
        /// property on the <see cref="TrackerConfiguration"/>.
        /// </remarks>
        public bool QRCodeTrackingSupported => MRUKNativeFuncs.CheckQrCodeTrackingSupported != null && MRUKNativeFuncs.CheckQrCodeTrackingSupported();

        /// <summary>
        /// Get all the trackables that have been detected so far.
        /// </summary>
        /// <param name="trackables">The list to populate with the trackables. The list is cleared before adding any elements.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="trackables"/> is `null`.</exception>
        public void GetTrackables(List<MRUKTrackable> trackables)
        {
            if (trackables == null)
            {
                throw new ArgumentNullException(nameof(trackables));
            }

            trackables.Clear();
            foreach (var trackable in _trackables.Values)
            {
                if (trackable)
                {
                    trackables.Add(trackable);
                }
            }
        }

        private readonly Dictionary<TrackableKey, MRUKTrackable> _trackables = new();

        private bool _hasScenePermission;

        private OVRAnchor.TrackerConfiguration _lastRequestedConfiguration;

        private TimeSpan _nextTrackerConfigurationTime;

        // 0.5 seconds because most of our trackers update at about 1 Hz
        private static readonly TimeSpan s_timeBetweenTrackerConfigurationAttempts = TimeSpan.FromSeconds(0.5);

        private static TrackableKey GetTrackableKey(ref MRUKNativeFuncs.MrukTrackable trackable)
        {
            return new TrackableKey(trackable.space, trackable.entityId);
        }

        private void UpdateTrackables()
        {
            var now = TimeSpan.FromSeconds(Time.realtimeSinceStartup);

            // We should only try to set the tracker configuration if
            // 1. The requested configuration has changed since last time
            // 2. The actual tracker configuration does not match the requested one, but permissions have also changed, which may now allow one of the failing requests to succeed.
            var desiredConfig = SceneSettings.TrackerConfiguration;
            if (_configureTrackersTask.HasValue ||
                TrackerConfiguration == desiredConfig ||
                now < _nextTrackerConfigurationTime)
            {
                return;
            }

            if (!_hasScenePermission && Permission.HasUserAuthorizedPermission(OVRPermissionsRequester.ScenePermission))
            {
                _hasScenePermission = true;
            }

            if (_hasScenePermission && _lastRequestedConfiguration != desiredConfig)
            {
                _lastRequestedConfiguration = desiredConfig;
                ConfigureTrackerAndLogResult(desiredConfig);
            }

            _nextTrackerConfigurationTime = now + s_timeBetweenTrackerConfigurationAttempts;
        }

        private void OnDisable()
        {
            if (MRUKNativeFuncs.ConfigureTrackers != null)
            {
                MRUKNativeFuncs.ConfigureTrackers(0);
            }
            _configureTrackersTask = null;
            _lastRequestedConfiguration = TrackerConfiguration = default;
            _nextTrackerConfigurationTime = TimeSpan.Zero;
        }

        private async void ConfigureTrackerAndLogResult(OVRAnchor.TrackerConfiguration config)
        {
            Debug.Assert(_configureTrackersTask == null);

            uint trackableMask = 0;
            if (config.KeyboardTrackingEnabled)
            {
                trackableMask |= (uint)MRUKNativeFuncs.MrukTrackableType.Keyboard;
            }

            if (config.QRCodeTrackingEnabled)
            {
                trackableMask |= (uint)MRUKNativeFuncs.MrukTrackableType.Qrcode;
            }

            _configureTrackersTask = OVRTask.Create<MRUKNativeFuncs.MrukResult>(Guid.NewGuid());
            MRUKNativeFuncs.ConfigureTrackers(trackableMask);

            var result = await _configureTrackersTask.Value;

            if (config.QRCodeTrackingEnabled)
            {
                var unifiedEvent = new UnifiedEventData(TelemetryConstants.EventName.StartMarkerTracker);
                unifiedEvent.result = result == MRUKNativeFuncs.MrukResult.Success ? UnifiedEventResult.SUCCESS : UnifiedEventResult.FAIL;
                unifiedEvent.SendMRUKEvent();
            }

            if (config.KeyboardTrackingEnabled)
            {
                var unifiedEvent = new UnifiedEventData(TelemetryConstants.EventName.StartKeyboardTracker);
                unifiedEvent.result = result == MRUKNativeFuncs.MrukResult.Success ? UnifiedEventResult.SUCCESS : UnifiedEventResult.FAIL;
                unifiedEvent.SendMRUKEvent();
            }

            if (result == MRUKNativeFuncs.MrukResult.Success)
            {
                Debug.Log($"Configured anchor trackers: {config}");
            }
            else
            {
                Debug.LogWarning($"{result}: Unable to fully satisfy requested tracker configuration. Requested={config}.");
            }

            if (this)
            {
                _configureTrackersTask = null;
                if (result == MRUKNativeFuncs.MrukResult.Success)
                {
                    TrackerConfiguration = config;
                }
            }
        }

        private void HandleTrackableAdded(ref MRUKNativeFuncs.MrukTrackable trackable)
        {
            var trackableKey = GetTrackableKey(ref trackable);
            if (_trackables.ContainsKey(trackableKey))
            {
                Debug.LogWarning($"{nameof(HandleTrackableAdded)}: Trackable {trackableKey} of type {trackable.trackableType} was previously added. Ignoring.");
                return;
            }

            var go = new GameObject($"Trackable({trackable.trackableType}) {trackableKey}");
            var component = go.AddComponent<MRUKTrackable>();
            _trackables.Add(GetTrackableKey(ref trackable), component);

            UpdateTrackableProperties(component, ref trackable);

            // Notify user
            SceneSettings.TrackableAdded.Invoke(component);
        }

        private void HandleTrackableUpdated(ref MRUKNativeFuncs.MrukTrackable trackable)
        {
            if (_trackables.TryGetValue(GetTrackableKey(ref trackable), out var component) && component)
            {
                UpdateTrackableProperties(component, ref trackable);
            }
        }

        private void HandleTrackableRemoved(ref MRUKNativeFuncs.MrukTrackable trackable)
        {
            if (_trackables.Remove(GetTrackableKey(ref trackable), out var component) && component)
            {
                component.IsTracked = false;
                SceneSettings.TrackableRemoved.Invoke(component);
            }
        }
    }
}
