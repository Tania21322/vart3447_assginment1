// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<9d1c2a751a0467697cd9673e8b211856>>
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
    /// The multiplayer error option is a feature that allows developers to display
    /// general error messages to User in invokable error dialogs. This option is
    /// particularly useful in multiplayer games or applications where errors can
    /// occur due to various reasons such as network connectivity issues, server
    /// downtime, or other technical problems. Read more about error dialogues in
    /// our
    /// [docs](https://developer.oculus.com/documentation/unity/ps-error-dialogs/).
    [Serializable]
    [Preserve]
    public class MultiplayerErrorOptions {
        /// Key associated with the predefined error message to be shown to users. Key
        /// List: - MultiplayerErrorErrorKey.DestinationUnavailable -
        /// MultiplayerErrorErrorKey.DlcRequired - MultiplayerErrorErrorKey.General -
        /// MultiplayerErrorErrorKey.GroupFull -
        /// MultiplayerErrorErrorKey.InviterNotJoinable -
        /// MultiplayerErrorErrorKey.LevelNotHighEnough -
        /// MultiplayerErrorErrorKey.LevelNotUnlocked -
        /// MultiplayerErrorErrorKey.NetworkTimeout -
        /// MultiplayerErrorErrorKey.NoLongerAvailable -
        /// MultiplayerErrorErrorKey.UpdateRequired -
        /// MultiplayerErrorErrorKey.TutorialRequired
        [JsonProperty("error_key")]
        [JsonConverter(typeof(EnumDescriptionConverter<MultiplayerErrorErrorKey>))]
        [Preserve]
        public MultiplayerErrorErrorKey ErrorKey = MultiplayerErrorErrorKey.Unknown;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public MultiplayerErrorOptions()
        {
        }
        
        [Preserve]
        public void SetErrorKey(MultiplayerErrorErrorKey value)
        {
            ErrorKey = value;
        }
        
        ~MultiplayerErrorOptions()
        {
        }
    }
}
