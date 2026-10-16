// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<dac5b1ac7b3ded09463c8f285a16e478>>
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

    /// A single user can have multiple sdk accounts associated with it. SDK
    /// accounts represent the oculus user and x-accounts that are linked to the
    /// particular user. Retrieve the SDK accounts by using, Users.GetSdkAccounts.
    [Preserve]
    public class SdkAccount
    {
        /// The specific type of account that this sdk account represents. The type can
        /// be one of the following: SdkAccountType.Unknown SdkAccountType.Oculus
        /// SdkAccountType.FacebookGameroom
        [JsonProperty("account_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<SdkAccountType>))]
        [Preserve]
        public SdkAccountType AccountType { get; set; }
        /// The ID of the user, User.Id, of the sdk account. This is a unique value for
        /// every User.
        [JsonProperty("user_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 UserId { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of SdkAccount elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class SdkAccountList : DeserializableList<SdkAccount>
    {
    }
}
