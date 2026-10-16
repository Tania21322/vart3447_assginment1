// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<33b70c9e8f0d2d3260b2ff62dd24c0ea>>
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

    /// Settings associated with a segment event, including cosmetics, difficulty,
    /// and game mode.
    [Preserve]
    public class SegmentSettings
    {
        /// The list of cosmetic items active during the segment.
        [JsonProperty("cosmetics")]
        [Preserve]
        public string[] Cosmetics { get; set; }
        /// The difficulty setting during the segment.
        [JsonProperty("difficulty")]
        [Preserve]
        public string Difficulty { get; set; }
        /// The game mode during the segment.
        [JsonProperty("game_mode")]
        [Preserve]
        public string GameMode { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
