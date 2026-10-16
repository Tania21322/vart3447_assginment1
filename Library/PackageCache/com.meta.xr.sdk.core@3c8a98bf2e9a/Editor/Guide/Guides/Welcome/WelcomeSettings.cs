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
using System.Linq;
using Meta.XR.Editor.Id;
using Meta.XR.Editor.ToolingSupport;
using Meta.XR.Editor.UserInterface;
using Meta.XR.Guides.Editor.SdkUpgrader;
using UnityEditor;
using UnityEngine;

namespace Meta.XR.Guides.Editor.Welcome
{
    internal static class WelcomeSettings
    {
        public const string PackageName = "com.meta.xr.sdk.core";
        public const int WindowWidth = 1024;
        public const int WindowHeight = 768;
        public const string WindowTitle = "Welcome to Meta XR SDK";
        public const string ReleaseNotesUrl = "https://developers.meta.com/horizon/downloads/package/meta-xr-core-sdk";
        public const string MetaVrCliDownloadUrl = "https://github.com/meta-quest/agentic-tools#prerequisites";
        public const string MetaVrCliDocsUrl = "https://github.com/meta-quest/agentic-tools#metavr-cli-quick-reference";

        // Fixed UI chrome — labels that are NOT remote-controlled. (The remote-managed copy —
        // subtitle, featured, resources, xr-tools order — lives in WelcomeContentManager.Content.)
        internal static class Labels
        {
            public const string CoverTitle = Meta.XR.Editor.StatusMenu.StatusMenuSettings.Labels.CoverTitle;
            public const string CoverSubtitle = Meta.XR.Editor.StatusMenu.StatusMenuSettings.Labels.CoverSubtitle;
            public const string OpenSdkMenuButton = "Open SDK menu";
            public const string ReleaseNotesButton = "View release notes";
            public const string ShowOnLaunchToggle = "Show this window on launch";
            public const string CloseButton = "Close";
            public const string ResourcesHeader = "Resources";
            public const string XrToolsHeader = "XR tools";
            public const string AlertBannerMessage = AlertBannerMessages.AlertBannerMessage;
            public const string AlertBannerMessagePinned = AlertBannerMessages.AlertBannerMessagePinned;
        }

        // The Building Blocks resource card is a fixed, in-editor entry (not remote-controlled): it
        // opens the Building Blocks menu instead of a URL, and renders first in the Resources section
        // ahead of the remote-controlled cards.
        internal static readonly ResourceDef BuildingBlocks = new(
            "Building Blocks",
            "Pre-built XR components you can drag and drop into your scene.",
            "Browse building blocks",
            () => EditorApplication.ExecuteMenuItem("Window/Meta/Tools/Building Blocks"));

        // A resource card: either an in-editor action (Building Blocks) or an external URL (the
        // remote-controlled cards). Reused for both so the Resources section renders them uniformly.
        internal readonly struct ResourceDef
        {
            public readonly string Label;
            public readonly string Description;
            public readonly string LinkText;
            public readonly Action LinkAction;

            // External-URL cards show the external-link icon; in-editor actions don't.
            public readonly bool OpensUrl;

            // In-editor action (no external-link icon).
            public ResourceDef(string label, string description, string linkText, Action linkAction)
            {
                Label = label;
                Description = description;
                LinkText = linkText;
                LinkAction = linkAction;
                OpensUrl = false;
            }

            // Opens an external URL (external-link icon).
            public ResourceDef(string label, string description, string linkText, string url)
            {
                Label = label;
                Description = description;
                LinkText = linkText;
                LinkAction = () => Application.OpenURL(url);
                OpensUrl = true;
            }
        }

        internal static class XrTools
        {
            internal readonly struct MetaVrCliCtaState
            {
                public readonly string Text;
                public readonly Action Action;
                public readonly Action InfoAction;

                public MetaVrCliCtaState(string text, Action action, Action infoAction)
                {
                    Text = text;
                    Action = action;
                    InfoAction = infoAction;
                }
            }

