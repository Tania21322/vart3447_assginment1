/*
 * Copyright (c) Meta Platforms, Inc. and affiliates.
 * All rights reserved.
 *
 * Licensed under the Oculus SDK License Agreement (the "License");
 * you may not use the Oculus SDK except in compliance with the License,
 * which is provided at the time of installation or download, or which
 * otherwise accompanies this software in either electronic or hard copy form.
 *
 * You may obtain a copy of the License at
 *
 * https://developer.oculus.com/licenses/oculussdk/
 *
 * Unless required by applicable law or agreed to in writing, the Oculus SDK
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#if UNITY_6000_3_OR_NEWER
#define USE_MAINTOOLBAR
#endif

using System.Collections.Generic;
using System.Linq;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.RemoteContent;
using Meta.XR.Editor.ToolingSupport;
using UnityEditor;
using UnityEngine;
using UIUtils = Meta.XR.Editor.UserInterface.Utils;

namespace Meta.XR.Editor.StatusMenu
{
    internal enum SdkUpdateCtaDestination
    {
        PackageManager,
        UpgradeAssistant
    }

    [InitializeOnLoad]
    internal class StatusMenu : EditorWindow
    {
        internal const string SdkUpgradeAssistantRampKey = "sdk_upgrade_assistant";

        private static IReadOnlyList<ToolDescriptor> _registeredItems;

        public static bool Visible => StatusMenuDrawer.Visible;

        internal static bool IsSdkUpgradeAssistantEnabled =>
            FeatureRampUpManager.GetRemoteKeysResult(SdkUpgradeAssistantRampKey, defaultValue: false);

        internal static SdkUpdateCtaDestination UpdateCtaDestination => IsSdkUpgradeAssistantEnabled
            ? SdkUpdateCtaDestination.UpgradeAssistant
            : SdkUpdateCtaDestination.PackageManager;

        // Per-user auto-show for first-run discoverability.
        // Separate key from Welcome/Nux so StatusMenu window can pop independently.
        // Uses EditorUserSettings (per-user, per-machine) not ProjectSettings or SessionState,
        // so first-run triggers once per user, persists across editor relaunches and domain reloads,
        // and does NOT trigger on SDK version bumps. Explicit close is respected forever.

        private const string HasShownStatusMenuKey = "StatusMenu.HasShown";

        private static bool HasShownStatusMenu
        {
            get => EditorUserSettings.GetConfigValue(HasShownStatusMenuKey) == "true";
            set => EditorUserSettings.SetConfigValue(HasShownStatusMenuKey, value ? "true" : "false");
        }

        static StatusMenu()
        {
            // Delay to let editor finish loading, similar to About.OnConsentSet pattern.
            EditorApplication.delayCall += ScheduleAutoShow;
        }

        private const double ShowOnLaunchTimeoutSeconds = 10.0;
        private static bool _showOnLaunchPolling;
        private static double _showOnLaunchDeadline;

        private static void ScheduleAutoShow()
        {
            if (!UIUtils.IsMainEditorProcess() || Application.isBatchMode || _showOnLaunchPolling)
            {
                return;
            }

            // Poll on EditorApplication.update similar to About.cs PollShowOnLaunch.
            // This survives domain reloads during startup and waits until ToolRegistry is populated.
            _showOnLaunchPolling = true;
            _showOnLaunchDeadline = EditorApplication.timeSinceStartup + ShowOnLaunchTimeoutSeconds;
            EditorApplication.update += PollShowOnLaunch;
        }

        private static void PollShowOnLaunch()
        {
            // Wait until ToolRegistry has items registered, or timeout to avoid hanging forever on first import.
            bool registryReady = ToolRegistry.Registry.Any(i => i.AddToStatusMenu);
            if (!registryReady && EditorApplication.timeSinceStartup < _showOnLaunchDeadline)
            {
                return;
            }

            EditorApplication.update -= PollShowOnLaunch;
            _showOnLaunchPolling = false;

            TryAutoShowStatusMenuOnFirstInstall();
        }

        private static void TryAutoShowStatusMenuOnFirstInstall()
        {
            if (!UIUtils.IsMainEditorProcess() || Application.isBatchMode)
            {
                return;
            }

            // Per-user gate: show once per user ever. HasShown persists in
            // EditorUserSettings, so it survives editor restarts, domain reloads,
            // and SDK version updates. No SessionState or version check.
            if (HasShownStatusMenu)
            {
                return;
            }

            HasShownStatusMenu = true;

            if (Visible)
            {
                return;
            }

            // Force ShowWindow for first-run discoverability, even on 6000.3+ where toolbar dropdown exists.
            // ShowWindow uses StatusMenuDrawer.ShowWindow which creates dockable window, not dropdown.
            EditorApplication.delayCall += () => ShowWindow();
        }

        private static void PrepareItems()
        {
            var registeredItems = ToolRegistry.Registry.Where(item => item.AddToStatusMenu && item.IsRampedUp).ToList();
            registeredItems.Sort((x, y) => x.Order.CompareTo(y.Order));
            _registeredItems = registeredItems;
        }

        public static ToolDescriptor GetHighestItem()
        {
            if (_registeredItems == null)
            {
                PrepareItems();
            }

            foreach (var item in _registeredItems)
            {
                var (_, color, showNotification) = item.PillIcon?.Invoke() ?? default;

                if (!showNotification)
                {
                    continue;
                }

                if (color.HasValue)
                {
                    return item;
                }
            }

            return default;
        }

        /// <summary>
        /// A utility function to show the status menu on pre-6000.3 Unity versions.
        /// </summary>
        [MenuItem("Window/Meta/Meta XR SDK", false, 3009)]
        public static void ShowWindow()
        {
            // We don't need to show the window in a specific location like the dropdown needs to.
            ShowDropdown(default);
        }

        public static void ShowDropdown(Rect source)
        {
            PrepareItems();

            if (_registeredItems.Count == 0)
            {
                return;
            }

            var (isUpdate, latestVersion) = GetUpdateState();

            if (source != default)
            {
                StatusMenuDrawer.ShowDropdown(source, _registeredItems, isUpdate, latestVersion);
            }
            else
            {
                StatusMenuDrawer.ShowWindow(_registeredItems, isUpdate, latestVersion);
            }
        }

        private static (bool isUpdateAvailable, string latestVersion) GetUpdateState()
        {
            var latestVersion = ToolRegistry.LatestAvailableVersion;
            return (latestVersion.HasValue, latestVersion?.ToString());
        }
    }
}
