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
using Meta.XR.Telemetry;
using UnityEngine;
using UnityEngine.Events;

namespace Meta.XR.MRUtilityKit
{
    /// <summary>
    /// The <c>DestructibleGlobalMeshSpawner</c> class manages the spawning and lifecycle of destructible global meshes within the MRUK framework.
    /// It listens to room events and dynamically creates or removes destructible meshes as rooms are created or removed.
    /// For more details on room management, see <see cref="MRUKRoom"/>.
    /// </summary>
    [HelpURL("https://developers.meta.com/horizon/reference/mruk/latest/class_meta_x_r_m_r_utility_kit_destructible_global_mesh_spawner")]
    public class DestructibleGlobalMeshSpawner : MonoBehaviour
    {
        [SerializeField] public MRUK.RoomFilter CreateOnRoomLoaded = MRUK.RoomFilter.CurrentRoomOnly;

        /// <summary>
        /// Event triggered when a destructible mesh is successfully created.
        /// </summary>
        public UnityEvent<DestructibleMeshComponent> OnDestructibleMeshCreated;

        /// <summary>
        /// Function delegate that processes the segmentation result. It can be used to modify the segmentation results before they are instantiated.
        /// </summary>
        public Func<DestructibleMeshComponent.MeshSegmentationResult, DestructibleMeshComponent.MeshSegmentationResult>
            OnSegmentationCompleted;

        [SerializeField] private bool _reserveSpace = false;
        [SerializeField] private Vector3 _reservedMin;
        [SerializeField] private Vector3 _reservedMax;
        [SerializeField] private Material _globalMeshMaterial;
        [SerializeField] private float _pointsPerUnitX = 2.0f;
        [SerializeField] private float _pointsPerUnitY = 2.0f;
        [SerializeField] private float _pointsPerUnitZ = 2.0f;
        [SerializeField] private float _reservedTop = 0f;
        [SerializeField] private float _reservedBottom = 0f;

        private const string DestructibleGlobalMeshObjectName = "DestructibleGlobalMesh";
        private readonly Dictionary<MRUKRoom, DestructibleGlobalMesh> _spawnedDestructibleMeshes = new();

        /// <summary>
        /// Gets or sets whether to keep some reserved un-destructible space (defined in meters).
        /// </summary>
        public bool ReserveSpace
        {
            get => _reserveSpace;
            set => _reserveSpace = value;
        }

        /// <summary>
        /// Gets or sets the number of points per unit along the X-axis for the destructible mesh.
        /// This setting affects the density and detail of the mesh, influencing both visual quality and performance.
        /// </summary>
        public float PointsPerUnitX
        {
            get => _pointsPerUnitX;
            set => _pointsPerUnitX = value;
        }

        /// <summary>
        /// Gets or sets the number of points per unit along the Y-axis for the destructible mesh.
        /// This setting affects the density and detail of the mesh, influencing both visual quality and performance.
        /// </summary>
        public float PointsPerUnitY
        {
            get => _pointsPerUnitY;
            set => _pointsPerUnitY = value;
        }

        /// <summary>
        /// Gets or sets the number of points per unit along the Z-axis for the destructible mesh.
        /// This setting affects the density and detail of the mesh, influencing both visual quality and performance.
        /// </summary>
        public float PointsPerUnitZ
        {
            get => _pointsPerUnitZ;
            set => _pointsPerUnitZ = value;
        }

        /// <summary>
        /// Gets or sets the material used for the mesh. This material is applied to the mesh segments that are created during the segmentation process.
        /// </summary>
        public Material GlobalMeshMaterial
        {
            get => _globalMeshMaterial;
            set => _globalMeshMaterial = value;
        }

        /// <summary>
        /// Gets or sets the reserved space at the top of the mesh. This space is not included in the destructible area, allowing for controlled segmentation.
        /// </summary>
        public float ReservedTop
        {
            get => _reservedTop;
            set => _reservedTop = value;
        }

        /// <summary>
        /// Gets or sets the reserved space at the bottom of the mesh. This space is not included in the destructible area, allowing for controlled segmentation.
        /// </summary>
        public float ReservedBottom
        {
            get => _reservedBottom;
            set => _reservedBottom = value;
        }

        private void Start()
        {
            var unifiedEvent = new UnifiedEventData(TelemetryConstants.EventName.LoadDestructibleGlobalMeshSpawner);
            unifiedEvent.SendMRUKEvent();
            MRUK.Instance?.RegisterSceneLoadedCallback(ReceiveSceneLoadedEvent);
            MRUK.Instance?.RoomCreatedEvent.AddListener(ReceiveCreatedRoom);
            MRUK.Instance?.RoomRemovedEvent.AddListener(ReceiveRemovedRoom);
        }

        private void OnDestroy()
        {
            MRUK.Instance?.SceneLoadedEvent.RemoveListener(ReceiveSceneLoadedEvent);
            MRUK.Instance?.RoomCreatedEvent.RemoveListener(ReceiveCreatedRoom);
            MRUK.Instance?.RoomRemovedEvent.RemoveListener(ReceiveRemovedRoom);
        }

