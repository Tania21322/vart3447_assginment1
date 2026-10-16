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
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Meta.XR.Editor.UserInterface.RLDS;
using Meta.XR.AI.AgentBridge;
using Meta.XR.Editor.Id;
using AgentBridgeSettings = Meta.XR.AI.AgentBridge.Settings;
using AgentBridgeUtils = Meta.XR.AI.AgentBridge.Utils;
using RLDSButton = Meta.XR.Editor.UserInterface.RLDS.Button;
using ActionLinkDescription = Meta.XR.Editor.UserInterface.ActionLinkDescription;

namespace Meta.HandReadinessTool.Editor.UI
{
    /// <summary>
    /// Project description input screen — "Tell us about your project."
    /// Only shown on the AI path; the prior CheckType screen routes Standard
    /// users straight to Scanning. Renders an "Active service provider" line
    /// when AgentBridge is on, or the upsell unit when it is off.
    /// </summary>
    public static class ProjectDescriptionScreen
    {
        // A UIToolkit text element stops rendering past ~16K characters (a single text mesh hits
        // the 65,535-vertex / 16-bit index limit) — text beyond it is present but invisible. The
        // whole field (typed prose + any uploaded file) is capped at this: maxLength blocks
        // typing past it, and an upload that would overflow it is rejected.
        private const int MaxDescriptionChars = 16000;

        // Reject before reading so a huge file can't block the main thread in File.ReadAllText;
        // the character cap above is the real limit. Decimal MB matches FormatFileSize ("1 MB").
        private const long MaxUploadBytes = 1 * 1_000_000;

        // The OS file picker's extension filter is only a hint the user can bypass
        // ("All files"), so the selection is re-checked against this allowlist.
        private static readonly string[] AllowedUploadExtensions = { "txt", "md", "json" };

        /// <summary>Creates the project description input screen UI with a text area, file upload, and AI provider state.</summary>
        /// <param name="projectDescription">The initial project description text to display.</param>
        /// <param name="onDescriptionChanged">Callback invoked with the updated text whenever the description changes.</param>
        /// <param name="onUseAIChanged">
        /// Callback invoked with the resolved `useAI` value. Always emits `true` when AgentBridge is
        /// enabled (the user already opted into AI on the prior screen) and `false` when it is
        /// disabled — the upsell unit is shown but the scan falls back to Standard so the user
        /// is not blocked.
        /// </param>
        /// <param name="onBack">Callback invoked when the Back button is clicked.</param>
        /// <param name="onNext">Callback invoked when the Next button is clicked.</param>
        /// <returns>A <see cref="VisualElement"/> containing the complete project description screen.</returns>
        public static VisualElement Create(
            string projectDescription,
            Action<string> onDescriptionChanged,
            Action<bool> onUseAIChanged,
            Action onBack,
            Action onNext)
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.justifyContent = Justify.SpaceBetween;

            var scrollView = new ScrollView();
            scrollView.verticalScrollerVisibility = ScrollerVisibility.Auto;
            scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            scrollView.style.flexGrow = 1;
            scrollView.style.paddingLeft = HandReadinessResources.WizardSideGutter;
            scrollView.style.paddingRight = HandReadinessResources.WizardSideGutter;

            var innerCol = new VisualElement();
            innerCol.style.flexGrow = 1;
            innerCol.style.justifyContent = Justify.Center;
            innerCol.style.paddingTop = RLDSConstants.Spacing.Size4XL;
            innerCol.style.paddingBottom = RLDSConstants.Spacing.Size4XL;

            var headerSection = new VisualElement();
            headerSection.style.paddingBottom = RLDSConstants.Spacing.Size2XL;

            var heading = new Label("Tell us about your project (Optional)");
            heading.AddToClassList(RLDSConstants.Typography.Heading2);
            heading.style.marginBottom = RLDSConstants.Spacing.Size4XS;
            heading.style.whiteSpace = WhiteSpace.Normal;
            headerSection.Add(heading);

            var subtitle = new Label(
                "Describe your game or app so the AI can tailor its recommendations to your project.");
            subtitle.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            headerSection.Add(subtitle);

            innerCol.Add(headerSection);

