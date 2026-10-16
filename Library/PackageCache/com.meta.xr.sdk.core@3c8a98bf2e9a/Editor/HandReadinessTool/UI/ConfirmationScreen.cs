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
using UnityEngine;
using UnityEngine.UIElements;
using Meta.XR.Editor.UserInterface.RLDS;
using ActionLinkDescription = Meta.XR.Editor.UserInterface.ActionLinkDescription;
using RLDSButton = Meta.XR.Editor.UserInterface.RLDS.Button;

namespace Meta.HandReadinessTool.Editor.UI
{
    /// <summary>
    /// Final-step confirmation screen. Reached from Results once every
    /// recommendation is resolved and the user clicks "Confirm readiness".
    /// Replaces the report with a hero confirmation block (green check, title,
    /// completion badge), a "What happens next" bullet list, and a Close /
    /// Explore Building Blocks footer.
    /// </summary>
    public static class ConfirmationScreen
    {
        /// <summary>
        /// Fixed hero-card content width. Keeps the card centered inside the outer
        /// 64px-gutter content area rather than stretching to fill it.
        /// </summary>
        private const int HeroContentWidth = 896;
        private const int HeroHeight = 240;
        internal const string FovSimulationSettingsButtonName = "fov-simulation-settings-button";
        internal const string FovSimulationGuidanceName = "fov-simulation-guidance";
        internal const string FovSimulationGuidanceRowName = "fov-simulation-guidance-row";
        internal const string FovSimulationDescriptionName = "fov-simulation-description";

        /// <summary>Creates the confirmation screen UI.</summary>
        /// <param name="totalResolvedCount">Total number of recommendations resolved (rendered as "All {N} recommendations have been resolved").</param>
        /// <param name="onClose">Callback invoked when the Close button is clicked.</param>
        /// <param name="onExploreBuildingBlocks">Callback invoked when the Explore Building Blocks button is clicked.</param>
        /// <param name="hasOvrManager">Whether the active scene contains an OVRManager.</param>
        /// <param name="onOpenFovSimulationSettings">Callback that selects and highlights the OVRManager FoV setting.</param>
        /// <returns>A <see cref="VisualElement"/> containing the complete confirmation screen.</returns>
        public static VisualElement Create(
            int totalResolvedCount,
            Action onClose,
            Action onExploreBuildingBlocks,
            bool hasOvrManager,
            Action onOpenFovSimulationSettings)
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.justifyContent = Justify.SpaceBetween;

            // 64px gutter on the Container that wraps the hero card + What happens
            // next section. WizardSideGutter (240px) is reserved for narrow
            // single-column wizard screens (Welcome, Scanning).
            var content = new VisualElement();
            content.style.flexGrow = 1;
            content.style.paddingLeft = RLDSConstants.Spacing.Size5XL;
            content.style.paddingRight = RLDSConstants.Spacing.Size5XL;
            content.style.paddingTop = RLDSConstants.Spacing.Size4XL;

            content.Add(BuildHeroPanel(totalResolvedCount));
            content.Add(BuildWhatHappensNext());

            content.Add(BuildFovSimulationGuidance(hasOvrManager, onOpenFovSimulationSettings));

            container.Add(content);

            var closeButton = HandReadinessResources.CreateBackButton("Close", onClose);
            var exploreButton = HandReadinessResources.CreateNextButton(
                "Explore Building Blocks", onExploreBuildingBlocks);
            container.Add(HandReadinessResources.CreateFooter(closeButton, exploreButton));

            return container;
        }

