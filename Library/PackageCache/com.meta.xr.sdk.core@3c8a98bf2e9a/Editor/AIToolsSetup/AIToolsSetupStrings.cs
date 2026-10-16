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

namespace Meta.XR.Editor
{
    internal static class AIToolsSetupStrings
    {
        internal static class Header
        {
            public const string WindowTitle = "AI Tools";
            public const string DisplayTitle = "AI Tools";
            public const string Description =
                "Meta XR SDK works with AI coding assistants like Claude Code, Codex, and Gemini CLI. " +
                "Connect yours to build XR apps from prompts with Meta XR Operator, and to bring AI into " +
                "XR tools like Runtime Optimizer, Immersive Debugger, and Hands-Only Optimizer.";
            public const string Resources = "Resources:";
            public const string QuickStartGuide = "Quick start guide";
            public const string ExperimentalBadge = "Experimental";
            public const string MenuDescription = "Set up AI tools for Meta XR SDK";
        }

        internal static class Tools
        {
            public const string SectionTitle = "What you can do with AI tools for Meta XR SDK";
            public const string LearnMore = "Learn more";

            public const string MetaXROperatorLabel = "Meta XR Operator";
            public const string MetaXROperatorDescription = "Build XR apps using prompts with your AI coding assistant";

            public const string RuntimeOptimizerLabel = "Runtime optimizer";
            public const string RuntimeOptimizerDescription = "Analyze and optimize your XR app's performance";

            public const string ImmersiveDebuggerLabel = "Immersive debugger";
            public const string ImmersiveDebuggerDescription = "Test and debug in VR without leaving your editor";

            public const string HandsOnlyOptimizerLabel = "Hands optimizer";
            public const string HandsOnlyOptimizerDescription = "Optimize your project for hand-tracking-only interactions";
        }

        internal static class Steps
        {
            // Bare titles — the wizard prefixes "Step N: " dynamically so the firewall step
            // (Windows-only) doesn't leave a numbering gap on macOS.
            public const string SelectServiceProvider = "Select your service provider";
            public const string InstallAndSetup = "Install and set up AI tools";
            public const string ConfigureFirewall = "Allow local network connections (Windows Firewall)";
            public const string InstallSkills = "Install project-level skills";
            public const string ReviewProjectSetup = "Review Project Setup Tool";
        }

        internal static class Columns
        {
            public const string AgenticBadge = "MCP Proxy";
            public const string AgenticHeading = "XR Operator";
            public const string AgenticDescription =
                "Let your AI coding assistant write C# scripts, build scenes, and test " +
                "in Meta XR Simulator from prompts. XR Operator is installed and served " +
                "through the Meta VR CLI (metavr).";

            public const string BridgeBadge = "AI Agent Bridge";
            public const string BridgeHeading = "AI-powered XR tools";
            public const string BridgeDescription =
                "Power Runtime optimizer, Immersive debugger, and Hands optimizer with tailored " +
                "AI recommendations.";
            public const string BridgeToolsNote =
                "Immersive Debugger's in-headset assistant gets its debugging tools from Meta XR Operator. " +
                "Set up the XR Operator card too so the assistant can act on your scene.";

            public const string RunCommandHelper =
                "Run in your terminal to configure the connection between your AI coding assistant " +
                "and Meta XR SDK.";
        }

        internal static class SubSteps
        {
            public const string InstallMcpProxy = "Install Meta XR Operator MCP Proxy";
            public const string InstallXROperator = "Install Meta XR Operator (via Meta VR CLI)";
            public const string InstallBridge = "Install AI Agent Bridge";
            public const string ValidateBridgeConnection = "Validate AI Agent Bridge and Meta XR Operator Connection";
            public const string ConfigureFirewall = "Allow local network connections (Windows Firewall)";
        }

        internal static class Firewall
        {
            public const string Description =
                "On Windows, the firewall blocks inbound connections from devices on your network. " +
                "Add a one-time rule so your Quest headset can reach the AI Tools servers running in this " +
                "Editor. You'll see a single Windows admin (UAC) prompt; after that it persists across sessions.";
            public const string ConfigureButton = "Configure Windows Firewall";
            public const string Configuring = "Configuring Windows Firewall";
            public const string Configured = "Windows Firewall allows local network connections";
            public const string NotConfigured = "Windows Firewall is not configured for local network connections";
        }

