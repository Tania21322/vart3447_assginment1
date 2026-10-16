// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<2f03c8b7d0aaa9ce69bbc5a8ec679280>>
 */

using System;
using System.Collections;
using Oculus.Platform.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Scripting;

namespace Oculus.Platform
{
    /// The Application option contains additional configuration to be passed in to
    /// Application.LaunchOtherApp and Application.InstallAppUpdateAndRelaunch. It
    /// contains 5 fields ApplicationOptions.DeeplinkMessage,
    /// ApplicationOptions.DestinationApiName, ApplicationOptions.LobbySessionId,
    /// ApplicationOptions.MatchSessionId and ApplicationOptions.RoomId.
    [Serializable]
    [Preserve]
    public class ApplicationOptions {
        /// A message to be passed to a launched app, which can be retrieved with
        /// LaunchDetails.DeeplinkMessage.
        [JsonProperty("deeplink_message")]
        [Preserve]
        public string DeeplinkMessage = "";
        /// If provided, the intended destination to be passed to the launched app,
        /// which can be retrieved with LaunchDetails.DestinationApiName.
        [JsonProperty("destination_api_name")]
        [Preserve]
        public string DestinationApiName = "";
        /// If provided, the intended lobby where the launched app should take the
        /// user. All users with the same lobby_session_id should end up grouped
        /// together in the launched app, which can be retrieved with
        /// LaunchDetails.LobbySessionId.
        [JsonProperty("lobby_session_id")]
        [Preserve]
        public string LobbySessionId = "";
        /// If provided, the intended instance of the destination that a user should be
        /// launched into, which can be retrieved with LaunchDetails.MatchSessionId.
        [JsonProperty("match_session_id")]
        [Preserve]
        public string MatchSessionId = "";
        /// [Deprecated]If provided, the intended room where the launched app should
        /// take the user (all users heading to the same place should have the same
        /// value). A room_id of 0 is INVALID.
        [JsonProperty("room_id")]
        [Preserve]
        public UInt64 RoomId;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public ApplicationOptions()
        {
        }
        
        [Preserve]
        public void SetDeeplinkMessage(string value)
        {
            DeeplinkMessage = value;
        }
        
        [Preserve]
        public void SetDestinationApiName(string value)
        {
            DestinationApiName = value;
        }
        
        [Preserve]
        public void SetLobbySessionId(string value)
        {
            LobbySessionId = value;
        }
        
        [Preserve]
        public void SetMatchSessionId(string value)
        {
            MatchSessionId = value;
        }
        
        [Preserve]
        public void SetRoomId(UInt64 value)
        {
            RoomId = value;
        }
        
        ~ApplicationOptions()
        {
        }
    }
}
