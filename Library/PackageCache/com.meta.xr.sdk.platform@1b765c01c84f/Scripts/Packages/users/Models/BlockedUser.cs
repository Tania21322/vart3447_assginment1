// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<cebac8a341951f25af0a6ae46f5a3f60>>
 */

namespace Oculus.Platform.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using UnityEngine.Scripting;

    /// It contains an array of users who have been blocked by the logged in user.
    /// You can't follow, be followed, invited, or searched by a blocked user. It
    /// can be retrieved using Users.GetBlockedUsers.
    [Preserve]
    public class BlockedUser
    {
        /// It represents the user ID that has been blocked by the logged in user. It
        /// is a type of ID and can be retrieved using User.Id.
        [JsonProperty("id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 Id { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of BlockedUser elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class BlockedUserList : DeserializableList<BlockedUser>
    {
    }
}