        internal static class Device
        {
            // AgentBridge card: adb reverse (device -> host). The in-headset assistant connects out to the
            // Remote Agent Server, so the device's port is reversed to this computer.
            public const string ReverseHeading = "Headset connection";
            public const string ReverseDescription =
                "Only needed for the Immersive Debugger in-headset AI Assistant on Quest APK builds. " +
                "Connect the headset to this computer with a USB cable.";
            public const string ReverseButton = "Configure ADB reverse";
            public const string Reversing = "Configuring ADB reverse";
            public const string ReverseConfiguredFormat = "ADB reverse configured for {0} device(s) on {1}";

            // XR Operator card: the on-device (Quest APK) setup, run as one action across every
            // authorized device. The `debug.` system properties it sets are cleared by a headset reboot.
            public const string QuestHeading = "Headset connection (Development builds only)";
            public const string QuestDescription =
                "Only needed for Quest APK builds. Connect the headset to this computer with a USB cable, " +
                "and enable Development Build in Build Profiles. These device settings are cleared when " +
                "the headset reboots, so run this again after a restart.";
            public const string SetUpButton = "Set up headset";
            public const string SettingUp = "Setting up headset";
            public const string SetUpCompleteFormat = "Headset set up for {0} device(s)";

            // Undo: puts every setting the setup applied back the way it was.
            public const string UndoButton = "Undo setup";
            public const string UndoingSetup = "Undoing headset setup";
            public const string UndoCompleteFormat = "Headset settings restored on {0} device(s)";

            // Checklist rows, in the order the steps run.
            public const string ForwardItemFormat = "Forward MCP server port {0}";
            public const string ExperimentalItem = "Enable experimental features";
            public const string CaptureItem = "Request screen capture permission at app start";
            public const string ProximityItem = "Disable proximity sensor (test off your face)";
            public const string ItemFailedFormat = "{0} — {1}";

            // Shared failure messages.
            public const string AdbUnavailable =
                "Android SDK (adb) not found. Set the Android SDK path in Preferences > External Tools.";
            public const string NoDeviceDetected =
                "No authorized device detected. Connect your Quest over USB and enable USB debugging.";
            public const string DeviceUnauthorized =
                "A device is connected but unauthorized. Accept the USB debugging prompt in the headset, then retry.";
            public const string PartialFailureFormat =
                "The ADB command failed on {0} device(s). Reconnect the device and retry.";
            public const string TimedOut =
                "ADB did not respond in time. Reconnect the headset (or restart the ADB server) and retry.";
        }

        internal static class ProjectSetup
        {
            public const string Description =
                "Open Project Setup Tool and fix any outstanding rules to ensure the project is ready for XR development";
            public const string OpenButton = "Open Project Setup Tool";
            public const string OutstandingAIToolWarningFormat =
                "{0} Project Setup Tool rules for AI tools features (e.g. Meta XR Operator) need fixing.";
        }

        internal static class Registration
        {
            public const string EditorToolsLabel = "Editor tools";
            public const string EditorToolsDescription = "Scene manipulation, compilation, testing, and screenshots";
            public const string RuntimeToolsLabel = "Runtime tools";
            public const string RuntimeToolsDescription = "OpenXR layer and live XR session interaction";
        }

        internal static class OpenCode
        {
            public const string DocsLinkText = "OpenCode MCP setup docs";
            public const string ManualInstructions =
                "Run the command above in your terminal to add the Meta VR CLI's MCP server to " +
                "OpenCode, or follow the OpenCode documentation to add it manually.";
        }

