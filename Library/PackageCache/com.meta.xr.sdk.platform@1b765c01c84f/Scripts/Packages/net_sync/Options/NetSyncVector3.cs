// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<009647bba7de982bf6135f2123370410>>
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
    /// Representation of 3D vectors and points.
    [Serializable]
    [Preserve]
    public class NetSyncVector3 {
        /// X component of the vector
        [JsonProperty("x")]
        [Preserve]
        public float X = 0.0f;
        /// Y component of the vector
        [JsonProperty("y")]
        [Preserve]
        public float Y = 0.0f;
        /// Z component of the vector
        [JsonProperty("z")]
        [Preserve]
        public float Z = 0.0f;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public NetSyncVector3()
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
        
        ~NetSyncVector3()
        {
        }
    }
}
