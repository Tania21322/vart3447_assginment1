// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<60208013abd1d97b4c32d9f6e5339615>>
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

    /// It's used to unblock a user. Results of the launched unblock dialog
    /// including whether the user was successfully unblocked and whether the
    /// viewer canceled the unblock flow. It can be retrieved using
    /// Users.LaunchUnblockFlow.
    [Preserve]
    public class LaunchUnblockFlowResult
    {
        /// A `boolean` indicates whether the viewer chose to cancel the unblock flow.
        /// It will be 'true' if the viewer canceled 'Unblock' from the modal.
        [JsonProperty("did_cancel")]
        [Preserve]
        public bool DidCancel { get; set; }
        /// A `boolean` indicates whether the viewer successfully unblocked the user.
        /// Learn more about [unblocking
        /// users](https://developer.oculus.com/documentation/{{platform_doc_path}}/ps-
        /// blockingsdk/) from our website.
        [JsonProperty("did_unblock")]
        [Preserve]
        public bool DidUnblock { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
