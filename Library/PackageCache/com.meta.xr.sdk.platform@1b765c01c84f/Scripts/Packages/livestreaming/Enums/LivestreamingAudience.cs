// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<90fcacc843d6071d089c6892a8df8aa6>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// It's a enum that represent the different types of audiences that can be
    /// selected for a livestreaming. It can be used to specify the audience when a
    /// live streaming starts. The livestreaming status change will be notified by
    /// Livestreaming.StatusChange
    [Preserve]
    public enum LivestreamingAudience : int
    {
        /// Represents that the LivestreamingAudience is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This value represents a public audience, meaning that anyone can view the
        /// livestream.
        [Description("PUBLIC")]
        [Preserve]
        Public,

        /// This value represents an audience consisting of the user's friends. Only
        /// people who are friends with the user will be able to view the livestream.
        [Description("FRIENDS")]
        [Preserve]
        Friends,

        /// This value represents an audience consisting only of the user themselves.
        /// Only the user who created the livestream will be able to view it.
        [Description("ONLY_ME")]
        [Preserve]
        OnlyMe,

        
    }

}
