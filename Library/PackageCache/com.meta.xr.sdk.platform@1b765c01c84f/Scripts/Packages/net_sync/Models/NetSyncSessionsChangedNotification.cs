// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<0369182eb9557cbbc1578cb1f36e4e88>>
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

    /// When a new list of sessions was added or the list of known connected
    /// sessions has changed, this NetSync.SessionsChanged will be sent. When the
    /// status of a connection has changed, the NetSync.ConnectionStatusChanged
    /// will be sent.
    [Preserve]
    public class NetSyncSessionsChangedNotification
    {
        /// A `long` integer ID which can be used to uniquely identify the network
        /// synchronization connection.
        [JsonProperty("connection_id")]
        [Preserve]
        public long ConnectionId { get; set; }
        /// An `array` which contains the new list of NetSyncSession.
        [JsonProperty("sessions")]
        [Preserve]
        public NetSyncSessionList Sessions { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
