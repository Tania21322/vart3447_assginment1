// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.
/*
 * This file was @generated with arvr/projects/horizon-platform-sdk/tools/codegen. Do not modify it!
 * To regenerate this file, run: `buck run //arvr/projects/horizon-platform-sdk/tools/codegen:cli - - -all -g "Unity, CSharp"`
 * @generated SignedSource<<64a5f7842d85b250c2e1f3dd6a118b65>>
 */

using UnityEngine;
using Newtonsoft.Json;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;

namespace Oculus.Platform
{
    /// The Application API provides ways to manage and interact with applications
    /// on the platform, including retrieving information about installed apps, for
    /// example: getting ApplicationVersion.CurrentCode, launching other apps and
    /// managing app downloads and updates.
    public static partial class Application
    {
        /// Cancel an app download that is in progress. It will return a result when
        /// the download is cancelled.
        public static Request<AppDownloadResult> CancelAppDownload()
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("application", "cancel_app_download", 1, jsonRequest, 0);
                return new Request<AppDownloadResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Track download progress for an app.
        public static Request<AppDownloadProgressResult> CheckAppDownloadProgress()
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("application", "check_app_download_progress", 1, jsonRequest, 0);
                return new Request<AppDownloadProgressResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Retrieves an array of all applications that are currently installed, the
        /// elements of which are InstalledApplication instances.
        public static Request<InstalledApplicationList> GetInstalledApplications()
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("application", "get_installed_applications", 1, jsonRequest, 0);
                return new Request<InstalledApplicationList>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Requests version information, including the ApplicationVersion.CurrentCode
        /// and ApplicationVersion.CurrentName of the currently installed app and
        /// ApplicationVersion.LatestCode, ApplicationVersion.LatestName,
        /// ApplicationVersion.Size and ApplicationVersion.ReleaseDate of the latest
        /// app update.
        public static Request<ApplicationVersion> GetVersion()
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("application", "get_version", 1, jsonRequest, 0);
                return new Request<ApplicationVersion>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Installs the app update that was previously downloaded. Once the install
        /// begins the application will exit automatically. After the installation
        /// process is complete, the app will be relaunched based on the options passed
        /// in.
        public static Request<AppDownloadResult> InstallAppUpdateAndRelaunch(ApplicationOptions deeplink_options = null)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "deeplink_options", deeplink_options }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("application", "install_app_update_and_relaunch", 1, jsonRequest, 0);
                return new Request<AppDownloadResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Launches a different application in the user's library. If the user does
        /// not have that application installed, they will be taken to that app's page
        /// in the Oculus Store
        public static Request<string> LaunchOtherApp(UInt64 appID, ApplicationOptions deeplink_options = null)
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                    { "app_id", appID.ToString() },
                    { "deeplink_options", deeplink_options }
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("application", "launch_other_app", 1, jsonRequest, 0);
                return new Request<string>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        /// Starts an app download. It will return a result when the download is
        /// finished. Download progress can be monitored using the
        /// Application.CheckAppDownloadProgress.
        public static Request<AppDownloadResult> StartAppDownload()
        {
            if (Core.IsInitialized())
            {
                  var request = new Dictionary<string, object>
                {
                };

                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                };
                string jsonRequest = JsonConvert.SerializeObject(request, settings);

                ulong requestHandle = PlatformClient.MakeRequest("application", "start_app_download", 1, jsonRequest, 0);
                return new Request<AppDownloadResult>(requestHandle);
            }

            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        

        public static Request<InstalledApplicationList> GetNextInstalledApplicationListPage(InstalledApplicationList list) {
            if (!list.HasNextPage)
            {
                Debug.LogWarning("Oculus.Platform.GetNextInstalledApplicationListPage: List has no next page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.NextUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("application", "get_installed_application_page_by_url", 1, jsonRequest, 0);
                return new Request<InstalledApplicationList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }

        public static Request<InstalledApplicationList> GetPreviousInstalledApplicationListPage(InstalledApplicationList list) {
            if (!list.HasPreviousPage)
            {
                Debug.LogWarning("Oculus.Platform.GetPreviousInstalledApplicationListPage: List has no previous page");
                return null;
            }
            if (Core.IsInitialized())
            {
                string jsonRequest = JsonConvert.SerializeObject(new
                {
                    url = list.PreviousUrl
                });
                ulong requestHandle = PlatformClient.MakeRequest("application", "get_installed_application_page_by_url", 1, jsonRequest, 0);
                return new Request<InstalledApplicationList>(requestHandle);
            }
            Debug.LogError(Core.PlatformUninitializedError);
            return null;
        }
    }
}
