// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<26e776fdee56f6bc4332adbf67d4ec3b>>
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

    /// Represents the result of a user's interaction with the invite panel, which
    /// is used to send out invitations to other users. It provides a way for
    /// developers to track the results of a user's interaction with the invite
    /// panel, including the list of users who were invited to the session. It can
    /// be retrieved using GroupPresence.InvitationsSent.
    [Preserve]
    public class LaunchInvitePanelFlowResult
    {
        /// A list of User who were invited to the session by the user who interacted
        /// with the invite panel.
        [JsonProperty("invited_users")]
        [Preserve]
        public UserList InvitedUsers { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
