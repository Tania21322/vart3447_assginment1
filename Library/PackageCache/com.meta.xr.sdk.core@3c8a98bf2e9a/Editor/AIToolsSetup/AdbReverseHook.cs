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

using Meta.XR.AI.AgentBridge;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Editor
{
    /// <summary>
    /// Registers an ADB-reverse provider on <see cref="RemoteAgentServer"/> so the AgentBridge Remote
    /// Server can set up reverse tunnels when it starts.
    /// </summary>
    /// <remarks>
    /// <see cref="RemoteAgentServer"/> lives in the AgentBridge editor assembly, which cannot reference the
    /// Oculus editor ADB tooling (<see cref="OVRADBTool"/> / <see cref="OVRConfig"/>) without creating an
    /// assembly dependency cycle. This initializer lives in the AI Tools Setup assembly, which already
    /// references both, and injects the implementation through the server's delegate seam.
    /// </remarks>
    [InitializeOnLoad]
    internal static class AdbReverseHook
    {
        static AdbReverseHook()
        {
            RemoteAgentServer.AdbReverseConfigurer = ConfigureReverse;
        }

        private static bool ConfigureReverse(int port)
        {
            var result = AdbSetupUtility.ConfigureReverse(
                OVRConfig.Instance.GetAndroidSDKPath(), new[] { port });

            if (!result.AdbAvailable)
            {
                Debug.Log("[AgentBridge] ADB not available; remote clients will use network fallback.");
            }
            else if (result.AuthorizedDeviceCount == 0)
            {
                Debug.Log("[AgentBridge] No authorized ADB device found; remote clients will use network fallback.");
            }
            else if (result.Ok)
            {
                Debug.Log($"[AgentBridge] Configured ADB reverse tcp:{port} for {result.AuthorizedDeviceCount} " +
                          "device(s). Remote clients can connect through 127.0.0.1.");
            }

            return result.Ok;
        }
    }
}
