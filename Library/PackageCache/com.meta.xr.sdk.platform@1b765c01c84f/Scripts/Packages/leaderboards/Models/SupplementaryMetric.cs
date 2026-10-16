// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<33d3de3835f7eff2c326b2850605cff6>>
 */

namespace Oculus.Platform.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using UnityEngine.Scripting;

    /// This is a supplemental piece of data that is used for a single write to
    /// leaderboard entries. This metric is used for tiebreaker scenarios. You can
    /// make such an entry by using Leaderboards.WriteEntryWithSupplementaryMetric
    [Preserve]
    public class SupplementaryMetric
    {
        /// The ID of the leaderboard that this supplementary metric belongs to. This
        /// is the unique value for every Leaderboard.
        [JsonProperty("id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 ID { get; set; }
        /// This is the metric that is used to determine tiebreaks.
        [JsonProperty("metric")]
        [Preserve]
        public long Metric { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
