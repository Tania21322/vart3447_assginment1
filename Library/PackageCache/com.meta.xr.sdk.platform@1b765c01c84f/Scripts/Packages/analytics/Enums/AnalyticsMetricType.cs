// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<3e4569b4048ace529cd5b85d40f25953>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Present the type of the metric, which provide a flexible, versatile and
    /// comprehensive schema that allows the developers to understand the user
    /// behavior in-depth.
    [Preserve]
    public enum AnalyticsMetricType : int
    {
        /// Represents that the AnalyticsMetricType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Action: Action type, e.g. jump, wave, rush, double jump, viewership,
        /// browse, npc_talk_teemo.
        [Description("ACTION")]
        [Preserve]
        Action,

        /// Damage: Damage type, e.g. damage_gun, damage_hand, damage_punch_left.
        [Description("DAMAGE")]
        [Preserve]
        Damage,

        /// Death: Includes both Death and KOs: death type, e.g. death_gun_ak47,
        /// death_missle, death_fell, ko_otherplayer.
        [Description("DEATH")]
        [Preserve]
        Death,

        /// Discovery: Discovering or interacting with some element in the content,
        /// usually a positive thing.
        [Description("DISCOVERY")]
        [Preserve]
        Discovery,

        /// Distance: Distance type, e.g. distance_total, distance_walk, distance_run,
        /// distance_teleport.
        [Description("DISTANCE")]
        [Preserve]
        Distance,

        /// Friction: Hitting a point of friction. e.g. waiting_in_queue, preq_low_fqs.
        [Description("FRICTION")]
        [Preserve]
        Friction,

        /// Outcome: Outcome or result, e.g. defender_win, team_a_loss.
        [Description("OUTCOME")]
        [Preserve]
        Outcome,

        /// Progression: Progression type, e.g. level_100_reached,
        /// quest_savecat_complete, tutorial_1_reload_complete,
        /// tutorial_2_pulltrigger_complete.
        [Description("PROGRESSION")]
        [Preserve]
        Progression,

        /// Rate: Rate type, e.g. hit_rate, miss_rate, completion_rate.
        [Description("RATE")]
        [Preserve]
        Rate,

        /// Reward Currency: A currency reward that users can spend, e.g. gems,
        /// diamonds, coins, subscription extension.
        [Description("REWARD_CURRENCY")]
        [Preserve]
        RewardCurrency,

        /// Reward Items: Rewards that give users specific items, e.g. skins, weapon,
        /// badge.
        [Description("REWARD_ITEMS")]
        [Preserve]
        RewardItems,

        /// Reward Points: A points-based reward, e.g. EXP, honor points, influence
        /// points, level.
        [Description("REWARD_POINTS")]
        [Preserve]
        RewardPoints,

        /// Reward Status: Rewards of status, e.g. rank, badge, title.
        [Description("REWARD_STATUS")]
        [Preserve]
        RewardStatus,

        /// social: Social type, e.g. friend_request_sent, friends_made.
        [Description("SOCIAL")]
        [Preserve]
        Social,

        /// PREQ: PREQ type, e.g. low_fqs.
        [Description("PREQ")]
        [Preserve]
        Preq,

        /// Other: Other or Unclassified Tracking Metric Category.
        [Description("OTHER")]
        [Preserve]
        Other,

        
    }

}
