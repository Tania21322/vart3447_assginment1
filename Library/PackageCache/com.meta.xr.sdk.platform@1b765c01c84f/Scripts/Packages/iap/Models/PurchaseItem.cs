// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<70675b4aeec6fdeb51971492dc8d0eb1>>
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

    /// The details for an IAP Product that was purchased
    [Preserve]
    public class PurchaseItem
    {
        /// The SKU of the IAP Iap.GetProductsBySku.
        [JsonProperty("sku")]
        [Preserve]
        public string Sku { get; set; }
        /// The Type of the IAP ProductType.Durable, ProductType.Consumable, or a
        /// ProductType.Subscription.
        [JsonProperty("type")]
        [Preserve]
        public ProductType Type { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
