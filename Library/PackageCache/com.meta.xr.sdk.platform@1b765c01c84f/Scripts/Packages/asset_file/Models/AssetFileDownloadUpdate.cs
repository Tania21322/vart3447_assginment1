// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<069d5b2934776dc00d8731c6cd074999>>
 */

namespace Oculus.Platform.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using UnityEngine.Scripting;

    /// An AssetFileDownloadUpdate represents the download status of an update for
    /// an asset file. It contains the asset file ID, the download progress of the
    /// update, and its completion status. It can be retrieved using
    /// AssetFile.DownloadUpdate. AssetFileDownloadUpdate.Completed is true means
    /// downloaded but probably not installed yet. Call AssetFile.StatusById until
    /// AssetDetails.DownloadStatus changes from 'available' to 'installed'.
    [Preserve]
    public class AssetFileDownloadUpdate
    {
        [JsonProperty("asset_file_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 AssetFileId { get; set; }
        /// ID of the asset file. It can be retrieved using AssetDetails.AssetId.
        [JsonProperty("asset_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 AssetId { get; set; }
        /// This field is of type uint and represents the total number of bytes in the
        /// asset file.
        [JsonProperty("bytes_total")]
        [Preserve]
        public ulong BytesTotal { get; set; }
        /// An integer represents the number of bytes that have been downloaded. -1 If
        /// the download hasn't started yet.
        [JsonProperty("bytes_transferred")]
        [Preserve]
        public long BytesTransferred { get; set; }
        /// This field is of type boolean and indicates whether the asset installation
        /// has been completed or not.
        [JsonProperty("completed")]
        [Preserve]
        public bool Completed { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
