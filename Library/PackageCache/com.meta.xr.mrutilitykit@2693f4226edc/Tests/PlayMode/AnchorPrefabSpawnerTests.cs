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
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class AnchorPrefabSpawnerTests : MRUKTestBase
    {
        private JSONTestHelper _helper;

        private static readonly int Room1WallCount = 7;
        private static readonly int Room1FloorCount = 1;
        private static readonly int Room1CeilingCount = 1;
        private static readonly int Room1TableCount = 1;
        private static readonly int Room1OtherCount = 2;

        private static readonly int Room3WallCount = 7;
        private static readonly int Room3FloorCount = 1;
        private static readonly int Room3CeilingCount = 1;
        private static readonly int Room3TableCount = 1;
        private static readonly int Room3OtherCount = 2;

        protected override string SceneToTest =>
            "Packages/com.meta.xr.mrutilitykit/Tests/AnchorPrefabSpawnerTests.unity";

        protected override bool AwaitForMRUKInitialization => false;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();
            _helper = Object.FindAnyObjectByType<JSONTestHelper>();
        }

        private (int, int, int, int, int) CountSpawnedChildrenInRoom(MRUKRoom room)
        {
            int createdWalls = 0;
            int createdFloor = 0;
            int createdCeiling = 0;
            int createdTable = 0;
            int createdOther = 0;

            foreach (var anchor in room.Anchors)
            {
                switch (anchor.Label)
                {
                    case MRUKAnchor.SceneLabels.FLOOR:
                        if (HasSpawnedChild(anchor))
                        {
                            createdFloor++;
                        }

                        break;
                    case MRUKAnchor.SceneLabels.CEILING:
                        if (HasSpawnedChild(anchor))
                        {
                            createdCeiling++;
                        }

                        break;
                    case MRUKAnchor.SceneLabels.WALL_FACE:
                        if (HasSpawnedChild(anchor))
                        {
                            createdWalls++;
                        }

                        break;
                    case MRUKAnchor.SceneLabels.TABLE:
                        createdTable++;
                        break;
                    case MRUKAnchor.SceneLabels.COUCH:
                    case MRUKAnchor.SceneLabels.DOOR_FRAME:
                    case MRUKAnchor.SceneLabels.WINDOW_FRAME:
                    case MRUKAnchor.SceneLabels.STORAGE:
                    case MRUKAnchor.SceneLabels.BED:
                    case MRUKAnchor.SceneLabels.SCREEN:
                    case MRUKAnchor.SceneLabels.LAMP:
                    case MRUKAnchor.SceneLabels.PLANT:
                    case MRUKAnchor.SceneLabels.WALL_ART:
                    case MRUKAnchor.SceneLabels.GLOBAL_MESH:
                    case MRUKAnchor.SceneLabels.INVISIBLE_WALL_FACE:
                    case MRUKAnchor.SceneLabels.OTHER:
                        if (HasSpawnedChild(anchor))
                        {
                            createdOther++;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            return (createdWalls, createdFloor, createdCeiling, createdTable, createdOther);
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountDifferentLabelsForSpawnedPrefab()
        {
            SetupAnchorPrefabSpawner();
            yield return LoadSceneFromJsonStringAndWait(_helper.SceneWithRoom1.text);

            var (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.GetCurrentRoom());

            Assert.AreEqual(Room1WallCount, createdWalls);
            Assert.AreEqual(Room1FloorCount, createdFloor);
            Assert.AreEqual(Room1CeilingCount, createdCeiling);
            Assert.AreEqual(Room1TableCount, createdTable);
            Assert.AreEqual(Room1OtherCount, createdOther);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountSpawnedItemsRoom3Room1()
        {
            SetupAnchorPrefabSpawner();
            yield return LoadSceneFromJsonStringAndWait(_helper.SceneWithRoom3Room1.text);

            var (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[0]);

            Assert.AreEqual(Room1WallCount, createdWalls);
            Assert.AreEqual(Room1FloorCount, createdFloor);
            Assert.AreEqual(Room1CeilingCount, createdCeiling);
            Assert.AreEqual(Room1TableCount, createdTable);
            Assert.AreEqual(Room1OtherCount, createdOther);

            (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[1]);

            Assert.AreEqual(Room3WallCount, createdWalls);
            Assert.AreEqual(Room3FloorCount, createdFloor);
            Assert.AreEqual(Room3CeilingCount, createdCeiling);
            Assert.AreEqual(Room3TableCount, createdTable);
            Assert.AreEqual(Room3OtherCount, createdOther);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountSpawnedItemsRoom1WallsOnly()
        {
            var anchorPrefabSpawner = SetupAnchorPrefabSpawner();
            string[] searchResults = AssetDatabase.FindAssets("WALL", new[] { "Packages/com.meta.xr.mrutilitykit/Core/Prefabs/" });
            string prefabPath = AssetDatabase.GUIDToAssetPath(searchResults[0]);

            anchorPrefabSpawner.PrefabsToSpawn = new List<AnchorPrefabSpawner.AnchorPrefabGroup>()
            {
                new()
                {
                    Labels = MRUKAnchor.SceneLabels.WALL_FACE,
                    Prefabs = new List<GameObject>()
                    {
                        AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath)
                    }
                }
            };

            yield return LoadSceneFromJsonStringAndWait(_helper.SceneWithRoom1.text);

            var (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[0]);

            Assert.AreEqual(Room1WallCount, createdWalls);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountSpawnedItemsRoom1ThenAddRoom3()
        {
            var anchorPrefabSpawner = SetupAnchorPrefabSpawner();
            yield return LoadSceneFromJsonStringAndWait(_helper.SceneWithRoom1.text);

            var (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[0]);

            Assert.AreEqual(Room1WallCount, createdWalls);
            Assert.AreEqual(Room1FloorCount, createdFloor);
            Assert.AreEqual(Room1CeilingCount, createdCeiling);
            Assert.AreEqual(Room1TableCount, createdTable);
            Assert.AreEqual(Room1OtherCount, createdOther);

            yield return LoadSceneFromJsonStringAndWait(_helper.SceneWithRoom3Room1.text);

            (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[1]);

            Assert.AreEqual(Room3WallCount, createdWalls);
            Assert.AreEqual(Room3FloorCount, createdFloor);
            Assert.AreEqual(Room3CeilingCount, createdCeiling);
            Assert.AreEqual(Room3TableCount, createdTable);
            Assert.AreEqual(Room3OtherCount, createdOther);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CountSpawnedItemsRoom1AddMoreAnchors()
        {
            SetupAnchorPrefabSpawner();

            yield return LoadSceneFromJsonStringAndWait(_helper.SceneWithRoom1.text);

            var (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[0]);

            Assert.AreEqual(Room1WallCount, createdWalls);
            Assert.AreEqual(Room1FloorCount, createdFloor);
            Assert.AreEqual(Room1CeilingCount, createdCeiling);
            Assert.AreEqual(Room1TableCount, createdTable);
            Assert.AreEqual(Room1OtherCount, createdOther);

            yield return LoadSceneFromJsonStringAndWait(_helper.SceneWithRoom1MoreAnchors.text);

            (createdWalls, createdFloor, createdCeiling, createdTable, createdOther) =
                CountSpawnedChildrenInRoom(MRUK.Instance.Rooms[0]);

            Assert.AreEqual(Room1WallCount, createdWalls);
            Assert.AreEqual(Room1FloorCount, createdFloor);
            Assert.AreEqual(Room1CeilingCount, createdCeiling);
            Assert.AreEqual(Room1TableCount + 2, createdTable);
            Assert.AreEqual(Room1OtherCount, createdOther);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator AllAnchorsHaveSpawnedPrefab()
        {
            SetupAnchorPrefabSpawner();

            yield return LoadSceneFromJsonStringAndWait(_helper.SceneWithRoom1.text);

            foreach (var anchor in MRUK.Instance.GetCurrentRoom().Anchors)
            {
                Assert.AreEqual(true, HasSpawnedChild(anchor));
            }

            yield return null;
        }

        private bool HasSpawnedChild(MRUKAnchor anchorParent)
        {
            for (int i = 0; i < anchorParent.transform.childCount; i++)
            {
                Transform child = anchorParent.transform.GetChild(i);
                if (child.name.Contains("(PrefabSpawner Clone)"))
                {
                    return true;
                }
            }
            return false;
        }

        private AnchorPrefabSpawner SetupAnchorPrefabSpawner()
        {
            var anchorPrefabSpawner = Object.FindAnyObjectByType<AnchorPrefabSpawner>();
            if (anchorPrefabSpawner == null)
            {
                Assert.Fail();
            }
            anchorPrefabSpawner.TrackUpdates = true;
            return anchorPrefabSpawner;
        }
    }

}