            var textField = new TextField();
            // Named so UI automation can target the description field directly.
            textField.name = "hrt-description-field";
            textField.multiline = true;
            // Hard-cap total input so typing (and paste) can't push the field past the render
            // limit; uploads are separately checked against the same budget before insertion.
            textField.maxLength = MaxDescriptionChars;
            textField.value = projectDescription ?? "";
#if UNITY_6000_0_OR_NEWER
            textField.textEdition.placeholder =
                "e.g., \"A multiplayer racing game with dynamic weather and physics\"";
#endif
            textField.AddToClassList(HandReadinessStyles.Description.Textarea);

            textField.RegisterValueChangedCallback(evt =>
            {
                onDescriptionChanged?.Invoke(evt.newValue);
            });

            // Fixed-height scroll box: long descriptions scroll within it rather than growing
            // the section. The content container must NOT flex-grow — that caps it to the
            // viewport and clips long content with no way to scroll to the hidden part.
            var textAreaBox = new ScrollView(ScrollViewMode.Vertical);
            textAreaBox.verticalScrollerVisibility = ScrollerVisibility.Auto;
            textAreaBox.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            textAreaBox.AddToClassList(HandReadinessStyles.Description.TextareaBox);
            textAreaBox.Add(textField);

            var contentSection = new VisualElement();
            contentSection.Add(textAreaBox);

            var uploadContainer = new VisualElement();
            uploadContainer.style.marginTop = RLDSConstants.Spacing.SizeXL;

            var uploadHeader = new Label("Upload project files");
            uploadHeader.AddToClassList(RLDSConstants.Typography.Body1Label);
            uploadContainer.Add(uploadHeader);

            var uploadDesc = new Label(
                "Upload .txt, .md, or .json files with technical specs or build info.");
            uploadDesc.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            uploadDesc.style.whiteSpace = WhiteSpace.Normal;
            uploadDesc.style.marginBottom = RLDSConstants.Spacing.SizeMD;
            uploadContainer.Add(uploadDesc);

            var fileCardContainer = new VisualElement();
            fileCardContainer.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            fileCardContainer.style.display = DisplayStyle.None;

            // The exact text block inserted by the most recent upload, so a later upload or
            // card removal can replace/remove just that block without touching user-typed text.
            string previousUploadBlock = null;

