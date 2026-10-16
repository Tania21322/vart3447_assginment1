// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<144182c7f16e1521b6b594442573feee>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Present the signal of user engagement for the segment.
    [Preserve]
    public enum AnalyticsSegmentEventType : int
    {
        /// Represents that the EventType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Marks the start of a segment, each segment should only have ONE 'start'
        /// type. Default duration_s of 'start' events should be 0.
        [Description("START")]
        [Preserve]
        Start,

        /// Marks the update of a segment, this is more generic, use 'change_setting'
        /// and 'heartbeat' to be more specific.
        [Description("UPDATE")]
        [Preserve]
        Update,

        /// Marks the pause of a segment.
        [Description("PAUSE")]
        [Preserve]
        Pause,

        /// Marks the resume of a segment.
        [Description("RESUME")]
        [Preserve]
        Resume,

        /// Marks the end of a segment, each segment should only have ONE 'end' type. A
        /// segment is completed when an 'end' event is received.
        [Description("END")]
        [Preserve]
        End,

        /// Alternative to 'end', 'end_abort' indicates that the user has intentionally
        /// terminated the segment. Segments marked with 'end_abort' are excluded from
        /// completion rate calculations. For instance, if a user skips the tutorial
        /// segment and begins a new main segment, use 'end_abort' for the tutorial
        /// segment to treat it as incomplete.
        [Description("END_ABORT")]
        [Preserve]
        EndAbort,

        /// Marks the change setting of a segment.
        [Description("CHANGE_SETTING")]
        [Preserve]
        ChangeSetting,

        /// Marks the heartbeat, use this event to more accurately capture the
        /// duration.
        [Description("HEARTBEAT")]
        [Preserve]
        Heartbeat,

        
    }

}
