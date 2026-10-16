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

using System;
using System.Collections.Generic;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.ToolingSupport;
using Meta.XR.Editor.UserInterface;
using Meta.XR.Editor.UserInterface.RLDS;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using RLDSButton = Meta.XR.Editor.UserInterface.RLDS.Button;
using Label = UnityEngine.UIElements.Label;
using ScrollView = UnityEngine.UIElements.ScrollView;
using UIStyles = Meta.XR.Editor.UserInterface.Styles;

namespace Meta.XR.Guides.Editor.SdkUpgrader
{
    /// <summary>
    /// The "SDK Upgrade Assistant" editor window: a colorful hero, an "Upgrade with AI" section
    /// pointing at the curated SDK Upgrade Skill (MetaVR CLI or GitHub), and the per-version release
    /// notes. It registers itself in the status menu so the update banner and Welcome hero pill route
    /// here.
    /// </summary>
    /// <remarks>
    /// This is a standard dockable <see cref="EditorWindow"/>, not a single window with in-window
    /// tabs. It opens standalone from the SDK menu (<see cref="Show"/>), or as a tab beside the
    /// existing Welcome window when launched from there (<see cref="ShowDockedNextTo"/>).
    /// </remarks>
    [InitializeOnLoad]
    internal class SdkUpgraderWindow : RLDSEditorWindow, IIdentified
    {
        private const string WindowName = "SDK Upgrade Assistant";
        private const string TelemetryWindowId = "SdkUpgraderWindow";

        // Focus target for the hero "Open Package Manager" button (the version-defining SDK package).
        private const string CorePackageId = "com.meta.xr.sdk.core";

        protected override string TelemetryId => TelemetryWindowId;
        protected override Origins TelemetryOrigin => Origins.GuidedSetup;

        private static readonly TextureContent ExternalLinkIcon =
            TextureContent.CreateContent("external_link.png", TextureContent.Categories.Generic, null);

        private static readonly TextureContent CopyIcon =
            TextureContent.CreateContent("copy.png", TextureContent.Categories.Generic, null);

        private static readonly TextureContent HelpIcon =
            TextureContent.CreateContent("ovr_icon_info.png", TextureContent.Categories.Generic, null);

        private static readonly TextureContent WindowIcon =
            TextureContent.CreateContent("ovr_icon_meta.png", TextureContent.Categories.Generic, null);

        // The internal hero design uses a "find in page" scan watermark; that cover icon is
        // internal-only, so shipping builds fall back to the generic SDK cover icon.
        private static readonly TextureContent HeroCoverIcon =
            UIStyles.Contents.SdkCoverIcon;

        public static readonly ToolDescriptor Tool = new()
        {
            Icon = WindowIcon,
            Name = WindowName,
            MenuDescription = "Review changes and upgrade with confidence",
            // Not a permanent menu row / Unity menu item — the assistant is surfaced only when an
            // update exists, via the status-menu update banner and the Welcome hero pill. It stays
            // registered so those surfaces can resolve it through the ToolRegistry.
            AddToStatusMenu = false,
            AddToMenu = false,
            MenuCategory = MenuCategory.Tools,
            OnClickDelegate = Show,
            AvailableVersionDelegate = () =>
                SdkUpgraderData.IsUpdateAvailable ? SdkUpgraderData.LatestVersion : (int?)null,
            PillIcon = () => SdkUpgraderData.IsUpdateAvailable
                ? ((TextureContent)null, (Color?)UIStyles.Colors.WarningColor, true)
                : ((TextureContent)null, (Color?)null, false)
        };

        public string Id => Tool.Id;

        static SdkUpgraderWindow()
        {
            // Touch Tool so its ToolDescriptor self-registers with the ToolRegistry on editor load.
            _ = Tool;
        }

        /// <summary>Opens the assistant as a standalone dockable window (the SDK menu entry point).</summary>
        internal static void Show(Origins _) => Open(null);

        /// <summary>
        /// Opens the assistant docked as a tab beside <paramref name="dockNextTo"/> (e.g. the Welcome
        /// window), so the two read as adjacent tabs — the entry point from the Welcome screen.
        /// </summary>
        internal static void ShowDockedNextTo(Type dockNextTo) => Open(dockNextTo);

