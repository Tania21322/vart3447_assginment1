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
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Meta.XR.Editor.UserInterface.RLDS;
using Meta.XR.AI.AgentBridge;
using Meta.HandReadinessTool.Editor.UI;
using AgentBridgeSettings = Meta.XR.AI.AgentBridge.Settings;
using TelemetryConstants = Meta.HandReadinessTool.Editor.HandReadinessTelemetryConstants;
using Debug = UnityEngine.Debug;

namespace Meta.HandReadinessTool.Editor
{
    /// <summary>
    /// Main editor window for the Hand Readiness Tool.
    /// Helps developers migrate their Unity projects to hands-only compatibility.
    /// </summary>
    public class HandReadinessToolWindow : EditorWindow
    {
        private const int MinWidth = 1024;
        private const int MinHeight = 768;
        private const int StepIndicatorWidth = 540;

        // Step indicator total adapts to the chosen path: AI flow has the extra
        // ProjectDescription step, Standard flow does not. The final Confirmation
        // step adds one to both. Recalculated on every SetState() / RenderStepIndicator().
        private int TotalSteps => _useAI ? 6 : 5;

        private const string PrefKeyState = "HandReadiness_CurrentState";
        private const string PrefKeyIssues = "HandReadiness_Issues";
        private const string PrefKeyDescription = "HandReadiness_ProjectDescription";

        // Fallback provider label when AgentBridge is off or the active service
        // name can't be resolved, so status copy never renders a blank name.
        private const string DefaultAiProviderLabel = "Your AI agent";

        // Primary scanning-status line during the AI scan. Shared by the full render
        // and the in-place animator update so the two can't drift apart.
        private string AiAnalyzingStatus =>
            $"{_aiProviderLabel} is analyzing your project for device readiness opportunities...";

        private ToolState _currentState = ToolState.Welcome;
        // Default true so the step indicator shows the (longer) AI path on the
        // Welcome and CheckType screens, before the user has actually picked.
        // `_checkTypeChosen` flips to true once Next is clicked on CheckType.
        private bool _useAI = true;
        private bool _checkTypeChosen = false;
        private string _projectDescription = "";
        private VisualElement _contentContainer;
        private VisualElement _stepIndicator;
        private bool _hasOvrManagerInScene;

        // Scan results
        private List<OVRConfigurationTask> _failedTasks;
        private List<IssueData> _issues;
        private float _scanProgress = 0f;
        private bool _isScanComplete = false;
        private int _currentCheckIndex = 0;

        // AI scan state
        private bool _isAIScanInProgress = false;
        private bool _isCompilingReport = false;
        private float _aiScanStartTime = 0f;
        private string _lastAIError = null;
        private IVisualElementScheduledItem _aiProgressAnimator;

        // Identifies the in-flight AI scan; bumped on scan start and on leaving the
        // scanning screen so a superseded scan can drop its late async continuation.
        private int _aiScanGeneration;
        private string _aiStatusText = "";
        // Provider display name (e.g. "Claude Code") captured at scan start so the
        // per-tick status animator doesn't re-query AgentBridge each frame.
        private string _aiProviderLabel = DefaultAiProviderLabel;

        // Cross-scan snapshots: kept across the lifetime of a RunScan so the merge
        // step at the end can carry forward user-marked-complete state and treat
        // disappeared items as resolved.
        private List<IssueData> _priorAiIssues;
        private List<IssueData> _priorRegexIssues;

        // Telemetry — stopwatches per long-running op so cancellation paths can
        // still emit a `_completed` event with a real `duration_ms`. Mirrors
        // MetaXROperatorBuilder.cs:243-313 (`buildStopwatch` + try/finally).
        private Stopwatch _scanStopwatch;
        // Snapshot of the user's CheckType selection. Separate from `_useAI` so
        // ProjectDescription's AgentBridge gate (which forces `_useAI=false` when
        // the provider is off) can't clobber the user's stated intent — that
        // distinction is what `fell_back_to_standard` measures.
        private bool _userChoseAiOnCheckType = true;
        private string _scanIntendedScanType;
        private string _scanActualScanType;
        private bool _scanFellBackToStandard;
        private bool _scanIsRescan;
        private int _scanPriorIssueCount;
        private int _scanSetupTaskCount;
        // Tracks the AI subscan outcome so the final `hrt_scan_completed` event
        // can carry `ai_subscan_success` and a specific `error_kind`. Null means
        // AI was never attempted on this scan.
        private bool? _aiSubscanSuccess;
        private string _aiErrorKind;
        private int _aiIssuesResolvedFromPrior;
        private DateTime _windowOpenedAt;

        // Updating state
        // Update state is tracked via ToolState.Updating

        /// <summary>
        /// Opens the Hand Readiness Tool editor window.
        /// </summary>
        public static void ShowWindow()
        {
            // Open as a normal (non-utility) window so it can be docked, sent behind
            // the Unity Editor, and minimized while a scan runs — a floating utility
            // window (utility: true) stays always-on-top and can't be tucked away.
            var window = GetWindow<HandReadinessToolWindow>(false, HandReadinessConstants.ToolTitle);
            window.minSize = new Vector2(MinWidth, MinHeight);
            window.Show();
        }

        private void OnEnable()
        {
            // Subscribe to task processor completion events to update UI when tasks change
            OVRProjectSetup.ProcessorQueue.OnProcessorCompleted += OnProcessorCompleted;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            // Re-render when the user toggles the AgentBridge master setting from
            // Preferences. The screen swaps between the normal checkbox + active-
            // provider row (enabled) and the ai-upsell-unit (disabled) without
            // needing a manual refresh or window reopen.
            AgentBridgeSettings.Activated += OnAgentBridgeToggleChanged;
            AgentBridgeSettings.Deactivated += OnAgentBridgeToggleChanged;

            _windowOpenedAt = DateTime.UtcNow;

            // Telemetry — emit `hrt_opened` only on a freshly minted session.
            // If EnsureSessionId returns false, we were just re-entering after a
            // domain reload (session_id was persisted in EditorPrefs).
            bool freshSession = HandReadinessTelemetry.EnsureSessionId();
            if (freshSession)
            {
                HandReadinessTelemetry.SendEvent(
                    TelemetryConstants.FalcoEventName.Opened,
                    evt => evt.SetMetadata(
                        TelemetryConstants.AnnotationType.EntryPoint,
                        TelemetryConstants.EntryPoint.Menu),
                    isEssential: true);
            }
        }

        private void OnDisable()
        {
            OVRProjectSetup.ProcessorQueue.OnProcessorCompleted -= OnProcessorCompleted;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            AgentBridgeSettings.Activated -= OnAgentBridgeToggleChanged;
            AgentBridgeSettings.Deactivated -= OnAgentBridgeToggleChanged;

            // If a scan was in flight when the window went away, close it out as a
            // cancellation so funnel math stays clean.
            if (_scanStopwatch != null && _scanStopwatch.IsRunning)
            {
                EmitScanCompleted(
                    success: false,
                    errorKind: TelemetryConstants.ErrorKind.Cancelled,
                    errorMessage: null,
                    cancelReason: TelemetryConstants.CancelReason.WindowClose);
            }

            // Emit close event before clearing state. Marked-complete events are
            // counted separately by session_id, so no local aggregate is needed.
            HandReadinessTelemetry.SendEvent(
                TelemetryConstants.FalcoEventName.Closed,
                evt =>
                {
                    evt.SetMetadata(
                        TelemetryConstants.AnnotationType.DurationMs,
                        (long)(DateTime.UtcNow - _windowOpenedAt).TotalMilliseconds);
                    evt.SetMetadata(
                        TelemetryConstants.AnnotationType.LastScreen,
                        ScreenNameForState(_currentState));
                    evt.SetMetadata(
                        TelemetryConstants.AnnotationType.CompletedScan,
                        _isScanComplete);
                });

            // Cancel any in-flight AI scan so the AgentBridge op doesn't keep
            // running after the window is gone.
            if (_isAIScanInProgress)
            {
                _aiScanGeneration++;
                _aiProgressAnimator?.Pause();
                _aiProgressAnimator = null;
                _isAIScanInProgress = false;
                StopAIProgressTracking();
                _ = AgentBridgeAPI.CancelCurrentOperationAsync(
                    new CallerIdentity(HandReadinessConstants.ToolIdentifier));
            }

            // Close the detached "View Details" popup with the main window — it's a separate
            // EditorWindow that otherwise lingers after the parent closes. HasOpenInstances
            // guards against spawning one here.
            if (HasOpenInstances<IssueDetailsPopup>())
            {
                GetWindow<IssueDetailsPopup>().Close();
            }

            // Reset persisted session id so the next open starts a new session.
            HandReadinessTelemetry.ResetSession();

            // Reset state so the window starts fresh next time
            EditorPrefs.DeleteKey(PrefKeyState);
            EditorPrefs.DeleteKey(PrefKeyIssues);
            EditorPrefs.DeleteKey(PrefKeyDescription);
        }

        private static string ScreenNameForState(ToolState state) => state switch
        {
            ToolState.Welcome => TelemetryConstants.ScreenName.Welcome,
            ToolState.CheckType => TelemetryConstants.ScreenName.CheckType,
            ToolState.ProjectDescription => TelemetryConstants.ScreenName.ProjectDescription,
            ToolState.Scanning => TelemetryConstants.ScreenName.Scanning,
            ToolState.Results => TelemetryConstants.ScreenName.Results,
            ToolState.Confirmation => TelemetryConstants.ScreenName.Confirmation,
            ToolState.TodoList => TelemetryConstants.ScreenName.Results,
            ToolState.Updating => TelemetryConstants.ScreenName.Results,
            _ => TelemetryConstants.ScreenName.Welcome,
        };

        private static string PriorityName(IssuePriority p) => p switch
        {
            IssuePriority.High => TelemetryConstants.Priority.High,
            IssuePriority.Medium => TelemetryConstants.Priority.Medium,
            _ => TelemetryConstants.Priority.Low,
        };

        private static string CategoryName(IssueCategory c) => c switch
        {
            IssueCategory.AI => TelemetryConstants.IssueCategoryName.Ai,
            IssueCategory.Manual => TelemetryConstants.IssueCategoryName.Manual,
            _ => TelemetryConstants.IssueCategoryName.Automation,
        };

