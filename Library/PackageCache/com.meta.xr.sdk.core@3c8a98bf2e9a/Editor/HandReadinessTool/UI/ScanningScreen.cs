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
using UnityEngine;
using UnityEngine.UIElements;
using Meta.XR.Editor.UserInterface.RLDS;

namespace Meta.HandReadinessTool.Editor.UI
{
    /// <summary>Represents the execution status of a scan phase.</summary>
    public enum ScanPhaseStatus
    {
        Pending,
        InProgress,
        Completed
    }

    /// <summary>Holds the title, description, and status for a single scan phase.</summary>
    public class ScanPhaseInfo
    {
        /// <summary>
        /// Gets or sets the display title of this scan phase.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Gets or sets the status description for this scan phase.
        /// </summary>
        public string Description { get; set; }
        /// <summary>
        /// Gets or sets the current execution status of this scan phase.
        /// </summary>
        public ScanPhaseStatus Status { get; set; }
    }

    /// <summary>
    /// Scanning screen. Single column: title + subtitle, phase rows with icons,
    /// divider, progress bar, status text.
    /// </summary>
    public static class ScanningScreen
    {
        /// <summary>Creates the scanning screen UI showing phase progress, a progress bar, and optional error state.</summary>
        /// <param name="phases">The list of scan phases to display with their statuses.</param>
        /// <param name="progress">The overall scan progress as a value between 0 and 1.</param>
        /// <param name="currentStatusText">The status text displayed below the progress bar.</param>
        /// <param name="isComplete">Whether the scan has completed successfully.</param>
        /// <param name="onNext">Callback invoked when the Next button is clicked.</param>
        /// <param name="onBack">Callback invoked when the Back button is clicked.</param>
        /// <param name="errorMessage">An optional error message to display instead of the progress bar.</param>
        /// <returns>A <see cref="VisualElement"/> containing the complete scanning screen.</returns>
        public static VisualElement Create(
            List<ScanPhaseInfo> phases,
            float progress,
            string currentStatusText,
            bool isComplete = false,
            Action onNext = null,
            Action onBack = null,
            string errorMessage = null,
            string aiActivityText = null,
            string aiProviderName = null)
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.justifyContent = Justify.SpaceBetween;

            // ---- Content ----
            var content = new VisualElement();
            content.style.flexGrow = 1;
            content.style.paddingLeft = HandReadinessResources.WizardSideGutter;
            content.style.paddingRight = HandReadinessResources.WizardSideGutter;
            content.style.paddingTop = RLDSConstants.Spacing.Size4XL;
            content.style.paddingBottom = RLDSConstants.Spacing.Size4XL;

            // Header rhythm matches the Figma scanning frame: 16px above the
            // title, 2px to the subtitle, 32px below the header block.
            var title = new Label(HandReadinessScreenContentProvider.Get(
                "scanning.title", "Analyzing your project files"));
            title.AddToClassList(RLDSConstants.Typography.Heading2);
            title.style.marginTop = RLDSConstants.Spacing.SizeMD;
            title.style.marginBottom = RLDSConstants.Spacing.Size4XS;
            title.style.whiteSpace = WhiteSpace.Normal;
            content.Add(title);

            var subtitleText = string.IsNullOrEmpty(aiProviderName)
                ? "Read-only analysis. Your project won't be modified."
                : $"Read-only analysis. Your project won't be modified. {aiProviderName} will analyze your project.";
            var subtitle = new Label(subtitleText);
            subtitle.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            content.Add(subtitle);

            // On the (longer) AI path, reassure the user they can background the window and
            // a notification will fire when the scan finishes.
            if (!string.IsNullOrEmpty(aiProviderName))
            {
                subtitle.style.marginBottom = RLDSConstants.Spacing.Size4XS;
                var backgroundHint = new Label(HandReadinessScreenContentProvider.Get(
                    "scanning.backgroundHint",
                    "This will likely take a few minutes — you can background this window or park " +
                    "the tab in your Editor, and we'll let you know when it's finished."));
                backgroundHint.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
                backgroundHint.style.whiteSpace = WhiteSpace.Normal;
                backgroundHint.style.marginTop = RLDSConstants.Spacing.SizeMD;
                backgroundHint.style.marginBottom = RLDSConstants.Spacing.Size2XL;
                content.Add(backgroundHint);
            }
            else
            {
                subtitle.style.marginBottom = RLDSConstants.Spacing.Size2XL;
            }

