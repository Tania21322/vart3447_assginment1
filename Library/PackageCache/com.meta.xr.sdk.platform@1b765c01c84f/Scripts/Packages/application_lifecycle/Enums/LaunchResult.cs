// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<63c449df743fc961f4472641be0f37b8>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// An enum that specifies the whether the attempt to launch this application
    /// via a deeplink was successful. The value is meant to be reported after a
    /// deeplink by calling ApplicationLifecycle.LogDeeplinkResult
    [Preserve]
    public enum LaunchResult : int
    {
        /// Represents that the LaunchResult is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// The application launched successfully.
        [Description("SUCCESS")]
        [Preserve]
        Success,

        /// The application launch failed because the room was full.
        [Description("FAILED_ROOM_FULL")]
        [Preserve]
        FailedRoomFull,

        /// The application launch failed because the game has already started.
        [Description("FAILED_GAME_ALREADY_STARTED")]
        [Preserve]
        FailedGameAlreadyStarted,

        /// The appplicatin launch failed because the room couldn't be found.
        [Description("FAILED_ROOM_NOT_FOUND")]
        [Preserve]
        FailedRoomNotFound,

        /// The application launch failed because the user declined the invitation.
        [Description("FAILED_USER_DECLINED")]
        [Preserve]
        FailedUserDeclined,

        /// The application launch failed due to some other reason.
        [Description("FAILED_OTHER_REASON")]
        [Preserve]
        FailedOtherReason,

        
    }

}
