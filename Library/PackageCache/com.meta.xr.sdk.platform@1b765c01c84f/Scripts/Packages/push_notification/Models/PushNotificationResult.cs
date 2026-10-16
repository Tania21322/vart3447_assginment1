// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<6eab57e3cfef04e48ea5093ec7a1d97e>>
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

    /// A PushNotificationResult represents the outcome of a user registering for
    /// third-party (3P) notifications. This object contains essential information
    /// about the registered notification, which can be used to send push
    /// notifications to the user. It can be retrieved using
    /// PushNotification.Register
    [Preserve]
    public class PushNotificationResult
    {
        /// The registered notification id is a type of string which you can push
        /// notification to.
        [JsonProperty("id")]
        [Preserve]
        public string Id { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
