// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<4d6fcfd0c85e419e78143573b4e74da7>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// How far should we go back in time looking at history.
    [Preserve]
    public enum TimeWindow : int
    {
        /// Represents that the TimeWindow is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// TODO: Add comment for this enum member!
        [Description("ONE_HOUR")]
        [Preserve]
        OneHour,

        /// TODO: Add comment for this enum member!
        [Description("ONE_DAY")]
        [Preserve]
        OneDay,

        /// TODO: Add comment for this enum member!
        [Description("ONE_WEEK")]
        [Preserve]
        OneWeek,

        /// TODO: Add comment for this enum member!
        [Description("THIRTY_DAYS")]
        [Preserve]
        ThirtyDays,

        /// TODO: Add comment for this enum member!
        [Description("NINETY_DAYS")]
        [Preserve]
        NinetyDays,

        
    }

}
