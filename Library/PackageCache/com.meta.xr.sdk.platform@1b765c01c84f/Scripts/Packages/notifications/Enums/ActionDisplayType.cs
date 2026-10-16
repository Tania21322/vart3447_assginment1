// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<f4a14339d3f2fea25cd701ce9241472f>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Enum for notification action display types. These values determine how
    /// notification actions are displayed.
    [Preserve]
    public enum ActionDisplayType : int
    {
        /// Actions are displayed with icons that can be colored.
        [Description("ICONABLE")]
        [Preserve]
        Iconable,

        /// Actions are displayed with icons that are colorless.
        [Description("ICONABLE_COLORLESS")]
        [Preserve]
        IconableColorless,

        /// Actions are displayed as text only, without icons.
        [Description("TEXT_ONLY")]
        [Preserve]
        TextOnly,

        /// Represents that the ActionDisplayType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        
    }

}
