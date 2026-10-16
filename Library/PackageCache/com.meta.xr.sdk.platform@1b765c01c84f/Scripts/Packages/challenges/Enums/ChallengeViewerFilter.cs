// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<8b3928ec8112ae18f7468d9aaa6b4e94>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// The available filtering options on the Challenge returned by
    /// Challenges.GetList. These filters allow users to customize their search
    /// results and retrieve only the challenges that meet specific criteria. See
    /// more details of Challenges in
    /// [here](https://developer.oculus.com/documentation/unity/ps-challenges/).
    [Preserve]
    public enum ChallengeViewerFilter : int
    {
        /// Represents that the ChallengeViewerFilter is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Returns all public ((ChallengeVisibility.Public))) and invite-only
        /// ChallengeVisibility.Private) challenges. This filter is useful for users
        /// who want to see all challenges they are involved in, regardless of their
        /// visibility settings.
        [Description("ALL_VISIBLE")]
        [Preserve]
        AllVisible,

        /// Returns challenges in which the user is a participant. This filter is
        /// useful for users who want to see only the challenges they are actively
        /// participating in.
        [Description("PARTICIPATING")]
        [Preserve]
        Participating,

        /// Returns challenges that the user has been invited to. This filter is useful
        /// for users who want to see only the challenges they have been explicitly
        /// invited to.
        [Description("INVITED")]
        [Preserve]
        Invited,

        /// Returns challenges the user is either participating in or invited to. This
        /// filter is useful for users who want to see all challenges they are involved
        /// in, whether as a participant or an invitee.
        [Description("PARTICIPATING_OR_INVITED")]
        [Preserve]
        ParticipatingOrInvited,

        
    }

}
