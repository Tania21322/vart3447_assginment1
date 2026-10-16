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
using Meta.XR.Editor.UserInterface.RLDS;
using UnityEngine.UIElements;

namespace Meta.XR.Editor.UserInterface
{
    /// <summary>
    /// A card summarizing a single SDK version's release notes: a version label with
    /// date and status badges, a bulleted list of highlights, and a "full release notes"
    /// link. The highlighted variant tints the card to mark the recommended target version.
    /// </summary>
    internal class ReleaseNoteCard : IUserInterfaceItem
    {
        public bool Hide { get; set; }

        private readonly string _versionLabel;
        private readonly string _dateLabel;
        private readonly IReadOnlyList<string> _bullets;
        private readonly IReadOnlyList<(string text, BadgePillType type)> _badges;
        private readonly string _linkText;
        private readonly TextureContent _linkIcon;
        private readonly bool _highlighted;
        private readonly string _id;
        private readonly Action _onLinkClicked;

        public ReleaseNoteCard(
            string versionLabel,
            string dateLabel,
            IReadOnlyList<string> bullets,
            IReadOnlyList<(string text, BadgePillType type)> badges = null,
            string linkText = "Full release notes",
            TextureContent linkIcon = null,
            bool highlighted = false,
            string id = null,
            Action onLinkClicked = null)
        {
            _versionLabel = versionLabel;
            _dateLabel = dateLabel;
            _bullets = bullets;
            _badges = badges;
            _linkText = linkText;
            _linkIcon = linkIcon;
            _highlighted = highlighted;
            _id = id;
            _onLinkClicked = onLinkClicked;
        }

        public void Draw()
        {
            // UIToolkit-only component; IMGUI rendering is not supported.
        }

        public VisualElement Build()
        {
            var root = new VisualElement();
            root.AddToClassList(RLDSConstants.ReleaseNoteCard.Root);
            if (_highlighted)
            {
                root.AddToClassList(RLDSConstants.ReleaseNoteCard.Highlight);
            }

            var header = new VisualElement();
            header.AddToClassList(RLDSConstants.ReleaseNoteCard.Header);

            var titleGroup = new VisualElement();
            titleGroup.AddToClassList(RLDSConstants.ReleaseNoteCard.TitleGroup);

            var version = new UnityEngine.UIElements.Label(_versionLabel);
            version.AddToClassList(RLDSConstants.Typography.Body1Label);
            version.AddToClassList(RLDSConstants.ReleaseNoteCard.Version);
            titleGroup.Add(version);

            if (!string.IsNullOrEmpty(_dateLabel))
            {
                var date = new UnityEngine.UIElements.Label(_dateLabel);
                date.AddToClassList(RLDSConstants.Typography.Meta);
                date.AddToClassList(RLDSConstants.ReleaseNoteCard.Date);
                titleGroup.Add(date);
            }

            if (_badges != null)
            {
                VisualElement lastBadge = null;
                foreach (var (text, type) in _badges)
                {
                    if (string.IsNullOrEmpty(text)) continue;
                    var badge = new BadgePill(text, type, BadgePillSize.Small).Build();
                    badge.style.marginRight = RLDSConstants.Spacing.Size3XS;
                    titleGroup.Add(badge);
                    lastBadge = badge;
                }

                if (lastBadge != null)
                {
                    lastBadge.style.marginRight = RLDSConstants.Spacing.None;
                }
            }

            header.Add(titleGroup);

            // Render the link only when both a label and a click action are supplied at
            // construction, so a caller relying on the default linkText without an action
            // doesn't render a dead hyperlink.
            if (_onLinkClicked != null && !string.IsNullOrEmpty(_linkText))
            {
                var link = new VisualElement();
                link.AddToClassList(RLDSConstants.ReleaseNoteCard.Link);
                link.RegisterCallback<ClickEvent>(evt =>
                {
                    evt.StopPropagation();
                    RLDSTelemetry.SendInteraction(link, GetType().Name, _id ?? _versionLabel, _linkText, actionData: "link");
                    _onLinkClicked();
                });

                var linkLabel = new UnityEngine.UIElements.Label(_linkText);
                linkLabel.AddToClassList(RLDSConstants.ReleaseNoteCard.LinkLabel);
                link.Add(linkLabel);

                if (_linkIcon != null)
                {
                    var linkIconEl = new VisualElement();
                    linkIconEl.AddToClassList(RLDSConstants.ReleaseNoteCard.LinkIcon);
                    // Assign unconditionally (the shared RLDS convention): RegisterToImageLoaded fires
                    // synchronously if the texture is already loaded, else once it loads. UIElements
                    // tolerates style writes on a not-yet-attached element, so the icon still applies even
                    // if the load wins the race to attach this reusable card into the tree.
                    _linkIcon.RegisterToImageLoaded(tex =>
                        linkIconEl.style.backgroundImage = tex as UnityEngine.Texture2D);
                    link.Add(linkIconEl);
                }

                header.Add(link);
            }

            root.Add(header);

            if (_bullets != null)
            {
                var items = new List<string>();
                foreach (var text in _bullets)
                {
                    if (!string.IsNullOrEmpty(text))
                    {
                        items.Add(text);
                    }
                }

                if (items.Count > 0)
                {
                    var bulletList = new VisualElement();
                    bulletList.AddToClassList(RLDSConstants.ReleaseNoteCard.BulletList);

                    // A lone highlight (e.g. a one-sentence AI summary) reads better as a plain sentence,
                    // so the leading dot is only drawn when there are multiple items.
                    var showDots = items.Count > 1;
                    foreach (var text in items)
                    {
                        var bullet = new VisualElement();
                        bullet.AddToClassList(RLDSConstants.ReleaseNoteCard.Bullet);

                        if (showDots)
                        {
                            var dot = new UnityEngine.UIElements.Label("•");
                            dot.AddToClassList(RLDSConstants.ReleaseNoteCard.BulletDot);
                            bullet.Add(dot);
                        }

                        var bulletText = new UnityEngine.UIElements.Label(text);
                        bulletText.AddToClassList(RLDSConstants.ReleaseNoteCard.BulletText);
                        bullet.Add(bulletText);

                        bulletList.Add(bullet);
                    }

                    root.Add(bulletList);
                }
            }

            return root;
        }
    }
}
