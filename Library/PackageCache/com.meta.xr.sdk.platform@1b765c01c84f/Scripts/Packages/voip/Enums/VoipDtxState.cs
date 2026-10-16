// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<0d17cd0138cfbcd47c4daf91a1ed0883>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// This is an enum that defines the possible states for the Opus codec's
    /// discontinuous transmission (DTX) feature. It allows you to control whether
    /// the Opus codec uses DTX to conserve battery power and reduce transmission
    /// rate during pauses in the voice chat. It can be used as the type for the
    /// VoipOptions.CreateNewConnectionUseDtx
    [Preserve]
    public enum VoipDtxState : int
    {
        /// Represents that the VoipDtxState is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This state indicates that the DTX feature is enabled. When enabled, the
        /// Opus codec will only transmit data when a person is speaking, which can
        /// conserve battery power and reduce transmission rate during pauses in the
        /// voice chat.
        [Description("ENABLED")]
        [Preserve]
        Enabled,

        /// This state indicates that the DTX feature is disabled. When disabled, the
        /// Opus codec will continuously transmit data, even during pauses in the voice
        /// chat.
        [Description("DISABLED")]
        [Preserve]
        Disabled,

        
    }

}
