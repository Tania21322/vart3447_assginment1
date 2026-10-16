// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<c775dd601d95430302f701c8ced43b65>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// A network synchronization system allows multiple clients to connect and
    /// communicate with each other in real-time. Once a NetSyncConnection is
    /// established, it uses Voice over Internet Protocol to allow users to make
    /// voice calls using the internet. The mic source will represent the current
    /// source of the mic in the call.
    [Preserve]
    public enum NetSyncVoipMicSource : int
    {
        /// Represents that the NetSyncVoipMicSource is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This `enum` member represents no net sync connection has been identified as
        /// the source from which the data was sent to the server
        [Description("NONE")]
        [Preserve]
        None,

        /// This `enum` member represents an internal net sync connection has been
        /// identified as the source from which the data was sent to the server
        [Description("INTERNAL")]
        [Preserve]
        Internal,

        
    }

}
