// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<ad5811ec4b8a73acc7b94c7bd33f374a>>
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

    /// Contains the details about the trial offer associated with a BillingPlan.
    [Preserve]
    public class TrialOffer
    {
        /// Represents the maximum term for which a trial_offer is valid.
        [JsonProperty("max_term_count")]
        [Preserve]
        public int MaxTermCount { get; set; }
        /// The Price of the trial offer contains the currency code, the amount in
        /// hundredths, and the formatted string representation.
        [JsonProperty("price")]
        [Preserve]
        public Price Price { get; set; }
        /// An enum that specifies the term. OfferTerm.Unknown: unknown
        /// OfferTerm.Weekly: WEEKLY OfferTerm.Biweekly: BIWEEKLY OfferTerm.Monthly:
        /// MONTHLY OfferTerm.Quarterly: QUARTERLY OfferTerm.Semiannual: SEMIANNUAL
        /// OfferTerm.Annual: ANNUAL OfferTerm.Biannual: BIANNUAL
        [JsonProperty("trial_term")]
        [JsonConverter(typeof(EnumDescriptionConverter<OfferTerm>))]
        [Preserve]
        public OfferTerm TrialTerm { get; set; }
        /// An enum that specifies the type of the trial. OfferType.IntroOffer: Intro
        /// Offer. OfferType.FreeTrial: Free Trial.
        [JsonProperty("trial_type")]
        [JsonConverter(typeof(EnumDescriptionConverter<OfferType>))]
        [Preserve]
        public OfferType TrialType { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of TrialOffer elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class TrialOfferList : DeserializableList<TrialOffer>
    {
    }
}
