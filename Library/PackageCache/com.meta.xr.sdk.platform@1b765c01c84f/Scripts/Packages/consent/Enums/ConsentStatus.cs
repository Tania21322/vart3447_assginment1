// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<40e1e14a2c450d1b362e3b353aae9729>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// This enum represents the current status of a Consent
    [Preserve]
    public enum ConsentStatus : int
    {
        /// User hasn't seen the consent yet.
        [Description("DEFAULT_NOT_SEEN")]
        [Preserve]
        DefaultNotSeen,

        /// User has seen the consent but hasn't approved/declined yet.
        [Description("SEEN")]
        [Preserve]
        Seen,

        /// User has seen the consent but either declined when consent was first shown
        /// or withdrawn at a later time from some settings UI.
        [Description("WITHDRAWN")]
        [Preserve]
        Withdrawn,

        /// User has consented to the consent
        [Description("CONSENTED")]
        [Preserve]
        Consented,

        
    }

}
