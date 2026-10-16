// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<c0dc7becd54abf998e7c517c55193e1f>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Represents the type of a segment in the in-app analytics system.
    [Preserve]
    public enum SegmentType : int
    {
        /// Represents that the SegmentType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// A change setting segment.
        [Description("CHANGE_SETTING")]
        [Preserve]
        ChangeSetting,

        /// A cutscene segment.
        [Description("CUTSCENE")]
        [Preserve]
        Cutscene,

        /// An enter queue segment.
        [Description("ENTER_QUEUE")]
        [Preserve]
        EnterQueue,

        /// A lobby segment.
        [Description("LOBBY")]
        [Preserve]
        Lobby,

        /// A main gameplay segment.
        [Description("MAIN")]
        [Preserve]
        Main,

        /// A menu segment.
        [Description("MENU")]
        [Preserve]
        Menu,

        /// A store segment.
        [Description("STORE")]
        [Preserve]
        Store,

        /// A tutorial segment.
        [Description("TUTORIAL")]
        [Preserve]
        Tutorial,

        /// A prerequisite segment.
        [Description("PREQ")]
        [Preserve]
        Preq,

        /// Other segment type not covered by the predefined types.
        [Description("OTHER")]
        [Preserve]
        Other,

        
    }

}
