// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<c2181ff3e167b784c2e6344baf6e02a4>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// TODO: add top-level comment for client!
    public static partial class Http
    {
        /// Make an authenticated HTTP GET call to the specified complete URL. Example:
        /// www.oculus.com/cinema_video_proxy?access_token=$TOKEN
        public static Request<string> Get(string url)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "url", url }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("http", "get", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Make an authenticated HTTP GET call to the specified complete URL. Save URL
        /// contents to diskFile.
        public static Request<string> GetToFile(string url, string diskFile)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "url", url },
                    { "disk_file", diskFile }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("http", "get_to_file", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Make an authenticated (using provided access token) HTTP POST call to the
        /// specified complete url. Uploads a file at filepath as a POST param with
        /// name file_param_name. Includes an optional array of key-value pairs of
        /// strings to include as POST params. Uses the provided access_token to make
        /// the POST call.
        public static Request<string> MultiPartPost(string url, string filepathParamName, string filepath, string accessToken, Dictionary<string, string> postParams)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "url", url },
                    { "filepath_param_name", filepathParamName },
                    { "filepath", filepath },
                    { "access_token", accessToken },
                    { "post_params", postParams }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("http", "multi_part_post", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Make an authenticated HTTP POST call to the specified complete URL.
        public static Request<string> Post(string url)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "url", url }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("http", "post", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Sent to indicate that more data has been read or an error occured.
        
        public static void SetTransferNotificationCallback(Message<HttpTransferUpdate>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("http", "transfer", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<HttpTransferUpdate>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        /// Initiates an HTTP transfer to a given URL.  Returns an id that can be used
        /// to write data to the stream.  A message will be posted when new data is
        /// received or in case of an error.
        public static Request<long> StartTransfer(string url, Dictionary<string, string> headers)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "url", url },
                    { "headers", headers }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("http", "start_transfer", 1, jsonRequest, 0);
                return new Request<long>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Writes data to the stream identified by a given `transferId'.
        public static Request<bool> Write(long transferId, byte[] bytes, UIntPtr length)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "transfer_id", transferId },
                    { "bytes", bytes },
                    { "length", length }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("http", "write", 1, jsonRequest, 0);
                return new Request<bool>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Indicates that the no more data will be sent to the server.  The connection
        /// will eventually be closed.  Note that we might still receive data from the
        /// server for a little while.
        public static Request WriteEom(long transferId)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "transfer_id", transferId }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("http", "write_eom", 1, jsonRequest, 0);
                return new Request(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
