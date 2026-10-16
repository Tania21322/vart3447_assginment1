// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<660d512009c9eb3be06327a93b0e148f>>
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

    /// Contains the details about the paid offer associated with a BillingPlan.
    [Preserve]
    public class PaidOffer
    {
        /// The Price of the paid offer contains the currency code, the amount in
        /// hundredths, and the formatted string representation.
        [JsonProperty("price")]
        [Preserve]
        public Price Price { get; set; }
        /// An enum that specifies the term. OfferTerm.Unknown: unknown
        /// OfferTerm.Weekly: WEEKLY OfferTerm.Biweekly: BIWEEKLY OfferTerm.Monthly:
        /// MONTHLY OfferTerm.Quarterly: QUARTERLY OfferTerm.Semiannual: SEMIANNUAL
        /// OfferTerm.Annual: ANNUAL OfferTerm.Biannual: BIANNUAL
        [JsonProperty("subscription_term")]
        [JsonConverter(typeof(EnumDescriptionConverter<OfferTerm>))]
        [Preserve]
        public OfferTerm SubscriptionTerm { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
