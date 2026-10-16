// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<4ebc3a745bd12d9e794dd1738750e7b0>>
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

    /// Represents the version information for an application. The information
    /// includes the date of latest release, the size of the latest release and the
    /// application name and version code of currently installed version and the
    /// latest release. You can retrieve it with Application.GetVersion.
    [Preserve]
    public class ApplicationVersion
    {
        /// The version code number for the version of the application that is
        /// currently installed on the device.
        [JsonProperty("currentVersionCode")]
        [Preserve]
        public int CurrentCode { get; set; }
        /// The version name string for the version of the application that is
        /// currently installed on the device.
        [JsonProperty("currentVersion")]
        [Preserve]
        public string CurrentName { get; set; }
        /// Version code number of the latest update of the application. This may or
        /// may not be currently installed on the device.
        [JsonProperty("latestVersionCode")]
        [Preserve]
        public int LatestCode { get; set; }
        /// Version name string of the latest update of the application. This may or
        /// may not be currently installed on the device.
        [JsonProperty("latestVersion")]
        [Preserve]
        public string LatestName { get; set; }
        /// Seconds since epoch when the latest application update was released. You
        /// need to convert this date to a human readable format before displaying it
        /// to the application users.
        [JsonProperty("releaseDate")]
        [Preserve]
        public long ReleaseDate { get; set; }
        /// Size of the latest application update in bytes.
        [JsonProperty("size")]
        [Preserve]
        public string Size { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
