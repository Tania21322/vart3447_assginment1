// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<50a12fb115181c765af5e93e0cfa47ef>>
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

    /// The result from users launching the Share to Facebook modal which enables
    /// them to share local media files through Media.ShareToFacebook. The result
    /// will contain a status field, ShareMediaStatus.
    [Preserve]
    public class ShareMediaResult
    {
        /// The status of the share media result. The status can be of the following
        /// types: ShareMediaStatus.Unknown ShareMediaStatus.Shared
        /// ShareMediaStatus.Canceled
        [JsonProperty("status")]
        [JsonConverter(typeof(EnumDescriptionConverter<ShareMediaStatus>))]
        [Preserve]
        public ShareMediaStatus Status { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
