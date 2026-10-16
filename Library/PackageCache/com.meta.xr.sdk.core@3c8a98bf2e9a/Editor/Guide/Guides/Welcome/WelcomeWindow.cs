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
using Meta.XR.Editor.RemoteContent;
using Meta.XR.Editor.StatusMenu;
using Meta.XR.Editor.ToolingSupport;
using Meta.XR.Editor.UserInterface;
using Meta.XR.Editor.UserInterface.RLDS;
using Meta.XR.Editor.Utils;
using Meta.XR.Guides.Editor.SdkUpgrader;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using RLDSButton = Meta.XR.Editor.UserInterface.RLDS.Button;
using Label = UnityEngine.UIElements.Label;
using ScrollView = UnityEngine.UIElements.ScrollView;
using Toggle = Meta.XR.Editor.UserInterface.Toggle;

namespace Meta.XR.Guides.Editor.Welcome
{
    internal class WelcomeWindow : RLDSEditorWindow
    {
        private const string TelemetryWindowId = "WelcomeWindow";

        protected override string TelemetryId => TelemetryWindowId;

        private static readonly TextureContent DownloadIcon =
            TextureContent.CreateContent("download.png", TextureContent.Categories.Generic, null);

        private static readonly TextureContent SetupIcon =
            TextureContent.CreateContent("feature_tools.png", TextureContent.Categories.Generic, null);

        private static readonly TextureContent ExternalLinkIcon =
            TextureContent.CreateContent("external_link.png", TextureContent.Categories.Generic, null);

        private static readonly TextureContent PinIcon =
            TextureContent.CreateContent("pin.png", TextureContent.Categories.Generic, null);

        // The upgrade pill reuses the shared SDK Update Assistant icon (Styles.Contents.SdkUpdaterIcon),
        // which ships in all builds — no internal guard needed.
        private static readonly TextureContent UpgradeIcon =
            Meta.XR.Editor.UserInterface.Styles.Contents.SdkUpdaterIcon;

        private static WelcomeWindow _instance;

        private const string AlertBannerDismissedKey = "Welcome.AlertBannerDismissed";

        private static bool AlertBannerDismissed => EditorUserSettings.GetConfigValue(AlertBannerDismissedKey) == "true";

        private static void SetAlertBannerDismissed(bool value)
        {
            EditorUserSettings.SetConfigValue(AlertBannerDismissedKey, value ? "true" : "false");
        }

        private const string ShowOnLaunchKey = "Welcome.ShowOnLaunch";

        private static bool ShowOnLaunchValue
        {
            get => EditorUserSettings.GetConfigValue(ShowOnLaunchKey) != "false";
            set => EditorUserSettings.SetConfigValue(ShowOnLaunchKey, value ? "true" : "false");
        }

        internal static bool ShouldShowOnLaunch => ShowOnLaunchValue;

        internal static void Show(Origins origin)
        {
            if (_instance != null)
            {
                _instance.Focus();
                return;
            }

            // Open as a dockable tab (utility:false), not a floating utility window. No maxSize
            // lock so it can be docked and resized.
            var window = GetWindow<WelcomeWindow>(false, WelcomeSettings.WindowTitle);
            window.minSize = new Vector2(WelcomeSettings.WindowWidth, WelcomeSettings.WindowHeight);
            _instance = window;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            _instance = this;
            // Defensive -= before += so a re-enable (without OnDisable firing) can't double-subscribe.
            WelcomeContentManager.OnContentChanged -= OnExternalStateChanged;
            WelcomeContentManager.OnContentChanged += OnExternalStateChanged;
            // Rebuild when the SDK version/update check resolves so the hero's upgrade CTA appears.
            SdkUpgraderData.OnChanged -= OnExternalStateChanged;
            SdkUpgraderData.OnChanged += OnExternalStateChanged;
            MetaVrCliInstaller.StateChanged -= OnExternalStateChanged;
            MetaVrCliInstaller.StateChanged += OnExternalStateChanged;
            FeatureRampUpManager.KeysReady -= OnExternalStateChanged;
            FeatureRampUpManager.KeysReady += OnExternalStateChanged;
            SdkUpgraderData.EnsureLoaded();
            if (FeatureRampUpManager.AreKeysReady)
            {
                OnExternalStateChanged();
            }
            EditorApplication.delayCall += RebuildIfEmpty;
            StartToolbarPolling();
        }

