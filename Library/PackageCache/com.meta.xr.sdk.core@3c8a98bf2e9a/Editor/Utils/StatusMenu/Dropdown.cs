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

#if UNITY_6000_3_OR_NEWER
#define USE_MAINTOOLBAR
#endif

using System;
#if USE_MAINTOOLBAR
using System.Collections.Generic;
using System.Linq;
#endif
using Meta.XR.Editor.Id;
using Meta.XR.Editor.Settings;
using Meta.XR.Editor.UserInterface.RLDS;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;
using SdkMenuButton = Meta.XR.Editor.UserInterface.SdkMenuButton;
using SdkMenuButtonVariant = Meta.XR.Editor.UserInterface.SdkMenuButtonVariant;
using UIStyles = Meta.XR.Editor.UserInterface.Styles;
using Utils = Meta.XR.Editor.UserInterface.Utils;

namespace Meta.XR.Editor.StatusMenu
{
    [InitializeOnLoad]
    internal static class Dropdown
    {
        private const string ElementClass = "unity-editor-toolbar-element";
        private const string Title = "Meta XR SDK";

        private static EditorToolbarButton _editorToolbarButton;
        private static SdkMenuButton _sdkMenuButton;
        private static SdkMenuButtonVariant _currentVariant = SdkMenuButtonVariant.Positive;
        private static Vector2 _rawPosition = Vector2.zero;

        static Dropdown()
        {
            if (!Utils.ShouldRenderEditorUI()) return;

            EditorApplication.update += Initialize;
        }

#if USE_MAINTOOLBAR
        private static MainToolbarButton _mainToolbarButton;
        private static VisualElement _mainToolbarVisualElement;

        private const string WindowTitle = "SDK Menu";
        private const string MainToolbarPath = "Meta XR SDK/" + WindowTitle;

        /// <summary>
        /// Returns true if the MainToolbar button is currently visible / pinned in the Unity editor toolbar.
        /// Used by Welcome window to decide whether to show the Open SDK Menu button.
        /// </summary>
        public static bool IsToolbarButtonVisible(bool forceRefresh = false)
        {
            if (forceRefresh)
            {
                _mainToolbarVisualElement = null;
            }
            // Always do fresh visual tree scan for visibility check to ensure immediate update on pin/unpin.
            // GetMainToolbarVisualElement caches, so we clear cache above if forceRefresh, else it may return stale.
            // For extra safety, we directly call FindMainToolbarVisualElement here to bypass cache entirely for visibility queries.
            var ve = forceRefresh ? FindMainToolbarVisualElement() : GetMainToolbarVisualElement();
            // Extra validation beyond panel null check – when user unpins via right-click,
            // Unity may keep VisualElement alive briefly but detached or zero-sized.
            // Treat as not visible if no panel, not visible flag false, or worldBound empty.
            if (ve == null) return false;
            if (ve.panel == null) return false;
            if (!ve.visible) return false;
            var wb = ve.worldBound;
            if (wb.width <= 0 || wb.height <= 0) return false;
            // Main toolbar is top area y < 80; overflow menu would be elsewhere or hidden.
            if (wb.y > 80) return false;
            return true;
        }

        /// <summary>
        /// Clears cached toolbar visual element to force re-scan on next query.
        /// Call this when toolbar layout may have changed (pin/unpin).
        /// </summary>
        public static void RefreshToolbarButtonCache()
        {
            _mainToolbarVisualElement = null;
        }

        /// <summary>
        /// Creates the toolbar button for Unity 6+ using the MainToolbarElement API.
        /// Called automatically by Unity's toolbar system.
        /// </summary>
        /// <returns>The created MainToolbarElement, or null if the status icon is disabled.</returns>
        [MainToolbarElement(MainToolbarPath, defaultDockPosition = MainToolbarDockPosition.Left)]
        public static MainToolbarElement CreateStatusMenuButton()
        {
            var icon = Styles.Contents.MetaIcon.GUIContent.image as Texture2D;
            var content = new MainToolbarContent(icon) { text = Title };

            _mainToolbarButton = new MainToolbarButton(content, ShowDropdown);

            return _mainToolbarButton;
        }

