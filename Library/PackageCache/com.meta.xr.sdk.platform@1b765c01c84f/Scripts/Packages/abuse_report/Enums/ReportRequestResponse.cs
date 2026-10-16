// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<95ad67ab748d8bf4819a348778b5bfb6>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// An application user can report abusive behavior or content following the
    /// in-app reporting flow. This report request response represents the possible
    /// states that the app can respond to the platform notification, i.e., the
    /// running application indicates whether they want to show their in-app
    /// reporting flow or that they choose to ignore the request via
    /// AbuseReport.ReportRequestHandled.
    [Preserve]
    public enum ReportRequestResponse : int
    {
        /// Represents that the ReportRequestResponse is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This 'enum' member represents the response to the platform notification
        /// that the in-app reporting flow request is handled.
        [Description("HANDLED")]
        [Preserve]
        Handled,

        /// This 'enum' member represents the response to the platform notification
        /// that the in-app reporting flow request is not handled.
        [Description("UNHANDLED")]
        [Preserve]
        Unhandled,

        /// This 'enum' member represents the response to the platform notification
        /// that the in-app reporting flow is unavailable or non-existent.
        [Description("UNAVAILABLE")]
        [Preserve]
        Unavailable,

        
    }

}
