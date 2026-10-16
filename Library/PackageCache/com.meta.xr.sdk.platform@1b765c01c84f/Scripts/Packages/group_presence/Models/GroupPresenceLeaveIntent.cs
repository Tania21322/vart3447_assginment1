// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<8936b586474ffdfb5fd2782091f1baa5>>
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

    /// An GroupPresenceLeaveIntent represents a user's intent to leave a group
    /// presence which is user's presence to be at a Destination and session.
    /// People with same session id are considered to be co-present together. Every
    /// combination of destination api name, lobby session id and match session id
    /// can uniquely identify a destination.
    [Preserve]
    public class GroupPresenceLeaveIntent
    {
        /// Destination.ApiName is the unique API Name that refers to an in-app
        /// destination.
        [JsonProperty("destination_api_name")]
        [Preserve]
        public string DestinationApiName { get; set; }
        /// This is the match session that the current user wants to leave. A lobby
        /// session is a session ID that represents a closer group/squad/party of
        /// users. It is expected that all users with the same lobby session id can see
        /// or hear each other. Users with the same lobby session id in their group
        /// presence will show up in the roster and will show up as "Recently Played
        /// With" for future invites if they aren't already Oculus friends.
        [JsonProperty("lobby_session_id")]
        [Preserve]
        public string LobbySessionId { get; set; }
        /// This is the match session that the current user wants to leave. A match
        /// session represents all the users that are playing a specific instance of a
        /// map, game mode, round, etc. This can include users from multiple different
        /// lobbies that joined together and the users may or may not remain together
        /// after the match is over.
        [JsonProperty("match_session_id")]
        [Preserve]
        public string MatchSessionId { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
