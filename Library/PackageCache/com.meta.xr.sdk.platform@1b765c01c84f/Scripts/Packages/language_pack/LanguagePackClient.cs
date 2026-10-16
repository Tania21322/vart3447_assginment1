// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<8a94207a5f45ea62df89bb07ca58a55c>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The LanguagePack API provides a way to manage language packs for an
    /// application. A language pack is a collection of assets that are specific to
    /// a particular language, such as translations of text, audio files, and
    /// images. For more information, see
    /// [here](https://developer.oculus.com/documentation/unity/ps-language-packs/).
    public static partial class LanguagePack
    {
        /// Returns currently installed and selected language pack for an app in the
        /// view of the AssetDetails.Language field to extract needed language info. A
        /// particular language can be download and installed by a user from the Oculus
        /// app on the application page.
        public static Request<AssetDetails> GetCurrent()
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

                ulong requestHandle = PlatformClient.MakeRequest("language_pack", "get_current", 1, jsonRequest, 0);
                return new Request<AssetDetails>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Sets the current language to specified. The parameter is the BCP47 language
        /// tag. If a language pack is not downloaded yet, spawns automatically the
        /// AssetFile.DownloadByName request, and sends periodic
        /// AssetFile.DownloadUpdate to track the downloads. Once the language asset
        /// file is downloaded, call LanguagePack.GetCurrent to retrieve the data, and
        /// use the language at runtime.
        public static Request<AssetFileDownloadResult> SetCurrent(string tag)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "tag", tag }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("language_pack", "set_current", 1, jsonRequest, 0);
                return new Request<AssetFileDownloadResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
