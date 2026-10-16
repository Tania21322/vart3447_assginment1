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

namespace Oculus.Interaction
{
    public class EnableGazeDebugVisuals : MonoBehaviour
    {
        public void ToggleDebugVisuals(bool isOn)
        {
            var canvases = FindObjectsByType<ConecastableCanvas>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var canvas in canvases)
            {
                var debugField = typeof(ConecastableCanvas).GetField("_enableDebugVisuals",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                debugField.SetValue(canvas, isOn);
            }

            var conecasters = FindObjectsByType<GazeConecaster>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var conecaster in conecasters)
            {
                var debugField = typeof(GazeConecaster).GetField("_enableDebugVisuals",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                debugField.SetValue(conecaster, isOn);
            }
        }
    }
}
