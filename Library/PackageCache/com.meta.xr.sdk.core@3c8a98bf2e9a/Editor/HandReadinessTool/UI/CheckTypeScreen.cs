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
using UnityEngine.UIElements;
using Meta.XR.Editor.UserInterface.RLDS;
using TextureContent = Meta.XR.Editor.UserInterface.TextureContent;

namespace Meta.HandReadinessTool.Editor.UI
{
    /// <summary>
    /// Check-type selection screen ("How should we check your project?"). Two
    /// mutually exclusive radio cards:
    ///
    /// - Standard check (regex-only) — runs the heuristic config + code-pattern scan.
    /// - AI-powered check — adds AI analysis, requires an AI service provider.
    ///
    /// Next is disabled until the user picks one. The choice flows out via
    /// <paramref name="useAI"/> on the `onNext` callback so the parent window
    /// can route AI users to the Project Description screen and Standard users
    /// straight to Scanning.
    /// </summary>
    public static class CheckTypeScreen
    {
        // Fixed width of the two-card column so the cards align consistently
        // with the title/subtitle lockup above and the footer below.
        private const int CardColumnWidth = 497;

        // External docs surfaced from the "How it works" accordion's "Read more" link.
        private const string DeviceReadinessDocsUrl =
            "https://developers.meta.com/horizon/documentation/unity/device-readiness";

        // External-link glyph (Meta generic icon) for the "Read more" affordance.
        private static readonly TextureContent ExternalLinkIcon =
            TextureContent.CreateContent("external_link.png", TextureContent.Categories.Generic, null);

        // RLDS text-link blue as a hex string, for inline rich-text <color> tags
        // (step 2's Label) — those spans can't reference a USS var. Standalone
        // elements use the `.hrt-doc-link`/`.hrt-doc-link__icon` USS classes instead.
        private static string LinkColorHex() =>
            UnityEditor.EditorGUIUtility.isProSkin ? "#64B5FF" : "#0173EC";

        /// <summary>Creates the check-type selection screen with two radio cards and a footer.</summary>
        /// <param name="initialUseAI">
        /// Pre-selected card on entry. When null, neither card is selected and Next is disabled.
        /// When non-null, the matching card starts selected (used when the user steps Back from a later screen).
        /// </param>
        /// <param name="onBack">Invoked when the Back button is clicked.</param>
        /// <param name="onNext">Invoked when Next is clicked, with the selected `useAI` flag.</param>
        /// <returns>A <see cref="VisualElement"/> containing the complete check-type screen.</returns>
        public static VisualElement Create(
            bool? initialUseAI,
            Action onBack,
            Action<bool> onNext,
            Func<string> buildAiPrompt = null,
            Action<bool> onHowItWorksToggled = null,
            Action<bool> onRunItYourselfToggled = null,
            Action onPromptCopied = null)
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;

            // ---- Main content ----
            var scroll = new ScrollView(ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = ScrollerVisibility.Hidden,
            };
            scroll.style.flexGrow = 1;

            var content = new VisualElement();
            content.style.alignItems = Align.Center;
            content.style.paddingLeft = RLDSConstants.Spacing.Size5XL;
            content.style.paddingRight = RLDSConstants.Spacing.Size5XL;
            content.style.paddingTop = RLDSConstants.Spacing.Size5XL;
            content.style.paddingBottom = RLDSConstants.Spacing.Size5XL;

            // Title lockup — centered.
            var titleLockup = new VisualElement();
            titleLockup.style.alignItems = Align.Center;
            titleLockup.style.marginBottom = RLDSConstants.Spacing.Size5XL;

            var heading = new Label(HandReadinessScreenContentProvider.Get(
                "checktype.heading", "How should we check your project?"));
            heading.AddToClassList(RLDSConstants.Typography.Heading2);
            heading.style.marginBottom = RLDSConstants.Spacing.Size3XS;
            heading.style.whiteSpace = WhiteSpace.Normal;
            titleLockup.Add(heading);

            var subtitle = new Label(HandReadinessScreenContentProvider.Get(
                "checktype.subtitle", "Choose how you'd like us to scan for issues."));
            subtitle.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            titleLockup.Add(subtitle);

            content.Add(titleLockup);

            var cardList = new VisualElement();
            cardList.style.width = CardColumnWidth;

