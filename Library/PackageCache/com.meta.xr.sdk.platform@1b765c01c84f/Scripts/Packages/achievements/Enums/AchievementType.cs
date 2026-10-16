// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<1c214cb23ad5d77cbc11e53b634f82bc>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Determines the type of the achievement. This enum defines the different
    /// types of achievements that can be used in the game. Each type has its own
    /// unique characteristics and requirements for unlocking. See more details
    /// about achievement
    /// [here](https://developer.oculus.com/documentation/unity/ps-achievements/).
    [Preserve]
    public enum AchievementType : int
    {
        /// Represents that the AchievementType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Simple achievements are unlocked by a single event or objective completion.
        /// They are often used to reward players for completing specific tasks or
        /// milestones within the game.
        [Description("SIMPLE")]
        [Preserve]
        Simple,

        /// Bitfield achievements are unlocked when a target number of bits are set
        /// within a bitfield.
        [Description("BITFIELD")]
        [Preserve]
        Bitfield,

        /// Count achievements are unlocked when a counter reaches a defined target.
        /// The counter is incremented each time the player completes the required
        /// action, and when it reaches the target value, the achievement is unlocked.
        [Description("COUNT")]
        [Preserve]
        Count,

        
    }

}
