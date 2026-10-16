// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<f8cda25d93688dc122d5bdf5c046b9d5>>
 */

namespace Oculus.Platform
{

    using Description = System.ComponentModel.DescriptionAttribute;
    using UnityEngine.Scripting;

    /// This property indicates the current status of the app on the device. It is
    /// important to note that an app can only query its own status, not the status
    /// of other apps installed on the device. It can be retrieved by
    /// AppDownloadProgressResult.StatusCode.
    [Preserve]
    public enum AppStatus : int
    {
        /// Represents that the AppStatus is unknown.
        [Description("UNKNOWN")]
        [Preserve]
        Unknown,

        /// The user possesses a valid entitlement for the app, indicating they have
        /// the right to install it, although it is not currently installed on the
        /// device.
        [Description("ENTITLED")]
        [Preserve]
        Entitled,

        /// The app is scheduled for download. The download will start as soon as prior
        /// queued downloads are completed.
        [Description("DOWNLOAD_QUEUED")]
        [Preserve]
        DownloadQueued,

        /// The app is currently being downloaded to the device. This status is active
        /// during the download process until it is complete.
        [Description("DOWNLOADING")]
        [Preserve]
        Downloading,

        /// The app is currently being installed on the device. This status remains
        /// until the installation is fully completed.
        [Description("INSTALLING")]
        [Preserve]
        Installing,

        /// The app is successfully installed on the device and is ready to be used.
        [Description("INSTALLED")]
        [Preserve]
        Installed,

        /// The app is currently being uninstalled from the device. This status remains
        /// until the uninstallation process is complete.
        [Description("UNINSTALLING")]
        [Preserve]
        Uninstalling,

        /// The installation of the app is scheduled and will commence once any prior
        /// installations are completed.
        [Description("INSTALL_QUEUED")]
        [Preserve]
        InstallQueued,

        
    }

}
