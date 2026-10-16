// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<26f72d2cd0db1c9740241b2f6b29ab03>>
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
    /// The roster option, to be passed into GroupPresence.LaunchRosterPanel which
    /// is used to launch the panel displaying the current users in the
    /// roster/party. You can read more about rosters in our
    /// [docs](https://developer.oculus.com/documentation/unity/ps-roster/).
    [Serializable]
    [Preserve]
    public class RosterOptions {
        /// Passing in these users will add them to the invitable users list. From the
        /// GroupPresence.LaunchRosterPanel, the User can open the invite list, where
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
        public RosterOptions()
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
        
        
        ~RosterOptions()
        {
        }
    }
}