            var uploadButton = new UnityEngine.UIElements.Button(() =>
            {
                string path = EditorUtility.OpenFilePanel("Select File", "", "txt,md,json");
                if (string.IsNullOrEmpty(path)) return;

                string fileNameForTelemetry = Path.GetFileName(path);
                string extForTelemetry = (Path.GetExtension(path) ?? "").TrimStart('.').ToLowerInvariant();
                long fileSizeForTelemetry = 0;
                try
                {
                    fileSizeForTelemetry = new FileInfo(path).Length;
                }
                catch { /* file metadata read may fail — that's tracked below as part of the read error */ }

                void SendUploadRejected(string errorKind, string errorMessage)
                {
                    HandReadinessTelemetry.SendEvent(
                        HandReadinessTelemetryConstants.FalcoEventName.ProjectDescriptionFileUploaded,
                        evt =>
                        {
                            evt.SetMetadata(HandReadinessTelemetryConstants.AnnotationType.Success, false);
                            evt.SetMetadata(
                                HandReadinessTelemetryConstants.AnnotationType.FileExtension,
                                extForTelemetry);
                            evt.SetMetadata(
                                HandReadinessTelemetryConstants.AnnotationType.FileSizeBytes,
                                fileSizeForTelemetry);
                            evt.SetMetadata(
                                HandReadinessTelemetryConstants.AnnotationType.ErrorKind,
                                errorKind);
                            evt.SetMetadata(
                                HandReadinessTelemetryConstants.AnnotationType.ErrorMessage,
                                errorMessage);
                        });
                }

                // Re-validate the actual selection: the picker's filter is only a hint on some platforms.
                if (Array.IndexOf(AllowedUploadExtensions, extForTelemetry) < 0)
                {
                    EditorUtility.DisplayDialog(
                        "Unsupported file type",
                        "Only .txt, .md, and .json files are supported. Please choose a different file.",
                        "OK");
                    SendUploadRejected(
                        HandReadinessTelemetryConstants.ErrorKind.UnsupportedFormat,
                        "unsupported_format");
                    return;
                }

                // Validate size before reading: an empty file gives the AI no context,
                // and a huge file would freeze the Editor when read into memory below.
                if (fileSizeForTelemetry == 0)
                {
                    EditorUtility.DisplayDialog(
                        "Empty file",
                        "The selected file is empty. Please upload a file with content.",
                        "OK");
                    SendUploadRejected(HandReadinessTelemetryConstants.ErrorKind.FileEmpty, "empty_file");
                    return;
                }
                if (fileSizeForTelemetry > MaxUploadBytes)
                {
                    EditorUtility.DisplayDialog(
                        "File too large",
                        $"File size exceeds the maximum limit of {FormatFileSize(MaxUploadBytes)}. " +
                        "Please upload a smaller file.",
                        "OK");
                    SendUploadRejected(HandReadinessTelemetryConstants.ErrorKind.FileTooLarge, "file_too_large");
                    return;
                }

                try
                {
                    string fileContent = File.ReadAllText(path);
                    string fileName = Path.GetFileName(path);
                    long fileSize = new FileInfo(path).Length;

                    // A new upload replaces the previous block so the single file card always
                    // matches the description content, rather than silently concatenating files.
                    string baseText = textField.value;
                    if (!string.IsNullOrEmpty(previousUploadBlock) && baseText.Contains(previousUploadBlock))
                        baseText = baseText.Replace(previousUploadBlock, "");

                    string separator = string.IsNullOrEmpty(baseText) ? "" : "\n\n";
                    string uploadBlock = separator + "--- Uploaded from: " + fileName + " ---\n\n" + fileContent;
                    string currentText = baseText + uploadBlock;

                    // Reject rather than truncate: the whole field must stay under the render
                    // limit, and silently dropping part of the file would send the AI less than
                    // the user believes they attached.
                    if (currentText.Length > MaxDescriptionChars)
                    {
                        EditorUtility.DisplayDialog(
                            "File too large",
                            $"Adding this file would exceed the {MaxDescriptionChars:N0}-character limit for the " +
                            "project description. Please upload a smaller file or shorten your description.",
                            "OK");
                        SendUploadRejected(HandReadinessTelemetryConstants.ErrorKind.FileTooLarge, "file_too_large");
                        return;
                    }

                    previousUploadBlock = uploadBlock;
                    textField.value = currentText;
                    onDescriptionChanged?.Invoke(currentText);

                    fileCardContainer.Clear();
                    fileCardContainer.Add(CreateFileCard(fileName, fileSize, () =>
                    {
                        // Remove only the uploaded block, preserving any user-typed text.
                        string remaining = textField.value;
                        if (!string.IsNullOrEmpty(previousUploadBlock) && remaining.Contains(previousUploadBlock))
                            remaining = remaining.Replace(previousUploadBlock, "");
                        previousUploadBlock = null;
                        textField.value = remaining;
                        onDescriptionChanged?.Invoke(remaining);
                        fileCardContainer.style.display = DisplayStyle.None;

                        HandReadinessTelemetry.SendEvent(
                            HandReadinessTelemetryConstants.FalcoEventName.ProjectDescriptionFileRemoved,
                            evt =>
                            {
                                evt.SetMetadata(
                                    HandReadinessTelemetryConstants.AnnotationType.FileExtension,
                                    extForTelemetry);
                            });
                    }));
                    fileCardContainer.style.display = DisplayStyle.Flex;

                    HandReadinessTelemetry.SendEvent(
                        HandReadinessTelemetryConstants.FalcoEventName.ProjectDescriptionFileUploaded,
                        evt =>
                        {
                            evt.SetMetadata(HandReadinessTelemetryConstants.AnnotationType.Success, true);
                            evt.SetMetadata(
                                HandReadinessTelemetryConstants.AnnotationType.FileExtension,
                                extForTelemetry);
                            evt.SetMetadata(
                                HandReadinessTelemetryConstants.AnnotationType.FileSizeBytes,
                                fileSize);
                        });
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[HRT] Failed to read file: {ex.Message}");

                    HandReadinessTelemetry.SendEvent(
                        HandReadinessTelemetryConstants.FalcoEventName.ProjectDescriptionFileUploaded,
                        evt =>
                        {
                            evt.SetMetadata(HandReadinessTelemetryConstants.AnnotationType.Success, false);
                            evt.SetMetadata(
                                HandReadinessTelemetryConstants.AnnotationType.FileExtension,
                                extForTelemetry);
                            evt.SetMetadata(
                                HandReadinessTelemetryConstants.AnnotationType.FileSizeBytes,
                                fileSizeForTelemetry);
                            evt.SetMetadata(
                                HandReadinessTelemetryConstants.AnnotationType.ErrorKind,
                                HandReadinessTelemetryConstants.ErrorKind.ReadFailed);
                            evt.SetMetadata(
                                HandReadinessTelemetryConstants.AnnotationType.ErrorMessage,
                                ex.Message);
                        });
                }
            });
            uploadButton.text = "";
            uploadButton.AddToClassList(HandReadinessStyles.UploadButton.Root);
            uploadButton.style.flexDirection = FlexDirection.Row;
            uploadButton.style.alignSelf = Align.FlexStart;
            uploadButton.style.flexGrow = 0;

