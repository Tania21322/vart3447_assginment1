// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<c42b4f7d675207ee4815be6f9b4f8b97>>
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

    /// Leaderboards provide a way for a game to keep track of players and their
    /// scores in relation to other. The leaderboard object contains information
    /// about this leaderboard ID, API name and destination. Lean more about
    /// [leaderboard](https://developer.oculus.com/documentation/unity/ps-leaderboards/)
    /// on our website.
    [Preserve]
    public class Leaderboard
    {
        /// The API name of this leaderboard. This is a unique string that your
        /// application will refer to this leaderboard in your app code.
        [JsonProperty("api_name")]
        [Preserve]
        public string ApiName { get; set; }
        /// An optional deep link Destination, which means when a user clicks on the
        /// leaderboard, they will be taken to this in-app destination.
        [JsonProperty("destination")]
        // May be null. Check before using.
        [Preserve]
        public Destination DestinationOptional { get; set; }
        /// The generated GUID of this leaderboard.
        [JsonProperty("id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 ID { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of Leaderboard elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class LeaderboardList : DeserializableList<Leaderboard>
    {
    }
}
