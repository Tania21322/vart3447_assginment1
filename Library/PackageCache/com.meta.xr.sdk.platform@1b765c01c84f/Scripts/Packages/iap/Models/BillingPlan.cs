// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<07714bad7ed99182258b1f634d9f521f>>
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

    /// Contains the detailed billing plan information for a
    /// ProductType.Subscription. A BillingPlan can have a PaidOffer and an array
    /// of TrialOffer. The TrialOfferArray can contain a FREE_TRIAL and an
    /// INTRO_OFFER.
    [Preserve]
    public class BillingPlan
    {
        /// Represents the PaidOffer associated with the billing_plan.
        [JsonProperty("paid_offer")]
        [Preserve]
        public PaidOffer PaidOffer { get; set; }
        /// A list of TrialOffer associated with the billing_plan.
        [JsonProperty("trial_offers")]
        // May be null. Check before using.
        [Preserve]
        public TrialOfferList TrialOffersOptional { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of BillingPlan elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class BillingPlanList : DeserializableList<BillingPlan>
    {
    }
}
