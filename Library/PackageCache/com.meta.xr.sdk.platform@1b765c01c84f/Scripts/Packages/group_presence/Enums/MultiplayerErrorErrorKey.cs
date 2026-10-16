// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<a603ab30f559b9882e2c65e3d4ad1819>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Possible keys of the errors which could occurred when using the
    /// [multiplayer features](https://developer.oculus.com/documentation/unity/ps-multiplayer-overview/).
    /// e.g., you may need to use `destination_unavailable` for destinations
    /// feature and use `inviter_not_joinable` for invite feature and so on.
    [Preserve]
    public enum MultiplayerErrorErrorKey : int
    {
        /// Represents that the MultiplayerErrorErrorKey is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This error key will be used to tell the user that the travel destination is
        /// not available any more.
        [Description("DESTINATION_UNAVAILABLE")]
        [Preserve]
        DestinationUnavailable,

        /// This error will tell the user that the downloadable content will be needed.
        [Description("DLC_REQUIRED")]
        [Preserve]
        DlcRequired,

        /// This error key will be used in a broad range of general errors which are
        /// not be covered by the members of the enum.
        [Description("GENERAL")]
        [Preserve]
        General,

        /// This error key may be used to explain to the user the reason why she/he
        /// failed in joining a group.
        [Description("GROUP_FULL")]
        [Preserve]
        GroupFull,

        /// This error key will be used in explaining why an inviter cannot invite a
        /// recepient successfully. The group presence can be set to joinable by using
        /// GroupPresence.SetIsJoinable.
        [Description("INVITER_NOT_JOINABLE")]
        [Preserve]
        InviterNotJoinable,

        /// Certain features will not be available to the user in the app because the
        /// user's level does not reach to certain level.
        [Description("LEVEL_NOT_HIGH_ENOUGH")]
        [Preserve]
        LevelNotHighEnough,

        /// This error key may be used to explain to the user the failure was occurred
        /// becasue some level has not been reached.
        [Description("LEVEL_NOT_UNLOCKED")]
        [Preserve]
        LevelNotUnlocked,

        /// When the predefined network timeout has reached, the ongoing activity would
        /// be stopped. The dialog will use this error key to give the user the
        /// information.
        [Description("NETWORK_TIMEOUT")]
        [Preserve]
        NetworkTimeout,

        /// TODO: Add comment for this enum member!
        [Description("NO_LONGER_AVAILABLE")]
        [Preserve]
        NoLongerAvailable,

        /// TODO: Add comment for this enum member!
        [Description("UPDATE_REQUIRED")]
        [Preserve]
        UpdateRequired,

        /// TODO: Add comment for this enum member!
        [Description("TUTORIAL_REQUIRED")]
        [Preserve]
        TutorialRequired,

        
    }

}
