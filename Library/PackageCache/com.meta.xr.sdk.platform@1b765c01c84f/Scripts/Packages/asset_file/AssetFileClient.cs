// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<855f83976271d0b569f9efe8c3c7f6c9>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The Asset File API provides methods for managing asset files in a virtual
    /// environment. The methods include getting details and status
    /// AssetDetails.DownloadStatus, downloading by ID or name, canceling
    /// downloads, and deleting files by ID or name.
    public static partial class AssetFile
    {
        public static Request<AssetFileDeleteResult> Delete(UInt64 assetFileID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_id", assetFileID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "delete", 1, jsonRequest, 0);
                return new Request<AssetFileDeleteResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Removes a previously installed asset file from the device by its ID.
        /// Returns an object containing the asset ID and file name, and a success
        /// flag.
        public static Request<AssetFileDeleteResult> DeleteById(UInt64 assetFileID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_id", assetFileID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "delete_by_id", 1, jsonRequest, 0);
                return new Request<AssetFileDeleteResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Removes a previously installed asset file from the device by its name.
        /// Returns an object containing the asset ID and file name, and a success
        /// flag.
        public static Request<AssetFileDeleteResult> DeleteByName(string assetFileName)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_name", assetFileName }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "delete_by_name", 1, jsonRequest, 0);
                return new Request<AssetFileDeleteResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<AssetFileDownloadResult> Download(UInt64 assetFileID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_id", assetFileID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "download", 1, jsonRequest, 0);
                return new Request<AssetFileDownloadResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Downloads an asset file by its ID on demand. Returns an object containing
        /// the asset ID and filepath. Sends periodic AssetFile.DownloadUpdate to track
        /// the downloads.
        public static Request<AssetFileDownloadResult> DownloadById(UInt64 assetFileID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_id", assetFileID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "download_by_id", 1, jsonRequest, 0);
                return new Request<AssetFileDownloadResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Downloads a list of asset files by their IDs on demand. Returns a session
        /// ID that can be used to track the batch download progress. All assets must
        /// succeed or fail together. Sends periodic AssetFile.DownloadUpdate to track
        /// the downloads.
        public static Request<int> DownloadByIdList(UInt64[] assetFileIdList)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_id_list", Array.ConvertAll(assetFileIdList, id => id.ToString()) }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "download_by_id_list", 1, jsonRequest, 0);
                return new Request<int>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Downloads an asset file by its name on demand. Returns an object containing
        /// the asset ID and filepath. Sends periodic
        /// {notifications.asset_file.download_update}} to track the downloads.
        public static Request<AssetFileDownloadResult> DownloadByName(string assetFileName)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_name", assetFileName }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "download_by_name", 1, jsonRequest, 0);
                return new Request<AssetFileDownloadResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Downloads a list of asset files by their names on demand. Returns a session
        /// ID that can be used to track the batch download progress. All assets must
        /// succeed or fail together. Sends periodic
        /// {notifications.asset_file.download_update}} to track the downloads.
        public static Request<int> DownloadByNameList(string[] assetFileNameList)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_name_list", assetFileNameList }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "download_by_name_list", 1, jsonRequest, 0);
                return new Request<int>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<AssetFileDownloadCancelResult> DownloadCancel(UInt64 assetFileID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_id", assetFileID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "download_cancel", 1, jsonRequest, 0);
                return new Request<AssetFileDownloadCancelResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Cancels a previously spawned download request for an asset file by its ID.
        /// Returns an object containing the asset ID and file path, and a success
        /// flag.
        public static Request<AssetFileDownloadCancelResult> DownloadCancelById(UInt64 assetFileID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_id", assetFileID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "download_cancel_by_id", 1, jsonRequest, 0);
                return new Request<AssetFileDownloadCancelResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Cancels a previously spawned download request for an asset file by its
        /// name. Returns an object containing the asset ID and file path, and a
        /// success flag.
        public static Request<AssetFileDownloadCancelResult> DownloadCancelByName(string assetFileName)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_name", assetFileName }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "download_cancel_by_name", 1, jsonRequest, 0);
                return new Request<AssetFileDownloadCancelResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns an array of asset details with asset file names and their
        /// associated IDs AssetDetails.AssetId, and whether it's currently installed
        /// AssetDetails.DownloadStatus.
        public static Request<AssetDetailsList> GetList()
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

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "get_list", 1, jsonRequest, 0);
                return new Request<AssetDetailsList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<AssetDetails> Status(UInt64 assetFileID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_id", assetFileID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "status", 1, jsonRequest, 0);
                return new Request<AssetDetails>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns the details AssetDetails on a single asset: ID, file name, and
        /// whether it's currently installed
        public static Request<AssetDetails> StatusById(UInt64 assetFileID)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_id", assetFileID.ToString() }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "status_by_id", 1, jsonRequest, 0);
                return new Request<AssetDetails>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Returns the details AssetDetails on a single asset: ID, file name, and
        /// whether it's currently installed
        public static Request<AssetDetails> StatusByName(string assetFileName)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "asset_file_name", assetFileName }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "status_by_name", 1, jsonRequest, 0);
                return new Request<AssetDetails>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Sent to indicate download progress for asset files.
        
        public static void SetDownloadUpdateNotificationCallback(Message<AssetFileDownloadUpdate>.Callback callback)
        {
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new { });
                ulong sessionId = PlatformClient.MakeSession("asset_file", "download_update", 1, jsonRequest, 0);
                Callback.SetNotificationCallback<AssetFileDownloadUpdate>(sessionId, callback);
            }
            else
            {
                Debug.LogError(Core.PlatformUninitializedError);
            }
        }

        

        public static Request<AssetDetailsList> GetNextAssetDetailsListPage(AssetDetailsList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextAssetDetailsListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "get_asset_details_page_by_url", 1, jsonRequest, 0);
                return new Request<AssetDetailsList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<AssetDetailsList> GetPreviousAssetDetailsListPage(AssetDetailsList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousAssetDetailsListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("asset_file", "get_asset_details_page_by_url", 1, jsonRequest, 0);
                return new Request<AssetDetailsList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }
    }
}