        internal static class Descriptions
        {
            public const string SelectServiceProvider =
                "Pick your AI coding assistant. The install commands below are tailored to your choice.";
            public const string InstallMcpProxy =
                "The MCP Proxy is a small binary that lets your AI coding assistant connect to " +
                "the Meta XR Operator runtime tools. It is copied from the SDK into your home folder so " +
                "AI clients can launch it directly.";
            public const string InstallXROperatorViaCli =
                "Meta XR Operator is installed through the Meta VR CLI (metavr). " +
                "Your AI assistant connects to metavr's MCP server, which exposes the XR Operator " +
                "runtime tools for inspecting and interacting with your running app.";
            public const string InstallBridge =
                "Enable AI-powered features for Runtime Optimizer, Immersive Debugger, and Hands Optimizer " +
                "by installing the AI Agent Bridge, which lets your AI coding assistant communicate with Meta XR SDK. " +
                "This window will update automatically once the bridge is installed.";
            public const string ConnectAssistant =
                "Run this command in your system terminal to configure the connection between your AI coding assistant and Meta XR SDK.";
            public const string VerifyConnection =
                "Your connection status will update automatically once the bridge detects " +
                "your AI coding assistant. If the status doesn't update after a few seconds, " +
                "click \"Verify connection\" to check manually.";
            public const string AdvancedSettings =
                "Customize proxy configuration, installation paths, and connection settings.";
            public const string ActivateSection =
                "Activate XR Operator to register the OpenXR API layer. " +
                "This enables your AI agent to interact with your XR application at runtime.";
            public const string SkillsSection =
                "For \"Meta XR Operator creation\", install project-level skills so your AI coding assistant " +
                "has XR workflow guidance when it runs from your project folder. You can skip this step " +
                "if you're only using \"AI-powered XR tools\".";
        }

        internal static class Buttons
        {
            public const string InstallBridge = "Install AI Agent Bridge";
            public const string InstallMcpProxy = "Install MCP Proxy";
            public const string ReinstallMcpProxy = "Reinstall MCP Proxy";
            public const string UpdateMcpProxy = "Update MCP Proxy";
            public const string DownloadMetaVrCli = "Download Meta VR CLI";
            public const string InstallMetaVrCli = "Install Meta VR CLI";
            public const string InstallXROperator = "Install XR Operator";
            public const string ReinstallXROperator = "Reinstall XR Operator";
            public const string UpdateXROperator = "Update XR Operator";
            public const string SetupServiceFormat = "Setup {0}";
            public const string RunCommand = "Run Command";
            public const string RunCommandBridgeDisabledTooltip = "Install AI Agent Bridge in Step 1 first";
            public const string RunCommandProxyDisabledTooltip = "Install Meta XR Operator in Step 1 first";
            public const string VerifyConnection = "Verify connection";
            public const string AdvancedSettingsShow = "Open advanced settings";
            public const string AdvancedSettingsHide = "Hide advanced settings";
            public const string Cancel = "Cancel";
            public const string TryAgain = "Try again";
            public const string Refresh = "Refresh";
            public const string Rerun = "Rerun";
            public const string Installed = "Installed";
            public const string OpenSetupWizard = "Open setup wizard";
            public const string ActivateMetaXROperator = "Activate XR Operator";
            public const string Deactivate = "Deactivate";
            public const string InstallSkillsFormat = "Install {0} skills in project";
        }

        internal static class ConnectionStatus
        {
            public const string BridgeInstalled = "AI Agent Bridge installed";
            public const string BridgeNotInstalled = "AI Agent Bridge is not installed";
            public const string ProxyInstalled = "MCP Proxy installed";
            public const string ProxyNotInstalled = "MCP Proxy is not installed";
            // Kept close in length to the ProxyInstalled/ProxyNotInstalled strings these replace —
            // the status row is one line in a narrow column and wraps badly past ~30 characters.
            public const string XROperatorInstalled = "XR Operator installed";
            public const string XROperatorNotInstalled = "XR Operator is not installed";
            public const string MetaVrCliNotInstalled = "Meta VR CLI is not installed";
            public const string AssistantConnectedFormat = "AI coding assistant connected: {0}";
            public const string AssistantConnectedWithSkillsFormat = "AI coding assistant connected: {0} ({1} skills installed)";
            public const string RuntimeToolsRegisteredFormat = "Runtime tools registered with {0}";
            public const string AssistantNotConnected = "Unable to connect AI coding assistant";
            public const string ConnectionVerified = "Connection verified";
            public const string ConnectionNotVerifiedFormat = "Unable to connect: {0}";
            public const string WaitingForConnection = "Waiting for connection";
        }

