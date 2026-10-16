# Immersive Debugger Integration for Meta XR Operator

This integration bridges Meta XR Operator with Unity's Immersive Debugger, exposing runtime scene inspection and manipulation tools through the Meta XR Operator MCP server.

## What It Provides

37 Meta XR Operator tools organized into groups:

| Group | Count | Description |
|-------|-------|-------------|
| **core** | 9 | Always loaded: search, inspect, hierarchy, health, diagnose, deep inspect |
| **scene_mutation** | 7 | Lazy-loaded: set values, toggle active, invoke methods (with args), settable overview, add/remove component |
| **diagnostics** | 6 | Lazy-loaded: logs, error/warning summaries, search, clear, write debug log |
| **debugger_ui** | 10 | Lazy-loaded: overlay visibility/opacity, inspector/console/LLM panel show-hide, hierarchy/category mode, head-follow, gizmos, inspector |
| **visualization** | 3 | Lazy-loaded: bounding boxes, drawing management |
| **discovery** | 2 | Lazy-loaded: find by component, find debug members |

Tools use a progressive disclosure pattern: 9 core tools load at startup, additional groups load on demand via `unity_load_tools`.

The `debugger_ui` and `scene_mutation` groups reach parity with the Immersive Debugger's own runtime MCPBridge tools, except for two deliberately-omitted capabilities: the `SelectObjectInHierarchy` smart hierarchy-navigation and the full `Reflection` object-registry subsystem (arbitrary type instantiation / static-method invocation).

## Architecture

```text
Meta XR Operator MCP Server (port 8720)
├── OpenXR tools (native C++)
└── Unity scene tools (this integration)
    ├── ImmersiveDebuggerBinder.cs  — MonoBehaviour that registers all tools on Start()
    ├── SceneQueryHelper.cs         — Scene traversal, reflection, type conversion
    └── LogCaptureService.cs        — Thread-safe ring buffer for Unity console logs
```

All tools register via `MetaXROperatorExternalTool.RegisterAgenticTool()` (Meta XR Core SDK) and appear alongside OpenXR tools on the same MCP endpoint.

## Files

- **`ImmersiveDebuggerBinder.cs`** — MonoBehaviour that registers the 9 core tools (plus `unity_load_tools`) with Meta XR Operator on `Start()`; the remaining groups (28 tools) load on demand via `unity_load_tools`. `LLMDialogPanelRegistrar` (DevAgent) attaches it to `ImmersiveDebuggerManager` when the in-headset AI Assistant is enabled (`RuntimeSettings.Enabled`).
- **`SceneQueryHelper.cs`** — Static helper for scene traversal, component inspection, type conversion, fuzzy matching, and instance ID caching.
- **`LogCaptureService.cs`** — Static service capturing Unity console logs into a thread-safe ring buffer. Auto-initializes via `[RuntimeInitializeOnLoadMethod]` before scene load.

## Integration Instructions

These scripts ship as part of the Meta XR Core SDK (`com.meta.xr.sdk.core`) under
`Assets/Oculus/VR/Scripts/ImmersiveDebugger/XROperator/` and compile into the
`Meta.XR.ImmersiveDebugger` assembly. They are guarded by
`#if USING_XR_SDK_OPENXR`, so they build when the OpenXR backend is selected and the
Meta XR Operator external-tool API (`XR_METAX1_agentic_external_tool`) is available.

To use them in a project:

1. Select the OpenXR backend and ensure the Meta XR Operator native API layer is present (see the `demo/immersive_debugger_demo` example under `arvr/projects/agentic_xr/`).
2. Enable Immersive Debugger in your project (via `Resources/ImmersiveDebuggerSettings.asset`).
3. Enable the in-headset AI Assistant (DevAgent), then press Play — `LLMDialogPanelRegistrar` adds the `ImmersiveDebuggerBinder` to `ImmersiveDebuggerManager`, and tools register with the Meta XR Operator MCP server on port 8720.

## Dependencies

- **Meta XR Operator C# API** — `MetaXROperatorExternalTool`, `AgenticToolParameter`, `XrAgenticExternalToolParameterTypeMETAX1` (Meta XR Core SDK, `Meta.XR` namespace / `Oculus.VR` assembly; gated by `USING_XR_SDK_OPENXR`)
- **Immersive Debugger runtime** — `Meta.XR.ImmersiveDebugger` (`RuntimeAPIs`, `DebugInterface`, `Console`, `DebugMember`, `DebugGizmoType`)
- **Unity 6000.0+** (Unity 6)

## Design Principles

- **LLM-First Output**: Consistent prefixes (`Error:`, `Success:`, `Data:`, `Tip:`, `Warning:`) for machine parsing
- **Bounded Output**: Size limits (16KB text, 4s timeout, 50 max results) to prevent context window waste
- **Fail-Forward Errors**: Every error includes recovery suggestions (alternatives, typo suggestions, current values)
- **Platform Consistency**: Follows `MetaXROperator_Binder.cs` patterns for error prefixes and callback flags

## See Also

- [Meta XR Operator Getting Started](https://www.internalfb.com/wiki/Agentic_XR/Getting_Started/)
- Demo project: `demo/immersive_debugger_demo/`
- Full specification: see `demo/immersive_debugger/specification.md` (D93750345)
