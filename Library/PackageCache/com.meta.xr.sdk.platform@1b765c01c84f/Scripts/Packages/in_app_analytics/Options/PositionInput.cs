// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<fc2261ada7d1dc4175967dd3d1ca83b1>>
 */

using System;
using System.Collections;
using Oculus.Platform.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;
using UnityEngine.Scripting;

namespace Oculus.Platform
{
    /// Input type for specifying a 3D position.
    [Serializable]
    [Preserve]
    public class PositionInput {
        /// X component of the position.
        [JsonProperty("x")]
        [Preserve]
        public float X;
        /// Y component of the position.
        [JsonProperty("y")]
        [Preserve]
        public float Y;
        /// Z component of the position.
        [JsonProperty("z")]
        [Preserve]
        public float Z;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public PositionInput()
        {
        }
        
        [Preserve]
        public void SetX(float value)
        {
            X = value;
        }
        
        [Preserve]
        public void SetY(float value)
        {
            Y = value;
        }
        
        [Preserve]
        public void SetZ(float value)
        {
            Z = value;
        }
        
        ~PositionInput()
        {
        }
    }
}