            var uploadIcon = HandReadinessResources.CreateTintableIcon("icon_upload",
                RLDSConstants.IconSize.SizeSM - 2);
            uploadIcon.AddToClassList(HandReadinessStyles.UploadButton.Icon);
            uploadIcon.style.marginRight = RLDSConstants.Spacing.Size2XS;
            uploadButton.Add(uploadIcon);
            var uploadBtnLabel = new Label("Upload file");
            uploadBtnLabel.AddToClassList(RLDSConstants.Typography.Body2SmallLabel);
            uploadButton.Add(uploadBtnLabel);

            uploadContainer.Add(uploadButton);
            uploadContainer.Add(fileCardContainer);
            contentSection.Add(uploadContainer);
            innerCol.Add(contentSection);

            // ---- AI provider section ----
            // When AgentBridge is enabled and a provider is initialized, surface the
            // active provider so the user can confirm which service the scan will
            // hit. When AgentBridge is disabled, replace this slot with the upsell
            // unit (badge + heading + description + "Open project settings" button)
            // and force the scan to fall back to Standard.
            var aiSection = new VisualElement();
            aiSection.style.paddingTop = RLDSConstants.Spacing.SizeMD;
            aiSection.style.paddingBottom = RLDSConstants.Spacing.SizeMD;

            if (AgentBridgeSettings.IsEnabled)
            {
                BuildActiveProviderRow(aiSection);
                onUseAIChanged?.Invoke(true);
            }
            else
            {
                BuildAiUpsellUnit(aiSection, onUseAIChanged);
            }
            innerCol.Add(aiSection);

            scrollView.Add(innerCol);
            container.Add(scrollView);

            var backButton = HandReadinessResources.CreateBackButton("Back", onBack);
            var nextButton = HandReadinessResources.CreateNextButton("Next", onNext);
            // The project description is optional — Next stays enabled even when empty.

            container.Add(HandReadinessResources.CreateFooter(backButton, nextButton));

            return container;
        }

        /// <summary>
        /// Renders an "Active service provider: {name}" line with a green check icon
        /// when a provider is initialized. No-op when no provider is set, since the
        /// AI scan would still fall through to AgentBridge's provider-resolution at
        /// runtime — we just don't have a name to show yet.
        /// </summary>
        private static void BuildActiveProviderRow(VisualElement aiSection)
        {
            AgentBridgeAPI.EnsureServiceInitialized();
            var providerName = AgentBridgeAPI.GetCurrentServiceName();
            if (string.IsNullOrEmpty(providerName) || providerName == "None") return;

            var providerRow = new VisualElement();
            providerRow.style.flexDirection = FlexDirection.Row;
            providerRow.style.alignItems = Align.Center;

            var providerIcon = HandReadinessResources.CreateTintableIcon(
                "icon_check_circle", RLDSConstants.IconSize.SizeXS);
            providerIcon.AddToClassList(HandReadinessStyles.Icon.ThemedPositive);
            providerIcon.style.marginRight = RLDSConstants.Spacing.Size2XS;
            providerRow.Add(providerIcon);

            var providerLabel = new Label($"Active service provider: {providerName}");
            providerLabel.AddToClassList(RLDSConstants.Typography.Tiny);
            providerRow.Add(providerLabel);

            aiSection.Add(providerRow);
        }

