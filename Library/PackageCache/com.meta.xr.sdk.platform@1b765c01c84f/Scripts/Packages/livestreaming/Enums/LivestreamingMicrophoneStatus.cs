// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<9da348713d398550a8d57c4643030ac0>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// It's an enum that represents the status of a microphone during a
    /// livestream. It provides a simple and standardized way to represent the
    /// status of a microphone during a livestream, which can be useful for various
    /// applications and use cases. See
    /// [livestreaming](https://developer.oculus.com/blog/sharing-via-livestreaming-now-available-for-rift-applications/)
    /// documentation for more details.
    [Preserve]
    public enum LivestreamingMicrophoneStatus : int
    {
        /// Represents that the LivestreamingMicrophoneStatus is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// It indicates that the microphone is currently on and transmitting audio.
        /// This value can be used to indicate that the microphone is functioning
        /// properly and that audio is being captured and transmitted during the
        /// livestream.
        [Description("MICROPHONE_ON")]
        [Preserve]
        MicrophoneOn,

        /// It indicates that the microphone is currently off and not transmitting
        /// audio. This value can be used to indicate that the microphone has been
        /// turned off or muted, either intentionally or unintentionally, during the
        /// livestream.
        [Description("MICROPHONE_OFF")]
        [Preserve]
        MicrophoneOff,

        
    }

}
