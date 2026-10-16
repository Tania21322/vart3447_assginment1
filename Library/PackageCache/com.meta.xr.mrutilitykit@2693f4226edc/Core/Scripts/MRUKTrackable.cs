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

using Meta.XR.Util;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// A "trackable" is a type of anchor that can be detected and tracked by the runtime.
    /// </summary>
    /// <remarks>
    /// Trackables are instantiated and managed by <see cref="MRUK"/>; you should not add this component
    /// to your own `GameObject`s.
    ///
    /// When <see cref="MRUK"/> detects a new trackable, it will invoke its <see cref="MRUK.MRUKSettings.TrackableAdded"/> event and provide an instance of
    /// <see cref="MRUKTrackable"/> to its subscribers.
    /// </remarks>
    [Feature(Feature.TrackedKeyboard)]
    public sealed class MRUKTrackable : MRUKAnchor
    {
        /// <summary>
        /// This specific type of trackable this <see cref="MRUKTrackable"/> represents.
        /// </summary>
        public OVRAnchor.TrackableType TrackableType { get; internal set; }

        /// <summary>
        /// Whether this trackable is currently considered tracked.
        /// </summary>
        /// <remarks>
        /// A trackable may become temporarily untracked if, for example, it cannot
        /// be seen by the device.
        /// </remarks>
        public bool IsTracked { get; internal set; }

        /// <summary>
        /// The marker's payload as a string.
        /// </summary>
        /// <remarks>
        /// If this trackable is a marker (e.g., a QR Code) and its payload can be interpreted as a string,
        /// use this property to get the payload as a string.
        ///
        /// If this trackable is not a marker, or its payload is not a string, this property is `null`.
        /// </remarks>
        /// <seealso cref="MarkerPayloadBytes"/>
        public string MarkerPayloadString { get; internal set; }

        /// <summary>
        /// The marker's payload as raw bytes.
        /// </summary>
        /// <remarks>
        /// If this trackable is a marker (e.g., a QR Code) use this property to get the payload bytes.
        ///
        /// If this trackable is not a marker, then this property is `null`.
        /// </remarks>
        /// <seealso cref="MarkerPayloadString"/>
        public byte[] MarkerPayloadBytes { get; internal set; }
    }
}
