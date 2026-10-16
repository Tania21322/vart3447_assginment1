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

using System.Text;
using UnityEngine;

namespace Oculus.Interaction.Telemetry
{
    internal static class ISDKTelemetryUtils
    {
        /// <summary>
        /// Build a JSON metadata string from key-value pairs.
        /// Shared by both Runtime and Editor telemetry event classes.
        /// </summary>
        internal static string BuildMetadataJson(params string[] keyValuePairs)
        {
            Debug.Assert(keyValuePairs.Length % 2 == 0,
                "[ISDK Telemetry] BuildMetadataJson requires an even number of arguments (key-value pairs).");

            var sb = new StringBuilder("{");
            for (int i = 0; i + 1 < keyValuePairs.Length; i += 2)
            {
                if (i > 0) sb.Append(",");
                sb.AppendFormat("\"{0}\":\"{1}\"",
                    EscapeJsonString(keyValuePairs[i]),
                    EscapeJsonString(keyValuePairs[i + 1]));
            }
            sb.Append("}");
            return sb.ToString();
        }

        private static string EscapeJsonString(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
        }
    }
}
