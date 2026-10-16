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

namespace Meta.XR.MRUtilityKit
{
    [AddComponentMenu("")] // Use empty string to hide it from "Add Component" menu
    internal class MRUKGlobalContext : MonoBehaviour
    {
        private bool _isQuitting;

        private void OnEnable() => Application.quitting += OnApplicationQuitting;
        private void OnDisable() => Application.quitting -= OnApplicationQuitting;
        private void OnApplicationQuitting() => _isQuitting = true;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void CreateInstance()
        {
            var go = new GameObject(nameof(MRUKGlobalContext))
            {
                hideFlags = HideFlags.HideInHierarchy
            };
            DontDestroyOnLoad(go);
            go.AddComponent<MRUKGlobalContext>();
        }

        private void Update() => MRUK.UpdateGlobalContext();

        private void OnDestroy()
        {
            if (!_isQuitting)
            {
#if UNITY_6000_4_OR_NEWER
                Debug.LogError($"{nameof(MRUKGlobalContext)} with instance id {GetEntityId()} was destroyed manually, this will prevent MRUK from working correctly. Recreating {nameof(MRUKGlobalContext)}...");
#else
                Debug.LogError($"{nameof(MRUKGlobalContext)} with instance id {GetInstanceID()} was destroyed manually, this will prevent MRUK from working correctly. Recreating {nameof(MRUKGlobalContext)}...");
#endif
                CreateInstance();
            }
        }
    }
}
