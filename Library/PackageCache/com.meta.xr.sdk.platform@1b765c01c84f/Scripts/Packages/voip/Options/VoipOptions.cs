// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<8ee019f445f04600df6506faeb1bd515>>
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
    /// The voip_options configuration is used to specify additional settings for
    /// the VoIP transmission for a User. It consists of two primary options which
    /// include using DTX for transmission and setting a maxmimum bitrate for the
    /// network connection. You can read more about VoIP
    /// [here](https://developer.oculus.com/documentation/unity/ps-parties/#voip-options).
    [Serializable]
    [Preserve]
    public class VoipOptions {
        /// Sets the maximum average bitrate the audio codec should use. Higher
        /// bitrates will increase audio quality at the expense of increased network
        /// usage. Use a lower bitrate if you think the quality is good but the network
        /// usage is too much. Use a higher bitrate if you think the quality is bad and
        /// you can afford to have a large streaming bitrate.
        [JsonProperty("bitrate_for_new_connections")]
        [JsonConverter(typeof(EnumDescriptionConverter<VoipBitrate>))]
        [Preserve]
        public VoipBitrate BitrateForNewConnections = VoipBitrate.B32000;
        /// Set the opus codec to use discontinous transmission (DTX). DTX only
        /// transmits data when a person is speaking. Setting this to
        /// VoipDtxState.Enabled takes advantage of the fact that in a two-way
        /// converstation each individual speaks for less than half the time. Enabling
        /// DTX will conserve battery power and reduce transmission rate when a pause
        /// in the voice chat is detected.
        [JsonProperty("create_new_connection_use_dtx")]
        [JsonConverter(typeof(EnumDescriptionConverter<VoipDtxState>))]
        [Preserve]
        public VoipDtxState CreateNewConnectionUseDtx = VoipDtxState.Enabled;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public VoipOptions()
        {
        }
        
        [Preserve]
        public void SetBitrateForNewConnections(VoipBitrate value)
        {
            BitrateForNewConnections = value;
        }
        
        [Preserve]
        public void SetCreateNewConnectionUseDtx(VoipDtxState value)
        {
            CreateNewConnectionUseDtx = value;
        }
        
        ~VoipOptions()
        {
        }
    }
}