        private void OnDisable()
        {
            WelcomeContentManager.OnContentChanged -= OnExternalStateChanged;
            SdkUpgraderData.OnChanged -= OnExternalStateChanged;
            MetaVrCliInstaller.StateChanged -= OnExternalStateChanged;
            FeatureRampUpManager.KeysReady -= OnExternalStateChanged;
            StopToolbarPolling();
        }

        protected override void OnDestroy()
        {
            StopToolbarPolling();
            base.OnDestroy();
            WelcomeContentManager.OnContentChanged -= OnExternalStateChanged;
            SdkUpgraderData.OnChanged -= OnExternalStateChanged;
            MetaVrCliInstaller.StateChanged -= OnExternalStateChanged;
            FeatureRampUpManager.KeysReady -= OnExternalStateChanged;
            _instance = null;
        }

        // Remote content can arrive on a background fetch continuation; marshal the rebuild onto the
        // editor tick so we don't touch UIToolkit from an unexpected context. Content, SDK-version,
        // and CLI-installer changes feed this, and any can fire several times before the tick
        // resolves (e.g. as EnsureLoaded progresses), so coalesce to at most one rebuild per tick.
        private bool _rebuildQueued;

        private void OnExternalStateChanged()
        {
            if (_rebuildQueued) return;
            _rebuildQueued = true;
            EditorApplication.delayCall += RebuildSafely;
        }

        private void RebuildSafely()
        {
            _rebuildQueued = false;
            if (this == null) return;
            BuildUI();
        }

        private void OnBecameVisible() { RebuildIfEmpty(); StartToolbarPolling(); }

        private void OnFocus() { RebuildIfEmpty(); StartToolbarPolling(); }

        private void OnLostFocus() { StopToolbarPolling(); }

        private void OnBecameInvisible() { StopToolbarPolling(); }

#if USE_MAINTOOLBAR
        private static bool _lastToolbarVisibleState;
        private bool _isPollingToolbar;
        private double _nextToolbarPollTime;

        private void StartToolbarPolling()
        {
            if (_isPollingToolbar) return;
            _lastToolbarVisibleState = Meta.XR.Editor.StatusMenu.Dropdown.IsToolbarButtonVisible(true);
            EditorApplication.update += PollToolbarState;
            _isPollingToolbar = true;
        }

        private void StopToolbarPolling()
        {
            if (!_isPollingToolbar) return;
            EditorApplication.update -= PollToolbarState;
            _isPollingToolbar = false;
        }

        private void PollToolbarState()
        {
            // Throttle to twice per second – toolbar pin is infrequent UI action, no need per-frame cost.
            if (EditorApplication.timeSinceStartup < _nextToolbarPollTime) return;
            _nextToolbarPollTime = EditorApplication.timeSinceStartup + 0.5;

            bool current = Meta.XR.Editor.StatusMenu.Dropdown.IsToolbarButtonVisible(true);
            if (current != _lastToolbarVisibleState)
            {
                _lastToolbarVisibleState = current;
                // Rebuild UI to show/hide Open SDK button immediately without requiring window reopen.
                BuildUI();
                Repaint();
            }
        }
#else
        private void StartToolbarPolling() { }
        private void StopToolbarPolling() { }
#endif

        private void RebuildIfEmpty()
        {
            if (this == null) return;
            if (rootVisualElement.childCount == 0)
            {
                BuildUI();
            }
        }

        internal void BuildUI()
        {
            if (this == null) return;
            var root = rootVisualElement;
            root.Clear();
            // Tag the window so every RLDS widget inside reports this surface as its navigation path.
            RLDSTelemetry.SetScope(root, Origins.GuidedSetup, TelemetryWindowId);

            try
            {
                var styleSheet = RLDSUtils.LoadStyleSheet(!EditorGUIUtility.isProSkin);
                if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
                {
                    root.styleSheets.Add(styleSheet);
                }

                var scrollView = new ScrollView(ScrollViewMode.Vertical);
                scrollView.AddToClassList(RLDSConstants.Flexbox.Grow1);
                root.Add(scrollView);

                var content = scrollView.contentContainer;

                BuildCoverSection(content);
                BuildAlertBannerSection(content);
                BuildFeaturedSection(content);
                BuildResourcesSection(content);
                BuildXrToolsSection(content);

                // Footer is part of the scrollable page (scrolls away at the top), not pinned.
                var footer = BuildFooter();
                content.Add(footer);
            }
            catch (Exception e)
            {
                // A transient failure (e.g. tool registry not ready on a restored window) must not
                // leave a half-built, blank window. Clear so OnFocus/OnBecameVisible can rebuild
                // cleanly on the next interaction.
                Debug.LogWarning($"[Welcome] Failed to build window, will retry on focus: {e}");
                root.Clear();
            }
        }

