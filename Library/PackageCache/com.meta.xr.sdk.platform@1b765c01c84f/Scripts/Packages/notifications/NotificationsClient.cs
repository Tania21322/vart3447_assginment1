// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<5c9c3f5afd08572e85b78d291b0c1a73>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The Notifications class provides a way to manage and display notifications
    /// to the user. Notifications can be used to inform the user of important
    /// events, such as new messages, friend requests, or updates to installed
    /// apps. See more info about Platform Solutions
    /// [here](https://developer.oculus.com/documentation/unity/ps-platform-intro/).
    public static partial class Notifications
    {
        /// Triggers event-based notifications to be delivered to users based on events
        /// defined in your code.
        public static Request TriggerNotification(UInt64 request_id, UInt64[] userIDs)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "request_id", request_id.ToString() },
                    { "user_ids", Array.ConvertAll(userIDs, id => id.ToString()) }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("notifications", "trigger_notification", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Triggers a device notification to show the notification toast and feed the
        /// notification to notification feed.
        public static Request DeviceNotification(DeviceNotificationConfig deviceNotificationConfig)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "device_notification_config", deviceNotificationConfig }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("notifications", "device_notification", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
