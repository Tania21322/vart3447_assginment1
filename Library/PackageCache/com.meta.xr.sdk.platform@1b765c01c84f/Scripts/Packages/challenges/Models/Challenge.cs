// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<c010f7547bdec62e392d6ab36dc1ae17>>
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

    /// Challenges leverage Destination and Group Presence to create shareable
    /// links that allow players to compete directly based on score. Challenges can
    /// be ranked by highest or lowest scores within a time period. Any application
    /// that uses Leaderboards gets Challenges for free. It appears in the
    /// Scoreboards UI. Players can create Challenges and send invites through the
    /// Challenges app.
    [Preserve]
    public class Challenge
    {
        /// An enum that specifies if this challenge was created by a user
        /// (ChallengeCreationType.UserCreated)) or the app developer .
        [JsonProperty("creation_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<ChallengeCreationType>))]
        [Preserve]
        public ChallengeCreationType CreationType { get; set; }
        /// A displayable string of the challenge's description.
        [JsonProperty("description")]
        [Preserve]
        public string Description { get; set; }
        /// The timestamp when this challenge ends. You can retrieve this field from
        /// the response of the challenge creation request.
        [JsonProperty("end_date")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        [Preserve]
        public DateTime EndDate { get; set; }
        /// The ID of the challenge. This is an unique string that the application will
        /// refer to this challenge in your app.
        [JsonProperty("id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 ID { get; set; }
        /// Users that are invited to this challenge.
        [JsonProperty("invited_users")]
        // May be null. Check before using.
        [Preserve]
        public UserList InvitedUsersOptional { get; set; }
        /// The Leaderboard associated with this challenge. You can retrieve the
        /// leaderboard ID from the response of the challenge creation request.
        [JsonProperty("leaderboard")]
        [Preserve]
        public Leaderboard Leaderboard { get; set; }
        /// Users that have participated in this challenge.
        [JsonProperty("participants")]
        // May be null. Check before using.
        [Preserve]
        public UserList ParticipantsOptional { get; set; }
        /// The timestamp when this challenge begins. You can retrieve this field from
        /// the response of the challenge creation request.
        [JsonProperty("start_date")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        [Preserve]
        public DateTime StartDate { get; set; }
        /// A displayable string of the challenge's title.
        [JsonProperty("title")]
        [Preserve]
        public string Title { get; set; }
        /// An enum that specifies who can see and participate in this challenge.
        /// ChallengeVisibility.InviteOnly - Only those invited can participate in it.
        /// Everyone can see it. ChallengeVisibility.Public - Everyone can participate
        /// and see this challenge. ChallengeVisibility.Private - Only those invited
        /// can participate and see this challenge.
        [JsonProperty("visibility")]
        [JsonConverter(typeof(EnumDescriptionConverter<ChallengeVisibility>))]
        [Preserve]
        public ChallengeVisibility Visibility { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of Challenge elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class ChallengeList : DeserializableList<Challenge>
    {
    }
}