            public static readonly XrToolDef[] All =
            {
                new("AI Tools",
                    "Connect AI tools to Meta XR SDK to build XR content with prompts and get project optimization recommendations.",
                    registryNameHint: "AI Tools",
                    isActive: () => FindToolInfoText("AI Tools") == "Connected",
                    activeBadgeText: "Connected", inactiveBadgeText: "Not connected",
                    activeCtaText: "View settings", inactiveCtaText: "Setup",
                    activeAction: () => OpenTool("AI Tools"),
                    inactiveAction: () => OpenTool("AI Tools")),
                new("Meta VR CLI",
                    "Use Quest developer tools from your terminal or AI assistant to find documentation and assets, manage devices, and analyze performance.",
                    registryNameHint: "Meta VR CLI",
                    isActive: () => SdkUpgraderData.GetMetaVrCliState(null) != SdkUpgraderData.MetaVrCliState.NotInstalled,
                    activeBadgeText: "Installed", inactiveBadgeText: "Not installed",
                    activeCtaText: "View docs", inactiveCtaText: "Download",
                    activeAction: () => Application.OpenURL(MetaVrCliDocsUrl),
                    inactiveAction: () => Application.OpenURL(MetaVrCliDownloadUrl),
                    infoAction: () => Application.OpenURL(MetaVrCliDocsUrl)),
                new("Device Readiness Check",
                    "Check hand tracking, field of view, and other device readiness signals for XR development.",
                    registryNameHint: "Hands readiness",
                    isActive: () => true,
                    activeBadgeText: "Available", inactiveBadgeText: "Available",
                    activeCtaText: "Open", inactiveCtaText: "Open",
                    activeAction: () => OpenToolByHint("Hands readiness"),
                    inactiveAction: () => OpenToolByHint("Hands readiness")),
                new("Runtime optimizer",
                    "Identify performance bottlenecks in XR apps and get actionable optimization recommendations. Enable to analyze whether apps are CPU or GPU bound.",
                    registryNameHint: "Runtime Optimizer",
                    isActive: () => FindToolEnabled("Runtime Optimizer"),
                    activeBadgeText: "Enabled", inactiveBadgeText: "Disabled",
                    activeCtaText: "Launch", inactiveCtaText: "Enable",
                    activeAction: () => OpenToolByHint("Runtime Optimizer"),
                    inactiveAction: () => OpenToolByHint("Runtime Optimizer"),
                    windowsOnly: true),
                new("Immersive debugger",
                    "Debug XR apps inside the headset. Enable to activate in the Meta XR SDK menu.",
                    registryNameHint: "Immersive Debugger",
                    isActive: () => FindToolEnabled("Immersive Debugger"),
                    activeBadgeText: "Enabled", inactiveBadgeText: "Disabled",
                    activeCtaText: "Launch", inactiveCtaText: "Enable",
                    activeAction: () => OpenToolByHint("Immersive Debugger"),
                    inactiveAction: () => OpenToolByHint("Immersive Debugger")),
                new("Meta XR Simulator",
                    "Test XR apps on your computer without a headset.",
                    registryNameHint: "Meta XR Simulator",
                    isActive: ExternalToolDetection.IsXRSimInstalled,
                    activeBadgeText: "Installed", inactiveBadgeText: "Not installed",
                    activeCtaText: "Open", inactiveCtaText: "Download",
                    activeAction: () => Application.OpenURL("xrsim://"),
                    inactiveAction: () => Application.OpenURL(ExternalToolDetection.PlatformDownloadUrl(
                        "https://developers.meta.com/horizon/downloads/package/meta-xr-simulator-mac-arm/",
                        "https://developers.meta.com/horizon/downloads/package/meta-xr-simulator-windows/"))),
                new("Meta Quest Developer Hub",
                    "Manage devices, install builds, and view logs.",
                    registryNameHint: "Meta Quest Developer Hub",
                    isActive: ExternalToolDetection.IsODHInstalled,
                    activeBadgeText: "Installed", inactiveBadgeText: "Not installed",
                    activeCtaText: "Open", inactiveCtaText: "Download",
                    activeAction: () =>
                    {
                        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        var v3Exe = System.IO.Path.Combine(programFiles, "Meta Quest Developer Hub", "Meta Quest Developer Hub.exe");
                        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        var v2Exe = System.IO.Path.Combine(localAppData, "Programs", "oculus-developer-hub", "Meta Quest Developer Hub.exe");
                        var exePath = System.IO.File.Exists(v3Exe) ? v3Exe : v2Exe;
                        ExternalToolDetection.TryOpenApp(
                            exePath,
                            "Meta Quest Developer Hub",
                            "https://developers.meta.com/horizon/documentation/unity/ts-odh-getting-started");
                    },
                    inactiveAction: () => Application.OpenURL(ExternalToolDetection.PlatformDownloadUrl(
                        "https://developers.meta.com/horizon/downloads/package/oculus-developer-hub-mac/",
                        "https://developers.meta.com/horizon/downloads/package/oculus-developer-hub-win/"))),
                new("Meta Quest Link",
                    "Connect Quest headset to PC for VR streaming.",
                    registryNameHint: "Meta Quest Link",
                    isActive: ExternalToolDetection.IsOculusLinkInstalled,
                    activeBadgeText: "Installed", inactiveBadgeText: "Not installed",
                    activeCtaText: "Open", inactiveCtaText: "Download",
                    activeAction: () => Application.OpenURL("https://developers.meta.com/horizon/documentation/unity/unity-link/#set-up-meta-horizon-link"),
                    inactiveAction: () => Application.OpenURL("https://developers.meta.com/horizon/documentation/unity/unity-link/#set-up-meta-horizon-link"),
                    windowsOnly: true),
                new("RenderDoc",
                    "Debug and optimize graphics with frame-level analysis.",
                    registryNameHint: "RenderDoc",
                    isActive: ExternalToolDetection.IsRenderDocInstalled,
                    activeBadgeText: "Installed", inactiveBadgeText: "Not installed",
                    activeCtaText: "Open", inactiveCtaText: "Download",
                    activeAction: () =>
                    {
                        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                        var metaForkExe = System.IO.Path.Combine(programFiles, "RenderDocForMetaQuest", "qrenderdoc.exe");
                        var standardExe = System.IO.Path.Combine(programFiles, "RenderDoc", "qrenderdoc.exe");
                        var exePath = System.IO.File.Exists(metaForkExe) ? metaForkExe : standardExe;
                        ExternalToolDetection.TryOpenApp(
                            exePath,
                            "RenderDoc",
                            "https://renderdoc.org");
                    },
                    inactiveAction: () => Application.OpenURL(ExternalToolDetection.PlatformDownloadUrl(
                        "https://developers.meta.com/horizon/downloads/package/renderdoc-meta-fork-for-mac-installer/",
                        "https://developers.meta.com/horizon/downloads/package/renderdoc-oculus/"))),
                new("Meta Haptics Studio",
                    "Design and test haptic feedback for controllers.",
                    registryNameHint: "Meta Haptics Studio",
                    isActive: ExternalToolDetection.IsHapticsStudioInstalled,
                    activeBadgeText: "Installed", inactiveBadgeText: "Not installed",
                    activeCtaText: "Open", inactiveCtaText: "Download",
                    activeAction: () =>
                    {
                        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                        var standaloneExe = System.IO.Path.Combine(localAppData, "Programs", "meta-haptics-studio", "Meta Haptics Studio.exe");
                        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                        var odhExe = System.IO.Path.Combine(appData, "odh", "packages", "tools", "meta-haptics-studio-win", "Meta Haptics Studio.exe");
                        var exePath = System.IO.File.Exists(standaloneExe) ? standaloneExe : odhExe;
                        ExternalToolDetection.TryOpenApp(
                            exePath,
                            "Meta Haptics Studio",
                            "https://developer.oculus.com/resources/haptics-overview/");
                    },
                    inactiveAction: () => Application.OpenURL(ExternalToolDetection.PlatformDownloadUrl(
                        "https://developers.meta.com/horizon/downloads/package/meta-haptics-studio-macos/",
                        "https://developers.meta.com/horizon/downloads/package/meta-haptics-studio-win/"))),
            };

