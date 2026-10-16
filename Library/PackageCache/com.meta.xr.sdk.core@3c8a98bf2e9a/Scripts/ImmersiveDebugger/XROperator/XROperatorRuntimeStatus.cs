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
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.XROperator
{
    internal enum XROperatorConnectionStatus
    {
        Unavailable,
        Registering,
        Connected,
        Error
    }

    internal static class XROperatorRuntimeStatus
    {
        internal static event Action<XROperatorConnectionStatus, string> OnConnectionStatusChanged;

        internal static XROperatorConnectionStatus ConnectionStatus { get; private set; } =
            XROperatorConnectionStatus.Unavailable;

        internal static string StatusMessage { get; private set; } = "XR Operator unavailable";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Init()
        {
            // Reset static state at subsystem registration so a stale subscriber or status from a
            // previous play session doesn't linger when Enter Play Mode has domain reload disabled.
            OnConnectionStatusChanged = null;
            ConnectionStatus = XROperatorConnectionStatus.Unavailable;
            StatusMessage = "XR Operator unavailable";
        }

        internal static void SetConnectionStatus(XROperatorConnectionStatus status, string message)
        {
            message ??= string.Empty;
            if (ConnectionStatus == status && StatusMessage == message)
            {
                return;
            }

            ConnectionStatus = status;
            StatusMessage = message;
            OnConnectionStatusChanged?.Invoke(status, message);
        }
    }
}
