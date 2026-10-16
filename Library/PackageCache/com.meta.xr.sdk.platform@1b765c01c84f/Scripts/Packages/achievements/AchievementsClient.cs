// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<9338be9dc2e55c28d41d322a5974bbea>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The Achievements API enables developers to create engaging experiences by
    /// awarding trophies, badges, and awards for reaching goals. Users can see
    /// friends' achievements Achievements.GetAllDefinitions, fostering
    /// competition, and earned achievements are displayed in Meta Quest Home,
    /// showcasing progress Achievements.Unlock and driving engagement.
    public static partial class Achievements
    {
        /// Add 'count' to the achievement with the given name. This must be a COUNT
        /// achievement. The largest number that is supported by this method is the max
        /// value of a signed 64-bit integer. If the number is larger than that, it is
        /// clamped to that max value before being passed to the servers.
        public static Request<AchievementUpdate> AddCount(string name, ulong count)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "name", name },
                    { "count", count }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("achievements", "add_count", 1, jsonRequest, 0);
                return new Request<AchievementUpdate>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Unlock fields of a BITFIELD achievement.
        public static Request<AchievementUpdate> AddFields(string name, string fields)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "name", name },
                    { "fields", fields }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("achievements", "add_fields", 1, jsonRequest, 0);
                return new Request<AchievementUpdate>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieve all achievement definitions for the app, including their name,
        /// unlock requirements, and any additional details.
        public static Request<AchievementDefinitionList> GetAllDefinitions()
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("achievements", "get_all_definitions", 1, jsonRequest, 0);
                return new Request<AchievementDefinitionList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieve the progress for the user on all achievements in the app.
        public static Request<AchievementProgressList> GetAllProgress()
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("achievements", "get_all_progress", 1, jsonRequest, 0);
                return new Request<AchievementProgressList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieve the achievement definitions that match the specified names,
        /// including their name, unlock requirements, and any additional details.
        public static Request<AchievementDefinitionList> GetDefinitionsByName(string[] names)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "names", names }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("achievements", "get_definitions_by_name", 1, jsonRequest, 0);
                return new Request<AchievementDefinitionList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieve the user's progress on the achievements that match the specified
        /// names.
        public static Request<AchievementProgressList> GetProgressByName(string[] names)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "names", names }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("achievements", "get_progress_by_name", 1, jsonRequest, 0);
                return new Request<AchievementProgressList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Unlock the achievement with the given name. This can be of any achievement
        /// type: a simple unlock, count-based, or bitfield-based achievement. The Meta
        /// Quest Platform supports three types of achievements: simple, count and
        /// bitfield. Each achievement type has a different unlock mechanism. Simple
        /// achievements are all-or-nothing. They are unlocked by a single event or
        /// objective completion. For example, a simple achievement is unlocked when
        /// Frodo reaches Mount Doom. Count achievements are unlocked when a counter
        /// reaches a defined target. Define the AchievementDefinition.Target to reach
        /// that triggers the achievement. For example, a target achievement is
        /// unlocked when Darth Vader chokes 3 disappointing Imperial officers.
        /// Bitfield achievements are unlocked when a target number of bits in a
        /// bitfield are set. Define the AchievementDefinition.Target and
        /// AchievementDefinition.BitfieldLength that triggers the achievement. For
        /// example, a bitfield achievement is unlocked when Harry destroys 5 of the 7
        /// Horcruxes.
        public static Request<AchievementUpdate> Unlock(string name)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "name", name }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("achievements", "unlock", 1, jsonRequest, 0);
                return new Request<AchievementUpdate>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        

        public static Request<AchievementDefinitionList> GetNextAchievementDefinitionListPage(AchievementDefinitionList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextAchievementDefinitionListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("achievements", "get_achievement_definition_page_by_url", 1, jsonRequest, 0);
                return new Request<AchievementDefinitionList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<AchievementProgressList> GetNextAchievementProgressListPage(AchievementProgressList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextAchievementProgressListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("achievements", "get_achievement_progress_page_by_url", 1, jsonRequest, 0);
                return new Request<AchievementProgressList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<AchievementDefinitionList> GetPreviousAchievementDefinitionListPage(AchievementDefinitionList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousAchievementDefinitionListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("achievements", "get_achievement_definition_page_by_url", 1, jsonRequest, 0);
                return new Request<AchievementDefinitionList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<AchievementProgressList> GetPreviousAchievementProgressListPage(AchievementProgressList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousAchievementProgressListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("achievements", "get_achievement_progress_page_by_url", 1, jsonRequest, 0);
                return new Request<AchievementProgressList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }
    }
}
