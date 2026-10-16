// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<3cd830b97ea0fc1298a43cc044984653>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// The age category in a Meta account is used to determine eligibility for
    /// certain features and services. This information is important for ensuring
    /// that users are able to access the appropriate content and functionality
    /// based on their age. The values are used in UserAccountAgeCategory API. See
    /// more details
    /// [here](https://developer.oculus.com/documentation/android-apps/ps-get-age-category-api).
    [Preserve]
    public enum AccountAgeCategory : int
    {
        /// Represents that the AccountAgeCategory is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Child age group for users between the ages of 10-12 (or applicable age in
        /// user's region).
        [Description("CH")]
        [Preserve]
        Ch,

        /// Teenage age group for users between the ages of 13-17 (or applicable age in
        /// user's region).
        [Description("TN")]
        [Preserve]
        Tn,

        /// Adult age group for users ages 18 and up (or applicable age in user's
        /// region).
        [Description("AD")]
        [Preserve]
        Ad,

        
    }

}
