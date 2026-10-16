// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<8f90641c0bb1c7fbe33b0e54c44be392>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// The system voip status is the priamary status in every SystemVoipState. The
    /// system voip state is used in parties and horizon home to describe the
    /// current state of the input/output for voip in an application. You can read
    /// more about system voip
    /// [here](https://developer.oculus.com/documentation/unity/ps-parties/#voip-options).
    [Preserve]
    public enum SystemVoipStatus : int
    {
        /// Represents that the SystemVoipStatus is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// TODO: Add comment for this enum member!
        [Description("UNAVAILABLE")]
        [Preserve]
        Unavailable,

        /// TODO: Add comment for this enum member!
        [Description("SUPPRESSED")]
        [Preserve]
        Suppressed,

        /// TODO: Add comment for this enum member!
        [Description("ACTIVE")]
        [Preserve]
        Active,

        
    }

}
