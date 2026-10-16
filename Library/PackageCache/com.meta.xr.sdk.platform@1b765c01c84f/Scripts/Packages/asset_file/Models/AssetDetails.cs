// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<32f07aa5be5152c1d1d640ca564fdf20>>
 */

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using UnityEngine.Scripting;

    /// An AssetDetails object contains detailed metadata for an asset file,
    /// including the asset file ID, file path, asset type, and additional metadata
    /// that characterizes the asset. You can retrieve the asset details using
    /// AssetFile.GetList
    [Preserve]
    public class AssetDetails
    {
        /// ID of the asset file. You can use this ID to uniquely identify a single
        /// asset. You can find more details from AssetFile.StatusById
        [JsonProperty("asset_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 AssetId { get; set; }
        /// One of 'default', 'store', 'shader_blob', 'shader_blob_final', 'dfm_apk',
        /// 'apk_v4_signature' or 'language_pack'. The 'default' type denotes this
        /// Asset File is used purely as an implementation detail (to download extra
        /// content post-installation). It's not shown on PDP, and is fully controlled
        /// by the app itself. The 'store' type indicates that the Asset File should be
        /// shown in Store. The 'language_pack' is a special type used to manage
        /// different languages and translation data, which can be downloaded post-
        /// installation. The 'shader_blob' type is used to manage shader blobs. The
        /// 'shader_blob_final' type is used to manage "final" shader blobs that may be
        /// merged shader blobs created by the backend. The 'dfm_apk' type is used for
        /// Dynamic Feature Module APKs. The 'apk_v4_signature' type is used for APKs
        /// v4 signatures.
        [JsonProperty("asset_type")]
        [Preserve]
        public string AssetType { get; set; }
        /// One of 'installed', 'available', or 'in-progress'
        [JsonProperty("download_status")]
        [Preserve]
        public string DownloadStatus { get; set; }
        /// File path of the asset file
        [JsonProperty("filepath")]
        [Preserve]
        public string Filepath { get; set; }
        /// The status of in app purchases which are some extra content that users can
        /// buy right from apps. The value could be one of 'free', 'entitled', or 'not-
        /// entitled'.
        [JsonProperty("iap_status")]
        [Preserve]
        public string IapStatus { get; set; }
        /// This field gives information about currently selected and installed
        /// language for the asset. The language info contains language name and tag in
        /// BCP47 format.
        [JsonProperty("language")]
        // May be null. Check before using.
        [Preserve]
        public LanguagePackInfo LanguageOptional { get; set; }
        /// Extra metadata associated with this asset file
        [JsonProperty("metadata")]
        [Preserve]
        public string Metadata { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of AssetDetails elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class AssetDetailsList : DeserializableList<AssetDetails>
    {
    }
}
