// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<853cf6ea6654d19e380b47f4ee1527e1>>
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
    /// The settings of the segment event
    [Serializable]
    [Preserve]
    public class AnalyticsSettings {
        /// The difficulty of the app/game
        [JsonProperty("difficulty")]
        [Preserve]
        public string Difficulty = null;
        /// The mode of the game. This can be a set of string values you define
        [JsonProperty("game_mode")]
        [Preserve]
        public string GameMode = null;
        /// The cosmetics of the game. This can be a set of string values you define
        [JsonProperty("cosmetics")]
        [Preserve]
        public string[] Cosmetics = null;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public AnalyticsSettings()
        {
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
        
        [Preserve]
        public void SetCosmetics(string[] value)
        {
            Cosmetics = value;
        }
        
        ~AnalyticsSettings()
        {
        }
    }
}
