// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<cc5003b1446399560e9438e61ac025fb>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// An enumeration that defines the type of the TrialOffer. This can be
    /// utilized to determine the length of time for which the offer is valid. By
    /// setting this field, you can easily manage the duration of your offers and
    /// provide a better user experience for your customers.
    [Preserve]
    public enum OfferTerm : int
    {
        /// Represents that the OfferTerm is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Represents that the offer term is weekly. This means that the offer will be
        /// valid for a period of one week from the date of purchase.
        [Description("WEEKLY")]
        [Preserve]
        Weekly,

        /// Represents that the offer term is biweekly. This means that the offer will
        /// be valid for a period of two weeks from the date of purchase.
        [Description("BIWEEKLY")]
        [Preserve]
        Biweekly,

        /// Represents that the offer term is monthly. This means that the offer will
        /// be valid for a period of one month from the date of purchase.
        [Description("MONTHLY")]
        [Preserve]
        Monthly,

        /// Represents that the offer term is quarterly. This means that the offer will
        /// be valid for a period of three months from the date of purchase.
        [Description("QUARTERLY")]
        [Preserve]
        Quarterly,

        /// Represents that the offer term is every 6 months. This means that the offer
        /// will be valid for a period of six months from the date of purchase.
        [Description("SEMIANNUAL")]
        [Preserve]
        Semiannual,

        /// Represents that the offer term is annual. This means that the offer will be
        /// valid for a period of one year from the date of purchase.
        [Description("ANNUAL")]
        [Preserve]
        Annual,

        /// Represents that the offer term is every 2 years. This means that the offer
        /// will be valid for a period of two years from the date of purchase.
        [Description("BIANNUAL")]
        [Preserve]
        Biannual,

        
        // Backward compatibility aliases
        UNKNOWN = Unknown,
        WEEKLY = Weekly,
        BIWEEKLY = Biweekly,
        MONTHLY = Monthly,
        QUARTERLY = Quarterly,
        SEMIANNUAL = Semiannual,
        ANNUAL = Annual,
        BIANNUAL = Biannual,
        
    }

}