        private static void Open(Type dockNextTo)
        {
            var window = dockNextTo != null
                ? GetWindow<SdkUpgraderWindow>(WindowName, false, dockNextTo)
                : GetWindow<SdkUpgraderWindow>(false, WindowName);
            window.minSize = new Vector2(880, 640);
            window.Focus();
        }

        private void CreateGUI() => BuildUI();

        protected override void OnEnable()
        {
            base.OnEnable();
            // Defensive -= before += so re-enabling the same instance can't double-subscribe.
            SdkUpgraderData.OnChanged -= OnDataChanged;
            SdkUpgraderData.OnChanged += OnDataChanged;
            SdkUpgraderContentManager.OnContentChanged -= OnDataChanged;
            SdkUpgraderContentManager.OnContentChanged += OnDataChanged;
            SdkUpgraderData.EnsureLoaded();
        }

        // Unity magic method (the RLDS base overrides OnEnable/OnDestroy but not OnDisable). Unsubscribe
        // here rather than in OnDestroy: OnDisable pairs with OnEnable and also fires on domain reloads,
        // so a recompiled/prior instance's handler can't linger on the static events.
        private void OnDisable()
        {
            SdkUpgraderData.OnChanged -= OnDataChanged;
            SdkUpgraderContentManager.OnContentChanged -= OnDataChanged;
        }

        private void OnDataChanged()
        {
            if (this == null) return;
            // Only rebuild an already-built window; the first build comes from CreateGUI.
            if (rootVisualElement is { childCount: > 0 })
            {
                BuildUI();
            }
        }

        private void OnFocus()
        {
            if (this == null) return;
            if (rootVisualElement is { childCount: 0 })
            {
                BuildUI();
            }
        }

        private void BuildUI()
        {
            if (this == null) return;

            var root = rootVisualElement;
            root.Clear();
            root.AddToClassList(RLDSConstants.Surface.Primary);
            root.style.flexGrow = 1;
            RLDSTelemetry.SetScope(root, TelemetryOrigin, TelemetryWindowId);

            var styleSheet = RLDSUtils.LoadStyleSheet(!EditorGUIUtility.isProSkin);
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            var contentScroll = new ScrollView(ScrollViewMode.Vertical);
            contentScroll.AddToClassList(RLDSConstants.Flexbox.Grow1);
            root.Add(contentScroll);

            BuildContent(contentScroll.contentContainer);
        }

        private void BuildContent(VisualElement parent)
        {
            BuildHero(parent);

            // The how-to and release-notes sections are only meaningful when an upgrade exists.
            if (SdkUpgraderData.IsUpdateAvailable)
            {
                var page = NewPage();
                page.Add(BuildUpgradeWithAiSection());
                page.Add(BuildReleaseNotesSection());
                parent.Add(page);
            }
        }

        #region Hero

