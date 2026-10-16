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
using System.Linq;
using Meta.XR.AI.AgentBridge;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.ToolingSupport;
using Meta.XR.Editor.UserInterface;
using Meta.XR.Editor.UserInterface.RLDS;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Label = UnityEngine.UIElements.Label;
using DropdownMenu = Meta.XR.Editor.UserInterface.DropdownMenu;
using DropdownMenuItem = Meta.XR.Editor.UserInterface.DropdownMenuItem;
using RLDSStyles = Meta.XR.Editor.UserInterface.RLDS.Styles;
using ScrollView = UnityEngine.UIElements.ScrollView;
using AgentBridgeSettings = Meta.XR.AI.AgentBridge.Settings;
using AgentBridgeUtils = Meta.XR.AI.AgentBridge.Utils;
using Spinner = Meta.XR.Editor.UserInterface.RLDS.Spinner;
using RLDSButton = Meta.XR.Editor.UserInterface.RLDS.Button;
using StepState = Meta.XR.Editor.AIToolsSetupModel.StepState;

namespace Meta.XR.Editor
{
    [InitializeOnLoad]
    internal class AIToolsSetupWindow : EditorWindow, IIdentified
    {
        public string Id => ToolDescriptor.Id;


        internal const float WindowWidth = 625;
        internal const float WindowHeight = 812;

        internal const string SettingsPath = "Preferences/Meta XR/AI Tools";

        // Matches WelcomeWindow.DownloadIcon so the "Download Meta VR CLI" CTA here reads the same
        // as the Welcome card's "Download" CTA for the same tool.
        private static readonly TextureContent DownloadIcon =
            TextureContent.CreateContent("download.png", TextureContent.Categories.Generic, null);

        internal static readonly ToolDescriptor ToolDescriptor = new()
        {
            Name = AIToolsSetupStrings.Header.WindowTitle,
            DisplayName = AIToolsSetupStrings.Header.DisplayTitle,
            MenuDescription = AIToolsSetupStrings.Header.MenuDescription,
            Description =
                "Configure AI coding assistant integration and the MCP tools server for Meta XR SDK. " +
                "Enables communication with AI assistants for inference, debugging, and automation.",
            Color = UserInterface.Styles.Colors.AI,
            Icon = TextureContent.CreateContent("feature_ai_agent.png", TextureContent.Categories.Generic),
            Experimental = true,
            DrawExperimentalInStatusMenu = true,
            AddToStatusMenu = true,
            MenuCategory = MenuCategory.Tools,
            AddToMenu = true,
            OnClickDelegate = OnToolClicked,
            OnUserSettingsGUI = OnSettingsGUI,
            InfoTextDelegate = ComputeInfoText,
            EnableRampUp = false,
            Order = 9,
            // Always render last in the Tools section of the SDK status menu.
            ShowLastInStatusMenu = true
        };

        static AIToolsSetupWindow()
        {
            AgentBridgeUtils.OpenWizardAction = OnToolClicked;
            AgentBridgeUtils.OpenWizardSettingsGUI = OnSettingsGUI;
        }

        private static (string, Color?) ComputeInfoText()
        {
            if (!AgentBridgeSettings.IsEnabled)
                return ("Not connected", UserInterface.Styles.Colors.DisabledColor);
            // The AI Agent Bridge being enabled is the connection signal; the editor-tools MCP is
            // internal-only and the per-card XR Operator status shows the tool source.
            return ("Connected", UserInterface.Styles.Colors.SuccessColor);
        }
        private AIToolsSetupModel _model;
        private StyleSheet _styleSheet;
        private float _savedScrollOffset;

        // Scroll-offset restore is deferred a frame (the new ScrollView has no layout yet), so these
        // guard against a rebuild landing inside that window. See BuildUI.
        private bool _scrollRestorePending;
        private int _scrollRestoreGeneration;

        // Reset at the start of each BuildUI; NextStepLabel increments it so step numbering
        // stays gap-free when the Windows-only firewall step is skipped on macOS.
        private int _stepCounter;
        private long _openedAtMs;
        private Origins _pendingOrigin = Origins.Unknown;

        private void OnFirewallStatusChanged()
        {
            if (_model == null)
            {
                return;
            }
            _model.SyncFirewallState();
            BuildUI();
        }

        private void OnProviderStateChanged()
        {
            if (_model == null)
            {
                return;
            }
            _model.RefreshStepStates();
            BuildUI();
        }

        private void OnAgentBridgeEnabledChanged()
        {
            if (_model == null)
            {
                return;
            }
            _model.RefreshStepStates();
            BuildUI();
        }

        private void OnSelectedServiceChangedExternally(string serviceId)
        {
            EditorApplication.delayCall += () =>
            {
                if (_model == null)
                {
                    return;
                }

                if (_model.SelectedServiceId != serviceId)
                {
                    _model.SyncSelectedServiceFromSettings();
                    BuildUI();
                }

                UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
            };
        }

        private void OnFocus()
        {
            // Some state can change outside this window without raising an event we subscribe to —
            // e.g. the Project Setup Tool's "Apply All" activates Meta XR Operator. Re-derive state
            // and rebuild on focus so the window reflects those external changes.
            if (_model == null)
            {
                return;
            }
            _model.RefreshStepStates();
            BuildUI();
        }

