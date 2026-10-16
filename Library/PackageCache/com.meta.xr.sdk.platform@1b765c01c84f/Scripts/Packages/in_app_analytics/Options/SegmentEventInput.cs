// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<ae0ade5a29ab67b88710de261762ec7b>>
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
    /// Input type for queuing a segment event for batch processing.
    [Serializable]
    [Preserve]
    public class SegmentEventInput {
        /// The unique identifier for the segment.
        [JsonProperty("seg_id")]
        [Preserve]
        public string SegId = null;
        /// The timestamp of the segment event.
        [JsonProperty("timestamp")]
        [Preserve]
        public long Timestamp;
        /// The sequence number of the segment event.
        [JsonProperty("seq_num")]
        [Preserve]
        public long SeqNum;
        /// The type of the segment.
        [JsonProperty("seg_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<SegmentType>))]
        [Preserve]
        public SegmentType SegType;
        /// The unique identifier for the match associated with this segment.
        [JsonProperty("match_id")]
        [Preserve]
        public string MatchId = null;
        /// The type of event within the segment.
        [JsonProperty("event_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<SegmentEventType>))]
        [Preserve]
        public SegmentEventType EventType;
        /// The 3D position associated with this segment event.
        [JsonProperty("position")]
        [Preserve]
        public PositionInput Position;
        /// The settings associated with this segment event.
        [JsonProperty("settings")]
        [Preserve]
        public SegmentSettingsInput Settings;
        /// The duration of the segment in seconds.
        [JsonProperty("duration_s")]
        [Preserve]
        public float DurationS;
        /// The name of the segment.
        [JsonProperty("seg_name")]
        [Preserve]
        public string SegName;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public SegmentEventInput()
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
        public void SetSegType(SegmentType value)
        {
            SegType = value;
        }
        
        [Preserve]
        public void SetMatchId(string value)
        {
            MatchId = value;
        }
        
        [Preserve]
        public void SetEventType(SegmentEventType value)
        {
            EventType = value;
        }
        
        [Preserve]
        public void SetPosition(PositionInput value)
        {
            Position = value;
        }
        
        [Preserve]
        public void SetSettings(SegmentSettingsInput value)
        {
            Settings = value;
        }
        
        [Preserve]
        public void SetDurationS(float value)
        {
            DurationS = value;
        }
        
        [Preserve]
        public void SetSegName(string value)
        {
            SegName = value;
        }
        
        ~SegmentEventInput()
        {
        }
    }
}
