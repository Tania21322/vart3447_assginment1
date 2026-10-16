// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.Collections.Generic;
using Oculus.Platform.Models;
using UnityEngine;

namespace Oculus.Platform
{
    /// <summary>
    /// Backward-compatible shim for the old Platform SDK's StandalonePlatform class.
    ///
    /// <para>
    /// The new Horizon Platform SDK unifies initialization through <see cref="Core.Initialize"/>
    /// and <see cref="Core.AsyncInitialize"/>. This class delegates to the new API to maintain
    /// compatibility with existing consumer code.
    /// </para>
    ///
    /// <para><b>Migration guide:</b></para>
    /// <code>
    /// // Old pattern:
    /// new StandalonePlatform().InitializeInEditor();
    ///
    /// // New pattern:
    /// // 1. Set UseStandalonePlatform = true in Meta > Platform > Edit Settings
    /// // 2. Call:
    /// Core.AsyncInitialize();
    /// </code>
    /// </summary>
    [Obsolete("StandalonePlatform is deprecated. Use Core.Initialize() or Core.AsyncInitialize() with UseStandalonePlatform enabled in Platform Settings instead.")]
    public sealed class StandalonePlatform
    {
        /// <summary>
        /// Initializes the platform in standalone mode within the Unity Editor.
        /// Reads the App ID and access token from Platform Settings and Standalone Platform Settings.
        /// </summary>
        /// <returns>A Request that resolves when initialization completes.</returns>
        [Obsolete("Use Core.AsyncInitialize() with UseStandalonePlatform enabled in Platform Settings instead.")]
        public Request<PlatformInitialize> InitializeInEditor()
        {
#if UNITY_ANDROID
            if (string.IsNullOrEmpty(PlatformSettings.MobileAppID))
            {
                throw new UnityException("Update your App ID by selecting 'Meta' > 'Platform' > 'Edit Settings'");
            }
            var appID = PlatformSettings.MobileAppID;
#else
            string appID;
            if (PlatformSettings.UseMobileAppIDInEditor)
            {
                appID = PlatformSettings.MobileAppID;
            }
            else
            {
                appID = PlatformSettings.AppID;
            }
            if (string.IsNullOrEmpty(appID))
            {
                throw new UnityException("Update your App ID by selecting 'Meta' > 'Platform' > 'Edit Settings'");
            }
#endif
            if (string.IsNullOrEmpty(StandalonePlatformSettings.OculusPlatformTestUserAccessToken))
            {
                throw new UnityException(
                    "Update your standalone credentials by selecting 'Meta' > 'Platform' > 'Edit Settings'");
            }

            var accessToken = StandalonePlatformSettings.OculusPlatformTestUserAccessToken;

            return AsyncInitialize(ulong.Parse(appID), accessToken);
        }

        /// <summary>
        /// Initializes the platform in standalone mode with the provided App ID and access token.
        /// </summary>
        /// <param name="appID">The application ID.</param>
        /// <param name="accessToken">The user access token for standalone testing.</param>
        /// <returns>A Request that resolves when initialization completes.</returns>
        [Obsolete("Use Core.AsyncInitialize() with UseStandalonePlatform enabled in Platform Settings instead.")]
        public Request<PlatformInitialize> AsyncInitialize(ulong appID, string accessToken)
        {
            StandalonePlatformSettings.OculusPlatformTestUserAccessToken = accessToken;
            PlatformSettings.UseStandalonePlatform = true;

            return Core.AsyncInitialize(appId: appID.ToString());
        }

        /// <summary>
        /// Initializes the platform in standalone mode with the provided App ID, access token,
        /// and initialization options.
        /// </summary>
        /// <param name="appId">The application ID.</param>
        /// <param name="accessToken">The user access token for standalone testing.</param>
        /// <param name="initConfigOptions">Configuration options for initialization.</param>
        /// <returns>A Request that resolves when initialization completes.</returns>
        [Obsolete("Use Core.AsyncInitialize() with UseStandalonePlatform enabled in Platform Settings instead.")]
        public Request<PlatformInitialize> AsyncInitializeWithAccessTokenAndOptions(
            string appId,
            string accessToken,
            Dictionary<InitConfigOptions, bool> initConfigOptions)
        {
            StandalonePlatformSettings.OculusPlatformTestUserAccessToken = accessToken;
            PlatformSettings.UseStandalonePlatform = true;

            return Core.AsyncInitialize(accessToken, initConfigOptions, appId: appId);
        }
    }
}
