// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<11b8f2440bc31f7611a399c98121cdf2>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// Push notification PushNotificationResult provides a simple and efficient
    /// way for devices to register for and receive push notifications, enabling
    /// developers to build engaging and interactive applications that deliver
    /// timely updates and alerts to users.
    public static partial class PushNotification
    {
        /// Register the device to receive push notification. The registered
        /// notification id can be fetched by PushNotificationResult.Id.
        public static Request<PushNotificationResult> Register()
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

                ulong requestHandle = PlatformClient.MakeRequest("push_notification", "register", 1, jsonRequest, 0);
                return new Request<PushNotificationResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Unregister the device from receiving push notifications. Returns a boolean
        /// to indicate successful/unsuccessful unregistration.
        public static Request<bool> Unregister()
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

                ulong requestHandle = PlatformClient.MakeRequest("push_notification", "unregister", 1, jsonRequest, 0);
                return new Request<bool>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
