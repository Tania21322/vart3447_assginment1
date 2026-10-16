// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<5a6ecd3c6c491e7c4dac6608da85c650>>
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
    /// FIXME: Add doc comment for this option!
    [Serializable]
    [Preserve]
    public class TestUnreleasedOptions {
        /// Tests that unreleased options objects do not appear in the public SDK
        [JsonProperty("test_property")]
        [Preserve]
        public uint TestProperty = 0;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public TestUnreleasedOptions()
        {
        }
        
        [Preserve]
        public void SetTestProperty(uint value)
        {
            TestProperty = value;
        }
        
        ~TestUnreleasedOptions()
        {
        }
    }
}
