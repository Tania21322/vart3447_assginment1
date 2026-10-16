// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<830850ec6ba5136ddedec29ff8e256b3>>
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

    /// This object is a response from the server that contains information about
    /// the status of the request to update the leaderboard. It can be retrieved
    /// using two different API requests: Leaderboards.WriteEntry and
    /// Leaderboards.WriteEntryWithSupplementaryMetric.
    [Preserve]
    public class LeaderboardUpdateStatus
    {
        /// A `boolean` indicates whether the leaderboard was updated.
        [JsonProperty("did_update")]
        [Preserve]
        public bool DidUpdate { get; set; }
        /// If the leaderboard is updated, this represents the updated challenge IDs.
        /// The updated challenge IDs can be used by Challenges.GetEntries,
        /// Challenges.GetEntriesAfterRank, or Challenges.GetEntriesByIds APIs.
        [JsonProperty("updated_challenge_ids")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64[] UpdatedChallengeIds { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
