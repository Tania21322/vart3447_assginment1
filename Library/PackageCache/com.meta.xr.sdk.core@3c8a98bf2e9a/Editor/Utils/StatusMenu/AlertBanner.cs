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
using Meta.XR.Editor.UserInterface.RLDS;
using UnityEngine;
using UnityEngine.UIElements;

namespace Meta.XR.Editor.UserInterface
{
    /// <summary>
    /// A dismissible informational banner for the Welcome window, following RLDS design.
    /// Displays optional left icon, message text, and close button on right.
    /// Blue background with darker blue border by default.
    /// </summary>
    internal class AlertBanner : IUserInterfaceItem
    {
        public bool Hide { get; set; }

        private readonly string _message;
        private readonly TextureContent _icon;
        private readonly Action _onDismiss;
        private readonly bool _wideMargins;

        private VisualElement _root;
        private VisualElement _closeButton;

        public event Action Dismissed;

        public AlertBanner(string message, TextureContent icon = null, Action onDismiss = null, bool wideMargins = false)
        {
            _message = message;
            _icon = icon;
            _onDismiss = onDismiss;
            _wideMargins = wideMargins;
        }

        public void Draw()
        {
            // UIToolkit-only component; IMGUI rendering is not supported.
        }

        public VisualElement Build()
        {
            if (_root != null) return _root;

            _root = new VisualElement();
            _root.AddToClassList(RLDSConstants.AlertBanner.Base);
            if (_wideMargins)
            {
                _root.AddToClassList(RLDSConstants.AlertBanner.Welcome);
            }

            var left = new VisualElement();
            left.AddToClassList(RLDSConstants.AlertBanner.Left);

            if (_icon != null)
            {
                var iconEl = new VisualElement();
                iconEl.AddToClassList(RLDSConstants.AlertBanner.Icon);
                _icon.RegisterToImageLoaded(tex => iconEl.style.backgroundImage = tex as Texture2D);
                left.Add(iconEl);
            }

            var label = new UnityEngine.UIElements.Label(_message ?? string.Empty);
            label.AddToClassList(RLDSConstants.AlertBanner.Label);
            left.Add(label);

            _root.Add(left);

            _closeButton = new VisualElement();
            _closeButton.AddToClassList(RLDSConstants.AlertBanner.CloseButton);
            _closeButton.AddToClassList(RLDSConstants.Utilities.CursorLink);
            _closeButton.RegisterCallback<ClickEvent>(_ =>
            {
                Dismiss();
                _onDismiss?.Invoke();
                Dismissed?.Invoke();
            });

            var closeIconEl = new VisualElement();
            closeIconEl.AddToClassList(RLDSConstants.AlertBanner.CloseIcon);
            Styles.Contents.CloseIcon.RegisterToImageLoaded(tex => closeIconEl.style.backgroundImage = tex as Texture2D);
            _closeButton.Add(closeIconEl);

            _root.Add(_closeButton);

            if (Hide)
            {
                _root.style.display = DisplayStyle.None;
            }

            return _root;
        }

        public void Dismiss()
        {
            Hide = true;
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
            }
        }

        public void Show()
        {
            Hide = false;
            if (_root != null)
            {
                _root.style.display = DisplayStyle.Flex;
            }
        }

        public void UpdateContent(string message)
        {
            // Update label text if already built
            if (_root == null) return;
            var label = _root.Q<UnityEngine.UIElements.Label>(className: RLDSConstants.AlertBanner.Label);
            if (label != null)
            {
                label.text = message ?? string.Empty;
            }
        }
    }
}