        #region Cover

        private static void ApplyWhenLoaded(TextureContent content, VisualElement target, System.Action<UnityEngine.Texture> apply)
        {
            if (content.Valid)
            {
                apply(content.Image);
                return;
            }

            content.RegisterToImageLoaded(tex =>
            {
                if (target.panel != null)
                {
                    apply(tex);
                }
            });
        }

        private void BuildCoverSection(VisualElement parent)
        {
            var cover = new CoverImage
            {
                FullBleed = true,
                CoverIcon = Meta.XR.Editor.UserInterface.Styles.Contents.SdkCoverIcon
            };

            var coverElement = cover.Build();
            ApplyWhenLoaded(Meta.XR.Editor.UserInterface.Styles.Contents.CoverBg, coverElement,
                tex => coverElement.style.backgroundImage = new StyleBackground(tex as Texture2D));
            parent.Add(coverElement);

            cover.ContentArea.style.paddingLeft = RLDSConstants.Spacing.Size3XL;
            cover.ContentArea.style.paddingRight = RLDSConstants.Spacing.Size3XL;
            cover.ContentArea.style.paddingBottom = RLDSConstants.Spacing.Size3XL;

            // GetSdkVersion() reads OVRPlugin, so the version resolves even when the SDK is
            // in-project source rather than an installed package (where ComputePackageVersion is 0).
            var version = ToolUsage.GetSdkVersion();
            if (version.HasValue)
            {
                var versionRow = new VisualElement();
                versionRow.AddToClassList(RLDSConstants.Flexbox.Row);
                versionRow.AddToClassList(RLDSConstants.Flexbox.AlignCenter);

                var versionBadge = new BadgePill($"Version {version.Value}", BadgePillType.Neutral, BadgePillSize.Small).Build();
                versionRow.Add(versionBadge);

                // Surface the SDK Upgrade Guide right where the version is shown, but only when an
                // update exists. A clickable warning pill — same size/feel as the version badge, with
                // a leading upgrade icon and a hand cursor so it reads as actionable — rather than a
                // full button. It opens the guide as a tab docked beside this Welcome window.
                if (StatusMenu.IsSdkUpgradeAssistantEnabled && SdkUpgraderData.IsUpdateAvailable)
                {
                    // Built from segments so "vN available" reads bold and "click to explore" reads as
                    // the normal-weight call to action.
                    var upgradePill = new VisualElement();
                    upgradePill.AddToClassList(RLDSConstants.BadgePill.Base);
                    upgradePill.AddToClassList(RLDSConstants.BadgePill.Warning);
                    upgradePill.AddToClassList(RLDSConstants.BadgePill.Small);
                    upgradePill.AddToClassList(RLDSConstants.Flexbox.Row);
                    upgradePill.AddToClassList(RLDSConstants.Flexbox.AlignCenter);
                    upgradePill.AddToClassList(RLDSConstants.Utilities.CursorLink);
                    upgradePill.style.marginLeft = RLDSConstants.Spacing.SizeXS;

                    // Leading upgrade icon so the pill reads as an SDK-upgrade CTA (not the external-link glyph).
                    var upgradeIcon = new UnityEngine.UIElements.Image
                    {
                        scaleMode = UnityEngine.ScaleMode.ScaleToFit,
                        pickingMode = PickingMode.Ignore
                    };
                    // icon-size-xs (12px) matches the native badge-pill icon so the pill height lines
                    // up with the sibling version badge (16px was taller than the pill's text).
                    upgradeIcon.style.width = RLDSConstants.IconSize.SizeXS;
                    upgradeIcon.style.height = RLDSConstants.IconSize.SizeXS;
                    upgradeIcon.style.marginRight = RLDSConstants.Spacing.Size3XS;
                    ApplyWhenLoaded(UpgradeIcon, upgradeIcon, tex => upgradeIcon.image = tex as Texture);
                    upgradePill.Add(upgradeIcon);

                    var availableLabel = new Label($"v{SdkUpgraderData.LatestVersion} available");
                    availableLabel.AddToClassList(RLDSConstants.BadgePill.Label);
                    availableLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                    availableLabel.pickingMode = PickingMode.Ignore;
                    upgradePill.Add(availableLabel);

                    var ctaLabel = new Label("click to explore");
                    ctaLabel.AddToClassList(RLDSConstants.BadgePill.Label);
                    ctaLabel.style.marginLeft = RLDSConstants.Spacing.Size3XS;
                    ctaLabel.pickingMode = PickingMode.Ignore;
                    upgradePill.Add(ctaLabel);

                    upgradePill.RegisterCallback<MouseEnterEvent>(_ => upgradePill.style.opacity = RLDSConstants.Opacity.Hover);
                    upgradePill.RegisterCallback<MouseLeaveEvent>(_ => upgradePill.style.opacity = RLDSConstants.Opacity.Default);
                    upgradePill.RegisterCallback<ClickEvent>(_ =>
                    {
                        RLDSTelemetry.SendInteraction(
                            upgradePill, "UpgradePill", "WelcomeUpgradeAvailable",
                            $"v{SdkUpgraderData.LatestVersion} available");
                        OnOpenUpgradeGuide();
                    });
                    versionRow.Add(upgradePill);
                }

                cover.ContentArea.Add(versionRow);
            }

            // Cover title is fixed product branding — not remote-controlled.
            var title = new Label(WelcomeSettings.Labels.CoverTitle);
            title.AddToClassList(RLDSConstants.Typography.Heading1);
            title.AddToClassList(RLDSConstants.Utilities.MarginTopXS);
            cover.ContentArea.Add(title);

            var subtitle = new Label(WelcomeContentManager.Content.cover.subtitle);
            subtitle.AddToClassList(RLDSConstants.Typography.Body1Text);
            subtitle.AddToClassList(RLDSConstants.Utilities.MarginTopXS);
            cover.ContentArea.Add(subtitle);

            var buttonRow = new VisualElement();
            buttonRow.AddToClassList(RLDSConstants.Flexbox.Row);
            buttonRow.AddToClassList(RLDSConstants.Utilities.MarginTopSM);

            // The Open SDK menu button opens the windowed SDK menu, which is available on every Unity
            // version regardless of whether the toolbar dropdown button is pinned, so always show it.
            var openSdkBtn = new RLDSButton(
                new ActionLinkDescription
                {
                    Content = new GUIContent(WelcomeSettings.Labels.OpenSdkMenuButton),
                    Action = OnOpenSdkMenu,
                    Id = "WelcomeOpenSdkMenu",
                    Origin = Origins.GuidedSetup,
                    OriginData = null
                },
                RLDSConstants.ButtonVariant.Primary,
                RLDSConstants.ButtonSize.Large).Build();
            buttonRow.Add(openSdkBtn);

            var releaseNotesBtn = new RLDSButton(
                new ActionLinkDescription
                {
                    Content = new GUIContent(WelcomeSettings.Labels.ReleaseNotesButton),
                    Action = OnViewReleaseNotes,
                    Id = "WelcomeViewReleaseNotes",
                    Origin = Origins.GuidedSetup,
                    OriginData = null
                },
                RLDSConstants.ButtonVariant.OnMedia,
                RLDSConstants.ButtonSize.Large).Build();
            releaseNotesBtn.style.marginLeft = RLDSConstants.Spacing.SizeSM;
            buttonRow.Add(releaseNotesBtn);

            cover.ContentArea.Add(buttonRow);
        }

