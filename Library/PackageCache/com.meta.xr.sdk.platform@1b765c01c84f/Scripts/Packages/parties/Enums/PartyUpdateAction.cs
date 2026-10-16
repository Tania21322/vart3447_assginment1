// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<47dedf423a6c336b63608131d9c43f36>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// An enum that specifies the type of action related to the party and the
    /// user. For example, PartyUpdateNotification.Action contains the information
    /// about the user joined or left the party or the user was invited or
    /// uninvited to the party.
    [Preserve]
    public enum PartyUpdateAction : int
    {
        /// Represents that the PartyUpdateAction is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// This `enum` member indicates the user joined the party.
        [Description("JOIN")]
        [Preserve]
        Join,

        /// This `enum` member indicates the user left the party.
        [Description("LEAVE")]
        [Preserve]
        Leave,

        /// This `enum` member indicates the user was invited to the party.
        [Description("INVITE")]
        [Preserve]
        Invite,

        /// This `enum` member indicates the user was uninvited to the party.
        [Description("UNINVITE")]
        [Preserve]
        Uninvite,

        
    }

}