        private void BuildHero(VisualElement parent)
        {
            var cover = new CoverImage
            {
                FullBleed = true,
                CoverIcon = HeroCoverIcon
            };

            var coverElement = cover.Build();
            ApplyWhenLoaded(UIStyles.Contents.CoverBg, coverElement,
                tex => coverElement.style.backgroundImage = new StyleBackground(tex as Texture2D));
            parent.Add(coverElement);

            cover.ContentArea.style.paddingLeft = RLDSConstants.Spacing.Size3XL;
            cover.ContentArea.style.paddingRight = RLDSConstants.Spacing.Size3XL;
            cover.ContentArea.style.paddingTop = RLDSConstants.Spacing.Size3XL;
            cover.ContentArea.style.paddingBottom = RLDSConstants.Spacing.Size3XL;

            string subtitleText;
            if (SdkUpgraderData.IsUpdateAvailable)
            {
                subtitleText =
                    "See what's new in each release, and use the tools Meta provides to navigate the upgrade.";
            }
            else if (SdkUpgraderData.IsLoading && !SdkUpgraderData.IsLoaded)
            {
                subtitleText = "Checking for updates…";
            }
            else
            {
                subtitleText = "Your installed Meta XR SDKs are up to date.";
            }

            // The "upgrade from xx to yy" pattern from the hero design: a multi-color
            // transition pill (faded current version → accent arrow → solid target) when an update
            // exists; a plain current-version pill otherwise.
            var versionPill = SdkUpgraderData.IsUpdateAvailable
                ? BuildVersionTransitionPill(SdkUpgraderData.CurrentVersion, SdkUpgraderData.LatestVersion)
                : new BadgePill($"Version {SdkUpgraderData.CurrentVersion}", BadgePillType.Neutral, BadgePillSize.Small).Build();
            cover.ContentArea.Add(versionPill);

            // The hero title is the product name in every state; the state (upgrade / up to date /
            // loading) is carried by the subtitle and the version pill.
            var title = new Label(WindowName);
            title.AddToClassList(RLDSConstants.Typography.Heading1);
            title.AddToClassList(RLDSConstants.Utilities.MarginTopXS);
            cover.ContentArea.Add(title);

            var subtitle = new Label(subtitleText);
            subtitle.AddToClassList(RLDSConstants.Typography.Body1Text);
            subtitle.AddToClassList(RLDSConstants.Utilities.MarginTopXS);
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            cover.ContentArea.Add(subtitle);

            // The actual version bump is a Package Manager action; offer it right from the hero.
            var actionRow = new VisualElement();
            actionRow.AddToClassList(RLDSConstants.Flexbox.Row);
            actionRow.style.marginTop = RLDSConstants.Spacing.SizeMD;

            var openPackageManager = new RLDSButton(
                new ActionLinkDescription
                {
                    Content = new GUIContent("Open Package Manager"),
                    Action = () => UnityEditor.PackageManager.UI.Window.Open(CorePackageId),
                    Id = "SdkUpgraderOpenPackageManager",
                    Origin = TelemetryOrigin,
                    OriginData = null
                },
                RLDSConstants.ButtonVariant.OnMedia,
                RLDSConstants.ButtonSize.Large).Build();
            actionRow.Add(openPackageManager);
            cover.ContentArea.Add(actionRow);
        }

        // Reuses the badge-pill container (so it inherits the cover's on-media styling) but renders
        // three segments: a faded current version, an accent arrow, and the solid target version.
        private static VisualElement BuildVersionTransitionPill(int from, int to)
        {
            var pill = new VisualElement();
            pill.AddToClassList(RLDSConstants.BadgePill.Base);
            pill.AddToClassList(RLDSConstants.BadgePill.Neutral);
            pill.AddToClassList(RLDSConstants.BadgePill.Small);

            var fromLabel = new Label($"Version {from}");
            fromLabel.AddToClassList(RLDSConstants.VersionPill.From);
            pill.Add(fromLabel);

            var arrow = new Label("→");
            arrow.AddToClassList(RLDSConstants.VersionPill.Arrow);
            pill.Add(arrow);

            var toLabel = new Label($"Version {to}");
            toLabel.AddToClassList(RLDSConstants.VersionPill.To);
            pill.Add(toLabel);

            return pill;
        }

        #endregion

        #region Upgrade with AI

        private VisualElement BuildUpgradeWithAiSection()
        {
            var content = SdkUpgraderContentManager.Content;

            var section = new VisualElement();
            section.style.marginBottom = RLDSConstants.Spacing.Size2XL;

            AddSectionHeading(section, content.heading);

            var intro = new Label(content.intro);
            intro.AddToClassList(RLDSConstants.Typography.Body1Text);
            intro.style.whiteSpace = WhiteSpace.Normal;
            intro.style.marginBottom = RLDSConstants.Spacing.SizeMD;
            section.Add(intro);

            section.Add(BuildInstallPanel(content));

            var runPanel = NewPanel();
            runPanel.style.marginTop = RLDSConstants.Spacing.SizeMD;

            var runLabel = new Label(content.runLabel);
            runLabel.AddToClassList(RLDSConstants.Typography.Body1Label);
            runPanel.Add(runLabel);

            var prompt = BuildInlineCommand(
                SdkUpgraderContentManager.UpgradePrompt(SdkUpgraderData.CurrentVersion, SdkUpgraderData.LatestVersion),
                "SdkUpgraderCopyPrompt",
                multiline: true);
            prompt.style.marginTop = RLDSConstants.Spacing.SizeXS;
            runPanel.Add(prompt);

            section.Add(runPanel);

            return section;
        }

