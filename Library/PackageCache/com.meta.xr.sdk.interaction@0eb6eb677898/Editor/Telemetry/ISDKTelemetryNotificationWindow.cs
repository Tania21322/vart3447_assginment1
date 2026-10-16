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

#if !HAS_META_XR_SDK_CORE

using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Oculus.Interaction.Editor.Telemetry
{
    internal class ISDKTelemetryNotificationWindow : EditorWindow
    {
        private const float WindowWidth = 400f;
        private const float Margin = 16f;
        private const float IconSize = 40f;
        private const float Padding = 12f;
        private const float AutoDismissSeconds = 20f;

        private static readonly Color BackgroundColor = new Color(0.18f, 0.18f, 0.18f, 1f);
        private static readonly Color BorderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
        private static readonly Color MetaBlue = new Color(0.24f, 0.56f, 1f, 1f);

        private string _messageText;
        private Action _onShownCallback;
        private double _showTime;
        private bool _callbackInvoked;
        private GUIStyle _messageStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _closeButtonStyle;

        public static void Show(string messageText, Action onShownCallback)
        {
            var window = CreateInstance<ISDKTelemetryNotificationWindow>();
            window._messageText = messageText;
            window._onShownCallback = onShownCallback;
            window._showTime = EditorApplication.timeSinceStartup;
            window.ShowToast();
        }

        private void ShowToast()
        {
            var mainWindowRect = GetMainWindowRect();
            float estimatedHeight = 140f;

            var windowRect = new Rect(
                mainWindowRect.xMax - WindowWidth - Margin,
                mainWindowRect.yMax - estimatedHeight - Margin,
                WindowWidth,
                estimatedHeight);

            position = windowRect;
            ShowPopup();

            InvokeOnShown();
        }

        private void InvokeOnShown()
        {
            if (_callbackInvoked)
            {
                return;
            }

            _callbackInvoked = true;
            _onShownCallback?.Invoke();
        }

        private void InitStyles()
        {
            if (_messageStyle != null)
            {
                return;
            }

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 13,
                wordWrap = true,
                normal = { textColor = Color.white }
            };

            _messageStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 12,
                wordWrap = true,
                richText = true,
                normal = { textColor = new Color(0.78f, 0.78f, 0.78f, 1f) }
            };

            _closeButtonStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.6f, 0.6f, 0.6f, 1f) },
                hover = { textColor = Color.white }
            };
        }

        private void OnGUI()
        {
            InitStyles();

            // Background
            var bgRect = new Rect(0, 0, position.width, position.height);
            EditorGUI.DrawRect(bgRect, BackgroundColor);

            // Border
            DrawBorder(bgRect, BorderColor);

            // Left accent bar
            var accentRect = new Rect(0, 0, 3, position.height);
            EditorGUI.DrawRect(accentRect, MetaBlue);

            // Close button
            var closeRect = new Rect(position.width - 28, 4, 24, 24);
            if (GUI.Button(closeRect, "\u00d7", _closeButtonStyle))
            {
                Close();
                return;
            }

            // Content area
            var contentRect = new Rect(Padding + 4, Padding, position.width - Padding * 2 - 28, position.height - Padding * 2);

            GUILayout.BeginArea(contentRect);
            {
                GUILayout.Label("Interaction SDK — Data Collection Notice", _titleStyle);
                GUILayout.Space(8);

                string richText = ConvertMarkdownToRichText(_messageText);
                GUILayout.Label(richText, _messageStyle);
            }
            GUILayout.EndArea();

            // Recalculate height based on content
            float contentHeight = _titleStyle.CalcHeight(
                new GUIContent("Interaction SDK — Data Collection Notice"), contentRect.width);
            contentHeight += 8; // spacing
            string converted = ConvertMarkdownToRichText(_messageText);
            contentHeight += _messageStyle.CalcHeight(new GUIContent(converted), contentRect.width);
            contentHeight += Padding * 2 + 8; // padding + buffer

            float desiredHeight = Mathf.Max(100f, contentHeight);
            if (Mathf.Abs(position.height - desiredHeight) > 2f)
            {
                var mainWindowRect = GetMainWindowRect();
                var newPos = position;
                newPos.height = desiredHeight;
                newPos.y = mainWindowRect.yMax - desiredHeight - Margin;
                position = newPos;
            }
        }

        private void Update()
        {
            if (EditorApplication.timeSinceStartup - _showTime > AutoDismissSeconds)
            {
                Close();
            }
        }

        private static void DrawBorder(Rect rect, Color color)
        {
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1, rect.width, 1), color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1, rect.y, 1, rect.height), color);
        }

        private static Rect GetMainWindowRect()
        {
            try
            {
                var containerWindowType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.ContainerWindow");
                if (containerWindowType != null)
                {
                    var windowsField = containerWindowType.GetProperty("windows",
                        BindingFlags.Static | BindingFlags.Public);
                    if (windowsField != null)
                    {
                        var windows = windowsField.GetValue(null) as Array;
                        if (windows != null)
                        {
                            foreach (var window in windows)
                            {
                                var showModeField = containerWindowType.GetField("m_ShowMode",
                                    BindingFlags.Instance | BindingFlags.NonPublic);
                                if (showModeField != null)
                                {
                                    int showMode = (int)showModeField.GetValue(window);
                                    if (showMode == 4) // MainWindow
                                    {
                                        var positionProp = containerWindowType.GetProperty("position",
                                            BindingFlags.Instance | BindingFlags.Public);
                                        if (positionProp != null)
                                        {
                                            return (Rect)positionProp.GetValue(window);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (System.Exception)
            {
                // Fall through to default
            }

            // Fallback: use the current screen resolution
            return new Rect(0, 0, Screen.currentResolution.width, Screen.currentResolution.height);
        }

        internal static string ConvertMarkdownToRichText(string markdown)
        {
            if (string.IsNullOrEmpty(markdown))
            {
                return string.Empty;
            }

            string result = markdown;

            // Bold: **text** → <b>text</b>
            result = System.Text.RegularExpressions.Regex.Replace(
                result, @"\*\*(.+?)\*\*", "<b>$1</b>");

            // Italic: *text* → <i>text</i>
            result = System.Text.RegularExpressions.Regex.Replace(
                result, @"\*(.+?)\*", "<i>$1</i>");

            // Links: [text](url) → <color=#3D8FFF>text</color> (Unity labels can't open links)
            result = System.Text.RegularExpressions.Regex.Replace(
                result, @"\[(.+?)\]\(.+?\)", "<color=#3D8FFF>$1</color>");

            return result;
        }
    }
}

#endif
