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

using System.Collections.Generic;
using System.Globalization;
using RO = Meta.XR.RuntimeOptimizer.Core;

namespace Meta.XR.RuntimeOptimizer.Editor
{
    /// <summary>
    /// Parses the delimited scene-diagnosis payload sent by the on-device service.
    /// </summary>
    /// <remarks>
    /// Wire format is <c>success;GameObjectName:a/b,CpuRenderThreadTime:,CpuMainThreadTime:1.23;...</c>
    /// or the single token <c>failed;</c>. GameObject names are arbitrary user strings, so the
    /// parser must tolerate separators appearing inside a name rather than throwing.
    /// </remarks>
    internal static class SceneDiagnosisPayload
    {
        internal const string SuccessToken = "success";

        internal static bool IsSuccess(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return false;
            }

            var firstToken = message.Split(';')[0].Trim();
            return firstToken.Equals(SuccessToken, System.StringComparison.OrdinalIgnoreCase);
        }

        internal static List<RO.GameObjectPerformanceMetaData> ParseDataPoints(string message)
        {
            var results = new List<RO.GameObjectPerformanceMetaData>();
            if (string.IsNullOrEmpty(message))
            {
                return results;
            }

            var records = message.Split(';');
            for (int i = 1; i < records.Length; i++)
            {
                var dataPoint = records[i];
                if (string.IsNullOrEmpty(dataPoint))
                {
                    continue;
                }

                var performanceData = new RO.GameObjectPerformanceMetaData();
                foreach (var property in dataPoint.Split(','))
                {
                    // Split on the first separator only: a GameObject name may contain ':'.
                    // Fragments with no separator at all are skipped rather than indexed into.
                    var separatorIndex = property.IndexOf(':');
                    if (separatorIndex < 0)
                    {
                        continue;
                    }

                    var key = property.Substring(0, separatorIndex).Trim();
                    var value = property.Substring(separatorIndex + 1).Trim();
                    switch (key)
                    {
                        case "GameObjectName":
                            performanceData.GameObjectName = value;
                            break;
                        case "CpuMainThreadTime":
                            // Empty for the individual GameObjects of a group-mode result.
                            if (!string.IsNullOrEmpty(value)
                                && double.TryParse(value, NumberStyles.Float,
                                    CultureInfo.InvariantCulture, out var parsedTime))
                            {
                                performanceData.CpuMainThreadTime = parsedTime;
                            }

                            break;
                    }
                }

                results.Add(performanceData);
            }

            return results;
        }
    }
}
