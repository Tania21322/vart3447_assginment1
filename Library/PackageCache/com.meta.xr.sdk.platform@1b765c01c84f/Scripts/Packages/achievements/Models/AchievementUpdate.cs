// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<73aae97c512760eacaa1a1163b4a65af>>
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

    /// Represents an update to an existing achievement. It will be the payload if
    /// there is any updates on achievements, as unlocking an achievement by
    /// Achievements.Unlock, adding 'count' to the achievement by
    /// Achievements.AddCount, and unlocking fields of a BITFIELD achievement by
    /// Achievements.AddFields.
    [Preserve]
    public class AchievementUpdate
    {
        /// This indicates if this update caused the achievement to unlock.
        [JsonProperty("just_unlocked")]
        [Preserve]
        public bool JustUnlocked { get; set; }
        /// The unique AchievementDefinition.ApiName used to reference the updated
        /// achievement, as specified in the developer dashboard.
        [JsonProperty("api_name")]
        [Preserve]
        public string Name { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
