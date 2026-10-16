// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<e931c1d48fdddaad586d2b46c65e2bb7>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// Consent APIs provide platform methods to check status of different consent
    /// types. And also provides way to launch a consent flow if required.
    public static partial class Consent
    {
        /// Returns the current status of a consent.
        public static Request<ConsentStatusResult[]> GetConsentStatus(string consentFlowName, string version = null, Dictionary<string, string> extraParams = null)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "consent_flow_name", consentFlowName },
                    { "version", version },
                    { "extra_params", extraParams }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("consent", "get_consent_status", 1, jsonRequest, 0);
                return new Request<ConsentStatusResult[]>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns the current status of a consent.
        public static Request<ConsentLaunchResult> LaunchConsentIfRequired(string consentFlowName, string version = null, Dictionary<string, string> extraParams = null)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "consent_flow_name", consentFlowName },
                    { "version", version },
                    { "extra_params", extraParams }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("consent", "launch_consent_if_required", 1, jsonRequest, 0);
                return new Request<ConsentLaunchResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
