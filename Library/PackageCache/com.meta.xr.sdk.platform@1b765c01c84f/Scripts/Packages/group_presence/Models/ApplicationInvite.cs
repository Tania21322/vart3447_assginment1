// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<b3095b936e663672b6678cf762e9184f>>
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

    /// An Application Invite contains information about the invite, including the
    /// application ID, destination, activity status, the lobby session id, the
    /// match session id, and recipient's user id or user alias. If the invite is
    /// generated from rich presence, the lobby/match session id will be auto
    /// populated by calling User.PresenceLobbySessionId and
    /// User.PresenceMatchSessionId from the inviter's viewer context.
    [Preserve]
    public class ApplicationInvite
    {
        /// The destination to which the recipient is invited.
        [JsonProperty("destination")]
        // May be null. Check before using.
        [Preserve]
        public Destination DestinationOptional { get; set; }
        /// The ID of the application invite.
        [JsonProperty("id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 ID { get; set; }
        /// A boolean value indicating whether the invite is still active or not.
        [JsonProperty("is_active")]
        [Preserve]
        public bool IsActive { get; set; }
        /// The lobby session id to which the recipient is invited. You can retrieve
        /// this by calling User.PresenceLobbySessionId from the inviter's viewer
        /// context.
        [JsonProperty("lobby_session_id")]
        [Preserve]
        public string LobbySessionId { get; set; }
        /// The match session id to which the recipient is invited. You can retrieve
        /// this by calling User.PresenceMatchSessionId from the inviter's viewer
        /// context.
        [JsonProperty("match_session_id")]
        [Preserve]
        public string MatchSessionId { get; set; }
        /// The recipient's user information, such as their ID and alias. You can get
        /// the ID of an user by calling User.Id.
        [JsonProperty("recipient")]
        // May be null. Check before using.
        [Preserve]
        public User RecipientOptional { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of ApplicationInvite elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class ApplicationInviteList : DeserializableList<ApplicationInvite>
    {
    }
}