        /// <summary>
        /// Adds a destructible global mesh to all rooms. This method is typically called when the <c>SpawnOnStart</c> setting includes all rooms.
        /// </summary>
        private void AddDestructibleGlobalMesh()
        {
            foreach (var room in MRUK.Instance.Rooms)
            {
                if (!room.GlobalMeshAnchor)
                {
                    Debug.LogWarning(
                        $"Can not find a global mesh anchor, skipping the destructible mesh creation for this room");
                    continue;
                }

                if (!_spawnedDestructibleMeshes.ContainsKey(room))
                {
                    AddDestructibleGlobalMesh(room);
                }
            }
        }

        /// <summary>
        /// Adds a destructible global mesh to a specific room. This method checks for existing meshes in the room and creates a new one if none exists.
        ///  The destructible mesh is created using the specified parameters, including the material, points per unit, and maximum points count.
        ///  A <see cref="DestructibleMeshComponent"/> is added to the destructible global mesh game object, and the segmentation process is started.
        /// </summary>
        /// <param name="room">The room to which the mesh will be added.</param>
        /// <returns>The destructible mesh created for the room.</returns>
        public DestructibleGlobalMesh AddDestructibleGlobalMesh(MRUKRoom room)
        {
            if (_spawnedDestructibleMeshes.ContainsKey(room))
            {
                throw new Exception("Cannot add a destructible mesh to this room as it already contains one.");
            }

            if (!room.GlobalMeshAnchor)
            {
                throw new Exception(
                    "A destructible mesh can not be created for this room as it does not contain a global mesh anchor.");
            }
            var destructibleGlobalMeshGO = new GameObject(DestructibleGlobalMeshObjectName);
            destructibleGlobalMeshGO.transform.SetParent(room.GlobalMeshAnchor.transform, false);
            var dMesh = destructibleGlobalMeshGO.AddComponent<DestructibleMeshComponent>();
            dMesh.GlobalMeshMaterial = _globalMeshMaterial;
            if (_reserveSpace == false)
            {
                ReservedBottom = -1;
                ReservedTop = -1;
            }
            dMesh.ReservedBottom = ReservedBottom;
            dMesh.ReservedTop = ReservedTop;
            dMesh.OnDestructibleMeshCreated = OnDestructibleMeshCreated;
            dMesh.OnSegmentationCompleted = OnSegmentationCompleted;
            var destructibleGlobalMesh = new DestructibleGlobalMesh
            {
                PointsPerUnitX = _pointsPerUnitX,
                PointsPerUnitY = _pointsPerUnitY,
                PointsPerUnitZ = _pointsPerUnitZ,
                DestructibleMeshComponent = dMesh
            };
            CreateDestructibleGlobalMesh(destructibleGlobalMesh, room);
            _spawnedDestructibleMeshes.Add(room, destructibleGlobalMesh);
            return destructibleGlobalMesh;
        }

        /// <summary>
        /// Creates a destructible mesh within a specified room. If no room is provided, it defaults to the current room.
        /// This method handles the mesh creation by calculating segmentation points and starts the segmentation process.
        /// </summary>
        /// <param name="destructibleGlobalMesh">The destructible global mesh to create.</param>
        /// <param name="room">The room where the mesh will be created.</param>
        private static void CreateDestructibleGlobalMesh(DestructibleGlobalMesh destructibleGlobalMesh, MRUKRoom room)
        {
            if (!room)
            {
                throw new Exception("Could not find a room for the destructible mesh");
            }
            if (!room.GlobalMeshAnchor || !room.GlobalMeshAnchor.Mesh)
            {
                throw new Exception("Could not load the mesh associated with the global mesh anchor of the room");
            }

            var meshPositions = room.GlobalMeshAnchor.Mesh.vertices;
            var meshIndices = room.GlobalMeshAnchor.Mesh.triangles;
            var meshIndicesUint = Array.ConvertAll(meshIndices, Convert.ToUInt32);

            destructibleGlobalMesh.DestructibleMeshComponent.SegmentMesh(meshPositions, meshIndicesUint,
                destructibleGlobalMesh.PointsPerUnitX, destructibleGlobalMesh.PointsPerUnitY, destructibleGlobalMesh.PointsPerUnitZ);
        }

        /// <summary>
        /// Attempts to find a destructible mesh associated with a specific room. If found, the method returns true and provides the mesh via an out parameter.
        /// </summary>
        /// <param name="room">The room for which to find the destructible mesh.</param>
        /// <param name="destructibleGlobalMesh">Out parameter that will hold the destructible mesh if found.</param>
        /// <returns>False if the mesh is not found (i.e., default value is returned), otherwise true.</returns>
        public bool TryGetDestructibleMeshForRoom(MRUKRoom room, out DestructibleGlobalMesh destructibleGlobalMesh)
        {
            destructibleGlobalMesh = _spawnedDestructibleMeshes.GetValueOrDefault(room);
            return destructibleGlobalMesh != default(DestructibleGlobalMesh);
        }