        private static VisualElement BuildFovSimulationGuidance(
            bool hasOvrManager,
            Action onOpenFovSimulationSettings)
        {
            var card = new VisualElement { name = FovSimulationGuidanceName };
            card.AddToClassList(HandReadinessStyles.Card.Root);
            card.style.alignSelf = Align.Center;
            card.style.width = HeroContentWidth;
            card.style.marginTop = RLDSConstants.Spacing.Size3XL;
            card.style.paddingLeft = RLDSConstants.Spacing.SizeXL;
            card.style.paddingRight = RLDSConstants.Spacing.SizeXL;
            card.style.paddingTop = RLDSConstants.Spacing.SizeLG;
            card.style.paddingBottom = RLDSConstants.Spacing.SizeLG;

            var row = new VisualElement { name = FovSimulationGuidanceRowName };
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexEnd;

            var textColumn = new VisualElement();
            textColumn.style.flexGrow = 1;
            textColumn.style.flexShrink = 1;

            var title = new Label(HandReadinessScreenContentProvider.Get(
                "confirmation.fov.title",
                "Validate the device field of view"));
            title.AddToClassList(RLDSConstants.Typography.Body1Label);
            title.style.marginBottom = RLDSConstants.Spacing.SizeXS;
            textColumn.Add(title);

            var description = new Label(hasOvrManager
                ? HandReadinessScreenContentProvider.Get(
                    "confirmation.fov.body",
                    "Run your app on Quest once with device field of view simulation enabled. " +
                    "Configure it in OVRManager > Quest > Experimental > Device Simulation.")
                : HandReadinessScreenContentProvider.Get(
                    "confirmation.fov.bodyNoManager",
                    "This scene has no OVRManager. Enable device field of view simulation in the " +
                    "Oculus Runtime Settings asset, or control the mask with Immersive Debugger at runtime."));
            description.name = FovSimulationDescriptionName;
            description.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            description.style.whiteSpace = WhiteSpace.Normal;
            textColumn.Add(description);
            row.Add(textColumn);

            if (hasOvrManager)
            {
                var button = (UnityEngine.UIElements.Button)new RLDSButton(
                    new ActionLinkDescription
                    {
                        Content = new GUIContent(HandReadinessScreenContentProvider.Get(
                            "confirmation.fov.button", "Open FoV setting")),
                        Action = onOpenFovSimulationSettings,
                        // Stable telemetry id: the label above is remotely tunable, so the
                        // click event must not be identified by label text.
                        Id = "hrt-confirmation-open-fov-setting",
                    },
                    RLDSConstants.ButtonVariant.Secondary,
                    RLDSConstants.ButtonSize.Small).Build();
                button.name = FovSimulationSettingsButtonName;
                button.style.alignSelf = Align.FlexEnd;
                button.style.flexShrink = 0;
                button.style.marginLeft = RLDSConstants.Spacing.SizeXL;
                row.Add(button);
            }

            card.Add(row);

            return card;
        }

        private static VisualElement BuildHeroPanel(int totalResolvedCount)
        {
            // Hero card sits centered inside the outer content area at a fixed
            // width. hrt-card provides the surface-secondary-background (lighter
            // than the surrounding window) + RLDS-sm border-radius. Cover.Root
            // is not suitable here — it forces a fixed 261px height and bottom-
            // aligned content, which is wrong for a centered hero.
            var panel = new VisualElement();
            panel.AddToClassList(HandReadinessStyles.Card.Root);
            panel.style.alignItems = Align.Center;
            panel.style.justifyContent = Justify.Center;
            panel.style.alignSelf = Align.Center;
            panel.style.width = HeroContentWidth;
            panel.style.maxWidth = HeroContentWidth;
            // minHeight, not a fixed height: on the "ready" state the check + two-line
            // heading + subtitle + badge exceed HeroHeight, and a fixed height makes the
            // column flex-shrink its children — collapsing the badge pill so its label
            // overflows below the pill border. Growing to fit avoids that.
            panel.style.minHeight = HeroHeight;
            panel.style.paddingTop = RLDSConstants.Spacing.Size3XL;
            panel.style.paddingBottom = RLDSConstants.Spacing.Size3XL;
            panel.style.paddingLeft = RLDSConstants.Spacing.Size3XL;
            panel.style.paddingRight = RLDSConstants.Spacing.Size3XL;
            panel.style.marginBottom = RLDSConstants.Spacing.Size4XL;

            var check = HandReadinessResources.CreateTintableIcon("icon_check_circle", 48);
            check.AddToClassList(HandReadinessStyles.Icon.ThemedPositive);
            check.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            panel.Add(check);

            var title = new Label(HandReadinessScreenContentProvider.Get(
                "confirmation.title",
                "You've completed the device readiness checks"));
            title.AddToClassList(RLDSConstants.Typography.Heading1);
            title.AddToClassList(HandReadinessStyles.Text.Primary);
            title.style.unityTextAlign = TextAnchor.MiddleCenter;
            title.style.whiteSpace = WhiteSpace.Normal;
            title.style.marginBottom = RLDSConstants.Spacing.SizeXS;
            panel.Add(title);

            var subtitle = new Label($"All {totalResolvedCount} recommendations have been resolved");
            subtitle.AddToClassList(RLDSConstants.Typography.Body1Text);
            subtitle.AddToClassList(HandReadinessStyles.Text.Primary);
            subtitle.style.unityTextAlign = TextAnchor.MiddleCenter;
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            subtitle.style.opacity = 0.6f;
            subtitle.style.marginBottom = RLDSConstants.Spacing.SizeXS;
            panel.Add(subtitle);

            panel.Add(BuildOptimizedBadge());

            return panel;
        }

