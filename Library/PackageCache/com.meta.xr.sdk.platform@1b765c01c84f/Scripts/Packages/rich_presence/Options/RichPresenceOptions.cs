// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<f1c3331353da2b861869930e41bc1f73>>
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
    /// FIXME: Add doc comment for this option!
    [Serializable]
    [Preserve]
    public class RichPresenceOptions {
        [JsonProperty("api_name")]
        [Preserve]
        public string ApiName = "";
        [JsonProperty("deeplink_message_override")]
        [Preserve]
        public string DeeplinkMessageOverride = "";
        [JsonProperty("is_joinable")]
        [Preserve]
        public bool IsJoinable = false;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public RichPresenceOptions()
        {
        }
        
        [Preserve]
        public void SetApiName(string value)
        {
            ApiName = value;
        }
        
        [Preserve]
        public void SetDeeplinkMessageOverride(string value)
        {
            DeeplinkMessageOverride = value;
        }
        
        [Preserve]
        public void SetIsJoinable(bool value)
        {
            IsJoinable = value;
        }
        
        ~RichPresenceOptions()
        {
        }
    }
}
