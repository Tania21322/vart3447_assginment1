// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<dc6666c38d1bb840137efa84337f38df>>
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

    ///
    /// The API has been deprecated. Please use LaunchReportFlowResult instead.
    [Preserve]
    public class UserReportID
    {
        /// Whether the viewer chose to cancel the report flow.
        [JsonProperty("did_cancel")]
        [Preserve]
        public bool DidCancel { get; set; }
        /// FIXME: Add doc comment for this field!
        [JsonProperty("user_report_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 ID { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