            bool? selected = initialUseAI;
            VisualElement standardCard = null;
            VisualElement aiCard = null;
            VisualElement aiSection = null;
            UnityEngine.UIElements.Button nextButton = null;

            void Refresh()
            {
                ApplySelection(standardCard, selected == false);
                ApplySelection(aiCard, selected == true);

                // Built lazily on first AI-select so BuildCompletePrompt (which reads the knowledge files) runs only when needed.
                if (selected == true && aiSection == null)
                {
                    aiSection = CreateAiInfoSection(
                        buildAiPrompt,
                        onHowItWorksToggled,
                        onRunItYourselfToggled,
                        onPromptCopied);
                    cardList.Add(aiSection);
                }
                if (aiSection != null)
                {
                    aiSection.style.display =
                        selected == true ? DisplayStyle.Flex : DisplayStyle.None;
                }

                if (nextButton != null)
                {
                    HandReadinessResources.SetButtonEnabled(nextButton, selected.HasValue);
                }
            }

            standardCard = CreateOptionCard(
                iconResourceName: "icon_list_checked",
                title: HandReadinessScreenContentProvider.Get(
                    "checktype.standard.title", "Standard check"),
                description: HandReadinessScreenContentProvider.Get(
                    "checktype.standard.desc",
                    "Scans your project for common configuration issues and " +
                    "controller-only input patterns that may prevent hand tracking " +
                    "from working."),
                onClick: () => { selected = false; Refresh(); });
            standardCard.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            cardList.Add(standardCard);

            aiCard = CreateOptionCard(
                iconResourceName: "icon_ai_agent",
                title: HandReadinessScreenContentProvider.Get(
                    "checktype.ai.title", "AI-powered check"),
                description: HandReadinessScreenContentProvider.Get(
                    "checktype.ai.desc",
                    "Includes everything in the standard check, plus AI analysis of " +
                    "your code and shaders for targeted, project-specific recommendations. " +
                    "Requires connecting an AI service provider."),
                onClick: () => { selected = true; Refresh(); });
            cardList.Add(aiCard);

            content.Add(cardList);
            scroll.Add(content);
            container.Add(scroll);

            // ---- Footer ----
            var backButton = HandReadinessResources.CreateBackButton("Back", onBack);
            nextButton = HandReadinessResources.CreateNextButton(
                "Next",
                () => onNext(selected ?? false));
            container.Add(HandReadinessResources.CreateFooter(backButton, nextButton));

            Refresh();

            return container;
        }

