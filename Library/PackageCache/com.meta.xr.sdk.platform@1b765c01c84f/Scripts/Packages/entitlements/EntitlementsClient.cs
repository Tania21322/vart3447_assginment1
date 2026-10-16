// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<f94f04875bc495a444655063c0d998ca>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The Entitlement API is a crucial component of the Meta Quest Store's app
    /// verification process. It allows developers to check whether a user has
    /// purchased or obtained their app legitimately, ensuring that only authorized
    /// users can access the app. The API must be called within 10 seconds of the
    /// user launching the app and does not require internet connectivity. In the
    /// event of a failed entitlement check, developers are responsible for
    /// handling the error in their app code, such as by displaying an error
    /// message and quitting the app. This API plays a vital role in maintaining
    /// the security and integrity of the Meta Quest Store ecosystem. See more
    /// details
    /// [here](https://developer.oculus.com/documentation/android-apps/ps-entitlement-check/).
    public static partial class Entitlements
    {
        /// Returns whether the current user is entitled to the current app. The
        /// primary purpose of this function is to verify user access rights to the
        /// application, ensuring that the user is authorized to use it. See example
        /// usage [here](https://developer.oculus.com/documentation/android-apps/ps-
        /// entitlement-check/#entitlement).
        public static Request GetIsViewerEntitled()
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

                ulong requestHandle = PlatformClient.MakeRequest("entitlements", "get_is_viewer_entitled", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