        #endregion

        #region Featured

        private void BuildFeaturedSection(VisualElement parent)
        {
            // Show the highlight whose SDK window contains the running SDK; none -> section hidden.
            var sdkVersion = ToolUsage.GetSdkVersion();
            if (sdkVersion == null) return;
            var selected = WelcomeContentManager.SelectFeatured(WelcomeContentManager.Content.featured, sdkVersion.Value);
            if (selected == null) return;
            var f = selected.Value;

            var section = new VisualElement();
            section.style.paddingTop = RLDSConstants.Spacing.SizeMD;
            section.style.paddingBottom = RLDSConstants.Spacing.SizeMD;
            section.style.paddingLeft = RLDSConstants.Spacing.Size3XL;
            section.style.paddingRight = RLDSConstants.Spacing.Size3XL;

            var header = new Label(f.header);
            header.AddToClassList(RLDSConstants.Typography.Heading2);
            header.style.marginBottom = RLDSConstants.Spacing.SizeMD;
            header.style.opacity = 0.7f;
            section.Add(header);

            var card = new VisualElement();
            card.AddToClassList(RLDSConstants.HighlightCard.Root);

            var content = new VisualElement();
            content.AddToClassList(RLDSConstants.HighlightCard.Content);

            var textGroup = new VisualElement();
            textGroup.AddToClassList(RLDSConstants.HighlightCard.TextGroup);

            var title = new Label(f.title);
            title.AddToClassList(RLDSConstants.HighlightCard.Title);
            textGroup.Add(title);

            var description = new Label(f.description);
            description.AddToClassList(RLDSConstants.HighlightCard.Description);
            textGroup.Add(description);

            content.Add(textGroup);

            if (f.buttons is { Length: > 0 })
            {
                var buttonRow = new VisualElement();
                buttonRow.AddToClassList(RLDSConstants.HighlightCard.ButtonRow);

                var buttonIndex = 0;
                foreach (var btn in f.buttons)
                {
                    // Skip malformed entries from a partial blob — a button with no label or no
                    // action target can't render or do anything useful.
                    if (string.IsNullOrEmpty(btn.label) || string.IsNullOrEmpty(btn.value))
                    {
                        continue;
                    }

                    var variant = buttonIndex == 0 ? RLDSConstants.ButtonVariant.Secondary : RLDSConstants.ButtonVariant.Primary;

                    var buttonLabel = btn.label;
                    var buttonValue = btn.value;
                    var buttonType = btn.type;

                    // Id intentionally reflects position + label: featured content is remote-
                    // controlled, so a blob edit that reorders or relabels buttons is treated as new
                    // content (telemetry id churn on remote updates is acceptable here).
                    var button = new RLDSButton(
                        new ActionLinkDescription
                        {
                            Content = new GUIContent(buttonLabel),
                            Action = () => ExecuteButtonAction(buttonType, buttonValue),
                            Id = $"Featured_{buttonIndex}_{buttonLabel.Replace(' ', '_')}",
                            Origin = Origins.GuidedSetup,
                            OriginData = null
                        },
                        variant,
                        RLDSConstants.ButtonSize.Small).Build();
                    buttonRow.Add(button);
                    buttonIndex++;
                }

                content.Add(buttonRow);
            }

            card.Add(content);

            // The image ships only via remote content; no device image is compiled into the build.
            // With no imageContentId the card renders text-only.
            if (f.imageContentId != 0)
            {
                var image = new VisualElement();
                image.AddToClassList(RLDSConstants.HighlightCard.Image);
                var remoteImage = RemoteTextureContent.CreateWithAutoDownload(
                    f.imageContentId, TextureContent.Categories.Generic);
                remoteImage.RegisterToImageLoaded(tex =>
                    image.style.backgroundImage = new StyleBackground(tex as Texture2D));
                card.Add(image);
            }

            section.Add(card);
            parent.Add(section);
        }

