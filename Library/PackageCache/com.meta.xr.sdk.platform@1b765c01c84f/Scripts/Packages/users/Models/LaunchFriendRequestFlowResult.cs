// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<45440d971dfeeb943834550c1a0e5acb>>
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

    /// Users can initiate follow requests to other users encountered in the app by
    /// launching the process from within the app. After the follow request has
    /// been sent via a deeplinked modal, the viewer is returned to the app. Users
    /// may find this process more convenient than using the Meta Quest mobile app
    /// or returning to Meta Horizon Home to send follow requests since it is less
    /// disruptive to the app experience they are currently focused on. You can
    /// retrieve it using Users.LaunchFriendRequestFlow.
    [Preserve]
    public class LaunchFriendRequestFlowResult
    {
        /// User can choose to cancel the friend request flow after sending it. You can
        /// use this to check whether the viewer chose to cancel the friend request
        /// flow.
        [JsonProperty("did_cancel")]
        [Preserve]
        public bool DidCancel { get; set; }
        /// Whether the viewer successfully sent the friend request.
        [JsonProperty("did_send_request")]
        [Preserve]
        public bool DidSendRequest { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
