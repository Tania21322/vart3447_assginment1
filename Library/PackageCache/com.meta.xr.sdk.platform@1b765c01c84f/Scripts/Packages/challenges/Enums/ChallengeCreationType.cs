// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<731b3f3c1969a9be52349f62910fdc6b>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Describes the creator of the associated challenge. This field indicates who
    /// created the challenge, whether it was ChallengeCreationType.UserCreated or
    /// ChallengeCreationType.DeveloperCreated. Understanding the creator of the
    /// associated challenge can provide valuable context and help participants
    /// better understand the nature and purpose of the challenge.
    [Preserve]
    public enum ChallengeCreationType : int
    {
        /// Represents that the ChallengeCreationType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// The challenge was created by a User. This means that a regular user of the
        /// app created the challenge, and it may be a community-driven challenge or a
        /// personal challenge created by the user for themselves or others.
        [Description("USER_CREATED")]
        [Preserve]
        UserCreated,

        /// The challenge was created by the app developer. This means that the
        /// challenge was created by the team behind the app, and it may be an official
        /// challenge or a special event created by the developers to engage with the
        /// community or promote specific features of the app.
        [Description("DEVELOPER_CREATED")]
        [Preserve]
        DeveloperCreated,

        
    }

}
