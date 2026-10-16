// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<5bd892ec5e97c4abf537c0d2f45db212>>
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

    /// The unlock progress of a particular achievement can be retrieved using
    /// Achievements.GetAllProgress. It can be used to display the progress of each
    /// achievement in your game. See the following
    /// [link](https://developer.oculus.com/documentation/unity/ps-achievements/)
    /// for more information.
    [Preserve]
    public class AchievementProgress
    {
        /// If the type of the achievement is AchievementType.Bitfield it represents
        /// the current bitfield state that the achievement has reached.
        [JsonProperty("bitfield_progress")]
        [Preserve]
        public string Bitfield { get; set; }
        /// If the type of the achievement is AchievementType.Count, it represents the
        /// current counter state that the achievement has reached.
        [JsonProperty("count_progress")]
        [Preserve]
        public ulong Count { get; set; }
        /// If the user has already unlocked this achievement.
        [JsonProperty("is_unlocked")]
        [Preserve]
        public bool IsUnlocked { get; set; }
        /// The unique string that you use to reference the achievement in your app, as
        /// specified in the developer dashboard. It can be retrieved using
        /// AchievementDefinition.ApiName.
        [JsonProperty("api_name")]
        [Preserve]
        public string Name { get; set; }
        /// If the achievement is unlocked, the time when it was unlocked.
        [JsonProperty("unlock_time")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        [Preserve]
        public DateTime UnlockTime { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of AchievementProgress elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class AchievementProgressList : DeserializableList<AchievementProgress>
    {
    }
}
