// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<98eecc7a6ac7c90baa41b2248d67c5a8>>
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

    /// A video recording evidence that can be used to collect video evidence when
    /// reporting abusive behavior or content within a platform. More details are
    /// available in our
    /// [User Reporting Service Virtual Reality Check guideline](https://developer.oculus.com/resources/reporting-service/)
    [Preserve]
    public class AbuseReportRecording
    {
        /// A unique UUID associated with the Abuse Report recording. It can be
        /// retrieved using LaunchReportFlowResult.UserReportId
        [JsonProperty("recording_uuid")]
        [Preserve]
        public string RecordingUuid { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
