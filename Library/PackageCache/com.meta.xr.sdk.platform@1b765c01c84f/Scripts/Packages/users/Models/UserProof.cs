// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<2325292690de53453fc19baab64878ed>>
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

    /// This object is retrieved from the Users.GetUserProof request and will
    /// contain a nonce that is used to verify the identity of the User. Read more
    /// about user verification in our
    /// [User Verification guide](https://developer.oculus.com/documentation/unity/ps-ownership/#integrate-user-verification)
    /// NOTE: The nonce is only good for one check and then it is invalidated.
    [Preserve]
    public class UserProof
    {
        /// A string that is returned from the client that is used to verify the
        /// identity of the User. The nonce can be used with the meta account to the
        /// `https://graph.oculus.com/user_nonce_validate` endpoint to verify identity.
        [JsonProperty("nonce")]
        [Preserve]
        public string Value { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
