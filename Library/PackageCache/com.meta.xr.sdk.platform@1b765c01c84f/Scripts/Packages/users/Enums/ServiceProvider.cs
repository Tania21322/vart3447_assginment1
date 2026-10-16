// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<562ecf38155c49aecb83f119846ef577>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// The service provider enum describes the specific provider that is
    /// associated with the LinkedAccount of a User. Linked accounts for users
    /// represent the third party identities that are used for services or apps in
    /// association with user.
    [Preserve]
    public enum ServiceProvider : int
    {
        /// Represents that the ServiceProvider is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// TODO: Add comment for this enum member!
        [Description("DROPBOX")]
        [Preserve]
        Dropbox,

        /// TODO: Add comment for this enum member!
        [Description("FACEBOOK")]
        [Preserve]
        Facebook,

        /// TODO: Add comment for this enum member!
        [Description("GOOGLE")]
        [Preserve]
        Google,

        /// TODO: Add comment for this enum member!
        [Description("INSTAGRAM")]
        [Preserve]
        Instagram,

        /// TODO: Add comment for this enum member!
        [Description("REMOTE_MEDIA")]
        [Preserve]
        RemoteMedia,

        
    }

}
