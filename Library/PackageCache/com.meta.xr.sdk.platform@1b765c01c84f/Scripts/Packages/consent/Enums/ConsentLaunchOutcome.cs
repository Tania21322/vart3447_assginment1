// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<e2f3e50b7617d1d07ff9384a15608144>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// This enum represents the outcome of launch request of a consent flow
    [Preserve]
    public enum ConsentLaunchOutcome : int
    {
        /// Consent flow was not launched because this consent was already completed
        /// and user already DISMISSED/CONSENTED/WITHDRAWN
        [Description("NOT_REQUIRED")]
        [Preserve]
        NotRequired,

        /// Consent flow was launched and user DISMISSED the consent without approving
        /// or declining.
        [Description("DISMISSED")]
        [Preserve]
        Dismissed,

        /// Consent flow was launched and user declined to the consent.
        [Description("DENIED")]
        [Preserve]
        Denied,

        /// Consent flow was launched and user agreed to the consent.
        [Description("APPROVED")]
        [Preserve]
        Approved,

        /// Represents that the ConsentLaunchOutcome is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        
    }

}
