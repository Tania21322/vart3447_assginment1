// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<929d072feadd8708364ea90447ba7d55>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Present the type of the segment, which enables aggregation and
    /// benchmarking.
    [Preserve]
    public enum AnalyticsSegmentType : int
    {
        /// Represents that the SegmentType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Loading segments, e.g. loading_game_start, loading_join_match.
        [Description("LOADING")]
        [Preserve]
        Loading,

        /// Lobby segments, e.g. lobby_control_room, lobby_social_area.
        [Description("LOBBY")]
        [Preserve]
        Lobby,

        /// Main segments, it represents the main content experience, e.g. arena_fight,
        /// combat_round, launch_mission, level_10.
        [Description("MAIN")]
        [Preserve]
        Main,

        /// Menu segments, e.g. menu_seat_selection, menu_song_selection.
        [Description("MENU")]
        [Preserve]
        Menu,

        /// (In-content) store segments, e.g. store_cosmetic, store_membership.
        [Description("STORE")]
        [Preserve]
        Store,

        /// Tutorial segments, e.g. tutorial_1_grab, tutorial_2_drop.
        [Description("TUTORIAL")]
        [Preserve]
        Tutorial,

        /// Cutscene segments, e.g. cutscene_npc_dialogue, cutscene_batman_reveal.
        [Description("CUTSCENE")]
        [Preserve]
        Cutscene,

        /// Other types.
        [Description("OTHER")]
        [Preserve]
        Other,

        
    }

}
