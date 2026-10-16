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

namespace Meta.XR.Editor.UserInterface
{
    /// <summary>
    /// Shared alert banner message constants used across Welcome window and Status Menu.
    /// Centralizing these strings prevents divergence during localization or wording updates.
    /// </summary>
    public static class AlertBannerMessages
    {
        /// <summary>
        /// Message shown when the main toolbar button is not available (pre-6000.3 Unity).
        /// Instructs user how to reopen the SDK panel via Window menu.
        /// </summary>
        public const string AlertBannerMessage =
            "You can open the Meta XR SDK panel by going to Unity's system toolbar and selecting <b>Window > Meta > Meta XR SDK</b>";

        /// <summary>
        /// Message shown when main toolbar is available (Unity 6000.3+).
        /// Instructs user to pin the SDK menu to the toolbar.
        /// </summary>
        public const string AlertBannerMessagePinned =
            "<b>Access XR tools and updates faster</b>\nRight-click Unity's toolbar and select <b>Meta XR SDK > SDK menu</b> to pin the SDK.";
    }
}
