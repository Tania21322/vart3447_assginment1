// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<cf7b55a9f304dbd38f10838e6fc064a3>>
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

    /// It contains information about a specific language, including its
    /// identification tag, and names in both English and the native language. It
    /// is useful for applications supporting multiple languages. It can be
    /// retrieved using AssetDetails.Language. Learn more about language pack in
    /// our
    /// [website](https://developer.oculus.com/documentation/unity/ps-language-packs/)
    [Preserve]
    public class LanguagePackInfo
    {
        /// Language name in English language. For example, the English name for
        /// "de.lang" will be "German".
        [JsonProperty("english_name")]
        [Preserve]
        public string EnglishName { get; set; }
        /// Language name in its native language. For example, the native name for
        /// "de.lang" will be "Deutsch".
        [JsonProperty("native_name")]
        [Preserve]
        public string NativeName { get; set; }
        /// Language tag in [BCP47](https://www.rfc-editor.org/info/bcp47) format with
        /// a suffix of "lang". For example, "de.lang" is a valid language pack name
        /// and its `tag` will be "de".
        [JsonProperty("tag")]
        [Preserve]
        public string Tag { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
