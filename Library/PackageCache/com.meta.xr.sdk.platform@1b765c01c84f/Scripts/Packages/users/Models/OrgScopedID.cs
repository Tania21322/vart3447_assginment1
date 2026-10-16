// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<5703e955cb14c577d6e4c98c97d08f2a>>
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

    /// An ID for a User which is unique per Developer Center organization. This ID
    /// allows different apps within the same org to be able to identify the user.
    /// You can retrieve this ID by using Users.GetOrgScopedID.
    [Preserve]
    public class OrgScopedID
    {
        /// The unique id of the User in each organization, allowing different apps
        /// within the same Developer Center organization to have a consistent id for
        /// the same user.
        [JsonProperty("org_scoped_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 ID { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
