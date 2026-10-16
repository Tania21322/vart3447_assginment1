// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<8b669cae66dd2e1b72293b07cf8fe619>>
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

    /// A Content Rating of a Product. This rating is sourced by the International
    /// Age Rating Coalition (IARC) certificate from the local rating authority. An
    /// Add-on's Content Rating can be configured by adding a IARC certificate in
    /// the developer dashboard. The Add-on can also inherit the Content Rating of
    /// the base App.
    [Preserve]
    public class ContentRating
    {
        /// URI for the image that needs to be shown for the content rating of the
        /// Product.
        [JsonProperty("age_rating_image_uri")]
        [Preserve]
        public string AgeRatingImageUri { get; set; }
        /// The age rating text is the text version of the rating used to describe age
        /// appropriateness by the International Age Rating Coalition (IARC).
        [JsonProperty("age_rating_text")]
        [Preserve]
        public string AgeRatingText { get; set; }
        /// The list of descriptors which indicate content within the product that may
        /// have triggered a particular age rating or may be of interest or concern to
        /// consumers, e.g.,  "Blood and Gore", "Intense Violence", etc.
        [JsonProperty("descriptors")]
        [Preserve]
        public readonly String[] Descriptors;/// The list of interactive elements, which advise consumers up front that a
        /// Product includes interactive or online behaviors/options that may be of
        /// interest or concern, e.g., "In-App Purchases".
        [JsonProperty("interactive_elements")]
        [Preserve]
        public readonly String[] InteractiveElements;/// The URI pointing to a website with International Age Rating Coalition
        /// (IARC) rating definitions from local rating authorities (e.g., Australian
        /// Classification Board, ESRB, GRAC, etc).
        [JsonProperty("rating_definition_uri")]
        [Preserve]
        public string RatingDefinitionUri { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
