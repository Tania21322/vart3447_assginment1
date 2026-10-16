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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Meta.XR.Editor.Callbacks;
using Meta.XR.Telemetry;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Meta.XR.MRUtilityKit.Tests
{
    /// <summary>
    /// Represents the result of a test execution on a specific room
    /// </summary>
    [Serializable]
    public class TestResult
    {
        [SerializeField] public string TestName = string.Empty;
        [SerializeField] public string RoomName = string.Empty;
        [SerializeField] public bool Passed;
        [SerializeField] public string ErrorMessage = string.Empty;
        [SerializeField] public bool IsConfigurationError;

        public override bool Equals(object obj) =>
            obj is TestResult other &&
            TestName == other.TestName &&
            RoomName == other.RoomName &&
            Passed == other.Passed;

        public override int GetHashCode() =>
            HashCode.Combine(TestName, RoomName, Passed);
    }

    /// <summary>
    /// Manages test result tracking and reporting for room-based tests
    /// </summary>
    public class TestResultTracker
    {
        private readonly Dictionary<string, List<string>> _testFailures = new Dictionary<string, List<string>>();
        private readonly List<TestResult> _allResults = new List<TestResult>();

        public void RecordTestResult(string testName, string roomName, bool passed, string errorMessage = null, Exception exception = null, bool isConfigurationError = false)
        {
            var result = new TestResult
            {
                TestName = testName,
                RoomName = roomName,
                Passed = passed,
                ErrorMessage = errorMessage ?? exception?.Message,
                IsConfigurationError = isConfigurationError
            };

            _allResults.Add(result);

            if (passed)
            {
                return;
            }

            if (!_testFailures.ContainsKey(testName))
            {
                _testFailures[testName] = new List<string>();
            }

            _testFailures[testName].Add(roomName);
        }

        public void GenerateFailureReport()
        {
            var report = new StringBuilder();
            report.AppendLine("=== TEST FAILURE REPORT ===");
            report.AppendLine();

            if (_testFailures.Count == 0)
            {
                report.AppendLine("All tests passed across all rooms!");
            }
            else
            {
                report.AppendLine($"Found {_testFailures.Count} test(s) with failures:");
                report.AppendLine();

                foreach (var testFailure in _testFailures)
                {
                    var testName = testFailure.Key;
                    var failedRooms = testFailure.Value;

                    report.AppendLine($"Test: {testName}");
                    report.AppendLine($"   Failed in {failedRooms.Count} room(s): {string.Join(", ", failedRooms)}");

                    // Add error details for the first failure
                    var firstFailure = _allResults.Find(r => r.TestName == testName && !r.Passed);
                    if (firstFailure != null && !string.IsNullOrEmpty(firstFailure.ErrorMessage))
                    {
                        report.AppendLine($"   Error: {firstFailure.ErrorMessage}");
                    }
                    report.AppendLine();
                }
            }

            GenerateSummaryStatistics(report);

            Debug.Log(report.ToString());

            SendReportToEditorWindow();
        }

        /// <summary>
        /// Sends the current test results to the MRUK Test Report Editor Window using the event system.
        /// This replaces the previous reflection-based approach for better performance and maintainability.
        /// </summary>
        private void SendReportToEditorWindow()
        {
#if UNITY_EDITOR
            try
            {
                Debug.Log($"[SendReportToEditorWindow] Sending report with {_testFailures.Count} test failures and {_allResults.Count} results");

                MRUKTestsSettings.SaveTestResults(_testFailures, _allResults);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SendReportToEditorWindow] Failed to send report via event system: {ex.Message}\n{ex.StackTrace}");
            }
#endif
        }

        private void GenerateSummaryStatistics(StringBuilder report)
        {
            report.AppendLine("=== SUMMARY STATISTICS ===");

            var roomStats = new Dictionary<string, (int total, int passed, int failed)>();

            foreach (var result in _allResults)
            {
                if (!roomStats.ContainsKey(result.RoomName))
                {
                    roomStats[result.RoomName] = (0, 0, 0);
                }

                var stats = roomStats[result.RoomName];
                stats.total++;
                if (result.Passed)
                    stats.passed++;
                else
                    stats.failed++;
                roomStats[result.RoomName] = stats;
            }

            report.AppendLine();
            report.AppendLine("Per-Room Statistics:");
            foreach (var roomStat in roomStats)
            {
                var roomName = roomStat.Key;
                var (total, passed, _) = roomStat.Value;
                var successRate = total > 0 ? (passed * 100.0 / total) : 0;

                report.AppendLine($"  {roomName}: {passed}/{total} passed ({successRate:F1}% success rate)");
            }

            var totalTests = _allResults.Count;
            var totalPassed = _allResults.FindAll(r => r.Passed).Count;
            var overallSuccessRate = totalTests > 0 ? (totalPassed * 100.0 / totalTests) : 0;

            report.AppendLine();
            report.AppendLine($"Overall: {totalPassed}/{totalTests} passed ({overallSuccessRate:F1}% success rate)");
        }

        public void Reset()
        {
            _testFailures.Clear();
            _allResults.Clear();
        }

        public List<TestResult> GetAllResults()
        {
            return _allResults;
        }
    }
    public class MRUKTestBase
    {
        /// <summary>
        /// Default timeout in milliseconds for async operations.
        /// Used to prevent tests from hanging indefinitely when waiting for async operations.
        /// </summary>
        protected const int DefaultTimeoutMs = 10000;

        /// <summary>
        /// Timeout in seconds for LoadSceneFromPrefabAndWait operations.
        /// Can be overridden by derived classes to adjust timeout behavior.
        /// </summary>
        protected virtual float LoadSceneFromPrefabTimeoutSeconds => 30f;

        /// <summary>
        /// Path to an empty scene used for unloading all other scenes.
        /// This scene is loaded when cleaning up test environments.
        /// </summary>
        private const string EmptyScene = "Packages/com.meta.xr.mrutilitykit/Tests/Empty.unity";

        private string _sceneToTest = "Packages/com.meta.xr.mrutilitykit/Tests/MRUKTestScene.unity";

        protected virtual string SceneToTest
        {
            get => _sceneToTest;
            set => _sceneToTest = value;
        }

        protected MRUKRoom CurrentRoom;

        // Test result tracking
        protected TestResultTracker ResultTracker { get; private set; } = new TestResultTracker();

        // Flag to track if a report has already been generated to prevent duplicates
        private bool _reportGenerated;

        /// <summary>
        /// Callback that gets executed before each room test for custom setup operations.
        /// This allows derived classes or external code to register setup logic that runs
        /// before each room test begins. Called for every room, including the first one.
        /// </summary>
        public Action<string, MRUKRoom> RoomSetUp { get; set; }

        /// <summary>
        /// Callback that gets executed after each room test for custom cleanup operations.
        /// This allows derived classes or external code to register cleanup logic that runs
        /// after each room test is completed. Called for every room, including the last one.
        /// </summary>
        public Action<string, MRUKRoom> RoomTearDown { get; set; }

        private bool _awaitForMRUKInitialization = true;

        protected virtual bool AwaitForMRUKInitialization
        {
            get => _awaitForMRUKInitialization;
            set => _awaitForMRUKInitialization = value;
        }

        [UnitySetUp]
        public virtual IEnumerator SetUp()
        {
            // Reset report generation flag for each test
            _reportGenerated = false;

            yield return LoadScene(SceneToTest);

            // Set up MRUK instance for tests that need it
            yield return SetUpMRUKInstance();
        }

        [UnityTearDown]
        public virtual IEnumerator TearDown()
        {
            // Generate a final report if there are any recorded failures and no report has been generated yet
            var allResults = ResultTracker.GetAllResults();
            if (allResults.Count > 0 && !_reportGenerated)
            {
                var hasFailures = allResults.Exists(r => !r.Passed);
                if (hasFailures)
                {
                    Debug.Log("Generating final test report from TearDown...");
                    GenerateTestFailureReport();
                }
            }
            yield return UnloadScene();
        }

        /// <summary>
        /// Loads a scene asynchronously in play mode.
        /// This method handles the scene loading process and optionally waits for MRUK initialization.
        /// </summary>
        /// <param name="sceneToLoad">Path to the scene to load.</param>
        /// <param name="awaitMRUKInit">If true, waits for MRUK to initialize after loading the scene.</param>
        /// <returns>An IEnumerator for use with Unity's coroutine system to handle the asynchronous operation.</returns>
        protected IEnumerator LoadScene(string sceneToLoad)
        {
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(sceneToLoad,
                new LoadSceneParameters(LoadSceneMode.Single));
        }

        /// <summary>
        /// Runs a test method on all rooms defined in MRUKTestsSettings, passing the current room to the test method.
        /// Each room is loaded sequentially and the test method is executed once per room.
        /// Tracks test results and generates a failure report at the end.
        /// If DataSource is set to a JSON, it uses SceneJsons instead of RoomPrefabs.
        /// </summary>
        /// <param name="testMethod">The test method to run on each room, taking a MRUKRoom parameter.</param>
        /// <param name="customSettings">Optional custom settings to use instead of the project's MRUKTestsSettings.Instance. When provided, MRUK will be re-initialized with these settings.</param>
        /// <param name="testName">The name of the test (automatically populated with the calling method name).</param>
        /// <returns>An enumerator for the coroutine.</returns>
        protected IEnumerator RunTestOnAllScenes(Func<MRUKRoom, IEnumerator> testMethod,
            MRUKTestsSettings customSettings = null,
            [System.Runtime.CompilerServices.CallerMemberName] string testName = null)
        {
            if (testMethod == null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            var unifiedEvent = new UnifiedEventData(TelemetryConstants.EventName.RunTestOnAllScenes);
            unifiedEvent.SendMRUKEvent();

            ResultTracker.Reset();

            // Use the provided test name (from CallerMemberName) or fallback to extracting from the method
            var testMethodName = !string.IsNullOrEmpty(testName)
                ? testName
                : ExtractCleanMethodName(testMethod.Method.Name);

            // If custom settings are provided, re-initialize MRUK with those settings
            if (customSettings != null)
            {
                Debug.Log($"Re-initializing MRUK with custom settings for test: {testMethodName}");
                yield return ReinitializeMRUKWithCustomSettings(customSettings);
            }

            if (!MRUK.Instance.IsInitialized)
            {
                NUnit.Framework.Assert.Fail("MRUK instance failed to initialize");
            }

            // Check if we should use JSON scenes or room prefabs
            var useJsonScenes = MRUK.Instance.SceneSettings.DataSource == MRUK.SceneDataSource.Json;

            if (useJsonScenes)
            {
                var sceneJsons = MRUK.Instance.SceneSettings.SceneJsons;
                if (sceneJsons == null || sceneJsons.Length == 0)
                {
                    // Record this as a configuration error and generate report instead of just failing
                    var configErrorMessage = "No scene JSONs provided for testing when DataSource is set to Json";
                    ResultTracker.RecordTestResult(testMethodName, testMethodName, false, configErrorMessage, null, true);
                    GenerateTestFailureReport();

                    NUnit.Framework.Assert.Fail(configErrorMessage);
                }

                for (var i = 0; i < sceneJsons.Length; i++)
                {
                    var sceneJson = sceneJsons[i];
                    if (sceneJson == null)
                    {
                        Debug.LogWarning($"Scene JSON at index {i} is null, skipping");
                        continue;
                    }

                    var sceneName = sceneJson.name;

                    yield return LoadSceneFromJsonStringAndWait(sceneJson.text);

                    CurrentRoom = MRUK.Instance.GetCurrentRoom();
                    if (CurrentRoom == null)
                    {
                        Debug.LogError($"Failed to get current room after loading JSON scene: {sceneName}");
                        ResultTracker.RecordTestResult(testMethodName, sceneName, false, "Failed to get current room after loading JSON scene");

                        continue;
                    }

                    yield return ExecuteTestWithResultTracking(testMethod, testMethodName, sceneName, CurrentRoom);
                }
            }
            else
            {
                var roomPrefabs = MRUK.Instance.SceneSettings.RoomPrefabs;
                if (roomPrefabs == null || roomPrefabs.Length == 0)
                {
                    var configErrorMessage = "No room prefabs provided for testing in MRUKTestsSettings";
                    ResultTracker.RecordTestResult(testMethodName, testMethodName, false, configErrorMessage, null, true);
                    GenerateTestFailureReport();

                    NUnit.Framework.Assert.Fail(configErrorMessage);
                }

                for (var i = 0; i < roomPrefabs.Length; i++)
                {
                    var roomPrefab = roomPrefabs[i];
                    if (roomPrefab == null)
                    {
                        Debug.LogWarning($"Room prefab at index {i} is null, skipping");
                        continue;
                    }

                    yield return LoadSceneFromPrefabAndWait(roomPrefab, clearSceneFirst: true);

                    CurrentRoom = MRUK.Instance.GetCurrentRoom();
                    if (CurrentRoom == null)
                    {
                        Debug.LogError($"Failed to get current room after loading prefab: {roomPrefab.name}");
                        ResultTracker.RecordTestResult(testMethodName, roomPrefab.name, false, "Failed to get current room after loading prefab");
                        continue;
                    }

                    yield return ExecuteTestWithResultTracking(testMethod, testMethodName, roomPrefab.name, CurrentRoom);
                }
            }
            // Generate and display the test failure report
            GenerateTestFailureReport();
            Debug.Log("Room tests completed.");
        }

        /// <summary>
        /// Executes a test method with proper exception handling and result tracking.
        /// </summary>
        /// <param name="testMethod">The test method to execute</param>
        /// <param name="testMethodName">Name of the test method for tracking</param>
        /// <param name="roomName">Name of the room being tested</param>
        /// <param name="room">The room instance to pass to the test</param>
        /// <returns>Coroutine for test execution</returns>
        private IEnumerator ExecuteTestWithResultTracking(Func<MRUKRoom, IEnumerator> testMethod, string testMethodName, string roomName, MRUKRoom room)
        {
            var testPassed = true;
            string errorMessage = null;
            Exception caughtException = null;

            try
            {
                RoomSetUp?.Invoke(roomName, room);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing RoomSetUp callback for room {roomName}: {ex.Message}");
                testPassed = false;
                errorMessage = $"RoomSetUp callback failed: {ex.Message}";
                caughtException = ex;
            }

            IEnumerator testCoroutine = null;
            if (testPassed)
            {
                try
                {
                    testCoroutine = testMethod(room);
                }
                catch (Exception ex)
                {
                    testPassed = false;
                    errorMessage = $"Exception creating test coroutine: {ex.Message}";
                    caughtException = ex;
                    Debug.LogError($"Test coroutine creation failed in room {roomName}: {ex.Message}");
                }
            }

            // Execute the coroutine if it was created successfully
            if (testCoroutine != null && testPassed)
            {
                yield return ExecuteCoroutineWithExceptionHandling(testCoroutine, roomName,
                    (passed, error, exception) =>
                    {
                        testPassed = passed;
                        errorMessage = error;
                        caughtException = exception;
                    });
            }
            // Record the test result
            ResultTracker.RecordTestResult(testMethodName, roomName, testPassed, errorMessage, caughtException);

            // Log the failure but don't generate a report yet - wait until all rooms are tested
            if (!testPassed)
            {
                Debug.LogWarning($"Test {testMethodName} failed in room {roomName}. Report will be generated after all rooms are tested.");
            }

            CleanupBetweenRooms(roomName, room);
        }

        /// <summary>
        /// Executes a coroutine with exception handling, avoiding yield return in try-catch blocks.
        /// </summary>
        /// <param name="coroutine">The coroutine to execute</param>
        /// <param name="roomName">Name of the room for logging</param>
        /// <param name="resultCallback">Callback to report the result</param>
        /// <returns>Coroutine for execution</returns>
        private IEnumerator ExecuteCoroutineWithExceptionHandling(IEnumerator coroutine, string roomName,
            Action<bool, string, Exception> resultCallback)
        {
            var hasException = false;
            string errorMessage = null;
            Exception caughtException = null;

            // Wrap the coroutine execution to catch exceptions
            var wrappedCoroutine = WrapCoroutineForExceptionHandling(coroutine,
                (error, exception) =>
                {
                    hasException = true;
                    errorMessage = error;
                    caughtException = exception;
                });

            yield return wrappedCoroutine;

            // Report the result
            if (hasException)
            {
                Debug.LogError($"Test execution failed in room {roomName}: {errorMessage}");
                resultCallback(false, errorMessage, caughtException);
            }
            else
            {
                resultCallback(true, null, null);
            }
        }

        /// <summary>
        /// Wraps a coroutine to catch exceptions during execution.
        /// </summary>
        /// <param name="coroutine">The coroutine to wrap</param>
        /// <param name="exceptionCallback">Callback for when an exception occurs</param>
        /// <returns>Wrapped coroutine</returns>
        private IEnumerator WrapCoroutineForExceptionHandling(IEnumerator coroutine,
            Action<string, Exception> exceptionCallback)
        {
            while (true)
            {
                object current;
                try
                {
                    if (!coroutine.MoveNext())
                        break;
                    current = coroutine.Current;
                }
                catch (AssertionException assertEx)
                {
                    exceptionCallback($"Assertion failed: {assertEx.Message}", assertEx);
                    yield break;
                }
                catch (NUnit.Framework.AssertionException nunitAssertEx)
                {
                    exceptionCallback($"NUnit assertion failed: {nunitAssertEx.Message}", nunitAssertEx);
                    yield break;
                }
                catch (Exception ex)
                {
                    exceptionCallback($"Exception: {ex.Message}", ex);
                    yield break;
                }

                yield return current;
            }
        }

        /// <summary>
        /// Generates and displays the test failure report. This method is wrapped separately
        /// so it can be moved or customized as needed.
        /// </summary>
        protected virtual void GenerateTestFailureReport()
        {
            if (_reportGenerated)
            {
                return;
            }

            _reportGenerated = true;
            ResultTracker.GenerateFailureReport();
        }

        /// <summary>
        /// Extracts a clean method name from compiler-generated method names.
        /// Handles cases where the C# compiler generates names surrounded by angle brackets for async methods,
        /// lambdas, and other constructs.
        /// </summary>
        /// <param name="rawMethodName">The raw method name from Method.Name</param>
        /// <returns>A cleaned method name suitable for display</returns>
        private static string ExtractCleanMethodName(string rawMethodName)
        {
            if (string.IsNullOrEmpty(rawMethodName))
            {
                return rawMethodName;
            }

            var angleStart = rawMethodName.IndexOf('<');
            var angleEnd = rawMethodName.IndexOf('>');

            if (angleStart >= 0 && angleEnd > angleStart)
            {
                return rawMethodName.Substring(angleStart + 1, angleEnd - angleStart - 1);
            }

            // If no angle brackets found, return the original name
            return rawMethodName;
        }

        /// <summary>
        /// Gets the name of the calling UnityTest method from the stack trace.
        /// This finds the actual test method (marked with [UnityTest]) that called RunTestOnAllScenes.
        /// </summary>
        /// <returns>The UnityTest method name if found, otherwise null</returns>
        private string GetCallingTestMethodName()
        {
            try
            {
                var stackTrace = new System.Diagnostics.StackTrace();
                var frames = stackTrace.GetFrames();

                // Walk up the stack to find a method that has the [UnityTest] attribute
                foreach (var frame in frames)
                {
                    var method = frame.GetMethod();
                    if (method == null) continue;

                    // Check if this method has the UnityTest attribute
                    var attributes = method.GetCustomAttributes(typeof(UnityTestAttribute), false);
                    if (attributes != null && attributes.Length > 0)
                    {
                        return method.Name;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to get calling test method name from stack trace: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Creates a deep copy of a UnityEvent using Unity's serialization system,
        /// preserving all persistent listeners while preventing runtime pollution.
        /// </summary>
        /// <typeparam name="T">The specific UnityEvent type</typeparam>
        /// <param name="source">The source UnityEvent to copy</param>
        /// <returns>A new UnityEvent instance with all persistent listeners copied</returns>
        private T DeepCopyUnityEvent<T>(T source) where T : UnityEngine.Events.UnityEventBase, new()
        {
            if (source == null)
            {
                return null;
            }

            try
            {
                // Use Unity's serialization system to create a deep copy
                var json = JsonUtility.ToJson(source);
                var copy = new T();
                JsonUtility.FromJsonOverwrite(json, copy);
                return copy;
            }
            catch (Exception ex)
            {
                Debug.LogWarning(
                    $"Failed to deep copy UnityEvent of type {typeof(T).Name}: {ex.Message}. Creating new empty event.");
                return new T();
            }
        }

        protected IEnumerator SetUpMRUKInstance()
        {
            if (MRUK.Instance != null)
            {
                if (!MRUK.Instance.IsInitialized && AwaitForMRUKInitialization)
                {
                    yield return new WaitUntil(() => MRUK.Instance.IsInitialized);
                }
            }
            else
            {
                yield return CreateAndConfigureMRUKInstance();
            }
        }

        /// <summary>
        /// Creates and configures a new MRUK instance with proper event handlers and settings.
        /// This method consolidates the MRUK setup logic to ensure consistency across different
        /// loading scenarios and prevents missing event handler registration.
        /// </summary>
        /// <returns>An IEnumerator for use with Unity's coroutine system.</returns>
        private IEnumerator CreateAndConfigureMRUKInstance()
        {
            MRUK mruk = null;
            if (MRUK.Instance == null)
            {
                var mrukGameObject = Object.Instantiate(MRUKTestsSettings.MrukPrefab);
                mruk = mrukGameObject.GetComponent<MRUK>();
                if (mruk == null)
                {
                    NUnit.Framework.Assert.Fail("MRUK component not found on instantiated prefab");
                }
            }

            yield return new WaitUntil(() => InitializeOnLoad.EditorReady);

            var sceneSettings = MRUKTestsSettings.Instance.SceneSettings;
            if (sceneSettings.RoomPrefabs == null || sceneSettings.RoomPrefabs.Length == 0)
            {
                NUnit.Framework.Assert.Fail("No room prefabs provided in MRUKTestsSettings");
            }

            mruk.SceneLoadedEvent = DeepCopyUnityEvent(MRUKTestsSettings.Instance.SceneLoadedEvent);
            mruk.RoomCreatedEvent = DeepCopyUnityEvent(MRUKTestsSettings.Instance.RoomCreatedEvent);
            mruk.RoomUpdatedEvent = DeepCopyUnityEvent(MRUKTestsSettings.Instance.RoomUpdatedEvent);
            mruk.RoomRemovedEvent = DeepCopyUnityEvent(MRUKTestsSettings.Instance.RoomRemovedEvent);
            mruk.EnableWorldLock = MRUKTestsSettings.Instance.WorldLockEnabled;
            mruk.SceneSettings = sceneSettings;

            if (AwaitForMRUKInitialization)
            {
                yield return new WaitUntil(() => MRUK.Instance != null && MRUK.Instance.IsInitialized);
            }
        }

        protected IEnumerator UnloadScene()
        {
            // Loading an empty scene as single will unload all other scenes
            yield return EditorSceneManager.LoadSceneAsyncInPlayMode(EmptyScene,
                new LoadSceneParameters(LoadSceneMode.Single));
        }

        /// <summary>
        /// Loads a scene from a JSON string representation and waits for the operation to complete.
        /// This method handles the asynchronous loading process and verifies successful completion.
        /// </summary>
        /// <param name="sceneJson">JSON string containing the scene definition to load.</param>
        /// <returns>An IEnumerator for use with Unity's coroutine system to handle the asynchronous operation.</returns>
        protected IEnumerator LoadSceneFromJsonStringAndWait(string sceneJson)
        {
            // Loading from JSON is an async operation in the shared library so wait
            // until the task completes before continuing
            var result = MRUK.Instance.LoadSceneFromJsonString(sceneJson);
            yield return new WaitUntil(() => result.IsCompleted);
            Assert.AreEqual(MRUK.LoadDeviceResult.Success, result.Result, "Failed to load scene from json string");
        }

        /// <summary>
        /// Loads a scene from a prefab and waits for the operation to complete.
        /// This method instantiates the prefab as a scene, handles the asynchronous loading process,
        /// and verifies successful completion.
        /// </summary>
        /// <param name="scenePrefab">The GameObject prefab to load as a scene.</param>
        /// <param name="clearSceneFirst">If true, clears the existing scene before loading the prefab.</param>
        /// <returns>An IEnumerator for use with Unity's coroutine system to handle the asynchronous operation.</returns>
        protected IEnumerator LoadSceneFromPrefabAndWait(GameObject scenePrefab, bool clearSceneFirst = true)
        {
            if (scenePrefab == null)
            {
                throw new ArgumentNullException(nameof(scenePrefab));
            }

            // Loading from prefab is an async operation in the shared library so wait
            // until the task completes before continuing with performance optimization
            var result = MRUK.Instance.LoadSceneFromPrefab(scenePrefab, clearSceneFirst);

            var timeout = Time.time + LoadSceneFromPrefabTimeoutSeconds;
            while (!result.IsCompleted && Time.time < timeout)
            {
                yield return null;
            }

            if (!result.IsCompleted)
            {
                throw new TimeoutException(
                    $"Loading scene from prefab {scenePrefab.name} timed out after {LoadSceneFromPrefabTimeoutSeconds} seconds");
            }

            Assert.AreEqual(MRUK.LoadDeviceResult.Success, result.Result,
                $"Failed to load scene from prefab: {scenePrefab.name}");
        }


        /// <summary>
        /// Re-initializes MRUK with custom settings by destroying the current instance and creating a new one.
        /// This allows tests to use different configurations without requiring a complete scene reload.
        /// </summary>
        /// <param name="customSettings">The custom settings to use for MRUK initialization</param>
        /// <returns>Coroutine for the re-initialization process</returns>
        private IEnumerator ReinitializeMRUKWithCustomSettings(MRUKTestsSettings customSettings)
        {
            // Destroy existing MRUK instance if it exists
            if (MRUK.Instance != null)
            {
                Object.DestroyImmediate(MRUK.Instance.gameObject);
                yield return null; // Wait a frame for cleanup
            }

            // Create new MRUK instance with custom settings
            var mrukGameObject = Object.Instantiate(MRUKTestsSettings.MrukPrefab);
            var mruk = mrukGameObject.GetComponent<MRUK>();
            if (mruk == null)
            {
                NUnit.Framework.Assert.Fail("MRUK component not found on instantiated prefab");
            }

            // Wait for editor readiness
            yield return new WaitUntil(() => InitializeOnLoad.EditorReady);


            var sceneSettings = customSettings.SceneSettings;

            mruk.SceneLoadedEvent = DeepCopyUnityEvent(customSettings.SceneLoadedEvent);
            mruk.RoomCreatedEvent = DeepCopyUnityEvent(customSettings.RoomCreatedEvent);
            mruk.RoomUpdatedEvent = DeepCopyUnityEvent(customSettings.RoomUpdatedEvent);
            mruk.RoomRemovedEvent = DeepCopyUnityEvent(customSettings.RoomRemovedEvent);
            mruk.EnableWorldLock = customSettings.WorldLockEnabled;
            mruk.SceneSettings = sceneSettings;

            // Wait for initialization if required
            if (AwaitForMRUKInitialization)
            {
                yield return new WaitUntil(() => MRUK.Instance != null && MRUK.Instance.IsInitialized);
            }

            Debug.Log(
                $"MRUK re-initialized with custom settings (resolved): DataSource={sceneSettings.DataSource}, Rooms={sceneSettings.RoomPrefabs?.Length ?? 0}, JSONs={sceneSettings.SceneJsons?.Length ?? 0}");
        }

        /// <summary>
        /// Performs cleanup between room tests to maintain optimal performance.
        /// </summary>
        /// <param name="roomName">Name of the room that was just tested</param>
        /// <param name="room">The room instance that was just tested</param>
        private void CleanupBetweenRooms(string roomName, MRUKRoom room)
        {
            try
            {
                RoomTearDown?.Invoke(roomName, room);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error executing RoomTearDown callback for room {roomName}: {ex.Message}");
            }

            Resources.UnloadUnusedAssets();
        }
    }
}
