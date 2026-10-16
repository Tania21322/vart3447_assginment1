// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<a22260b267e4b715f431909e7ed1695f>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// It represents the possible reasons why the status of a NetSyncConnection,
    /// which allows multiple clients to connect and communicate with each other in
    /// real-time, is disconnected. The connection provides a way to manage and
    /// facilitate real-time communication and data synchronization between
    /// multiple clients in a networked environment.
    [Preserve]
    public enum NetSyncDisconnectReason : int
    {
        /// Represents that the NetSyncDisconnectReason is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This member indicates that the disconnect of the NetSyncConnection was
        /// initialized from the a user request.
        [Description("LOCAL_TERMINATED")]
        [Preserve]
        LocalTerminated,

        /// This member indicates that the connection of the NetSyncConnection was
        /// shutdown by the server intentionally.
        [Description("SERVER_TERMINATED")]
        [Preserve]
        ServerTerminated,

        /// This member indicates that the initial connection request never succeeded.
        [Description("FAILED")]
        [Preserve]
        Failed,

        /// This member indicates that the NetSyncConnection was shutdown because of
        /// the network timeout.
        [Description("LOST")]
        [Preserve]
        Lost,

        
    }

}