        private void OnAgentBridgeToggleChanged()
        {
            if (_contentContainer == null) return;
            RenderCurrentState();
            Repaint();
        }

        private void OnProcessorCompleted(OVRConfigurationTaskProcessor processor)
        {
            // Only refresh on the results screen, and only when at least one issue's
            // IsFixed state actually changed. OVRProjectSetup fires this event on every
            // background queue pass (many times per second during asset work), so an
            // unconditional rebuild competes with scroll frames and causes jitter.
            if (_currentState != ToolState.Results || _issues == null) return;

            var buildTarget = GetCurrentBuildTarget();
            bool changed = false;
            foreach (var issue in _issues)
            {
                if (issue.IsFixed) continue;
                // Automated items that weren't already satisfied at scan time must be
                // resolved by an explicit "Apply" in the tool, not auto-marked done when
                // the underlying setting changes elsewhere.
                if (issue.Category == IssueCategory.Automation) continue;
                var task = _failedTasks?.FirstOrDefault(t => t.Uid.ToString() == issue.TaskUid);
                if (task != null && task.IsDone(buildTarget))
                {
                    issue.IsFixed = true;
                    changed = true;
                }
            }
            if (changed) RenderCurrentState();
        }

        private void CreateGUI()
        {
            var root = rootVisualElement;

            // Load RLDS + Hand Readiness stylesheets. Idempotent so domain-reload re-entry of
            // CreateGUI does not accumulate duplicate sheets on root.
            var lightMode = !EditorGUIUtility.isProSkin;
            var styleSheet = RLDSUtils.LoadStyleSheet(lightMode);
            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            var hrtStyleSheet = UI.HandReadinessStyles.LoadStyleSheet();
            if (hrtStyleSheet != null && !root.styleSheets.Contains(hrtStyleSheet))
            {
                root.styleSheets.Add(hrtStyleSheet);
            }

            // Match the window surface; nested cards use the secondary surface.
            root.AddToClassList(RLDSConstants.Surface.Primary);
            root.style.flexDirection = FlexDirection.Column;

            // Step indicator
            _stepIndicator = CreateStepIndicator();
            root.Add(_stepIndicator);

            // Create content container
            _contentContainer = new VisualElement();
            _contentContainer.style.flexGrow = 1;
            root.Add(_contentContainer);

            // Restore state after domain reload if we were on a post-scan screen
            RestoreStateFromPrefs();

            // Render initial state
            UpdateStepIndicator();
            RenderCurrentState();
        }

        private VisualElement CreateStepIndicator()
        {
            var container = new VisualElement();
            container.AddToClassList(RLDSConstants.ProgressStepper.Container);
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.Center;
            container.style.alignSelf = Align.Center;
            container.style.width = StepIndicatorWidth;
            container.style.paddingTop = RLDSConstants.Spacing.SizeXL;
            container.style.paddingBottom = 0;

            int activeStep = GetStepIndex(_currentState);

            for (int i = 0; i < TotalSteps; i++)
            {
                var segment = new VisualElement();
                segment.name = $"step-segment-{i}";
                segment.AddToClassList(RLDSConstants.ProgressStepper.Step);
                if (i <= activeStep)
                {
                    segment.AddToClassList(i == activeStep
                        ? RLDSConstants.ProgressStepper.StepCurrent
                        : RLDSConstants.ProgressStepper.StepCompleted);
                }
                if (i < TotalSteps - 1)
                {
                    segment.style.marginRight = RLDSConstants.Spacing.SizeXS;
                }

                container.Add(segment);
            }

            return container;
        }

        private void UpdateStepIndicator()
        {
            if (_stepIndicator == null) return;

            int total = TotalSteps;

            // Rebuild segments when the count changes (e.g. user picks Standard
            // and the flow drops from 5 to 4 steps). `TotalSteps` flips on the
            // CheckType → Scanning transition; without this, the trailing
            // segment would stay in the DOM and remain visually "inactive".
            if (_stepIndicator.childCount != total)
            {
                _stepIndicator.Clear();
                for (int i = 0; i < total; i++)
                {
                    var segment = new VisualElement();
                    segment.name = $"step-segment-{i}";
                    segment.AddToClassList(RLDSConstants.ProgressStepper.Step);
                    if (i < total - 1)
                    {
                        segment.style.marginRight = RLDSConstants.Spacing.SizeXS;
                    }
                    _stepIndicator.Add(segment);
                }
            }

            int activeStep = GetStepIndex(_currentState);

            for (int i = 0; i < total; i++)
            {
                var segment = _stepIndicator.Q<VisualElement>($"step-segment-{i}");
                if (segment == null) continue;

                segment.RemoveFromClassList(RLDSConstants.ProgressStepper.StepCompleted);
                segment.RemoveFromClassList(RLDSConstants.ProgressStepper.StepCurrent);
                if (i <= activeStep)
                {
                    segment.AddToClassList(i == activeStep
                        ? RLDSConstants.ProgressStepper.StepCurrent
                        : RLDSConstants.ProgressStepper.StepCompleted);
                }
            }
        }

        private int GetStepIndex(ToolState state)
        {
            // Step layout differs between AI and Standard flows. Standard skips
            // ProjectDescription, so its Scanning/Results step indices shift down.
            // Confirmation is the terminal step in both flows.
            if (_useAI)
            {
                return state switch
                {
                    ToolState.Welcome => 0,
                    ToolState.CheckType => 1,
                    ToolState.ProjectDescription => 2,
                    ToolState.Scanning => 3,
                    ToolState.Results => 4,
                    ToolState.Confirmation => 5,
                    ToolState.TodoList => 4,
                    ToolState.Updating => 4,
                    _ => 0
                };
            }

            return state switch
            {
                ToolState.Welcome => 0,
                ToolState.CheckType => 1,
                ToolState.Scanning => 2,
                ToolState.Results => 3,
                ToolState.Confirmation => 4,
                ToolState.TodoList => 3,
                ToolState.Updating => 3,
                _ => 0
            };
        }

        private BuildTargetGroup GetCurrentBuildTarget()
        {
            return EditorUserBuildSettings.selectedBuildTargetGroup;
        }

