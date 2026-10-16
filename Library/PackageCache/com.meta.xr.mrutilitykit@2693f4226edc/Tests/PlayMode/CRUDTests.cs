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


using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Meta.XR.MRUtilityKit.Tests
{
    public class CRUDTests : MRUKTestBase
    {
        private MRUKRoom _currentRoom;
        private JSONTestHelper _jsonTestHelper;

        protected override string SceneToTest => "Packages/com.meta.xr.mrutilitykit/Tests/CRUDTests.unity";
        protected override bool AwaitForMRUKInitialization => false;

        [UnitySetUp]
        public override IEnumerator SetUp()
        {
            yield return base.SetUp();
            _jsonTestHelper = Object.FindAnyObjectByType<JSONTestHelper>();
            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithRoom1.text);
            _currentRoom = MRUK.Instance.GetCurrentRoom();
        }

        [UnityTearDown]
        public override IEnumerator TearDown()
        {
            if (MRUK.Instance != null)
            {
                foreach (var room in MRUK.Instance.Rooms)
                {
                    room.AnchorCreatedEvent.RemoveAllListeners();
                    room.AnchorUpdatedEvent.RemoveAllListeners();
                    room.AnchorRemovedEvent.RemoveAllListeners();
                }
                MRUK.Instance.RoomCreatedEvent.RemoveAllListeners();
                MRUK.Instance.RoomUpdatedEvent.RemoveAllListeners();
                MRUK.Instance.RoomRemovedEvent.RemoveAllListeners();
            }

            yield return base.TearDown();
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator VerifyStartFromJson()
        {
            Assert.AreEqual(12, _currentRoom.Anchors.Count, "Number of anchors in room");
            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator TwoAnchorsLess()
        {
            int roomUpdatedCounter = 0;
            int roomDeletedCounter = 0;
            int roomCreatedCounter = 0;

            int anchorUpdatedCounter = 0;
            int anchorDeletedCounter = 0;
            int anchorCreatedCounter = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => roomUpdatedCounter++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => roomCreatedCounter++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => roomDeletedCounter++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => anchorCreatedCounter++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => anchorDeletedCounter++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => anchorUpdatedCounter++);

            int oldRoomAnchorsCount = _currentRoom.Anchors.Count;

            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithRoom1LessAnchors.text);

            Debug.Log($"roomUpdatedCounter {roomUpdatedCounter} roomDeletedCounter {roomDeletedCounter} roomCreatedCounter {roomCreatedCounter} " +
                      $"anchorUpdatedCounter {anchorUpdatedCounter} anchorDeletedCounter {anchorDeletedCounter} anchorCreatedCounter {anchorCreatedCounter}");

            Assert.AreEqual(1, roomUpdatedCounter, "Counter for rooms updated");
            Assert.AreEqual(0, roomCreatedCounter, "Counter for rooms created");
            Assert.AreEqual(0, roomDeletedCounter, "Counter for rooms deleted");
            Assert.AreEqual(0, anchorCreatedCounter, "Counter for anchors created");
            Assert.AreEqual(2, anchorDeletedCounter, "Counter for anchors deleted");
            Assert.AreEqual(0, anchorUpdatedCounter, "Counter for anchors updated");

            Assert.AreEqual(oldRoomAnchorsCount, _currentRoom.Anchors.Count + anchorDeletedCounter);

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator TwoNewAnchors()
        {
            int roomUpdatedCounter = 0;
            int roomDeletedCounter = 0;
            int roomCreatedCounter = 0;

            int anchorUpdatedCounter = 0;
            int anchorDeletedCounter = 0;
            int anchorCreatedCounter = 0;
            int updateAnchorsCount = _currentRoom.Anchors.Count;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => roomUpdatedCounter++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => roomCreatedCounter++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => roomDeletedCounter++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => anchorCreatedCounter++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => anchorDeletedCounter++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => anchorUpdatedCounter++);
            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithRoom1MoreAnchors.text);

            Debug.Log($"roomUpdatedCounter {roomUpdatedCounter} roomDeletedCounter {roomDeletedCounter} roomCreatedCounter {roomCreatedCounter} " +
                      $"anchorUpdatedCounter {anchorUpdatedCounter} anchorDeletedCounter {anchorDeletedCounter} anchorCreatedCounter {anchorCreatedCounter}");

            Assert.AreEqual(1, roomUpdatedCounter, "Counter for rooms updated");
            Assert.AreEqual(0, roomCreatedCounter, "Counter for rooms created");
            Assert.AreEqual(0, roomDeletedCounter, "Counter for rooms deleted");
            Assert.AreEqual(2, anchorCreatedCounter, "Counter for anchors created");
            Assert.AreEqual(0, anchorDeletedCounter, "Counter for anchors deleted");
            Assert.AreEqual(0, anchorUpdatedCounter, "Counter for anchors updated");
            Assert.AreEqual(updateAnchorsCount, _currentRoom.Anchors.Count - anchorCreatedCounter, "Counter for anchors update");

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomOrderSwitched()
        {
            int roomUpdatedCounter = 0;
            int roomDeletedCounter = 0;
            int roomCreatedCounter = 0;

            int anchorUpdatedCounter = 0;
            int anchorDeletedCounter = 0;
            int anchorCreatedCounter = 0;

            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithRoom1Room3.text);

            MRUK.Instance.RoomUpdatedEvent.AddListener(room => roomUpdatedCounter++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => roomCreatedCounter++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => roomDeletedCounter++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => anchorCreatedCounter++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => anchorDeletedCounter++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => anchorUpdatedCounter++);

            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithRoom3Room1.text);
            Debug.Log($"roomUpdatedCounter {roomUpdatedCounter} roomDeletedCounter {roomDeletedCounter} roomCreatedCounter {roomCreatedCounter} " +
                      $"anchorUpdatedCounter {anchorUpdatedCounter} anchorDeletedCounter {anchorDeletedCounter} anchorCreatedCounter {anchorCreatedCounter}");

            Assert.AreEqual(0, roomUpdatedCounter, "Counter for rooms updated");
            Assert.AreEqual(0, roomCreatedCounter, "Counter for rooms created");
            Assert.AreEqual(0, roomDeletedCounter, "Counter for rooms deleted");
            Assert.AreEqual(0, anchorCreatedCounter, "Counter for anchors created");
            Assert.AreEqual(0, anchorDeletedCounter, "Counter for anchors deleted");
            Assert.AreEqual(0, anchorUpdatedCounter, "Counter for anchors updated");

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomAnchorPlaneBoundaryChanged()
        {
            int roomUpdatedCounter = 0;
            int roomDeletedCounter = 0;
            int roomCreatedCounter = 0;

            int anchorUpdatedCounter = 0;
            int anchorDeletedCounter = 0;
            int anchorCreatedCounter = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => roomUpdatedCounter++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => roomCreatedCounter++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => roomDeletedCounter++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => anchorCreatedCounter++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => anchorDeletedCounter++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => anchorUpdatedCounter++);

            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithRoom1SceneAnchorPlaneBoundaryChanged.text);
            Debug.Log($"roomUpdatedCounter {roomUpdatedCounter} roomDeletedCounter {roomDeletedCounter} roomCreatedCounter {roomCreatedCounter} " +
                      $"anchorUpdatedCounter {anchorUpdatedCounter} anchorDeletedCounter {anchorDeletedCounter} anchorCreatedCounter {anchorCreatedCounter}");

            Assert.AreEqual(1, roomUpdatedCounter, "Counter for rooms updated");
            Assert.AreEqual(0, roomCreatedCounter, "Counter for rooms created");
            Assert.AreEqual(0, roomDeletedCounter, "Counter for rooms deleted");
            Assert.AreEqual(0, anchorCreatedCounter, "Counter for anchors created");
            Assert.AreEqual(0, anchorDeletedCounter, "Counter for anchors deleted");
            Assert.AreEqual(1, anchorUpdatedCounter, "Counter for anchors updated");

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomAnchorVolumeBoundsChanged()
        {
            int roomUpdatedCounter = 0;
            int roomDeletedCounter = 0;
            int roomCreatedCounter = 0;

            int anchorUpdatedCounter = 0;
            int anchorDeletedCounter = 0;
            int anchorCreatedCounter = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => roomUpdatedCounter++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => roomCreatedCounter++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => roomDeletedCounter++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => anchorCreatedCounter++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => anchorDeletedCounter++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => anchorUpdatedCounter++);

            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithRoom1SceneAnchorVolumeBoundsChanged.text);

            Debug.Log($"roomUpdatedCounter {roomUpdatedCounter} roomDeletedCounter {roomDeletedCounter} roomCreatedCounter {roomCreatedCounter} " +
                      $"anchorUpdatedCounter {anchorUpdatedCounter} anchorDeletedCounter {anchorDeletedCounter} anchorCreatedCounter {anchorCreatedCounter}");

            Assert.AreEqual(1, roomUpdatedCounter, "Counter for rooms updated");
            Assert.AreEqual(0, roomCreatedCounter, "Counter for rooms created");
            Assert.AreEqual(0, roomDeletedCounter, "Counter for rooms deleted");
            Assert.AreEqual(0, anchorCreatedCounter, "Counter for anchors created");
            Assert.AreEqual(0, anchorDeletedCounter, "Counter for anchors deleted");
            Assert.AreEqual(1, anchorUpdatedCounter, "Counter for anchors updated");

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomAnchorPlaneRectChanged()
        {
            int roomUpdatedCounter = 0;
            int roomDeletedCounter = 0;
            int roomCreatedCounter = 0;

            int anchorUpdatedCounter = 0;
            int anchorDeletedCounter = 0;
            int anchorCreatedCounter = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => roomUpdatedCounter++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => roomCreatedCounter++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => roomDeletedCounter++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => anchorCreatedCounter++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => anchorDeletedCounter++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => anchorUpdatedCounter++);

            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithRoom1SceneAnchorPlaneRectChanged.text);

            Debug.Log($"roomUpdatedCounter {roomUpdatedCounter} roomDeletedCounter {roomDeletedCounter} roomCreatedCounter {roomCreatedCounter} " +
                      $"anchorUpdatedCounter {anchorUpdatedCounter} anchorDeletedCounter {anchorDeletedCounter} anchorCreatedCounter {anchorCreatedCounter}");

            Assert.AreEqual(1, roomUpdatedCounter, "Counter for rooms updated");
            Assert.AreEqual(0, roomCreatedCounter, "Counter for rooms created");
            Assert.AreEqual(0, roomDeletedCounter, "Counter for rooms deleted");
            Assert.AreEqual(0, anchorCreatedCounter, "Counter for anchors created");
            Assert.AreEqual(0, anchorDeletedCounter, "Counter for anchors deleted");
            Assert.AreEqual(1, anchorUpdatedCounter, "Counter for anchors updated");

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomAnchorLabelChanged()
        {
            int roomUpdatedCounter = 0;
            int roomDeletedCounter = 0;
            int roomCreatedCounter = 0;

            int anchorUpdatedCounter = 0;
            int anchorDeletedCounter = 0;
            int anchorCreatedCounter = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => roomUpdatedCounter++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => roomCreatedCounter++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => roomDeletedCounter++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => anchorCreatedCounter++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => anchorDeletedCounter++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => anchorUpdatedCounter++);

            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithRoom1SceneAnchorLabelChanged.text);

            Debug.Log($"roomUpdatedCounter {roomUpdatedCounter} roomDeletedCounter {roomDeletedCounter} roomCreatedCounter {roomCreatedCounter} " +
                      $"anchorUpdatedCounter {anchorUpdatedCounter} anchorDeletedCounter {anchorDeletedCounter} anchorCreatedCounter {anchorCreatedCounter}");

            Assert.AreEqual(1, roomUpdatedCounter, "Counter for rooms updated");
            Assert.AreEqual(0, roomCreatedCounter, "Counter for rooms created");
            Assert.AreEqual(0, roomDeletedCounter, "Counter for rooms deleted");
            Assert.AreEqual(0, anchorCreatedCounter, "Counter for anchors created");
            Assert.AreEqual(0, anchorDeletedCounter, "Counter for anchors deleted");
            Assert.AreEqual(1, anchorUpdatedCounter, "Counter for anchors updated");

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator RoomUUIDChanged()
        {
            int roomUpdatedCounter = 0;
            int roomDeletedCounter = 0;
            int roomCreatedCounter = 0;

            int anchorUpdatedCounter = 0;
            int anchorDeletedCounter = 0;
            int anchorCreatedCounter = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => roomUpdatedCounter++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => roomCreatedCounter++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => roomDeletedCounter++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => anchorCreatedCounter++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => anchorDeletedCounter++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => anchorUpdatedCounter++);

            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithScene1NewRoomGUID.text);
            Debug.Log($"roomUpdatedCounter {roomUpdatedCounter} roomDeletedCounter {roomDeletedCounter} roomCreatedCounter {roomCreatedCounter} " +
                      $"anchorUpdatedCounter {anchorUpdatedCounter} anchorDeletedCounter {anchorDeletedCounter} anchorCreatedCounter {anchorCreatedCounter}");

            Assert.AreEqual(1, roomUpdatedCounter, "Counter for rooms updated");
            Assert.AreEqual(0, roomCreatedCounter, "Counter for rooms created");
            Assert.AreEqual(0, roomDeletedCounter, "Counter for rooms deleted");
            Assert.AreEqual(0, anchorCreatedCounter, "Counter for anchors created");
            Assert.AreEqual(0, anchorDeletedCounter, "Counter for anchors deleted");
            Assert.AreEqual(0, anchorUpdatedCounter, "Counter for anchors updated");

            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator Room2Loaded()
        {
            int roomUpdatedCounter = 0;
            int roomDeletedCounter = 0;
            int roomCreatedCounter = 0;

            int anchorUpdatedCounter = 0;
            int anchorDeletedCounter = 0;
            int anchorCreatedCounter = 0;
            MRUK.Instance.RoomUpdatedEvent.AddListener(room => roomUpdatedCounter++);
            MRUK.Instance.RoomCreatedEvent.AddListener(room => roomCreatedCounter++);
            MRUK.Instance.RoomRemovedEvent.AddListener(room => roomDeletedCounter++);

            _currentRoom.AnchorCreatedEvent.AddListener(anchor => anchorCreatedCounter++);
            _currentRoom.AnchorRemovedEvent.AddListener(anchor => anchorDeletedCounter++);
            _currentRoom.AnchorUpdatedEvent.AddListener(anchor => anchorUpdatedCounter++);

            yield return LoadSceneFromJsonStringAndWait(_jsonTestHelper.SceneWithRoom2.text);

            Debug.Log($"roomUpdatedCounter {roomUpdatedCounter} roomDeletedCounter {roomDeletedCounter} roomCreatedCounter {roomCreatedCounter} " +
                      $"anchorUpdatedCounter {anchorUpdatedCounter} anchorDeletedCounter {anchorDeletedCounter} anchorCreatedCounter {anchorCreatedCounter}");

            Assert.AreEqual(0, roomUpdatedCounter, "Counter for rooms updated");
            Assert.AreEqual(1, roomCreatedCounter, "Counter for rooms created");
            Assert.AreEqual(1, roomDeletedCounter, "Counter for rooms deleted");
            Assert.AreEqual(0, anchorCreatedCounter, "Counter for anchors created");
            Assert.AreEqual(12, anchorDeletedCounter, "Counter for anchors deleted");
            Assert.AreEqual(0, anchorUpdatedCounter, "Counter for anchors updated");

            yield return null;
        }
    }
}

