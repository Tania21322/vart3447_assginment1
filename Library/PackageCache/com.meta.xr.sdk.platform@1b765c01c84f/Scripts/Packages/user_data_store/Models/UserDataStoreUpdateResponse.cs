// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<cd337c9f6cf27695ecb7a20c741ed278>>
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

    /// This payload contains information about the result of an update request to
    /// the user data store. It includes a success field @internal_link(horizon.pla
    /// tform.user_data_store.models.UserDataStoreUpdateResponse#success) that
    /// indicates whether the update was successful or not. See more info about
    /// Platform Solutions
    /// [here](https://developer.oculus.com/documentation/{{platform_doc_path}}/ps-
    /// platform-intro/).
    [Preserve]
    public class UserDataStoreUpdateResponse
    {
        /// Indicates whether the update request was successful or not. A value of true
        /// indicates that the update was successful, while a value of false indicates
        /// that the update failed.
        [JsonProperty("success")]
        [Preserve]
        public bool Success { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
