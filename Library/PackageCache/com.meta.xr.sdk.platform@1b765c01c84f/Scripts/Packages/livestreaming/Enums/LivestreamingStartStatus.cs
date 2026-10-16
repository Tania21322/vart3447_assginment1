// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<de8f9387a4ed963241bf1e8d38ded9d6>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// It's an enum that represents the possible outcomes of starting a
    /// livestreaming session. This allows the API to return a standardized and
    /// well-defined set of values to indicate the outcome of starting a
    /// livestreaming session. It can be used in
    /// LivestreamingStartResult.StreamingResult as type information.
    [Preserve]
    public enum LivestreamingStartStatus : int
    {
        /// This member represents a successful start of the livestreaming session.
        [Description("SUCCESS")]
        [Preserve]
        Success,

        /// Represents that the LivestreamingStartStatus is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This member represents an error where the package was not set during the
        /// livestreaming start process.
        [Description("NO_PACKAGE_SET")]
        [Preserve]
        NoPackageSet,

        /// This member represents an error where Facebook Connect was not enabled
        /// during the livestreaming start process.
        [Description("NO_FB_CONNECT")]
        [Preserve]
        NoFbConnect,

        /// This member represents an error where a session ID was not provided during
        /// the livestreaming start process.
        [Description("NO_SESSION_ID")]
        [Preserve]
        NoSessionId,

        /// This member represents an error where required parameters were missing
        /// during the livestreaming start process.
        [Description("MISSING_PARAMETERS")]
        [Preserve]
        MissingParameters,

        
    }

}