        internal static class Processing
        {
            public const string Installing = "Installing";
            public const string InstallingProxy = "Installing MCP Proxy";
            public const string InstallingXROperator = "Installing XR Operator";
            // Covers the whole one-click State-A action (CLI install, then XR Operator). Named for
            // the CLI because that phase dominates: it downloads and runs a full installer, while
            // `tools install xroperator` finishes in a few seconds.
            public const string InstallingMetaVrCli = "Installing Meta VR CLI";
            public const string Connecting = "Connecting";
            public const string Verifying = "Verifying";
        }

        internal static class Explainer
        {
            public const string ToggleShow = "New here? What is MCP and what does this do?";
            public const string ToggleHide = "Hide: What is MCP and what does this do?";
            public const string WhatIsMcpTitle = "What is MCP?";
            public const string WhatIsMcpBody =
                "MCP (Model Context Protocol) is the standard way AI coding assistants talk to external tools. " +
                "When you \"add an MCP server\" to your AI client, you're telling it: " +
                "\"here's a tool you can use — call it whenever you need to interact with this system.\"";
            public const string WhatDoesMetaXROperatorAddTitle = "What does Meta XR Operator add?";
            public const string WhatDoesMetaXROperatorAddBody =
                "Meta XR Operator is an OpenXR API layer that runs during your app's runtime. " +
                "While your XR application is running (on Quest or in the Simulator), " +
                "Meta XR Operator exposes an MCP server that lets your AI agent interact with the live session — " +
                "inspecting the scene, reading properties, and calling tools in real time.";
            public const string ScopeTitle = "Per-project or system-wide?";
            public const string ScopeBody =
                "We recommend setting up MCP per project — run the registration command from your Unity project's " +
                "directory. This way the AI agent only has access to this project's tools when you're working on it.\n\n" +
                "System-wide setup is also possible (add a --scope global flag or edit your AI client's global config), " +
                "but per-project is cleaner and more secure.";
        }

        internal static class Transport
        {
            public const string Label = "Transport:";
            public const string ProxyLabel = "MCP Proxy (Recommended)";
            public const string SSELabel = "Direct SSE";
            public const string CompareShow = "Which one should I pick?";
            public const string CompareHide = "Hide comparison";
        }

        internal static class TransportComparison
        {
            public const string PanelTitle = "MCP Proxy vs Direct SSE";
            public const string Recommendation =
                "Recommendation: Use MCP Proxy unless you specifically need direct SSE for latency reasons.";

            public static readonly string[] Headers = { "Feature", "MCP Proxy", "Direct SSE" };
            public static readonly float[] ColumnWidths = { 0.35f, 0.35f, 0.30f };

            private const CellStyle D = CellStyle.Default;
            private const CellStyle P = CellStyle.Positive;
            private const CellStyle N = CellStyle.Negative;

            public static readonly string[][] Rows =
            {
                new[] { "Works offline", "Yes — cached tools", "No" },
                new[] { "Auto-reconnect", "Yes", "No" },
                new[] { "Startup order", "Doesn't matter", "App must start first" },
                new[] { "Extra process", "One lightweight binary", "None" },
                new[] { "Latency", "One extra hop", "Direct" },
                new[] { "Dynamic tool discovery", "Yes", "No" },
            };

            public static readonly CellStyle[][] RowStyles =
            {
                new[] { D, P, N },
                new[] { D, P, N },
                new[] { D, P, N },
                new[] { D, N, P },
                new[] { D, N, P },
                new[] { D, P, N },
            };
        }

        internal static class Activate
        {
            public const string SectionTitle = "Activate XR Operator";
            public const string BinariesNotFound = "XR Operator binaries not found. Install them from the Meta XR SDK package.";
            public const string Activated = "XR Operator is activated";
            public const string NotActivated = "XR Operator is not activated";
        }

        internal static class AdvancedSettingsSection
        {
            public const string Title = "Advanced settings";
        }

        internal static class Skills
        {
            public const string SectionTitle = "Meta XR Operator skills";
            public const string InstalledStatusFormat = "{0}/{1} installed";
            public const string DestinationFormat = "Destination: {0}";
            public const string InstalledFormat = "Installed {0} Meta XR Operator skills at {1}";
        }

