// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<3969d92623097b310e6dc850d15f3ec3>>
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

    /// Represents an object that contains boolean indicating whether the user is
    /// eligible to launch the rating and review UI.
    [Preserve]
    public class ApplicationCanViewerRateAndReview
    {
        /// Boolean indicating whether the user is eligible to launch the rating and
        /// review UI.
        [JsonProperty("can_viewer_rate_and_review")]
        [Preserve]
        public bool CanViewerRateAndReview { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
