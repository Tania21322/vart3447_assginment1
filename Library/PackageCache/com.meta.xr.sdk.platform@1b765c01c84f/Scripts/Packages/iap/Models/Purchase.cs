// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<6101a0b5ae765ef3a00a39989cc9a6a6>>
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

    /// A purchase is made when a user buys a Product. The IAP product, which can
    /// represent a consumable item, a durable item, or a subscription, must be
    /// defined for purchase through the developer dashboard.
    [Preserve]
    public class Purchase
    {
        /// The developer payload feature is unimplemented.
        [JsonProperty("developer_payload")]
        [Preserve]
        public string DeveloperPayload { get; set; }
        /// The time when the purchased Product expires. This value only applies to
        /// subscriptions, and will be null for durable and consumable IAP items.
        [JsonProperty("expiration_time")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        [Preserve]
        public DateTime ExpirationTime { get; set; }
        /// The timestamp that represents when the user was granted entitlement to the
        /// Product that was purchased.
        [JsonProperty("grant_time")]
        [JsonConverter(typeof(UnixDateTimeConverter))]
        [Preserve]
        public DateTime GrantTime { get; set; }
        /// The unique identifier of a Purchase represents a user's unique entitlement
        /// to a Product. This value is 0 for shared IAP entitlements.
        [JsonProperty("id")]
        [Preserve]
        public string ID { get; set; }
        /// The Reporting ID feature is not implemented.
        [JsonProperty("reporting_id")]
        [Preserve]
        public string ReportingId { get; set; }
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
    
    /// Represents a paginated list of Purchase elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class PurchaseList : DeserializableList<Purchase>
    {
    }
}
