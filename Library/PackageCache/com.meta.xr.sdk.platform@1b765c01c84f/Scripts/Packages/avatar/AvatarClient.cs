// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<7a2cdb1453d18a577e30aaf9b5857648>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The Avatars API allows developers to create highly expressive, diverse, and
    /// customizable avatar identities for the Meta ecosystem, Unity VR apps, and
    /// other multiplayer experiences. The Avatar.LaunchAvatarEditor method
    /// launches the Avatar Editor, where users can create and customize their
    /// avatars, the result can be retrieved by AvatarEditorResult.RequestSent.
    public static partial class Avatar
    {
        /// Launches the Avatar Editor. Meta Avatars Editor is a feature that allows
        /// users to edit their Meta Avatars appearances within the VR application that
        /// they are currently using. This experience is often used by users to switch
        /// their outfit and accessories to better suit the VR experience they are
        /// experiencing. The result can be retrieved by
        /// AvatarEditorResult.RequestSent.
        public static Request<AvatarEditorResult> LaunchAvatarEditor(AvatarEditorOptions options = null)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "options", options }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("avatar", "launch_avatar_editor", 1, jsonRequest, 0);
                return new Request<AvatarEditorResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        
    }
}
