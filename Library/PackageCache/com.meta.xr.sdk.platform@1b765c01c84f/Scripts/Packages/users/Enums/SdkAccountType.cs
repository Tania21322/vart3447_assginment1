// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<6027be8d22e97f124415706903007e4a>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// A user can have multiple sdk accounts associated with it. An SdkAccount
    /// represents the oculus user and the particular x-account that is linked to
    /// the  user. SDK accounts can be retrived for a particular user by calling
    /// Users.GetSdkAccounts. This enumeration represents the specific type of SDK
    /// account that is associated.
    [Preserve]
    public enum SdkAccountType : int
    {
        /// Represents that the SdkAccountType is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// TODO: Add comment for this enum member!
        [Description("OCULUS")]
        [Preserve]
        Oculus,

        /// TODO: Add comment for this enum member!
        [Description("FACEBOOK_GAMEROOM")]
        [Preserve]
        FacebookGameroom,

        
    }

}