        /// <summary>
        /// Emit `hrt_scan_completed` and stop the scan stopwatch. Idempotent —
        /// subsequent calls in the same scan (e.g. from OnDisable after a
        /// terminal path already emitted) are no-ops.
        /// </summary>
        private void EmitScanCompleted(
            bool success,
            string errorKind,
            string errorMessage,
            string cancelReason = null)
        {
            if (_scanStopwatch == null || !_scanStopwatch.IsRunning) return;

            _scanStopwatch.Stop();
            var durationMs = _scanStopwatch.ElapsedMilliseconds;
            _scanStopwatch = null;

            int issuesFromSetupTasks = _issues?.Count(i => i.Category == IssueCategory.Automation) ?? 0;
            int issuesFromRegex = _issues?.Count(i => i.Category == IssueCategory.Manual) ?? 0;
            int issuesFromAi = _issues?.Count(i => i.Category == IssueCategory.AI) ?? 0;
            int issuesHigh = _issues?.Count(i => !i.IsFixed && i.Priority == IssuePriority.High) ?? 0;
            int issuesMed = _issues?.Count(i => !i.IsFixed && i.Priority == IssuePriority.Medium) ?? 0;
            int issuesLow = _issues?.Count(i => !i.IsFixed && i.Priority == IssuePriority.Low) ?? 0;
            int issuesTotal = _issues?.Count ?? 0;

            HandReadinessTelemetry.SendEvent(
                TelemetryConstants.FalcoEventName.ScanCompleted,
                evt =>
                {
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IntendedScanType, _scanIntendedScanType ?? TelemetryConstants.ScanType.Standard);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.ActualScanType, _scanActualScanType ?? TelemetryConstants.ScanType.Standard);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.FellBackToStandard, _scanFellBackToStandard);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IsRescan, _scanIsRescan);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.Success, success);
                    if (_aiSubscanSuccess.HasValue)
                    {
                        evt.SetMetadata(TelemetryConstants.AnnotationType.AiSubscanSuccess, _aiSubscanSuccess.Value);
                    }
                    evt.SetMetadata(TelemetryConstants.AnnotationType.DurationMs, durationMs);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssuesFoundTotal, issuesTotal);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssuesHigh, issuesHigh);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssuesMedium, issuesMed);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssuesLow, issuesLow);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssuesFromSetupTasks, issuesFromSetupTasks);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssuesFromRegex, issuesFromRegex);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssuesFromAi, issuesFromAi);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssuesResolvedFromPrior, _aiIssuesResolvedFromPrior);

                    if (!string.IsNullOrEmpty(errorKind))
                    {
                        evt.SetMetadata(TelemetryConstants.AnnotationType.ErrorKind, errorKind);
                    }
                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        evt.SetMetadata(TelemetryConstants.AnnotationType.ErrorMessage, errorMessage);
                    }
                    if (!string.IsNullOrEmpty(cancelReason))
                    {
                        evt.SetMetadata(TelemetryConstants.AnnotationType.CancelReason, cancelReason);
                        evt.SetMetadata(
                            TelemetryConstants.AnnotationType.CancelledFromScreen,
                            ScreenNameForState(_currentState));
                    }
                },
                isEssential: true);
        }

        private void RunScan()
        {
            // Running the scan is the strongest signal that this developer cares
            // about hand readiness for this project — escalates the UPST hand-
            // tracking task from Recommended to Required from now on.
            HandReadinessSetupTasks.MarkUserOptedIn();

            // Capture the provider name once so the scanning copy can read
            // "Claude Code is analyzing…" without re-querying AgentBridge per tick.
            _aiProviderLabel = GetAiProviderLabel();

            _failedTasks = new List<OVRConfigurationTask>();
            // Snapshot AI + regex items from the previous scan so the merge step at
            // the end can keep user-marked-complete state and fold in resolved items.
            // Setup-task items don't need this — OVRProjectSetup.IsDone() is the
            // ground truth and we re-evaluate it fresh every scan.
            var prior = _issues ?? new List<IssueData>();
            _priorAiIssues = prior.Where(i => i.Category == IssueCategory.AI).ToList();
            _priorRegexIssues = prior.Where(i => i.Category == IssueCategory.Manual).ToList();
            _issues = new List<IssueData>();
            _scanProgress = 0f;
            _isScanComplete = false;
            _currentCheckIndex = 0;

            // Get hand-readiness tasks from OVRProjectSetup
            var buildTarget = GetCurrentBuildTarget();
            var allTasks = OVRProjectSetup.GetTasks(buildTarget)
                .Where(task => HandReadinessSetupTasks.IsHandReadinessTask(task, buildTarget))
                .ToList();

            // Telemetry — capture scan-intent for both `hrt_scan_started` and the
            // matching `hrt_scan_completed`. Intent comes from the CheckType
            // snapshot (`_userChoseAiOnCheckType`) so ProjectDescription's
            // AgentBridge gate flipping `_useAI=false` doesn't erase what the
            // user originally picked. When CheckType was never visited (e.g.
            // restored state from a prior session), fall back to `_useAI`.
            bool intendedAi = _checkTypeChosen ? _userChoseAiOnCheckType : _useAI;
            _scanIntendedScanType = intendedAi
                ? TelemetryConstants.ScanType.Ai
                : TelemetryConstants.ScanType.Standard;
            // After ProjectDescription forces `_useAI = false` when AgentBridge is
            // off, the actual run is Standard regardless of the user's pick.
            _scanActualScanType = _useAI ? TelemetryConstants.ScanType.Ai : TelemetryConstants.ScanType.Standard;
            _scanFellBackToStandard = _scanIntendedScanType == TelemetryConstants.ScanType.Ai
                && _scanActualScanType == TelemetryConstants.ScanType.Standard;
            _scanIsRescan = (_priorAiIssues?.Count ?? 0) > 0 || (_priorRegexIssues?.Count ?? 0) > 0;
            _scanPriorIssueCount = (_priorAiIssues?.Count ?? 0) + (_priorRegexIssues?.Count ?? 0);
            _scanSetupTaskCount = allTasks.Count;
            _aiSubscanSuccess = null;
            _aiErrorKind = null;
            _aiIssuesResolvedFromPrior = 0;
            // Clear any AI error from a prior scan so a stale banner doesn't carry over. The
            // Standard flow never enters StartAIScan (where the other reset lives), so reset here too.
            _lastAIError = null;
            _scanStopwatch = Stopwatch.StartNew();

            HandReadinessTelemetry.SendEvent(
                TelemetryConstants.FalcoEventName.ScanStarted,
                evt =>
                {
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IntendedScanType, _scanIntendedScanType);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.ActualScanType, _scanActualScanType);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.FellBackToStandard, _scanFellBackToStandard);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IsRescan, _scanIsRescan);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.PriorIssueCount, _scanPriorIssueCount);
                    evt.SetMetadata(TelemetryConstants.AnnotationType.SetupTaskCount, _scanSetupTaskCount);
                },
                isEssential: true);

            if (allTasks.Count == 0)
            {
                // No hand-readiness config issues, but the AI/regex code scan can still surface
                // patterns in user code that need migration — skip straight to that step.
                rootVisualElement.schedule.Execute(() =>
                {
                    if (_currentState != ToolState.Scanning) return;

                    if (_useAI)
                    {
                        _ = StartAIScan();
                    }
                    else
                    {
                        RunCodeScan();
                        _isScanComplete = true;
                        EmitScanCompleted(success: true, errorKind: null, errorMessage: null);
                        RenderCurrentState();
                    }
                }).StartingIn(500);
                return;
            }

            // Schedule incremental check execution for visual feedback
            rootVisualElement.schedule.Execute(() => RunNextCheck(allTasks)).Every(100);
        }

        private void RunNextCheck(List<OVRConfigurationTask> tasks)
        {
            if (_currentState != ToolState.Scanning || _currentCheckIndex >= tasks.Count)
            {
                return;
            }

            var buildTarget = GetCurrentBuildTarget();
            var task = tasks[_currentCheckIndex];

            // Check if task is done (passed)
            bool isDone = task.IsDone(buildTarget);

            // Only surface automated checks that need action. A task already satisfied at scan
            // time wasn't applied by this tool, so listing it as a completed row reads as
            // "auto-applied without clicking". Items the user applies flip to IsFixed and stay
            // visible as feedback; a re-scan drops them once satisfied.
            if (!isDone)
            {
                _failedTasks.Add(task);

                _issues.Add(new IssueData
                {
                    Title = GetTaskTitle(task, buildTarget),
                    Description = task.Message.GetValue(buildTarget),
                    Priority = ConvertTaskLevel(task.Level.GetValue(buildTarget)),
                    RequiresAI = false,
                    IsFixed = false,
                    TaskUid = task.Uid.ToString(),
                    Category = IssueCategory.Automation
                });
            }

            _currentCheckIndex++;
            // Checks fill 0-50% if AI is enabled, 0-100% if not
            float checkPortion = _useAI ? 0.5f : 1f;
            _scanProgress = ((float)_currentCheckIndex / tasks.Count) * checkPortion;

            // Update scanning screen
            RenderScanningScreen();

            // Check if we're done
            if (_currentCheckIndex >= tasks.Count)
            {
                // Small delay before showing results or starting AI
                rootVisualElement.schedule.Execute(() =>
                {
                    if (_currentState == ToolState.Scanning)
                    {
                        if (_useAI)
                        {
                            _ = StartAIScan();
                        }
                        else
                        {
                            RunCodeScan();
                            _isScanComplete = true;
                            EmitScanCompleted(success: true, errorKind: null, errorMessage: null);
                            RenderCurrentState();
                        }
                    }
                }).StartingIn(500);
            }
        }

        /// <summary>
        /// Heuristic code-pattern scan that runs when the user opts out of the AI flow.
        /// Walks `Application.dataPath` (the project's Assets folder) for `*.cs` files,
        /// applies the rules in <see cref="HandReadinessCodeScanRules"/>, and appends one
        /// IssueData per (rule × file) match to the running issue list.
        /// </summary>
        private void RunCodeScan()
        {
            var prior = _priorRegexIssues ?? new List<IssueData>();
            _priorRegexIssues = null;

            try
            {
                var fresh = HandReadinessCodeScanRules.Scan(Application.dataPath);
                _issues.AddRange(MergeRegexIssues(prior, fresh));
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HRT] Code scan failed: {ex.Message} — keeping prior regex items");
                _issues.AddRange(prior);
            }
        }

        /// <summary>
        /// Merge AI items across scans. Each fresh suggestion may carry a `PreviousTaskUid`
        /// pointing back at a prior item; we use that to inherit the user's `IsFixed` state.
        /// Prior items the AI explicitly flagged in `resolvedFromPrior` are folded in as
        /// IsFixed=true. Prior items that the AI didn't address at all are carried forward
        /// as-is — that preserves user mark-complete and avoids silently dropping work.
        /// </summary>
        private static List<IssueData> MergeAiIssues(
            List<IssueData> prior,
            List<IssueData> fresh,
            List<string> resolvedFromPrior)
        {
            var priorByUid = prior
                .Where(p => !string.IsNullOrEmpty(p.TaskUid))
                .GroupBy(p => p.TaskUid)
                .ToDictionary(g => g.Key, g => g.First());
            var resolvedSet = new HashSet<string>(resolvedFromPrior ?? new List<string>());
            var consumedPriorUids = new HashSet<string>();
            var result = new List<IssueData>();

            foreach (var freshItem in fresh)
            {
                if (!string.IsNullOrEmpty(freshItem.PreviousTaskUid)
                    && priorByUid.TryGetValue(freshItem.PreviousTaskUid, out var match))
                {
                    consumedPriorUids.Add(match.TaskUid);
                    // Inherit prior IsFixed; AI's resolvedFromPrior overrides only upward.
                    freshItem.IsFixed = match.IsFixed || resolvedSet.Contains(match.TaskUid);
                }
                result.Add(freshItem);
            }

            foreach (var priorItem in prior)
            {
                if (string.IsNullOrEmpty(priorItem.TaskUid)) continue;
                if (consumedPriorUids.Contains(priorItem.TaskUid)) continue;
                if (resolvedSet.Contains(priorItem.TaskUid))
                {
                    priorItem.IsFixed = true;
                }
                // Carry forward — either now resolved per AI, or still pending /
                // user-marked-complete. Either way the UI should keep it.
                result.Add(priorItem);
            }

            return result;
        }

        /// <summary>
        /// Merge regex items across scans. Stable UID is `hrt-code-scan:{rule}:{file}`
        /// (built by `HandReadinessCodeScanRules`) so matching is exact. Items that disappear
        /// from the fresh scan are treated as resolved (the underlying code no longer
        /// matches the rule); items still present inherit their prior IsFixed state.
        /// </summary>
        private static List<IssueData> MergeRegexIssues(List<IssueData> prior, List<IssueData> fresh)
        {
            var priorByUid = prior
                .Where(p => !string.IsNullOrEmpty(p.TaskUid))
                .GroupBy(p => p.TaskUid)
                .ToDictionary(g => g.Key, g => g.First());
            var freshUids = new HashSet<string>(fresh
                .Where(f => !string.IsNullOrEmpty(f.TaskUid))
                .Select(f => f.TaskUid));
            var result = new List<IssueData>();

            foreach (var freshItem in fresh)
            {
                if (!string.IsNullOrEmpty(freshItem.TaskUid)
                    && priorByUid.TryGetValue(freshItem.TaskUid, out var match))
                {
                    freshItem.IsFixed = match.IsFixed;
                }
                result.Add(freshItem);
            }

            foreach (var priorItem in prior)
            {
                if (string.IsNullOrEmpty(priorItem.TaskUid)) continue;
                if (freshUids.Contains(priorItem.TaskUid)) continue;
                priorItem.IsFixed = true;
                result.Add(priorItem);
            }

            return result;
        }

        private string GetTaskTitle(OVRConfigurationTask task, BuildTargetGroup buildTarget)
        {
            var message = task.Message.GetValue(buildTarget) ?? "Unknown Issue";

            var periodIndex = message.IndexOf('.');
            if (periodIndex > 0 && periodIndex < 60)
            {
                return message.Substring(0, periodIndex);
            }
            return message.Length > 60 ? message.Substring(0, 57) + "..." : message;
        }

        private IssuePriority ConvertTaskLevel(OVRProjectSetup.TaskLevel level)
        {
            return level switch
            {
                OVRProjectSetup.TaskLevel.Required => IssuePriority.High,
                OVRProjectSetup.TaskLevel.Recommended => IssuePriority.Medium,
                OVRProjectSetup.TaskLevel.Optional => IssuePriority.Low,
                _ => IssuePriority.Medium
            };
        }

        private void SetState(ToolState newState)
        {
            var fromState = _currentState;
            _currentState = newState;
            UpdateStepIndicator();
            SaveStateToPrefs();
            RenderCurrentState();

            // Ping the developer when a (long) AI scan finishes, in case they minimized
            // or parked this window while it ran.
            if (fromState == ToolState.Scanning && newState == ToolState.Results && _useAI)
            {
                NotifyScanFinished();
            }

            // Funnel — one event per state entry so screen-by-screen drop-off
            // is queryable directly. Skip when nothing actually changed.
            if (fromState != newState)
            {
                HandReadinessTelemetry.SendEvent(
                    TelemetryConstants.FalcoEventName.ScreenViewed,
                    evt =>
                    {
                        evt.SetMetadata(
                            TelemetryConstants.AnnotationType.Screen,
                            ScreenNameForState(newState));
                        evt.SetMetadata(
                            TelemetryConstants.AnnotationType.FromScreen,
                            ScreenNameForState(fromState));
                        evt.SetMetadata(
                            TelemetryConstants.AnnotationType.IsRescan,
                            _scanIsRescan);
                    });
            }
        }

        // Kept so the scan-finished toast can be dismissed when the tool tab closes or the
        // user clicks through to the results.
        private Meta.XR.Editor.Notifications.Notification _scanFinishedNotification;

        // Fire a Meta XR editor notification (core-SDK toast) when the AI scan finishes,
        // so a developer who minimized or backgrounded the window gets pinged. The CTA
        // focuses the window and jumps to the results, then dismisses the toast.
        private void NotifyScanFinished()
        {
            try
            {
                var window = this;
                Meta.XR.Editor.Notifications.Notification notification = null;
                notification =
                    new Meta.XR.Editor.Notifications.Notification("hrt_scan_finished")
                    {
                        Items = new Meta.XR.Editor.UserInterface.IUserInterfaceItem[]
                        {
                            new Meta.XR.Editor.UserInterface.Label(
                                "Device Readiness Check analysis finished",
                                EditorStyles.boldLabel),
                            new Meta.XR.Editor.UserInterface.GroupedItem(
                                new Meta.XR.Editor.UserInterface.IUserInterfaceItem[]
                                {
                                    new Meta.XR.Editor.UserInterface.AddSpace(flexibleSpace: true),
                                    new Meta.XR.Editor.UserInterface.Button(
                                        new Meta.XR.Editor.UserInterface.ActionLinkDescription
                                        {
                                            Content = new GUIContent("Click to view results"),
                                            Action = () =>
                                            {
                                                if (window != null)
                                                {
                                                    window.Focus();
                                                    window.SetState(ToolState.Results);
                                                }
                                                // Dismiss once acted on (or if the window is
                                                // already gone) so the toast never lingers.
                                                try { notification?.Remove(Meta.XR.Editor.Id.Origins.Self); }
                                                catch { }
                                            },
                                        }),
                                }),
                        },
                        ShowCloseButton = true,
                        // Narrower than the 512px default; the two short lines don't need it.
                        ExpectedWidth = 400,
                    };
                // Dismiss any previously-enqueued toast before tracking the new one so at most
                // one scan-finished toast is ever active (e.g. re-scanning without acting on the
                // first toast would otherwise leave the earlier one orphaned).
                try { _scanFinishedNotification?.Remove(Meta.XR.Editor.Id.Origins.Self); }
                catch { }
                // Track the new toast only after Enqueue succeeds, so a throw here can't leave
                // _scanFinishedNotification pointing at a never-shown toast after the previous
                // valid one was already removed.
                notification.Enqueue(Meta.XR.Editor.Id.Origins.Component);
                _scanFinishedNotification = notification;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HRT] Could not show completion notification: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            // Clear the scan-finished toast when the tool tab is closed so a stale toast
            // pointing at a gone window doesn't linger (its CTA would otherwise no-op).
            try { _scanFinishedNotification?.Remove(Meta.XR.Editor.Id.Origins.Self); }
            catch { }
            _scanFinishedNotification = null;
        }

        private void SaveStateToPrefs()
        {
            EditorPrefs.SetInt(PrefKeyState, (int)_currentState);
            EditorPrefs.SetString(PrefKeyDescription, _projectDescription ?? "");

            if (_issues != null && _issues.Count > 0)
            {
                try
                {
                    var issuesJson = JsonUtility.ToJson(new IssueDataListWrapper { Items = _issues });
                    EditorPrefs.SetString(PrefKeyIssues, issuesJson);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[HRT] Failed to save issues: {ex.Message}");
                }
            }
        }

        private void RestoreStateFromPrefs()
        {
            if (!EditorPrefs.HasKey(PrefKeyState)) return;

            var savedState = (ToolState)EditorPrefs.GetInt(PrefKeyState, 0);

            // Only restore post-scan states (Results, TodoList, Updating)
            // Earlier states are cheap to redo
            if (savedState < ToolState.Results) return;

            _projectDescription = EditorPrefs.GetString(PrefKeyDescription, "");

            var issuesJson = EditorPrefs.GetString(PrefKeyIssues, "");
            if (!string.IsNullOrEmpty(issuesJson))
            {
                try
                {
                    var wrapper = JsonUtility.FromJson<IssueDataListWrapper>(issuesJson);
                    _issues = wrapper?.Items ?? new List<IssueData>();
                    _isScanComplete = true;
                    _currentState = savedState;

                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.StateRestored,
                        evt =>
                        {
                            evt.SetMetadata(
                                TelemetryConstants.AnnotationType.RestoredState,
                                ScreenNameForState(savedState));
                            evt.SetMetadata(
                                TelemetryConstants.AnnotationType.RestoredIssueCount,
                                _issues.Count);
                        });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[HRT] Failed to restore issues: {ex.Message}");
                }
            }
        }

        [Serializable]
        private class IssueDataListWrapper
        {
            public List<IssueData> Items;
        }

        private void RenderCurrentState()
        {
            _contentContainer.Clear();

            switch (_currentState)
            {
                case ToolState.Welcome:
                    RenderWelcomeScreen();
                    break;
                case ToolState.CheckType:
                    RenderCheckTypeScreen();
                    break;
                case ToolState.ProjectDescription:
                    RenderProjectDescriptionScreen();
                    break;
                case ToolState.Scanning:
                    RenderScanningScreen();
                    break;
                case ToolState.Results:
                    RenderResultsScreen();
                    break;
                case ToolState.Confirmation:
                    RenderConfirmationScreen();
                    break;
                case ToolState.TodoList:
                case ToolState.Updating:
                    // These screens are removed — fall back to Results
                    RenderResultsScreen();
                    break;
            }
        }

        private void RenderWelcomeScreen()
        {
            _contentContainer.Add(WelcomeScreen.Create(
                onGetStarted: () =>
                {
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.WelcomeGetStartedClicked,
                        isEssential: true);
                    OnStartScan();
                }));
        }

        private void RenderCheckTypeScreen()
        {
            _contentContainer.Add(CheckTypeScreen.Create(
                initialUseAI: _checkTypeChosen ? _useAI : (bool?)null,
                onBack: () =>
                {
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.CheckTypeBackClicked);
                    SetState(ToolState.Welcome);
                },
                onNext: useAI =>
                {
                    _useAI = useAI;
                    _userChoseAiOnCheckType = useAI;
                    _checkTypeChosen = true;
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.CheckTypeSelected,
                        evt => evt.SetMetadata(
                            TelemetryConstants.AnnotationType.IntendedScanType,
                            useAI ? TelemetryConstants.ScanType.Ai : TelemetryConstants.ScanType.Standard),
                        isEssential: true);
                    if (useAI)
                    {
                        SetState(ToolState.ProjectDescription);
                    }
                    else
                    {
                        SetState(ToolState.Scanning);
                        RunScan();
                    }
                },
                buildAiPrompt: () => KnowledgeLoader.BuildCompletePrompt(includeJsonResponseFormat: false),
                onHowItWorksToggled: expanded =>
                {
                    if (expanded)
                    {
                        HandReadinessTelemetry.SendEvent(
                            TelemetryConstants.FalcoEventName.AiHowItWorksExpanded);
                    }
                },
                onRunItYourselfToggled: expanded =>
                {
                    if (expanded)
                    {
                        HandReadinessTelemetry.SendEvent(
                            TelemetryConstants.FalcoEventName.AiRunItYourselfExpanded);
                    }
                },
                onPromptCopied: () =>
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.AiPromptCopied)
            ));
        }

        private void RenderProjectDescriptionScreen()
        {
            // Capture whether the upsell was visible at the moment the user
            // submitted, so we can distinguish "AI users who hit Continue with
            // AgentBridge off" (guaranteed silent fallback) from normal AI submits.
            bool upsellShown = !AgentBridgeSettings.IsEnabled;
            _contentContainer.Add(ProjectDescriptionScreen.Create(
                projectDescription: _projectDescription,
                onDescriptionChanged: (value) => _projectDescription = value,
                // ProjectDescription gates AI based on AgentBridge state at render
                // time: it forces `false` when AgentBridge is disabled (upsell mode)
                // so the downstream scan falls back to Standard.
                onUseAIChanged: (value) => _useAI = value,
                onBack: () =>
                {
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.ProjectDescriptionBackClicked);
                    SetState(ToolState.CheckType);
                },
                onNext: () =>
                {
                    int chars = _projectDescription?.Length ?? 0;
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.ProjectDescriptionSubmitted,
                        evt =>
                        {
                            evt.SetMetadata(TelemetryConstants.AnnotationType.DescriptionChars, chars);
                            evt.SetMetadata(TelemetryConstants.AnnotationType.HasText, chars > 0);
                            evt.SetMetadata(TelemetryConstants.AnnotationType.AgentBridgeUpsellShown, upsellShown);
                        },
                        isEssential: true);
                    SetState(ToolState.Scanning);
                    RunScan();
                }
            ));
        }

        private void RenderScanningScreen()
        {
            _contentContainer.Clear();

            var phases = BuildScanPhases();

            string currentCheckName;
            string aiActivityText = null;

            if (_isAIScanInProgress)
            {
                currentCheckName = AiAnalyzingStatus;
                aiActivityText = BuildAiActivityLine();
            }
            else if (_isCompilingReport)
            {
                currentCheckName = "Compiling your readiness report...";
            }
            else
            {
                var buildTarget = GetCurrentBuildTarget();
                var allTasks = OVRProjectSetup.GetTasks(buildTarget)
                    .Where(task => HandReadinessSetupTasks.IsHandReadinessTask(task, buildTarget))
                    .ToList();

                if (_currentCheckIndex < allTasks.Count)
                {
                    currentCheckName = $"Checking: {GetTaskTitle(allTasks[_currentCheckIndex], buildTarget)}...";
                }
                else
                {
                    currentCheckName = "Finalizing scan...";
                }
            }

            _contentContainer.Add(ScanningScreen.Create(
                phases, _scanProgress, currentCheckName,
                isComplete: _isScanComplete,
                onNext: () => SetState(ToolState.Results),
                // Scanning's Back returns to whichever screen the user came
                // from: ProjectDescription on the AI path, CheckType on Standard.
                onBack: OnScanningBack,
                errorMessage: _lastAIError,
                aiActivityText: aiActivityText,
                aiProviderName: _useAI ? _aiProviderLabel : null
            ));
        }

        // Cancel any in-flight AI scan before navigating away from Scanning, so a
        // late-completing AgentBridge result can't clobber the screen the user
        // navigated to. The `_currentState != Scanning` guards in StartAIScan and
        // ProcessAIResponse make the continuation a no-op once we leave here.
        private void OnScanningBack()
        {
            if (_isAIScanInProgress)
            {
                // Invalidate the in-flight scan so its late async continuation is dropped.
                _aiScanGeneration++;
                _ = AgentBridgeAPI.CancelCurrentOperationAsync(
                    new CallerIdentity(HandReadinessConstants.ToolIdentifier));
                _aiProgressAnimator?.Pause();
                _aiProgressAnimator = null;
                _isAIScanInProgress = false;
                StopAIProgressTracking();

                EmitScanCompleted(
                    success: false,
                    errorKind: TelemetryConstants.ErrorKind.Cancelled,
                    errorMessage: null,
                    cancelReason: TelemetryConstants.CancelReason.BackClicked);
            }

            SetState(_useAI ? ToolState.ProjectDescription : ToolState.CheckType);
        }

        private List<ScanPhaseInfo> BuildScanPhases()
        {
            string analysisSubtitle = _useAI
                ? $"{_aiProviderLabel} is reviewing your code for device readiness opportunities"
                : "Scanning scripts for controller-only patterns";

            var phases = new List<ScanPhaseInfo>
            {
                new ScanPhaseInfo
                {
                    Title = _isScanComplete ? "Read project configuration" : "Reading project configuration",
                    Description = "OVR and Meta XR settings",
                    Status = _isScanComplete ? ScanPhaseStatus.Completed
                           : _scanProgress >= 0.25f ? ScanPhaseStatus.Completed : ScanPhaseStatus.InProgress
                },
                new ScanPhaseInfo
                {
                    Title = _isScanComplete ? "Checked device requirements" : "Checking device requirements",
                    Description = "Hand tracking, manifest entries",
                    Status = _isScanComplete ? ScanPhaseStatus.Completed
                           : _scanProgress >= 0.25f ? (_scanProgress >= 0.5f ? ScanPhaseStatus.Completed : ScanPhaseStatus.InProgress) : ScanPhaseStatus.Pending
                },
                new ScanPhaseInfo
                {
                    Title = _isScanComplete ? "Analyzed project for migration" : "Analyzing project for migration",
                    Description = analysisSubtitle,
                    Status = _isScanComplete ? ScanPhaseStatus.Completed
                           : _isAIScanInProgress ? ScanPhaseStatus.InProgress
                           : _scanProgress >= 0.5f ? (_scanProgress >= 0.75f ? ScanPhaseStatus.Completed : ScanPhaseStatus.InProgress) : ScanPhaseStatus.Pending
                },
                new ScanPhaseInfo
                {
                    Title = _isScanComplete ? "Compiled readiness report" : "Compiling readiness report",
                    Description = "Building your recommendations",
                    Status = _isScanComplete ? ScanPhaseStatus.Completed
                           : _isCompilingReport ? ScanPhaseStatus.InProgress
                           : _isAIScanInProgress ? ScanPhaseStatus.Pending
                           : _scanProgress >= 0.75f ? (_scanProgress >= 1f ? ScanPhaseStatus.Completed : ScanPhaseStatus.InProgress) : ScanPhaseStatus.Pending
                }
            };

            return phases;
        }

        // A scan is stale once a newer scan supersedes it or the user leaves the
        // scanning screen. StartAIScan awaits the full AI response, so a scan the
        // user navigated away from must not write results or errors into the live
        // screen when its await finally returns.
        private bool IsScanStale(int scanGeneration) =>
            scanGeneration != _aiScanGeneration || _currentState != ToolState.Scanning;

        private async Task StartAIScan()
        {
            var scanGeneration = ++_aiScanGeneration;
            _lastAIError = null;
            _isAIScanInProgress = true;
            _aiScanStartTime = Time.realtimeSinceStartup;
            _aiStatusText = "";
            ConversationEventBroker.MessageAdded += OnAIStreamingMessage;

            // Animate progress bar slowly from 70% to 95% during AI analysis
            _aiProgressAnimator = rootVisualElement.schedule.Execute(() =>
            {
                if (!_isAIScanInProgress) return;
                if (_scanProgress < 0.90f)
                {
                    // Diminishing rate: fills quickly at first, slows asymptotically.
                    // ~64% at 30s, ~73% at 60s, ~82% at 120s, ~87% at 180s, ~89% at 240s.
                    var remaining = 0.90f - _scanProgress;
                    _scanProgress += remaining * 0.003f;
                }
                // Update the progress bar + text in place rather than rebuilding the whole
                // scanning screen: a full rebuild every 200ms recreated the footer's Back
                // button mid-click, dropping clicks that straddled a tick.
                UpdateScanningProgressInPlace();
            }).Every(200);

            RenderScanningScreen();

            // Build the complete prompt with knowledge. On a re-scan the prior AI
            // list is appended so the AI can compose `previousId` mappings and a
            // `resolvedFromPrior` array — see KnowledgeLoader for the contract.
            var completePrompt = KnowledgeLoader.BuildCompletePrompt(_projectDescription, _priorAiIssues);
            if (string.IsNullOrEmpty(completePrompt))
            {
                _lastAIError = "Failed to load AI prompt. Check that Knowledge files exist.";
                Debug.LogWarning($"[HRT] {_lastAIError}");
                _isAIScanInProgress = false;
                StopAIProgressTracking();
                RestorePriorAiOnFailure();
                _aiSubscanSuccess = false;
                _aiErrorKind = TelemetryConstants.ErrorKind.AiKnowledgeMissing;
                _isScanComplete = true;
                EmitScanCompleted(success: true, errorKind: _aiErrorKind, errorMessage: _lastAIError);
                RenderCurrentState();
                return;
            }

            // Send the prompt to the AI
            try
            {
                AgentBridgeAPI.EnsureServiceInitialized();
                AgentBridgeAPI.ClearConversation(new CallerIdentity(HandReadinessConstants.ToolIdentifier));

                var success = await AgentBridgeAPI.SendPromptAsync(completePrompt, new CallerIdentity(HandReadinessConstants.ToolIdentifier));

                // Dropped if the user left Scanning mid-flight or a newer scan superseded
                // this one (Back then Next); see OnScanningBack and IsScanStale.
                if (IsScanStale(scanGeneration)) return;

                if (success)
                {
                    // SendPromptAsync awaits the full AI response via ProcessUserInputAsync,
                    // so by the time we get here the response should be in conversation history.
                    ProcessAIResponse(scanGeneration);
                }
                else
                {
                    var error = AgentBridgeAPI.GetLastError() ?? "Unknown error";
                    _lastAIError = $"AI analysis failed: {error}";
                    Debug.LogError($"[HRT] {_lastAIError}");
                    _isAIScanInProgress = false;
                    _aiProgressAnimator?.Pause();
                    _aiProgressAnimator = null;
                    StopAIProgressTracking();
                    RestorePriorAiOnFailure();
                    _aiSubscanSuccess = false;
                    _aiErrorKind = TelemetryConstants.ErrorKind.AiSendReturnedFalse;
                    _isScanComplete = true;
                    EmitScanCompleted(success: true, errorKind: _aiErrorKind, errorMessage: error);
                    RenderCurrentState();
                }
            }
            catch (Exception ex)
            {
                // Dropped if the user left Scanning mid-flight or a newer scan superseded
                // this one (Back then Next); see OnScanningBack and IsScanStale.
                if (IsScanStale(scanGeneration)) return;

                _lastAIError = $"Failed to send prompt to AI: {ex.Message}";
                _isAIScanInProgress = false;
                _aiProgressAnimator?.Pause();
                _aiProgressAnimator = null;
                StopAIProgressTracking();
                Debug.LogError($"[HRT] {_lastAIError}\n{ex.StackTrace}");
                RestorePriorAiOnFailure();
                _aiSubscanSuccess = false;
                _aiErrorKind = TelemetryConstants.ErrorKind.AiSendThrew;
                _isScanComplete = true;
                EmitScanCompleted(success: true, errorKind: _aiErrorKind, errorMessage: ex.Message);
                RenderCurrentState();
            }
        }

        /// <summary>
        /// On any AI failure path, fold the snapshotted prior AI items back into
        /// `_issues` so a transient outage on a re-scan does not blow away earlier
        /// recommendations or the user's mark-complete state.
        /// </summary>
        private void RestorePriorAiOnFailure()
        {
            if (_priorAiIssues == null) return;
            foreach (var item in _priorAiIssues)
            {
                _issues.Add(item);
            }
            _priorAiIssues = null;
        }

        private void ProcessAIResponse(int scanGeneration)
        {
            // Dropped if the user left Scanning mid-flight or a newer scan superseded
            // this one (Back then Next); see OnScanningBack and IsScanStale.
            if (IsScanStale(scanGeneration)) return;

            _isAIScanInProgress = false;
            _isCompilingReport = true;
            _aiProgressAnimator?.Pause();
            _aiProgressAnimator = null;
            StopAIProgressTracking();
            _scanProgress = 0.90f;

            RenderScanningScreen();

            try
            {
                var hrtCaller = new CallerIdentity(HandReadinessConstants.ToolIdentifier);
                var history = AgentBridgeAPI.GetConversationHistoryForCaller(hrtCaller);
                if (history == null || history.Count == 0)
                {
                    // Fallback: try global history
                    history = AgentBridgeAPI.GetConversationHistory();
                }
                if (history == null || history.Count == 0)
                {
                    _lastAIError = "No response received from AI.";
                    Debug.LogWarning($"[HRT] {_lastAIError}");
                    // Still proceed to results with automated checks only;
                    // restore prior AI items so user mark-complete state survives.
                    RestorePriorAiOnFailure();
                    _isCompilingReport = false;
                    _isScanComplete = true;
                    _aiSubscanSuccess = false;
                    _aiErrorKind = TelemetryConstants.ErrorKind.AiNoAssistantMessage;
                    EmitScanCompleted(success: true, errorKind: _aiErrorKind, errorMessage: _lastAIError);
                    RenderCurrentState();
                    return;
                }

                string lastResponse = null;
                string longestResponse = null;
                int longestLength = 0;

                for (int i = history.Count - 1; i >= 0; i--)
                {
                    if (history[i].MessageType == "assistant")
                    {
                        var content = history[i].Content;

                        if (content != null && content.Contains("\"analysisComplete\""))
                        {
                            lastResponse = content;
                            break;
                        }

                        if (content != null && content.Length > longestLength)
                        {
                            longestLength = content.Length;
                            longestResponse = content;
                        }
                    }
                }

                if (string.IsNullOrEmpty(lastResponse) && !string.IsNullOrEmpty(longestResponse))
                {
                    lastResponse = longestResponse;
                }

                if (string.IsNullOrEmpty(lastResponse))
                {
                    _lastAIError = "AI response was empty.";
                    RestorePriorAiOnFailure();
                    _isCompilingReport = false;
                    _isScanComplete = true;
                    _aiSubscanSuccess = false;
                    _aiErrorKind = TelemetryConstants.ErrorKind.AiResponseEmpty;
                    EmitScanCompleted(success: true, errorKind: _aiErrorKind, errorMessage: _lastAIError);
                    RenderCurrentState();
                    return;
                }

                var parseResult = AIResponseParser.Parse(lastResponse);
                if (!parseResult.Success)
                {
                    Debug.LogWarning($"[HRT] Failed to parse AI response: {parseResult.ErrorMessage}");
                    RestorePriorAiOnFailure();
                    _aiSubscanSuccess = false;
                    _aiErrorKind = TelemetryConstants.ErrorKind.AiParseFailed;
                }
                else
                {
                    foreach (var suggestion in parseResult.Suggestions)
                    {
                        suggestion.Category = IssueCategory.AI;
                    }
                    var merged = MergeAiIssues(
                        _priorAiIssues ?? new List<IssueData>(),
                        parseResult.Suggestions,
                        parseResult.ResolvedFromPrior ?? new List<string>());
                    _issues.AddRange(merged);
                    _priorAiIssues = null;
                    _aiSubscanSuccess = true;
                    _aiIssuesResolvedFromPrior = parseResult.ResolvedFromPrior?.Count ?? 0;
                }

                _isCompilingReport = false;
                _scanProgress = 1f;
                _isScanComplete = true;
                EmitScanCompleted(
                    success: true,
                    errorKind: _aiErrorKind,
                    errorMessage: !string.IsNullOrEmpty(_aiErrorKind) ? parseResult.ErrorMessage : null);

                RenderScanningScreen();
                rootVisualElement.schedule.Execute(() =>
                {
                    if (!IsScanStale(scanGeneration))
                        SetState(ToolState.Results);
                }).StartingIn(1500);
            }
            catch (Exception ex)
            {
                _lastAIError = $"Error processing AI response: {ex.Message}";
                Debug.LogError($"[HRT] {_lastAIError}\n{ex.StackTrace}");
                RestorePriorAiOnFailure();
                _isCompilingReport = false;
                _aiSubscanSuccess = false;
                _aiErrorKind = TelemetryConstants.ErrorKind.Unknown;
                _isScanComplete = true;
                EmitScanCompleted(success: true, errorKind: _aiErrorKind, errorMessage: ex.Message);
                RenderCurrentState();
            }
        }

        private void RenderResultsScreen()
        {
            var issues = _issues ?? new List<IssueData>();
            bool isAiReport = _scanActualScanType == TelemetryConstants.ScanType.Ai;
            bool hasPendingAutomatedFix = _failedTasks != null && _failedTasks.Any(task =>
                task.FixAction != null && issues.Any(issue =>
                    !issue.IsFixed
                    && issue.Category == IssueCategory.Automation
                    && issue.TaskUid == task.Uid.ToString()));

            // Non-empty only when the active provider can resume in a terminal (Claude Code, Gemini); else the banner stays hidden.
            string aiResumeCommand = null;
            if (isAiReport)
            {
                try
                {
                    var hrtCaller = new CallerIdentity(HandReadinessConstants.ToolIdentifier);
                    var rawResume = AgentBridgeAPI.GetResumeCommandForCaller(hrtCaller);
                    if (!string.IsNullOrEmpty(rawResume))
                    {
                        // Reduce the CLI to its bare command name (drop the absolute path):
                        // split at the first flag so a path containing spaces stays intact, and
                        // strip surrounding quotes so GetFileName doesn't keep a trailing ".
                        int flagIdx = rawResume.IndexOf(" --", StringComparison.Ordinal);
                        var bareResume = flagIdx > 0
                            ? System.IO.Path.GetFileName(rawResume.Substring(0, flagIdx).Trim().Trim('"')) + rawResume.Substring(flagIdx)
                            : rawResume;

                        var projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath)?.Replace('\\', '/');
                        if (string.IsNullOrEmpty(projectRoot))
                        {
                            aiResumeCommand = bareResume;
                        }
                        else
                        {
                            // Prepend `cd` so the session resumes in the project dir. POSIX shells chain
                            // with `&&`; Windows PowerShell (the Win10/11 default, and it rejects `&&`)
                            // chains with `;` and its Set-Location switches drives. Single-quote the path
                            // so shell metacharacters ($, `, ") in it stay literal.
                            bool isWindows = Application.platform == RuntimePlatform.WindowsEditor;
                            var quotedRoot = isWindows
                                ? "'" + projectRoot.Replace("'", "''") + "'"
                                : "'" + projectRoot.Replace("'", "'\\''") + "'";
                            aiResumeCommand = $"cd {quotedRoot} {(isWindows ? ";" : "&&")} {bareResume}";
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[HRT] Could not resolve AI resume command: {ex.Message}");
                }
            }

            _contentContainer.Add(ResultsScreen.Create(
                issues: issues,
                onViewDetails: OnAIFixIssue,
                onApplyFix: OnFixIssue,
                onCopyToClipboard: OnCopyIssueToClipboard,
                onMarkComplete: OnMarkIssueComplete,
                onExport: OnExportReport,
                onCheckReadiness: () =>
                {
                    // Two roles for the primary action: re-scan to verify outstanding work, or
                    // advance to the Confirmation screen once every recommendation is resolved
                    // (label mirrors this in ResultsScreen.CreateFooter). An empty list means the
                    // project is already ready → treat as complete so the button confirms readiness.
                    bool allComplete = issues.All(i => i.IsFixed);
                    if (allComplete)
                    {
                        HandReadinessTelemetry.SendEvent(
                            TelemetryConstants.FalcoEventName.ConfirmReadinessClicked,
                            evt => evt.SetMetadata(
                                TelemetryConstants.AnnotationType.IssueCount, issues.Count),
                            isEssential: true);
                        SetState(ToolState.Confirmation);
                        return;
                    }

                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.RescanClicked,
                        evt => evt.SetMetadata(
                            TelemetryConstants.AnnotationType.Source,
                            TelemetryConstants.Source.CheckReadinessButton),
                        isEssential: true);
                    // Re-run scan to check what was fixed
                    SetState(ToolState.Scanning);
                    RunScan();
                },
                onReanalyze: () =>
                {
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.RescanClicked,
                        evt => evt.SetMetadata(
                            TelemetryConstants.AnnotationType.Source,
                            TelemetryConstants.Source.ReanalyzeLink),
                        isEssential: true);
                    // Rerun the full configured scan. Prior recommendations are retained only
                    // so the fresh AI/regex results can reconcile completed and removed items.
                    SetState(ToolState.Scanning);
                    RunScan();
                },
                onApplyAllAutomated: hasPendingAutomatedFix ? OnFixAllAuto : null,
                onMarkAllComplete: OnMarkAllComplete,
                aiResumeCommand: aiResumeCommand,
                aiTokensSummary: isAiReport ? FormatAiTokensUsedPrefix() : null,
                isAiReport: isAiReport,
                onResumeCommandCopied: () =>
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.AiSessionResumeCopied)
            ));
        }

        private void RenderConfirmationScreen()
        {
            int resolvedCount = _issues?.Count ?? 0;
            _hasOvrManagerInScene = FindOvrManagerInScene() != null;
            _contentContainer.Add(ConfirmationScreen.Create(
                totalResolvedCount: resolvedCount,
                onClose: () =>
                {
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.ConfirmationClosed,
                        evt => evt.SetMetadata(
                            TelemetryConstants.AnnotationType.IssueCount, resolvedCount),
                        isEssential: true);
                    Close();
                },
                onExploreBuildingBlocks: () =>
                {
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.ConfirmationExploreClicked,
                        evt => evt.SetMetadata(
                            TelemetryConstants.AnnotationType.IssueCount, resolvedCount),
                        isEssential: true);
                    EditorApplication.ExecuteMenuItem("Window/Meta/Tools/Building Blocks");
                    Close();
                },
                hasOvrManager: _hasOvrManagerInScene,
                onOpenFovSimulationSettings: () =>
                {
                    OVRManager manager = FindOvrManagerInScene();
                    if (manager != null)
                    {
                        OVRManagerEditor.HighlightFovSimulationSetting(manager);
                    }
                    else
                    {
                        RenderCurrentState();
                        ShowNotification(new GUIContent(
                            "No OVRManager was found. Use Oculus Runtime Settings or Immersive Debugger instead."));
                    }
                }));
        }

        private static OVRManager FindOvrManagerInScene() =>
            UnityEngine.Object.FindFirstObjectByType<OVRManager>(FindObjectsInactive.Include);

        private void OnHierarchyChanged()
        {
            if (_currentState != ToolState.Confirmation)
            {
                return;
            }

            bool hasOvrManager = FindOvrManagerInScene() != null;
            if (hasOvrManager == _hasOvrManagerInScene)
            {
                return;
            }

            _hasOvrManagerInScene = hasOvrManager;
            RenderCurrentState();
        }

        // RenderUpdatingScreen removed — Results is now the final screen

        // Resolved AI provider display name for status copy on the scanning screen.
        // Falls back to a generic label when AgentBridge is disabled or the active
        // service name is unavailable ("None").
        private string GetAiProviderLabel()
        {
            try
            {
                if (!AgentBridgeSettings.IsEnabled)
                {
                    return DefaultAiProviderLabel;
                }
                AgentBridgeAPI.EnsureServiceInitialized();
                var name = AgentBridgeAPI.GetCurrentServiceName();
                return string.IsNullOrEmpty(name) || name == "None"
                    ? DefaultAiProviderLabel
                    : name;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[HRT] Could not resolve AI provider name: {ex.Message}");
                return DefaultAiProviderLabel;
            }
        }

        private void RunAutoFixes()
        {
            if (_failedTasks == null || _failedTasks.Count == 0)
            {
                // Mark all automation issues as fixed
                if (_issues != null)
                {
                    foreach (var issue in _issues.Where(i => i.Category == IssueCategory.Automation))
                        issue.IsFixed = true;
                }
                SetState(ToolState.TodoList);
                return;
            }

            var buildTarget = GetCurrentBuildTarget();
            var tasksToFix = _failedTasks.Where(t => t.FixAction != null).ToList();

            if (tasksToFix.Count == 0)
            {
                SetState(ToolState.TodoList);
                return;
            }

            OVRProjectSetup.FixTasks(buildTarget,
                filter: tasks => tasks.Where(t => tasksToFix.Any(ft => ft.Uid == t.Uid)).ToList(),
                logMessages: OVRProjectSetup.LogMessages.Changed,
                blocking: true,
                allowHiddenGroupTasks: true,
                onCompleted: _ =>
                {
                    foreach (var issue in _issues)
                    {
                        var task = _failedTasks.FirstOrDefault(t => t.Uid.ToString() == issue.TaskUid);
                        if (task != null && task.IsDone(buildTarget))
                        {
                            issue.IsFixed = true;
                        }
                    }
                    SetState(ToolState.TodoList);
                });
        }

        private void OnStartScan()
        {
            SetState(ToolState.CheckType);
        }

        // Results-row overflow "Copy to clipboard" action — same payload as the
        // details popup, copied without opening it.
        private void OnCopyIssueToClipboard(IssueData issue)
        {
            HandReadinessTelemetry.SendEvent(
                TelemetryConstants.FalcoEventName.IssueCopiedToClipboard,
                evt =>
                {
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssueUid, issue.TaskUid ?? "");
                    evt.SetMetadata(TelemetryConstants.AnnotationType.Priority, PriorityName(issue.Priority));
                    evt.SetMetadata(TelemetryConstants.AnnotationType.Category, CategoryName(issue.Category));
                });
            EditorGUIUtility.systemCopyBuffer = IssueDetailsPopup.BuildClipboardPayload(issue);
            ShowNotification(new GUIContent("Copied to clipboard!"), 1.5);
        }

        // Results-row overflow "Mark as complete" action — mirrors the details popup.
        private void OnMarkIssueComplete(IssueData issue)
        {
            MarkIssueComplete(issue);
            RenderCurrentState();
        }

        private void OnMarkAllComplete()
        {
            foreach (var issue in (_issues ?? new List<IssueData>()).Where(issue => !issue.IsFixed))
            {
                MarkIssueComplete(issue);
            }
            RenderCurrentState();
        }

        private static void MarkIssueComplete(IssueData issue)
        {
            issue.IsFixed = true;
            HandReadinessTelemetry.SendEvent(
                TelemetryConstants.FalcoEventName.IssueMarkedComplete,
                evt =>
                {
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssueUid, issue.TaskUid ?? "");
                    evt.SetMetadata(TelemetryConstants.AnnotationType.Priority, PriorityName(issue.Priority));
                    evt.SetMetadata(TelemetryConstants.AnnotationType.Category, CategoryName(issue.Category));
                },
                isEssential: true);
        }

        private void OnFixIssue(IssueData issue)
        {
            var buildTarget = GetCurrentBuildTarget();

            HandReadinessTelemetry.SendEvent(
                TelemetryConstants.FalcoEventName.FixStarted,
                evt =>
                {
                    evt.SetMetadata(TelemetryConstants.AnnotationType.IssueUid, issue.TaskUid ?? "");
                    evt.SetMetadata(TelemetryConstants.AnnotationType.Priority, PriorityName(issue.Priority));
                    evt.SetMetadata(TelemetryConstants.AnnotationType.Category, CategoryName(issue.Category));
                });

            var fixStopwatch = Stopwatch.StartNew();
            var task = _failedTasks?.FirstOrDefault(t => t.Uid.ToString() == issue.TaskUid);
            if (task == null || task.FixAction == null)
            {
                // Post-domain-reload case the QA doc calls out: `_failedTasks` isn't
                // persisted, so an Apply click on a restored row finds no task.
                fixStopwatch.Stop();
                HandReadinessTelemetry.SendEvent(
                    TelemetryConstants.FalcoEventName.FixCompleted,
                    evt =>
                    {
                        evt.SetMetadata(TelemetryConstants.AnnotationType.IssueUid, issue.TaskUid ?? "");
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Priority, PriorityName(issue.Priority));
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Category, CategoryName(issue.Category));
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Success, false);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.ErrorKind, TelemetryConstants.ErrorKind.TaskLookupMissed);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.DurationMs, fixStopwatch.ElapsedMilliseconds);
                    },
                    isEssential: true);
                return;
            }

            try
            {
                OVRProjectSetup.FixTask(buildTarget, task, OVRProjectSetup.LogMessages.Changed, blocking: true,
                    allowHiddenGroupTasks: true,
                    onCompleted: _ =>
                    {
                        fixStopwatch.Stop();
                        bool resolved = task.IsDone(buildTarget);
                        if (resolved)
                        {
                            issue.IsFixed = true;
                        }
                        HandReadinessTelemetry.SendEvent(
                            TelemetryConstants.FalcoEventName.FixCompleted,
                            evt =>
                            {
                                evt.SetMetadata(TelemetryConstants.AnnotationType.IssueUid, issue.TaskUid ?? "");
                                evt.SetMetadata(TelemetryConstants.AnnotationType.Priority, PriorityName(issue.Priority));
                                evt.SetMetadata(TelemetryConstants.AnnotationType.Category, CategoryName(issue.Category));
                                evt.SetMetadata(TelemetryConstants.AnnotationType.Success, resolved);
                                evt.SetMetadata(TelemetryConstants.AnnotationType.DurationMs, fixStopwatch.ElapsedMilliseconds);
                                if (!resolved)
                                {
                                    evt.SetMetadata(TelemetryConstants.AnnotationType.ErrorKind, TelemetryConstants.ErrorKind.FixDidntResolve);
                                }
                            },
                            isEssential: true);
                        RenderCurrentState();
                    });
            }
            catch (Exception ex)
            {
                fixStopwatch.Stop();
                HandReadinessTelemetry.SendEvent(
                    TelemetryConstants.FalcoEventName.FixCompleted,
                    evt =>
                    {
                        evt.SetMetadata(TelemetryConstants.AnnotationType.IssueUid, issue.TaskUid ?? "");
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Priority, PriorityName(issue.Priority));
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Category, CategoryName(issue.Category));
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Success, false);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.ErrorKind, TelemetryConstants.ErrorKind.FixThrew);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.ErrorMessage, ex.Message);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.DurationMs, fixStopwatch.ElapsedMilliseconds);
                    },
                    isEssential: true);
                throw;
            }
        }

        private void OnAIFixIssue(IssueData issue)
        {
            // Mirror ResultsScreen ordering (unfixed by priority, then fixed by
            // priority) so the popup's Back/Next buttons walk the same sequence
            // the user sees in the table.
            var all = _issues ?? new List<IssueData>();
            var orderedIssues = all.Where(i => !i.IsFixed).OrderBy(i => i.Priority)
                .Concat(all.Where(i => i.IsFixed).OrderBy(i => i.Priority))
                .ToList();
            IssueDetailsPopup.Show(issue, orderedIssues, () =>
            {
                // Re-render so an item just marked complete moves to the bottom
                // of the list with a checkmark instead of staying as unfixed.
                RenderCurrentState();
            });
        }

        private void OnExportReport()
        {
            var path = EditorUtility.SaveFilePanel(
                "Export Device Readiness Report",
                "",
                "DeviceReadinessReport",
                "md");

            if (string.IsNullOrEmpty(path))
            {
                HandReadinessTelemetry.SendEvent(
                    TelemetryConstants.FalcoEventName.ReportExported,
                    evt =>
                    {
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Success, false);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Cancelled, true);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Format, "md");
                    },
                    isEssential: true);
                return;
            }

            var sb = new System.Text.StringBuilder();

            sb.AppendLine("# Device Readiness Report");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {System.DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine();

            if (_issues == null || _issues.Count == 0)
            {
                sb.AppendLine("No issues found. Your project is ready!");
                System.IO.File.WriteAllText(path, sb.ToString());
                EditorUtility.DisplayDialog("Export Complete", $"Report saved to:\n{path}", "OK");
                return;
            }

            // Summary
            int manualCount = _issues.Count(i => !i.IsFixed && i.Category == IssueCategory.Manual);
            int aiCount = _issues.Count(i => !i.IsFixed && i.Category == IssueCategory.AI);
            int autoCount = _issues.Count(i => !i.IsFixed && i.Category == IssueCategory.Automation);
            int fixedCount = _issues.Count(i => i.IsFixed);

            sb.AppendLine("## Summary");
            sb.AppendLine();
            sb.AppendLine($"| Category | Count |");
            sb.AppendLine($"|----------|-------|");
            sb.AppendLine($"| Manual review | {manualCount} |");
            sb.AppendLine($"| AI assisted | {aiCount} |");
            sb.AppendLine($"| Automated | {autoCount} |");
            sb.AppendLine($"| Already passing | {fixedCount} |");
            sb.AppendLine();

            // Issues by category
            void WriteCategory(string heading, IssueCategory category)
            {
                var items = _issues.Where(i => i.Category == category && !i.IsFixed).ToList();
                if (items.Count == 0) return;

                sb.AppendLine($"## {heading}");
                sb.AppendLine();

                foreach (var issue in items)
                {
                    var complexity = issue.IsAISuggestion ? $" · Complexity: {issue.Complexity}" : "";
                    var priority = $" · Priority: {issue.Priority}";
                    sb.AppendLine($"### {issue.Title}");
                    sb.AppendLine();
                    sb.AppendLine($"**Status:** Pending{priority}{complexity}");
                    sb.AppendLine();

                    if (!string.IsNullOrEmpty(issue.Description))
                    {
                        sb.AppendLine(issue.Description);
                        sb.AppendLine();
                    }

                    if (!string.IsNullOrEmpty(issue.HandTrackingAdaptation))
                    {
                        sb.AppendLine($"**Recommended Adaptation:** {issue.HandTrackingAdaptation}");
                        sb.AppendLine();
                    }

                    if (issue.ImplementationSteps != null && issue.ImplementationSteps.Count > 0)
                    {
                        sb.AppendLine("**Implementation Steps:**");
                        for (int i = 0; i < issue.ImplementationSteps.Count; i++)
                        {
                            sb.AppendLine($"{i + 1}. {issue.ImplementationSteps[i]}");
                        }
                        sb.AppendLine();
                    }

                    sb.AppendLine("---");
                    sb.AppendLine();
                }
            }

            WriteCategory("Manual Review", IssueCategory.Manual);
            WriteCategory("AI Assisted", IssueCategory.AI);
            WriteCategory("Automated", IssueCategory.Automation);

            // Fixed items
            var fixedItems = _issues.Where(i => i.IsFixed).ToList();
            if (fixedItems.Count > 0)
            {
                sb.AppendLine("## Already Passing");
                sb.AppendLine();
                foreach (var issue in fixedItems)
                {
                    sb.AppendLine($"- ~~{issue.Title}~~ ✓");
                }
                sb.AppendLine();
            }

            try
            {
                System.IO.File.WriteAllText(path, sb.ToString());
                EditorUtility.DisplayDialog("Export Complete", $"Report saved to:\n{path}", "OK");
                int unfixed = _issues?.Count(i => !i.IsFixed) ?? 0;
                int total = _issues?.Count ?? 0;
                HandReadinessTelemetry.SendEvent(
                    TelemetryConstants.FalcoEventName.ReportExported,
                    evt =>
                    {
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Success, true);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Cancelled, false);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Format, "md");
                        evt.SetMetadata(TelemetryConstants.AnnotationType.IssueCount, total);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.UnfixedCount, unfixed);
                    },
                    isEssential: true);
            }
            catch (Exception ex)
            {
                HandReadinessTelemetry.SendEvent(
                    TelemetryConstants.FalcoEventName.ReportExported,
                    evt =>
                    {
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Success, false);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Cancelled, false);
                        evt.SetMetadata(TelemetryConstants.AnnotationType.Format, "md");
                        evt.SetMetadata(TelemetryConstants.AnnotationType.ErrorMessage, ex.Message);
                    },
                    isEssential: true);
                throw;
            }
        }

        private void OnFixAllAuto()
        {
            if (_failedTasks == null || _failedTasks.Count == 0)
                return;

            var buildTarget = GetCurrentBuildTarget();
            var tasksToFix = _failedTasks.Where(t => t.FixAction != null).ToList();

            if (tasksToFix.Count == 0)
                return;

            int totalCount = tasksToFix.Count;
            HandReadinessTelemetry.SendEvent(
                TelemetryConstants.FalcoEventName.ApplyAllStarted,
                evt => evt.SetMetadata(TelemetryConstants.AnnotationType.TotalCount, totalCount));

            var applyStopwatch = Stopwatch.StartNew();
            OVRProjectSetup.FixTasks(buildTarget,
                filter: tasks => tasks.Where(t => tasksToFix.Any(ft => ft.Uid == t.Uid)).ToList(),
                logMessages: OVRProjectSetup.LogMessages.Changed,
                blocking: true,
                allowHiddenGroupTasks: true,
                onCompleted: _ =>
                {
                    int successCount = 0;
                    foreach (var issue in _issues)
                    {
                        var task = _failedTasks.FirstOrDefault(t => t.Uid.ToString() == issue.TaskUid);
                        if (task != null && task.IsDone(buildTarget))
                        {
                            issue.IsFixed = true;
                            successCount++;
                        }
                    }
                    applyStopwatch.Stop();
                    int finalSuccessCount = successCount;
                    HandReadinessTelemetry.SendEvent(
                        TelemetryConstants.FalcoEventName.ApplyAllCompleted,
                        evt =>
                        {
                            evt.SetMetadata(TelemetryConstants.AnnotationType.TotalCount, totalCount);
                            evt.SetMetadata(TelemetryConstants.AnnotationType.SuccessCount, finalSuccessCount);
                            evt.SetMetadata(TelemetryConstants.AnnotationType.FailCount, totalCount - finalSuccessCount);
                            evt.SetMetadata(TelemetryConstants.AnnotationType.DurationMs, applyStopwatch.ElapsedMilliseconds);
                        },
                        isEssential: true);
                    RenderCurrentState();
                });
        }

        private void OnAIStreamingMessage(ConversationMessage message)
        {
            if (!_isAIScanInProgress || _currentState != ToolState.Scanning) return;
            if (message.CallerId != HandReadinessConstants.ToolIdentifier) return;

            if (message.MessageType == "tool_use" && !string.IsNullOrEmpty(message.ToolName))
            {
                _aiStatusText = FormatAIToolActivity(message);
            }
        }

        private static string FormatAIToolActivity(ConversationMessage message)
        {
            var content = message.Content ?? "";
            var bracketEnd = content.IndexOf(']');
            if (bracketEnd < 0)
                return content.Length > 80 ? content.Substring(0, 77) + "..." : content;

            var context = content.Substring(bracketEnd + 1).Trim();
            if (context.StartsWith("(") && context.EndsWith(")"))
                context = context.Substring(1, context.Length - 2);

            switch (message.ToolName)
            {
                case "Read":
                    return $"Reading {ShortenPath(context)}";
                case "Bash":
                    if (string.IsNullOrEmpty(context)) return "Running analysis command...";
                    return context.Length > 70 ? context.Substring(0, 67) + "..." : context;
                case "Grep":
                    if (string.IsNullOrEmpty(context)) return "Searching project...";
                    return $"Searching: {(context.Length > 60 ? context.Substring(0, 57) + "..." : context)}";
                case "Glob":
                    return "Scanning project structure...";
                case "Edit":
                case "Write":
                    return "Processing analysis results...";
                case "search_files":
                    return "Searching project files...";
                case "ToolSearch":
                    return "Loading analysis tools...";
                default:
                    if (string.IsNullOrEmpty(context))
                        return $"Using {message.ToolName}...";
                    return context.Length > 70 ? context.Substring(0, 67) + "..." : context;
            }
        }

        private static string ShortenPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "project files...";
            var fileName = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName)) return "project files...";
            var dir = System.IO.Path.GetDirectoryName(path);
            var parentDir = string.IsNullOrEmpty(dir) ? null : System.IO.Path.GetFileName(dir);
            if (!string.IsNullOrEmpty(parentDir))
                return $"{parentDir}/{fileName}";
            return fileName;
        }

        private void StopAIProgressTracking()
        {
            ConversationEventBroker.MessageAdded -= OnAIStreamingMessage;
            _aiStatusText = "";
        }

        private string FormatAIElapsedTime()
        {
            var elapsed = Time.realtimeSinceStartup - _aiScanStartTime;
            var mins = (int)(elapsed / 60);
            var secs = (int)(elapsed % 60);
            return mins > 0 ? $"Elapsed: {mins}m {secs:D2}s" : $"Elapsed: {secs}s";
        }

        private string BuildAiActivityLine()
        {
            var activity = !string.IsNullOrEmpty(_aiStatusText) ? _aiStatusText : "Starting...";
            return $"{activity} · {FormatAIElapsedTime()}";
        }

        // "12.3k tokens used. " prefix for the results-page resume banner, shown when the
        // active provider reported usage. Surfaced on the results page rather than the live
        // scanning line because most providers (codex, gemini) report their total only at
        // the end of the turn, so a per-line counter would sit empty for the whole scan.
        // Empty when no tokens were counted.
        private static string FormatAiTokensUsedPrefix()
        {
            try
            {
                var usage = AgentBridgeAPI.GetUsageForCaller(
                    new CallerIdentity(HandReadinessConstants.ToolIdentifier));
                var total = usage?.TotalTokens ?? 0;
                if (total <= 0)
                {
                    return "";
                }
                string display;
                if (total >= 999_500)
                {
                    display = $"{total / 1_000_000.0:0.0}M";
                }
                else if (total >= 1000)
                {
                    display = $"{total / 1000.0:0.0}k";
                }
                else
                {
                    display = total.ToString();
                }
                return $"{display} tokens used. ";
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.LogWarning($"[HRT] Failed to read AI token usage: {ex.Message}");
                return "";
            }
        }

        // Mutate the existing progress bar + status lines rather than rebuilding the screen,
        // so the footer's Back button survives across animator ticks. Falls back to a full
        // render if the screen elements aren't built yet (first tick).
        private void UpdateScanningProgressInPlace()
        {
            var fill = _contentContainer.Q<VisualElement>(ScanningScreen.ProgressFillName);
            var statusLabel = _contentContainer.Q<Label>(ScanningScreen.StatusLabelName);
            if (fill == null || statusLabel == null)
            {
                RenderScanningScreen();
                return;
            }

            fill.style.width = Length.Percent(Mathf.Max(_scanProgress * 100f, 3f));
            statusLabel.text = AiAnalyzingStatus;

            var activityLabel = _contentContainer.Q<Label>(ScanningScreen.ActivityLabelName);
            if (activityLabel != null)
            {
                activityLabel.text = BuildAiActivityLine();
            }
        }

        private void OnDone()
        {
            Close();
        }
    }
}
