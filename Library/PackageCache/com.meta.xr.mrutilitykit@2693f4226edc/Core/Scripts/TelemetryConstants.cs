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

/// @cond

using Meta.XR.Telemetry;
using UnityEngine;

namespace Meta.XR.MRUtilityKit
{
    public static class TelemetryConstants
    {
        public static class MarkerId
        {
            public const int LoadSceneFromDevice = 651892966;
            public const int LoadSceneFromPrefab = 651889651;
            public const int LoadSceneFromJson = 651895197;
            public const int LoadEffectMesh = 651897605;
            public const int LoadFindSpawnPositions = 651888440;
            public const int LoadRoomGuardian = 651901100;
            public const int LoadSceneDebugger = 651897568;
            public const int LoadAnchorPrefabSpawner = 651902681;
            public const int LoadGridSliceResizer = 651896136;
            public const int LoadSceneNavigation = 651889094;
            public const int LoadSceneDecoration = 651888752;
            public const int LoadDestructibleGlobalMeshSpawner = 651898938;
            public const int LoadEnvironmentRaycastManager = 651891190;
            public const int LoadEnvironmentRaycastManagerOpenXR = 651888817;
            public const int LoadSpaceMapGPU = 651896914;
            public const int LoadHiFiScene = 651888532;
            public const int SetCustomWorldLockAnchor = 651891924;
            public const int LoadPassthroughCameraAccess = 651892106;
            public const int StartMarkerTracker = 651896615;
            public const int StartKeyboardTracker = 651896297;
        }

        public static class EventName
        {
            public const string LoadAnchorPrefabSpawner = "LOAD_ANCHOR_PREFAB_SPAWNER";
            public const string LoadRoomGuardian = "LOAD_ROOM_GUARDIAN";
            public const string LoadDestructibleGlobalMeshSpawner = "LOAD_DESTRUCTIBLE_GLOBAL_MESH_SPAWNER";
            public const string LoadEffectMesh = "LOAD_EFFECT_MESH";
            public const string LoadSceneDebugger = "LOAD_SCENE_DEBUGGER";
            public const string LoadSpaceMap = "LOAD_SPACE_MAP";
            public const string StartMarkerTracker = "START_MARKER_TRACKER";
            public const string StartKeyboardTracker = "START_KEYBOARD_TRACKER";
            public const string LoadGridSliceResizer = "LOAD_GRID_SLICE_RESIZER";
            public const string LoadSceneFromJson = "LOAD_SCENE_FROM_JSON";
            public const string LoadSceneFromDevice = "LOAD_SCENE_FROM_DEVICE";
            public const string LoadPassthroughCameraAccess = "LOAD_PASSTHROUGH_CAMERA_ACCESS";
            public const string MrukSetCustomWorldLockAnchor = "MRUK_SET_CUSTOM_WORLD_LOCK_ANCHOR";
            public const string LoadEnvironmentRaycast = "LOAD_ENVIRONMENT_RAYCAST";
            public const string LoadSceneFromPrefab = "LOAD_SCENE_FROM_PREFAB";
            public const string LoadSceneNavigation = "LOAD_SCENE_NAVIGATION";
            public const string LoadEnvironmentRaycastOpenxr = "LOAD_ENVIRONMENT_RAYCAST_OPENXR";
            public const string LoadSceneDecoration = "LOAD_SCENE_DECORATION";
            public const string LoadHifiScene = "LOAD_HIFI_SCENE";
            public const string LoadFindSpawnPositions = "LOAD_FIND_SPAWN_POSITIONS";
            public const string RunTestOnAllScenes = "RUN_TEST_ON_ALL_SCENES";
        }

        public static class AnnotationType
        {
            public const string SceneName = "SceneName";
            public const string NumRooms = "NumRooms";
        }
    }

    internal static class MRUKTelemetryEvent
    {
        private static readonly string ApplicationPlatform = Application.platform.ToString();

        public static bool SendMRUKEvent(this UnifiedEventData eventData)
        {
            eventData.productType = TelemetryProductType.Mruk;
            eventData.isEssential = false;
            eventData.SetMetadata("device_os", SystemInfo.operatingSystem);
            eventData.SetMetadata("developer_platform", ApplicationPlatform);
            eventData.SetMetadata("openxr_runtime_name", OVRPlugin.runtimeName);
            return eventData.Send();
        }
    }
}
/// @endcond
