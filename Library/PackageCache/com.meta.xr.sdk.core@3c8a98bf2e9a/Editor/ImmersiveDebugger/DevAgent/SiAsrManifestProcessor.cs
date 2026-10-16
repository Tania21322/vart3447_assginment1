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
using System.IO;
using System.Xml;
using UnityEditor.Android;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.DevAgent.Editor
{
    /// <summary>
    /// Injects the manifest entries that on-device System Intelligence ASR requires into the built
    /// APK: the RECORD_AUDIO permission, and a <c>&lt;queries&gt;</c> block declaring the
    /// <c>android.speech.RecognitionService</c> intent and the <c>com.oculus.systemintelligence</c>
    /// package. On Android 11+ the package-visibility rules hide the recognizer service without the
    /// queries block, and <c>SpeechRecognizer.isOnDeviceRecognitionAvailable</c> then reports false.
    /// </summary>
    internal class SiAsrManifestProcessor : IPostGenerateGradleAndroidProject
    {
        private const string AndroidNamespaceAttribute = "xmlns:android";
        private const string AndroidNamespaceUri = "http://schemas.android.com/apk/res/android";
        private const string RecordAudioPermission = "android.permission.RECORD_AUDIO";
        private const string RecognitionServiceAction = "android.speech.RecognitionService";
        private const string SiPackage = "com.oculus.systemintelligence";
        private const string OverlayKeyboardFeature = "oculus.software.overlay_keyboard";

        // Run after OVRGradleGeneration (order 3) so the manifest is fully emitted before patching.
        public int callbackOrder => 10;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            // Only inject when the AI Assistant (whose voice input uses SI ASR) is enabled, so apps
            // that don't use the feature don't gain a microphone-permission surface.
            var settings = RuntimeSettings.Instance;
            if (settings == null || !settings.Enabled)
            {
                return;
            }

            // Patch only this module's own source manifest, not every AndroidManifest.xml under the
            // export. Library/intermediate manifests lack an android namespace and must not be touched.
            var manifestFile = Path.Combine(path, "src", "main", "AndroidManifest.xml");
            if (!File.Exists(manifestFile))
            {
                Debug.LogWarning($"[SiAsr] App manifest not found at {manifestFile}; skipping SI ASR manifest injection.");
                return;
            }

            try
            {
                PatchManifest(manifestFile);
            }
            catch (Exception)
            {
                // Retry with a Windows extended-length path in case the export path is long.
                try
                {
                    PatchManifest("\\\\?\\" + Path.GetFullPath(manifestFile));
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SiAsr] Unable to update AndroidManifest {manifestFile}: {e}");
                }
            }
        }

        private static void PatchManifest(string manifestPath)
        {
            var doc = new XmlDocument();
            doc.Load(manifestPath);

            var manifest = (XmlElement)doc.SelectSingleNode("/manifest");
            if (manifest == null)
            {
                return;
            }

            var androidNs = manifest.GetAttribute(AndroidNamespaceAttribute);
            if (string.IsNullOrEmpty(androidNs))
            {
                androidNs = AndroidNamespaceUri;
            }
            bool changed = EnsureRecordAudio(doc, manifest, androidNs);
            changed |= EnsureSiQueries(doc, manifest, androidNs);
            changed |= EnsureOverlayKeyboard(doc, manifest, androidNs);
            if (!changed)
            {
                return;
            }

            var settings = new XmlWriterSettings { NewLineChars = "\n", Indent = true };
            using var writer = XmlWriter.Create(manifestPath, settings);
            doc.Save(writer);
        }

        private static bool EnsureRecordAudio(XmlDocument doc, XmlElement manifest, string androidNs)
        {
            var existing = doc.SelectNodes("/manifest/uses-permission");
            if (existing != null)
            {
                foreach (XmlElement e in existing)
                {
                    if (e.GetAttribute("name", androidNs) == RecordAudioPermission)
                    {
                        return false;
                    }
                }
            }

            var permission = doc.CreateElement("uses-permission");
            permission.SetAttribute("name", androidNs, RecordAudioPermission);
            manifest.AppendChild(permission);
            return true;
        }

        // The system overlay keyboard the DevAgent's text-input field raises on device.
        private static bool EnsureOverlayKeyboard(XmlDocument doc, XmlElement manifest, string androidNs)
        {
            var existing = doc.SelectNodes("/manifest/uses-feature");
            if (existing != null)
            {
                foreach (XmlElement e in existing)
                {
                    if (e.GetAttribute("name", androidNs) == OverlayKeyboardFeature)
                    {
                        return false;
                    }
                }
            }

            var feature = doc.CreateElement("uses-feature");
            feature.SetAttribute("name", androidNs, OverlayKeyboardFeature);
            feature.SetAttribute("required", androidNs, "false");
            manifest.AppendChild(feature);
            return true;
        }

        private static bool EnsureSiQueries(XmlDocument doc, XmlElement manifest, string androidNs)
        {
            var queries = (XmlElement)doc.SelectSingleNode("/manifest/queries");
            bool changed = false;
            if (queries == null)
            {
                queries = doc.CreateElement("queries");
                manifest.AppendChild(queries);
                changed = true;
            }

            if (!HasNamedChild(queries, "package", androidNs, SiPackage))
            {
                var package = doc.CreateElement("package");
                package.SetAttribute("name", androidNs, SiPackage);
                queries.AppendChild(package);
                changed = true;
            }

            if (!HasRecognitionServiceIntent(queries, androidNs))
            {
                var intent = doc.CreateElement("intent");
                var action = doc.CreateElement("action");
                action.SetAttribute("name", androidNs, RecognitionServiceAction);
                intent.AppendChild(action);
                queries.AppendChild(intent);
                changed = true;
            }

            return changed;
        }

        private static bool HasNamedChild(XmlElement parent, string childName, string androidNs, string value)
        {
            foreach (XmlNode child in parent.ChildNodes)
            {
                if (child is XmlElement e && e.Name == childName && e.GetAttribute("name", androidNs) == value)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasRecognitionServiceIntent(XmlElement queries, string androidNs)
        {
            foreach (XmlNode child in queries.ChildNodes)
            {
                if (!(child is XmlElement intent) || intent.Name != "intent")
                {
                    continue;
                }

                foreach (XmlNode grandChild in intent.ChildNodes)
                {
                    if (grandChild is XmlElement action &&
                        action.Name == "action" &&
                        action.GetAttribute("name", androidNs) == RecognitionServiceAction)
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
