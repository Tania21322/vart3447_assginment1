// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<e6adc2a5ab2eff398f6b02a86744da65>>
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

    /// The notification about updating a party status information to a
    /// participating user. Party Notifications are intended to update the user on
    /// various actions that are occurring in the user's party. It can be retrieved
    /// using Parties.PartyUpdate.
    [Preserve]
    public class PartyUpdateNotification
    {
        /// An enum that specifies the type of action related to the party and user
        /// that this notification holds. PartyUpdateAction.Join: The user joined the
        /// party. PartyUpdateAction.Leave: The user left the party.
        /// PartyUpdateAction.Invite: The user was invited to the party.
        /// PartyUpdateAction.Uninvite: The user was uninvited to the party.
        [JsonProperty("action")]
        [JsonConverter(typeof(EnumDescriptionConverter<PartyUpdateAction>))]
        [Preserve]
        public PartyUpdateAction Action { get; set; }
        /// The ID of the party that will be updated. This can be retrieved with
        /// PartyID.
        [JsonProperty("party_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 PartyId { get; set; }
        /// The id of the User.Id for this user.
        [JsonProperty("sender_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 SenderId { get; set; }
        /// A timestamp denoting when the party action occurred that this status update
        /// notification refers to.
        [JsonProperty("update_timestamp")]
        [Preserve]
        public string UpdateTimestamp { get; set; }
        /// The alias of the User whose party status has changed. This is an alias that
        /// is set by the user.
        [JsonProperty("user_alias")]
        [Preserve]
        public string UserAlias { get; set; }
        /// The ID of the user, User.Id, whose party status has changed. This is a
        /// unique value for every User.
        [JsonProperty("user_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 UserId { get; set; }
        /// The displayable name of the User.DisplayName for this user.
        [JsonProperty("user_name")]
        [Preserve]
        public string UserName { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
