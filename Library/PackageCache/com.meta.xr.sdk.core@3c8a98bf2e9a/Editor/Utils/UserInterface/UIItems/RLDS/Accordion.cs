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
using UnityEngine.UIElements;

namespace Meta.XR.Editor.UserInterface.RLDS
{
    /// <summary>
    /// A collapsible disclosure section: a clickable header (optional leading
    /// icon, title, trailing chevron) that shows or hides a caller-provided
    /// content region.
    /// </summary>
    internal class Accordion : IUserInterfaceItem
    {
        public bool Hide { get; set; }

        private readonly string _title;
        private readonly VisualElement _content;
        private readonly TextureContent _leadingIcon;
        private readonly Action<bool> _onToggle;
        private bool _expanded;

        private VisualElement _root;

        /// <summary>Creates a collapsible accordion.</summary>
        /// <param name="title">Header label.</param>
        /// <param name="content">Element revealed when expanded. Owned by the accordion.</param>
        /// <param name="expanded">Initial expanded state (collapsed by default).</param>
        /// <param name="leadingIcon">Optional icon shown before the title.</param>
        /// <param name="onToggle">Optional callback invoked with the new expanded state on user toggle.</param>
        public Accordion(
            string title,
            VisualElement content,
            bool expanded = false,
            TextureContent leadingIcon = null,
            Action<bool> onToggle = null)
        {
            _title = title;
            _content = content ?? throw new ArgumentNullException(nameof(content));
            _expanded = expanded;
            _leadingIcon = leadingIcon;
            _onToggle = onToggle;
        }

        /// <summary>Expanded state. Setting it updates the rendered element if built.</summary>
        public bool Expanded
        {
            get => _expanded;
            set
            {
                _expanded = value;
                ApplyExpanded();
            }
        }

        public void Draw()
        {
            // UIToolkit-only component; IMGUI rendering is not supported.
        }

        public VisualElement Build()
        {
            if (_root != null)
            {
                return _root;
            }

            _root = new VisualElement();
            _root.AddToClassList(RLDSConstants.Accordion.Root);

            var header = new VisualElement();
            header.AddToClassList(RLDSConstants.Accordion.Header);
            header.focusable = true;

            if (_leadingIcon != null)
            {
                var leading = new VisualElement();
                leading.AddToClassList(RLDSConstants.Accordion.LeadingIcon);
                _leadingIcon.RegisterToImageLoaded(tex => leading.style.backgroundImage = tex as Texture2D);
                header.Add(leading);
            }

            var titleLabel = new UnityEngine.UIElements.Label(_title);
            titleLabel.AddToClassList(RLDSConstants.Accordion.Title);
            titleLabel.AddToClassList(RLDSConstants.Typography.Body1Label);
            header.Add(titleLabel);

            var chevron = new VisualElement();
            chevron.AddToClassList(RLDSConstants.Accordion.Chevron);
            UserInterface.Styles.Contents.DownArrowIcon.RegisterToImageLoaded(
                tex => chevron.style.backgroundImage = tex as Texture2D);
            header.Add(chevron);

            header.AddManipulator(new Clickable(Toggle));
            header.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return
                    || evt.keyCode == KeyCode.KeypadEnter
                    || evt.keyCode == KeyCode.Space)
                {
                    Toggle();
                    evt.StopPropagation();
                }
            });

            _content.AddToClassList(RLDSConstants.Accordion.Content);

            _root.Add(header);
            _root.Add(_content);

            ApplyExpanded();

            return _root;
        }

        private void Toggle()
        {
            _expanded = !_expanded;
            ApplyExpanded();
            _onToggle?.Invoke(_expanded);
        }

        private void ApplyExpanded()
        {
            if (_root == null)
            {
                return;
            }

            // Show/hide is USS-class-driven (--expanded) so caller inline styles aren't clobbered.
            _root.EnableInClassList(RLDSConstants.Accordion.Expanded, _expanded);
        }
    }
}
