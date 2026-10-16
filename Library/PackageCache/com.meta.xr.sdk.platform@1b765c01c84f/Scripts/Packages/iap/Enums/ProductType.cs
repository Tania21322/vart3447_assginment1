// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<db82b19d66ae02791d7242d05c96025d>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// This `enum` value represents the possible types of a Product, which is an
    /// item purchased in the application. An In-app purchase (IAP) add-on can be
    /// durable, consuable, or a subscription.
    [Preserve]
    public enum ProductType : int
    {
        /// Represents that the ProductType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This product is a durable IAP item that can be consumed multiple times. It
        /// can be purchased only once.
        [Description("DURABLE")]
        [Preserve]
        Durable,

        /// This product is an IAP item that can be consumed only once. It can only be
        /// purchased again after it is consumed.
        [Description("CONSUMABLE")]
        [Preserve]
        Consumable,

        /// This product represents a subscription. Subscriptions provide a way for
        /// users to purchase your app or its premium content by way of a recurring
        /// payment model.
        [Description("SUBSCRIPTION")]
        [Preserve]
        Subscription,

        
        // Backward compatibility aliases
        UNKNOWN = Unknown,
        DURABLE = Durable,
        CONSUMABLE = Consumable,
        SUBSCRIPTION = Subscription,
        
    }

}
