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


using Meta.XR.Editor.Callbacks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Meta.XR.Editor.Notifications;
using Meta.XR.Editor.UserInterface;
using Meta.XR.Editor.Id;
using Meta.XR.MRUtilityKit.Tests.Editor;
using RLDSStyles = Meta.XR.Editor.UserInterface.RLDS.Styles;
using UIStyles = Meta.XR.Editor.UserInterface.Styles;

namespace Meta.XR.MRUtilityKit.Tests
{

    /// <summary>
    /// Room-level statistics computed from test results.
    /// </summary>
    [Serializable]
    public sealed class RoomStatistics
    {
        [SerializeField] public string RoomName = string.Empty;
        [SerializeField] public int Total;
        [SerializeField] public int Passed;
        [SerializeField] public int Failed;
        [SerializeField] public double SuccessRate;

        public override bool Equals(object obj) =>
            obj is RoomStatistics other && RoomName == other.RoomName;

        public override int GetHashCode() =>
            RoomName?.GetHashCode() ?? 0;
    }

    /// <summary>
    /// Overall test statistics for a complete test run.
    /// </summary>
    [Serializable]
    public sealed class TestStatistics
    {
        [SerializeField] public int TotalTests;
        [SerializeField] public int TotalPassed;
        [SerializeField] public double OverallSuccessRate;
    }

    /// <summary>
    /// Main test report container - simplified and cohesive.
    /// </summary>
    [Serializable]
    public sealed class TestReport
    {
        [SerializeField] private string timestampString;
        [SerializeField] public string TestMethodName = string.Empty;
        [SerializeField] public List<TestResult> Results = new List<TestResult>();
        [SerializeField] public TestStatistics Statistics = new TestStatistics();

        /// <summary>
        /// The timestamp when this test report was generated.
        /// </summary>
        public DateTime Timestamp
        {
            get => DateTime.TryParse(timestampString, out var dt) ? dt : DateTime.Now;
            set => timestampString = value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Computed property: Results grouped by room name.
        /// </summary>
        public Dictionary<string, List<TestResult>> ResultsByRoom =>
            Results.GroupBy(r => r.RoomName)
                   .ToDictionary(g => g.Key, g => g.ToList());

        /// <summary>
        /// Computed property: Room statistics calculated from results.
        /// </summary>
        public Dictionary<string, RoomStatistics> RoomStatistics =>
            Results.GroupBy(r => r.RoomName)
                   .ToDictionary(g => g.Key, g => new RoomStatistics
                   {
                       RoomName = g.Key,
                       Total = g.Count(),
                       Passed = g.Count(r => r.Passed),
                       Failed = g.Count(r => !r.Passed),
                       SuccessRate = g.Count() > 0 ? (g.Count(r => r.Passed) * 100.0 / g.Count()) : 0
                   });

        /// <summary>
        /// Computed property: Test failures grouped by test name.
        /// </summary>
        public Dictionary<string, List<string>> FailuresByTest =>
            Results.Where(r => !r.Passed)
                   .GroupBy(r => r.TestName)
                   .ToDictionary(g => g.Key, g => g.Select(r => r.RoomName).ToList());

        /// <summary>
        /// Legacy compatibility properties for existing UI code.
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<string>> TestFailures =>
            FailuresByTest.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<string>)kvp.Value.AsReadOnly());

        public IReadOnlyDictionary<string, RoomStatistics> RoomStats =>
            RoomStatistics.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

        public IReadOnlyDictionary<string, IReadOnlyList<TestResult>> TestsByRoom =>
            ResultsByRoom.ToDictionary(kvp => kvp.Key, kvp => (IReadOnlyList<TestResult>)kvp.Value.AsReadOnly());

        public int TotalTests => Statistics.TotalTests;
        public int TotalPassed => Statistics.TotalPassed;
        public double OverallSuccessRate => Statistics.OverallSuccessRate;

        public TestReport()
        {
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Adds a test result and updates statistics.
        /// </summary>
        public void AddResult(TestResult result)
        {
            if (result == null) return;

            Results.Add(result);
            RecalculateStatistics();
        }

        /// <summary>
        /// Recalculates overall statistics from current results.
        /// </summary>
        private void RecalculateStatistics()
        {
            Statistics.TotalTests = Results.Count;
            Statistics.TotalPassed = Results.Count(r => r.Passed);
            Statistics.OverallSuccessRate = Statistics.TotalTests > 0
                ? (Statistics.TotalPassed * 100.0 / Statistics.TotalTests)
                : 0;
        }

        public override bool Equals(object obj) =>
            obj is TestReport other &&
            Timestamp.Equals(other.Timestamp) &&
            TestMethodName == other.TestMethodName;

        public override int GetHashCode() =>
            HashCode.Combine(Timestamp, TestMethodName);
    }

    /// <summary>
    /// Simple container for serializing test report collections.
    /// </summary>
    [Serializable]
    public sealed class TestReportCollection
    {
        public List<TestReport> Reports = new List<TestReport>();
    }

    /// <summary>
    /// Editor window for displaying MRUK test reports with a user-friendly interface
    /// </summary>
    public class MRUKTestReportWindow : EditorWindow
    {
        private const float COMPACT_ROW_HEIGHT = 44f;
        private const float ROOM_LABEL_WIDTH = 150f;
        private const float SPLITTER_WIDTH = 5f;

        private static MRUKTestReportWindow _instance;
        private List<TestReport> _testReports = new List<TestReport>();
        private Vector2 _scrollPosition;
        private Vector2 _roomScrollPosition;
        private Vector2 _detailsScrollPosition;
        private int _selectedReportIndex = -1;
        private string _searchFilter = "";
        private bool _showOnlyFailures;

        // View mode selection
        private enum ViewMode { ByReport, ByRoom }
        private ViewMode _currentViewMode = ViewMode.ByReport;

        // Room grouping variables
        private string _selectedRoomName = "";
        private Dictionary<string, List<TestResult>> _allTestsByRoom = new Dictionary<string, List<TestResult>>();

