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
using System.Collections.Generic;
using System.Threading.Tasks;
using Meta.XR.Editor.Callbacks;
using Meta.XR.Editor.UserInterface;
using UnityEditor;
using UnityEngine;

namespace Meta.HandReadinessTool.Editor
{
    /// <summary>
    /// Remote source for the tool's screen copy — the window/screen UI strings —
    /// so the device-readiness messaging can be tuned without shipping a new SDK
    /// version. Mirrors <see cref="HandReadinessPromptProvider"/>: fetches a static
    /// JSON asset at editor startup, keyed by a stable string id. Screens call
    /// <see cref="Get"/> with that key and their bundled default; the default is used
    /// whenever the remote payload is absent, is missing that key, has an empty value
    /// for it, the fetch fails, or the schema version doesn't match — so the UI always
    /// renders complete copy even offline.
    /// </summary>
    [InitializeOnLoad]
    internal static class HandReadinessScreenContentProvider
    {
        // bb_content id of the remote screen-copy payload; each Get() falls back to its
        // bundled default when the fetch fails or the key is absent. Bump when uploading
        // a new payload version. MUST stay static readonly (not const) so the `== 0UL`
        // guard below isn't constant-folded into unreachable FetchAndSwap() under
        // warnings-as-errors.
        private static readonly ulong ScreenContentId = 27829343043367588UL;
        private const string CacheFileName = "hrt_screen_copy.json";
        private const int SupportedSchemaVersion = 1;

        private static Dictionary<string, string> _entries;

        static HandReadinessScreenContentProvider()
        {
            InitializeOnLoad.Register(Initialize);
        }

        /// <summary>
        /// Remote copy for <paramref name="key"/>, or <paramref name="fallback"/> when
        /// no remote payload is loaded or it has no non-empty entry for the key.
        /// </summary>
        public static string Get(string key, string fallback)
        {
            if (_entries != null
                && _entries.TryGetValue(key, out var value)
                && !string.IsNullOrEmpty(value))
            {
                return value;
            }
            return fallback;
        }

        private static void Initialize()
        {
            // No remote payload provisioned yet — screens keep their bundled copy and
            // we skip the fetch entirely.
            if (ScreenContentId == 0UL)
            {
                return;
            }
#pragma warning disable CS4014
            FetchAndSwap();
#pragma warning restore CS4014
        }

        private static async Task FetchAndSwap(bool clearCache = false)
        {
            try
            {
                var result =
                    await RemoteJsonContent<ScreenCopyPayload>.Create(
                        CacheFileName, ScreenContentId, clearCache: clearCache);
                if (!result.IsSuccess)
                {
                    return;
                }

                var payload = result.Content;
                if (payload.version != SupportedSchemaVersion)
                {
                    return;
                }

                _entries = BuildMap(payload.entries);
            }
            catch (Exception)
            {
                // Remote fetch threw — keep the bundled screen copy.
            }
        }

        private static Dictionary<string, string> BuildMap(ScreenCopyEntry[] entries)
        {
            var map = new Dictionary<string, string>();
            if (entries == null) return map;

            foreach (var e in entries)
            {
                if (string.IsNullOrEmpty(e.key)) continue;
                map[e.key] = e.value;
            }
            return map;
        }

        [Serializable]
        internal struct ScreenCopyPayload
        {
            public int version;
            public ScreenCopyEntry[] entries;
        }

        [Serializable]
        internal struct ScreenCopyEntry
        {
            public string key;
            public string value;
        }

    }
}
