// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<04ee69ff3fff666b1cab00b1d7ebe922>>
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

    /// Represents a 3D position in space.
    [Preserve]
    public class Position
    {
        /// X component of the position.
        [JsonProperty("x")]
        [Preserve]
        public float X { get; set; }
        /// Y component of the position.
        [JsonProperty("y")]
        [Preserve]
        public float Y { get; set; }
        /// Z component of the position.
        [JsonProperty("z")]
        [Preserve]
        public float Z { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
