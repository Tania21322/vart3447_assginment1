// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<5329b4eb119b468cc91ca5694169729b>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// A network synchronization system allows multiple clients to connect and
    /// communicate with each other in real-time. Once a NetSyncConnection is
    /// established, user can choose the stream mode for the connection. The
    /// NetSyncOptions.VoipStreamDefault will be used when a new VoIP(Voice over
    /// Internet Protocol) user connects.
    [Preserve]
    public enum NetSyncVoipStreamMode : int
    {
        /// Represents that the NetSyncVoipStreamMode is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This `enum` member represents the ambisonic steam mode the VoIP stream
        /// uses. It is the default value of NetSyncOptions.VoipStreamDefault. Since it
        /// allows for the creation of immersive, surround sound experiences that
        /// simulate real-world audio environments, it typically used in virtual
        /// reality (VR) and augmented reality (AR) applications.
        [Description("AMBISONIC")]
        [Preserve]
        Ambisonic,

        /// This `enum` member represents the mono steam mode the VoIP stream uses. The
        /// advantages mono stream mode has over ambisonic steam mode is the audio
        /// encoding and decoding require less computational resources and thus audio
        /// streams require less bandwidth. So it is typically used in applications
        /// with limited network resources.
        [Description("MONO")]
        [Preserve]
        Mono,

        
    }

}