        // A single "install the skill" card holding the two install options as rows. Copy + commands
        // come from the remote-hosted content (SdkUpgraderContentManager), with a compiled default.
        private VisualElement BuildInstallPanel(SdkUpgraderContentManager.SdkUpgraderContent content)
        {
            var panel = NewPanel();

            // Option A — the MetaVR CLI (recommended). The remote kill-switch hides this whole path
            // (header, install/upgrade steps, and the trailing "or" divider) when the metavr CLI skill
            // downloader isn't shipped, so the panel falls back to only the GitHub path below without
            // leaving a dangling divider.
            if (!content.hideMetaVrCliSkillOption)
            {
                var optionAHeader = new VisualElement();
                optionAHeader.AddToClassList(RLDSConstants.Flexbox.Row);
                optionAHeader.AddToClassList(RLDSConstants.Flexbox.AlignCenter);

                var optionATitle = new Label(content.cliOptionLabel);
                optionATitle.AddToClassList(RLDSConstants.Typography.Body1Label);
                optionAHeader.Add(optionATitle);

                // Fold the minimum-version requirement into the "?" tooltip rather than giving it its own row.
                var cliTooltip = string.IsNullOrEmpty(content.cliMinVersion)
                    ? content.cliTooltip
                    : $"{content.cliTooltip} Requires metavr CLI {content.cliMinVersion} or newer.";
                optionAHeader.Add(BuildHelpIcon(cliTooltip, content.cliDocUrl, "SdkUpgraderCliHelp"));

                var recommended = new BadgePill("Recommended", BadgePillType.Info, BadgePillSize.Small).Build();
                recommended.style.marginLeft = RLDSConstants.Spacing.SizeXS;
                optionAHeader.Add(recommended);

                panel.Add(optionAHeader);

                // First step depends on the CLI's state: install it, upgrade it (installed but below the
                // minimum version), or — when it's already current — skip straight to the skill command (the
                // option header "Install the skill with metavr CLI" already labels that, so no redundant
                // "Install the skill" sub-label).
                var cliState = SdkUpgraderData.GetMetaVrCliState(content.cliMinVersion);
                if (cliState == SdkUpgraderData.MetaVrCliState.Ready)
                {
                    var skillCommand = BuildInlineCommand(content.cliInstallSkillCommand, "SdkUpgraderCopySkillInstall");
                    skillCommand.style.marginTop = RLDSConstants.Spacing.SizeSM;
                    panel.Add(skillCommand);
                }
                else
                {
                    if (cliState == SdkUpgraderData.MetaVrCliState.Outdated)
                    {
                        panel.Add(BuildStep(1, content.cliUpgradeLabel, content.cliUpgradeCommand, "SdkUpgraderCopyCliUpgrade"));
                    }
                    else
                    {
                        var cliInstallCommand = Application.platform == RuntimePlatform.WindowsEditor
                            ? content.cliInstallCliCommandWindows
                            : content.cliInstallCliCommand;
                        panel.Add(BuildStep(1, content.cliInstallCliLabel, cliInstallCommand, "SdkUpgraderCopyCliInstall"));
                    }

                    panel.Add(BuildStep(2, content.cliInstallSkillLabel, content.cliInstallSkillCommand, "SdkUpgraderCopySkillInstall"));
                }

                panel.Add(BuildOrDivider());
            }

            // Option B — grab it from GitHub and set it up manually. The description and the link share
            // one wrapping line, matching the Welcome "Browse samples" resource-link style.
            var optionBTitle = new Label(content.githubOptionLabel);
            optionBTitle.AddToClassList(RLDSConstants.Typography.Body1Label);
            panel.Add(optionBTitle);

            var optionBRow = new VisualElement();
            optionBRow.AddToClassList(RLDSConstants.Flexbox.Row);
            optionBRow.AddToClassList(RLDSConstants.Flexbox.AlignCenter);
            optionBRow.AddToClassList(RLDSConstants.Flexbox.Wrap);
            optionBRow.style.marginTop = RLDSConstants.Spacing.Size2XS;

            var optionBDesc = new Label(content.githubOptionDescription);
            optionBDesc.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            optionBDesc.style.whiteSpace = WhiteSpace.Normal;
            optionBDesc.style.marginRight = RLDSConstants.Spacing.SizeSM;
            optionBRow.Add(optionBDesc);

            optionBRow.Add(BuildHyperlink("Open on GitHub", content.githubUrl, "SdkUpgraderGitHub"));
            panel.Add(optionBRow);

            return panel;
        }

