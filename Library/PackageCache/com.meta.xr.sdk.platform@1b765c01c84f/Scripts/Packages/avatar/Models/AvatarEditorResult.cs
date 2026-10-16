// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<53b148acb099e8fed576073663386b6f>>
 */

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using UnityEngine.Scripting;

    /// This is the result that can be extracted from message payload when the
    /// avatar editor is launched via a call to Avatar.LaunchAvatarEditor. It
    /// contains information about whether the avatar editor result has
    /// successfully been sent.
    [Preserve]
    public class AvatarEditorResult
    {
        /// This indicates whether the request has been sent successfully. This is an
        /// optional `boolean`. If the boolean field isn't there, it indicates that the
        /// response is an error and will throw an error message.
        [JsonProperty("request_sent")]
        [Preserve]
        public bool RequestSent { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
