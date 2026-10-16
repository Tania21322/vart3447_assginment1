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
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

namespace Meta.XR.Editor.ToolingSupport
{
    internal static class ToolRegistry
    {
        private static readonly List<ToolDescriptor> _registry = new();
        private static readonly List<ToolDescriptor> ToInitialize = new();
        private static bool _initializing = false;

        public static IEnumerable<ToolDescriptor> Registry => _registry;

        public static int? LatestAvailableVersion
        {
            get
            {
                int? latest = null;
                foreach (var item in _registry)
                {
                    if (item.AvailableVersionDelegate == null || item.PillIcon == null)
                    {
                        continue;
                    }

                    var (_, color, showNotification) = item.PillIcon();
                    if (!showNotification || !color.HasValue)
                    {
                        continue;
                    }

                    var version = item.AvailableVersionDelegate();
                    if (version.HasValue && (!latest.HasValue || version.Value > latest.Value))
                    {
                        latest = version;
                    }
                }

                return latest;
            }
        }

        /// <summary>
        /// True once descriptor initialization (including remote ramp-up resolution) has completed at
        /// least once. Before this, ramp-gated descriptors report IsRampedUp == false.
        /// </summary>
        public static bool IsInitialized { get; private set; }

        /// <summary>
        /// Raised after a batch of descriptors finishes initializing, once their ramp-up state has been
        /// resolved from the remote keys. Consumers that cached a filtered view of the registry (e.g.
        /// the status menu) should re-derive it here so ramp-gated items that were hidden while keys
        /// were still loading become visible.
        /// </summary>
        public static event Action OnInitialized;

        /// <summary>
        /// Raised when a tool's status changes. Surfaces that render cached status badges
        /// should re-derive and repaint here so their display stays in sync
        /// instead of going stale until the window is reopened or a row is clicked. The changed
        /// descriptor is passed when known; null indicates a non-specific/global status change.
        /// </summary>
        public static event Action<ToolDescriptor> OnStatusChanged;

        /// <summary>
        /// Notifies listeners that a tool's status has changed. Tools call this after mutating any
        /// state that their status delegates (StatusBadges / InfoTextDelegate / PillIcon) read from.
        /// </summary>
        /// <param name="descriptor">The descriptor whose status changed, or null for a global change.</param>
        public static void NotifyStatusChanged(ToolDescriptor descriptor = null)
        {
            OnStatusChanged?.Invoke(descriptor);
        }

        public static void Unregister(string name)
        {
            _registry.RemoveAll(d => d.Name == name);
            ToInitialize.RemoveAll(d => d.Name == name);
        }

        public static void Register(ToolDescriptor descriptor)
        {
            if (_registry.Any(d => d.Name == descriptor.Name))
            {
                return;
            }

            _registry.Add(descriptor);

            // Register a delayed callback to initialize descriptors
            if (!ToInitialize.Any() && !_initializing)
            {
                EditorApplication.update += InitializeDescriptorsAsync;
            }

            ToInitialize.Add(descriptor);
        }

        private static async void InitializeDescriptorsAsync()
        {
            // Remove the callback immediately to prevent multiple calls
            EditorApplication.update -= InitializeDescriptorsAsync;
            _initializing = true;

            // First, wait for all ramp-up checks to complete
            var rampUpTasks = ToInitialize
                .Where(d => d.EnableRampUp)
                .Select(d => d.CheckRampUpAsync())
                .ToArray();

            if (rampUpTasks.Any())
            {
                await Task.WhenAll(rampUpTasks);
            }

            // For tools without ramp-up, just mark them as checked
            foreach (var descriptor in ToInitialize.Where(d => !d.EnableRampUp))
            {
                await descriptor.CheckRampUpAsync();
            }

            // Now initialize all tools (only ramped-up ones will register menus)
            foreach (var descriptor in ToInitialize)
            {
                descriptor.Initialize();
            }

            ToInitialize.Clear();
            _initializing = false;
            IsInitialized = true;
            OnInitialized?.Invoke();
        }
    }
}
