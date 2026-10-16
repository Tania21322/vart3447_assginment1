// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<320b7038011b489549900566f0b8fd9d>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Represents the type of metric being recorded in the in-app analytics
    /// system.
    [Preserve]
    public enum MetricType : int
    {
        /// Represents that the MetricType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// An action metric.
        [Description("ACTION")]
        [Preserve]
        Action,

        /// A damage metric.
        [Description("DAMAGE")]
        [Preserve]
        Damage,

        /// A death metric.
        [Description("DEATH")]
        [Preserve]
        Death,

        /// A discovery metric.
        [Description("DISCOVERY")]
        [Preserve]
        Discovery,

        /// A distance metric.
        [Description("DISTANCE")]
        [Preserve]
        Distance,

        /// A friction metric.
        [Description("FRICTION")]
        [Preserve]
        Friction,

        /// An outcome metric.
        [Description("OUTCOME")]
        [Preserve]
        Outcome,

        /// A progression metric.
        [Description("PROGRESSION")]
        [Preserve]
        Progression,

        /// A prerequisite metric.
        [Description("PREQ")]
        [Preserve]
        Preq,

        /// A purchase metric.
        [Description("PURCHASE")]
        [Preserve]
        Purchase,

        /// A rate metric.
        [Description("RATE")]
        [Preserve]
        Rate,

        /// A reward currency metric.
        [Description("REWARD_CURRENCY")]
        [Preserve]
        RewardCurrency,

        /// A reward items metric.
        [Description("REWARD_ITEMS")]
        [Preserve]
        RewardItems,

        /// A reward points metric.
        [Description("REWARD_POINTS")]
        [Preserve]
        RewardPoints,

        /// A reward status metric.
        [Description("REWARD_STATUS")]
        [Preserve]
        RewardStatus,

        /// A social metric.
        [Description("SOCIAL")]
        [Preserve]
        Social,

        /// Other metric type not covered by the predefined types.
        [Description("OTHER")]
        [Preserve]
        Other,

        
    }

}