        // Resizable splitter state
        private float _leftPanelWidth = 300f;
        private bool _isResizing = false;
        private const float MinLeftPanelWidth = 200f;
        private const float MaxLeftPanelWidth = 600f;

        // GUI Styles
        private GUIStyle _headerStyle;
        private GUIStyle _roomStatStyle;
        private GUIStyle _alternateRowStyle1;
        private GUIStyle _alternateRowStyle2;
        private GUIStyle _selectedRowStyle;
        private GUIStyle _hoverRowStyle;
        private bool _stylesInitialized = false;

        // Grouping state for hierarchical display
        private Dictionary<string, bool> _testGroupExpanded = new Dictionary<string, bool>();

        // Cached hierarchical grouping data for performance
        private Dictionary<string, List<(TestReport report, int originalIndex)>> _cachedReportsByTestName;
        private int _lastCachedReportCount = -1;

        private GUIContent _successIcon;
        private GUIContent _failureIcon;
        private GUIContent _warningIcon;

        private static DateTime _lastNotificationTime = DateTime.MinValue;
        private const double NotificationCooldownSeconds = 5.0; // Prevent notifications within 5 seconds of each other

        [MenuItem("Meta/Tools/MRUK Test Reports")]
        public static void ShowWindow()
        {
            _instance = GetWindow<MRUKTestReportWindow>("MRUK Test Reports");
            _instance.minSize = new Vector2(600, 400);
        }


        /// <summary>
        /// Updates the room grouping data by aggregating results from all test reports
        /// </summary>
        private void UpdateRoomGroupingData()
        {
            _allTestsByRoom.Clear();

            // Aggregate test results from all reports to show complete history per room
            foreach (var report in _testReports)
            {
                foreach (var roomTests in report.TestsByRoom)
                {
                    var roomName = roomTests.Key;
                    var tests = roomTests.Value;

                    // Initialize room list if it doesn't exist
                    if (!_allTestsByRoom.ContainsKey(roomName))
                    {
                        _allTestsByRoom[roomName] = new List<TestResult>();
                    }

                    // Add tests from this report with timestamp information, excluding configuration errors
                    foreach (var test in tests)
                    {
                        // Skip configuration errors from room view as they don't represent accurate room testing
                        if (test.IsConfigurationError)
                            continue;

                        _allTestsByRoom[roomName].Add(new TestResult
                        {
                            TestName = $"{test.TestName} ({report.Timestamp:MM/dd HH:mm})",
                            RoomName = test.RoomName,
                            Passed = test.Passed,
                            ErrorMessage = test.ErrorMessage,
                            IsConfigurationError = test.IsConfigurationError
                        });
                    }
                }
            }

            // Sort tests within each room by timestamp (newest first)
            foreach (var roomName in _allTestsByRoom.Keys.ToList())
            {
                _allTestsByRoom[roomName] = _allTestsByRoom[roomName]
                    .OrderByDescending(t => t.TestName) // Since we embedded timestamp in the name
                    .ToList();
            }

            // If no room is selected or the selected room no longer exists, select the first available room
            if (string.IsNullOrEmpty(_selectedRoomName) || !_allTestsByRoom.ContainsKey(_selectedRoomName))
            {
                _selectedRoomName = _allTestsByRoom.Keys.FirstOrDefault() ?? "";
            }
        }

        private void OnEnable()
        {
            _instance = this;

            // Subscribe to test data change notifications
            MRUKTestsSettings.TestDataChanged += OnTestDataChanged;

            // Wait for the editor to be ready before loading test reports
            if (InitializeOnLoad.EditorReady)
            {
                LoadTestReports();
            }
            else
            {
                InitializeOnLoad.Register(LoadTestReports);
            }
        }

        private void OnDisable()
        {
            // Unsubscribe from test data change notifications
            MRUKTestsSettings.TestDataChanged -= OnTestDataChanged;
        }

        /// <summary>
        /// Event handler for when test data has changed in storage.
        /// Simply reloads data from storage to pick up new reports.
        /// </summary>
        private void OnTestDataChanged()
        {
            LoadTestReports();

            // Update room grouping data after loading new reports
            UpdateRoomGroupingData();

            // Force styles to re-initialize to ensure proper styling on new data
            _stylesInitialized = false;

            // Show notification for the latest report if notifications are enabled
            if (_testReports.Count > 0)
            {
                ShowTestCompletionNotification(_testReports[0]);
            }

            Repaint();
        }


        /// <summary>
        /// Load test reports using Meta XR Editor Settings
        /// </summary>
        private void LoadTestReports()
        {
            try
            {
                _testReports.Clear();

                // Load using Meta.XR.Editor.Settings (with automatic migration from old EditorPrefs key)
                var json = MRUKTestsUtils.TestReportsSetting.Value;
                if (string.IsNullOrEmpty(json))
                {
                    Debug.Log("No test reports found in settings");
                    return;
                }

                // Direct deserialization using Unity's JsonUtility - no conversion needed
                var reportList = JsonUtility.FromJson<TestReportCollection>(json);
                if (reportList?.Reports == null)
                {
                    Debug.LogWarning("Failed to deserialize test reports from settings");
                    return;
                }

                _testReports = reportList.Reports;

                // Sort by timestamp (newest first)
                _testReports.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));

                // Update room grouping data after loading reports
                UpdateRoomGroupingData();

