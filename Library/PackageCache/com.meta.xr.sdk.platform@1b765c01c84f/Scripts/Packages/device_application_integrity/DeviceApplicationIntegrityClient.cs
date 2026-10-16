// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<a7cfdbf0031db6b45cb47abb0f890e8d>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The Device Application Integrity API is a key function for developers
    /// committed to protecting their applications from unauthorized modifications
    /// and potential security breaches. Leveraging this API can greatly enhance
    /// the security of your applications, ensuring a seamless and safe user
    /// experience. See more details
    /// [here](https://developer.oculus.com/documentation/unity/ps-attestation-api/).
    public static partial class DeviceApplicationIntegrity
    {
        /// Returns Device and Application Integrity Attestation JSON Web Token. The
        /// token has format of header.claims.signature encoded in base64. Header
        /// contains algorithm type (PS256) and token type (JWT). See more details
        /// [here](https://developer.oculus.com/documentation/{{platform_doc_path}}/ps-
        /// attestation-api/#how-does-this-work).
        public static Request<string> GetAttestationToken(string challengeNonce)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_nonce", challengeNonce }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("device_application_integrity", "get_attestation_token", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns Device and Application Integrity Attestation JSON Web Token. The
        /// token has format of header.claims.signature encoded in base64. Header
        /// contains algorithm type (PS256) and token type (JWT). See more details
        /// [here](https://developer.oculus.com/documentation/{{platform_doc_path}}/ps-
        /// attestation-api/#how-does-this-work).
        public static Request<string> GetIntegrityToken(string challenge_nonce)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "challenge_nonce", challenge_nonce }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("device_application_integrity", "get_integrity_token", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
