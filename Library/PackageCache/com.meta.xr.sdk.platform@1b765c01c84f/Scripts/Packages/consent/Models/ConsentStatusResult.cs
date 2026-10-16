// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<0483804a40cebdb5ba2be0a34a4a67b5>>
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

    /// This holds the result of getConsentStatus() API call.
    [Preserve]
    public class ConsentStatusResult
    {
        /// Current status of a consent.
        [JsonProperty("status")]
        [JsonConverter(typeof(EnumDescriptionConverter<ConsentStatus>))]
        [Preserve]
        public ConsentStatus Status { get; set; }
        /// Type of a consent.
        [JsonProperty("consent_type")]
        [Preserve]
        public string ConsentType { get; set; }
        /// Last status update time for the consent
        [JsonProperty("decision_time")]
        [Preserve]
        public long DecisionTime { get; set; }
        /// Version of the consent. Some consents can have different versions.
        [JsonProperty("version")]
        [Preserve]
        public string Version { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
