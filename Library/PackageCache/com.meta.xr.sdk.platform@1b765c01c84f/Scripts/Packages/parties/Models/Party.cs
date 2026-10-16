// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<ce2c545d7bf10ee404b8695abf514b35>>
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

    /// Parties allow users to start a voice chat with other members of the party.
    /// Party voice chats persist across apps in VR and users can continue to
    /// interact while navigating between apps. You can use Parties.GetCurrent to
    /// load the party the current user is in.
    [Preserve]
    public class Party
    {
        /// A unique identifier of this party.
        [JsonProperty("id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 ID { get; set; }
        /// An array of users who are invited to this party. These users are not a part
        /// of the party yet but have been invited.
        [JsonProperty("invited_users")]
        // May be null. Check before using.
        [Preserve]
        public UserList InvitedUsersOptional { get; set; }
        /// The user who initialized this party. It's also the first user who joined
        /// the party. The leader can invite and kick other users.
        [JsonProperty("leader")]
        // May be null. Check before using.
        [Preserve]
        public User LeaderOptional { get; set; }
        /// An array that contains the users who are currently in this party. These
        /// users will remain in the party while navigating between apps. Each user in
        /// the party will get the update by Parties.PartyUpdate which is intended to
        /// update the user on various actions that are occurring in the party.
        [JsonProperty("users")]
        // May be null. Check before using.
        [Preserve]
        public UserList UsersOptional { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
