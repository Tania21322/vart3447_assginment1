// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<c30f01f3f336cf0977d9ff60ccbcfb04>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The Abuse Report API provides a way for users to report abusive behavior or
    /// content within the platform. It allows developers to submit reports for
    /// various types of content, including users AbuseReportType.User, or an
    /// object/content AbuseReportType.Object.
    public static partial class AbuseReport
    {
        /// Launch the flow for reporting a user or piece of content. This advanced
        /// version of the report endpoint allows for submitting reports for worlds and
        /// other content, as well as customizing the way the report flow operates.
        /// Returns the ID of the filed report upon success.
        public static Request<LaunchReportFlowResult> LaunchAdvancedReportFlow(string contentId, AdvancedAbuseReportOptions abuseReportOptions = null)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "content_id", contentId },
                    { "abuse_report_options", abuseReportOptions }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("abuse_report", "launch_advanced_report_flow", 1, jsonRequest, 0);
                return new Request<LaunchReportFlowResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// The currently running application has indicated they want to show their in-
        /// app reporting flow or that they choose to ignore the request.
        public static Request ReportRequestHandled(ReportRequestResponse response)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "response", response.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("abuse_report", "report_request_handled", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// The user has tapped the report button in the panel that appears after
        /// pressing the Oculus button.
        
        public static void SetReportButtonPressedNotificationCallback(Message<String>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("abuse_report", "report_button_pressed", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<String>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        
    }
}
