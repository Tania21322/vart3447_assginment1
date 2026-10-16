// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<12faf382834e7edf16a25f2fe4eb3310>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// The visibility of the challenge. A challenge may be invite-only, public, or
    /// private. The visibility setting determines who can see and participate in
    /// the challenge. See more details of Challenges in
    /// [here](https://developer.oculus.com/documentation/unity/ps-challenges/).
    [Preserve]
    public enum ChallengeVisibility : int
    {
        /// Represents that the ChallengeVisibility is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Only those invited can participate in it. Everyone can see it, but only
        /// those with an invitation can joinand participate in the challenge. This
        /// setting is useful for challenges that are meant to be exclusive or for a
        /// specific group of people.
        [Description("INVITE_ONLY")]
        [Preserve]
        InviteOnly,

        /// Everyone can participate and see this challenge. This setting makes the
        /// challenge open to anyone who wants to join, and everyone can see the
        /// challenge details and progress. This setting is useful for challenges that
        /// are meant to be open and inclusive.
        [Description("PUBLIC")]
        [Preserve]
        Public,

        /// Only those invited can participate and see this challenge. This setting
        /// makes the challenge invisible to everyone except those who have been
        /// explicitly invited. Only those with an invitation can see the challenge
        /// details and progress, and only they can participate. This setting is useful
        /// for challenges that are meant to be highly exclusive or confidential.
        [Description("PRIVATE")]
        [Preserve]
        Private,

        
    }

}
