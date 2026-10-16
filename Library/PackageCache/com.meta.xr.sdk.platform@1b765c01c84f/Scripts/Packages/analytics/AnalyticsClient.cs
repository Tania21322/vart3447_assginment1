// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<fa7864095d56bb97a3c93db33c75ae90>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The Analytics class provides the function for sending the analytic data.
    public static partial class Analytics
    {
        /// Send the analytics metric event with details fill into
        /// AnalyticsMetricEvent. See details on AnalyticsMetricEvent.
        public static Request SendMetricEvent(AnalyticsMetricEvent analyticsMetricEvent)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "analytics_metric_event", analyticsMetricEvent }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("analytics", "send_metric_event", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Send the analytics segment event with details fill into
        /// AnalyticsSegmentEvent. See details on AnalyticsSegmentEvent.
        public static Request SendSegmentEvent(AnalyticsSegmentEvent analyticsSegmentEvent)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "analytics_segment_event", analyticsSegmentEvent }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("analytics", "send_segment_event", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
