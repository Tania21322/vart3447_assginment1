// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<a266d86c65420f976540336f80a3520f>>
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

    /// Represents a linked account that is associated with the User's account in
    /// the system. It provides a way to store and manage information about linked
    /// accounts in the system, allowing users to easily access and manage their
    /// data or services from multiple platforms in one place
    [Preserve]
    public class LinkedAccount
    {
        /// Access token of the linked account. This token is used to authenticate the
        /// user on the service provider's platform and grant access to their data or
        /// services.
        [JsonProperty("access_token")]
        [Preserve]
        public string AccessToken { get; set; }
        /// Service provider with which the linked account is associated. There are
        /// several possible service providers that can be found in ServiceProvider.
        [JsonProperty("service_provider")]
        [JsonConverter(typeof(EnumDescriptionConverter<ServiceProvider>))]
        [Preserve]
        public ServiceProvider ServiceProvider { get; set; }
        /// A unique identifier represents the user ID of the linked account. It can be
        /// retrieved using User.Id
        [JsonProperty("user_id")]
        [Preserve]
        public string UserId { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of LinkedAccount elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class LinkedAccountList : DeserializableList<LinkedAccount>
    {
    }
}
