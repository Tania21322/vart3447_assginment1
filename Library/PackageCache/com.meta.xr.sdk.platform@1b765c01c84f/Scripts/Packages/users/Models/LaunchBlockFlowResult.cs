// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<a85197e21180ad8b8de7742db04cd20f>>
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

    /// It's used to block a user. Results of the launched block dialog including 
    /// whether the user was successfully blocked and whether the viewer canceled
    /// the block flow. It can be retrieved using Users.LaunchBlockFlow
    [Preserve]
    public class LaunchBlockFlowResult
    {
        /// A `boolean` indicates whether the viewer successfully blocked the user.
        /// Learn more about [blocking
        /// users](https://developer.oculus.com/documentation/{{platform_doc_path}}/ps-
        /// blockingsdk/) from our website.
        [JsonProperty("did_block")]
        [Preserve]
        public bool DidBlock { get; set; }
        /// A `boolean` indicates whether the viewer chose to cancel the block flow. It
        /// will be 'true' if the viewer canceled 'Block' from the modal.
        [JsonProperty("did_cancel")]
        [Preserve]
        public bool DidCancel { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
