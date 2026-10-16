// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<31de05ef62e01d55732c220077116668>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The livestreaming API provides a way to receive notifications
    /// Livestreaming.StatusChange when the streaming session changes, such as when
    /// the user starts or stops streaming, or when the streaming status changes.
    /// This allows developers to respond to changes in the streaming session in
    /// real-time, providing a seamless and engaging experience for users.
    public static partial class Livestreaming
    {
        /// Query if an application is allowed to LS. Only permissioned apps can call
        /// this.
        public static Request<LivestreamingApplicationStatus> IsAllowedForApplication(string packageName)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "package_name", packageName }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("livestreaming", "is_allowed_for_application", 1, jsonRequest, 0);
                return new Request<LivestreamingApplicationStatus>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Starts a livestream to the party. Only permissioned apps can call this.
        public static Request<LivestreamingStartResult> StartPartyStream()
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

                ulong requestHandle = PlatformClient.MakeRequest("livestreaming", "start_party_stream", 1, jsonRequest, 0);
                return new Request<LivestreamingStartResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Starts a livestream to Facebook. Only permissioned apps can call this.
        public static Request<LivestreamingStartResult> StartStream(LivestreamingAudience audience, LivestreamingMicrophoneStatus micStatus)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "audience", audience.GetDescription() },
                    { "mic_status", micStatus.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("livestreaming", "start_stream", 1, jsonRequest, 0);
                return new Request<LivestreamingStartResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Stops a currently running party livestream. Only permissioned apps can call
        /// this.
        public static Request StopPartyStream()
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

                ulong requestHandle = PlatformClient.MakeRequest("livestreaming", "stop_party_stream", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Stops a currently running livestream. Only permissioned apps can call this.
        public static Request<LivestreamingVideoStats> StopStream()
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

                ulong requestHandle = PlatformClient.MakeRequest("livestreaming", "stop_stream", 1, jsonRequest, 0);
                return new Request<LivestreamingVideoStats>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Updates mic status of a livestream. Only permissioned apps can call this.
        public static Request UpdateMicStatus(LivestreamingMicrophoneStatus micStatus)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "mic_status", micStatus.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("livestreaming", "update_mic_status", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Indicates that the livestreaming session has been updated. You can use this
        /// information to throttle your game performance or increase CPU/GPU
        /// performance.  Use LivestreamingStatus to extract the updated livestreaming
        /// status.
        
        public static void SetStatusChangeNotificationCallback(Message<LivestreamingStatus>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("livestreaming", "status_change", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<LivestreamingStatus>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        
    }
}
