// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<e3f1da67c188f00a2ec7ced187ced134>>
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

    /// It contains information about the user's invitation to others to join their
    /// current session. It is used to provide feedback to the user about whether
    /// their invitations have been successfully sent. It can be retrieved using
    /// GroupPresence.LaunchInvitePanel.
    [Preserve]
    public class InvitePanelResultInfo
    {
        /// This field indicates whether any invitations have been sent successfully.
        /// It is a boolean value where true means that one or more invites have been
        /// successfully sent, and false indicates that no invites were sent or the
        /// sending process failed.
        [JsonProperty("invites_sent")]
        [Preserve]
        public bool InvitesSent { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