        private static VisualElement GetMainToolbarVisualElement()
        {
            if (_mainToolbarVisualElement != null && _mainToolbarVisualElement.panel != null)
            {
                return _mainToolbarVisualElement;
            }

            _mainToolbarVisualElement = FindMainToolbarVisualElement();
            if (_mainToolbarVisualElement != null)
            {
                _mainToolbarVisualElement.RegisterCallback<GeometryChangedEvent>(_ => RefreshRawPosition());
            }
            return _mainToolbarVisualElement;
        }

        private static VisualElement FindMainToolbarVisualElement()
        {
            // We have _mainToolbarButton cached from CreateStatusMenuButton (line 75) as the descriptor.
            // The corresponding VisualElement is created by Unity's MainToolbar system; we need to locate it in the UI tree.
            // Use broad VisualElement query not just ToolbarButton type, because MainToolbarButton visual type may vary across Unity versions.
            // Match by tooltip containing WindowTitle or text containing Title, to robustly detect pinned state without reflection.
            var panels = FindAllEditorPanelRoots();
            VisualElement bestCandidate = null;
            float bestScore = float.MaxValue;

            foreach (var panel in panels)
            {
                // Query broadly for any VisualElement with tooltip matching our window title – MainToolbarButton visual may not be ToolbarButton type in all Unity versions.
                var candidates = panel.Query<VisualElement>()
                    .Where(b => b != null && b.visible && b.enabledInHierarchy && !string.IsNullOrEmpty(b.tooltip) && b.tooltip.Contains(WindowTitle))
                    .ToList();
                // Fallback also check by text content in case tooltip is stripped in some skins.
                // Check self as TextElement, or descendant Label text, since MainToolbarButton may structure text in child Label not on root VisualElement.
                // VisualElement base does not have text property – cast to TextElement safely.
                if (candidates.Count == 0)
                {
                    candidates = panel.Query<VisualElement>()
                        .Where(b => {
                            if (b == null || !b.visible || !b.enabledInHierarchy) return false;
                            var te = b as UnityEngine.UIElements.TextElement;
                            if (te != null && !string.IsNullOrEmpty(te.text) && te.text.Contains(Title)) return true;
                            var label = b.Q<UnityEngine.UIElements.Label>();
                            if (label != null && !string.IsNullOrEmpty(label.text) && label.text.Contains(Title)) return true;
                            var label2 = b.Q<UnityEngine.UIElements.Label>(null, "unity-toolbar-button__label");
                            return label2 != null;
                        })
                        .ToList();
                    // Further fallback: check any descendant label with our title text to catch MainToolbarButton structure variations.
                    if (candidates.Count == 0)
                    {
                        var allLabels = panel.Query<UnityEngine.UIElements.Label>().Where(l => l != null && l.text != null && l.text.Contains(Title)).ToList();
                        foreach (var label in allLabels)
                        {
                            // Walk up to find plausible toolbar button ancestor with suitable worldBound
                            var ancestor = label.parent;
                            while (ancestor != null && ancestor.worldBound.height > 80) ancestor = ancestor.parent;
                            if (ancestor != null) candidates.Add(ancestor);
                        }
                    }
                }
                foreach (var b in candidates)
                {
                    if (b.worldBound.width <= 0 || b.worldBound.height <= 0) continue;
                    // Main toolbar is at top of screen, height typically 20-40px, y < 80
                    if (b.worldBound.y > 80) continue;
                    var score = b.worldBound.y * 1000 + b.worldBound.x; // prefer top-left most
                    if (score < bestScore) { bestScore = score; bestCandidate = b; }
                }
                // Also try specific ToolbarButton type as fallback for older pattern matching.
                if (bestCandidate == null)
                {
                    var exactButtons = panel.Query<UnityEditor.UIElements.ToolbarButton>()
                        .Where(b => !string.IsNullOrEmpty(b.tooltip) && b.tooltip.Contains(WindowTitle))
                        .ToList();
                    foreach (var b in exactButtons)
                    {
                        if (b.worldBound.width <= 0 || b.worldBound.height <= 0) continue;
                        if (b.worldBound.y > 80) continue;
                        var score = b.worldBound.y * 1000 + b.worldBound.x;
                        if (score < bestScore) { bestScore = score; bestCandidate = b; }
                    }
                }
            }
            return bestCandidate;
        }

