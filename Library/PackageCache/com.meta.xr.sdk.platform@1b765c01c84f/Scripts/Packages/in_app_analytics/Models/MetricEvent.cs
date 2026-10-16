// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<9b46a5b8e7652173a4b7a7631f5c415a>>
 */

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using UnityEngine.Scripting;

    /// Represents a metric event in the in-app analytics system.
    [Preserve]
    public class MetricEvent
    {
        /// The unique identifier for the segment associated with this metric.
        [JsonProperty("seg_id")]
        [Preserve]
        public string SegId { get; set; }
        /// The timestamp of the metric event.
        [JsonProperty("timestamp")]
        [Preserve]
        public long Timestamp { get; set; }
        /// The sequence number of the metric event.
        [JsonProperty("seq_num")]
        [Preserve]
        public long SeqNum { get; set; }
        /// The type of metric being recorded.
        [JsonProperty("metric_type")]
        [Preserve]
        public MetricType MetricType { get; set; }
        /// The name of the metric.
        [JsonProperty("metric_name")]
        [Preserve]
        public string MetricName { get; set; }
        /// The value of the metric.
        [JsonProperty("value")]
        [Preserve]
        public float Value { get; set; }
        /// The 3D position associated with this metric event.
        [JsonProperty("position")]
        // May be null. Check before using.
        [Preserve]
        public Position PositionOptional { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