        private static void ExecuteButtonAction(string type, string value)
        {
            if (string.IsNullOrEmpty(value)) return;

            switch (type)
            {
                case "url":
                    Application.OpenURL(value);
                    break;
                case "tool":
                    // Exact name match (a tool's Name is its stable identity) so a short or generic
                    // remote value can't accidentally resolve to the wrong tool.
                    var tool = ToolRegistry.Registry
                        .FirstOrDefault(t => t.Name == value);
                    if (tool != null)
                    {
                        tool.OnClickDelegate?.Invoke(Origins.GuidedSetup);
                    }
                    else
                    {
                        Debug.LogWarning($"[Welcome] Could not find tool '{value}' in ToolRegistry");
                    }
                    break;
                default:
                    Debug.LogWarning($"[Welcome] Unknown featured button type '{type}' (expected 'url' or 'tool').");
                    break;
            }
        }

        #endregion

        #region AlertBanner

        private void BuildAlertBannerSection(VisualElement parent)
        {
            if (AlertBannerDismissed)
            {
                return;
            }

#if USE_MAINTOOLBAR
            var message = AlertBannerMessages.AlertBannerMessagePinned;
            var icon = PinIcon;
#else
            var message = AlertBannerMessages.AlertBannerMessage;
            TextureContent icon = null;
#endif
            var banner = new AlertBanner(message, icon, () => SetAlertBannerDismissed(true), wideMargins: true);
            var bannerElement = banner.Build();
            parent.Add(bannerElement);
        }