        internal static class Errors
        {
            public const string ProxyInstallFailed = "Could not install the MCP proxy binary.";
            public const string ProxyBinaryNotFound =
                "Could not find the MCP proxy binary.\n\n" +
                "Please ensure the Meta XR Operator package is installed correctly.";
            public const string ProxyOverwriteFormat =
                "Cannot overwrite {0} — the proxy may be running.\n\n" +
                "Please stop the proxy process and try again.";
            public const string ProxyInstallExceptionFormat = "Failed to install MCP proxy:\n\n{0}";
            public const string ClientNotFoundFormat = "{0} not found. Is it installed and in your PATH?";
            public const string AgentNotInstalledFormat =
                "{0} was not detected on your system. Install it (or set its executable path in " +
                "Meta XR > Preferences), then click \"Try again\".";
            public const string VerifyTimedOutFormat =
                "{0} started but timed out. It may be checking MCP server health — try again.";
            // Transport-neutral: the McpProxy path registers the federated `metavr` server while
            // DirectSSE still registers `meta-xr-operator`, so naming either one here would be
            // wrong half the time.
            public const string NotRegisteredFormat =
                "The Meta XR Operator MCP server is not registered with {0}";
            public const string MetaVrCliMissingForInstall =
                "Meta VR CLI (metavr) was not found. Download and install it, then click again.";
            public const string MetaVrCliInstallFailed =
                "Meta VR CLI installation failed. See the Console for details, or install it " +
                "manually from the Meta VR CLI prerequisites page.";
            public const string MetaVrCliDirectInstallUnavailable =
                "Meta VR CLI cannot be installed from here on this platform. Install it manually " +
                "from the Meta VR CLI prerequisites page, then click again.";
            public const string MetaVrCliOutdatedForInstall =
                "Meta VR CLI (metavr) is out of date. Run \"metavr update\" in your terminal, " +
                "then click again.";
            public const string XROperatorInstallFailed = "XR Operator install failed: {0}";
            public const string XROperatorUpdateFailed = "XR Operator update failed: {0}";
            public const string VerifyFailedFormat = "Verification failed: {0}";
            public const string SetupFailedFormat = "Setup failed: {0}";
            public const string RegistrationFailedFormat = "Failed to register with {0}: {1}";
            public const string ProjectRootNotFound = "Could not find the project root folder.";
            public const string SkillsDirectoryNotFound =
                "Could not find the bundled Meta XR Operator skills. " +
                "Please ensure the Meta XR SDK package is installed correctly.";
            public const string NoSkillsFound = "No Meta XR Operator skills were found to install.";
            public const string SkillsInstallExceptionFormat = "Failed to install Meta XR Operator skills: {0}";
        }

        internal static class Links
        {
            public const string QuickStartUrl = "https://developers.meta.com/horizon/documentation/unity/meta-xr-operator/getting-started/";
            public const string MetaXROperatorLearnMoreUrl = "https://developers.meta.com/horizon/documentation/unity/meta-xr-operator/";
            public const string RuntimeOptimizerLearnMoreUrl = "https://developers.meta.com/horizon/documentation/unity/unity-quest-runtime-optimizer/";
            public const string ImmersiveDebuggerLearnMoreUrl = "https://developers.meta.com/horizon/documentation/unity/immersivedebugger-overview/";
            public const string HandsOnlyOptimizerLearnMoreUrl = "https://developers.meta.com/horizon/documentation/unity/hands-optimizer/";
            public const string OpenCodeMcpDocsUrl = "https://opencode.ai/docs/mcp-servers/";

            // Must match the landed Welcome card destinations (WelcomeSettings.MetaVrCliDownloadUrl /
            // MetaVrCliDocsUrl) so both entry points send developers to the same instructions.
            public const string MetaVrCliDownloadUrl = "https://github.com/meta-quest/agentic-tools#prerequisites";
            public const string MetaVrCliDocsUrl = "https://github.com/meta-quest/agentic-tools#metavr-cli-quick-reference";
        }
    }
}
