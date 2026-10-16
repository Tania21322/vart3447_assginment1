// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<39bb9f7f5efe6b3afba19b709012953c>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// It's a set of predefined values that represent different levels of audio
    /// quality for Voice over IP (VoIP) connections. It can be used in
    /// VoipOptions.BitrateForNewConnection to set the maximum average bitrate the
    /// audio codec should use for new VoIP connections.
    [Preserve]
    public enum VoipBitrate : int
    {
        /// Represents that the VoipBitrate is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Very low audio quality for minimal network usage. This may not give the
        /// full range of Hz for audio, but it will save on network usage.
        [Description("B16000")]
        [Preserve]
        B16000,

        /// Lower audio quality but also less network usage.
        [Description("B24000")]
        [Preserve]
        B24000,

        /// This is the default bitrate for voip connections. It should be the best
        /// tradeoff between audio quality and network usage.
        [Description("B32000")]
        [Preserve]
        B32000,

        /// Higher audio quality at the expense of network usage. Good if there's music
        /// being streamed over the connections
        [Description("B64000")]
        [Preserve]
        B64000,

        /// Even higher audio quality for music streaming or radio-like quality.
        [Description("B96000")]
        [Preserve]
        B96000,

        /// At this point the audio quality should be perceptually indistinguishable
        /// from the uncompressed input.
        [Description("B128000")]
        [Preserve]
        B128000,

        
    }

}