        /// <summary>
        /// Completion badge-pill: neutral badge shell with a positive check glyph.
        /// </summary>
        private static VisualElement BuildOptimizedBadge()
        {
            var badge = new VisualElement();
            badge.AddToClassList(RLDSConstants.BadgePill.Base);
            badge.AddToClassList(RLDSConstants.BadgePill.Neutral);
            // Never let the pill compress vertically to fit a tight parent — that clips its label.
            badge.style.flexShrink = 0;

            var icon = HandReadinessResources.CreateTintableIcon(
                "icon_checkbox_check", RLDSConstants.IconSize.SizeXS);
            icon.AddToClassList(RLDSConstants.BadgePill.Icon);
            icon.AddToClassList(HandReadinessStyles.Icon.ThemedPositive);
            badge.Add(icon);

            var label = new Label(HandReadinessScreenContentProvider.Get(
                "confirmation.badge",
                "Readiness checks complete"));
            label.AddToClassList(RLDSConstants.BadgePill.Label);
            badge.Add(label);

            return badge;
        }

        private static VisualElement BuildWhatHappensNext()
        {
            var section = new VisualElement();
            section.style.alignSelf = Align.Center;
            section.style.width = HeroContentWidth;
            section.style.paddingLeft = RLDSConstants.Spacing.Size3XL;
            section.style.paddingRight = RLDSConstants.Spacing.Size3XL;

            var heading = new Label("What happens next");
            heading.AddToClassList(RLDSConstants.Typography.Heading2);
            heading.style.marginBottom = RLDSConstants.Spacing.SizeXL;
            section.Add(heading);

            AddBullet(
                section,
                "icon_hand_tracking",
                HandReadinessScreenContentProvider.Get(
                    "confirmation.step1",
                    "You've addressed the current readiness recommendations for the next generation of Meta XR devices."),
                addBottomMargin: true);
            AddBullet(
                section,
                "icon_touch_3_left",
                HandReadinessScreenContentProvider.Get(
                    "confirmation.step2",
                    "Your project remains compatible with other input methods like controllers."),
                addBottomMargin: true);
            AddBullet(
                section,
                "icon_list_checked",
                HandReadinessScreenContentProvider.Get(
                    "confirmation.recheck",
                    "Requirements are still evolving — re-run this check as you get closer to the device release for the latest recommendations."),
                addBottomMargin: true);
            AddBullet(
                section,
                "icon_default_app",
                HandReadinessScreenContentProvider.Get(
                    "confirmation.step3",
                    "Explore Building Blocks related to hand tracking."),
                addBottomMargin: false);

            return section;
        }

        private static void AddBullet(
            VisualElement parent,
            string iconResourceName,
            string text,
            bool addBottomMargin)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            if (addBottomMargin)
            {
                row.style.marginBottom = RLDSConstants.Spacing.SizeMD;
            }

            // Bullet icons use the brand/icon-primary tint (white in dark mode).
            // Themed (not ThemedSecondary) maps to that token.
            var icon = HandReadinessResources.CreateTintableIcon(
                iconResourceName, RLDSConstants.IconSize.SizeLG);
            icon.AddToClassList(HandReadinessStyles.Icon.Themed);
            icon.style.marginRight = RLDSConstants.Spacing.SizeSM;
            row.Add(icon);

            var label = new Label(text);
            label.AddToClassList(RLDSConstants.Typography.Body1Text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexGrow = 1;
            row.Add(label);

            parent.Add(row);
        }
    }
}