        /// <summary>
        /// Removes a destructible global mesh from the specified room. If no room is specified, it defaults to the current room.
        /// </summary>
        /// <param name="room">The room from which the mesh will be removed. If null, the current room is used.</param>
        public void RemoveDestructibleGlobalMesh(MRUKRoom room = null)
        {
            if (MRUK.Instance == null || MRUK.Instance.GetCurrentRoom() == null)
            {
                throw new Exception(
                    "Can not remove a destructible global mesh when MRUK instance has not been initialized.");
            }
            if (room == null)
            {
                room = MRUK.Instance.GetCurrentRoom();
            }

            if (TryGetDestructibleMeshForRoom(room, out var destructibleGlobalMesh))
            {
                if (destructibleGlobalMesh.DestructibleMeshComponent)
                {
                    Destroy(destructibleGlobalMesh.DestructibleMeshComponent.gameObject);
                }
                _spawnedDestructibleMeshes.Remove(room);
            }
        }

        private void ReceiveSceneLoadedEvent()
        {
            if (CreateOnRoomLoaded == MRUK.RoomFilter.None)
            {
                return;
            }

            switch (CreateOnRoomLoaded)
            {
                case MRUK.RoomFilter.CurrentRoomOnly:
                    var currentRoom = MRUK.Instance.GetCurrentRoom();
                    if (!_spawnedDestructibleMeshes.ContainsKey(currentRoom))
                    {
                        AddDestructibleGlobalMesh(currentRoom);
                    }

                    break;
                case MRUK.RoomFilter.AllRooms:
                    AddDestructibleGlobalMesh();
                    break;
                case MRUK.RoomFilter.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ReceiveCreatedRoom(MRUKRoom room)
        {
            if (CreateOnRoomLoaded == MRUK.RoomFilter.CurrentRoomOnly && _spawnedDestructibleMeshes.Count > 0)
            {
                return;
            }

            if (CreateOnRoomLoaded == MRUK.RoomFilter.AllRooms)
            {
                AddDestructibleGlobalMesh();
            }
        }

        private void ReceiveRemovedRoom(MRUKRoom room)
        {
            if (room == null)
            {
                throw new Exception("Received a Room Removed event but the room is null.");
            }

            RemoveDestructibleGlobalMesh(room);
        }
    }

    /// <summary>
    /// The <c>DestructibleGlobalMesh</c> struct represents a destructible global mesh within a specific room.
    /// It includes functionality to create and manage the mesh based on room-specific parameters and global settings.
    /// Every DestructibleGlobalMesh is associated with a <see cref="DestructibleMeshComponent"/> that handles the actual mesh manipulation, including segmentation and rendering.
    /// </summary>
    public struct DestructibleGlobalMesh
    {
        /// <summary>
        /// The <see cref="MRUtilityKit.DestructibleMeshComponent"/> associated with this global mesh. This component handles the actual mesh manipulation, including segmentation and rendering, based on the parameters provided.
        /// </summary>
        public DestructibleMeshComponent DestructibleMeshComponent;

        /// <summary>
        /// Specifies the number of points per unit along the X-axis for the destructible mesh. This setting affects the density and detail of the mesh, influencing both visual quality and performance.
        /// Use <see cref="MRUtilityKit.DestructibleGlobalMeshSpawner"/>  to configure this value.
        /// </summary>
        public float PointsPerUnitX;

        /// <summary>
        /// Specifies the number of points per unit along the Y-axis for the destructible mesh. This setting affects the density and detail of the mesh, influencing both visual quality and performance.
        /// Use <see cref="MRUtilityKit.DestructibleGlobalMeshSpawner"/>  to configure this value.
        /// </summary>
        public float PointsPerUnitY;

        /// <summary>
        /// Specifies the number of points per unit along the Z-axis for the destructible mesh. This setting affects the density and detail of the mesh, influencing both visual quality and performance.
        /// Use <see cref="MRUtilityKit.DestructibleGlobalMeshSpawner"/>  to configure this value.
        /// </summary>
        public float PointsPerUnitZ;

        private bool Equals(DestructibleGlobalMesh other)
        {
            return DestructibleMeshComponent == other.DestructibleMeshComponent &&
                   Mathf.Approximately(PointsPerUnitX, other.PointsPerUnitX) &&
                   Mathf.Approximately(PointsPerUnitY, other.PointsPerUnitY) &&
                   Mathf.Approximately(PointsPerUnitZ, other.PointsPerUnitZ);
        }

        // @cond
        public override bool Equals(object obj)
        {
            return obj is DestructibleGlobalMesh other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(DestructibleMeshComponent, PointsPerUnitX, PointsPerUnitY, PointsPerUnitZ);
        }

        public static bool operator ==(DestructibleGlobalMesh left, DestructibleGlobalMesh right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DestructibleGlobalMesh left, DestructibleGlobalMesh right)
        {
            return !left.Equals(right);
        }
        // @endcond
    }
}