            internal static XrToolDef ApplyRemoteContent(
                XrToolDef tool,
                WelcomeContentManager.XrToolContent content)
            {
                var activeAction = tool.ActiveAction;
                if (!string.IsNullOrEmpty(content.activeUrl))
                {
                    activeAction = () => Application.OpenURL(content.activeUrl);
                }

                var inactiveAction = tool.InactiveAction;
                if (!string.IsNullOrEmpty(content.inactiveUrl))
                {
                    inactiveAction = () => Application.OpenURL(content.inactiveUrl);
                }

                return new XrToolDef(
                    Coalesce(content.label, tool.Label),
                    Coalesce(content.description, tool.Description),
                    tool.RegistryNameHint,
                    tool.IsActive,
                    Coalesce(content.activeBadgeText, tool.ActiveBadgeText),
                    Coalesce(content.inactiveBadgeText, tool.InactiveBadgeText),
                    Coalesce(content.activeCtaText, tool.ActiveCtaText),
                    Coalesce(content.inactiveCtaText, tool.InactiveCtaText),
                    activeAction,
                    inactiveAction,
                    infoAction: tool.InfoAction,
                    enableDirectInstall: content.enableDirectInstall,
                    windowsOnly: tool.WindowsOnly);
            }

            internal static MetaVrCliCtaState ResolveMetaVrCliCta(
                XrToolDef configuredDef,
                bool supportsDirectInstall) =>
                configuredDef.EnableDirectInstall && supportsDirectInstall
                    ? new MetaVrCliCtaState("Install", () => MetaVrCliInstaller.Install(), configuredDef.InfoAction)
                    : new MetaVrCliCtaState(configuredDef.InactiveCtaText, configuredDef.InactiveAction, null);

