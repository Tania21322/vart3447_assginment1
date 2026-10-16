// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<4fccc7acc58a5fff0ca1cbb4d4f1588e>>
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

    /// A challenge entry object contains information about an individual entry
    /// within a challenge such as the user who made the entry, the score achieved,
    /// and other relevant details. It's the array element type of
    /// ChallengeEntryArray and can be retrieved using Challenges.GetEntries.
    [Preserve]
    public class ChallengeEntry
    {
        /// A displayable score for this challenge entry. The score is formatted with
        /// thousands separators and the relevant units are appended based on the
        /// associated leaderboard's score type.
        [JsonProperty("display_score")]
        [Preserve]
        public string DisplayScore { get; set; }
        /// A 2KB custom data field that is associated with the challenge entry.
        [JsonProperty("extra_data_base64")]
        [Preserve]
        public byte[] ExtraData { get; set; }
        /// The unique identifier of this challenge entry which can be used by
        /// Challenges.GetEntriesByIds and Challenges.GetEntries.
        [JsonProperty("id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 ID { get; set; }
        /// Challenges can be ranked by highest or lowest scores within a time period.
        /// This indicates the position of this challenge entry.
        [JsonProperty("rank")]
        [Preserve]
        public int Rank { get; set; }
        /// The raw underlying value of the challenge entry score. It is a type of
        /// string that is returned by a long integer.
        [JsonProperty("score")]
        [Preserve]
        public long Score { get; set; }
        /// The timestamp of the creation of this entry in the challenge.
        [JsonProperty("timestamp")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        [Preserve]
        public DateTime Timestamp { get; set; }
        /// The user corresponding to this entry within the challenge.
        [JsonProperty("user")]
        [Preserve]
        public User User { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of ChallengeEntry elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class ChallengeEntryList : DeserializableList<ChallengeEntry>
    {
    }
}
