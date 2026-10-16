// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<8205fe490c0e6fd47fec90b9960a0d54>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// Users are divided into three categories based on their age:
    /// -AccountAgeCategory.Ch: Child age group for users between the ages of 10-12
    /// (or applicable age in user's region). -AccountAgeCategory.Tn: Teenage age
    /// group for users between the ages of 13-17 (or applicable age in user's
    /// region). -AccountAgeCategory.Ad: Adult age group for users ages 18 and up
    /// (or applicable age in user's region).
    public static partial class UserAgeCategory
    {
        /// Retrieve the user age category for the current user. It can be used in
        /// UserAccountAgeCategory.AgeCategory
        public static Request<UserAccountAgeCategory> Get()
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

                ulong requestHandle = PlatformClient.MakeRequest("user_age_category", "get", 1, jsonRequest, 0);
                return new Request<UserAccountAgeCategory>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Report the current user's age category to Meta.
        public static Request Report(AppAgeCategory age_category)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "age_category", age_category.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("user_age_category", "report", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
