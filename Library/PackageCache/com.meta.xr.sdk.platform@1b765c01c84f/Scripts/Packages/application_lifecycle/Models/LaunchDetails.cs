// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<3e70b88e513e3b56b345d3131c598a42>>
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

    /// Details about the launch of the appplication. It can be used to check if
    /// your app is being launched using App to App Travel. It can be retrieved
    /// using ApplicationLifecycle.GetLaunchDetails.
    [Preserve]
    public class LaunchDetails
    {
        /// An opaque string provided by the developer to help them deeplink to content
        /// on app startup.
        [JsonProperty("deeplink_message")]
        [Preserve]
        public string DeeplinkMessage { get; set; }
        /// If provided, the intended destination the user would like to go to
        [JsonProperty("destination_api_name")]
        [Preserve]
        public string DestinationApiName { get; set; }
        /// A string typically used to distinguish where the deeplink came from. For
        /// instance, a DEEPLINK launch type could be coming from events or rich
        /// presence.
        [JsonProperty("launch_source")]
        [Preserve]
        public string LaunchSource { get; set; }
        /// A LaunchType.Normal - Normal launch from the user's library.
        /// LaunchType.Invite - Launch from the user accepting an invite.
        /// LaunchType.Deeplink) - Launched from a deeplink. This flow is typically
        /// kicked off from @internal_link
        [JsonProperty("type")]
        [JsonConverter(typeof(EnumDescriptionConverter<LaunchType>))]
        [Preserve]
        public LaunchType LaunchType { get; set; }
        /// If provided, the intended lobby the user would like to be in
        [JsonProperty("lobby_session_id")]
        [Preserve]
        public string LobbySessionID { get; set; }
        /// If provided, the intended session the user would like to be in
        [JsonProperty("match_session_id")]
        [Preserve]
        public string MatchSessionID { get; set; }
        /// A unique identifier to keep track of a user going through the deeplinking
        /// flow
        [JsonProperty("tracking_id")]
        [Preserve]
        public string TrackingID { get; set; }
        /// If provided, the intended users the user would like to be with
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