        /// <summary>
        /// Renders the AI upsell unit shown when the AgentBridge master toggle
        /// is off: a Notification-pill "New" badge, bold headline, supporting
        /// description, and an "Open project settings" button that jumps to the
        /// AgentBridge preferences. Forces the parent scan to skip the AI phase
        /// via `onUseAIChanged(false)` so a user who proceeds anyway runs
        /// Standard rather than hitting a failed AI scan.
        /// </summary>
        private static void BuildAiUpsellUnit(VisualElement aiSection, Action<bool> onUseAIChanged)
        {
            // The AI scan is unreachable without an enabled AgentBridge — make the
            // downstream scan fall back to Standard so the user isn't blocked.
            onUseAIChanged?.Invoke(false);

            aiSection.style.flexDirection = FlexDirection.Column;
            aiSection.style.alignItems = Align.FlexStart;

            // "New" BadgePill with the star icon and Notification/Info variant.
            var newBadge = new VisualElement();
            newBadge.AddToClassList(RLDSConstants.BadgePill.Base);
            newBadge.AddToClassList(RLDSConstants.BadgePill.Info);
            newBadge.style.marginBottom = RLDSConstants.Spacing.SizeXS;

            var newIcon = HandReadinessResources.CreateTintableIcon(
                "icon_new_star", RLDSConstants.IconSize.SizeXS);
            newIcon.AddToClassList(HandReadinessStyles.Icon.Themed);
            newIcon.AddToClassList(RLDSConstants.BadgePill.Icon);
            newBadge.Add(newIcon);

            var newLabel = new Label("New");
            newLabel.AddToClassList(RLDSConstants.BadgePill.Label);
            newBadge.Add(newLabel);
            aiSection.Add(newBadge);

            var heading = new Label("AI-powered recommendations now available");
            heading.AddToClassList(RLDSConstants.Typography.Body1Label);
            aiSection.Add(heading);

            var description = new Label(
                "Enable AI recommendations to get intelligent suggestions for improving " +
                "your project. Choose your preferred AI provider (Claude Code, ChatGPT, " +
                "and more) in Project Settings.");
            description.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            aiSection.Add(description);

            var openSettingsBtn = new RLDSButton(
                new ActionLinkDescription
                {
                    Content = new GUIContent("Open project settings"),
                    Action = () =>
                    {
                        HandReadinessTelemetry.SendEvent(
                            HandReadinessTelemetryConstants.FalcoEventName.AgentBridgeSetupClicked,
                            isEssential: true);
                        AgentBridgeUtils.ToolDescriptor.OnClickDelegate?.Invoke(Origins.ProjectSettings);
                    },
                },
                RLDSConstants.ButtonVariant.Secondary, RLDSConstants.ButtonSize.Small).Build();
            openSettingsBtn.style.alignSelf = Align.FlexStart;
            aiSection.Add(openSettingsBtn);
        }

        private static VisualElement CreateFileCard(string fileName, long fileSizeBytes, Action onRemove)
        {
            var card = new VisualElement();
            card.AddToClassList(HandReadinessStyles.FileCard.Root);
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;

            var fileIcon = HandReadinessResources.CreateTintableIcon("icon_file", 20);
            fileIcon.AddToClassList(HandReadinessStyles.FileCard.Icon);
            fileIcon.style.marginRight = RLDSConstants.Spacing.SizeXS;
            card.Add(fileIcon);

            var textCol = new VisualElement();
            textCol.style.flexGrow = 1;
            var nameLabel = new Label(fileName);
            nameLabel.AddToClassList(RLDSConstants.Typography.Body2SmallLabel);
            textCol.Add(nameLabel);
            var sizeLabel = new Label(FormatFileSize(fileSizeBytes));
            sizeLabel.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            textCol.Add(sizeLabel);
            card.Add(textCol);

            var removeBtn = new UnityEngine.UIElements.Button(onRemove);
            removeBtn.text = "\u2715";
            removeBtn.AddToClassList(HandReadinessStyles.FileCard.RemoveButton);
            card.Add(removeBtn);

            return card;
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes >= 1_000_000)
                return $"{bytes / 1_000_000.0:F1} MB";
            if (bytes >= 1_000)
                return $"{bytes / 1_000.0:F1} KB";
            return $"{bytes} B";
        }
    }
}
