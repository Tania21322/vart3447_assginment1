// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<39be2d419de5bf8ea73e0591ff924cc2>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// TODO: Add comment for this enum!
    [Preserve]
    public enum VoipSampleRate : int
    {
        /// Represents that the VoipSampleRate is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// TODO: Add comment for this enum member!
        [Description("HZ24000")]
        [Preserve]
        Hz24000,

        /// TODO: Add comment for this enum member!
        [Description("HZ44100")]
        [Preserve]
        Hz44100,

        /// TODO: Add comment for this enum member!
        [Description("HZ48000")]
        [Preserve]
        Hz48000,

        
    }

}
