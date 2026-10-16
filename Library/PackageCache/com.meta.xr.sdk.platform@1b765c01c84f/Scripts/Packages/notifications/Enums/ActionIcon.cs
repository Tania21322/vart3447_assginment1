// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<a5c418302c6938c12ffaa2725fa20995>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Enum for supported notification action icons. These values correspond to
    /// the icons supported by the NotificationIconMapper.
    [Preserve]
    public enum ActionIcon : int
    {
        /// Accept icon - typically used for accepting invitations or requests.
        [Description("ACCEPT")]
        [Preserve]
        Accept,

        /// Close icon - typically used for dismissing or closing notifications.
        [Description("CLOSE")]
        [Preserve]
        Close,

        /// Destination icon (filled) - typically used for navigation or location-based
        /// actions.
        [Description("DESTINATION")]
        [Preserve]
        Destination,

        /// Call icon - typically used for initiating phone calls.
        [Description("CALL")]
        [Preserve]
        Call,

        /// Dismiss call icon - typically used for declining or ending calls.
        [Description("DISMISS_CALL")]
        [Preserve]
        DismissCall,

        /// Add friend icon - typically used for sending or accepting friend requests.
        [Description("ADD_FRIEND")]
        [Preserve]
        AddFriend,

        /// Info icon - typically used for displaying additional information.
        [Description("INFO")]
        [Preserve]
        Info,

        /// Party icon - typically used for party or group-related actions.
        [Description("PARTY")]
        [Preserve]
        Party,

        /// Play icon - typically used for starting media playbook or games.
        [Description("PLAY")]
        [Preserve]
        Play,

        /// Follow accept icon - typically used for accepting follow requests.
        [Description("FOLLOW_ACCEPT")]
        [Preserve]
        FollowAccept,

        /// Follow reject icon - typically used for rejecting follow requests.
        [Description("FOLLOW_REJECT")]
        [Preserve]
        FollowReject,

        /// Remove icon - typically used for deleting or removing items.
        [Description("REMOVE")]
        [Preserve]
        Remove,

        /// Friends icon - typically used for friend-related actions.
        [Description("FRIENDS")]
        [Preserve]
        Friends,

        /// Chat icon - typically used for opening chat or messaging.
        [Description("CHAT")]
        [Preserve]
        Chat,

        /// Destination icon (outline) - typically used for navigation with outline
        /// style.
        [Description("DESTINATION_OUTLINE")]
        [Preserve]
        DestinationOutline,

        /// Travel icon - typically used for travel or transportation actions.
        [Description("TRAVEL")]
        [Preserve]
        Travel,

        /// Download icon - typically used for downloading content.
        [Description("DOWNLOAD")]
        [Preserve]
        Download,

        /// Check icon - typically used for confirmation or completion actions.
        [Description("CHECK")]
        [Preserve]
        Check,

        /// Share icon - typically used for sharing content with others.
        [Description("SHARE")]
        [Preserve]
        Share,

        /// Represents that the ActionIcon is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        
    }

}
