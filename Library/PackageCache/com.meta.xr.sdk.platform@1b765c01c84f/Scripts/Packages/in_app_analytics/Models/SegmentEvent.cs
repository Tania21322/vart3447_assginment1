// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<88d9e503716546346950035d70e69a57>>
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

    /// Represents a segment event in the in-app analytics system.
    [Preserve]
    public class SegmentEvent
    {
        /// The unique identifier for the segment.
        [JsonProperty("seg_id")]
        [Preserve]
        public string SegId { get; set; }
        /// The timestamp of the segment event.
        [JsonProperty("timestamp")]
        [Preserve]
        public long Timestamp { get; set; }
        /// The sequence number of the segment event.
        [JsonProperty("seq_num")]
        [Preserve]
        public long SeqNum { get; set; }
        /// The type of the segment.
        [JsonProperty("seg_type")]
        [Preserve]
        public SegmentType SegType { get; set; }
        /// The unique identifier for the match associated with this segment.
        [JsonProperty("match_id")]
        [Preserve]
        public string MatchId { get; set; }
        /// The type of event within the segment.
        [JsonProperty("event_type")]
        [Preserve]
        public SegmentEventType EventType { get; set; }
        /// The 3D position associated with this segment event.
        [JsonProperty("position")]
        // May be null. Check before using.
        [Preserve]
        public Position PositionOptional { get; set; }
        /// The settings associated with this segment event.
        [JsonProperty("settings")]
        // May be null. Check before using.
        [Preserve]
        public SegmentSettings SettingsOptional { get; set; }
        /// The duration of the segment in seconds.
        [JsonProperty("duration_s")]
        [Preserve]
        public float DurationS { get; set; }
        /// The name of the segment.
        [JsonProperty("seg_name")]
        [Preserve]
        public string SegName { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