        private void OnDisable()
        {
            WindowsFirewallUtility.StatusChanged -= OnFirewallStatusChanged;
            AIToolsSetupModel.ProviderStateChanged -= OnProviderStateChanged;
            AgentBridgeSettings.Activated -= OnAgentBridgeEnabledChanged;
            AgentBridgeSettings.Deactivated -= OnAgentBridgeEnabledChanged;
            AgentBridgeSettings.SelectedServiceChanged -= OnSelectedServiceChangedExternally;
            var elapsed = (System.Diagnostics.Stopwatch.GetTimestamp() - _openedAtMs)
                * 1000 / System.Diagnostics.Stopwatch.Frequency;
            var completedAll = _model != null && _model.IsFullSetupComplete;
            AIToolsSetupTelemetry.SendEvent(
                AIToolsSetupTelemetryConstants.FalcoEventName.Closed,
                evt =>
                {
                    evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.DurationMs, elapsed);
                    evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.CompletedFullSetup, completedAll);
                });
            AIToolsSetupTelemetry.ResetSession();
            AIToolsSetupTelemetry.ActiveModel = null;
        }

        private static void OnToolClicked(Origins origin)
        {
            var window = GetWindow<AIToolsSetupWindow>();
            window.titleContent = new GUIContent(AIToolsSetupStrings.Header.DisplayTitle);
            window.minSize = new Vector2(WindowWidth, WindowHeight);
            window._pendingOrigin = origin;
            window.Show();
            window.Focus();
        }

        private static string EntryPointFor(Origins origin)
        {
            switch (origin)
            {
                case Origins.UserSettings:
                case Origins.ProjectSettings:
                    return AIToolsSetupTelemetryConstants.EntryPoint.Preferences;
                case Origins.GuidedSetup:
                    return AIToolsSetupTelemetryConstants.EntryPoint.GuidedSetup;
                case Origins.Self:
                case Origins.Automatic:
                    return AIToolsSetupTelemetryConstants.EntryPoint.Code;
                case Origins.Unknown:
                    return AIToolsSetupTelemetryConstants.EntryPoint.Unknown;
                default:
                    return AIToolsSetupTelemetryConstants.EntryPoint.Menu;
            }
        }

        private static void OnSettingsGUI(Origins origin, string searchContext)
        {
            ToolDescriptor.DrawButton(null, false, true, origin);

            EditorGUILayout.Space();

            AgentBridgeSettings.EmbeddedMode = true;
            try { AgentBridgeSettings.OnGUI(origin, searchContext); }
            finally { AgentBridgeSettings.EmbeddedMode = false; }

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Reset All to Defaults"))
            {
                AgentBridgeSettings.ResetToDefaults();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void CreateGUI()
        {
            // Ensure service registry is populated so Step 2 can render the service
            // selector and commands even before the bridge is enabled.
            AIServiceRegistry.Initialize();

            titleContent = new GUIContent(AIToolsSetupStrings.Header.WindowTitle);
            _model = new AIToolsSetupModel();
            _model.SetStateChangedCallback(BuildUI);

            // Keep the firewall sub-step live when the rule changes outside this window (e.g. the
            // Meta/Internal debug menu or the preferences panel). Unsubscribe-then-subscribe is idempotent.
            WindowsFirewallUtility.StatusChanged -= OnFirewallStatusChanged;
            WindowsFirewallUtility.StatusChanged += OnFirewallStatusChanged;

            // Providers detect some prerequisites asynchronously (e.g. the Meta VR CLI's
            // `tools info` probe); re-derive when one lands. Unsubscribe-then-subscribe is idempotent.
            AIToolsSetupModel.ProviderStateChanged -= OnProviderStateChanged;
            AIToolsSetupModel.ProviderStateChanged += OnProviderStateChanged;

            // Keep the bridge column live when the AI Agent Bridge is toggled elsewhere (e.g. the
            // preferences panel). Unsubscribe-then-subscribe is idempotent.
            AgentBridgeSettings.Activated -= OnAgentBridgeEnabledChanged;
            AgentBridgeSettings.Activated += OnAgentBridgeEnabledChanged;
            AgentBridgeSettings.Deactivated -= OnAgentBridgeEnabledChanged;
            AgentBridgeSettings.Deactivated += OnAgentBridgeEnabledChanged;

            // Keep the service dropdown live when the selection changes elsewhere (e.g. the
            // preferences popup). Unsubscribe-then-subscribe is idempotent.
            AgentBridgeSettings.SelectedServiceChanged -= OnSelectedServiceChangedExternally;
            AgentBridgeSettings.SelectedServiceChanged += OnSelectedServiceChangedExternally;
            AIToolsSetupTelemetry.ActiveModel = _model;
            _openedAtMs = System.Diagnostics.Stopwatch.GetTimestamp();
            AIToolsSetupTelemetry.EnsureSessionId();
            AIToolsSetupTelemetry.SendEvent(
                AIToolsSetupTelemetryConstants.FalcoEventName.Opened,
                evt => evt.SetMetadata(
                    AIToolsSetupTelemetryConstants.AnnotationType.EntryPoint,
                    EntryPointFor(_pendingOrigin)),
                isEssential: true);
            rootVisualElement.schedule.Execute(BuildUI);
            rootVisualElement.schedule.Execute(() => _ = _model.VerifyConnectionAsync(silent: true));
            rootVisualElement.schedule.Execute(() => _ = _model.CheckSelectedServiceInstalledAsync());
        }

        private void BuildUI()
        {
            var root = rootVisualElement;

            // Skip the save while a restore is still pending: that ScrollView is a fresh one sitting at
            // 0 until the scheduled restore below runs, so reading it would overwrite the real offset
            // with 0. Rebuilds can arrive faster than one per frame — the headset setup publishes a
            // state change as each of its steps starts and finishes — which otherwise walks the panel
            // back to the top mid-run.
            var oldScrollView = root.Q<ScrollView>();
            if (oldScrollView != null && !_scrollRestorePending)
            {
                _savedScrollOffset = oldScrollView.scrollOffset.y;
            }

            root.Clear();

            _styleSheet = RLDSUtils.LoadStyleSheet(isLightMode: !EditorGUIUtility.isProSkin);
            if (_styleSheet != null && !root.styleSheets.Contains(_styleSheet))
            {
                root.styleSheets.Add(_styleSheet);
            }

            var scrollView = new ScrollView(ScrollViewMode.Vertical);
            scrollView.style.paddingBottom = RLDSConstants.Spacing.SizeLG;
            scrollView.style.paddingLeft = RLDSConstants.Spacing.SizeLG;
            scrollView.style.paddingRight = RLDSConstants.Spacing.SizeLG;
            scrollView.style.paddingTop = RLDSConstants.Spacing.SizeLG;
            root.Add(scrollView);

            var container = new VisualElement();
            container.style.paddingBottom = RLDSConstants.Spacing.SizeLG;
            scrollView.Add(container);

            _stepCounter = 0;

            AddHeader(container);
            AddDivider(container);
            AddSelectServiceStep(container);
            AddInstallAndSetupStep(container);
            if (AIToolsSetupModel.IsWindows())
            {
                AddFirewallStep(container);
            }
            AddSkillsStep(container);
            AddProjectSetupStep(container);
            AddDivider(container);
            AddAdvancedSettingsSection(container);

            // Only the newest rebuild's restore may apply: an earlier one would target a detached
            // ScrollView and clear the pending flag while this rebuild is still at offset 0.
            var restoreGeneration = ++_scrollRestoreGeneration;
            _scrollRestorePending = true;
            scrollView.schedule.Execute(() =>
            {
                if (restoreGeneration != _scrollRestoreGeneration)
                {
                    return;
                }

                scrollView.scrollOffset = new Vector2(0, _savedScrollOffset);
                _scrollRestorePending = false;
            });
        }

        // --- Header ---

        private void AddHeader(VisualElement container)
        {
            var titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.marginBottom = RLDSConstants.Spacing.SizeSM;

            var title = new Label(AIToolsSetupStrings.Header.WindowTitle);
            title.AddToClassList(RLDSConstants.Typography.Heading2);
            title.style.marginRight = RLDSConstants.Spacing.SizeSM;
            titleRow.Add(title);

            var badgeElement = new BadgePill(
                AIToolsSetupStrings.Header.ExperimentalBadge,
                BadgePillType.Neutral,
                BadgePillSize.Normal,
                UserInterface.Styles.Contents.ExperimentalIcon).Build();
            titleRow.Add(badgeElement);

            container.Add(titleRow);

            var description = new Label(AIToolsSetupStrings.Header.Description);
            description.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            description.style.whiteSpace = WhiteSpace.Normal;
            container.Add(description);

            AddSpacer(container, RLDSConstants.Spacing.SizeSM);

            var resourcesRow = new VisualElement();
            resourcesRow.style.flexDirection = FlexDirection.Row;
            resourcesRow.style.alignItems = Align.Center;

            var resourcesLabel = new Label(AIToolsSetupStrings.Header.Resources);
            resourcesLabel.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            resourcesLabel.style.marginRight = RLDSConstants.Spacing.SizeXS;
            resourcesRow.Add(resourcesLabel);

            var quickStartLink = new LinkLabel(
                new UrlLinkDescription()
                {
                    Content = new GUIContent(AIToolsSetupStrings.Header.QuickStartGuide),
                    URL = AIToolsSetupStrings.Links.QuickStartUrl,
                    Origin = Origins.GuidedSetup,
                    OriginData = this,
                    Color = EditorGUIUtility.isProSkin ? RLDSStyles.Colors.TextLink : RLDSStyles.Colors.LightTextLink
                }).Build();
            resourcesRow.Add(quickStartLink);

            container.Add(resourcesRow);
        }

        // --- Step 1: Select your service provider ---

        private void AddSelectServiceStep(VisualElement container)
        {
            AddStepTitle(container, AIToolsSetupStrings.Steps.SelectServiceProvider);

            var desc = new Label(AIToolsSetupStrings.Descriptions.SelectServiceProvider);
            desc.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            container.Add(desc);

            container.Add(MakeServiceSelector(true));

            AddSpacer(container, RLDSConstants.Spacing.SizeLG);
        }

        // --- Step numbering helpers ---

        private string NextStepLabel(string title) => $"Step {++_stepCounter}: {title}";

        private void AddStepTitle(VisualElement container, string title)
        {
            var stepTitle = new Label(NextStepLabel(title));
            stepTitle.AddToClassList(RLDSConstants.Typography.Heading3);
            stepTitle.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            container.Add(stepTitle);
        }

        // --- Step 2: Install and set up AI tools (two product columns) ---

        private void AddInstallAndSetupStep(VisualElement container)
        {
            AddStepTitle(container, AIToolsSetupStrings.Steps.InstallAndSetup);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Stretch;

            // Left column: the MetaXROperator provider (MCP Proxy + meta-xr-operator registration + activate).
            var provider = AIToolsSetupRegistry.GetProvidersSorted()
                .FirstOrDefault(p => p.GetPrerequisiteInstall() != null
                    || p.GetRegistrationInfo(_model.GetSelectedServiceExecutable()).HasValue);
            if (provider != null)
            {
                var agenticCard = BuildAgenticColumn(provider);
                agenticCard.style.marginRight = RLDSConstants.Spacing.SizeSM;
                row.Add(agenticCard);
            }

            // Right column: the AI Agent Bridge (Unity editor tools -> local agent). It does not
            // register an MCP server; the editor-tools MCP is internal-only.
            row.Add(BuildBridgeColumn());

            container.Add(row);

            AddSpacer(container, RLDSConstants.Spacing.SizeLG);
        }

        private VisualElement BuildAgenticColumn(IAIToolsProvider provider)
        {
            var card = MakeProductCard();
            AddCardHeader(card,
                AIToolsSetupStrings.Columns.AgenticBadge,
                TextureContent.CreateContent(
                    "feature_ai_agent.png", TextureContent.Categories.Generic),
                AIToolsSetupStrings.Columns.AgenticHeading,
                AIToolsSetupStrings.Columns.AgenticDescription);

            // Install: MCP Proxy prerequisite.
            var installComplete = true;
            var prereq = provider.GetPrerequisiteInstall();
            if (prereq != null)
            {
                var state = StepState.Incomplete;
                string error = null;
                var updateAvailable = false;
                if (_model.PrerequisiteStates.TryGetValue(provider.Id, out var status))
                {
                    state = status.State;
                    error = status.Error;
                    updateAvailable = status.UpdateAvailable;
                }
                installComplete = state == StepState.Complete;
                AddColumnInstallControl(card, state, error,
                    installText: prereq.InstallButtonText
                        ?? AIToolsSetupStrings.Buttons.InstallMcpProxy,
                    installingText: prereq.InstallingText
                        ?? AIToolsSetupStrings.Processing.InstallingProxy,
                    installedText: prereq.InstalledStatusText
                        ?? AIToolsSetupStrings.ConnectionStatus.ProxyInstalled,
                    notInstalledText: prereq.NotInstalledStatusText
                        ?? AIToolsSetupStrings.ConnectionStatus.ProxyNotInstalled,
                    onInstall: () => _ = _model.InstallPrerequisiteAsync(provider.Id),
                    updateAvailable: updateAvailable,
                    updateText: prereq.UpdateButtonText,
                    onUpdate: () => _ = _model.InstallPrerequisiteAsync(provider.Id),
                    ctaUrl: prereq.CtaUrl);
            }

            // Register: meta-xr-operator command + run. Divider separates the install step above
            // from the registration step below.
            var regInfo = provider.GetRegistrationInfo(_model.GetSelectedServiceExecutable());
            if (regInfo.HasValue)
            {
                if (prereq != null)
                    AddCardDivider(card);

                if (regInfo.Value.ManualOnly)
                {
                    // Provider can't be auto-registered from the CLI (e.g. OpenCode): show the
                    // proxy path to copy plus a docs link, instead of a runnable command + Run button.
                    AddCommandBlock(card, regInfo.Value.Command,
                        regInfo.Value.ManualInstructions
                            ?? AIToolsSetupStrings.OpenCode.ManualInstructions);
                    if (!string.IsNullOrEmpty(regInfo.Value.DocsUrl))
                        AddDocsLink(card, regInfo.Value.DocsLinkText, regInfo.Value.DocsUrl);
                }
                else
                {
                    AddCommandBlock(card, regInfo.Value.Command);
                    AddColumnRunAndStatus(card,
                        installComplete,
                        _model.Step2RuntimeState,
                        _model.Step2RuntimeError,
                        onSetup: () => _ = _model.SetupProvidersAsync(),
                        disabledTooltip: AIToolsSetupStrings.Buttons.RunCommandProxyDisabledTooltip);
                }
            }

            // Activate: provider-specific (MetaXROperator OpenXR layer) at the bottom of the column.
            var activate = provider.BuildActivateSection(BuildUI);
            if (activate != null)
            {
                AddCardDivider(card);
                card.Add(activate);
            }

            // Headset connection: one action for everything a Quest APK build needs on device, with a
            // checklist under it so each step's progress — and any single step's failure — stays visible.
            AddDeviceSectionHeader(card,
                AIToolsSetupStrings.Device.QuestHeading, AIToolsSetupStrings.Device.QuestDescription);

            AddHeadsetControls(card);
            AddHeadsetSetupChecklist(card);

            return card;
        }

        private VisualElement BuildBridgeColumn()
        {
            var card = MakeProductCard();
            AddCardHeader(card,
                AIToolsSetupStrings.Columns.BridgeBadge,
                TextureContent.CreateContent(
                    "arrow_left_right.png", TextureContent.Categories.Generic),
                AIToolsSetupStrings.Columns.BridgeHeading,
                AIToolsSetupStrings.Columns.BridgeDescription);

            var toolsNote = new Label(AIToolsSetupStrings.Columns.BridgeToolsNote);
            toolsNote.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            toolsNote.style.whiteSpace = WhiteSpace.Normal;
            toolsNote.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            card.Add(toolsNote);

            var bridgeState = _model.Step1BridgeState;
            AddColumnInstallControl(card, bridgeState, _model.Step1BridgeError,
                installText: AIToolsSetupStrings.Buttons.InstallBridge,
                installingText: AIToolsSetupStrings.Processing.Installing,
                installedText: AIToolsSetupStrings.ConnectionStatus.BridgeInstalled,
                notInstalledText: AIToolsSetupStrings.ConnectionStatus.BridgeNotInstalled,
                onInstall: () => _model.InstallBridgeAsync());

            // No MCP registration here: the AI Agent Bridge lets Unity's editor tools call the local
            // AI agent; it does not register an MCP server. (The editor-tools MCP is internal-only.)

            // Headset connection: adb reverse (device -> host) so a Quest build's in-headset assistant
            // reaches this Remote Agent Server.
            AddDeviceSectionHeader(card,
                AIToolsSetupStrings.Device.ReverseHeading, AIToolsSetupStrings.Device.ReverseDescription);

            AddDeviceAction(card, _model.AdbReverse,
                description: null,
                AIToolsSetupStrings.Device.ReverseButton,
                AIToolsSetupStrings.Device.Reversing,
                RunDeviceAction(_model.EnsureAgentBridgeReverseAsync));

            return card;
        }

        private static VisualElement MakeProductCard(bool grow = true)
        {
            var card = new VisualElement();
            card.AddToClassList(RLDSConstants.Surface.Secondary);
            // grow=true: equal-width columns in a row. grow=false: full-width block in the
            // vertical step stack (flexGrow/flexBasis here would collapse the card's height).
            if (grow)
            {
                card.style.flexGrow = 1;
                card.style.flexBasis = 0;
                card.style.flexShrink = 1;
            }
            card.style.borderTopLeftRadius = RLDSConstants.Radius.SizeSM;
            card.style.borderTopRightRadius = RLDSConstants.Radius.SizeSM;
            card.style.borderBottomLeftRadius = RLDSConstants.Radius.SizeSM;
            card.style.borderBottomRightRadius = RLDSConstants.Radius.SizeSM;
            card.style.paddingTop = RLDSConstants.Spacing.SizeMD;
            card.style.paddingBottom = RLDSConstants.Spacing.SizeMD;
            card.style.paddingLeft = RLDSConstants.Spacing.SizeMD;
            card.style.paddingRight = RLDSConstants.Spacing.SizeMD;
            return card;
        }

        private static void AddCardHeader(VisualElement card, string badge,
            TextureContent badgeIcon, string heading, string description)
        {
            var badgeElement = new BadgePill(
                badge, BadgePillType.Neutral, BadgePillSize.Small, badgeIcon).Build();
            badgeElement.style.alignSelf = Align.FlexStart;
            badgeElement.style.marginBottom = RLDSConstants.Spacing.SizeXS;
            card.Add(badgeElement);

            var headingLabel = new Label(heading);
            headingLabel.AddToClassList(RLDSConstants.Typography.Body1Label);
            headingLabel.style.marginBottom = RLDSConstants.Spacing.Size2XS;
            card.Add(headingLabel);

            var descLabel = new Label(description);
            descLabel.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            descLabel.style.whiteSpace = WhiteSpace.Normal;
            descLabel.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            card.Add(descLabel);
        }

        private void AddColumnInstallControl(VisualElement card, StepState state, string error,
            string installText, string installingText, string installedText,
            string notInstalledText, Action onInstall,
            bool updateAvailable = false, string updateText = null, Action onUpdate = null,
            string ctaUrl = null)
        {
            switch (state)
            {
                case StepState.Incomplete:
                    if (!string.IsNullOrEmpty(ctaUrl))
                    {
                        // A prerequisite the panel can't install itself: the CTA only opens the
                        // instructions. Deliberately NOT routed through InstallPrerequisiteAsync —
                        // returning failure from there sets StepState.Error, which RefreshStepStates
                        // refuses to clear, and emits a false essential
                        // InstallCompleted{Success=false} event on every click.
                        var ctaBtn = new RLDSButton(
                            new ActionLinkDescription
                            {
                                Content = new GUIContent(installText),
                                Action = () => UnityEngine.Application.OpenURL(ctaUrl),
                                Id = "AIToolsPrerequisiteCtaUrl",
                                Origin = Origins.GuidedSetup,
                                OriginData = this
                            },
                            RLDSConstants.ButtonVariant.Secondary,
                            RLDSConstants.ButtonSize.Small)
                        {
                            // Same download glyph the Welcome card puts on its "Download" CTA.
                            LeftIcon = DownloadIcon
                        }.Build();
                        ctaBtn.style.alignSelf = Align.FlexStart;
                        ctaBtn.style.marginBottom = RLDSConstants.Spacing.SizeSM;
                        card.Add(ctaBtn);
                        break;
                    }

                    var installBtn = new UnityEngine.UIElements.Button(onInstall) { text = installText };
                    installBtn.AddToClassList(RLDSConstants.Button.SecondarySmall);
                    installBtn.style.alignSelf = Align.FlexStart;
                    installBtn.style.marginBottom = RLDSConstants.Spacing.SizeSM;
                    card.Add(installBtn);
                    break;

                case StepState.Processing:
                    AddConnectionStatus(card, installingText, StepState.Processing);
                    break;

                case StepState.Complete:
                    if (updateAvailable)
                    {
                        // Installed but stale vs the SDK copy: show only the Update action (no "installed"
                        // status) so the card reads as actionable. State stays Complete, so an outdated
                        // proxy never blocks later steps (only a missing proxy, which is Incomplete, does).
                        var updateBtn = new UnityEngine.UIElements.Button(onUpdate ?? onInstall)
                        {
                            text = updateText ?? AIToolsSetupStrings.Buttons.ReinstallMcpProxy
                        };
                        updateBtn.AddToClassList(RLDSConstants.Button.SecondarySmall);
                        updateBtn.style.alignSelf = Align.FlexStart;
                        updateBtn.style.marginBottom = RLDSConstants.Spacing.SizeSM;
                        card.Add(updateBtn);
                    }
                    else
                    {
                        AddConnectionStatus(card, installedText, StepState.Complete);
                    }
                    break;

                case StepState.Error:
                    AddConnectionStatus(card, error ?? notInstalledText, StepState.Error,
                        onRetry: onInstall);
                    break;
            }
        }

        private void AddColumnRunAndStatus(VisualElement card, bool installComplete,
            StepState state, string error, Action onSetup,
            string disabledTooltip)
        {
            // A known-not-installed agent can't run setup: show the "not found" message + Try Again
            // here (reusing the error UI) and skip the Run command / connection-status section.
            if (_model.SelectedServiceIsKnownNotInstalled)
            {
                AddConnectionStatus(
                    card,
                    _model.SelectedServiceInstallMessage,
                    StepState.Error,
                    onRetry: () => _ = _model.CheckSelectedServiceInstalledAsync(),
                    actionLabel: AIToolsSetupStrings.Buttons.TryAgain);
                return;
            }

            if (_model.CanAutoSetup() && state == StepState.Incomplete)
            {
                var runBtn = new UnityEngine.UIElements.Button(onSetup)
                {
                    text = AIToolsSetupStrings.Buttons.RunCommand
                };
                runBtn.AddToClassList(RLDSConstants.Button.SecondarySmall);
                runBtn.SetEnabled(installComplete);
                if (!installComplete && disabledTooltip != null)
                    runBtn.tooltip = disabledTooltip;
                runBtn.style.alignSelf = Align.FlexStart;
                runBtn.style.marginBottom = RLDSConstants.Spacing.SizeXS;
                card.Add(runBtn);
            }

            switch (state)
            {
                case StepState.Incomplete:
                    AddWaitingStatus(card);
                    break;

                case StepState.Processing:
                    AddConnectionStatus(card,
                        AIToolsSetupStrings.Processing.Connecting,
                        StepState.Processing);
                    break;

                case StepState.Complete:
                    AddConnectionStatus(card,
                        AIToolsSetupStrings.ConnectionStatus.ConnectionVerified,
                        StepState.Complete);
                    break;

                case StepState.Error:
                    AddConnectionStatus(card,
                        error ?? AIToolsSetupStrings.ConnectionStatus.AssistantNotConnected,
                        StepState.Error,
                        onRetry: onSetup);
                    break;
            }
        }

        private void AddCommandBlock(VisualElement card, string command, string helperText = null)
        {
            var codeBlockElement = new CodeBlockItem(command).Build();

            var copyBtn = codeBlockElement.Q<UnityEngine.UIElements.Button>(
                className: RLDSConstants.IconButton.Root);
            if (copyBtn != null)
            {
                copyBtn.style.display = DisplayStyle.Flex;
                codeBlockElement.RegisterCallback<MouseLeaveEvent>(
                    _ => copyBtn.style.display = DisplayStyle.Flex);
            }

            // Reserve a little space on the right so wrapped command text doesn't run under the
            // absolutely-positioned copy icon.
            var codeLabel = codeBlockElement.Q<Label>(className: RLDSConstants.CodeBlock.Label);
            if (codeLabel != null)
                codeLabel.style.paddingRight = RLDSConstants.Spacing.SizeSM;

            codeBlockElement.style.marginBottom = RLDSConstants.Spacing.Size2XS;
            card.Add(codeBlockElement);

            var helper = new Label(helperText ?? AIToolsSetupStrings.Columns.RunCommandHelper);
            helper.AddToClassList(RLDSConstants.Typography.Meta);
            helper.style.whiteSpace = WhiteSpace.Normal;
            helper.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            card.Add(helper);
        }

        // Inline documentation link (LinkLabel) rendered under a command/path block, used for
        // providers that can only be set up manually (e.g. OpenCode).
        private void AddDocsLink(VisualElement card, string text, string url)
        {
            var link = new LinkLabel(
                new UrlLinkDescription()
                {
                    Content = new GUIContent(text ?? url),
                    URL = url,
                    Origin = Origins.GuidedSetup,
                    OriginData = this,
                    Color = EditorGUIUtility.isProSkin
                        ? RLDSStyles.Colors.TextLink
                        : RLDSStyles.Colors.LightTextLink
                }).Build();
            link.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            card.Add(link);
        }

        private void AddWaitingStatus(VisualElement container)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            VisualElement leading;
            if (_model.IsVerifying)
            {
                // Actively checking for an existing connection (on load or after switching
                // assistants) — show a spinner instead of the idle dot.
                leading = new Spinner(RingSize.Size16, RingColor.Disabled).Build();
            }
            else
            {
                var dot = new VisualElement();
                dot.style.width = RLDSConstants.Spacing.SizeXS;
                dot.style.height = RLDSConstants.Spacing.SizeXS;
                dot.style.borderTopLeftRadius = RLDSConstants.Radius.Full;
                dot.style.borderTopRightRadius = RLDSConstants.Radius.Full;
                dot.style.borderBottomLeftRadius = RLDSConstants.Radius.Full;
                dot.style.borderBottomRightRadius = RLDSConstants.Radius.Full;
                dot.style.backgroundColor = RLDSStyles.Colors.IconSecondary;
                leading = dot;
            }
            leading.style.marginRight = RLDSConstants.Spacing.Size2XS;
            row.Add(leading);

            var label = new Label(AIToolsSetupStrings.ConnectionStatus.WaitingForConnection);
            label.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            row.Add(label);

            container.Add(row);
        }

        private static void AddCardDivider(VisualElement card)
        {
            var divider = new VisualElement();
            divider.AddToClassList(RLDSConstants.Divider.Base);
            divider.style.marginTop = RLDSConstants.Spacing.SizeSM;
            divider.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            card.Add(divider);
        }

        // --- Per-card device sub-steps (adb reverse for AgentBridge; forward + device setup for XR Operator) ---

        // Opens a card's headset section: a divider, then the heading and description shared by the
        // one-click ADB actions rendered under it.
        private static void AddDeviceSectionHeader(VisualElement card, string heading, string description)
        {
            AddCardDivider(card);

            var headingLabel = new Label(heading);
            headingLabel.AddToClassList(RLDSConstants.Typography.Body2SmallLabel);
            headingLabel.style.marginBottom = RLDSConstants.Spacing.Size2XS;
            card.Add(headingLabel);

            var desc = new Label(description);
            desc.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            card.Add(desc);
        }

        // Renders one one-click ADB action, matching the card's install/register fashion: what it does,
        // then a run button / spinner / success + rerun / error + retry. Pass a null description when the
        // section header already covers the action (a card with a single action).
        private void AddDeviceAction(VisualElement card, AIToolsSetupModel.DeviceAction action,
            string description, string buttonText, string runningText, Action onRun)
        {
            var group = new VisualElement();
            group.style.marginBottom = RLDSConstants.Spacing.SizeSM;

            if (description != null)
            {
                var desc = new Label(description);
                desc.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
                desc.style.whiteSpace = WhiteSpace.Normal;
                desc.style.marginBottom = RLDSConstants.Spacing.Size2XS;
                group.Add(desc);
            }

            switch (action.State)
            {
                case StepState.Incomplete:
                    var runBtn = new UnityEngine.UIElements.Button(onRun) { text = buttonText };
                    runBtn.AddToClassList(RLDSConstants.Button.SecondarySmall);
                    runBtn.style.alignSelf = Align.FlexStart;
                    group.Add(runBtn);
                    break;

                case StepState.Processing:
                    AddConnectionStatus(group, runningText, StepState.Processing);
                    break;

                case StepState.Complete:
                    AddConnectionStatus(group, action.Detail ?? buttonText, StepState.Complete);
                    AddSpacer(group, RLDSConstants.Spacing.Size2XS);
                    var rerunBtn = new UnityEngine.UIElements.Button(onRun)
                    {
                        text = AIToolsSetupStrings.Buttons.Rerun
                    };
                    rerunBtn.AddToClassList(RLDSConstants.Button.SecondarySmall);
                    rerunBtn.style.alignSelf = Align.FlexStart;
                    group.Add(rerunBtn);
                    break;

                case StepState.Error:
                    AddConnectionStatus(group,
                        action.Error ?? AIToolsSetupStrings.Device.NoDeviceDetected,
                        StepState.Error,
                        onRetry: onRun);
                    break;
            }

            card.Add(group);
        }

        // Fires a device action and surfaces any unhandled fault in the Console rather than losing it
        // to an unobserved task.
        private static Action RunDeviceAction(Func<System.Threading.Tasks.Task> start) => () =>
            _ = start().ContinueWith(t => Debug.LogException(t.Exception),
                System.Threading.Tasks.TaskContinuationOptions.OnlyOnFaulted);

        // Set up / undo sit side by side and stay put: unlike the single-action sub-steps, neither is
        // replaced by its status, so the pair never reflows and either action stays one click away. They
        // share the status line below, since only one of them can have run last.
        private void AddHeadsetControls(VisualElement card)
        {
            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginBottom = RLDSConstants.Spacing.Size2XS;

            var busy = _model.IsHeadsetActionRunning;

            var setUpBtn = new UnityEngine.UIElements.Button(
                RunDeviceAction(_model.SetUpHeadsetAsync))
            {
                text = AIToolsSetupStrings.Device.SetUpButton
            };
            setUpBtn.AddToClassList(RLDSConstants.Button.SecondarySmall);
            setUpBtn.SetEnabled(!busy);
            setUpBtn.style.marginRight = RLDSConstants.Spacing.Size2XS;
            buttons.Add(setUpBtn);

            var undoBtn = new UnityEngine.UIElements.Button(
                RunDeviceAction(_model.UndoHeadsetSetupAsync))
            {
                text = AIToolsSetupStrings.Device.UndoButton
            };
            undoBtn.AddToClassList(RLDSConstants.Button.SecondarySmall);
            undoBtn.SetEnabled(!busy);
            buttons.Add(undoBtn);

            card.Add(buttons);

            // Whichever action ran last owns the status line; at most one is ever non-Incomplete.
            var active = _model.HeadsetSetup.State != StepState.Incomplete
                ? _model.HeadsetSetup
                : _model.HeadsetUndo;
            if (active.State == StepState.Incomplete)
            {
                return;
            }

            var status = new VisualElement();
            status.style.marginBottom = RLDSConstants.Spacing.Size2XS;
            var processingText = ReferenceEquals(active, _model.HeadsetSetup)
                ? AIToolsSetupStrings.Device.SettingUp
                : AIToolsSetupStrings.Device.UndoingSetup;

            AddConnectionStatus(status,
                active.State switch
                {
                    StepState.Processing => processingText,
                    StepState.Complete => active.Detail ?? processingText,
                    _ => active.Error ?? AIToolsSetupStrings.Device.NoDeviceDetected,
                },
                active.State);

            card.Add(status);
        }

        // One compact row per headset setup step, so a developer can see which step is running and
        // which one failed without the card carrying four separate buttons.
        private void AddHeadsetSetupChecklist(VisualElement card)
        {
            AddHeadsetSetupStep(card, _model.AdbForward, string.Format(
                AIToolsSetupStrings.Device.ForwardItemFormat, AdbSetupUtility.MetaXROperatorPort));
            AddHeadsetSetupStep(card, _model.ExperimentalFeatures,
                AIToolsSetupStrings.Device.ExperimentalItem);
            AddHeadsetSetupStep(card, _model.CapturePermission,
                AIToolsSetupStrings.Device.CaptureItem);
            AddHeadsetSetupStep(card, _model.ProximitySensor,
                AIToolsSetupStrings.Device.ProximityItem);
        }

        private void AddHeadsetSetupStep(VisualElement card, AIToolsSetupModel.DeviceAction step,
            string label)
        {
            var row = new VisualElement();
            row.style.marginBottom = RLDSConstants.Spacing.Size2XS;

            switch (step.State)
            {
                case StepState.Processing:
                case StepState.Complete:
                    AddConnectionStatus(row, label, step.State);
                    break;

                case StepState.Error:
                    // Keep the step's own name alongside the reason: the card-level control carries the
                    // retry, so this row only has to say which step failed and why.
                    AddConnectionStatus(row,
                        string.Format(AIToolsSetupStrings.Device.ItemFailedFormat, label, step.Error),
                        StepState.Error);
                    break;

                default:
                    row.Add(BuildPendingRow(label));
                    break;
            }

            card.Add(row);
        }

        // Neutral "not run yet" row (grey dot + label), matching the idle dot in AddWaitingStatus.
        private static VisualElement BuildPendingRow(string text)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var dot = new VisualElement();
            dot.style.width = RLDSConstants.Spacing.SizeXS;
            dot.style.height = RLDSConstants.Spacing.SizeXS;
            dot.style.borderTopLeftRadius = RLDSConstants.Radius.Full;
            dot.style.borderTopRightRadius = RLDSConstants.Radius.Full;
            dot.style.borderBottomLeftRadius = RLDSConstants.Radius.Full;
            dot.style.borderBottomRightRadius = RLDSConstants.Radius.Full;
            dot.style.backgroundColor = RLDSStyles.Colors.IconSecondary;
            dot.style.flexShrink = 0;
            dot.style.marginRight = RLDSConstants.Spacing.Size2XS;
            row.Add(dot);

            var label = new Label(text);
            label.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexGrow = 1;
            row.Add(label);

            return row;
        }

        // --- Step 3 (Windows only): Allow local network connections (Windows Firewall) ---

        private void AddFirewallStep(VisualElement container)
        {
            AddStepTitle(container, AIToolsSetupStrings.Steps.ConfigureFirewall);

            var card = MakeProductCard(grow: false);

            var heading = new Label(AIToolsSetupStrings.SubSteps.ConfigureFirewall);
            heading.AddToClassList(RLDSConstants.Typography.Body2SmallLabel);
            heading.style.marginBottom = RLDSConstants.Spacing.Size2XS;
            card.Add(heading);

            var desc = new Label(AIToolsSetupStrings.Firewall.Description);
            desc.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            card.Add(desc);

            switch (_model.FirewallState)
            {
                case StepState.Incomplete:
                    var configureBtn = new UnityEngine.UIElements.Button(() => _ = _model.EnsureFirewallAsync())
                    {
                        text = AIToolsSetupStrings.Firewall.ConfigureButton
                    };
                    configureBtn.AddToClassList(RLDSConstants.Button.SecondarySmall);
                    configureBtn.style.alignSelf = Align.FlexStart;
                    card.Add(configureBtn);
                    break;

                case StepState.Processing:
                    AddConnectionStatus(card,
                        AIToolsSetupStrings.Firewall.Configuring,
                        StepState.Processing);
                    break;

                case StepState.Complete:
                    AddConnectionStatus(card,
                        AIToolsSetupStrings.Firewall.Configured,
                        StepState.Complete);
                    break;

                case StepState.Error:
                    AddConnectionStatus(card,
                        _model.FirewallError ?? AIToolsSetupStrings.Firewall.NotConfigured,
                        StepState.Error,
                        onRetry: () => _ = _model.EnsureFirewallAsync());
                    break;
            }

            container.Add(card);
            AddSpacer(container, RLDSConstants.Spacing.SizeLG);
        }

        // --- Step 3/4: Install project-level skills ---

        private void AddSkillsStep(VisualElement container)
        {
            VisualElement skills = null;
            foreach (var provider in AIToolsSetupRegistry.GetProvidersSorted())
            {
                skills = provider.BuildSkillsSection(BuildUI);
                if (skills != null)
                    break;
            }

            if (skills == null)
                return;

            AddStepTitle(container, AIToolsSetupStrings.Steps.InstallSkills);
            container.Add(skills);
            AddSpacer(container, RLDSConstants.Spacing.SizeSM);
        }

        // --- Final step: Review Project Setup Tool ---

        private void AddProjectSetupStep(VisualElement container)
        {
            AddStepTitle(container, AIToolsSetupStrings.Steps.ReviewProjectSetup);

            var card = MakeProductCard(grow: false);

            var desc = new Label(AIToolsSetupStrings.ProjectSetup.Description);
            desc.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            desc.style.whiteSpace = WhiteSpace.Normal;
            desc.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            card.Add(desc);

            // Only call out outstanding rules for this window's AI-tools features (e.g. Meta XR
            // Operator) — those are the rules this wizard is responsible for. Other Project Setup Tool
            // rules are the developer's to review via the button below, so we don't warn on them here.
            if (_model.HasOutstandingAIToolRules)
            {
                AddConnectionStatus(card,
                    string.Format(AIToolsSetupStrings.ProjectSetup.OutstandingAIToolWarningFormat,
                        _model.OutstandingAIToolRuleCount),
                    StepState.Error);
                AddSpacer(card, RLDSConstants.Spacing.SizeSM);
            }

            var openBtn = new UnityEngine.UIElements.Button(
                () => OVRProjectSetupSettingsProvider.OpenSettingsWindow(Origins.GuidedSetup))
            {
                text = AIToolsSetupStrings.ProjectSetup.OpenButton
            };
            openBtn.AddToClassList(RLDSConstants.Button.SecondarySmall);
            openBtn.style.alignSelf = Align.FlexStart;
            card.Add(openBtn);

            container.Add(card);
            AddSpacer(container, RLDSConstants.Spacing.SizeLG);
        }

        // Service IDs to hide from the wizard's selector even though they're registered
        // (e.g. test-only mocks that exist purely to validate 3P discovery in unit tests).
        private static readonly HashSet<string> HiddenServiceIds = new()
        {
            "mock-thirdparty",
        };

        private VisualElement MakeServiceSelector(bool enabled)
        {
            var services = AIServiceRegistry.GetAllServices()
                ?.Where(s => !HiddenServiceIds.Contains(s.Id))
                .ToArray();
            if (services == null || services.Length == 0)
            {
                return new VisualElement();
            }

            var items = new List<DropdownMenuItem>(services.Length);
            foreach (var service in services)
            {
                items.Add(new DropdownMenuItem
                {
                    Id = service.Id,
                    Label = service.DisplayName,
                });
            }

            var dropdown = new DropdownMenu(items, _model.SelectedServiceId, id =>
            {
                var previousId = _model.SelectedServiceId;
                _model.OnSelectedServiceChanged(id);
                AgentBridgeSettings.SetSelectedService(id, Origins.Menu);
                AIToolsSetupTelemetry.SendEvent(
                    AIToolsSetupTelemetryConstants.FalcoEventName.ServiceSelected,
                    evt =>
                    {
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.ServiceId, id);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.PreviousServiceId, previousId);
                        evt.SetMetadata(AIToolsSetupTelemetryConstants.AnnotationType.CanAutoRegister,
                            AIToolsSetupModel.ServiceSupportsAutoRegister(id));
                    },
                    isEssential: true);
                BuildUI();
                // Re-detect connection status for the newly selected assistant; the columns
                // show their default (Run command + checking spinner) until this completes.
                _ = _model.VerifyConnectionAsync(silent: true);
                // Re-check whether the newly selected assistant's CLI is installed so the XR Operator
                // Run/Status card reflects the new selection (not-found message + Try Again when
                // missing). Must run after the SetSelectedService call above, since detection
                // re-syncs off that persisted setting.
                _ = _model.CheckSelectedServiceInstalledAsync();
            });

            var element = dropdown.Build();
            element.style.alignSelf = Align.FlexStart;
            element.style.width = Length.Percent(50);
            element.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            element.SetEnabled(enabled);
            return element;
        }

        // --- Connection status component ---

        private void AddConnectionStatus(VisualElement container, string text, StepState state,
            Action onRetry = null, string actionLabel = null)
        {
            // Success and processing states render as borderless inline rows (no box, no buttons),
            // matching the "Connection verified" style.
            if (state == StepState.Complete)
            {
                container.Add(BuildInlineSuccessRow(text));
                return;
            }
            if (state == StepState.Processing)
            {
                container.Add(BuildInlineProcessingRow(text));
                return;
            }

            // Error state: borderless inline row (icon + wrapping label + retry). The message is
            // sanitized first because Win32/exception messages can contain stray carriage returns
            // or other line-break characters that Unity renders as blank lines, which inflate the
            // wrapping label's height and leave a large gap below the text.
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.FlexStart;

            var errorIcon = new VisualElement();
            errorIcon.AddToClassList(RLDSConstants.StatusNotice.Icon);
            errorIcon.style.backgroundImage =
                UserInterface.Styles.Contents.ErrorMaskIcon.Image as Texture2D;
            errorIcon.style.unityBackgroundImageTintColor = RLDSStyles.Colors.IconNegative;
            errorIcon.style.flexShrink = 0;
            errorIcon.style.marginRight = RLDSConstants.Spacing.Size2XS;
            row.Add(errorIcon);

            var label = new Label(SanitizeMessage(text));
            label.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.flexGrow = 1;
            label.style.flexShrink = 1;
            row.Add(label);

            if (onRetry != null)
            {
                var retryBtn = new UnityEngine.UIElements.Button(onRetry)
                {
                    text = actionLabel ?? AIToolsSetupStrings.Buttons.TryAgain
                };
                retryBtn.AddToClassList(RLDSConstants.Button.SecondarySmall);
                retryBtn.style.flexShrink = 0;
                retryBtn.style.marginLeft = RLDSConstants.Spacing.SizeSM;
                retryBtn.style.alignSelf = Align.Center;
                row.Add(retryBtn);
            }

            container.Add(row);
        }

        // Collapses any run of whitespace (spaces, tabs, and line breaks such as \r and \n) into a
        // single space and trims. Win32/exception messages often contain stray carriage returns
        // that Unity renders as blank lines, inflating a wrapping label's height.
        // Native error buffers can leave NUL padding (and other control characters) after the
        // actual text; Unity renders these as blank lines that inflate a wrapping label's height.
        // Strip control characters (except standard whitespace), then collapse whitespace runs into
        // single spaces and trim.
        private static string SanitizeMessage(string message)
        {
            if (string.IsNullOrEmpty(message))
                return message;
            var stripped = System.Text.RegularExpressions.Regex.Replace(
                message, @"[\x00-\x08\x0E-\x1F\x7F]", "");
            return System.Text.RegularExpressions.Regex.Replace(stripped, @"\s+", " ").Trim();
        }

        // Borderless inline success row (green check + label) used for all Complete states,
        // matching the skills list style.
        private static VisualElement BuildInlineSuccessRow(string text)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var icon = new VisualElement();
            icon.AddToClassList(RLDSConstants.StatusNotice.Icon);
            icon.style.backgroundImage =
                UserInterface.Styles.Contents.CheckMaskIcon.Image as Texture2D;
            icon.style.unityBackgroundImageTintColor = RLDSStyles.Colors.TextPositive;
            icon.style.marginRight = RLDSConstants.Spacing.Size2XS;
            row.Add(icon);

            var label = new Label(text);
            label.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            label.style.whiteSpace = WhiteSpace.Normal;
            row.Add(label);

            return row;
        }

        // Borderless inline processing row (spinner + label) used for all Processing states,
        // matching the inline success style.
        private static VisualElement BuildInlineProcessingRow(string text)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var spinner = new Spinner(RingSize.Size16, RingColor.Accent).Build();
            spinner.style.marginRight = RLDSConstants.Spacing.Size2XS;
            row.Add(spinner);

            var label = new Label(text);
            label.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            label.style.whiteSpace = WhiteSpace.Normal;
            row.Add(label);

            return row;
        }

        // --- Advanced settings ---

        private void AddAdvancedSettingsSection(VisualElement container)
        {
            var sectionTitle = new Label(AIToolsSetupStrings.AdvancedSettingsSection.Title);
            sectionTitle.AddToClassList(RLDSConstants.Typography.Heading3);
            sectionTitle.style.marginBottom = RLDSConstants.Spacing.SizeXS;
            container.Add(sectionTitle);

            var sectionDescription = new Label(AIToolsSetupStrings.Descriptions.AdvancedSettings);
            sectionDescription.AddToClassList(RLDSConstants.Typography.Body2SupportingText);
            sectionDescription.style.whiteSpace = WhiteSpace.Normal;
            sectionDescription.style.marginBottom = RLDSConstants.Spacing.SizeSM;
            container.Add(sectionDescription);

            var toggleButton = new UnityEngine.UIElements.Button(() =>
            {
                _model.ShowAdvancedSettings = !_model.ShowAdvancedSettings;
                BuildUI();
            })
            {
                text = _model.ShowAdvancedSettings
                    ? AIToolsSetupStrings.Buttons.AdvancedSettingsHide
                    : AIToolsSetupStrings.Buttons.AdvancedSettingsShow
            };
            toggleButton.AddToClassList(RLDSConstants.Button.SecondarySmall);
            toggleButton.style.alignSelf = Align.FlexStart;
            container.Add(toggleButton);

            if (!_model.ShowAdvancedSettings)
                return;

            var advancedContainer = new VisualElement();
            advancedContainer.style.marginTop = RLDSConstants.Spacing.SizeMD;
            container.Add(advancedContainer);

            // Provider advanced settings
            foreach (var provider in AIToolsSetupRegistry.GetProvidersSorted())
            {
                var section = provider.BuildAdvancedSettings(BuildUI);
                if (section != null)
                {
                    advancedContainer.Add(section);
                    AddDivider(advancedContainer);
                }
            }

            var openPrefsBtn = new UnityEngine.UIElements.Button(() =>
            {
                SettingsService.OpenUserPreferences(SettingsPath);
            })
            {
                text = "Open Preferences"
            };
            openPrefsBtn.AddToClassList(RLDSConstants.Button.TertiarySmall);
            openPrefsBtn.style.alignSelf = Align.FlexStart;
            advancedContainer.Add(openPrefsBtn);
        }

        // --- Utilities ---

        private static void AddDivider(VisualElement container)
        {
            var divider = new VisualElement();
            divider.AddToClassList(RLDSConstants.Divider.Base);
            divider.style.marginTop = RLDSConstants.Spacing.SizeMD;
            divider.style.marginBottom = RLDSConstants.Spacing.SizeMD;
            container.Add(divider);
        }

        private static void AddSpacer(VisualElement container, float height)
        {
            var spacer = new VisualElement();
            spacer.style.height = height;
            container.Add(spacer);
        }
    }
}
