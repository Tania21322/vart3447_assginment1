// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<68bf663d9ddd344c15edc78ca970aa9c>>
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

    /// The livestreaming status represents the status of a livestreaming event in
    /// your app. You will receive a notification by Livestreaming.StatusChange
    /// whenever your livestreaming session gets updated. The status contains info
    /// about your livestream type, whether your mic is enabled, whether the
    /// comments are visible and etc.
    [Preserve]
    public class LivestreamingStatus
    {
        /// This boolean field indicates if the comments from the audience in your
        /// livestreaming are visible.
        [JsonProperty("comments_visible")]
        [Preserve]
        public bool CommentsVisible { get; set; }
        /// This boolean field indicates if your livestreaming in the app is paused or
        /// not.
        [JsonProperty("is_paused")]
        [Preserve]
        public bool IsPaused { get; set; }
        /// This boolean field indicates if your app is livestreaming enabled. If your
        /// app is enabled, you will receive a notification by
        /// Livestreaming.StatusChange when the livestreaming session gets updated.
        [JsonProperty("livestreaming_enabled")]
        [Preserve]
        public bool LivestreamingEnabled { get; set; }
        /// This field indicates the type of your livestreaming.
        [JsonProperty("livestreaming_type")]
        [Preserve]
        public int LivestreamingType { get; set; }
        /// This boolean field indicates if your connected mic is enabled. The speaker
        /// will be muted if the field is false.
        [JsonProperty("mic_enabled")]
        [Preserve]
        public bool MicEnabled { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
