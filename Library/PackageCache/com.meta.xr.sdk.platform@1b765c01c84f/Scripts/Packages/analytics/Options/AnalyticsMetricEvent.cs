// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<454db9dcaab5830e29c0a5a1876b92e3>>
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
    /// An Analytics Metric event provide a flexible, versatile and comprehensive
    /// schema that allows the developers to understand the user behavior in-depth.
    /// The metric events can track actions such as jump and explore, progression
    /// such as training and milestone, the store activity such as browse and
    /// purchase, etc.
    [Serializable]
    [Preserve]
    public class AnalyticsMetricEvent {
        /// The unix timestamp in milliseconds since epoch
        [JsonProperty("timestamp")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        [Preserve]
        public DateTime Timestamp;
        /// A unique identifier of your segment ID (recommend UUID v4). This segment id
        /// should stay the same for the segment (all events between its segment's
        /// start and end).
        [JsonProperty("segment_id")]
        [Preserve]
        public string SegmentId;
        /// A customizable name represents the meaning of this metric event. Recommend
        /// using snake case.
        [JsonProperty("metric_name")]
        [Preserve]
        public string MetricName;
        /// The pre-defined metric type for this metric event.
        [JsonProperty("metric_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<AnalyticsMetricType>))]
        [Preserve]
        public AnalyticsMetricType MetricType;
        /// The value of this metric event.
        [JsonProperty("value")]
        [Preserve]
        public float Value;
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
        /// Position value of player in 3D scene
        [JsonProperty("position")]
        [Preserve]
        public AnalyticsPosition Position;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public AnalyticsMetricEvent()
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
        public void SetMetricName(string value)
        {
            MetricName = value;
        }
        
        [Preserve]
        public void SetMetricType(AnalyticsMetricType value)
        {
            MetricType = value;
        }
        
        [Preserve]
        public void SetValue(float value)
        {
            Value = value;
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
        public void SetPosition(AnalyticsPosition value)
        {
            Position = value;
        }
        
        ~AnalyticsMetricEvent()
        {
        }
    }
}