        private static VisualElement CreateAiInfoSection(
            Func<string> buildAiPrompt,
            Action<bool> onHowItWorksToggled,
            Action<bool> onRunItYourselfToggled,
            Action onPromptCopied)
        {
            var section = new VisualElement();
            section.AddToClassList(HandReadinessStyles.AiInfo.Section);

            var steps = new VisualElement();
            steps.Add(CreateStep(
                "1",
                HandReadinessScreenContentProvider.Get(
                    "checktype.step1.title", "Project context is gathered"),
                HandReadinessScreenContentProvider.Get(
                    "checktype.step1.body",
                    "The tool collects your project's structure, scripts, and dependencies to give " +
                    "the AI agent the context it needs.")));
            steps.Add(CreateStep(
                "2",
                HandReadinessScreenContentProvider.Get(
                    "checktype.step2.title", "Your agent of choice analyzes your project"),
                // The link color is theme-dependent, so the inline <color><u> span is applied
                // in code; the remote value keeps a {link} token where the link phrase goes.
                HandReadinessScreenContentProvider.Get(
                    "checktype.step2.body",
                    "Using the AI agent you connected in the {link}, it looks for " +
                    "controller-dependent patterns, missing hand-tracking components, " +
                    "and gesture gaps, then proposes targeted fixes.")
                    .Replace(
                        "{link}",
                        $"<color={LinkColorHex()}><u>" +
                        HandReadinessScreenContentProvider.Get(
                            "checktype.step2.link", "Meta XR AI Tools setup") +
                        "</u></color>"),
                onLinkClicked: OnStepLinkClicked));
            steps.Add(CreateStep(
                "3",
                HandReadinessScreenContentProvider.Get(
                    "checktype.step3.title", "You get a prioritized report"),
                HandReadinessScreenContentProvider.Get(
                    "checktype.step3.body",
                    "Recommendations are ranked High / Medium / Low. Automated fixes can be applied " +
                    "in one click; manual or AI-assisted ones include step-by-step guidance, and you " +
                    "can send them to your own AI agent to execute further.")));
            if (steps.childCount > 0)
            {
                steps.ElementAt(steps.childCount - 1).style.marginBottom = 0;
            }

            steps.Add(CreateAiInfoLinksRow());

            section.Add(new Accordion(
                HandReadinessScreenContentProvider.Get(
                    "checktype.howItWorks.title", "How AI-powered check works"),
                steps,
                expanded: true,
                leadingIcon: Meta.XR.Editor.UserInterface.Styles.Contents.InfoIcon,
                onToggle: onHowItWorksToggled).Build());

            section.Add(CreateOrDivider());

            var runContent = new VisualElement();

            var promptText = buildAiPrompt?.Invoke();
            if (!string.IsNullOrEmpty(promptText))
            {
                var intro = new Label(
                    HandReadinessScreenContentProvider.Get(
                        "checktype.runItYourself.intro",
                        "Copy the prompt below and paste it into any AI coding agent — Claude Code, " +
                        "Codex, Cursor, or your own. You can review exactly what it will do before " +
                        "running; it's the same analysis, in your environment. <b>Once you've copied it, " +
                        "you can close this window and run it in your own agent whenever you like.</b>"));
                intro.enableRichText = true;
                intro.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
                intro.AddToClassList(HandReadinessStyles.AiInfo.Intro);
                runContent.Add(intro);

                // Known issue: the full prompt exceeds UIToolkit's single text-element
                // render limit, so the code block may not display every line. The Copy
                // button always copies the complete prompt, so this is left as-is.
                runContent.Add(new CodeBlock(
                    promptText,
                    language: null,
                    showCopyButton: true,
                    copyIcon: HandReadinessIcons.Copy,
                    onCopied: onPromptCopied).Build());
            }
            else
            {
                var unavailable = new Label(
                    "The prompt could not be loaded. Check that the Knowledge files are present.");
                unavailable.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
                unavailable.AddToClassList(HandReadinessStyles.WarningText.Root);
                unavailable.style.whiteSpace = WhiteSpace.Normal;
                runContent.Add(unavailable);
            }

            section.Add(new Accordion(
                HandReadinessScreenContentProvider.Get(
                    "checktype.runItYourself.title", "Run it yourself in any AI agent"),
                runContent,
                expanded: false,
                onToggle: onRunItYourselfToggled).Build());

            return section;
        }

        // Right-aligned "Read more" external-doc link with a trailing link icon, in
        // link-blue — mirrors the Welcome window's "Browse Samples [icon]" affordance.
        // The Meta XR AI Tools setup deep-link lives inline in step 2 above, not here.
        private static VisualElement CreateAiInfoLinksRow()
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.FlexEnd;
            row.style.alignItems = Align.Center;
            row.style.marginTop = RLDSConstants.Spacing.SizeMD;

            var readMore = new Label(
                HandReadinessScreenContentProvider.Get("checktype.readMore", "Read more"));
            readMore.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            readMore.AddToClassList(HandReadinessStyles.DocLink.Root);
            row.Add(readMore);

            var icon = new VisualElement();
            icon.style.width = RLDSConstants.IconSize.SizeXS;
            icon.style.height = RLDSConstants.IconSize.SizeXS;
            icon.style.flexShrink = 0;
            icon.style.marginLeft = RLDSConstants.Spacing.Size2XS;
            icon.AddToClassList(HandReadinessStyles.DocLink.Icon);
            ExternalLinkIcon.RegisterToImageLoaded(
                tex => icon.style.backgroundImage = tex as UnityEngine.Texture2D);
            row.Add(icon);

            row.AddManipulator(new Clickable(() =>
                UnityEngine.Application.OpenURL(HandReadinessScreenContentProvider.Get(
                    "checktype.readMore.url", DeviceReadinessDocsUrl))));

