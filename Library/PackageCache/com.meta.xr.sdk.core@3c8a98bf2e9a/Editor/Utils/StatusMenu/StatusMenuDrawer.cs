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


using System;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.Settings;
using Meta.XR.Editor.ToolingSupport;
using Meta.XR.Editor.UserInterface;
using Meta.XR.Editor.UserInterface.RLDS;

using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Meta.XR.Editor.StatusMenu
{
    internal class StatusMenuDrawer : RLDSEditorWindow
    {
        private const string TelemetryWindowId = "SdkMenu";
        private const string CorePackageId = "com.meta.xr.sdk.core";
        protected override string TelemetryId => TelemetryWindowId;
        protected override Origins TelemetryOrigin => _origin;

        [SerializeField] private Origins _origin = Origins.Unknown;

        // Both ShowDropdown and ShowWindow set this explicitly right before creating the instance, so
        // the only path that falls back to this default is a window restored from a Unity layout at
        // startup (OnEnable runs with no caller) — which is always the windowed surface.
        private static Origins _nextOrigin = Origins.StatusMenuWindow;

        private const float MenuWidth = 432f;
        private const float HeaderHeight = 60f;
        private const float DividerHeight = 6f;
        private const float CategoryLabelHeight = 34f;
        private const float MenuItemHeight = 55f;
        private const float MenuItemWithStatusHeight = 72f;
        private const float BottomPadding = 12f;

        private static readonly TextureContent.Category BuildingBlocksIcons = new("BuildingBlocks/Icons");

        private static readonly TextureContent NewIcon =
            TextureContent.CreateContent("ovr_icon_new.png", BuildingBlocksIcons, null);

        private static readonly TextureContent ExperimentalIcon =
            TextureContent.CreateContent("ovr_icon_experimental.png", TextureContent.Categories.Generic, null);

        private const float BannerHeight = 72f;

        private static StatusMenuDrawer _windowInstance;
        private static StatusMenuDrawer _dropdownInstance;
        private IReadOnlyList<ToolDescriptor> _items;
        private string _versionText;
        private bool _isUpdateAvailable;
        private string _latestVersionText;
        private bool _isDropdown;

        private const string AlertBannerDismissedKey = "StatusMenu.AlertBannerDismissed";

        private static bool AlertBannerDismissed => EditorUserSettings.GetConfigValue(AlertBannerDismissedKey) == "true";

        private static void SetAlertBannerDismissed(bool value)
        {
            EditorUserSettings.SetConfigValue(AlertBannerDismissedKey, value ? "true" : "false");
        }

        private static readonly TextureContent PinIcon =
            TextureContent.CreateContent("pin.png", TextureContent.Categories.Generic, null);

        internal static bool Visible => _windowInstance != null;
        private static StatusMenuDrawer _instance => _windowInstance ?? _dropdownInstance;

        /// <summary>
        /// Used to display the StatusMenuDrawer as a dropdown from the toolbar button
        /// </summary>
        /// <param name="source">The screen position of the dropdown</param>
        /// <param name="items">The items to render in the window, </param>
        /// <param name="isUpdateAvailable">Whether the Oculus SDK needs to be updated</param>
        /// <param name="latestVersion">The latest version of the package in use</param>
        internal static void ShowDropdown(Rect source, IReadOnlyList<ToolDescriptor> items, bool isUpdateAvailable = false, string latestVersion = null)
        {
            // Close existing dropdown only, do not affect docked window version.
            if (_dropdownInstance != null)
            {
                _dropdownInstance.Close();
                _dropdownInstance = null;
            }

            if (items == null || items.Count == 0) return;

            if (float.IsNaN(source.x) || float.IsNaN(source.y) ||
                float.IsNaN(source.width) || float.IsNaN(source.height))
            {
                return;
            }

            _nextOrigin = Origins.StatusMenu;
            var instance = CreateInstance<StatusMenuDrawer>();
            instance._isDropdown = true;
            instance.InitInstanceWithItems(items, isUpdateAvailable, latestVersion);

            instance.ShowAsDropDown(source, new Vector2(MenuWidth, instance.ComputeHeight()));
            instance.Focus();
            _dropdownInstance = instance;
        }

        /// <summary>
        /// A utility method, used to initialize a StatusMenuDrawer instance with the items to render in the window, and the version text.
        /// This should be called from ShowDropdown or ShowWindow and not from OnEnable, as the items are not available yet in OnEnable.
        /// </summary>
        /// <param name="instance">The StatusMenuDrawer being initialized</param>
        /// <param name="items"></param>
        private void InitInstanceWithItems(IReadOnlyList<ToolDescriptor> items, bool isUpdateAvailable = false, string latestVersion = null)
        {
            _items = items;
            _versionText = GetSdkVersion();
            _isUpdateAvailable = isUpdateAvailable;
            _latestVersionText = latestVersion;
        }

        /// <summary>
        /// Mirroring ShowDropdown for versions of Unity that don't support Reflection, and therefore need a dedicated window vs the editor toolbar button dropdown
        /// </summary>
        /// <param name="items">The items to render in the window, </param>
        /// <param name="isUpdateAvailable">Whether the Oculus SDK needs to be updated</param>
        /// <param name="latestVersion">The latest version of the package in use</param>
        internal static void ShowWindow(IReadOnlyList<ToolDescriptor> items, bool isUpdateAvailable = false, string latestVersion = null)
        {
            // GetWindow ensures single window instance; do not close dropdown here – dropdown is transient and will auto-close on focus loss, but we allow coexistence.
            // If an old window instance exists tracked separately, GetWindow will reuse it anyway.
            if (items == null || items.Count == 0) return;

            _nextOrigin = Origins.StatusMenuWindow;

            System.Type inspectorType = null;
            var allWindows = UnityEngine.Resources.FindObjectsOfTypeAll<UnityEditor.EditorWindow>();

            foreach (var w in allWindows)
            {
                // Inspector window type name is stable "InspectorWindow" across Unity versions. Use GetType().Name to avoid compile-time reference to internal type.
                if (w != null && w.GetType().Name == "InspectorWindow")
                {
                    inspectorType = w.GetType();
                    break;
                }
            }
            // Fallback also try by title content in case type name changes in future Unity versions – look for window with title containing "Inspector".
            if (inspectorType == null)
            {
                foreach (var w in allWindows)
                {
                    if (w != null && w.titleContent != null && w.titleContent.text != null && w.titleContent.text.Contains("Inspector"))
                    {
                        inspectorType = w.GetType();
                        break;
                    }
                }
            }
            StatusMenuDrawer instance;
            if (inspectorType != null)
            {
                instance = EditorWindow.GetWindow<StatusMenuDrawer>("Meta XR SDK", false, inspectorType);
            }
            else
            {
                instance = EditorWindow.GetWindow<StatusMenuDrawer>("Meta XR SDK");
            }

            instance._isDropdown = false;
            instance.InitInstanceWithItems(items, isUpdateAvailable, latestVersion);

            // GetWindow triggers CreateGUI before Init, so UI was built empty. Rebuild now that data is set.
            instance.RebuildUI();

            instance.Focus();
            _windowInstance = instance;
        }

        internal void RebuildUI()
        {
            if (rootVisualElement != null)
            {
                BuildUI(rootVisualElement);
            }
        }

        private float ComputeHeight()
        {
            float height = HeaderHeight + DividerHeight;

            if (_isUpdateAvailable)
            {
                height += BannerHeight;
            }

            var resources = _items.Where(i => i.MenuCategory == MenuCategory.Resources).ToList();
            var tools = _items.Where(i => i.MenuCategory == MenuCategory.Tools).ToList();
            var uncategorized = _items.Where(i => i.MenuCategory == MenuCategory.None).ToList();

            if (resources.Count > 0)
            {
                height += CategoryLabelHeight + resources.Sum(GetItemHeight);
            }

            if (tools.Count > 0)
            {
                height += CategoryLabelHeight + tools.Sum(GetItemHeight);
            }

            height += uncategorized.Sum(GetItemHeight);
            height += BottomPadding;

            return height;
        }

        private static float GetItemHeight(ToolDescriptor descriptor)
        {
            if (descriptor.EnablementDescriptor != null)
            {
                var (enabled, text) = descriptor.EnablementDescriptor();
                if (!enabled && !string.IsNullOrEmpty(text)) return MenuItemWithStatusHeight;
            }

            // InfoText rendered as a trailing badge keeps the row at its base height.
            if (descriptor.ShowInfoTextAsBadge) return MenuItemHeight;

            if (descriptor.StatusBadges != null)
            {
                var badges = descriptor.StatusBadges();
                var hasBadge = false;
                if (badges != null)
                {
                    foreach (var badge in badges)
                    {
                        if (!string.IsNullOrEmpty(badge.text))
                        {
                            hasBadge = true;
                            break;
                        }
                    }
                }

                return hasBadge ? MenuItemWithStatusHeight : MenuItemHeight;
            }

            if (descriptor.InfoTextDelegate == null) return MenuItemHeight;
            var (infoText, _) = descriptor.InfoTextDelegate();
            return string.IsNullOrEmpty(infoText) ? MenuItemHeight : MenuItemWithStatusHeight;
        }

        private void CreateGUI()
        {
            var isLightMode = !EditorGUIUtility.isProSkin;
            var styleSheet = RLDSUtils.LoadStyleSheet(isLightMode);
            if (styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(styleSheet);
            }

            BuildUI(rootVisualElement);
        }

        private void BuildUI(VisualElement root)
        {
            root.Clear();
            // Tag the surface so every RLDS row/header/banner reports it as its path.
            RLDSTelemetry.SetScope(root, _origin, TelemetryWindowId);

            if (_items == null)
            {
                return;
            }

            // Guard against building UI before assets are imported – ToolDescriptors' EnablementDelegate may access RuntimeSettings which loads ScriptableObject assets.
            // This mirrors WelcomeWindow pattern of try-catch and deferring rebuild, but we explicitly check EditorReady to avoid log spam from OVRRuntimeAssetsBase.
            if (!Meta.XR.Editor.Callbacks.InitializeOnLoad.EditorReady)
            {
                Meta.XR.Editor.Callbacks.InitializeOnLoad.Register(() => BuildUI(root));
                return;
            }

            var container = new VisualElement();
            container.style.flexGrow = 1f;
            container.AddToClassList(RLDSConstants.BeveledDropdown.Base);
            root.Add(container);

            // Add Cover banner and Alert banner at top for window version only, matching Welcome screen design but without buttons.
            // Dropdown version remains compact without cover to preserve existing dropdown height behavior.
            if (!_isDropdown)
            {
                BuildCoverSection(container);
                BuildAlertBannerSection(container);
            }
            else
            {
                // We should only use the non-banner header in the dropdown version.
                BuildHeader(container);
                BuildDivider(container);
            }
            if (_isUpdateAvailable)
            {
                BuildBanner(container);
            }
            BuildItemSections(container);
        }

        private void BuildCoverSection(VisualElement parent)
        {
            var cover = new CoverImage
            {
                FullBleed = true,
                CoverIcon = Meta.XR.Editor.UserInterface.Styles.Contents.SdkCoverIcon
            };

            var coverElement = cover.Build();
            Meta.XR.Editor.UserInterface.Styles.Contents.CoverBg.RegisterToImageLoaded(tex =>
                coverElement.style.backgroundImage = new StyleBackground(tex as Texture2D));
            // Override min-height to about half of Welcome's 261px default to make StatusMenu cover more compact.
            coverElement.style.minHeight = 130;
            coverElement.style.maxHeight = 140;
            coverElement.style.alignItems = Align.Center;
            parent.Add(coverElement);

            // Scale down and vertically center the decorative watermark icon to fit half-height cover without clipping.
            // USS defaults to 220px square at top:21px right:40px which overflows 130px height. Override to 110px and center vertically, keep right aligned.
            var iconContainer = coverElement.Q(className: RLDSConstants.CoverImage.Icon);
            if (iconContainer != null)
            {
                iconContainer.style.width = 110;
                iconContainer.style.height = 110;
                iconContainer.style.top = StyleKeyword.Null;
                iconContainer.style.bottom = StyleKeyword.Null;
                iconContainer.style.right = 20;
                iconContainer.style.position = Position.Absolute;
                // Center vertically within 130px height: (130-110)/2 =10px top offset
                iconContainer.style.top = 10;
                iconContainer.style.alignSelf = Align.Center;
            }
            var iconImage = coverElement.Q(className: RLDSConstants.CoverImage.IconImage);
            if (iconImage != null)
            {
                iconImage.style.width = 110;
                iconImage.style.height = 110;
            }

            cover.ContentArea.style.paddingLeft = RLDSConstants.Spacing.SizeXL; // 30% reduction from Size3XL 40px
            cover.ContentArea.style.paddingRight = RLDSConstants.Spacing.Size3XL;
            cover.ContentArea.style.paddingBottom = RLDSConstants.Spacing.SizeMD;
            cover.ContentArea.style.paddingTop = RLDSConstants.Spacing.SizeMD;
            cover.ContentArea.style.justifyContent = Justify.Center;

            var version = ToolUsage.GetSdkVersion();
            if (version.HasValue)
            {
                var versionBadge = new BadgePill($"Version {version.Value}", BadgePillType.Warning, BadgePillSize.Small).Build();
                cover.ContentArea.Add(versionBadge);
            }

            var title = new UnityEngine.UIElements.Label(StatusMenuSettings.Labels.CoverTitle);
            title.AddToClassList(RLDSConstants.Typography.Heading1);
            title.AddToClassList(RLDSConstants.Utilities.MarginTopXS);
            title.style.fontSize = new StyleLength(new Length(26, LengthUnit.Pixel)); // 20% smaller than Heading1 32px
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            cover.ContentArea.Add(title);

            var subtitle = new UnityEngine.UIElements.Label(StatusMenuSettings.Labels.CoverSubtitle);
            subtitle.AddToClassList(RLDSConstants.Typography.Body1Text);
            subtitle.AddToClassList(RLDSConstants.Utilities.MarginTopXS);
            subtitle.style.fontSize = new StyleLength(new Length(11, LengthUnit.Pixel)); // 20% smaller than Body1 14px
            cover.ContentArea.Add(subtitle);
            // No buttons per spec – cover is informational only in StatusMenu window version.
        }

        private void BuildAlertBannerSection(VisualElement parent)
        {
            if (AlertBannerDismissed)
            {
                return;
            }

#if USE_MAINTOOLBAR
            var message = AlertBannerMessages.AlertBannerMessagePinned + "\nYou can also re-open this menu as a window through <b>Window > Meta > Meta XR SDK</b>";
            var icon = PinIcon;
#else
            var message = AlertBannerMessages.AlertBannerMessage;
            TextureContent icon = null;
#endif
            var banner = new AlertBanner(message, icon, () => SetAlertBannerDismissed(true));
            // AlertBanner lives in Meta.XR.Editor.UserInterface namespace but is defined in StatusMenu folder for reuse.
            var bannerElement = banner.Build();
            parent.Add(bannerElement);
        }

        private void BuildHeader(VisualElement root)
        {
            var headerActions = _items
                .Where(i => i.MenuCategory == MenuCategory.Header)
                .OrderBy(i => i.Order)
                .Select(i => new HeaderAction(i.Icon, () =>
                {
                    i.OnClickDelegate?.Invoke(_origin);
                    Close();
                }, i.Id))
                .ToList();

            // The "· Update available" inline link is intentionally omitted — the update banner
            // ("v{N} available") shown below is the single update signal.
            var header = new MenuHeader
            {
                Version = _versionText,
                Actions = headerActions
            };
            root.Add(header.Build());
        }

        private void BuildBanner(VisualElement root)
        {
            var banner = new MenuBanner
            {
                VersionNumber = _latestVersionText != null ? $"v{_latestVersionText} available" : "Update available",
                FeatureDescription = "New features and improvements"
            };
            banner.CtaClicked += () =>
            {
                if (StatusMenu.UpdateCtaDestination == SdkUpdateCtaDestination.UpgradeAssistant)
                {
                    OpenUpdateAssistant();
                }
                else
                {
                    UnityEditor.PackageManager.UI.Window.Open(CorePackageId);
                }
                Close();
            };
            root.Add(banner.Build());
        }

        private static void BuildDivider(VisualElement root)
        {
            var container = new VisualElement();
            container.AddToClassList(RLDSConstants.Divider.Section);

            var divider = new VisualElement();
            divider.AddToClassList(RLDSConstants.Divider.Base);
            container.Add(divider);

            var highlight = new VisualElement();
            highlight.AddToClassList(RLDSConstants.Divider.Highlight);
            container.Add(highlight);

            root.Add(container);
        }

        private void BuildItemSections(VisualElement root)
        {
            var resources = _items
                .Where(i => i.MenuCategory == MenuCategory.Resources)
                .OrderBy(i => SortKeyForItem(i)).ThenBy(i => i.Order).ToList();
            var tools = _items
                .Where(i => i.MenuCategory == MenuCategory.Tools)
                .OrderBy(i => SortKeyForItem(i)).ThenBy(i => i.Order).ToList();
            var uncategorized = _items
                .Where(i => i.MenuCategory == MenuCategory.None)
                .OrderBy(i => SortKeyForItem(i)).ThenBy(i => i.Order).ToList();

            if (resources.Count > 0)
            {
                root.Add(new CategoryLabel("Resources").Build());
                BuildItemList(root, resources);
            }

            if (tools.Count > 0)
            {
                root.Add(new CategoryLabel("Tools").Build());
                BuildItemList(root, tools, addBottomPadding: true);
            }

            if (uncategorized.Count > 0)
            {
                BuildItemList(root, uncategorized);
            }
        }

        private void BuildItemList(VisualElement root, List<ToolDescriptor> items, bool addBottomPadding = false)
        {
            var listContainer = new VisualElement();
            listContainer.style.paddingLeft = RLDSConstants.Spacing.SizeSM;
            listContainer.style.paddingRight = RLDSConstants.Spacing.SizeSM;
            if (addBottomPadding)
            {
                listContainer.style.paddingBottom = RLDSConstants.Spacing.SizeSM;
            }

            foreach (var descriptor in items)
            {
                listContainer.Add(BuildMenuItemRow(descriptor));
            }

            root.Add(listContainer);
        }

        private VisualElement BuildMenuItemRow(ToolDescriptor descriptor)
        {
            // Use the display name (Label = DisplayName ?? Name) so the row matches the
            // sentence-case labels in the design (Figma node 4818-71850).
            var title = descriptor.Label;

            var isDisabled = false;
            string enablementText = null;
            VisualElement enablementContent = null;
            if (descriptor.EnablementDescriptor != null)
            {
                var (enabled, text) = descriptor.EnablementDescriptor();
                isDisabled = !enabled;
                if (isDisabled)
                {
                    if (descriptor.EnablementLink != null)
                    {
                        enablementContent = BuildEnablementLink(descriptor);
                    }
                    else
                    {
                        enablementText = text;
                    }
                }
            }

            var row = new MenuItemRow(
                title: title,
                icon: descriptor.Icon,
                subtitle: descriptor.MenuDescription,
                statusContent: isDisabled ? null : BuildStatusContent(descriptor),
                trailingContent: BuildTrailingContent(descriptor),
                disabled: isDisabled,
                enablementText: enablementText,
                enablementContent: enablementContent,
                id: descriptor.Id);

            row.Clicked += () =>
            {
                descriptor.MarkSeen();
                descriptor.OnClickDelegate?.Invoke(_origin);
                // Window version should stay open to allow multiple actions; dropdown version closes on click to mimic menu behavior.
                if (_isDropdown && descriptor.CloseOnClick)
                {
                    Close();
                }
                else
                {
                    // Rebuild so status badges re-evaluate after an in-menu toggle
                    // (e.g. the Meta XR Simulator enable/disable from this same dropdown).
                    // For window version we always rebuild and never close, even if CloseOnClick is true.
                    BuildUI(rootVisualElement);
                    Repaint();
                }
            };

            return row.Build();
        }

        private static VisualElement BuildStatusContent(ToolDescriptor descriptor)
        {
            // InfoText promoted to the trailing badge is not also shown under the description.
            if (descriptor.ShowInfoTextAsBadge) return null;

            if (descriptor.StatusBadges != null)
            {
                return BuildStatusBadges(descriptor.StatusBadges());
            }

            if (descriptor.InfoTextDelegate == null) return null;

            var (text, color) = descriptor.InfoTextDelegate();
            if (string.IsNullOrEmpty(text)) return null;

            var type = MapColorToBadgeType(color);
            return new BadgeTag(text, type, BadgeTagSize.Small).Build();
        }

        // Renders one or more severity badges on a single row (e.g. the Project setup tool's
        // red "outstanding issues" and amber "manually fixable items").
        private static VisualElement BuildStatusBadges(
            System.Collections.Generic.IReadOnlyList<(string text, Color? color)> badges)
        {
            if (badges == null) return null;

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            foreach (var (text, color) in badges)
            {
                if (string.IsNullOrEmpty(text)) continue;

                var badge = new BadgeTag(text, MapColorToBadgeType(color), BadgeTagSize.Small).Build();
                if (row.childCount > 0)
                {
                    badge.style.marginLeft = RLDSConstants.Spacing.Size2XS;
                }

                row.Add(badge);
            }

            return row.childCount > 0 ? row : null;
        }

        private VisualElement BuildEnablementLink(ToolDescriptor descriptor)
        {
            var (prefix, linkText, onClick) = descriptor.EnablementLink();

            var line = new VisualElement();
            line.AddToClassList(RLDSConstants.MenuItem.EnablementLine);

            if (!string.IsNullOrEmpty(prefix))
            {
                var prefixLabel = new UnityEngine.UIElements.Label(prefix);
                prefixLabel.AddToClassList(RLDSConstants.MenuItem.EnablementPrefix);
                line.Add(prefixLabel);
            }

            var link = new UnityEngine.UIElements.Label(linkText);
            link.AddToClassList(RLDSConstants.MenuItem.EnablementLink);
            link.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                onClick?.Invoke(_origin);
                Close();
            });
            line.Add(link);

            return line;
        }

        private static BadgeTagType MapColorToBadgeType(Color? color)
        {
            if (!color.HasValue) return BadgeTagType.Neutral;

            if (color.Value == UserInterface.Styles.Colors.SuccessColor) return BadgeTagType.Positive;
            if (color.Value == UserInterface.Styles.Colors.ErrorColor) return BadgeTagType.Negative;
            if (color.Value == UserInterface.Styles.Colors.WarningColor) return BadgeTagType.Warning;
            if (color.Value == UserInterface.Styles.Colors.InfoColor) return BadgeTagType.Info;

            return BadgeTagType.Neutral;
        }

        internal static VisualElement BuildTrailingContent(ToolDescriptor descriptor)
        {
            if (descriptor.DrawExperimentalInStatusMenu)
            {
                return new BadgePill("Experimental", BadgePillType.Neutral, BadgePillSize.Small, ExperimentalIcon).Build();
            }

            if (descriptor.ShowInfoTextAsBadge && descriptor.InfoTextDelegate != null)
            {
                var (infoText, _) = descriptor.InfoTextDelegate();
                if (!string.IsNullOrEmpty(infoText))
                {
                    return new BadgePill(infoText, BadgePillType.Info, BadgePillSize.Small, NewIcon).Build();
                }
            }

            if (descriptor.IsNew)
            {
                return new BadgePill("New", BadgePillType.Info, BadgePillSize.Small, NewIcon).Build();
            }

            return null;
        }

        private static int SortKeyForItem(ToolDescriptor descriptor)
        {
            // Tools pinned to the bottom sort after both internal and non-internal tools.
            if (descriptor.ShowLastInStatusMenu) return 2;
            return 0;
        }

        private static void OpenAbout()
        {
            EditorApplication.ExecuteMenuItem("Window/Meta/About Meta XR SDK");
        }

        private const string SdkUpdateAssistantName = "SDK update assistant";
        private const string SdkUpgradeAssistantName = "SDK Upgrade Assistant";

        private void OpenUpdateAssistant()
        {
            // Prefer the SDK Upgrade Guide window by its stable tool name, then the legacy "SDK update
            // assistant" tool, then the About window. Matching by name (rather than "first tool that
            // reports a version") avoids opening an unrelated tool such as About, whose
            // AvailableVersionDelegate also resolves to a value.
            // Resolved through the full ToolRegistry (not the menu list _items) so it still finds the
            // upgrade guide, which is registered but not a standing menu row (AddToStatusMenu = false).
            var descriptor =
                ToolRegistry.Registry.FirstOrDefault(i => i.Name == SdkUpgradeAssistantName)
                ?? ToolRegistry.Registry.FirstOrDefault(i => i.Name == SdkUpdateAssistantName);
            if (descriptor?.OnClickDelegate != null)
            {
                descriptor.OnClickDelegate(_origin);
                return;
            }

            // Fallback if no update-surfacing tool is registered (e.g. minimal external SDK build).
            // Log when both name lookups miss so a future rename of either tool's Name is visible
            // rather than silently landing the user on About. (These are display-name string matches
            // because this Utils-layer drawer intentionally doesn't reference the Guides assembly.)
            UnityEngine.Debug.LogWarning(
                $"[SdkUpgrader] No update-assistant tool matched '{SdkUpgradeAssistantName}' or "
                + $"'{SdkUpdateAssistantName}' in the ToolRegistry; opening About instead.");
            OpenAbout();
        }

        private static string GetSdkVersion()
        {
            var version = ToolUsage.GetSdkVersion();
            return version.HasValue ? $"Version {version.Value}" : null;
        }

        protected override void OnEnable()
        {
            // Adopt the surface chosen by the caller on first creation only
            if (_origin == Origins.Unknown)
            {
                _origin = _nextOrigin;
            }

            base.OnEnable();
            if (!Meta.XR.Editor.Callbacks.InitializeOnLoad.EditorReady)
            {
                Meta.XR.Editor.Callbacks.InitializeOnLoad.Register(OnEditorReadyRebuild);
                return;
            }
            OnEditorReadyRebuild();
        }

        private void OnEditorReadyRebuild()
        {
            if (_items == null)
            {
                InitInstanceWithItems(DeriveItems(), false, null);
            }

            // Fixes a race condition if the Meta XR SDK window is open on editor start up. Need to have tools finished initializing before building UI.
            ToolRegistry.OnInitialized -= OnToolsInitialized;
            ToolRegistry.OnInitialized += OnToolsInitialized;

            // Refresh badges live when a tool's status changes
            ToolRegistry.OnStatusChanged -= OnToolStatusChanged;
            ToolRegistry.OnStatusChanged += OnToolStatusChanged;

            if (ToolRegistry.IsInitialized)
            {
                OnToolsInitialized();
            }

            EditorApplication.delayCall += RebuildIfEmpty;
        }

        private static List<ToolDescriptor> DeriveItems() =>
            ToolRegistry.Registry
                .Where(i => i.AddToStatusMenu && i.IsRampedUp)
                .OrderBy(i => i.Order)
                .ToList();

        private void OnToolsInitialized()
        {
            if (this == null)
            {
                return;
            }

            _items = DeriveItems();
            if (rootVisualElement != null && Meta.XR.Editor.Callbacks.InitializeOnLoad.EditorReady)
            {
                BuildUI(rootVisualElement);
                Repaint();
            }
        }

        private void OnToolStatusChanged(ToolDescriptor descriptor)
        {
            if (this == null) return;
            if (rootVisualElement == null) return;
            if (!Meta.XR.Editor.Callbacks.InitializeOnLoad.EditorReady) return;
            BuildUI(rootVisualElement);
            Repaint();
        }

        private void OnBecameVisible() => RebuildIfEmpty();

        private void OnFocus() => RebuildIfEmpty();

        private void RebuildIfEmpty()
        {
            if (this == null) return;
            if (rootVisualElement == null) return;
            if (!Meta.XR.Editor.Callbacks.InitializeOnLoad.EditorReady) return;
            // If UI was built empty due to null _items during CreateGUI, rebuild now that OnEnable has rehydrated.
            if (rootVisualElement.childCount == 0 && _items != null)
            {
                BuildUI(rootVisualElement);
                Repaint();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            ToolRegistry.OnInitialized -= OnToolsInitialized;
            ToolRegistry.OnStatusChanged -= OnToolStatusChanged;
            if (_windowInstance == this) _windowInstance = null;
            if (_dropdownInstance == this) _dropdownInstance = null;
        }
    }
}
