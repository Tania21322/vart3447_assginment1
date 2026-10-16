// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<e9dc1b661377e83d62763bb5af7d68de>>
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

    /// This holds the result of the launchConsentIfRequired() call.
    [Preserve]
    public class ConsentLaunchResult
    {
        /// Outcome of the launch request.
        [JsonProperty("outcome")]
        [JsonConverter(typeof(EnumDescriptionConverter<ConsentLaunchOutcome>))]
        [Preserve]
        public ConsentLaunchOutcome Outcome { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
