// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<68321d465982ddd85a2039be888ef2c0>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The Challenges API enhances social interactions in your app, which use
    /// GroupPresence.SetDestination to create shareable links for score-based
    /// competition. Players can repeatedly challenge others, get to know them, and
    /// compete asynchronously. Challenges can be ranked by highest/lowest scores
    /// within a time period. Leaderboard-integrated apps get Challenges for free,
    /// accessible through the Scoreboards UI. Players can create and invite others
    /// to Challenges via the Challenges app.
    public static partial class Challenges
    {
        public static Request<Challenge> Create(string leaderboardName, ChallengeOptions challengeOptions)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "leaderboard_name", leaderboardName },
                    { "challenge_options", challengeOptions }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "create", 1, jsonRequest, 0);
                return new Request<Challenge>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// If the current user has the necessary permissions, they can decline a
        /// challenge by providing the challenge ID, which can be obtained using
        /// Challenge.Id.
        public static Request<Challenge> DeclineInvite(UInt64 challengeID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_id", challengeID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "decline_invite", 1, jsonRequest, 0);
                return new Request<Challenge>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request Delete(UInt64 challengeID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_id", challengeID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "delete", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets detailed information for a single challenge by providing the challenge
        /// ID, which can be retrieved by calling Challenge.Id.
        public static Request<Challenge> Get(UInt64 challengeID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_id", challengeID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "get", 1, jsonRequest, 0);
                return new Request<Challenge>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieves a list of entries for a specific challenge, with options to
        /// filter and limit the results. By providing the challengeID, you can specify
        /// which challenge's entries you want to retrieve. The limit parameter allows
        /// you to control the number of entries returned. The filter parameter enables
        /// you to refine the results to only include entries from users who are
        /// bidirectional followers. The startAt parameter allows you to define whether
        /// to center the query on the user or start at the top of the challenge.
        public static Request<ChallengeEntryList> GetEntries(UInt64 challengeID, int limit, LeaderboardFilterType filter, LeaderboardStartAt startAt)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_id", challengeID.ToString() },
                    { "limit", limit },
                    { "filter", filter.GetDescription() },
                    { "start_at", startAt.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "get_entries", 1, jsonRequest, 0);
                return new Request<ChallengeEntryList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns a list of entries for a specific challenge, starting from a
        /// specified rank. By providing the challengeID and rank, you can specify
        /// which challenge's entries you want to retrieve and where to start the
        /// query. The limit parameter allows you to control the number of entries
        /// returned.
        public static Request<ChallengeEntryList> GetEntriesAfterRank(UInt64 challengeID, int limit, ulong afterRank)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_id", challengeID.ToString() },
                    { "limit", limit },
                    { "after_rank", afterRank }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "get_entries_after_rank", 1, jsonRequest, 0);
                return new Request<ChallengeEntryList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieves a list of challenge entries for a specific set of user IDs, with
        /// options to filter and limit the results. This method is useful for
        /// retrieving a list of challenge entries for a specific set of users,
        /// allowing you to display their progress and rankings within the challenge.
        public static Request<ChallengeEntryList> GetEntriesByIds(UInt64 challengeID, int limit, LeaderboardStartAt startAt, UInt64[] userIDs)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_id", challengeID.ToString() },
                    { "limit", limit },
                    { "start_at", startAt.GetDescription() },
                    { "user_ids", Array.ConvertAll(userIDs, id => id.ToString()) }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "get_entries_by_ids", 1, jsonRequest, 0);
                return new Request<ChallengeEntryList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns a list of challenges that match the specified options. The
        /// ChallengeOptions parameter allows you to specify the criteria for the
        /// challenges you want to retrieve. The limit parameter allows you to control
        /// the number of challenges returned.
        public static Request<ChallengeList> GetList(ChallengeOptions challengeOptions, int limit)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_options", challengeOptions },
                    { "limit", limit }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "get_list", 1, jsonRequest, 0);
                return new Request<ChallengeList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Invites users to a challenge by providing the challenge ID and a list of
        /// user IDs to invite.
        public static Request<Challenge> InviteUsers(UInt64 challengeID, UInt64[] userIDs)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_id", challengeID.ToString() },
                    { "user_ids", Array.ConvertAll(userIDs, id => id.ToString()) }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "invite_users", 1, jsonRequest, 0);
                return new Request<Challenge>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// If the current user has the necessary permissions to join, participate in a
        /// challenge by providing the challenge ID, which can be retrieved using
        /// Challenge.Id.
        public static Request<Challenge> Join(UInt64 challengeID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_id", challengeID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "join", 1, jsonRequest, 0);
                return new Request<Challenge>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// If the current user has the necessary permissions, they can leave a
        /// challenge by providing the challenge ID, which can be obtained using
        /// Challenge.Id.
        public static Request<Challenge> Leave(UInt64 challengeID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_id", challengeID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "leave", 1, jsonRequest, 0);
                return new Request<Challenge>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<Challenge> UpdateInfo(UInt64 challengeID, ChallengeOptions challengeOptions)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_id", challengeID.ToString() },
                    { "challenge_options", challengeOptions }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("challenges", "update_info", 1, jsonRequest, 0);
                return new Request<Challenge>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        

        public static Request<ChallengeList> GetNextChallenges(ChallengeList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextChallenges: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("challenges", "get_challenge_page_by_url", 1, jsonRequest, 0);
                return new Request<ChallengeList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<ChallengeEntryList> GetNextEntries(ChallengeEntryList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextEntries: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("challenges", "get_challenge_entry_page_by_url", 1, jsonRequest, 0);
                return new Request<ChallengeEntryList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<ChallengeList> GetPreviousChallenges(ChallengeList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousChallenges: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("challenges", "get_challenge_page_by_url", 1, jsonRequest, 0);
                return new Request<ChallengeList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<ChallengeEntryList> GetPreviousEntries(ChallengeEntryList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousEntries: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("challenges", "get_challenge_entry_page_by_url", 1, jsonRequest, 0);
                return new Request<ChallengeEntryList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }
    }
}