        private static List<VisualElement> FindAllEditorPanelRoots()
        {
            var panelRoots = new List<VisualElement>();

            var allWindows = Resources.FindObjectsOfTypeAll<EditorWindow>();

            foreach (var window in allWindows)
            {
                if (window == null || window.rootVisualElement == null) continue;

                var element = window.rootVisualElement;
                while (element.parent != null)
                {
                    element = element.parent;
                }

                if (element.GetType().Name == "EditorPanelRootElement")
                {
                    if (!panelRoots.Contains(element))
                    {
                        panelRoots.Add(element);
                    }
                }
            }

            return panelRoots;
        }
#endif

        private static void CustomizeButton(EditorToolbarButton button)
        {
            button.AddToClassList(ElementClass);
            button.RegisterCallback<GeometryChangedEvent>(evt => RefreshRawPosition());

            button.text = "";
            button.icon = null;

            // Remove the Button's Clickable manipulator so pointer events propagate
            // to SdkMenuButton's root element instead of being captured by the parent.
            if (button.clickable != null)
            {
                button.RemoveManipulator(button.clickable);
            }

            // Strip EditorToolbarButton's visual chrome so SdkMenuButton
            // renders directly without a wrapper appearance.
            button.style.backgroundColor = Color.clear;
            button.style.borderTopWidth = RLDSConstants.BorderWidth.None;
            button.style.borderBottomWidth = RLDSConstants.BorderWidth.None;
            button.style.borderLeftWidth = RLDSConstants.BorderWidth.None;
            button.style.borderRightWidth = RLDSConstants.BorderWidth.None;
            button.style.paddingTop = RLDSConstants.Spacing.None;
            button.style.paddingBottom = RLDSConstants.Spacing.None;
            button.style.paddingLeft = RLDSConstants.Spacing.None;
            button.style.paddingRight = RLDSConstants.Spacing.None;

            var styleSheet = RLDSUtils.LoadStyleSheet(!EditorGUIUtility.isProSkin);
            if (styleSheet != null)
            {
                button.styleSheets.Add(styleSheet);
            }

            _sdkMenuButton = new SdkMenuButton(ComputeCurrentVariant());
            _sdkMenuButton.Clicked += () =>
            {
                RefreshRawPosition();
                ShowDropdown();
            };
            var sdkElement = _sdkMenuButton.Build();
            button.Add(sdkElement);
        }

        private static void Initialize()
        {
#if USE_MAINTOOLBAR
            EditorApplication.update -= Initialize;
            // MainToolbarElement path creates _mainToolbarButton via CreateStatusMenuButton attribute.
            // We don't need to find EditorToolbarButton; just subscribe to Update for variant handling.
            // If _mainToolbarButton is null at this point, Unity hasn't created it yet; Update will handle null checks.
            EditorApplication.update += Update;
            return;
#else
            EditorApplication.update -= Initialize;
            // Pre-6000.3 uses floating window via MenuItem, no toolbar button.
            // Keeping Dropdown class alive for ShowDropdown() API used by WelcomeWindow and Onboarding,
            // which will open StatusMenu as a floating EditorWindow via StatusMenu.ShowDropdown -> StatusMenuDrawer.ShowWindow.
            return;
#endif
        }

