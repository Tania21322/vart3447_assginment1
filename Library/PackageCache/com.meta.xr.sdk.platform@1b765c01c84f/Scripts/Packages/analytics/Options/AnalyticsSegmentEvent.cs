// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<ee7a5ae1f7aa509e6e3673cf1252b416>>
 */

using System;
using System.Collections;
using Oculus.Platform.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Scripting;

namespace Oculus.Platform
{
    /// An analytics Segment event is the foundation of in-content analytics. All
    /// advanced analytics (e.g. 3D heatmap, fine-tune design, object interaction)
    /// can benefit from having a high quality segment_events to stitch together
    /// other event logging. Segments represent the most granular and symbolic
    /// content experience that users can engage with.
    [Serializable]
    [Preserve]
    public class AnalyticsSegmentEvent {
        /// The unix timestamp in milliseconds since epoch
        [JsonProperty("timestamp")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        [Preserve]
        public DateTime Timestamp;
        /// A unique identifier of your segment ID (recommend UUID v4). The frontend
        /// oculus app code should be responsible for tracking this and make sure all
        /// session events happening between 'start' and 'end' session event type have
        /// the same session id.
        [JsonProperty("segment_id")]
        [Preserve]
        public string SegmentId;
        /// A customizable name represents the meaning of this segment. Recommend using
        /// snake case.
        [JsonProperty("segment_name")]
        [Preserve]
        public string SegmentName;
        /// The pre-defined segment type for this segment.
        [JsonProperty("segment_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<AnalyticsSegmentType>))]
        [Preserve]
        public AnalyticsSegmentType SegmentType;
        /// The pre-defined event type for this segment.
        [JsonProperty("event_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<AnalyticsSegmentEventType>))]
        [Preserve]
        public AnalyticsSegmentEventType EventType;
        /// The duration (in seconds) up to this segment since the start of the current
        /// app segment, since segment start and before segment end.
        [JsonProperty("duration")]
        [Preserve]
        public float Duration;
        /// The position of this event in the sequence of events you sent to the
        /// endpoint. This number should be strictly increasing event by event, call by
        /// call
        [JsonProperty("sequence_number")]
        [Preserve]
        public int SequenceNumber;
        /// The app's build version string
        [JsonProperty("build_version")]
        [Preserve]
        public string BuildVersion;
        /// The pre-defined type of the session event
        [JsonProperty("settings")]
        [Preserve]
        public AnalyticsSettings Settings;
        /// Position value of player in 3D scene
        [JsonProperty("position")]
        [Preserve]
        public AnalyticsPosition Position;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public AnalyticsSegmentEvent()
        {
        }
        
        [Preserve]
        public void SetTimestamp(DateTime value)
        {
            Timestamp = value;
        }
        
        [Preserve]
        public void SetSegmentId(string value)
        {
            SegmentId = value;
        }
        
        [Preserve]
        public void SetSegmentName(string value)
        {
            SegmentName = value;
        }
        
        [Preserve]
        public void SetSegmentType(AnalyticsSegmentType value)
        {
            SegmentType = value;
        }
        
        [Preserve]
        public void SetEventType(AnalyticsSegmentEventType value)
        {
            EventType = value;
        }
        
        [Preserve]
        public void SetDuration(float value)
        {
            Duration = value;
        }
        
        [Preserve]
        public void SetSequenceNumber(int value)
        {
            SequenceNumber = value;
        }
        
        [Preserve]
        public void SetBuildVersion(string value)
        {
            BuildVersion = value;
        }
        
        [Preserve]
        public void SetSettings(AnalyticsSettings value)
        {
            Settings = value;
        }
        
        [Preserve]
        public void SetPosition(AnalyticsPosition value)
        {
            Position = value;
        }
        
        ~AnalyticsSegmentEvent()
        {
        }
    }
}
