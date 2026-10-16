// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<c2005825d6abc2d5382cf58757211a20>>
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

    /// The party ID is a unique identifier of the party that will be generated for
    /// every distinct party. This ID can be used to make requests such as
    /// Parties.GetCurrent to get the current party by its id. Read more about
    /// [parties](https://developer.oculus.com/documentation/unity/ps-parties/).
    [Preserve]
    public class PartyID
    {
        /// The party ID can be used to retrieve Party. Every party will have a unique
        /// ID that is associated with it.
        [JsonProperty("party_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 ID { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
