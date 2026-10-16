// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<3128319238ab44365eacd9926783515b>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The party API allows you to retrieve information about parties and manage
    /// various interactions between users and their respective Party. Calling
    /// Parties.GetCurrent with a party ID will grab the Party object and the
    /// associated metadata.
    public static partial class Parties
    {
        /// Create a new party and adds the caller to it.
        public static Request<PartyID> Create()
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

                ulong requestHandle = PlatformClient.MakeRequest("parties", "create", 1, jsonRequest, 0);
                return new Request<PartyID>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Invite all users in your party to an application.
        public static Request<PartyID> GatherInApplication(UInt64 partyId, UInt64 appId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "party_id", partyId.ToString() },
                    { "app_id", appId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("parties", "gather_in_application", 1, jsonRequest, 0);
                return new Request<PartyID>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Allows parties to be loaded.
        public static Request<Party> Get(UInt64 partyId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "party_id", partyId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("parties", "get", 1, jsonRequest, 0);
                return new Request<Party>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Loads the party the specified user is in. Privacy settings are applied so
        /// party may not be allowed to be loaded.
        public static Request<Party> GetCurrentForUser(UInt64 userId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("parties", "get_current_for_user", 1, jsonRequest, 0);
                return new Request<Party>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Invite another user to an existing party.
        public static Request<PartyID> Invite(UInt64 partyId, UInt64 userId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "party_id", partyId.ToString() },
                    { "user_id", userId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("parties", "invite", 1, jsonRequest, 0);
                return new Request<PartyID>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Add the caller to an existing party.
        public static Request<PartyID> Join(UInt64 partyId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "party_id", partyId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("parties", "join", 1, jsonRequest, 0);
                return new Request<PartyID>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Remove the caller from an existing party
        public static Request Leave(UInt64 partyId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "party_id", partyId.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("parties", "leave", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Indicates that party has been updated. This will return a
        /// PartyUpdateNotification object.
        
        public static void SetPartyUpdateNotificationCallback(Message<PartyUpdateNotification>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("parties", "party_update", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<PartyUpdateNotification>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        /// Returns a handle to a shared memory object that can be used to receive pcm
        /// data in passthrough mode.
        public static Request<int> PluginGetSharedMemHandle()
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

                ulong requestHandle = PlatformClient.MakeRequest("parties", "plugin_get_shared_mem_handle", 1, jsonRequest, 0);
                return new Request<int>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Internal use only.  Noop if called from a 3P app context. Gets party chat
        /// microphone mute status.
        public static Request<VoipMuteState> PluginGetVoipMicrophoneMuted()
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

                ulong requestHandle = PlatformClient.MakeRequest("parties", "plugin_get_voip_microphone_muted", 1, jsonRequest, 0);
                return new Request<VoipMuteState>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Internal use only.  Noop if called from a 3P app context. Returns true if
        /// we are in voip passthrough mode.
        public static Request<bool> PluginGetVoipPassthrough()
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

                ulong requestHandle = PlatformClient.MakeRequest("parties", "plugin_get_voip_passthrough", 1, jsonRequest, 0);
                return new Request<bool>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Internal use only.  Noop if called from a 3P app context. Gets party chat
        /// status.
        public static Request<SystemVoipStatus> PluginGetVoipStatus()
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

                ulong requestHandle = PlatformClient.MakeRequest("parties", "plugin_get_voip_status", 1, jsonRequest, 0);
                return new Request<SystemVoipStatus>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
