// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<bd7981e86895efb5260310e12d4631bf>>
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

    /// An GroupPresenceJoinIntent represents a user's intent to join a group
    /// presence which is user's presence to be at a Destination and session.
    /// People with same session id are considered to be co-present together. Every
    /// combination of destination api name, lobby session id and match session id
    /// can uniquely identify a destination.
    [Preserve]
    public class GroupPresenceJoinIntent
    {
        /// An opaque string provided by the developer to help bringing users together.
        [JsonProperty("deeplink_message")]
        [Preserve]
        public string DeeplinkMessage { get; set; }
        /// Destination.ApiName is the unique API Name that refers to an in-app
        /// destination.
        [JsonProperty("destination_api_name")]
        [Preserve]
        public string DestinationApiName { get; set; }
        /// This is a session ID that represents a closer group/squad/party of users.
        /// It is expected that all users with the same lobby session id can see or
        /// hear each other. Users with the same lobby session id in their group
        /// presence will show up in the roster and will show up as "Recently Played
        /// With" for future invites if they aren't already Oculus friends.
        [JsonProperty("lobby_session_id")]
        [Preserve]
        public string LobbySessionId { get; set; }
        /// This is a session that represents all the users that are playing a specific
        /// instance of a map, game mode, round, etc. This can include users from
        /// multiple different lobbies that joined together and the users may or may
        /// not remain together after the match is over. Users with the same match
        /// session id in their group presence will not show up in the Roster, but will
        /// show up as "Recently Played with" for future invites.
        [JsonProperty("match_session_id")]
        [Preserve]
        public string MatchSessionId { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
