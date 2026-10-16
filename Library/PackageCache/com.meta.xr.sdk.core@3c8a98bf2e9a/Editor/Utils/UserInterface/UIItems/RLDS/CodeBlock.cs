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
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Meta.XR.Editor.UserInterface.RLDS
{
    /// <summary>
    /// A read-only, scrollable code/command display rendered in the RLDS code
    /// typography, with an optional language label and a copy-to-clipboard button.
    /// The text is also user-selectable for manual copy.
    /// </summary>
    internal class CodeBlock : IUserInterfaceItem
    {
        private const string CopyLabel = "Copy";
        private const string CopiedLabel = "Copied!";
        private const long CopiedResetMs = 1500;

        public bool Hide { get; set; }

        private readonly string _code;
        private readonly string _language;
        private readonly bool _showCopyButton;
        private readonly TextureContent _copyIcon;
        private readonly Action _onCopied;
        private VisualElement _root;
        private UnityEngine.UIElements.Button _copyButton;
        private UnityEngine.UIElements.IVisualElementScheduledItem _copyResetItem;

        /// <summary>Creates a code block.</summary>
        /// <param name="code">The code/command text to display and copy.</param>
        /// <param name="language">Optional language/label shown above the code.</param>
        /// <param name="showCopyButton">Whether to render the copy-to-clipboard button.</param>
        /// <param name="copyIcon">Optional icon for the copy button (label-only if null).</param>
        /// <param name="onCopied">Optional callback invoked after a successful copy.</param>
        public CodeBlock(
            string code,
            string language = null,
            bool showCopyButton = true,
            TextureContent copyIcon = null,
            Action onCopied = null)
        {
            _code = code ?? string.Empty;
            _language = language;
            _showCopyButton = showCopyButton;
            _copyIcon = copyIcon;
            _onCopied = onCopied;
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
            _root.AddToClassList(RLDSConstants.CodeBlockView.Container);
            _root.RegisterCallback<UnityEngine.UIElements.DetachFromPanelEvent>(_ =>
            {
                _copyResetItem?.Pause();
                if (_copyButton != null)
                {
                    ShowCopied(_copyButton, false);
                }
            });

            var hasLanguage = !string.IsNullOrEmpty(_language);
            if (hasLanguage || _showCopyButton)
            {
                var header = new VisualElement();
                header.AddToClassList(RLDSConstants.CodeBlockView.Header);

                if (hasLanguage)
                {
                    var languageLabel = new UnityEngine.UIElements.Label(_language);
                    languageLabel.AddToClassList(RLDSConstants.CodeBlockView.Language);
                    languageLabel.AddToClassList(RLDSConstants.Typography.Meta);
                    header.Add(languageLabel);
                }

                if (_showCopyButton)
                {
                    header.Add(BuildCopyButton());
                }

                _root.Add(header);
            }

            // A read-only multiline TextField (not a Label) so the text stays
            // user-selectable/copyable; wrapped in a ScrollView because TextField
            // exposes no scroll API of its own.
            var scroll = new UnityEngine.UIElements.ScrollView(UnityEngine.UIElements.ScrollViewMode.Vertical)
            {
                horizontalScrollerVisibility = UnityEngine.UIElements.ScrollerVisibility.Hidden,
                verticalScrollerVisibility = UnityEngine.UIElements.ScrollerVisibility.Auto,
            };
            scroll.AddToClassList(RLDSConstants.CodeBlockView.Scroll);

            var codeField = new UnityEngine.UIElements.TextField
            {
                multiline = true,
                isReadOnly = true,
                value = _code,
            };
            codeField.AddToClassList(RLDSConstants.CodeBlockView.Code);
            codeField.AddToClassList(RLDSConstants.Typography.BodyCode);
            scroll.Add(codeField);
            _root.Add(scroll);

            return _root;
        }

        private VisualElement BuildCopyButton()
        {
            UnityEngine.UIElements.Button copyButton = null;
            var action = new ActionLinkDescription
            {
                Content = new GUIContent(CopyLabel),
                Action = () =>
                {
                    EditorGUIUtility.systemCopyBuffer = _code;
                    _onCopied?.Invoke();
                    if (copyButton != null)
                    {
                        ShowCopied(copyButton, true);
                        _copyResetItem?.Pause();
                        _copyResetItem = copyButton.schedule
                            .Execute(() =>
                            {
                                if (copyButton.panel == null)
                                {
                                    return;
                                }
                                ShowCopied(copyButton, false);
                            })
                            .StartingIn(CopiedResetMs);
                    }
                },
            };

            copyButton = (UnityEngine.UIElements.Button)new Button(
                action, RLDSConstants.ButtonVariant.Secondary, RLDSConstants.ButtonSize.XSmall)
            {
                LeftIcon = _copyIcon,
            }.Build();
            copyButton.AddToClassList(RLDSConstants.CodeBlockView.Copy);
            _copyButton = copyButton;
            return copyButton;
        }

        private void ShowCopied(UnityEngine.UIElements.Button button, bool copied)
        {
            SetButtonLabel(button, copied ? CopiedLabel : CopyLabel);
            button.EnableInClassList(RLDSConstants.CodeBlockView.Copied, copied);

            var iconEl = button.Q(className: RLDSConstants.Button.IconLeft);
            if (iconEl == null)
            {
                return;
            }
            var icon = copied ? UserInterface.Styles.Contents.CheckMaskIcon : _copyIcon;
            if (icon != null)
            {
                icon.RegisterToImageLoaded(tex => iconEl.style.backgroundImage = tex as Texture2D);
            }
            else
            {
                iconEl.style.backgroundImage = null;
            }
        }

        // The RLDS Button renders its text on the Button itself when it has no
        // icon, and as a child Label when it does — handle both.
        private static void SetButtonLabel(UnityEngine.UIElements.Button button, string text)
        {
            var childLabel = button.Q<UnityEngine.UIElements.Label>();
            if (childLabel != null)
            {
                childLabel.text = text;
            }
            else
            {
                button.text = text;
            }
        }
    }
}
