// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<a2fe1e83ff1abf39273e7607454871a0>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    public static partial class UserDataStore
    {
        /// Delete an entry by a key from a private user data store.
        public static Request<UserDataStoreUpdateResponse> PrivateDeleteEntryByKey(UInt64 userId, string key)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() },
                    { "key", key }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("user_data_store", "private_delete_entry_by_key", 1, jsonRequest, 0);
                return new Request<UserDataStoreUpdateResponse>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Get entries from a private user data store.
        public static Request<Dictionary<string, string>> PrivateGetEntries(UInt64 userId)
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

                ulong requestHandle = PlatformClient.MakeRequest("user_data_store", "private_get_entries", 1, jsonRequest, 0);
                return new Request<Dictionary<string, string>>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Get an entry by a key from a private user data store.
        public static Request<Dictionary<string, string>> PrivateGetEntryByKey(UInt64 userId, string key)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() },
                    { "key", key }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("user_data_store", "private_get_entry_by_key", 1, jsonRequest, 0);
                return new Request<Dictionary<string, string>>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Write a single entry to a private user data store.
        public static Request<UserDataStoreUpdateResponse> PrivateWriteEntry(UInt64 userId, string key, string @value)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() },
                    { "key", key },
                    { "value", @value }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("user_data_store", "private_write_entry", 1, jsonRequest, 0);
                return new Request<UserDataStoreUpdateResponse>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Delete an entry by a key from a public user data store.
        public static Request<UserDataStoreUpdateResponse> PublicDeleteEntryByKey(UInt64 userId, string key)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() },
                    { "key", key }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("user_data_store", "public_delete_entry_by_key", 1, jsonRequest, 0);
                return new Request<UserDataStoreUpdateResponse>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Get entries from a public user data store.
        public static Request<Dictionary<string, string>> PublicGetEntries(UInt64 userId)
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

                ulong requestHandle = PlatformClient.MakeRequest("user_data_store", "public_get_entries", 1, jsonRequest, 0);
                return new Request<Dictionary<string, string>>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Get an entry by a key from a public user data store.
        public static Request<Dictionary<string, string>> PublicGetEntryByKey(UInt64 userId, string key)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() },
                    { "key", key }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("user_data_store", "public_get_entry_by_key", 1, jsonRequest, 0);
                return new Request<Dictionary<string, string>>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Write a single entry to a public user data store.
        public static Request<UserDataStoreUpdateResponse> PublicWriteEntry(UInt64 userId, string key, string @value)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "user_id", userId.ToString() },
                    { "key", key },
                    { "value", @value }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("user_data_store", "public_write_entry", 1, jsonRequest, 0);
                return new Request<UserDataStoreUpdateResponse>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
