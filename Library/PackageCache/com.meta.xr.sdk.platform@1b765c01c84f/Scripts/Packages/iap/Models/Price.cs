// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<96ec3fb367a3bc319b8ba7d2e172bcb6>>
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

    /// The price of a Product. A price contains a currency code, an amount in
    /// hundredths, and its formatted string representation. For example, a price
    /// with a currency code of "USD" and an amount in hundredths of 99 has a
    /// formatted string of "$0.99".
    [Preserve]
    public class Price
    {
        /// The price of the product in hundredths of currency units.
        [JsonProperty("amount_in_hundredths")]
        [Preserve]
        public uint AmountInHundredths { get; set; }
        /// The ISO 4217 currency code for the price of the product. For example,
        /// "USD", "GBP", "JPY".
        [JsonProperty("currency")]
        [Preserve]
        public string Currency { get; set; }
        /// The formatted string representation of the price, e.g., "$0.78". The value
        /// depends on the Price.Currency and Price.AmountInHundredths.
        [JsonProperty("formatted")]
        [Preserve]
        public string Formatted { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
