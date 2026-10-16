// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<e5f30c5e992dc4f1c652aaabe7ae5a7b>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// An enum that defines the different types of filters that can be applied to
    /// a leaderboard. It can be used in Leaderboards.GetEntries to filter the
    /// leaderboard entries, such as only friends or specific user IDs.
    [Preserve]
    public enum LeaderboardFilterType : int
    {
        /// No filter enabled on the leaderboard.
        [Description("NONE")]
        [Preserve]
        None,

        /// This value indicates that the leaderboard should be filtered to include
        /// only friends (bidirectional followers) of the current user.
        [Description("FRIENDS")]
        [Preserve]
        Friends,

        /// Represents that the LeaderboardFilterType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Filter the leaderboard to include specific user IDs. Use this filter to get
        /// rankings for users that are competing against each other. You specify the
        /// leaderboard name and whether to start at the top, or for the results to
        /// center on the (client) user. Note that if you specify the results to center
        /// on the client user, their leaderboard entry will be included in the
        /// returned array, regardless of whether their ID is explicitly specified in
        /// the list of IDs.
        [Description("USER_IDS")]
        [Preserve]
        UserIds,

        
    }

}