            // ---- Phase rows ----
            // Figma status list: 8px above the first row, 16px below the last,
            // 4px between rows.
            if (phases != null)
            {
                var statusList = new VisualElement();
                statusList.style.paddingTop = RLDSConstants.Spacing.SizeXS;
                statusList.style.paddingBottom = RLDSConstants.Spacing.SizeMD;
                for (int i = 0; i < phases.Count; i++)
                {
                    var row = CreatePhaseRow(phases[i]);
                    if (i < phases.Count - 1)
                    {
                        row.style.marginBottom = RLDSConstants.Spacing.Size3XS;
                    }
                    statusList.Add(row);
                }
                content.Add(statusList);
            }

            // ---- Error or progress ----
            if (!string.IsNullOrEmpty(errorMessage))
            {
                content.Add(CreateDivider());
                content.Add(CreateErrorBanner(errorMessage));
            }
            else if (isComplete)
            {
                content.Add(CreateDivider());
                content.Add(CreateCompleteRow());
            }
            else
            {
                content.Add(CreateDivider());
                content.Add(CreateProgressBar(progress, currentStatusText, aiActivityText));
            }

            container.Add(content);

            // ---- Footer ----
            var backButton = HandReadinessResources.CreateBackButton("Back", onBack);
            var nextButton = HandReadinessResources.CreateNextButton("Next", isComplete ? onNext : null);
            HandReadinessResources.SetButtonEnabled(nextButton, isComplete);

            container.Add(HandReadinessResources.CreateFooter(backButton, nextButton));

            return container;
        }

        /// <summary>Creates a scanning screen with default phases and zero progress.</summary>
        /// <param name="statusText">The status text displayed below the progress bar.</param>
        /// <returns>A <see cref="VisualElement"/> containing the scanning screen with default phases.</returns>
        public static VisualElement Create(string statusText)
        {
            var defaultPhases = new List<ScanPhaseInfo>
            {
                new ScanPhaseInfo { Title = "Project configuration", Description = "Checking build settings", Status = ScanPhaseStatus.InProgress },
                new ScanPhaseInfo { Title = "Asset analysis", Description = "Scanning assets", Status = ScanPhaseStatus.Pending },
                new ScanPhaseInfo { Title = "Shader compatibility", Description = "Verifying shaders", Status = ScanPhaseStatus.Pending },
                new ScanPhaseInfo { Title = "Performance check", Description = "Analyzing performance", Status = ScanPhaseStatus.Pending }
            };
            return Create(defaultPhases, 0f, statusText);
        }

        private static VisualElement CreatePhaseRow(ScanPhaseInfo phase)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            // Figma scanning-status row: 12px vertical padding. The 4px gap
            // between rows is applied by the caller so the last row stays flush.
            row.style.paddingTop = RLDSConstants.Spacing.SizeSM;
            row.style.paddingBottom = RLDSConstants.Spacing.SizeSM;

            var icon = CreatePhaseIcon(phase.Status);
            icon.style.marginRight = RLDSConstants.Spacing.SizeMD;
            icon.style.flexShrink = 0;
            row.Add(icon);

            var textContainer = new VisualElement();
            textContainer.style.flexGrow = 1;

            var titleLabel = new Label(phase.Title);
            titleLabel.AddToClassList(RLDSConstants.Typography.Body1Label);
            if (phase.Status == ScanPhaseStatus.Pending)
            {
                titleLabel.style.opacity = 0.4f;
            }
            textContainer.Add(titleLabel);

            if (!string.IsNullOrEmpty(phase.Description))
            {
                titleLabel.style.marginBottom = RLDSConstants.Spacing.Size4XS;
                var descLabel = new Label(phase.Description);
                descLabel.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
                if (phase.Status == ScanPhaseStatus.Pending)
                {
                    descLabel.style.opacity = 0.4f;
                }
                textContainer.Add(descLabel);
            }