        private static void Update()
        {
            UpdateVariant();
        }

        private static void UpdateVariant()
        {
            if (_sdkMenuButton == null) return;

            var variant = ComputeCurrentVariant();
            if (variant == _currentVariant) return;
            _currentVariant = variant;
            _sdkMenuButton.Variant = variant;
        }

        private static SdkMenuButtonVariant ComputeCurrentVariant()
        {
            var item = StatusMenu.GetHighestItem();
            if (item?.PillIcon == null)
            {
                return SdkMenuButtonVariant.Positive;
            }

            var (_, color, showNotification) = item.PillIcon();
            return ComputeVariantForColor(color, showNotification);
        }

        // The toolbar button has only three states (Figma node 2567-63486): Positive (all good),
        // Warning, and Error. Anything that is not a warning or error status — including the
        // Optional/Info color — maps to Positive; the button has no Info variant.
        internal static SdkMenuButtonVariant ComputeVariantForColor(Color? color, bool showNotification)
        {
            if (!showNotification || color == null)
            {
                return SdkMenuButtonVariant.Positive;
            }

            var c = color.Value;
            var errorColor = UIStyles.Colors.ErrorColor;
            if (Mathf.Abs(c.r - errorColor.r) < 0.1f && Mathf.Abs(c.g - errorColor.g) < 0.1f)
                return SdkMenuButtonVariant.Error;

            var warningColor = UIStyles.Colors.WarningColor;
            if (Mathf.Abs(c.r - warningColor.r) < 0.1f && Mathf.Abs(c.g - warningColor.g) < 0.1f)
                return SdkMenuButtonVariant.Warning;

            return SdkMenuButtonVariant.Positive;
        }

        private static void RefreshRawPosition()
        {
#if USE_MAINTOOLBAR
            var ve = GetMainToolbarVisualElement();
            if (ve != null)
            {
                var bound = ve.worldBound;
                _rawPosition = GUIUtility.GUIToScreenPoint(bound.position);
                return;
            }
#endif
            _rawPosition = GUIUtility.GUIToScreenPoint(Vector2.zero);
        }

        private static Rect ComputeCurrentRect()
        {
#if USE_MAINTOOLBAR
            var ve = GetMainToolbarVisualElement();
            if (ve != null)
            {
                var bound = ve.worldBound;
                if (!float.IsNaN(bound.x) && !float.IsNaN(bound.y) &&
                    !float.IsNaN(bound.width) && !float.IsNaN(bound.height) &&
                    bound.width > 0 && bound.height > 0)
                {
                    var screenPos = GUIUtility.GUIToScreenPoint(bound.position);
                    return new Rect(screenPos, bound.size);
                }
            }

            // Fallback: use mouse position at time of click if VisualElement lookup failed.
            // Event.current may be null in MainToolbarButton action callback, but try anyway to avoid Rect.zero upper-left.
            if (Event.current != null)
            {
                var mouseScreen = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
                return new Rect(mouseScreen, new Vector2(1, 1));
            }

            // Last resort: return zero which will open at upper-left, but log warning in debug.
            UnityEngine.Debug.LogWarning("[Meta XR SDK] StatusMenu Dropdown: could not locate MainToolbar VisualElement for '" + Title + "'. Falling back to Rect.zero.");
            return Rect.zero;
#else
            if (_editorToolbarButton == null) return Rect.zero;

            var layoutSize = _editorToolbarButton.layout.size;
            if (float.IsNaN(layoutSize.x) || float.IsNaN(layoutSize.y))
            {
                return Rect.zero;
            }

            var position = _rawPosition;

            var parent = _editorToolbarButton as VisualElement;
            while (parent != null)
            {
                position += parent.layout.position;
                parent = parent.parent;
            }

            return new Rect(position, layoutSize);
#endif
        }

        internal static void ShowDropdown()
        {
            StatusMenu.ShowDropdown(ComputeCurrentRect());
        }

    }
}
