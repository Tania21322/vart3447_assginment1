// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<c78017546e6a5fd9ccf80e3c28461d16>>
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
    /// The Invite Option, to be passed in to GroupPresence.GetInvitableUsers and
    /// GroupPresence.LaunchInvitePanel, is a field that allows developers to
    /// specify a list of suggested users to be added to the invitable users list,
    /// making it easier for users to connect with others and create a more social
    /// experience.
    [Serializable]
    [Preserve]
    public class InviteOptions {
        /// Passing in these users will add them to the invitable users list. From the
        /// GroupPresence.LaunchInvitePanel, the user can open the invite list, where
        /// the suggested users will be added.
        [JsonProperty("suggested_users")]
        [JsonConverter(typeof(UInt64ArrayAsStringConverter))]
        [Preserve]
        public UInt64[] SuggestedUsers;
        
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        [Preserve]
        public InviteOptions()
        {
        }
        
        [Preserve]
        public void SetSuggestedUsers(UInt64[] value)
        {
            SuggestedUsers = value;
        }

        /// Adds a single user to the SuggestedUsers list.
        [Preserve]
        public void AddSuggestedUser(UInt64 userID)
        {
            var list = SuggestedUsers != null
                ? new List<UInt64>(SuggestedUsers)
                : new List<UInt64>();
            list.Add(userID);
            SuggestedUsers = list.ToArray();
        }

        /// Clears the SuggestedUsers list.
        [Preserve]
        public void ClearSuggestedUsers()
        {
            SuggestedUsers = null;
        }
        
        
        ~InviteOptions()
        {
        }
    }
}