        #endregion

        #region Resources

        private void BuildResourcesSection(VisualElement parent)
        {
            var section = new VisualElement();
            section.AddToClassList(RLDSConstants.Utilities.Padding2xMD);
            section.style.paddingTop = RLDSConstants.Spacing.SizeMD;
            section.style.paddingBottom = RLDSConstants.Spacing.SizeMD;
            section.style.paddingLeft = RLDSConstants.Spacing.Size3XL;
            section.style.paddingRight = RLDSConstants.Spacing.Size3XL;

            var header = new Label(WelcomeSettings.Labels.ResourcesHeader);
            header.AddToClassList(RLDSConstants.Typography.Heading2);
            header.style.marginBottom = RLDSConstants.Spacing.SizeMD;
            header.style.opacity = 0.7f;
            section.Add(header);

            var grid = new VisualElement();
            grid.AddToClassList(RLDSConstants.Flexbox.Row);
            grid.AddToClassList(RLDSConstants.Flexbox.Wrap);
            grid.style.justifyContent = Justify.FlexStart;

            // Building Blocks first (fixed in-editor card), then the remote-controlled cards.
            AddResourceCard(grid, WelcomeSettings.BuildingBlocks);

            foreach (var res in WelcomeContentManager.Content.resources)
            {
                // Skip a remote card that duplicates the hardcoded Building Blocks card.
                if (string.Equals(res.label, WelcomeSettings.BuildingBlocks.Label, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Skip malformed remote entries — a card missing label/linkText/url would render
                // blank and its link would open an empty URL.
                if (string.IsNullOrEmpty(res.label) ||
                    string.IsNullOrEmpty(res.linkText) ||
                    string.IsNullOrEmpty(res.url))
                {
                    Debug.LogWarning(
                        $"[Welcome] Skipping malformed remote resource '{res.label}' (missing label, link text, or url).");
                    continue;
                }

                AddResourceCard(grid, new WelcomeSettings.ResourceDef(res.label, res.description, res.linkText, res.url));
            }

            section.Add(grid);
            parent.Add(section);
        }

        private void AddResourceCard(VisualElement grid, WelcomeSettings.ResourceDef res)
        {
            var card = new FeatureCard(
                res.Label,
                res.Description,
                FeatureCardVariant.Interactive,
                linkText: res.LinkText,
                linkIcon: res.OpensUrl ? ExternalLinkIcon : null,
                id: res.Label);
            if (res.LinkAction != null)
            {
                card.LinkClicked += res.LinkAction;
            }

            var cardElement = card.Build();
            cardElement.style.flexGrow = 1;
            cardElement.style.flexBasis = 0;
            cardElement.style.marginRight = RLDSConstants.Spacing.SizeSM;
            cardElement.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            grid.Add(cardElement);
        }

        #endregion

        #region XR Tools

        private void BuildXrToolsSection(VisualElement parent)
        {
            var section = new VisualElement();
            section.AddToClassList(RLDSConstants.Utilities.Padding2xMD);
            section.style.paddingTop = RLDSConstants.Spacing.SizeMD;
            section.style.paddingBottom = RLDSConstants.Spacing.SizeMD;
            section.style.paddingLeft = RLDSConstants.Spacing.Size3XL;
            section.style.paddingRight = RLDSConstants.Spacing.Size3XL;

            var header = new Label(WelcomeSettings.Labels.XrToolsHeader);
            header.AddToClassList(RLDSConstants.Typography.Heading2);
            header.style.marginBottom = RLDSConstants.Spacing.SizeMD;
            header.style.opacity = 0.7f;
            section.Add(header);

            var grid = new VisualElement();
            grid.AddToClassList(RLDSConstants.Flexbox.Row);
            grid.AddToClassList(RLDSConstants.Flexbox.Wrap);
            grid.style.justifyContent = Justify.FlexStart;

            var toolOrder = WelcomeContentManager.Content.xrToolsOrder;
            IEnumerable<WelcomeSettings.XrToolDef> orderedTools;
            if (toolOrder is { Length: > 0 })
            {
                // Resolve each hint to a known tool, warning once on any that don't match. Done as an
                // explicit loop (not a LINQ projection) so the warning isn't a side effect in Select.
                var resolved = new List<WelcomeSettings.XrToolDef>();
                foreach (var hint in toolOrder)
                {
                    var tool = Array.Find(WelcomeSettings.XrTools.All, t => t.RegistryNameHint == hint);
                    if (tool.Label == null)
                    {
                        Debug.LogWarning($"[Welcome] xrToolsOrder hint '{hint}' matched no known XR tool; skipping.");
                        continue;
                    }
                    resolved.Add(tool);
                }
                orderedTools = resolved;
            }
            else
            {
                orderedTools = WelcomeSettings.XrTools.All;
            }

            foreach (var def in orderedTools)
            {
                var content = Array.Find(
                    WelcomeContentManager.Content.xrTools,
                    tool => tool.id == def.RegistryNameHint);
                var configuredDef = WelcomeSettings.XrTools.ApplyRemoteContent(def, content);

                if (configuredDef.WindowsOnly && Application.platform != RuntimePlatform.WindowsEditor)
                {
                    continue;
                }

                var descriptor = ToolRegistry.Registry
                    .FirstOrDefault(t => t.Name != null && t.Name.Contains(configuredDef.RegistryNameHint));

                var toolIcon = descriptor?.Icon ?? ResolveFallbackIcon(configuredDef.RegistryNameHint);
                var active = configuredDef.IsActive?.Invoke() == true;
                var ctaText = active ? configuredDef.ActiveCtaText : configuredDef.InactiveCtaText;
                var ctaAction = active ? configuredDef.ActiveAction : configuredDef.InactiveAction;
                var badgeText = active ? configuredDef.ActiveBadgeText : configuredDef.InactiveBadgeText;
                var badgeType = active ? BadgeTagType.Positive : BadgeTagType.Neutral;
                var ctaEnabled = true;
                var infoAction = active ? null : configuredDef.InfoAction;

                if (configuredDef.RegistryNameHint == "Meta VR CLI" && !active)
                {
                    var supportsDirectInstall = MetaVrCliInstaller.SupportsDirectInstall(Application.platform);
                    if (MetaVrCliInstaller.IsInstalling)
                    {
                        ctaText = "Installing…";
                        badgeText = "Installing";
                        badgeType = BadgeTagType.Info;
                        ctaEnabled = false;
                    }
                    else
                    {
                        var ctaState = WelcomeSettings.XrTools.ResolveMetaVrCliCta(
                            configuredDef,
                            supportsDirectInstall);
                        ctaText = ctaState.Text;
                        ctaAction = ctaState.Action;
                        infoAction = ctaState.InfoAction;
                    }
                }

                var ctaIcon = ctaText is "Download" or "Install" ? DownloadIcon
                    : ctaText == "Setup" ? SetupIcon
                    : null;

                var card = new FeatureCard(
                    configuredDef.Label,
                    configuredDef.Description,
                    FeatureCardVariant.Cta,
                    icon: toolIcon,
                    ctaText: ctaText,
                    ctaIcon: ctaIcon,
                    badgeText: badgeText,
                    badgeType: badgeType,
                    isActive: active,
                    ctaEnabled: ctaEnabled,
                    infoIcon: infoAction != null ? Meta.XR.Editor.UserInterface.Styles.Contents.InfoMaskIcon : null,
                    infoTooltip: infoAction != null ? $"Learn more about {configuredDef.Label}" : null,
                    id: configuredDef.RegistryNameHint);

                if (ctaAction != null)
                {
                    card.CtaClicked += ctaAction;
                }
                if (infoAction != null)
                {
                    card.InfoClicked += infoAction;
                }

                var cardElement = card.Build();
                cardElement.style.width = 298;
                cardElement.style.marginRight = RLDSConstants.Spacing.SizeSM;
                cardElement.style.marginBottom = RLDSConstants.Spacing.SizeSM;
                grid.Add(cardElement);
            }

            section.Add(grid);
            parent.Add(section);
        }

        /// <summary>
        /// Resolves an icon for XR Tools cards whose backing tool is not present in the
        /// <see cref="ToolRegistry"/> (e.g. external companion apps) and therefore exposes no
        /// descriptor icon. Returns null for tools that already supply an icon via their descriptor,
        /// letting the caller fall back to that descriptor icon.
        /// </summary>
        private static TextureContent ResolveFallbackIcon(string registryNameHint) => registryNameHint switch
        {
            "AI Agent Bridge" => TextureContent.CreateContent("feature_ai_agent.png", TextureContent.Categories.Generic, null),
            "Meta VR CLI" => TextureContent.CreateContent("metavr_cli_terminal.png", TextureContent.Categories.Generic, null),
            "Runtime Optimizer" => TextureContent.CreateContent("ovr_icon_runtime_optimizer.png", TextureContent.Categories.Generic, null),
            "Meta Quest Developer Hub" => TextureContent.CreateContent("ovr_icon_meta.png", TextureContent.Categories.Generic, null),
            "Meta Quest Link" => TextureContent.CreateContent("ovr_icon_link.png", TextureContent.Categories.Generic, null),
            "RenderDoc" => TextureContent.CreateContent("ovr_icon_gpu.png", TextureContent.Categories.Generic, null),
            "Meta Haptics Studio" => TextureContent.CreateContent("ovr_icon_vibration.png", TextureContent.Categories.Generic, null),
            _ => null
        };

        #endregion

        #region Footer

        private VisualElement BuildFooter()
        {
            var footer = new VisualElement();
            footer.AddToClassList(RLDSConstants.Flexbox.Row);
            footer.AddToClassList(RLDSConstants.Flexbox.AlignCenter);
            footer.AddToClassList(RLDSConstants.Utilities.Padding2xMD);
            footer.style.paddingTop = RLDSConstants.Spacing.SizeMD;
            footer.style.paddingBottom = RLDSConstants.Spacing.SizeMD;
            footer.style.paddingLeft = RLDSConstants.Spacing.Size3XL;
            footer.style.paddingRight = RLDSConstants.Spacing.Size3XL;
            footer.AddToClassList("rlds-welcome-footer");

            var leftActions = new VisualElement();
            leftActions.AddToClassList(RLDSConstants.Flexbox.Row);
            leftActions.AddToClassList(RLDSConstants.Flexbox.AlignCenter);
            leftActions.AddToClassList(RLDSConstants.Flexbox.Grow1);

            var toggle = new Toggle(
                WelcomeSettings.Labels.ShowOnLaunchToggle,
                ShowOnLaunchValue,
                RLDSConstants.Typography.Body2SmallLabel,
                value => ShowOnLaunchValue = value);
            leftActions.Add(toggle.Build());

            footer.Add(leftActions);

            var closeBtn = new RLDSButton(
                new ActionLinkDescription
                {
                    Content = new GUIContent(WelcomeSettings.Labels.CloseButton),
                    Action = OnClose,
                    Id = "WelcomeClose",
                    Origin = Origins.GuidedSetup,
                    OriginData = null
                },
                RLDSConstants.ButtonVariant.Secondary,
                RLDSConstants.ButtonSize.Large).Build();
            footer.Add(closeBtn);

            return footer;
        }

        #endregion

        #region Actions

        private void OnOpenSdkMenu()
        {
            Close();
            EditorApplication.delayCall += Meta.XR.Editor.StatusMenu.StatusMenu.ShowWindow;
        }

        private void OnOpenUpgradeGuide()
        {
            // Dock the guide as a tab beside this Welcome window rather than replacing it, so the
            // two read as adjacent tabs (GetType() is WelcomeWindow — the dock target).
            SdkUpgraderWindow.ShowDockedNextTo(GetType());
        }

        private void OnViewReleaseNotes()
        {
            Application.OpenURL(WelcomeSettings.ReleaseNotesUrl);
        }

        private void OnClose()
        {
            Close();
        }

        #endregion
    }
}
