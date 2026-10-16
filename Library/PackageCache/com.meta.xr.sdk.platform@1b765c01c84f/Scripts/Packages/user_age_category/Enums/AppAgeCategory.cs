// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<8560e3b14c35a4b65c47b3f5906a0fb4>>
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
    public enum AppAgeCategory : int
    {
        /// Represents that the AppAgeCategory is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Child age group for users between the ages of 10-12 (or applicable age in
        /// user's region).
        [Description("CH")]
        [Preserve]
        Ch,

        /// Non-child age group for users ages 13 and up (or applicable age in user's
        /// region).
        [Description("NCH")]
        [Preserve]
        Nch,

        
    }

}
