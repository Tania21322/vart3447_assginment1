// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<49c8e58c58ac21a732f31d83403429f7>>
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

    /// This is the class that represents the individual users who use your
    /// application. Use the User object to retrieve information about your users,
    /// help them interact with your application, and customize their experience.
    /// You can query for information about a particular user using their user id,
    /// User.Id. To learn more, read our
    /// [docs](https://developer.oculus.com/documentation/unity/ps-presence/#user-and-friends)
    /// about users. Note: You must complete a Data Use Checkup(DUC) in order to
    /// gain access to user platform features. Read more about DUC
    /// [here](https://developer.oculus.com/resources/publish-data-use/).
    [Preserve]
    public class User
    {
        /// A potentially non unique displayable name chosen by the user. Could also be
        /// the same as the oculus_ID. This is often the name shown to other users.
        [JsonProperty("display_name")]
        [Preserve]
        public string DisplayName { get; set; }
        /// The ID of the user, User.Id. This is a unique value for every individual
        /// user.
        [JsonProperty("id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 ID { get; set; }
        /// The url of the profile picture that is chosen by the user. Retrieve this
        /// url by using Users.GetLoggedInUser.
        [JsonProperty("profile_url")]
        [Preserve]
        public string ImageURL { get; set; }
        /// Managed account info, ManagedInfo, for the user which contains further
        /// metadata that is only available if the user is a Meta Managed Account(MMA).
        /// There must be user consent via dialog during installation, your app must
        /// have DUC enabled, and the app must be admin-approved.
        [JsonProperty("managed_info")]
        // May be null. Check before using.
        [Preserve]
        public ManagedInfo ManagedInfoOptional { get; set; }
        /// The oculus ID of the user. This is used across the developer dashboard and
        /// is unique to oculus.
        [JsonProperty("alias")]
        [Preserve]
        public string OculusID { get; set; }
        /// Human readable string of what the user is currently doing. Not intended to
        /// be parsed as it might change at anytime or be translated.
        [JsonProperty("presence")]
        [Preserve]
        public string Presence { get; set; }
        /// Intended to be parsed and used to deeplink to parts of the app. Read more
        /// about deeplinking
        /// [here](https://developer.oculus.com/documentation/{{platform_doc_path}}/ps-
        /// deep-linking/).
        [JsonProperty("presence_deeplink_message")]
        [Preserve]
        public string PresenceDeeplinkMessage { get; set; }
        /// If provided, this is the unique API Name that refers to the Destination
        /// this user is currently at in the app. Read more about destinations
        /// [here](https://developer.oculus.com/documentation/{{platform_doc_path}}/ps-
        /// destinations-overview/)
        [JsonProperty("presence_destination_api_name")]
        [Preserve]
        public string PresenceDestinationApiName { get; set; }
        /// If provided, the lobby session this user is currently at in the
        /// application. If the ApplicationInvite is generated from rich presence, the
        /// lobby session id will be auto populated by calling
        /// User.PresenceLobbySessionId from the inviters' viewer context.
        [JsonProperty("presence_lobby_session_id")]
        [Preserve]
        public string PresenceLobbySessionId { get; set; }
        /// If provided, the match session this user is currently at in the
        /// application. If the ApplicationInvite is generated from rich presence, the
        /// match session id will be auto populated by calling
        /// User.PresenceMatchSessionId from the inviters' viewer context.
        [JsonProperty("presence_match_session_id")]
        [Preserve]
        public string PresenceMatchSessionId { get; set; }
        /// An enum value for the different statuses representing what the user is
        /// currently doing. The different statuses can be UserPresenceStatus.Unknown,
        /// UserPresenceStatus.Offline, UserPresenceStatus.Online.
        [JsonProperty("presence_status")]
        [Preserve]
        public UserPresenceStatus PresenceStatus { get; set; }
        /// The url of the smaller/secondary profile picture that is chosen by the
        /// user. Retrieve this url by using User.SmallImageUrl.
        [JsonProperty("profile_url_small")]
        [Preserve]
        public string SmallImageUrl { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of User elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class UserList : DeserializableList<User>
    {
    }
}
