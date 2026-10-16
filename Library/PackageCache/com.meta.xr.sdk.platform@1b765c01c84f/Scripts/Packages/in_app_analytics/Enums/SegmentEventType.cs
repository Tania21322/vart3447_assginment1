// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<7e30b1075f53f564477cd770def3d700>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Represents the type of event within a segment in the in-app analytics
    /// system.
    [Preserve]
    public enum SegmentEventType : int
    {
        /// Represents that the SegmentEventType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// The segment has started.
        [Description("START")]
        [Preserve]
        Start,

        /// The segment has ended.
        [Description("END")]
        [Preserve]
        End,

        /// The segment has been updated.
        [Description("UPDATE")]
        [Preserve]
        Update,

        /// The segment has been paused.
        [Description("PAUSE")]
        [Preserve]
        Pause,

        /// The segment has been resumed.
        [Description("RESUME")]
        [Preserve]
        Resume,

        /// The segment has been aborted.
        [Description("END_ABORT")]
        [Preserve]
        EndAbort,

        /// A setting change occurred within the segment.
        [Description("CHANGE_SETTING")]
        [Preserve]
        ChangeSetting,

        /// A periodic heartbeat event within the segment.
        [Description("HEARTBEAT")]
        [Preserve]
        Heartbeat,

        
    }

}
