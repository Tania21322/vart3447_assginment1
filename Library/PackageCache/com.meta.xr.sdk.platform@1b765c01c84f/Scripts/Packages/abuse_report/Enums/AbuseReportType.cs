// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<38e222a3d072b7a4926775efaa71b8aa>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Represents the type of abuse report, can be categorized as either
    /// concerning a user, an object, or being unknown. It can be specified as a
    /// field in the AbuseReportOptions.ReportType option.
    [Preserve]
    public enum AbuseReportType : int
    {
        /// Represents that the AbuseReportType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This refers to reports that are related to non-user entities, such as a
        /// virtual environment or an inanimate object within a platform. An example
        /// provided is a report concerning a "world," which could be a virtual space
        /// or environment.
        [Description("OBJECT")]
        [Preserve]
        Object,

        /// This category is used for reports that directly involve a user's actions or
        /// behavior. This could include reports on harassment, inappropriate behavior,
        /// or other violations that are directly linked to a user's conduct on a
        /// platform.
        [Description("USER")]
        [Preserve]
        User,

        
    }

}
