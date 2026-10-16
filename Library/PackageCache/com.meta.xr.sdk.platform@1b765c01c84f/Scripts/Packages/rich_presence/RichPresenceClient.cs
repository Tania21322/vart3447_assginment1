// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<6bc12d1c90021147b68fdc0cfde43b45>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// Rich Presence has been deprecated in favor of
    /// [Group Presence](https://developers.meta.com/horizon/documentation/unity/ps-group-presence-overview).
    public static partial class RichPresence
    {
        public static Request Clear()
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

                ulong requestHandle = PlatformClient.MakeRequest("group_presence", "clear", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets all the Destination that the presence can be set to
        public static Request<DestinationList> GetDestinations()
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

                ulong requestHandle = PlatformClient.MakeRequest("group_presence", "get_destinations", 1, jsonRequest, 0);
                return new Request<DestinationList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request Set(RichPresenceOptions richPresenceOptions)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "rich_presence_options", richPresenceOptions }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("group_presence", "set", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request SetDestination(string apiName)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "api_name", apiName }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("group_presence", "set_destination", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request SetIsJoinable(bool isJoinable)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "is_joinable", isJoinable }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("group_presence", "set_is_joinable", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request SetLobbySession(string id)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "id", id }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("group_presence", "set_lobby_session", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request SetMatchSession(string id)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "id", id }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("group_presence", "set_match_session", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        

        public static Request<DestinationList> GetNextDestinationListPage(DestinationList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextDestinationListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("group_presence", "get_destination_page_by_url", 1, jsonRequest, 0);
                return new Request<DestinationList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<DestinationList> GetPreviousDestinationListPage(DestinationList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousDestinationListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("group_presence", "get_destination_page_by_url", 1, jsonRequest, 0);
                return new Request<DestinationList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }
    }
}
