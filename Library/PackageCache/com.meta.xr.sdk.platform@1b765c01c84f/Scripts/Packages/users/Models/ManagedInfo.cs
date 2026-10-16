// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<f74efe449fe613a859f5cf47d9a2261d>>
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

    /// If the user is a Meta Managed Account(MMA), the managed account for the
    /// user will contain further metadata information. There must be user consent
    /// via dialog during installation, your app must have
    /// [DUC](https://developer.oculus.com/resources/publish-data-use/) enabled,
    /// and the app must be admin-approved.
    ///
    /// The API has been deprecated from version 203. Please use No longer
    /// supported instead.
    [Preserve]
    public class ManagedInfo
    {
        /// A string represents the department name in the organization to which the
        /// user belongs to.
        [JsonProperty("department")]
        [Preserve]
        public string Department { get; set; }
        /// The email address of the account user which owns the MMA, i.e., Meta
        /// Managed Account.
        [JsonProperty("email")]
        [Preserve]
        public string Email { get; set; }
        /// A string represents the employee number of the person who owns MMA, i.e.,
        /// Meta Managed Account.
        [JsonProperty("employee_number")]
        [Preserve]
        public string EmployeeNumber { get; set; }
        /// A string which can be used to uniquely identify the user of the MMA, i.e.,
        /// Meta Managed Account.
        [JsonProperty("external_id")]
        [Preserve]
        public string ExternalId { get; set; }
        /// A string contains the information about the location of the user.
        [JsonProperty("location")]
        [Preserve]
        public string Location { get; set; }
        /// A string contains the information about the manager of the user.
        [JsonProperty("manager")]
        [Preserve]
        public string Manager { get; set; }
        /// A string contrains the information about the user's name.
        [JsonProperty("name")]
        [Preserve]
        public string Name { get; set; }
        /// A string which can be used to uniquely identify the organization which owns
        /// the MMA, i.e., Meta Managed Account.
        [JsonProperty("organization_id")]
        [Preserve]
        public string OrganizationId { get; set; }
        /// The name of the organization to which the MMA(i.e., Meta Managed Account)
        /// account user belongs to.
        [JsonProperty("organization_name")]
        [Preserve]
        public string OrganizationName { get; set; }
        /// A string contains the position information of the user.
        [JsonProperty("position")]
        [Preserve]
        public string Position { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
