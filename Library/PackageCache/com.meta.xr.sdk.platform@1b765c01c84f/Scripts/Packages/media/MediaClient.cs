// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<63bad17f33a4d617d2995a8a75ae9bdd>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The media API provides a convenient and seamless way to share local media
    /// files, such as photos (currently the only supported type), directly to
    /// Facebook from within your application. This allows users to easily share
    /// their favorite moments and memories with their friends and family on the
    /// world's largest social media platform. With just a few simple steps, you
    /// can enable your users to share their media files to Facebook, making it
    /// easy for them to spread the word about your app and increase its
    /// visibility. The payload returned for the sharing result is defined as
    /// ShareMediaResult.
    public static partial class Media
    {
        /// Launch the Share to Facebook modal, allowing users to share local media
        /// files to Facebook. Accepts a postTextSuggestion string for the default text
        /// of the Facebook post. Requires a filePath string as the path to the image
        /// to be shared to Facebook. This image should be located in your app's
        /// internal storage directory. Requires a contentType indicating the type of
        /// media to be shared (only 'photo' is currently supported). The payload for
        /// the result is defined as ShareMediaResult.
        public static Request<ShareMediaResult> ShareToFacebook(string postTextSuggestion, string filePath, MediaContentType contentType)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "post_text_suggestion", postTextSuggestion },
                    { "file_path", filePath },
                    { "content_type", contentType.GetDescription() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("media", "share_to_facebook", 1, jsonRequest, 0);
                return new Request<ShareMediaResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
