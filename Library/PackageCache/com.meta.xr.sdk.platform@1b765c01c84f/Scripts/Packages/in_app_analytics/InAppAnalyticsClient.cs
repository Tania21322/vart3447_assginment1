// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<2457a771708877445e69975d528e51af>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// In-World Analytics provides an event processing system for tracking segment
    /// and metric events within an application. It supports batch processing,
    /// segment lifecycle management, and event counters for aggregating
    /// high-frequency metrics.
    public static partial class InAppAnalytics
    {
        /// Queues a segment event for batch processing. The event will be sent to the
        /// analytics backend in the next batch cycle.
        public static Request QueueSegmentEvent(SegmentEventInput @event)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "event", @event }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "queue_segment_event", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Opens a new segment and sends a START segment event. The segment is tracked
        /// internally and can be closed later by name.
        public static Request<SegmentEvent> OpenSegment(string segName)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "seg_name", segName }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "open_segment", 1, jsonRequest, 0);
                return new Request<SegmentEvent>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Closes a segment by name and sends an END segment event. The segment is
        /// removed from internal tracking.
        public static Request<SegmentEvent> CloseSegment(string segName)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "seg_name", segName }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "close_segment", 1, jsonRequest, 0);
                return new Request<SegmentEvent>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Closes all currently open segments and sends an END segment event for each
        /// one.
        public static Request<SegmentEvent[]> CloseAllOpenSegments()
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

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "close_all_open_segments", 1, jsonRequest, 0);
                return new Request<SegmentEvent[]>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns all currently active segments.
        public static Request<SegmentEvent[]> GetAllSegments()
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

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "get_all_segments", 1, jsonRequest, 0);
                return new Request<SegmentEvent[]>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Sends a simple metric event with a name and value. For high-frequency
        /// events, use the event counter APIs instead.
        public static Request SendEvent(string metricName, float @value)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "metric_name", metricName },
                    { "value", @value }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "send_event", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Queues a metric event for batch processing. The event will be sent to the
        /// analytics backend in the next batch cycle.
        public static Request QueueMetricEvent(MetricEventInput @event)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "event", @event }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "queue_metric_event", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Creates a new event counter with an initial value of 1. The counter is
        /// stored internally and can be incremented before sending. requiring explicit
        /// send_event_counter() calls. Defaults to false (auto-flush enabled).
        public static Request<MetricEvent> CreateEventCounter(string counterName, bool? manualFlush = null)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "counter_name", counterName },
                    { "manual_flush", manualFlush }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "create_event_counter", 1, jsonRequest, 0);
                return new Request<MetricEvent>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Increments an existing event counter by the specified amount.
        public static Request<MetricEvent> IncrementEventCounter(string counterName, float amount)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "counter_name", counterName },
                    { "amount", amount }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "increment_event_counter", 1, jsonRequest, 0);
                return new Request<MetricEvent>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Sends the event counter as a metric event and removes it from internal
        /// tracking.
        public static Request<MetricEvent> SendEventCounter(string counterName)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "counter_name", counterName }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "send_event_counter", 1, jsonRequest, 0);
                return new Request<MetricEvent>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Gets the current value of an event counter without sending it.
        public static Request<MetricEvent> GetEventCounter(string counterName)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "counter_name", counterName }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "get_event_counter", 1, jsonRequest, 0);
                return new Request<MetricEvent>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns all event counter names currently being tracked.
        public static Request<EventCounterNames> GetAllEventCounterNames()
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

                ulong requestHandle = PlatformClient.MakeRequest("in_app_analytics", "get_all_event_counter_names", 1, jsonRequest, 0);
                return new Request<EventCounterNames>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