            return row;
        }

        // Opens the Meta XR AI Tools setup window (the inline link in step 2's description).
        private static void OnStepLinkClicked()
        {
            Meta.XR.AI.AgentBridge.Utils.OpenWizardAction?.Invoke(
                Meta.XR.Editor.Id.Origins.GuidedSetup);
        }

        private static VisualElement CreateStep(
            string number, string title, string description, Action onLinkClicked = null)
        {
            var row = new VisualElement();
            row.AddToClassList(HandReadinessStyles.AiInfo.Step);

            var numberBadge = new VisualElement();
            numberBadge.AddToClassList(HandReadinessStyles.AiInfo.StepNumber);
            var numberLabel = new Label(number);
            numberLabel.AddToClassList(RLDSConstants.Typography.BodySmallCode);
            numberLabel.AddToClassList(HandReadinessStyles.AiInfo.StepNumberLabel);
            numberBadge.Add(numberLabel);
            row.Add(numberBadge);

            var body = new VisualElement();
            body.AddToClassList(HandReadinessStyles.AiInfo.StepBody);

            var titleLabel = new Label(title);
            titleLabel.AddToClassList(RLDSConstants.Typography.Body2SmallLabel);
            titleLabel.AddToClassList(HandReadinessStyles.AiInfo.StepTitle);
            body.Add(titleLabel);

            var descLabel = new Label(description);
            descLabel.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            descLabel.AddToClassList(HandReadinessStyles.AiInfo.StepDesc);
            if (onLinkClicked != null)
            {
                // Rich text renders the blue underlined link span; the description is the
                // click target (a UIToolkit Label can't reliably surface per-span link-tag
                // click events from this assembly), so a click anywhere in it opens setup.
                descLabel.enableRichText = true;
                descLabel.RegisterCallback<ClickEvent>(_ => onLinkClicked());
            }
            body.Add(descLabel);

            row.Add(body);
            return row;
        }

        private static VisualElement CreateOrDivider()
        {
            var row = new VisualElement();
            row.AddToClassList(HandReadinessStyles.AiInfo.OrDivider);

            var leftLine = new VisualElement();
            leftLine.AddToClassList(HandReadinessStyles.AiInfo.OrDividerLine);
            row.Add(leftLine);

            var orLabel = new Label("or");
            orLabel.AddToClassList(RLDSConstants.Typography.Meta);
            orLabel.AddToClassList(HandReadinessStyles.AiInfo.OrDividerLabel);
            row.Add(orLabel);

            var rightLine = new VisualElement();
            rightLine.AddToClassList(HandReadinessStyles.AiInfo.OrDividerLine);
            row.Add(rightLine);

            return row;
        }

        private static VisualElement CreateOptionCard(
            string iconResourceName,
            string title,
            string description,
            Action onClick)
        {
            var card = new VisualElement();
            card.AddToClassList(HandReadinessStyles.OptionCard.Root);
            if (onClick != null)
            {
                // Make the whole card clickable (not just the label) so the user can
                // tap anywhere in the row to pick it.
                card.AddManipulator(new Clickable(onClick));
            }

            var icon = HandReadinessResources.CreateTintableIcon(
                iconResourceName, RLDSConstants.IconSize.SizeLG);
            icon.AddToClassList(HandReadinessStyles.OptionCard.Icon);
            card.Add(icon);

            var labelDescription = new VisualElement();
            labelDescription.style.flexGrow = 1;
            labelDescription.style.flexBasis = 0;

            var titleLabel = new Label(title);
            titleLabel.AddToClassList(RLDSConstants.Typography.Heading3);
            titleLabel.AddToClassList(HandReadinessStyles.OptionCard.Title);
            titleLabel.style.marginBottom = RLDSConstants.Spacing.Size2XS;
            labelDescription.Add(titleLabel);

            var descLabel = new Label(description);
            descLabel.AddToClassList(RLDSConstants.Typography.Body1Text);
            descLabel.AddToClassList(HandReadinessStyles.OptionCard.Description);
            labelDescription.Add(descLabel);

            card.Add(labelDescription);

            return card;
        }

        // Selected state is communicated via the `.hrt-option-card--selected`
        // modifier class — a filled background plus border accent. No separate
        // check-icon affordance.
        private static void ApplySelection(VisualElement card, bool selected)
        {
            if (card == null) return;
            if (selected)
            {
                card.AddToClassList(HandReadinessStyles.OptionCard.Selected);
            }
            else
            {
                card.RemoveFromClassList(HandReadinessStyles.OptionCard.Selected);
            }
        }
    }
}
