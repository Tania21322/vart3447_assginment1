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

using UnityEditor;
using UnityEngine;
using Oculus.Interaction.Telemetry;

namespace Oculus.Interaction.Editor.Telemetry
{
    internal class ISDKTelemetryConsentWindow : EditorWindow
    {
        private const float Padding = 12f;

        private static readonly Color ButtonAcceptColor = new Color(0.24f, 0.56f, 1f, 1f);

        private Vector2 _size = new Vector2(600f, 320f);
        private string _titleText;
        private string _bodyText;
        private bool _choiceMade;
        private GUIStyle _rootStyle;
        private GUIStyle _titleStyle;
        private GUIStyle _bodyStyle;

        public static void Show()
        {
            string title;
            string body;
            try
            {
                title = ISDKEngineTelemetryNative.GetConsentTitle();
                body = ISDKEngineTelemetryNative.GetConsentMarkdownText();
            }
            catch (System.DllNotFoundException)
            {
                return;
            }

            if (string.IsNullOrEmpty(title))
            {
                title = "Interaction SDK — Data Collection";
            }

            if (string.IsNullOrEmpty(body))
            {
                body = "Meta collects usage data to improve the Interaction SDK. " +
                    "You can change this anytime under " +
                    "**Meta > Interaction SDK > Telemetry Settings**.";
            }

            var window = CreateInstance<ISDKTelemetryConsentWindow>();
            window._titleText = title;
            window._bodyText = body;
            window.titleContent = new GUIContent(title);
            window.ShowUtility();
        }

        private void InitStyles()
        {
            if (_rootStyle != null)
            {
                return;
            }

            _rootStyle = new GUIStyle
            {
                padding = new RectOffset((int)Padding, (int)Padding, (int)Padding, (int)Padding)
            };

            _titleStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 14,
                wordWrap = true
            };

            _bodyStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                fontSize = 12,
                richText = true
            };
        }

        private void OnGUI()
        {
            InitStyles();

            var rect = EditorGUILayout.BeginVertical(_rootStyle);

            GUILayout.Label(_titleText, _titleStyle);
            GUILayout.Space(8);
            GUILayout.Label(
                ISDKTelemetryNotificationWindow.ConvertMarkdownToRichText(_bodyText),
                _bodyStyle);

            GUILayout.Space(12);

            EditorGUILayout.BeginHorizontal();

            var buttonLayout = GUILayout.Height(40);

            if (GUILayout.Button("Only share essential data", buttonLayout))
            {
                ApplyConsent(false);
            }

            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = ButtonAcceptColor;
            if (GUILayout.Button("Share additional data", buttonLayout))
            {
                ApplyConsent(true);
            }
            GUI.backgroundColor = prevColor;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            UpdateHeight(rect);
        }

        private void ApplyConsent(bool enabled)
        {
            _choiceMade = true;
            ISDKTelemetryConsent.SetConsent(enabled);
            Close();
            GUIUtility.ExitGUI();
        }

        private void OnDestroy()
        {
            // Closing via the title-bar (×) without picking a button is treated
            // as opt-out, so consent is always recorded and we don't re-prompt.
            if (!_choiceMade)
            {
                ISDKTelemetryConsent.SetConsent(false);
            }
        }

        private void UpdateHeight(Rect rect)
        {
            if (Event.current.type != EventType.Repaint)
            {
                return;
            }

            const float bottomMargin = 3f;
            _size.y = rect.height + bottomMargin;
            minSize = _size;
            maxSize = _size;
        }
    }
}

#endif
