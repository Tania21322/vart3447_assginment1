// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<299c1cfba9bcc45c71bd8f20a7fbbf4d>>
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
    /// Input type for specifying segment settings, including cosmetics,
    /// difficulty, and game mode.
    [Serializable]
    [Preserve]
    public class SegmentSettingsInput {
        /// The list of cosmetic items active during the segment.
        [JsonProperty("cosmetics")]
        [Preserve]
        public string[] Cosmetics = null;
        /// The difficulty setting during the segment.
        [JsonProperty("difficulty")]
        [Preserve]
        public string Difficulty = null;
        /// The game mode during the segment.
        [JsonProperty("game_mode")]
        [Preserve]
        public string GameMode = null;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public SegmentSettingsInput()
        {
        }
        
        [Preserve]
        public void SetCosmetics(string[] value)
        {
            Cosmetics = value;
        }
        
        [Preserve]
        public void SetDifficulty(string value)
        {
            Difficulty = value;
        }
        
        [Preserve]
        public void SetGameMode(string value)
        {
            GameMode = value;
        }
        
        ~SegmentSettingsInput()
        {
        }
    }
}
