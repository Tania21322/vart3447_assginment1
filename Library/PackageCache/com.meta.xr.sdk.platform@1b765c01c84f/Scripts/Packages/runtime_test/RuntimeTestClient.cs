// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<46d8121242a3840179bda9504a2c23f5>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// Dummy Package to test the Runtime app E2E
    public static partial class RuntimeTest
    {
        /// Method to test the serviceConnection.makeRequest() method
        public static Request<string> GetMakeRequestResponse()
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

                ulong requestHandle = PlatformClient.MakeRequest("runtime_test", "get_make_request_response", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Method to test the case where serviceConnection.makeRequest() method throws
        /// an error
        public static Request<string> MakeRequestThrowsError()
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

                ulong requestHandle = PlatformClient.MakeRequest("runtime_test", "make_request_throws_error", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Method to test the serviceConnection.makeSession() method
        
        public static void SetGetMakeSessionResponseNotificationCallback(Message<String>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("runtime_test", "get_make_session_response", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<String>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        
    }
}
