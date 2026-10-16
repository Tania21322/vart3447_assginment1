// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<87eb6e8eb53123c3959f050b9910339d>>
 */

#pragma warning disable 0618

namespace Oculus.Platform.Models
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using UnityEngine.Scripting;

    /// It represents the connection of a network synchronization system that
    /// allows multiple clients to connect and communicate with each other in
    /// real-time. It provides a way to manage and facilitate real-time
    /// communication and data synchronization between multiple clients in a
    /// networked environment.
    [Preserve]
    public class NetSyncConnection
    {
        /// A 'long' integer ID which can be used to uniquely identify the network
        /// synchronization connection.
        [JsonProperty("connection_id")]
        [Preserve]
        public long ConnectionId { get; set; }
        /// If the status is NetSyncConnectionStatus.Disconnected,
        /// NetSyncDisconnectReason.Unknown - The disconnect reason was unknown.
        /// NetSyncDisconnectReason.LocalTerminated - The disconnect was requested by
        /// an user. NetSyncDisconnectReason.ServerTerminated - The server closed the
        /// connection. NetSyncDisconnectReason.Failed - The initial connection never
        /// succeeded. NetSyncDisconnectReason.Lost - The disconnect was caused by
        /// network timeout.
        [JsonProperty("disconnect_reason")]
        [Preserve]
        public NetSyncDisconnectReason DisconnectReason { get; set; }
        /// The ID of the local session. Will be null if the connection is not active.
        [JsonProperty("session_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 SessionId { get; set; }
        /// A NetSyncConnectionStatus.Unknown - The current connection status is
        /// unknown. NetSyncConnectionStatus.Connecting - The connection has been
        /// started and the process is ongoing. NetSyncConnectionStatus.Disconnected -
        /// The current connection status is disconnected.
        /// NetSyncConnectionStatus.Connected - The connection has been established.
        [JsonProperty("status")]
        [JsonConverter(typeof(EnumDescriptionConverter<NetSyncConnectionStatus>))]
        [Preserve]
        public NetSyncConnectionStatus Status { get; set; }
        /// A `string` represents the unique identifier within the current application
        /// grouping.
        [JsonProperty("zone_id")]
        [Preserve]
        public string ZoneId { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
}
