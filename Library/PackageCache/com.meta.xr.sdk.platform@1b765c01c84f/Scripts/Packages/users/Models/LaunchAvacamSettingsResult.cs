// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<00101d8dcbfe2d0a0e1b0812e91a6bd2>>
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

    /// It is used to launch the Avacam (avatar camera) Settings Activity. This
    /// activity will enable users to modify the appearance of their avatar during
    /// any usage of the Avacam (calls, camera app, e.t.c)..
    [Preserve]
    public class LaunchAvacamSettingsResult
    {
        /// A boolean indicating whether the activity was launched successfuly.
        [JsonProperty("avacam_settings_launched")]
        [Preserve]
        public bool AvacamSettingsLaunched { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
