// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<01be00449446bec3fafb53244bcaebc5>>
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
    /// The Advanced Abuse Report Options provides a way for developers to
    /// customize the reporting flow and specify the type of content being
    /// reported, which can be either a AbuseReportType.User or an
    /// AbuseReportType.Object, helping to maintain a safe and respectful community
    /// within their application.
    [Serializable]
    [Preserve]
    public class AdvancedAbuseReportOptions {
        /// This field is intended to allow developers to pass custom metadata through
        /// the report flow. The metadata passed through is included with the report
        /// received by the developer.
        [JsonProperty("developer_defined_context")]
        [Preserve]
        public Dictionary<string, string> DeveloperDefinedContext = null;
        /// If AbuseReportType.Object, a string representing the type of content being
        /// reported. This should correspond to the object_type string used in the UI.
        [JsonProperty("object_type")]
        [Preserve]
        public string ObjectType = "";
        /// The intended entity type AbuseReportType.User or an object/content
        /// AbuseReportType.Object.
        [JsonProperty("report_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<AbuseReportType>))]
        [Preserve]
        public AbuseReportType ReportType = AbuseReportType.Unknown;
        /// Provide a list of users to suggest for reporting. This list should include
        /// users that the reporter has recently interacted with to aid them in
        /// selecting the right user to report.
        [JsonProperty("suggested_users")]
        [JsonConverter(typeof(UInt64ArrayAsStringConverter))]
        [Preserve]
        public UInt64[] SuggestedUsers;
        /// The video mode AbuseReportVideoMode.Collect requires video evidence to be
        /// provided by the user. AbuseReportVideoMode.Optional presents the user with
        /// the option to provide video evidence. AbuseReportVideoMode.Skip bypasses
        /// the video evidence collection step altogether.
        [JsonProperty("video_mode")]
        [JsonConverter(typeof(EnumDescriptionConverter<AbuseReportVideoMode>))]
        [Preserve]
        public AbuseReportVideoMode VideoMode = AbuseReportVideoMode.Unknown;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public AdvancedAbuseReportOptions()
        {
        }
        
        [Preserve]
        public void SetDeveloperDefinedContext(Dictionary<string, string> value)
        {
            DeveloperDefinedContext = value;
        }
        /// Sets a single key-value pair in the DeveloperDefinedContext dictionary. This method is provided for backward compatibility.
        [Preserve]
        public void SetDeveloperDefinedContext(string key, string value)
        {
            if (DeveloperDefinedContext == null)
            {
                DeveloperDefinedContext = new Dictionary<string, string>();
            }
            DeveloperDefinedContext[key] = value;
        }

        /// Clears the DeveloperDefinedContext dictionary. This method is provided for backward compatibility.
        [Preserve]
        public void ClearDeveloperDefinedContext()
        {
            DeveloperDefinedContext = null;
        }
        
        [Preserve]
        public void SetObjectType(string value)
        {
            ObjectType = value;
        }
        
        [Preserve]
        public void SetReportType(AbuseReportType value)
        {
            ReportType = value;
        }
        
        [Preserve]
        public void SetSuggestedUsers(UInt64[] value)
        {
            SuggestedUsers = value;
        }

        /// Adds a single user to the SuggestedUsers list.
        [Preserve]
        public void AddSuggestedUser(UInt64 userID)
        {
            var list = SuggestedUsers != null
                ? new List<UInt64>(SuggestedUsers)
                : new List<UInt64>();
            list.Add(userID);
            SuggestedUsers = list.ToArray();
        }

        /// Clears the SuggestedUsers list.
        [Preserve]
        public void ClearSuggestedUsers()
        {
            SuggestedUsers = null;
        }
        
        
        [Preserve]
        public void SetVideoMode(AbuseReportVideoMode value)
        {
            VideoMode = value;
        }
        
        ~AdvancedAbuseReportOptions()
        {
        }
    }
}