        // A "———— or ————" separator (two lines around a centered "or"), matching the Hand Readiness tool.
        private static VisualElement BuildOrDivider()
        {
            var row = new VisualElement();
            row.AddToClassList(RLDSConstants.Divider.Or);

            var leftLine = new VisualElement();
            leftLine.AddToClassList(RLDSConstants.Divider.OrLine);
            row.Add(leftLine);

            var orLabel = new Label("or");
            orLabel.AddToClassList(RLDSConstants.Typography.Meta);
            orLabel.AddToClassList(RLDSConstants.Divider.OrLabel);
            row.Add(orLabel);

            var rightLine = new VisualElement();
            rightLine.AddToClassList(RLDSConstants.Divider.OrLine);
            row.Add(rightLine);

            return row;
        }

        // An inline blue hyperlink (blue label + blue-tinted external-link icon) that opens a URL, with
        // telemetry. Mirrors the Welcome resource-link look ("Browse samples").
        private VisualElement BuildHyperlink(string text, string url, string telemetryId)
        {
            var row = new VisualElement();
            row.AddToClassList(RLDSConstants.Flexbox.Row);
            row.AddToClassList(RLDSConstants.Flexbox.AlignCenter);
            row.AddToClassList(RLDSConstants.Utilities.CursorLink);

            var label = new Label(text);
            label.AddToClassList(RLDSConstants.Utilities.LinkText);
            label.pickingMode = PickingMode.Ignore;
            row.Add(label);

            // Icon as a background-image on a VisualElement so the RLDS link-icon class blue-tints it
            // (-unity-background-image-tint-color); an Image.image would not pick up the USS tint.
            var icon = new VisualElement();
            icon.AddToClassList(RLDSConstants.FeatureCard.LinkIcon);
            icon.style.marginLeft = RLDSConstants.Spacing.Size3XS;
            icon.pickingMode = PickingMode.Ignore;
            ApplyWhenLoaded(ExternalLinkIcon, icon, tex => icon.style.backgroundImage = tex as Texture2D);
            row.Add(icon);

            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (!string.IsNullOrEmpty(url))
                {
                    Application.OpenURL(url);
                }
                RLDSTelemetry.SendInteraction(row, "Hyperlink", telemetryId, text);
            });
            return row;
        }

        // A help affordance (info icon): a tooltip explains what the MetaVR CLI is; clicking opens the docs.
        private VisualElement BuildHelpIcon(string tooltip, string url, string telemetryId)
        {
            var help = new UnityEngine.UIElements.Image
            {
                scaleMode = UnityEngine.ScaleMode.ScaleToFit
            };
            help.style.width = RLDSConstants.IconSize.SizeSM;
            help.style.height = RLDSConstants.IconSize.SizeSM;
            help.style.marginLeft = RLDSConstants.Spacing.SizeXS;
            help.AddToClassList(RLDSConstants.Utilities.CursorLink);
            help.tooltip = tooltip;
            ApplyWhenLoaded(HelpIcon, help, tex => help.image = tex as Texture);
            help.RegisterCallback<ClickEvent>(_ =>
            {
                if (!string.IsNullOrEmpty(url))
                {
                    Application.OpenURL(url);
                }
                RLDSTelemetry.SendInteraction(help, "HelpIcon", telemetryId, "MetaVR CLI docs");
            });
            return help;
        }

        private VisualElement BuildStep(int number, string label, string command, string telemetryId)
        {
            var step = new VisualElement();
            step.style.marginTop = RLDSConstants.Spacing.SizeSM;

            var header = new Label($"{number}. {label}");
            header.AddToClassList(RLDSConstants.Typography.Body1Label);
            step.Add(header);

            step.Add(BuildInlineCommand(command, telemetryId));
            return step;
        }

        // Command text + a right-aligned copy button on ONE line (a compact alternative to CodeBlock,
        // which put its copy button on a separate row). Copying swaps the icon to a check for ~1.5s and
        // logs a distinct telemetry event.
        private VisualElement BuildInlineCommand(string command, string telemetryId, bool multiline = false)
        {
            var row = new VisualElement();
            row.AddToClassList(RLDSConstants.Flexbox.Row);
            // Top-align the copy button for a multi-line block (the prompt); center it for one-liners.
            row.AddToClassList(multiline ? RLDSConstants.Flexbox.AlignStart : RLDSConstants.Flexbox.AlignCenter);
            row.AddToClassList(RLDSConstants.Surface.Tertiary);
            row.style.marginTop = RLDSConstants.Spacing.Size2XS;
            row.style.paddingLeft = RLDSConstants.Spacing.SizeSM;
            row.style.paddingRight = RLDSConstants.Spacing.SizeSM;
            row.style.paddingTop = RLDSConstants.Spacing.Size2XS;
            row.style.paddingBottom = RLDSConstants.Spacing.Size2XS;
            row.style.borderTopLeftRadius = RLDSConstants.Radius.SizeSM;
            row.style.borderTopRightRadius = RLDSConstants.Radius.SizeSM;
            row.style.borderBottomLeftRadius = RLDSConstants.Radius.SizeSM;
            row.style.borderBottomRightRadius = RLDSConstants.Radius.SizeSM;

            var code = new Label(command);
            code.AddToClassList(RLDSConstants.Typography.BodySmallCode);
            code.style.flexGrow = 1;
            code.style.whiteSpace = WhiteSpace.Normal;
            row.Add(code);

            var copyButton = new VisualElement();
            copyButton.AddToClassList(RLDSConstants.Flexbox.Row);
            copyButton.AddToClassList(RLDSConstants.Flexbox.AlignCenter);
            copyButton.AddToClassList(RLDSConstants.Utilities.CursorLink);
            copyButton.style.flexShrink = 0;
            copyButton.style.marginLeft = RLDSConstants.Spacing.SizeSM;
            copyButton.tooltip = "Copy";

            var copyIcon = new UnityEngine.UIElements.Image
            {
                scaleMode = UnityEngine.ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            copyIcon.style.width = RLDSConstants.IconSize.SizeSM;
            copyIcon.style.height = RLDSConstants.IconSize.SizeSM;
            ApplyWhenLoaded(CopyIcon, copyIcon, tex => copyIcon.image = tex as Texture);
            copyButton.Add(copyIcon);

            var checkIcon = new UnityEngine.UIElements.Image
            {
                scaleMode = UnityEngine.ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore
            };
            checkIcon.style.width = RLDSConstants.IconSize.SizeSM;
            checkIcon.style.height = RLDSConstants.IconSize.SizeSM;
            checkIcon.style.display = DisplayStyle.None;
            ApplyWhenLoaded(UIStyles.Contents.CheckMaskIcon, checkIcon, tex => checkIcon.image = tex as Texture);
            copyButton.Add(checkIcon);

            // Tracks the outstanding icon-reset scheduled by the latest click so rapid successive clicks
            // can cancel the prior reset before it fires and flips the icon back early.
            IVisualElementScheduledItem resetIconScheduled = null;
            copyButton.RegisterCallback<ClickEvent>(_ =>
            {
                EditorGUIUtility.systemCopyBuffer = command;
                RLDSTelemetry.SendInteraction(copyButton, "CopyCommand", telemetryId, command);
                copyIcon.style.display = DisplayStyle.None;
                checkIcon.style.display = DisplayStyle.Flex;
                resetIconScheduled?.Pause();
                resetIconScheduled = copyButton.schedule.Execute(() =>
                {
                    // The window may have been closed during the 1.5s delay; skip if the element is
                    // no longer attached to a panel.
                    if (copyButton.panel == null)
                    {
                        return;
                    }

                    copyIcon.style.display = DisplayStyle.Flex;
                    checkIcon.style.display = DisplayStyle.None;
                }).StartingIn(1500);
            });

            row.Add(copyButton);
            return row;
        }

        #endregion

        #region Release notes

        private VisualElement BuildReleaseNotesSection()
        {
            var section = new VisualElement();
            AddSectionHeading(section, $"What changed since v{SdkUpgraderData.CurrentVersion}");

            var releases = SdkUpgraderData.Releases;
            if (releases == null || releases.Length == 0)
            {
                var message = new Label(SdkUpgraderData.IsLoading
                    ? "Checking for updates…"
                    : "No newer versions found.");
                message.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
                section.Add(message);
                return section;
            }

            // All versions render as the same plain grey card — no "Latest"/"Breaking changes" badges
            // and no highlighted (amber) variant.
            foreach (var release in releases)
            {
                var releaseUrl = SdkUpgraderData.FullReleaseNotesUrl(release);

                // One neutral pill per contributing SDK so a version's changes are attributed to their SDK.
                var sdkBadges = new List<(string, BadgePillType)>();
                foreach (var sdk in release.Sdks ?? Array.Empty<string>())
                {
                    sdkBadges.Add((sdk, BadgePillType.Neutral));
                }

                var card = new ReleaseNoteCard(
                    release.VersionLabel,
                    release.DateLabel,
                    release.Highlights,
                    sdkBadges,
                    linkIcon: ExternalLinkIcon,
                    highlighted: false,
                    id: $"{release.Slug}-v{release.Version}",
                    onLinkClicked: () => { if (!string.IsNullOrEmpty(releaseUrl)) Application.OpenURL(releaseUrl); });

                var cardElement = card.Build();
                cardElement.style.marginBottom = RLDSConstants.Spacing.SizeXS;
                section.Add(cardElement);
            }

            return section;
        }

        #endregion

        #region Helpers & actions

        private static VisualElement NewPage()
        {
            var page = new VisualElement();
            page.style.paddingTop = RLDSConstants.Spacing.SizeXL;
            page.style.paddingBottom = RLDSConstants.Spacing.SizeXL;
            page.style.paddingLeft = RLDSConstants.Spacing.Size3XL;
            page.style.paddingRight = RLDSConstants.Spacing.Size3XL;
            return page;
        }

        // Assigns a texture to a UI element once it's available. Applies synchronously when the texture
        // is already loaded (so it lands during the initial Build, before the element is attached);
        // otherwise defers, and skips the assignment if the element was detached before the texture
        // finished loading (window closed or UI rebuilt) so stale rebuild closures can't mutate orphaned
        // elements or keep them alive.
        private static void ApplyWhenLoaded(TextureContent content, VisualElement target, Action<Texture> apply)
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

        // A rounded card surface (the same subtle fill as the release-note and Welcome resource cards),
        // used to group section content so the page reads as distinct panels rather than one flat column.
        private static VisualElement NewPanel()
        {
            var panel = new VisualElement();
            panel.AddToClassList(RLDSConstants.Surface.Card);
            panel.style.paddingTop = RLDSConstants.Spacing.SizeLG;
            panel.style.paddingBottom = RLDSConstants.Spacing.SizeLG;
            panel.style.paddingLeft = RLDSConstants.Spacing.SizeLG;
            panel.style.paddingRight = RLDSConstants.Spacing.SizeLG;
            panel.style.borderTopLeftRadius = RLDSConstants.Radius.SizeMD;
            panel.style.borderTopRightRadius = RLDSConstants.Radius.SizeMD;
            panel.style.borderBottomLeftRadius = RLDSConstants.Radius.SizeMD;
            panel.style.borderBottomRightRadius = RLDSConstants.Radius.SizeMD;
            return panel;
        }

        // Matches the Welcome screen's section headers (e.g. "Resources"): a larger, dimmed heading
        // that reads as a section label distinct from body text.
        private static void AddSectionHeading(VisualElement parent, string text)
        {
            var heading = new Label(text);
            heading.AddToClassList(RLDSConstants.Typography.Heading2);
            heading.style.marginBottom = RLDSConstants.Spacing.SizeMD;
            heading.style.opacity = 0.7f;
            parent.Add(heading);
        }

        #endregion
    }
}
