// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<965e3b68847b7bb67a6f268680539fd2>>
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

    /// InstalledApplication provides a way to retrieve information about installed
    /// application on a device, including their package name, application ID,
    /// version name, version code, and status. This information can be useful for
    /// tracking the status of applications on a device and identifying any issues
    /// that may need to be addressed.
    [Preserve]
    public class InstalledApplication
    {
        /// It's a type of `string` represents the ID of the application, which is a
        /// unique identifier for the app.
        [JsonProperty("application_id")]
        [Preserve]
        public string ApplicationId { get; set; }
        /// The package name of the installed application.
        [JsonProperty("package_name")]
        [Preserve]
        public string PackageName { get; set; }
        /// A `string` represents the status of the installed application.
        [JsonProperty("status")]
        [Preserve]
        public string Status { get; set; }
        /// It's a type of `int` represents the current version code of the installed
        /// application. It can be retreiving using ApplicationVersion.CurrentCode
        [JsonProperty("version_code")]
        [Preserve]
        public int VersionCode { get; set; }
        /// It's a type of `string` represents the current version name of the
        /// installed application. It can be retreiving using
        /// ApplicationVersion.CurrentName
        [JsonProperty("version_name")]
        [Preserve]
        public string VersionName { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of InstalledApplication elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class InstalledApplicationList : DeserializableList<InstalledApplication>
    {
    }
}
