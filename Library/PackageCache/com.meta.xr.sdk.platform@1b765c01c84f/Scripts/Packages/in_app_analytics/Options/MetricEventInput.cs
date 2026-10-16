// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<edb7716895ffd1cf95abceab25ffdfac>>
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
    /// Input type for queuing a metric event for batch processing.
    [Serializable]
    [Preserve]
    public class MetricEventInput {
        /// The unique identifier for the segment associated with this metric.
        [JsonProperty("seg_id")]
        [Preserve]
        public string SegId = null;
        /// The timestamp of the metric event.
        [JsonProperty("timestamp")]
        [Preserve]
        public long Timestamp;
        /// The sequence number of the metric event.
        [JsonProperty("seq_num")]
        [Preserve]
        public long SeqNum;
        /// The type of metric being recorded.
        [JsonProperty("metric_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<MetricType>))]
        [Preserve]
        public MetricType MetricType;
        /// The name of the metric.
        [JsonProperty("metric_name")]
        [Preserve]
        public string MetricName;
        /// The value of the metric.
        [JsonProperty("value")]
        [Preserve]
        public float Value;
        /// The 3D position associated with this metric event.
        [JsonProperty("position")]
        [Preserve]
        public PositionInput Position;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public MetricEventInput()
        {
        }
        
        [Preserve]
        public void SetSegId(string value)
        {
            SegId = value;
        }
        
        [Preserve]
        public void SetTimestamp(long value)
        {
            Timestamp = value;
        }
        
        [Preserve]
        public void SetSeqNum(long value)
        {
            SeqNum = value;
        }
        
        [Preserve]
        public void SetMetricType(MetricType value)
        {
            MetricType = value;
        }
        
        [Preserve]
        public void SetMetricName(string value)
        {
            MetricName = value;
        }
        
        [Preserve]
        public void SetValue(float value)
        {
            Value = value;
        }
        
        [Preserve]
        public void SetPosition(PositionInput value)
        {
            Position = value;
        }
        
        ~MetricEventInput()
        {
        }
    }
}
