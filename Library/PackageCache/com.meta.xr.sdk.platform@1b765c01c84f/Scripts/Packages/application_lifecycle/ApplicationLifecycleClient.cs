// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<1cda2cf6ace5bf87621cc764dabd0f18>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// This ApplicationLifecycle API provides methods for managing the lifecycle
    /// of an application, including retrieving information about how the
    /// application was started, such as the type of intent
    /// LaunchDetails.LaunchType, logging the results of deeplinking attempts,
    /// whether it was LaunchResult.Success or not, and handling changes to the
    /// launch intent.
    public static partial class ApplicationLifecycle
    {
        /// This event is triggered when a launch intent is received, whether it's a
        /// cold or warm start. The payload contains the type of intent that was
        /// received. To obtain additional details, you should call the
        /// ApplicationLifecycle.GetLaunchDetails function.
        
        public static void SetLaunchIntentChangedNotificationCallback(Message<String>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("application_lifecycle", "launch_intent_changed", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<String>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        /// Returns information about how the application was started. This function
        /// provides details about the launch intent, such as the type of intent
        /// LaunchDetails.LaunchType and any additional data that was passed along with
        /// it. By calling this function, you can gain insight into how your
        /// application was launched and take appropriate action based on that
        /// information.
        public static Request<LaunchDetails> GetLaunchDetailsRequest()
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

                ulong requestHandle = PlatformClient.MakeRequest("application_lifecycle", "get_launch_details", 1, jsonRequest, 0);
                return new Request<LaunchDetails>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Logs if the user successfully deeplinked to a destination. This function
        /// takes two parameters: a string tracking ID and a launch result. The
        /// tracking ID is used to identify the specific deeplink attempt, while the
        /// launch result indicates whether the deeplink was LaunchResult.Success or
        /// not. By logging this information, you can track the effectiveness of your
        /// deeplinking efforts and make adjustments as needed.
        public static Request LogDeeplinkResultRequest(string trackingId, LaunchResult result)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "tracking_id", trackingId },
                    { "result", result.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("application_lifecycle", "log_deeplink_result", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
