// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<652bc503ccb9cf50b515ba32877c3aac>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// Result of installing an app. In case of an error during install process,
    /// the error message contains the string representation of this result. This
    /// is returned from Application.StartAppDownload,
    /// Application.CancelAppDownload and Application.InstallAppUpdateAndRelaunch
    /// APIs.
    [Preserve]
    public enum AppInstallResult : int
    {
        /// Represents that the AppInstallResult is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// Install of the app failed due to low storage on the device
        [Description("LOW_STORAGE")]
        [Preserve]
        LowStorage,

        /// Install of the app failed due to a network error
        [Description("NETWORK_ERROR")]
        [Preserve]
        NetworkError,

        /// Install of the app failed as another install request for this application
        /// is already being processed by the installer
        [Description("DUPLICATE_REQUEST")]
        [Preserve]
        DuplicateRequest,

        /// Install of the app failed due to an internal installer error
        [Description("INSTALLER_ERROR")]
        [Preserve]
        InstallerError,

        /// Install of the app failed because the user canceled the install operation
        [Description("USER_CANCELLED")]
        [Preserve]
        UserCancelled,

        /// Install of the app failed due to a user authorization error
        [Description("AUTHORIZATION_ERROR")]
        [Preserve]
        AuthorizationError,

        /// Install of the app succeeded
        [Description("SUCCESS")]
        [Preserve]
        Success,

        /// No new binaries are available for the app. The app is already up to date.
        [Description("NO_NEW_BINARIES_AVAILABLE")]
        [Preserve]
        NoNewBinariesAvailable,

        
    }

}
