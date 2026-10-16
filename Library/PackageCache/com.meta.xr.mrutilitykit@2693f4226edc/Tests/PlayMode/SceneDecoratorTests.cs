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
using Meta.XR.MRUtilityKit.SceneDecorator;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Meta.XR.MRUtilityKit.Tests
{
    public class SceneDecoratorTests : MRUKTestBase
    {
        private static readonly int ExpectedCubeGridFloorNotInsideGlobalMesh = 7;
        private static readonly int ExpectedEggSimplexWallPhysicsCollider = 18;
        private static readonly int ExpectedFloorCeilingWallGlobalMeshStaggered = 988;
        protected override string SceneToTest => "Packages/com.meta.xr.mrutilitykit/Tests/SceneDecoratorTests.unity";

        private IEnumerator RunTest(int decorationIndex, int expected)
        {
            var decorator = SetupSceneDecorator();
            var decoration = GetSceneDecoration(decorationIndex);
            decorator.sceneDecorations.Clear();
            decorator.sceneDecorations.Add(decoration);
            yield return LoadSceneFromJsonStringAndWait(GetJsonString());
            decorator.DecorateScene();
            yield return null;
            var createdDecorations = CountDecorations(decorationIndex);
            Assert.AreEqual(expected, createdDecorations);
            yield return null;
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator CubeGridFloorNotInside_GlobalMesh()
        {
            yield return RunTest(0, ExpectedCubeGridFloorNotInsideGlobalMesh);
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator EggSimplexWallPhysicsCollider()
        {
            yield return RunTest(1, ExpectedEggSimplexWallPhysicsCollider);
        }

        [UnityTest]
        [Timeout(DefaultTimeoutMs)]
        public IEnumerator FloorCeilingWallGlobalMeshStaggered()
        {
            yield return RunTest(2, ExpectedFloorCeilingWallGlobalMeshStaggered);
        }

        private int CountDecorations(int decorationsIndex)
        {
            var counter = 0;
            var prefab = GetSceneDecoration(decorationsIndex).decorationPrefabs[0];
            foreach (var go in Object.FindObjectsByType<PoolManagerComponent.PoolableData>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (go.name.Length < prefab.name.Length)
                {
                    continue;
                }

                if (go.name.Substring(0, prefab.name.Length) == prefab.name && go.gameObject.activeSelf)
                {
                    counter++;
                }

            }
            return counter;
        }

        private string GetJsonString()
        {
            var decorationRefs = Object.FindAnyObjectByType<SceneDecoratorTestReferences>();
            if (decorationRefs == null)
            {
                Assert.Fail();
            }
            return decorationRefs.Scene1.text;
        }

        private SceneDecoration GetSceneDecoration(int index)
        {
            var decorationRefs = Object.FindAnyObjectByType<SceneDecoratorTestReferences>();
            if (decorationRefs == null)
            {
                Assert.Fail();
            }
            return decorationRefs.Decorations[index];
        }
        private SceneDecorator.SceneDecorator SetupSceneDecorator()
        {
            var sceneDecorator = Object.FindAnyObjectByType<SceneDecorator.SceneDecorator>();
            sceneDecorator.recursionLimit = 1;
            sceneDecorator.DecorateOnStart = MRUK.RoomFilter.None;
            sceneDecorator.TrackUpdates = true;
            if (sceneDecorator == null)
            {
                Assert.Fail();
            }
            return sceneDecorator;
        }
    }
}
