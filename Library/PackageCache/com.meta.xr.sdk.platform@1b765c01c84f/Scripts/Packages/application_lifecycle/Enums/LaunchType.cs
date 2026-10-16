// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<7a5b6f2b57fe7c7f14ac422e9deaf9c5>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// It's an enum that defines several different types of launches for an
    /// application. It provides a way to differentiate between different types of
    /// launches and to access additional information about the launch details. It
    /// can be retrieved using LaunchDetails.LaunchType to get the launch type
    /// information of an application.
    [Preserve]
    public enum LaunchType : int
    {
        /// Represents that the LaunchType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Normal launch from the user's library
        [Description("NORMAL")]
        [Preserve]
        Normal,

        /// Launch from the user accepting an invite.  Check
        /// LaunchDetails.LobbySessionId, LaunchDetails.MatchSessionId,
        /// LaunchDetails.DestinationApiName and LaunchDetails.DeeplinkMessage.
        [Description("INVITE")]
        [Preserve]
        Invite,

        /// DEPRECATED
        [Description("COORDINATED")]
        [Preserve]
        Coordinated,

        /// Launched from Application.LaunchOtherApp. Check LaunchDetails.LaunchSource
        /// and LaunchDetails.DeeplinkMessage.
        [Description("DEEPLINK")]
        [Preserve]
        Deeplink,

        
    }

}
