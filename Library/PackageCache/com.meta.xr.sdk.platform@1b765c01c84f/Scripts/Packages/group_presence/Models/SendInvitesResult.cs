// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<1eb0dfd9218341fb8c3685835002de94>>
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

    /// The result of sending an invite to a user or list of users shows invites
    /// were sent successfully through the resulting array. This is the model
    /// returned in a successful response to the GroupPresence.SendInvites api.
    [Preserve]
    public class SendInvitesResult
    {
        /// The list of invites that was sent through GroupPresence.SendInvites. This
        /// invite list can comprise of friends and recently met users.
        [JsonProperty("invites")]
        [Preserve]
        public ApplicationInviteList Invites { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
