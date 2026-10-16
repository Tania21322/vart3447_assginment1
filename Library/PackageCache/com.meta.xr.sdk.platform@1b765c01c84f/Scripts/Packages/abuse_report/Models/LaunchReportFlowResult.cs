// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<220ade40113716fe0fb58f4750ea5854>>
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

    /// Results of the launched report dialog including resulting report ID and
    /// user action. It can be retrieved using AbuseReport.LaunchAdvancedReportFlow
    /// to handle the result of a report request. Learn more about our
    /// [User Reporting Service](https://developer.oculus.com/resources/reporting-service#faq_856753478660534).
    [Preserve]
    public class LaunchReportFlowResult
    {
        /// A `boolean` which indicates whether the viewer chose to cancel the report
        /// flow before completing it.
        [JsonProperty("did_cancel")]
        [Preserve]
        public bool DidCancel { get; set; }
        /// ID of the report created by the user. It's optional and may not be present
        /// if the user cancelled the report flow. Learn more about the [user reporting
        /// plugin](https://developer.oculus.com/resources/reporting-plugin) in our
        /// website.
        [JsonProperty("user_report_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 UserReportId { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
