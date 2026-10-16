// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<702edabcc992354ccb9af4cde5eac160>>
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
    /// The NetSyncOption configures network connections, focusing on VoIP data
    /// streams. It includes 'zone_id' for unique identifiers within the
    /// application, 'voip_stream_default' to set default VoIP stream modes, and
    /// 'voip_group' to assign a VoIP group upon connection. It can be set when
    /// create a new connection and retrieved using NetSyncConnection.
    [Serializable]
    [Preserve]
    public class NetSyncOptions {
        /// If provided, immediately set the voip_group to this value upon connection
        /// and can be retrieved using NetSyncSession.VoipGroup
        [JsonProperty("voip_group")]
        [Preserve]
        public string VoipGroup = "";
        /// When a new remote voip user connects, default that connection to this
        /// stream type by default. There are three modes:
        /// NetSyncVoipStreamMode.Unknown, NetSyncVoipStreamMode.Ambisonic,
        /// NetSyncVoipStreamMode.Mono.
        [JsonProperty("voip_stream_default")]
        [JsonConverter(typeof(EnumDescriptionConverter<NetSyncVoipStreamMode>))]
        [Preserve]
        public NetSyncVoipStreamMode VoipStreamDefault = NetSyncVoipStreamMode.Ambisonic;
        /// A `string` represents the unique identifier within the current application
        /// grouping.
        [JsonProperty("zone_id")]
        [Preserve]
        public string ZoneId = "";
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public NetSyncOptions()
        {
        }
        
        [Preserve]
        public void SetVoipGroup(string value)
        {
            VoipGroup = value;
        }
        
        [Preserve]
        public void SetVoipStreamDefault(NetSyncVoipStreamMode value)
        {
            VoipStreamDefault = value;
        }
        
        [Preserve]
        public void SetZoneId(string value)
        {
            ZoneId = value;
        }
        
        ~NetSyncOptions()
        {
        }
    }
}
