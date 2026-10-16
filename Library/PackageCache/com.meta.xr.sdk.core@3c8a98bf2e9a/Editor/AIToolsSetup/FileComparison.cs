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

using System.IO;

namespace Meta.XR.Editor
{
    /// <summary>
    /// Small file helper used by the AI Tools setup wizard to detect when an installed binary differs
    /// from the one bundled in the SDK. The comparison is content-based (not a stored checksum): both
    /// files are present on disk at check time, so a direct byte compare is enough.
    /// </summary>
    internal static class FileComparison
    {
        // Returns true only when both files exist and are byte-for-byte identical.
        internal static bool ContentsEqual(string pathA, string pathB)
        {
            if (string.IsNullOrEmpty(pathA) || string.IsNullOrEmpty(pathB))
                return false;
            if (!File.Exists(pathA) || !File.Exists(pathB))
                return false;

            var infoA = new FileInfo(pathA);
            var infoB = new FileInfo(pathB);
            if (infoA.Length != infoB.Length)
                return false;

            const int bufferSize = 64 * 1024;
            using (var streamA = new FileStream(pathA, FileMode.Open, FileAccess.Read))
            using (var streamB = new FileStream(pathB, FileMode.Open, FileAccess.Read))
            {
                var bufA = new byte[bufferSize];
                var bufB = new byte[bufferSize];
                int readA;
                while ((readA = streamA.Read(bufA, 0, bufferSize)) > 0)
                {
                    var readB = 0;
                    while (readB < readA)
                    {
                        var n = streamB.Read(bufB, readB, readA - readB);
                        if (n == 0) return false;
                        readB += n;
                    }
                    for (var i = 0; i < readA; i++)
                        if (bufA[i] != bufB[i]) return false;
                }
            }
            return true;
        }
    }
}
