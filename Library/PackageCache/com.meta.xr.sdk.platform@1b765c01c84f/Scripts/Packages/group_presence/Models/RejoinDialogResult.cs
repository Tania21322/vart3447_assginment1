// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<c6259f164aa476e297009dd8e02a4261>>
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

    /// A boolean that indicates the result of GroupPresence.LaunchRejoinDialog.
    /// 'True' indicates that the application will rejoin the dialog, otherwise the
    /// application will not rejoin the dialog. Read more about the
    /// [rejoin dialog](https://developer.oculus.com/documentation/unity/ps-rejoin/#rejoin-apis).
    [Preserve]
    public class RejoinDialogResult
    {
        /// A boolean for if the user has decided to rejoin. This is used in
        /// GroupPresence.LaunchRejoinDialog.
        [JsonProperty("rejoin_selected")]
        [Preserve]
        public bool RejoinSelected { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