            row.Add(textContainer);
            return row;
        }

        private static VisualElement CreatePhaseIcon(ScanPhaseStatus status)
        {
            var icon = new VisualElement();
            icon.AddToClassList(HandReadinessStyles.PhaseIcon.Root);
            switch (status)
            {
                case ScanPhaseStatus.Completed:
                    icon.AddToClassList(HandReadinessStyles.PhaseIcon.Complete);
                    var checkImg = new Label("\u2713");
                    checkImg.AddToClassList(HandReadinessStyles.PhaseIcon.CheckGlyph);
                    icon.Add(checkImg);
                    break;
                case ScanPhaseStatus.InProgress:
                    icon.AddToClassList(HandReadinessStyles.PhaseIcon.Active);
                    break;
                default:
                    icon.AddToClassList(HandReadinessStyles.PhaseIcon.Pending);
                    break;
            }
            return icon;
        }

        private static VisualElement CreateDivider()
        {
            var divider = new VisualElement();
            divider.AddToClassList(RLDSConstants.Divider.Base);
            // Figma divider: 4px padding above and below the rule.
            divider.style.marginTop = RLDSConstants.Spacing.Size3XS;
            divider.style.marginBottom = RLDSConstants.Spacing.Size3XS;
            return divider;
        }

        /// <summary>Element name of the progress-bar fill, for in-place updates during the AI scan.</summary>
        public const string ProgressFillName = "hrt-scan-progress-fill";
        /// <summary>Element name of the primary status label, for in-place updates during the AI scan.</summary>
        public const string StatusLabelName = "hrt-scan-status-label";
        /// <summary>Element name of the secondary activity label, for in-place updates during the AI scan.</summary>
        public const string ActivityLabelName = "hrt-scan-activity-label";

        private static VisualElement CreateProgressBar(float progress, string statusText, string aiActivityText = null)
        {
            var container = new VisualElement();
            // Figma footer: 24px above the progress bar, 16px to the status text.
            container.style.marginTop = RLDSConstants.Spacing.SizeXL;

            var barBg = new VisualElement();
            barBg.AddToClassList(HandReadinessStyles.Progress.Track);

            var fill = new VisualElement();
            fill.name = ProgressFillName;
            fill.AddToClassList(HandReadinessStyles.Progress.Fill);
            fill.style.width = Length.Percent(Mathf.Max(progress * 100f, 3f));
            barBg.Add(fill);
            container.Add(barBg);

            var label = new Label(statusText ?? "Scanning packages....");
            label.name = StatusLabelName;
            label.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            label.style.marginTop = RLDSConstants.Spacing.SizeMD;
            container.Add(label);

            if (!string.IsNullOrEmpty(aiActivityText))
            {
                var activityLabel = new Label(aiActivityText);
                activityLabel.name = ActivityLabelName;
                activityLabel.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
                activityLabel.style.marginTop = RLDSConstants.Spacing.Size3XS;
                activityLabel.style.opacity = 0.6f;
                container.Add(activityLabel);
            }

            return container;
        }

        private static VisualElement CreateCompleteRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexStart;
            // Sits in the Figma footer slot: 24px below the divider.
            row.style.marginTop = RLDSConstants.Spacing.SizeXL;

            var icon = new VisualElement();
            icon.AddToClassList(HandReadinessStyles.PhaseIcon.Root);
            icon.AddToClassList(HandReadinessStyles.PhaseIcon.Complete);
            icon.style.marginRight = RLDSConstants.Spacing.SizeSM;

            var checkImg = new Label("\u2713");
            checkImg.AddToClassList(HandReadinessStyles.PhaseIcon.CheckGlyph);
            icon.Add(checkImg);
            row.Add(icon);

            var textCol = new VisualElement();
            var label = new Label(HandReadinessScreenContentProvider.Get(
                "scanning.complete.title", "Analysis complete"));
            label.AddToClassList(RLDSConstants.Typography.Body1Label);
            textCol.Add(label);

            var hint = new Label(HandReadinessScreenContentProvider.Get(
                "scanning.complete.hint", "Click \"Next\" to view recommendations"));
            hint.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            textCol.Add(hint);

            row.Add(textCol);
            return row;
        }

        private static VisualElement CreateErrorBanner(string errorMessage)
        {
            var banner = new VisualElement();
            banner.AddToClassList(HandReadinessStyles.ErrorBanner.Root);
            banner.style.marginTop = RLDSConstants.Spacing.SizeXL;
            // Never let a long provider error push the banner past the footer.
            banner.style.flexShrink = 0;

            var title = new Label(HandReadinessScreenContentProvider.Get(
                "scanning.error.title", "AI analysis failed"));
            title.AddToClassList(RLDSConstants.Typography.Body1Label);
            title.AddToClassList(HandReadinessStyles.ErrorBanner.Title);
            title.style.marginBottom = RLDSConstants.Spacing.Size3XS;
            banner.Add(title);

            // Provider errors can be many lines (e.g. a CLI stack dump); cap the height and let
            // them scroll instead of overflowing the screen onto the Back/Next buttons.
            var descScroll = new ScrollView(ScrollViewMode.Vertical);
            descScroll.verticalScrollerVisibility = ScrollerVisibility.Auto;
            descScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            descScroll.style.maxHeight = 120;

            var desc = new Label(errorMessage);
            desc.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            desc.style.whiteSpace = WhiteSpace.Normal;
            descScroll.Add(desc);
            banner.Add(descScroll);

            var hint = new Label(HandReadinessScreenContentProvider.Get(
                "scanning.error.hint",
                "Proceeding with automated checks only. Click \"Next\" to view results."));
            hint.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.marginTop = RLDSConstants.Spacing.SizeXS;
            banner.Add(hint);

            return banner;
        }
    }
}
