// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<19a30d31488f26070290a6a5629e6aaf>>
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

    /// Represents the result of an app download. You will use it when you start an
    /// app download, cancel an app download or install an app update which was
    /// previous downloaded. In those scenarios, the app download result will be
    /// the payload of Application.StartAppDownload, Application.CancelAppDownload
    /// or Application.InstallAppUpdateAndRelaunch API calls.
    [Preserve]
    public class AppDownloadResult
    {
        /// Result of the install operation returned by the installer. You can find
        /// more information about possible members from AppInstallResult. In case of
        /// an error during install process, the error message contains the string
        /// representation of this result.
        [JsonProperty("installResult")]
        [JsonConverter(typeof(EnumDescriptionConverter<AppInstallResult>))]
        [Preserve]
        public AppInstallResult AppInstallResult { get; set; }
        /// Timestamp in milliseconds when the operation finished.
        [JsonProperty("timestamp")]
        [Preserve]
        public long Timestamp { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
