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

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.XR.FovSimulator
{
    [AddComponentMenu("")]
    internal sealed class FovSimulatorLoaderOwnedObject : MonoBehaviour
    {
        internal bool DestroyScheduled { get; set; }

        private void Awake()
        {
            FovSimulatorLoader.RegisterOwnedObject(this);
        }

        private void OnDestroy()
        {
            FovSimulatorLoader.UnregisterOwnedObject(this);
        }
    }

    [AddComponentMenu("")]
    internal sealed class FovSimulatorLoaderActivationMonitor : MonoBehaviour
    {
        private FovSimulator _simulator;
        private string _environment;
        private bool _cancelled;
        internal bool IsCancelled => _cancelled;

        internal void Begin(FovSimulator simulator, string environment)
        {
            _simulator = simulator;
            _environment = environment;
            _cancelled = false;
            enabled = true;
        }

        internal void CompleteIfActive(string environment = null)
        {
            if (_simulator == null || !_simulator.OverlayActive)
            {
                return;
            }

            environment ??= _environment;
            Cancel();
            OVRFovSimulationTelemetry.SendRuntimeStarted(environment);
        }

        internal void Cancel()
        {
            if (_cancelled)
            {
                return;
            }

            _cancelled = true;
            _simulator = null;
            _environment = null;
            enabled = false;
        }

        private void Update()
        {
            CompleteIfActive();
        }
    }

    internal static class FovSimulatorLoader
    {
        private static FovSimulatorLoaderOwnedObject _ownedObject;

        internal static void RegisterOwnedObject(FovSimulatorLoaderOwnedObject ownership)
        {
            if (_ownedObject == null || _ownedObject.DestroyScheduled)
            {
                _ownedObject = ownership;
            }
        }

        internal static void UnregisterOwnedObject(FovSimulatorLoaderOwnedObject ownership)
        {
            if (_ownedObject == ownership)
            {
                _ownedObject = null;
            }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            ApplyAndroidSetting();
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            ApplyAndroidSetting();
        }

        private static void ApplyAndroidSetting()
        {
            OVRRuntimeSettings runtimeSettings = OVRRuntimeSettings.Instance;
            if (runtimeSettings == null)
            {
                return;
            }

            Apply(
                runtimeSettings.FovSimulationEnabled,
                OVRFovSimulationTelemetry.AndroidEnvironment);
        }
#endif

        internal static void ApplyConfiguredSetting()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            ApplyAndroidSetting();
#elif UNITY_EDITOR
            EditorSettingApplier?.Invoke();
#endif
        }

#if UNITY_EDITOR
        internal static System.Action EditorSettingApplier { get; set; }
#endif

        internal static void Apply(bool enabled, string environment)
        {
            if (!enabled)
            {
                DisableAll(environment);
                return;
            }

            FovSimulator simulator = FindCanonicalSimulator(environment);
            GameObject createdGameObject = null;
            bool createdComponent = false;
            if (simulator == null)
            {
                try
                {
                    GameObject owner = OVRManager.instance != null &&
                        OVRManager.instance.gameObject.activeInHierarchy
                        ? OVRManager.instance.gameObject
                        : null;
                    if (owner == null)
                    {
                        createdGameObject = new GameObject(nameof(FovSimulator));
                        FovSimulatorLoaderOwnedObject ownership =
                            createdGameObject.AddComponent<FovSimulatorLoaderOwnedObject>();
                        ownership.hideFlags = HideFlags.HideInInspector;
                        Object.DontDestroyOnLoad(createdGameObject);
                        owner = createdGameObject;
                    }

                    simulator = owner.AddComponent<FovSimulator>();
                    createdComponent = simulator != null;
                }
                catch (System.Exception exception)
                {
                    DestroyCreatedSimulator(simulator, createdGameObject, createdComponent);
                    Debug.LogException(exception);
                    OVRFovSimulationTelemetry.SendRuntimeStartFailed(
                        OVRFovSimulationTelemetry.ComponentCreationFailedReason,
                        environment,
                        exception);
                    return;
                }
                if (simulator == null)
                {
                    DestroyCreatedSimulator(simulator, createdGameObject, createdComponent);
                    Debug.LogError("[FovSimulator] Failed to create the FoV simulation component.");
                    OVRFovSimulationTelemetry.SendRuntimeStartFailed(
                        OVRFovSimulationTelemetry.ComponentCreationFailedReason,
                        environment);
                    return;
                }
            }

            if (simulator.OverlayActive)
            {
                FovSimulatorLoaderActivationMonitor pendingMonitor =
                    FindPendingActivationMonitor(simulator);
                if (pendingMonitor != null)
                {
                    pendingMonitor.CompleteIfActive(environment);
                }
                return;
            }

            FovSimulatorLoaderActivationMonitor monitor = null;
            try
            {
                monitor = GetOrCreateActivationMonitor(simulator);
                monitor.hideFlags = HideFlags.HideInInspector;
                monitor.Begin(simulator, environment);
                simulator.OverlayActive = true;
            }
            catch (System.Exception exception)
            {
                monitor?.Cancel();
                TryDeactivate(simulator, environment, reportFailure: false);
                DestroyCreatedSimulator(simulator, createdGameObject, createdComponent);
                Debug.LogException(exception);
                OVRFovSimulationTelemetry.SendRuntimeStartFailed(
                    OVRFovSimulationTelemetry.ActivationExceptionReason,
                    environment,
                    exception);
                return;
            }

            if (simulator.OverlayActive)
            {
                monitor.CompleteIfActive(environment);
                return;
            }

            if (simulator.enabled)
            {
                return;
            }

            Debug.LogError("[FovSimulator] Failed to activate FoV simulation.", simulator);
            monitor.Cancel();
            TryDeactivate(simulator, environment, reportFailure: false);
            DestroyCreatedSimulator(simulator, createdGameObject, createdComponent);
            OVRFovSimulationTelemetry.SendRuntimeStartFailed(
                OVRFovSimulationTelemetry.ActivationFailedReason,
                environment);
        }

        private static FovSimulator FindCanonicalSimulator(string environment)
        {
            OVRManager manager = OVRManager.instance;
            if (manager != null && manager.gameObject.activeInHierarchy)
            {
                FovSimulator managerSimulator = manager.GetComponent<FovSimulator>();
                if (managerSimulator != null)
                {
                    DestroyLoaderOwnedObjects(environment);
                    return managerSimulator;
                }
            }

            FovSimulator loaderOwnedSimulator = FindLoaderOwnedSimulator(environment);
            if (loaderOwnedSimulator != null)
            {
                return loaderOwnedSimulator;
            }
            return null;
        }

        private static FovSimulator FindLoaderOwnedSimulator(string environment)
        {
            FovSimulatorLoaderOwnedObject ownership = _ownedObject;
            if (ownership == null || ownership.DestroyScheduled)
            {
                return null;
            }

            FovSimulator simulator = ownership.GetComponent<FovSimulator>();
            if (simulator == null || !ownership.gameObject.activeInHierarchy)
            {
                ScheduleDestroy(ownership);
                return null;
            }
            return simulator;
        }

        private static void DisableAll(string environment)
        {
            bool failureReported = false;
            OVRManager manager = OVRManager.instance;
            FovSimulator managerSimulator = manager != null
                ? manager.GetComponent<FovSimulator>()
                : null;
            if (!DisableSimulator(managerSimulator, environment, reportFailure: true))
            {
                failureReported = true;
            }

            FovSimulatorLoaderOwnedObject ownership = _ownedObject;
            FovSimulator ownedSimulator = ownership != null
                ? ownership.GetComponent<FovSimulator>()
                : null;
            if (ownedSimulator != managerSimulator)
            {
                DisableSimulator(
                    ownedSimulator,
                    environment,
                    reportFailure: !failureReported);
            }
            DestroyLoaderOwnedObjects(environment);
        }

        private static bool DisableSimulator(
            FovSimulator simulator,
            string environment,
            bool reportFailure)
        {
            if (simulator == null)
            {
                return true;
            }

            FovSimulatorLoaderActivationMonitor monitor =
                FindPendingActivationMonitor(simulator);
            monitor?.Cancel();
            return TryDeactivate(simulator, environment, reportFailure);
        }

        private static FovSimulatorLoaderActivationMonitor GetOrCreateActivationMonitor(
            FovSimulator simulator)
        {
            FovSimulatorLoaderActivationMonitor monitor =
                simulator.GetComponent<FovSimulatorLoaderActivationMonitor>();
            return monitor != null
                ? monitor
                : simulator.gameObject.AddComponent<FovSimulatorLoaderActivationMonitor>();
        }

        private static FovSimulatorLoaderActivationMonitor FindPendingActivationMonitor(
            FovSimulator simulator)
        {
            FovSimulatorLoaderActivationMonitor monitor =
                simulator.GetComponent<FovSimulatorLoaderActivationMonitor>();
            return monitor != null && !monitor.IsCancelled ? monitor : null;
        }

        private static void DestroyLoaderOwnedObjects(string environment)
        {
            FovSimulatorLoaderOwnedObject ownership = _ownedObject;
            if (ownership == null || ownership.DestroyScheduled)
            {
                return;
            }

            FovSimulator simulator = ownership.GetComponent<FovSimulator>();
            if (simulator != null)
            {
                TryDeactivate(simulator, environment, reportFailure: false);
            }
            ScheduleDestroy(ownership);
        }

        private static bool TryDeactivate(
            FovSimulator simulator,
            string environment,
            bool reportFailure)
        {
            if (simulator == null)
            {
                return true;
            }

            try
            {
                simulator.OverlayActive = false;
                return true;
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                if (reportFailure)
                {
                    OVRFovSimulationTelemetry.SendRuntimeStopFailed(
                        OVRFovSimulationTelemetry.DeactivationExceptionReason,
                        environment,
                        exception);
                }
                return false;
            }
        }

        private static void ScheduleDestroy(FovSimulatorLoaderOwnedObject ownership)
        {
            if (ownership == null || ownership.DestroyScheduled)
            {
                return;
            }

            ownership.DestroyScheduled = true;
            FovSimulatorLoaderActivationMonitor monitor =
                ownership.GetComponent<FovSimulatorLoaderActivationMonitor>();
            if (monitor != null)
            {
                monitor.Cancel();
            }
            DestroyInCurrentMode(ownership.gameObject);
        }

        private static void DestroyCreatedSimulator(
            FovSimulator simulator,
            GameObject createdGameObject,
            bool createdComponent)
        {
            if (createdGameObject != null)
            {
                FovSimulatorLoaderOwnedObject ownership =
                    createdGameObject.GetComponent<FovSimulatorLoaderOwnedObject>();
                if (ownership != null)
                {
                    ownership.DestroyScheduled = true;
                }
                DestroyInCurrentMode(createdGameObject);
            }
            else if (createdComponent && simulator != null)
            {
                DestroyInCurrentMode(simulator);
            }
        }

        internal static void DestroyInCurrentMode(Object value)
        {
            if (Application.isPlaying)
            {
                Object.Destroy(value);
            }
            else
            {
                Object.DestroyImmediate(value);
            }
        }
    }
}
