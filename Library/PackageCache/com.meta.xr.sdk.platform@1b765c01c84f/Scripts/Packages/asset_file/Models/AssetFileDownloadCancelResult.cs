// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<e86502e96b6e2cec4040cfdb1edd4e77>>
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

    /// An AssetFileDownloadCancelResult represents the result of a canceled
    /// download action of an asset file. You can cancel a download of an asset
    /// file by using AssetFile.DownloadCancelById or
    /// AssetFile.DownloadCancelByName. The result contains three fields which are
    /// the asset file ID(use AssetFileDownloadCancelResult.AssetId to access), the
    /// file path, and success status of the canceled download.
    [Preserve]
    public class AssetFileDownloadCancelResult
    {
        [JsonProperty("asset_file_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 AssetFileId { get; set; }
        /// ID of the asset file. NOTE: this does not represent the ID of the asset.
        [JsonProperty("asset_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 AssetId { get; set; }
        /// File path of the asset file.
        [JsonProperty("filepath")]
        [Preserve]
        public string Filepath { get; set; }
        /// You can use this to determine whether the cancel request of downloading an
        /// asset file has succeeded.
        [JsonProperty("success")]
        [Preserve]
        public bool Success { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
