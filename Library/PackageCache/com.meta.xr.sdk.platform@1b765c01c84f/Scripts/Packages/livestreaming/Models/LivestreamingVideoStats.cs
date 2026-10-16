// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<cc2226c847a4391f4f1ecf8a91c5a84f>>
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

    /// The livestreaming video statistics represents the statistics info about a
    /// livestreaming video in your app. The statistics include the total number of
    /// views, the number of reactions and the number of comments. You can retrieve
    /// the information about whether your comments are visible from
    /// LivestreamingStatus.CommentsVisible.
    [Preserve]
    public class LivestreamingVideoStats
    {
        /// An 'integer' represents the information about the total number of comments
        /// left for your livestream video.
        [JsonProperty("comment_count")]
        [Preserve]
        public int CommentCount { get; set; }
        /// An 'integer' represents the information about the total number of reactions
        /// your livestream video received.
        [JsonProperty("reaction_count")]
        [Preserve]
        public int ReactionCount { get; set; }
        /// This field gives the information about the total number of views of your
        /// livestream video.
        [JsonProperty("total_views")]
        [Preserve]
        public string TotalViews { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