            /// <summary>The <c>id</c>/<c>RegistryNameHint</c> of the Meta VR CLI XR tool entry.</summary>
            internal const string MetaVrCliToolId = "Meta VR CLI";

            /// <summary>
            /// The currently resolved <c>enableDirectInstall</c> flag for the Meta VR CLI, i.e. the
            /// remote-content half of the <see cref="ResolveMetaVrCliCta"/> decision (the other half
            /// being <see cref="MetaVrCliInstaller.SupportsDirectInstall"/>). Exposed so the AI Tools
            /// setup panel, which offers the same CTA, reads the same flag from the same place
            /// instead of duplicating the lookup and drifting out of sync.
            /// </summary>
            internal static bool IsMetaVrCliDirectInstallEnabled()
            {
                var def = Array.Find(All, t => t.RegistryNameHint == MetaVrCliToolId);
                if (def.RegistryNameHint == null) return false;

                var content = Array.Find(
                    WelcomeContentManager.Content.xrTools
                        ?? Array.Empty<WelcomeContentManager.XrToolContent>(),
                    t => t.id == MetaVrCliToolId);

                return ApplyRemoteContent(def, content).EnableDirectInstall;
            }

            private static string Coalesce(string value, string fallback) =>
                string.IsNullOrEmpty(value) ? fallback : value;

            private static string FindToolInfoText(string nameHint)
            {
                var descriptor = ToolRegistry.Registry
                    .FirstOrDefault(t => t.Name != null && t.Name.Contains(nameHint));
                return descriptor?.InfoTextDelegate?.Invoke().Item1;
            }

            // Reads the tool's actual enablement state from its ToolDescriptor
            // (the SDK menu's EnablementDescriptor) rather than the InfoText string,
            // which no longer carries an "Enabled" label after the status-menu redesign.
            private static bool FindToolEnabled(string nameHint)
            {
                var descriptor = ToolRegistry.Registry
                    .FirstOrDefault(t => t.Name != null && t.Name.Contains(nameHint));
                return descriptor?.EnablementDescriptor?.Invoke().Item1 ?? false;
            }

            private static void OpenTool(string toolId)
            {
                var descriptor = ToolRegistry.Registry
                    .FirstOrDefault(t => t.Id == toolId);
                descriptor?.OnClickDelegate?.Invoke(Origins.GuidedSetup);
            }

            private static void OpenToolByHint(string nameHint)
            {
                var descriptor = ToolRegistry.Registry
                    .FirstOrDefault(t => t.Name != null && t.Name.Contains(nameHint));
                descriptor?.OnClickDelegate?.Invoke(Origins.GuidedSetup);
            }
        }

        internal readonly struct XrToolDef
        {
            public readonly string Label;
            public readonly string Description;
            public readonly string RegistryNameHint;
            public readonly Func<bool> IsActive;
            public readonly string ActiveBadgeText;
            public readonly string InactiveBadgeText;
            public readonly string ActiveCtaText;
            public readonly string InactiveCtaText;
            public readonly Action ActiveAction;
            public readonly Action InactiveAction;
            public readonly Action InfoAction;
            public readonly bool EnableDirectInstall;

            // Tools that only exist on Windows (e.g. Runtime Optimizer, Meta Quest Link) —
            // hidden on non-Windows editors. See BuildXrToolsSection.
            public readonly bool WindowsOnly;

            public XrToolDef(
                string label, string description,
                string registryNameHint,
                Func<bool> isActive,
                string activeBadgeText, string inactiveBadgeText,
                string activeCtaText, string inactiveCtaText,
                Action activeAction, Action inactiveAction,
                Action infoAction = null,
                bool enableDirectInstall = false,
                bool windowsOnly = false)
            {
                Label = label;
                Description = description;
                RegistryNameHint = registryNameHint;
                IsActive = isActive;
                ActiveBadgeText = activeBadgeText;
                InactiveBadgeText = inactiveBadgeText;
                ActiveCtaText = activeCtaText;
                InactiveCtaText = inactiveCtaText;
                ActiveAction = activeAction;
                InactiveAction = inactiveAction;
                InfoAction = infoAction;
                EnableDirectInstall = enableDirectInstall;
                WindowsOnly = windowsOnly;
            }
        }
    }
}
