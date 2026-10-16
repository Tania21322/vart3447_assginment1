// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<ebd70f9ffde2b49cd4407f686b5fff5e>>
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

    /// It represents the availability of a microphone device. It can be retrieved
    /// using Voip.GetMicrophoneAvailability. It has only one field,
    /// microphone_available, which is a boolean value that indicates whether a
    /// microphone is available or not. This data structure can be used by
    /// applications to determine whether they can use the microphone for audio
    /// input or not.
    [Preserve]
    public class MicrophoneAvailabilityState
    {
        /// A `boolean` indicates whether the microphone is currently available or not.
        /// If there is any update on the microphone availability, it will be retrieved
        /// as a notification using Voip.MicrophoneAvailabilityStateUpdate.
        [JsonProperty("microphone_available")]
        [Preserve]
        public bool MicrophoneAvailable { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
