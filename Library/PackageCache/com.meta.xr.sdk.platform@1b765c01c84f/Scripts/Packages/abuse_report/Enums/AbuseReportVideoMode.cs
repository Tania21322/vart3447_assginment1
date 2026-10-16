// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<724ec48512452e156d8c229a4dabde94>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Determines under what circumstances the frontend UI will attempt to gather
    /// video evidence to support a report, and the object_type is defined in
    /// AdvancedAbuseReportOptions.ObjectType. This setting is crucial in ensuring
    /// that the reporting process is thorough and accurate, as video evidence can
    /// provide valuable context and proof of misconduct.
    [Preserve]
    public enum AbuseReportVideoMode : int
    {
        /// Represents that the AbuseReportVideoMode is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// The UI will collect video evidence if the object_type supports it, the
        /// object_type is defined in AdvancedAbuseReportOptions.ObjectType.
        [Description("COLLECT")]
        [Preserve]
        Collect,

        /// The UI will try to collect video evidence if the object_type supports it,
        /// but will allow the user to skip that step if they wish.
        [Description("OPTIONAL")]
        [Preserve]
        Optional,

        /// The UI will not collect video evidence.
        [Description("SKIP")]
        [Preserve]
        Skip,

        
    }

}
