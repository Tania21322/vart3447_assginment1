// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<cd1e3a55bf432c6691a5f188a278c2fa>>
 */

using System;
using System.Collections;
using Oculus.Platform.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Scripting;

namespace Oculus.Platform
{
    /// The Abuse Report Options provide a way for developers to customize the
    /// reporting flow and specify the type of content being reported, which can be
    /// either a AbuseReportType.User or an AbuseReportType.Object, helping to
    /// maintain a safe and respectful community within their application.
    [Serializable]
    [Preserve]
    public class AbuseReportOptions {
        /// Set whether or not to show the user selection step. If the reported object
        /// is a user, they can choose to block the reported user from further
        /// interactions within the platform.
        [JsonProperty("prevent_people_chooser")]
        [Preserve]
        public bool PreventPeopleChooser = false;
        /// The intended entity type AbuseReportType.User or an object/content
        /// AbuseReportType.Object.
        [JsonProperty("report_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<AbuseReportType>))]
        [Preserve]
        public AbuseReportType ReportType = AbuseReportType.Unknown;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public AbuseReportOptions()
        {
        }
        
        [Preserve]
        public void SetPreventPeopleChooser(bool value)
        {
            PreventPeopleChooser = value;
        }
        
        [Preserve]
        public void SetReportType(AbuseReportType value)
        {
            ReportType = value;
        }
        
        ~AbuseReportOptions()
        {
        }
    }
}