                Debug.Log($"Successfully loaded {_testReports.Count} test reports using Meta.XR.Editor.Settings");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load test reports using Meta.XR.Editor.Settings: {ex.Message}\n{ex.StackTrace}");
                _testReports = new List<TestReport>();
            }
        }

        private void InitializeStyles()
        {
            var needsReinit = !_stylesInitialized || _headerStyle == null || _roomStatStyle == null ||
                              _alternateRowStyle1 == null || _alternateRowStyle2 == null || _selectedRowStyle == null;
            if (!needsReinit)
            {
                return;
            }

            _headerStyle = RLDSStyles.Typography.Heading2.ToGUIStyle();
            _headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

            _roomStatStyle = RLDSStyles.Typography.Body2SupportingText.ToGUIStyle();
            _roomStatStyle.normal.textColor = EditorGUIUtility.isProSkin ? RLDSStyles.Colors.TextUISecondary : new Color(0.3f, 0.3f, 0.3f);

            _successIcon = new GUIContent(UIStyles.Contents.CheckIcon);
            _failureIcon = new GUIContent(UIStyles.Contents.ErrorIcon);
            _warningIcon = new GUIContent(UIStyles.Contents.UpdateIcon);

            // Create alternating row background styles with padding
            _alternateRowStyle1 = new GUIStyle
            {
                normal =
                {
                    background = MakeTexture(2, 2,
                        EditorGUIUtility.isProSkin
                            ? new Color(0.3f, 0.3f, 0.3f, 0.3f)
                            : new Color(0.8f, 0.8f, 0.8f, 0.4f))
                },
                padding = new RectOffset(4, 4, 4, 4)
            };

            _alternateRowStyle2 = new GUIStyle
            {
                normal =
                {
                    background = MakeTexture(2, 2,
                        EditorGUIUtility.isProSkin
                            ? new Color(0.25f, 0.25f, 0.25f, 0.3f)
                            : new Color(0.9f, 0.9f, 0.9f, 0.4f))
                },
                padding = new RectOffset(4, 4, 4, 4)
            };

            // Create selected row style with clear blue highlighting
            _selectedRowStyle = new GUIStyle
            {
                normal =
                {
                    background = MakeTexture(2, 2,
                        EditorGUIUtility.isProSkin
                            ? new Color(0.2f, 0.45f, 0.8f, 0.9f) // Brighter blue for dark theme
                            : new Color(0.4f, 0.6f, 0.9f, 0.9f)) // Blue tint for light theme
                },
                padding = new RectOffset(4, 4, 4, 4),
                border = new RectOffset(1, 1, 1, 1)
            };

            // Create hover row style with subtle highlighting
            _hoverRowStyle = new GUIStyle
            {
                normal =
                {
                    background = MakeTexture(2, 2,
                        EditorGUIUtility.isProSkin
                            ? new Color(0.35f, 0.35f, 0.35f, 0.5f) // Lighter gray for dark theme
                            : new Color(0.7f, 0.7f, 0.7f, 0.5f)) // Gray for light theme
                },
                padding = new RectOffset(4, 4, 4, 4),
                border = new RectOffset(1, 1, 1, 1)
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            InitializeStyles();

            EditorGUILayout.BeginVertical();

            // Header with improved styling
            GUILayout.Space(RLDSStyles.Spacing.SpaceXS);
            EditorGUILayout.LabelField("MRUK Test Report Viewer", _headerStyle);
            GUILayout.Space(RLDSStyles.Spacing.SpaceXS);

            DrawControlsSection();
            GUILayout.Space(RLDSStyles.Spacing.SpaceXS);

            // Main content based on view mode
            if (_currentViewMode == ViewMode.ByReport)
            {
                DrawReportView();
            }
            else
            {
                DrawRoomView();
            }

            GUILayout.Space(RLDSStyles.Spacing.Space2XS);
            DrawBottomToolbar();

            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Draws the controls section with improved styling
        /// </summary>
        private void DrawControlsSection()
        {
            // Row 1: View mode and Show Only Failures together
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("View Mode:", "Choose how to display test results"), GUILayout.Width(RLDSStyles.ButtonSize.Small.MinWidth));
            var newViewMode = (ViewMode)EditorGUILayout.EnumPopup(_currentViewMode, GUILayout.Width(RLDSStyles.ButtonSize.Large.MinWidth));
            if (newViewMode != _currentViewMode)
            {
                _currentViewMode = newViewMode;
                if (_currentViewMode == ViewMode.ByRoom)
                {
                    UpdateRoomGroupingData();
                }
            }

            GUILayout.Space(RLDSStyles.Spacing.SpaceLG);
            _showOnlyFailures = EditorGUILayout.Toggle(new GUIContent("Show Only Failures", "Filter to show only failed tests"), _showOnlyFailures);

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(RLDSStyles.Spacing.Space2XS);

            // Row 2: Search box with expandable width
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(new GUIContent("Search:", "Filter test reports by test name, room name, or timestamp"), GUILayout.Width(RLDSStyles.ButtonSize.XSmall.MinWidth));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Action method for clearing all reports
        /// </summary>
        private void ClearAllReports()
        {
            _testReports.Clear();
            _selectedReportIndex = -1;
            _allTestsByRoom.Clear();
            _selectedRoomName = "";

            // Clear using Meta.XR.Editor.Settings for true persistence
            MRUKTestsUtils.TestReportsSetting.SetValue("", Origins.ProjectSettings);

            Repaint();
        }

        /// <summary>
        /// Draws the traditional report-based view
        /// </summary>
        private void DrawReportView()
        {
            EditorGUILayout.BeginHorizontal();

            // Left panel - Report list with resizable width
            EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth));
            var testReportsHeaderStyle = RLDSStyles.Typography.Heading3.ToGUIStyle();
            testReportsHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            EditorGUILayout.LabelField("Test Reports", testReportsHeaderStyle);

            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.ExpandHeight(true));

            // Use cached hierarchical grouping data for performance
            // Rebuild cache if report count changed
            if (_cachedReportsByTestName == null || _lastCachedReportCount != _testReports.Count)
            {
                _cachedReportsByTestName = new Dictionary<string, List<(TestReport report, int originalIndex)>>();

                for (int i = 0; i < _testReports.Count; i++)
                {
                    var report = _testReports[i];

                    // Get clean test name
                    var displayName = report.TestMethodName;
                    if (string.IsNullOrEmpty(displayName) || displayName == "Unknown Test")
                    {
                        var firstTest = report.Results.FirstOrDefault();
                        if (firstTest != null && !string.IsNullOrEmpty(firstTest.TestName))
                        {
                            displayName = firstTest.TestName;
                            var timestampIndex = displayName.LastIndexOf(" (", StringComparison.Ordinal);
                            if (timestampIndex > 0)
                            {
                                displayName = displayName.Substring(0, timestampIndex);
                            }
                        }
                        else
                        {
                            displayName = $"Report {report.Timestamp:HH:mm:ss}";
                        }
                    }

                    if (!_cachedReportsByTestName.ContainsKey(displayName))
                    {
                        _cachedReportsByTestName[displayName] = new List<(TestReport, int)>();
                    }
                    _cachedReportsByTestName[displayName].Add((report, i));
                }

                _lastCachedReportCount = _testReports.Count;
            }

            // Apply filters on the cached data
            var reportsByTestName = _cachedReportsByTestName
                .Where(kvp => kvp.Value.Any(r =>
                    (string.IsNullOrEmpty(_searchFilter) || ContainsSearchTerm(r.report, _searchFilter)) &&
                    (!_showOnlyFailures || r.report.TotalPassed < r.report.TotalTests)))
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value
                        .Where(r =>
                            (string.IsNullOrEmpty(_searchFilter) || ContainsSearchTerm(r.report, _searchFilter)) &&
                            (!_showOnlyFailures || r.report.TotalPassed < r.report.TotalTests))
                        .ToList());

            // Display grouped reports hierarchically
            int visibleIndex = 0;
            foreach (var testGroup in reportsByTestName)
            {
                var testName = testGroup.Key;
                var reports = testGroup.Value;

                // Ensure expanded state exists
                if (!_testGroupExpanded.ContainsKey(testName))
                {
                    _testGroupExpanded[testName] = true;
                }

                // Calculate group statistics
                var groupTotalTests = reports.Sum(r => r.report.TotalTests);
                var groupPassedTests = reports.Sum(r => r.report.TotalPassed);
                var groupSuccessRate = groupTotalTests > 0 ? (groupPassedTests * 100.0 / groupTotalTests) : 0;

                // Group header
                var hasFailures = groupPassedTests < groupTotalTests;
                var iconContent = new GUIContent(hasFailures ? UIStyles.Contents.ErrorIcon : UIStyles.Contents.CheckIcon);

                var rect = EditorGUILayout.GetControlRect(GUILayout.Height(RLDSStyles.IconSize.SizeMD));
                var foldoutRect = new Rect(rect.x, rect.y, 12, rect.height);
                var iconRect = new Rect(rect.x + 15, rect.y + 1, 16, 16);
                var labelRect = new Rect(rect.x + 35, rect.y, rect.width - 35, rect.height);

                _testGroupExpanded[testName] = EditorGUI.Foldout(foldoutRect, _testGroupExpanded[testName], "", true);

                var previousColor = GUI.contentColor;
                GUI.contentColor = hasFailures ? RLDSStyles.Colors.IconNegative : RLDSStyles.Colors.IconPositive;
                GUI.Label(iconRect, iconContent);
                GUI.contentColor = previousColor;

                EditorGUI.LabelField(labelRect, $"{testName} ({reports.Count} run{(reports.Count > 1 ? "s" : "")})", EditorStyles.boldLabel);

                // Show expanded items
                if (_testGroupExpanded[testName])
                {
                    foreach (var (report, originalIndex) in reports)
                    {
                        var isSelected = _selectedReportIndex == originalIndex;
                        var backgroundStyle = isSelected ? _selectedRowStyle : (visibleIndex % 2 == 0 ? _alternateRowStyle1 : _alternateRowStyle2);

                        var reportRect = EditorGUILayout.BeginVertical(backgroundStyle, GUILayout.Height(RLDSStyles.ButtonSize.Large.Height));

                        if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                            reportRect.Contains(Event.current.mousePosition))
                        {
                            _selectedReportIndex = originalIndex;
                            Event.current.Use();
                            GUI.changed = true;
                        }

                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(RLDSStyles.Spacing.SpaceLG);

                        var hasConfigurationErrors = report.TestsByRoom.Values.Any(tests => tests.Any(t => t.IsConfigurationError));

                        GUIContent statusIcon;
                        if (hasConfigurationErrors)
                        {
                            statusIcon = _warningIcon;
                        }
                        else if (report.TotalPassed == report.TotalTests)
                        {
                            statusIcon = _successIcon;
                        }
                        else
                        {
                            statusIcon = _failureIcon;
                        }

                        var iconColor = hasConfigurationErrors ? RLDSStyles.Colors.IconWarning :
                                       (report.TotalPassed == report.TotalTests ? RLDSStyles.Colors.IconPositive : RLDSStyles.Colors.IconNegative);

                        var prevContentColor = GUI.contentColor;
                        GUI.contentColor = iconColor;
                        GUILayout.Label(statusIcon, GUILayout.Width(RLDSStyles.IconSize.SizeSM), GUILayout.Height(RLDSStyles.IconSize.SizeSM));
                        GUI.contentColor = prevContentColor;

                        GUILayout.Space(2);

                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField($"{report.Timestamp:MM/dd HH:mm} - {report.TotalPassed}/{report.TotalTests} Passed", GUILayout.Height(RLDSStyles.IconSize.SizeMD));
                        EditorGUILayout.LabelField($"Success: {report.OverallSuccessRate:F1}%", _roomStatStyle);
                        EditorGUILayout.EndVertical();

                        EditorGUILayout.EndHorizontal();
                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);

                        visibleIndex++;
                    }
                }
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Draw splitter
            DrawSplitter();

            // Right panel - Report details
            EditorGUILayout.BeginVertical();
            var reportDetailsHeaderStyle = RLDSStyles.Typography.Heading3.ToGUIStyle();
            reportDetailsHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            EditorGUILayout.LabelField("Report Details", reportDetailsHeaderStyle);

            if (_selectedReportIndex >= 0 && _selectedReportIndex < _testReports.Count)
            {
                DrawReportDetails(_testReports[_selectedReportIndex]);
            }
            else
            {
                EditorGUILayout.LabelField("Select a report to view details", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the room-based grouping view
        /// </summary>
        private void DrawRoomView()
        {
            if (_allTestsByRoom.Count == 0)
            {
                EditorGUILayout.LabelField("No test data available. Run some tests first.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            EditorGUILayout.BeginHorizontal();

            // Left panel - Room list with resizable width
            EditorGUILayout.BeginVertical(GUILayout.Width(_leftPanelWidth));
            var roomsHeaderStyle = RLDSStyles.Typography.Heading3.ToGUIStyle();
            roomsHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            EditorGUILayout.LabelField("Rooms", roomsHeaderStyle);

            _roomScrollPosition = EditorGUILayout.BeginScrollView(_roomScrollPosition, GUILayout.ExpandHeight(true));

            var roomNames = _allTestsByRoom.Keys.ToList();
            roomNames.Sort(); // Sort room names alphabetically

            int visibleIndex = 0;
            foreach (var roomName in roomNames)
            {
                var tests = _allTestsByRoom[roomName];

                // Apply search filter
                if (!string.IsNullOrEmpty(_searchFilter) && !roomName.ToLower().Contains(_searchFilter.ToLower()))
                {
                    // Also check if any test names match the search
                    bool hasMatchingTest = tests.Any(t => t.TestName.ToLower().Contains(_searchFilter.ToLower()));
                    if (!hasMatchingTest)
                        continue;
                }

                // Apply failure filter
                if (_showOnlyFailures && tests.All(t => t.Passed))
                    continue;

                // Calculate room statistics
                var totalTests = tests.Count;
                var passedTests = tests.Count(t => t.Passed);
                var failedTests = totalTests - passedTests;
                var successRate = totalTests > 0 ? (passedTests * 100.0 / totalTests) : 0;

                var isSelected = _selectedRoomName == roomName;
                var backgroundStyle = isSelected ? _selectedRowStyle : (visibleIndex % 2 == 0 ? _alternateRowStyle1 : _alternateRowStyle2);

                var roomRect = EditorGUILayout.BeginVertical(backgroundStyle, GUILayout.Height(COMPACT_ROW_HEIGHT));

                if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                    roomRect.Contains(Event.current.mousePosition))
                {
                    _selectedRoomName = roomName;
                    Event.current.Use();
                    GUI.changed = true;
                }

                EditorGUILayout.BeginHorizontal();
                var statusIcon = failedTests == 0 ? _successIcon : _failureIcon;
                var iconColor = failedTests == 0 ? RLDSStyles.Colors.IconPositive : RLDSStyles.Colors.IconNegative;

                var prevContentColor = GUI.contentColor;
                GUI.contentColor = iconColor;
                GUILayout.Label(statusIcon, GUILayout.Width(RLDSStyles.IconSize.SizeSM), GUILayout.Height(RLDSStyles.IconSize.SizeSM));
                GUI.contentColor = prevContentColor;

                EditorGUILayout.LabelField($"{roomName}", GUILayout.Height(20));
                EditorGUILayout.EndHorizontal();

                // Success rate without color
                EditorGUILayout.LabelField($"{passedTests}/{totalTests} passed ({successRate:F1}%)", _roomStatStyle);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(2);

                visibleIndex++;
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            // Draw splitter
            DrawSplitter();

            // Right panel - Tests for selected room
            EditorGUILayout.BeginVertical();

            if (!string.IsNullOrEmpty(_selectedRoomName) && _allTestsByRoom.ContainsKey(_selectedRoomName))
            {
                DrawRoomTestDetails(_allTestsByRoom[_selectedRoomName]);
            }
            else
            {
                EditorGUILayout.LabelField("Select a room to view test details", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// Draws the test details for a specific room
        /// </summary>
        /// <param name="tests">List of tests for the room</param>
        private void DrawRoomTestDetails(List<TestResult> tests)
        {
            var scrollPosition = EditorGUILayout.BeginScrollView(Vector2.zero);

            // Room summary
            var totalTests = tests.Count;
            var passedTests = tests.Count(t => t.Passed);
            var failedTests = totalTests - passedTests;
            var successRate = totalTests > 0 ? (passedTests * 100.0 / totalTests) : 0;

            var summaryHeaderStyle = RLDSStyles.Typography.Heading3.ToGUIStyle();
            summaryHeaderStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
            EditorGUILayout.LabelField($"Room Summary: {_selectedRoomName}", summaryHeaderStyle);
            EditorGUILayout.LabelField($"Total Test Executions: {totalTests}");
            EditorGUILayout.LabelField($"Passed: {passedTests}");
            EditorGUILayout.LabelField($"Failed: {failedTests}");

            EditorGUILayout.BeginHorizontal();
            var summaryIcon = failedTests == 0 ? _successIcon : _failureIcon;
            var summaryIconColor = failedTests == 0 ? RLDSStyles.Colors.IconPositive : RLDSStyles.Colors.IconNegative;

            var prevColor = GUI.contentColor;
            GUI.contentColor = summaryIconColor;
            GUILayout.Label(summaryIcon, GUILayout.Width(RLDSStyles.IconSize.SizeSM), GUILayout.Height(RLDSStyles.IconSize.SizeSM));
            GUI.contentColor = prevColor;

            EditorGUILayout.LabelField($"Success Rate: {successRate:F1}%", EditorStyles.label);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Group tests by name and aggregate results
            EditorGUILayout.LabelField("Test Results by Type", EditorStyles.boldLabel);

            if (tests.Count == 0)
            {
                EditorGUILayout.LabelField("No tests found for this room.", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                // Group tests by their base name (removing timestamp)
                var testGroups = new Dictionary<string, List<TestResult>>();
                foreach (var test in tests)
                {
                    // Extract the base test name by removing the timestamp part
                    var baseName = test.TestName;
                    var timestampIndex = baseName.LastIndexOf(" (", StringComparison.Ordinal);
                    if (timestampIndex > 0)
                    {
                        baseName = baseName.Substring(0, timestampIndex);
                    }

                    if (!testGroups.ContainsKey(baseName))
                    {
                        testGroups[baseName] = new List<TestResult>();
                    }
                    testGroups[baseName].Add(test);
                }

                // Sort test groups alphabetically
                var sortedTestGroups = testGroups.OrderBy(kvp => kvp.Key).ToList();

                foreach (var testGroup in sortedTestGroups)
                {
                    var testName = testGroup.Key;
                    var testResults = testGroup.Value;

                    var totalRuns = testResults.Count;
                    var passedRuns = testResults.Count(t => t.Passed);
                    var failedRuns = totalRuns - passedRuns;
                    var testSuccessRate = totalRuns > 0 ? (passedRuns * 100.0 / totalRuns) : 0;

                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.BeginHorizontal();

                    var groupIcon = failedRuns == 0 ? _successIcon : _failureIcon;
                    var groupIconColor = failedRuns == 0 ? RLDSStyles.Colors.IconPositive : RLDSStyles.Colors.IconNegative;

                    var prevIconColor = GUI.contentColor;
                    GUI.contentColor = groupIconColor;
                    GUILayout.Label(groupIcon, GUILayout.Width(RLDSStyles.IconSize.SizeSM), GUILayout.Height(RLDSStyles.IconSize.SizeSM));
                    GUI.contentColor = prevIconColor;

                    var statusText = $"{passedRuns}/{totalRuns} passed ({testSuccessRate:F1}%)";

                    EditorGUILayout.LabelField($"  {testName}:", EditorStyles.boldLabel, GUILayout.ExpandWidth(true));
                    EditorGUILayout.LabelField(statusText, EditorStyles.label, GUILayout.Width(150));

                    EditorGUILayout.EndHorizontal();

                    // Show recent failures if any exist
                    var recentFailures = testResults.Where(t => !t.Passed).Take(3).ToList();
                    if (recentFailures.Count > 0)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField("", GUILayout.Width(20)); // Indent

                        EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField($"Recent Failures ({recentFailures.Count}):", EditorStyles.miniLabel);

                        foreach (var failure in recentFailures)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("", GUILayout.Width(10)); // Additional indent

                            // Extract timestamp from test name for display
                            var timestamp = "Unknown";
                            var timestampIndex = failure.TestName.LastIndexOf(" (", StringComparison.Ordinal);
                            if (timestampIndex > 0)
                            {
                                var timestampPart = failure.TestName.Substring(timestampIndex + 2);
                                if (timestampPart.EndsWith(")"))
                                {
                                    timestamp = timestampPart.Substring(0, timestampPart.Length - 1);
                                }
                            }

                            EditorGUILayout.LabelField($"• {timestamp}", EditorStyles.miniLabel, GUILayout.Width(80));

                            if (!string.IsNullOrEmpty(failure.ErrorMessage))
                            {
                                var errorStyle = new GUIStyle(EditorStyles.miniLabel)
                                {
                                    wordWrap = true,
                                    normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.7f, 0.7f) : new Color(0.8f, 0f, 0f) }
                                };
                                EditorGUILayout.LabelField(failure.ErrorMessage, errorStyle, GUILayout.ExpandWidth(true));
                            }
                            else
                            {
                                EditorGUILayout.LabelField("No error message", EditorStyles.miniLabel);
                            }

                            EditorGUILayout.EndHorizontal();
                        }

                        // Show indication if there are more failures
                        if (testResults.Count(t => !t.Passed) > 3)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.LabelField("", GUILayout.Width(10)); // Additional indent
                            EditorGUILayout.LabelField($"... and {testResults.Count(t => !t.Passed) - 3} more failures", EditorStyles.miniLabel);
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(2);
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private bool ContainsSearchTerm(TestReport report, string searchTerm)
        {
            var lowerSearch = searchTerm.ToLower();

            // Search in timestamp
            if (report.Timestamp.ToString(CultureInfo.InvariantCulture).ToLower().Contains(lowerSearch))
                return true;

            // Search in ALL test names (both passed and failed)
            foreach (var result in report.Results)
            {
                if (result.TestName.ToLower().Contains(lowerSearch))
                    return true;
            }

            // Search in room names
            foreach (var roomStat in report.RoomStats.Values)
            {
                if (roomStat.RoomName.ToLower().Contains(lowerSearch))
                    return true;
            }

            return false;
        }

        private void DrawReportDetails(TestReport report)
        {
            _detailsScrollPosition = EditorGUILayout.BeginScrollView(_detailsScrollPosition);

            // Get clean test name
            var displayName = report.TestMethodName;
            if (string.IsNullOrEmpty(displayName) || displayName == "Unknown Test")
            {
                var firstTest = report.Results.FirstOrDefault();
                if (firstTest != null && !string.IsNullOrEmpty(firstTest.TestName))
                {
                    displayName = firstTest.TestName;
                    var timestampIndex = displayName.LastIndexOf(" (", StringComparison.Ordinal);
                    if (timestampIndex > 0)
                    {
                        displayName = displayName.Substring(0, timestampIndex);
                    }
                }
                else
                {
                    displayName = $"Report {report.Timestamp:HH:mm:ss}";
                }
            }

            EditorGUILayout.BeginHorizontal();
            var overallIcon = report.TotalPassed == report.TotalTests ? _successIcon : _failureIcon;
            var overallIconColor = report.TotalPassed == report.TotalTests ? RLDSStyles.Colors.IconPositive : RLDSStyles.Colors.IconNegative;

            var prevColor = GUI.contentColor;
            GUI.contentColor = overallIconColor;
            GUILayout.Label(overallIcon, GUILayout.Width(RLDSStyles.IconSize.SizeSM), GUILayout.Height(RLDSStyles.IconSize.SizeSM));
            GUI.contentColor = prevColor;

            EditorGUILayout.LabelField($"{displayName}", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Timestamp: {report.Timestamp:yyyy-MM-dd HH:mm:ss}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Results: {report.TotalPassed}/{report.TotalTests} passed ({report.OverallSuccessRate:F1}%)", EditorStyles.label);

            EditorGUILayout.Space(RLDSStyles.Spacing.SpaceXS);

            // Show failures prominently at the top if there are any
            if (report.TestFailures.Count > 0)
            {
                EditorGUILayout.LabelField("Failures", EditorStyles.boldLabel);

                foreach (var testFailure in report.TestFailures)
                {
                    var testName = testFailure.Key;
                    var failedRooms = testFailure.Value;

                    // Check if this is a configuration error
                    var isConfigurationError = false;
                    var configurationErrorMessage = "";
                    string detailedErrorMessage = "";

                    if (report.TestsByRoom.TryGetValue("Configuration", out var configTests))
                    {
                        var configTest = configTests.FirstOrDefault(t => t.TestName == testName);
                        if (configTest != null && configTest.IsConfigurationError)
                        {
                            isConfigurationError = true;
                            configurationErrorMessage = configTest.ErrorMessage;
                        }
                    }

                    // Get detailed error message from first failed test
                    if (!isConfigurationError && failedRooms.Count > 0)
                    {
                        var firstFailedRoom = failedRooms[0];
                        if (report.TestsByRoom.TryGetValue(firstFailedRoom, out var roomTests))
                        {
                            var failedTest = roomTests.FirstOrDefault(t => t.TestName == testName && !t.Passed);
                            if (failedTest != null && !string.IsNullOrEmpty(failedTest.ErrorMessage))
                            {
                                detailedErrorMessage = failedTest.ErrorMessage;
                            }
                        }
                    }

                    EditorGUILayout.BeginVertical("box");

                    EditorGUILayout.BeginHorizontal();
                    var icon = isConfigurationError ? _warningIcon : _failureIcon;
                    var failureIconColor = isConfigurationError ? RLDSStyles.Colors.IconWarning : RLDSStyles.Colors.IconNegative;

                    var prevIconColor = GUI.contentColor;
                    GUI.contentColor = failureIconColor;
                    GUILayout.Label(icon, GUILayout.Width(RLDSStyles.IconSize.SizeSM), GUILayout.Height(RLDSStyles.IconSize.SizeSM));
                    GUI.contentColor = prevIconColor;

                    EditorGUILayout.LabelField($"{testName}", EditorStyles.boldLabel);
                    EditorGUILayout.EndHorizontal();

                    if (isConfigurationError)
                    {
                        EditorGUILayout.LabelField("Configuration Error", EditorStyles.miniBoldLabel);
                        if (!string.IsNullOrEmpty(configurationErrorMessage))
                        {
                            var errorStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                            {
                                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.7f, 0.7f) : new Color(0.8f, 0f, 0f) }
                            };
                            EditorGUILayout.LabelField(configurationErrorMessage, errorStyle);
                        }
                    }
                    else
                    {
                        EditorGUILayout.LabelField($"Failed in: {string.Join(", ", failedRooms)}", EditorStyles.label);

                        if (!string.IsNullOrEmpty(detailedErrorMessage))
                        {
                            EditorGUILayout.Space(5);
                            EditorGUILayout.LabelField("Error Details:", EditorStyles.miniBoldLabel);
                            var errorStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
                            {
                                font = EditorStyles.miniFont,
                                normal = { textColor = EditorGUIUtility.isProSkin ? new Color(1f, 0.7f, 0.7f) : new Color(0.8f, 0f, 0f) }
                            };
                            EditorGUILayout.LabelField(detailedErrorMessage, errorStyle);
                        }
                    }

                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(RLDSStyles.Spacing.Space2XS);
                }

                EditorGUILayout.Space(RLDSStyles.Spacing.SpaceXS);
            }

            // Condensed room statistics
            if (report.RoomStats.Count > 0)
            {
                EditorGUILayout.LabelField("Room Results", EditorStyles.boldLabel);

                foreach (var roomStat in report.RoomStats.Values)
                {
                    EditorGUILayout.BeginHorizontal();

                    var roomIcon = roomStat.Failed == 0 ? _successIcon : _failureIcon;
                    var roomIconColor = roomStat.Failed == 0 ? RLDSStyles.Colors.IconPositive : RLDSStyles.Colors.IconNegative;

                    var prevRoomIconColor = GUI.contentColor;
                    GUI.contentColor = roomIconColor;
                    GUILayout.Label(roomIcon, GUILayout.Width(RLDSStyles.IconSize.SizeSM), GUILayout.Height(RLDSStyles.IconSize.SizeSM));
                    GUI.contentColor = prevRoomIconColor;

                    EditorGUILayout.LabelField($"{roomStat.RoomName}:", GUILayout.Width(ROOM_LABEL_WIDTH));
                    EditorGUILayout.LabelField($"{roomStat.Passed}/{roomStat.Total} ({roomStat.SuccessRate:F1}%)", EditorStyles.label);

                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// Shows a notification about test completion using Meta.XR.Editor.Notifications
        /// </summary>
        /// <param name="reportData">The test report data to summarize in the notification</param>
        private void ShowTestCompletionNotification(TestReport reportData)
        {
            var hasFailures = reportData.TotalPassed < reportData.TotalTests;

            // Auto-open window on failure if enabled
            if (hasFailures && MRUKTestsSettings.Instance.AutoOpenOnFailure)
            {
                ShowWindow();
            }

            // Show notification if enabled
            if (!MRUKTestsSettings.Instance.EnableNotifications)
            {
                return;
            }

            // Check cooldown to prevent spam notifications
            var now = DateTime.Now;
            var timeSinceLastNotification = (now - _lastNotificationTime).TotalSeconds;
            if (timeSinceLastNotification < NotificationCooldownSeconds)
            {
                Debug.Log($"Skipping notification due to cooldown. {timeSinceLastNotification:F1}s since last notification (cooldown: {NotificationCooldownSeconds}s)");
                return;
            }

            try
            {
                var notification = new Notification($"mruk_test_results_{DateTime.Now.Ticks}")
                {
                    Duration = hasFailures ? 10.0f : 5.0f,
                    ShowCloseButton = true
                };

                var notificationTitle = "MRUK Test Results Ready";
                var summary = hasFailures
                    ? "Test completed with failures"
                    : "All tests passed successfully";

                var items = new List<IUserInterfaceItem>
                {
                    new Label(notificationTitle, new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 }),
                    new Label(summary, EditorStyles.label),
                    new Button(new ActionLinkDescription
                    {
                        Content = new GUIContent("View Detailed Report"),
                        Action = ShowWindow
                    })
                };

                if (hasFailures)
                {
                    items.Add(new Button(new ActionLinkDescription
                    {
                        Content = new GUIContent("View Failures Only"),
                        Action = () =>
                        {
                            ShowWindow();
                            if (_instance != null)
                            {
                                _instance._showOnlyFailures = true;
                                _instance.Repaint();
                            }
                        }
                    }));
                }

                notification.Items = items;
                notification.Enqueue(Origins.Notification);

                _lastNotificationTime = now;

                Debug.Log("MRUK test results notification sent");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to show test completion notification: {ex.Message}");
            }
        }

        private void ExportReportsToFile()
        {
            if (_testReports.Count == 0)
            {
                EditorUtility.DisplayDialog("No Reports", "No test reports to export.", "OK");
                return;
            }

            var path = EditorUtility.SaveFilePanel("Export Test Reports", "", "MRUK_TestReports", "txt");
            if (string.IsNullOrEmpty(path))
                return;

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("MRUK Test Reports Export");
                sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine(new string('=', 50));
                sb.AppendLine();

                foreach (var report in _testReports)
                {
                    sb.AppendLine($"Test Method: {report.TestMethodName}");
                    sb.AppendLine($"Report: {report.Timestamp:yyyy-MM-dd HH:mm:ss}");
                    sb.AppendLine($"Total Tests: {report.TotalTests}");
                    sb.AppendLine($"Passed: {report.TotalPassed}");
                    sb.AppendLine($"Failed: {report.TotalTests - report.TotalPassed}");
                    sb.AppendLine($"Success Rate: {report.OverallSuccessRate:F1}%");
                    sb.AppendLine();

                    if (report.TestFailures.Count > 0)
                    {
                        sb.AppendLine("Test Failures:");
                        foreach (var testFailure in report.TestFailures)
                        {
                            var testName = testFailure.Key;
                            var failedRooms = testFailure.Value;
                            sb.AppendLine($"  {testName}: Failed in {string.Join(", ", failedRooms)}");
                        }
                        sb.AppendLine();
                    }

                    sb.AppendLine("Per-Room Statistics:");
                    foreach (var roomStat in report.RoomStats.Values)
                    {
                        sb.AppendLine($"  {roomStat.RoomName}: {roomStat.Passed}/{roomStat.Total} ({roomStat.SuccessRate:F1}%)");
                    }
                    sb.AppendLine(new string('-', 30));
                    sb.AppendLine();
                }

                File.WriteAllText(path, sb.ToString());
                EditorUtility.DisplayDialog("Export Complete", $"Reports exported to:\n{path}", "OK");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("Export Failed", $"Failed to export reports:\n{ex.Message}", "OK");
            }
        }

        /// <summary>
        /// Draws a resizable splitter between the left and right panels
        /// </summary>
        private void DrawSplitter()
        {
            var splitterRect = EditorGUILayout.GetControlRect(GUILayout.Width(5), GUILayout.ExpandHeight(true));
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0 &&
                splitterRect.Contains(Event.current.mousePosition))
            {
                _isResizing = true;
                Event.current.Use();
            }

            if (_isResizing)
            {
                if (Event.current.type == EventType.MouseDrag)
                {
                    _leftPanelWidth += Event.current.delta.x;
                    _leftPanelWidth = Mathf.Clamp(_leftPanelWidth, MinLeftPanelWidth, MaxLeftPanelWidth);
                    Event.current.Use();
                    Repaint();
                }
                else if (Event.current.type == EventType.MouseUp)
                {
                    _isResizing = false;
                    Event.current.Use();
                }
            }

            // Draw the splitter visual indicator
            var oldColor = GUI.color;
            GUI.color = EditorGUIUtility.isProSkin ? Color.gray : Color.black;
            GUI.DrawTexture(new Rect(splitterRect.x + 2, splitterRect.y, 1, splitterRect.height),
                EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;
        }

        /// <summary>
        /// Draws the bottom toolbar with action buttons
        /// </summary>
        private void DrawBottomToolbar()
        {
            var oldColor = GUI.color;
            GUI.color = EditorGUIUtility.isProSkin ? Color.gray : Color.black;
            var separatorRect = EditorGUILayout.GetControlRect(GUILayout.Height(1));
            GUI.DrawTexture(separatorRect, EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;

            GUILayout.Space(RLDSStyles.Spacing.SpaceLG);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(RLDSStyles.Spacing.SpaceSM);

            var clearButton = new Button(new ActionLinkDescription
            {
                Content = new GUIContent("Clear Reports", "Delete all stored test reports"),
                Action = ClearAllReports
            });
            clearButton.Style = RLDSStyles.Buttons.Primary;
            clearButton.Draw();

            GUILayout.Space(RLDSStyles.Spacing.SpaceXS);

            var exportButton = new Button(new ActionLinkDescription
            {
                Content = new GUIContent("Export to File", "Export test reports to a text file"),
                Action = ExportReportsToFile
            });
            exportButton.Style = RLDSStyles.Buttons.Primary;
            exportButton.Draw();

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(RLDSStyles.Spacing.SpaceLG);
        }

        /// <summary>
        /// Helper method to create a texture for GUI styling
        /// </summary>
        /// <param name="width">Width of the texture</param>
        /// <param name="height">Height of the texture</param>
        /// <param name="color">Color to fill the texture</param>
        /// <returns>Created texture</returns>
        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }
    }
}
