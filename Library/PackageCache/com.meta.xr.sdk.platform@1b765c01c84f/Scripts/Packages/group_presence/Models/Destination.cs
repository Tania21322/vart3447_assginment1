// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<fe80830d8fc0f221d8185ae531870f5c>>
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

    /// The destination represents where the user would like to go to in the app.
    /// It's usually associated with a travel or an invitation. Each destination
    /// has Destination.ApiName, Destination.DisplayName,
    /// Destination.DeeplinkMessage and Destination.ShareableUri link. Please refer
    /// to member data documentation for details.
    [Preserve]
    public class Destination
    {
        /// You can pass it into GroupPresenceOptions.DestinationApiName) when calling
        /// @internalLink to set this user's group presence.
        [JsonProperty("api_name")]
        [Preserve]
        public string ApiName { get; set; }
        /// The information that will be in LaunchDetails.DeeplinkMessage when a user
        /// enters via a deeplink. Alternatively will be in
        /// User.PresenceDeeplinkMessage if the group presence is set for the user.
        [JsonProperty("deeplink_message")]
        [Preserve]
        public string DeeplinkMessage { get; set; }
        /// A displayable string of the destination name and it can be retrieved with
        /// Destination.DisplayName.
        [JsonProperty("display_name")]
        [Preserve]
        public string DisplayName { get; set; }
        /// A URI that allows the user to deeplink directly to this destination
        [JsonProperty("shareable_uri")]
        [Preserve]
        public string ShareableUri { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of Destination elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class DestinationList : DeserializableList<Destination>
    {
    }
}
