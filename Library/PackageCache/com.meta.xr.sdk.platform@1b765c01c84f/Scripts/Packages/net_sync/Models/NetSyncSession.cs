// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<73d4f559a0d8ac1593d1d50536c1f50d>>
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

    /// It represents the session of NetSyncConnection that allows multiple clients
    /// to connect and communicate with each other in real-time. It provides a way
    /// to manage and facilitate real-time communication and data synchronization
    /// between multiple clients in a networked environment.
    [Preserve]
    public class NetSyncSession
    {
        /// A `long` integer represents the unique ID of the NetSyncConnection within
        /// which this session exists.
        [JsonProperty("connection_id")]
        [Preserve]
        public long ConnectionId { get; set; }
        /// `True` if the local session has muted this session.
        [JsonProperty("muted")]
        [Preserve]
        public bool Muted { get; set; }
        /// The cloud networking internal session ID that can uniquely represent this
        /// session within the connection.
        [JsonProperty("session_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 SessionId { get; set; }
        /// The ovrID of the user behind this session.
        [JsonProperty("user_id")]
        [JsonConverter(typeof(UInt64EmptyStringConverter))]
        [Preserve]
        public UInt64 UserId { get; set; }
        /// A `string` represents the name of the voip group that this session is
        /// subscribed to.
        [JsonProperty("voip_group")]
        [Preserve]
        public string VoipGroup { get; set; }
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
    
    /// Represents a paginated list of NetSyncSession elements. It allows you to
    /// easily access and manipulate the elements in the paginated list, such as
    /// the size of the list and if there is a next page of elements available.
    [Preserve]
    public class NetSyncSessionList : DeserializableList<NetSyncSession>
    {
    }
}
