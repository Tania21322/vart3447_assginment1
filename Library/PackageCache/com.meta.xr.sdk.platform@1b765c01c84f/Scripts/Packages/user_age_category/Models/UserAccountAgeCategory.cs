// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<0a308e5dae6a49b3f1db88a077bdcbce>>
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

    /// A UserAccountAgeCategory represents the age category of a Meta user. This
    /// object contains information about the user's age group, which can be used
    /// for various purposes such as targeted advertising or content restriction.
    /// The possible values for the age category are defined in the
    /// AccountAgeCategory.
    [Preserve]
    public class UserAccountAgeCategory
    {
        /// Age category of the user in Meta account. This field represents the age
        /// group that the user falls into, and the possible values for this field are
        /// defined in the AccountAgeCategory.
        [JsonProperty("age_category")]
        [JsonConverter(typeof(EnumDescriptionConverter<AccountAgeCategory>))]
        [Preserve]
        public AccountAgeCategory AgeCategory { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
