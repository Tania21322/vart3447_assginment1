// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<38e246764556ec19893ee550a5e29522>>
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
    /// The Avatar Editor Option is a feature that allows users to create and
    /// customize their avatars. It is launched by the Avatar.LaunchAvatarEditor
    /// request and provides a way for users to specify the source of the request,
    /// allowing for more flexibility and customization in the avatar creation
    /// process.
    [Serializable]
    [Preserve]
    public class AvatarEditorOptions {
        /// Optional override for where the request is coming from. This field allows
        /// you to specify the source of the request in the launched editor by calling
        /// Avatar.LaunchAvatarEditor, which can be useful in cases where you want to
        /// track or identify the origin of the request.
        [JsonProperty("source_override")]
        [Preserve]
        public string SourceOverride = "";
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public AvatarEditorOptions()
        {
        }
        
        [Preserve]
        public void SetSourceOverride(string value)
        {
            SourceOverride = value;
        }
        
        ~AvatarEditorOptions()
        {
        }
    }
}
