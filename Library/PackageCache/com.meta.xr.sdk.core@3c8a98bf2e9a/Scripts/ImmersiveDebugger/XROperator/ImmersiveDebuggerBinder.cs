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

#if USING_XR_SDK_OPENXR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using ImmersiveDebugger = Meta.XR.ImmersiveDebugger;

namespace Meta.XR.ImmersiveDebugger.XROperator
{
    /// <summary>
    /// Registers ImmersiveDebugger scene-manipulation and debugger-UI tools
    /// with the Meta XR Operator MCP server via the external tool API.
    ///
    /// Added programmatically at runtime by LLMDialogPanelRegistrar (via AddComponent)
    /// when the AI Assistant is enabled; it is internal and not attached manually in the
    /// Editor. On Start() it registers all tools so they appear in the single Meta XR
    /// Operator MCP server on port 8720.
    ///
    /// All tool callbacks use the ApplicationMainThreadBit flag so they execute on
    /// the Unity main thread (required for scene access).
    /// </summary>
    internal class ImmersiveDebuggerBinder : MonoBehaviour
    {
        // TODO: Update XrAgenticExternalToolCallbackInfoFlagsMETAX1 with the new flag bits

        private const XrAgenticExternalToolCallbackInfoFlagsMETAX1 MainThread =
            XrAgenticExternalToolCallbackInfoFlagsMETAX1.ApplicationMainThreadBit;

        /// <summary>Maximum gizmo descriptions retained in the tracking list.</summary>
        private const int MaxGizmoListSize = 100;

        private static bool _sceneEventsRegistered;

        /// <summary>Host GameObject of the Immersive Debugger; destroying it would take the assistant
        /// and its tools down mid-session, so unity_destroy_game_object refuses to.</summary>
        private static GameObject _debuggerRoot;

        /// <summary>Tracks which tool groups have been loaded. Core is always loaded.</summary>
        private static readonly HashSet<string> _loadedGroups = new HashSet<string> { "core" };

        /// <summary>Valid group names for lazy registration.</summary>
        private static readonly string[] ValidGroups = new[]
        {
            "scene_mutation", "diagnostics", "debugger_ui",
            "visualization", "discovery", "all"
        };

        void Start()
        {
            try
            {
                if (!MetaXRFeature.TryGet(out var feature))
                {
                    XROperatorRuntimeStatus.SetConnectionStatus(
                        XROperatorConnectionStatus.Unavailable,
                        "Meta XR OpenXR feature unavailable");
                    return;
                }

                if (!feature.AgenticExternalToolEnabled)
                {
                    XROperatorRuntimeStatus.SetConnectionStatus(
                        XROperatorConnectionStatus.Unavailable,
                        "XR Operator external tool extension unavailable");
                    return;
                }

                XROperatorRuntimeStatus.SetConnectionStatus(
                    XROperatorConnectionStatus.Registering,
                    "Registering XR Operator tools");

                // Initialize cache for fast object lookup
                SceneQueryHelper.InitializeCache();

                _debuggerRoot = gameObject;

                // Register scene event handlers for static state cleanup (once only)
                if (!_sceneEventsRegistered)
                {
                    _sceneEventsRegistered = true;
                    UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => OnSceneChanged();
                    UnityEngine.SceneManagement.SceneManager.sceneUnloaded += (scene) => OnSceneChanged();
                }

                // Core tools (always loaded — 9 tools)
                RegisterSearchGameObjects();
                RegisterInspectGameObject();
                RegisterInspectComponent();
                RegisterGetComponentValue();
                RegisterGetSceneHierarchy();
                RegisterGetHealthStatus();

                // Derivative core tools — combine primitives for common workflows
                RegisterDiagnose();
                RegisterDeepInspect();

                // Meta-tool for lazy group loading
                RegisterLoadTools();

                int coreCount = 9;
                XROperatorRuntimeStatus.SetConnectionStatus(
                    XROperatorConnectionStatus.Connected,
                    $"Registered {coreCount} core tools");
                Debug.Log($"[ImmersiveDebuggerBinder] Registered {coreCount} core Immersive Debugger tools + unity_load_tools. " +
                          $"Use unity_load_tools to load additional groups: {string.Join(", ", ValidGroups)}");
            }
            catch (Exception ex)
            {
                // Any failure (feature probe, extension check, or partial tool registration) is
                // surfaced as Error. Tools registered before a mid-registration throw are not rolled
                // back; a subsequent successful Start re-registers the full set.
                XROperatorRuntimeStatus.SetConnectionStatus(
                    XROperatorConnectionStatus.Error,
                    ex.Message);
                throw;
            }
        }

        /// <summary>Register all tools in a named group. Skips if already loaded.</summary>
        private static int LoadToolGroup(string group)
        {
            if (_loadedGroups.Contains(group))
                return 0;

            int count = 0;
            switch (group)
            {
                case "scene_mutation":
                    RegisterSetComponentValue();
                    RegisterSetGameObjectActive();
                    RegisterInvokeMethod();
                    RegisterToggleGameObjectActive();
                    RegisterGetSettableOverview();
                    RegisterAddComponent();
                    RegisterRemoveComponent();
                    RegisterDuplicateGameObject();
                    RegisterDestroyGameObject();
                    count = 9;
                    break;

                case "diagnostics":
                    RegisterGetRecentLogs();
                    RegisterGetErrorSummary();
                    RegisterGetWarningSummary();
                    RegisterSearchLogs();
                    RegisterClearDiagnosticData();
                    RegisterDebugLog();
                    count = 6;
                    break;

                case "debugger_ui":
                    RegisterDebuggerSetVisibility();
                    RegisterToggleDebuggerVisibility();
                    RegisterDebuggerStatus();
                    RegisterDebuggerSetOpacity();
                    RegisterDebuggerSetPanelVisibility();
                    RegisterDebuggerSetMode();
                    RegisterDebuggerSetFollow();
                    RegisterAddGizmo();
                    RegisterListAddedGizmos();
                    RegisterDebuggerAddInspector();
                    count = 10;
                    break;

                case "visualization":
                    RegisterDrawBoundingBox();
                    RegisterClearDrawings();
                    RegisterGetDrawingStatus();
                    count = 3;
                    break;

                case "discovery":
                    RegisterFindByComponent();
                    RegisterFindDebugMembers();
                    count = 2;
                    break;

                case "all":
                    foreach (var g in ValidGroups)
                    {
                        if (g != "all")
                            count += LoadToolGroup(g);
                    }
                    _loadedGroups.Add("all");
                    return count;

                default:
                    return -1; // Invalid group
            }

            _loadedGroups.Add(group);
            return count;
        }

        /// <summary>
        /// Clear static tracking state on scene reload. Gizmos and drawings created
        /// in a previous scene are no longer relevant.
        /// </summary>
        private static void OnSceneChanged()
        {
            _addedGizmos.Clear();
            // Drawings are parented under _drawingContainer which is DontDestroyOnLoad,
            // so they survive scene loads. Clear tracking to match reality — the gizmo
            // GameObjects will be destroyed with their targets but the container persists.
            // Active drawings with null targets are cleaned up in GetDrawingStatus.
        }

        // =================================================================
        // Tool grouping: unity_load_tools
        // =================================================================

        [Serializable]
        private struct LoadToolsParams
        {
            public string action;
            public string group;
        }

        private static void RegisterLoadTools()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_load_tools",
                "[META] Load additional Unity runtime tool groups. By default only core tools " +
                "(health, hierarchy, search, inspect) are registered. " +
                "Groups: scene_mutation (set values, toggle active, invoke methods), " +
                "diagnostics (logs, errors, warnings), debugger_ui (visibility, gizmos, inspector), " +
                "visualization (bounding boxes, drawings), discovery (find by component, debug members), " +
                "all (load everything). Use action 'list' to see group status.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "action",
                        Description = "'load' to load a tool group, 'list' to see available groups and status",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "group",
                        Description = "Group ID: scene_mutation, diagnostics, debugger_ui, visualization, discovery, all",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    }
                },
                LoadTools_Callback,
                MainThread);
        }

        private static string LoadTools_Callback(string parameters)
        {
            LoadToolsParams p;
            try { p = JsonUtility.FromJson<LoadToolsParams>(parameters); }
            catch
            {
                return "Error: Invalid parameters. Expected: {\"action\": \"list\"} or {\"action\": \"load\", \"group\": \"scene_mutation\"}\n" +
                           "Tip: Use action 'list' to see available groups.";
            }

            string action = (p.action ?? "").Trim().ToLowerInvariant();

            if (action == "list")
            {
                var sb = new StringBuilder();
                sb.AppendLine("Unity Tool Groups:");
                sb.AppendLine();

                sb.AppendLine($"  core (9 tools) [LOADED] — search, inspect object, inspect component, get value, hierarchy, health status, diagnose, deep inspect, load tools");

                var groupDescriptions = new Dictionary<string, (int count, string desc)>
                {
                    ["scene_mutation"] = (9, "set values, toggle active, invoke methods, settable overview, add/remove component, duplicate/destroy object"),
                    ["diagnostics"] = (6, "recent logs, error/warning summary, search logs, clear data, write debug log"),
                    ["debugger_ui"] = (10, "visibility, opacity, panel show/hide, hierarchy/category mode, follow, gizmos, inspector"),
                    ["visualization"] = (3, "bounding boxes, clear drawings, drawing status"),
                    ["discovery"] = (2, "find by component type, find [DebugMember] annotations"),
                };

                foreach (var kv in groupDescriptions)
                {
                    string status = _loadedGroups.Contains(kv.Key) ? "LOADED" : "available";
                    sb.AppendLine($"  {kv.Key} ({kv.Value.count} tools) [{status}] — {kv.Value.desc}");
                }

                sb.AppendLine($"  all [{(_loadedGroups.Contains("all") ? "LOADED" : "available")}] — loads all groups at once");
                sb.AppendLine();
                sb.AppendLine($"Total loaded: {_loadedGroups.Count - 1} group(s) (core is always loaded).");
                sb.AppendLine("Tip: Use unity_load_tools with action 'load' and group name to register more tools.");
                return sb.ToString();
            }

            if (action == "load")
            {
                string group = (p.group ?? "").Trim().ToLowerInvariant();
                if (string.IsNullOrEmpty(group))
                    return "Error: 'group' parameter required when action is 'load'.\n" +
                           $"Tip: Valid groups: {string.Join(", ", ValidGroups)}";

                if (_loadedGroups.Contains(group))
                    return $"Group '{group}' is already loaded. Use action 'list' to see all group statuses.";

                int count = LoadToolGroup(group);
                if (count < 0)
                    return $"Error: Unknown group '{group}'.\n" +
                           $"Tip: Valid groups: {string.Join(", ", ValidGroups)}";

                return $"Success: Loaded group '{group}' — {count} new tool(s) registered.\n" +
                       "The new tools are now available. Use unity_load_tools with action 'list' to see all loaded groups.";
            }

            return $"Error: Unknown action '{action}'. Use 'list' or 'load'.\n" +
                   "Tip: unity_load_tools(action='list') shows available groups. " +
                   "unity_load_tools(action='load', group='scene_mutation') loads a group.";
        }

        // =================================================================
        // Shared helpers
        // =================================================================

        /// <summary>
        /// Builds a standard "not found" error with tip for stale instance IDs.
        /// </summary>
        private static string GameObjectNotFoundError(long id)
        {
            return $"Error: GameObject with ID {id} not found.\n" +
                   "Tip: Instance IDs may change after scene reload. " +
                   "Use unity_search_game_objects to find the object by name again.";
        }

        /// <summary>
        /// Builds a standard "component not found" error with available list and typo suggestion.
        /// </summary>
        private static string ComponentNotFoundError(GameObject obj, string componentName)
        {
            var available = string.Join(", ", obj.GetComponents<Component>()
                .Where(c => c != null).Select(c => c.GetType().Name));
            var suggestion = SceneQueryHelper.FindClosestComponentName(obj, componentName);
            var sb = new StringBuilder();
            sb.AppendLine($"Error: Component '{componentName}' not found on '{obj.name}' (ID: {SceneQueryHelper.GetObjectId(obj)}).");
            if (suggestion != null)
                sb.AppendLine($"Did you mean '{suggestion}'?");
            sb.AppendLine($"Available: {available}");
            sb.Append($"Tip: Use unity_inspect_game_object with game_object_id {SceneQueryHelper.GetObjectId(obj)} to see all components.");
            return sb.ToString();
        }

        /// <summary>
        /// Builds a standard "member not found" error.
        /// </summary>
        private static string MemberNotFoundError(string componentName, string memberName, long gameObjectId)
        {
            return $"Error: Member '{memberName}' not found on '{componentName}'.\n" +
                   $"Tip: Use unity_inspect_component with game_object_id {gameObjectId} and " +
                   $"component_name \"{componentName}\" to see available members.";
        }

        /// <summary>
        /// Appends inactive-object warning if applicable.
        /// </summary>
        private static void AppendInactiveWarning(StringBuilder sb, GameObject obj)
        {
            if (!obj.activeInHierarchy)
            {
                sb.AppendLine();
                if (!obj.activeSelf)
                    sb.Append($"Warning: '{obj.name}' is DISABLED. Changes will not take effect until enabled. " +
                        "Use unity_set_game_object_active to enable it.");
                else
                    sb.Append($"Warning: '{obj.name}' is inactive because a parent is disabled. " +
                        "Check parent hierarchy with unity_inspect_game_object.");
            }
        }

        // =================================================================
        // 1. unity_search_game_objects
        // =================================================================

        [Serializable]
        private struct SearchParams
        {
            public string search_pattern;
            public bool exact_match;
            public bool include_inactive;
            public int max_results;
        }

        private static string SearchGameObjects_Callback(string parameters)
        {
            SearchParams p;
            try { p = JsonUtility.FromJson<SearchParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"search_pattern\": \"...\"}"; }

            if (string.IsNullOrEmpty(p.search_pattern))
                return "Error: search_pattern is required.";

            try
            {
                int maxResults = p.max_results > 0 ? p.max_results : 50;
                var found = SceneQueryHelper.SearchGameObjects(p.search_pattern, p.exact_match,
                    p.include_inactive, maxResults);
                int totalMatches = SceneQueryHelper.LastSearchTotalMatches;

                if (found.Count == 0)
                    return $"No GameObjects found matching '{p.search_pattern}'.\n" +
                           "Tip: Try a shorter pattern, or set include_inactive to true to search disabled objects.";

                var sb = new StringBuilder();
                sb.AppendLine($"Found {totalMatches} GameObject(s) matching '{p.search_pattern}'" +
                    (totalMatches > found.Count ? $" (showing first {found.Count}):" : ":"));
                for (int i = 0; i < found.Count; i++)
                {
                    var obj = found[i];
                    var comps = obj.GetComponents<Component>();
                    sb.AppendLine($"  {i + 1}. {obj.name}");
                    sb.AppendLine($"     ID: {SceneQueryHelper.GetObjectId(obj)}");
                    sb.AppendLine($"     Path: {SceneQueryHelper.GetGameObjectPath(obj)}");
                    sb.AppendLine($"     Active: {obj.activeSelf}");
                    sb.AppendLine($"     Components: {string.Join(", ", comps.Take(5).Select(c => c?.GetType().Name ?? "Missing"))}{(comps.Length > 5 ? ", ..." : "")}");
                }

                if (totalMatches > found.Count)
                {
                    sb.AppendLine();
                    sb.AppendLine($"... {totalMatches - found.Count} more results. Increase max_results to see more.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: Unexpected failure during search: {ex.Message}\nTip: Scene objects may have been destroyed. Try again.";
            }
        }

        private static void RegisterSearchGameObjects()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_search_game_objects",
                "[CORE] Search for GameObjects in a running XR app's Unity scene by name (case-insensitive substring match, NOT regex). " +
                "Returns a list of matches with instance IDs, active state, and parent path. " +
                "Use the returned instance IDs with unity_inspect_game_object or unity_deep_inspect for details.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "search_pattern",
                        Description = "Name or partial name to search for (case-insensitive substring match — NOT regex or glob).",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "exact_match",
                        Description = "If true, match the full name exactly. Default false (partial match).",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "include_inactive",
                        Description = "If true, include disabled GameObjects. Default false.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "max_results",
                        Description = "Maximum number of results to return. Default 50.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    }
                },
                SearchGameObjects_Callback,
                MainThread
            );
        }

        // =================================================================
        // 2. unity_find_by_component
        // =================================================================

        [Serializable]
        private struct FindByComponentParams
        {
            public string component_type;
            public bool include_inactive;
            public int max_results;
        }

        private static string FindByComponent_Callback(string parameters)
        {
            FindByComponentParams p;
            try { p = JsonUtility.FromJson<FindByComponentParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"component_type\": \"...\"}"; }

            if (string.IsNullOrEmpty(p.component_type))
                return "Error: component_type is required.";

            try
            {
                int maxResults = p.max_results > 0 ? p.max_results : 50;
                var found = SceneQueryHelper.FindByComponent(p.component_type, p.include_inactive, maxResults);
                int totalMatches = SceneQueryHelper.LastSearchTotalMatches;

                if (found.Count == 0)
                    return $"No GameObjects found with component '{p.component_type}'.\n" +
                           "Tip: Component names are partial-matched (e.g. 'Renderer' matches 'MeshRenderer'). " +
                           "Check spelling or try a shorter name.";

                var sb = new StringBuilder();
                sb.AppendLine($"Found {totalMatches} GameObject(s) with '{p.component_type}'" +
                    (totalMatches > found.Count ? $" (showing first {found.Count}):" : ":"));
                foreach (var obj in found)
                {
                    sb.AppendLine($"  {obj.name} (ID: {SceneQueryHelper.GetObjectId(obj)})");
                    sb.AppendLine($"    Path: {SceneQueryHelper.GetGameObjectPath(obj)}");
                    sb.AppendLine($"    Active: {obj.activeSelf}");
                }

                if (totalMatches > found.Count)
                {
                    sb.AppendLine();
                    sb.AppendLine($"... {totalMatches - found.Count} more results. Increase max_results to see more.");
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: Unexpected failure during component search: {ex.Message}\nTip: Try again or use unity_search_game_objects instead.";
            }
        }

        private static void RegisterFindByComponent()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_find_by_component",
                "Find GameObjects in a running XR app that have a specific component type (e.g. Rigidbody, MeshRenderer). Returns instance IDs. Then use unity_inspect_game_object or unity_get_component_value.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "component_type",
                        Description = "Component type name to search for (partial match, case-insensitive).",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "include_inactive",
                        Description = "If true, include disabled GameObjects. Default false.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "max_results",
                        Description = "Maximum number of results to return. Default 50.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    }
                },
                FindByComponent_Callback,
                MainThread
            );
        }

        // =================================================================
        // 3. unity_inspect_game_object
        // =================================================================

        [Serializable]
        private struct InspectGameObjectParams
        {
            public long game_object_id;
        }

        private static string InspectGameObject_Callback(string parameters)
        {
            InspectGameObjectParams p;
            try { p = JsonUtility.FromJson<InspectGameObjectParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345}"; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            try
            {
                var sb = new StringBuilder();
                sb.AppendLine($"GameObject: {obj.name} (ID: {p.game_object_id})");
                sb.AppendLine($"Path: {SceneQueryHelper.GetGameObjectPath(obj)}");
                sb.AppendLine($"Active: {obj.activeSelf} (in hierarchy: {obj.activeInHierarchy})");
                sb.AppendLine($"Layer: {LayerMask.LayerToName(obj.layer)}");
                sb.AppendLine($"Position: {obj.transform.position}");
                sb.AppendLine($"Rotation: {obj.transform.rotation.eulerAngles}");
                sb.AppendLine($"Scale: {obj.transform.localScale}");
                sb.AppendLine();
                sb.AppendLine("Components:");
                foreach (var comp in obj.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    sb.AppendLine($"  - {comp.GetType().Name} {SceneQueryHelper.GetComponentEnabledStatus(comp)}");
                }

                AppendInactiveWarning(sb, obj);

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: Unexpected failure inspecting GameObject: {ex.Message}\n" +
                       "Tip: The object may have been destroyed. Use unity_search_game_objects to find it again.";
            }
        }

        private static void RegisterInspectGameObject()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_inspect_game_object",
                "[CORE] Get overview of a GameObject: transform (position/rotation/scale), active state, layer, tag, and list of all attached components. " +
                "Use the instance ID from unity_search_game_objects. Follow up with unity_inspect_component for component details.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    }
                },
                InspectGameObject_Callback,
                MainThread
            );
        }

        // =================================================================
        // 4. unity_inspect_component
        // =================================================================

        [Serializable]
        private struct InspectComponentParams
        {
            public long game_object_id;
            public string component_name;
            public string member_filter;
        }

        private static string InspectComponent_Callback(string parameters)
        {
            InspectComponentParams p;
            try { p = JsonUtility.FromJson<InspectComponentParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345, \"component_name\": \"Transform\"}\nTip: Use unity_inspect_game_object to find component names."; }

            try
            {
                var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
                if (obj == null)
                    return GameObjectNotFoundError(p.game_object_id);

                var comp = SceneQueryHelper.FindComponent(obj, p.component_name);
                if (comp == null)
                    return ComponentNotFoundError(obj, p.component_name);

                const int MaxOutputChars = 10000;
                const int MaxMembers = 60;

                // When a member_filter is provided, include NonPublic so the user can
                // inspect internal state. Without filter, show only public members.
                BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                     BindingFlags.GetField | BindingFlags.GetProperty;
                if (!string.IsNullOrEmpty(p.member_filter))
                    flags |= BindingFlags.NonPublic;

                var members = comp.GetType().GetMembers(flags);

                // Apply optional member filter
                HashSet<string> filterSet = null;
                if (!string.IsNullOrEmpty(p.member_filter))
                {
                    filterSet = new HashSet<string>(
                        p.member_filter.Split(',').Select(s => s.Trim()),
                        StringComparer.OrdinalIgnoreCase);
                }

                var settable = new List<MemberInfo>();
                var readOnly = new List<MemberInfo>();
                int totalSkipped = 0;

                foreach (var m in members)
                {
                    if ((m.MemberType & (MemberTypes.Property | MemberTypes.Field)) == 0) continue;
                    if (filterSet != null && !filterSet.Contains(m.Name)) continue;

                    if (settable.Count + readOnly.Count >= MaxMembers)
                    {
                        totalSkipped++;
                        continue;
                    }

                    if (SceneQueryHelper.CanBeChanged(m))
                        settable.Add(m);
                    else
                        readOnly.Add(m);
                }

                var sb = new StringBuilder();
                sb.AppendLine($"Component '{p.component_name}' on '{obj.name}' (ID: {p.game_object_id}):");
                sb.AppendLine($"Status: {SceneQueryHelper.GetComponentEnabledStatus(comp)}");
                sb.AppendLine();

                if (settable.Count > 0)
                {
                    sb.AppendLine("SETTABLE MEMBERS:");
                    foreach (var m in settable)
                    {
                        if (sb.Length > MaxOutputChars - 200) { sb.AppendLine("  ... (truncated)"); break; }
                        var val = SceneQueryHelper.GetMemberValue(m, comp);
                        var typeName = SceneQueryHelper.GetMemberType(m)?.Name ?? "Unknown";
                        sb.AppendLine($"  {m.Name} ({typeName}): {SceneQueryHelper.FormatValue(val)}");
                    }
                    sb.AppendLine();
                }


                if (readOnly.Count > 0)
                {
                    sb.AppendLine("READ-ONLY MEMBERS:");
                    foreach (var m in readOnly)
                    {
                        if (sb.Length > MaxOutputChars - 200) { sb.AppendLine("  ... (truncated)"); break; }
                        var val = SceneQueryHelper.GetMemberValue(m, comp);
                        var typeName = SceneQueryHelper.GetMemberType(m)?.Name ?? "Unknown";
                        sb.AppendLine($"  {m.Name} ({typeName}): {SceneQueryHelper.FormatValue(val)}");
                    }
                }


                sb.AppendLine($"Total: {settable.Count} settable, {readOnly.Count} read-only.");
                if (totalSkipped > 0)
                    sb.AppendLine($"({totalSkipped} members omitted — use member_filter to narrow results.)");
                sb.Append("Tip: Use unity_set_component_value to modify settable members (load 'scene_mutation' group first via unity_load_tools), or unity_get_component_value for a single value.");

                string result = sb.ToString();
                if (result.Length > MaxOutputChars)
                    result = result.Substring(0, MaxOutputChars) + "\n... (truncated)";

                return result;
            }
            catch (Exception ex)
            {
                return $"Error: Unexpected failure inspecting component: {ex.Message}\n" +
                       "Tip: The component may have been destroyed. Use unity_search_game_objects to find it again.";
            }
        }

        private static void RegisterInspectComponent()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_inspect_component",
                "[CORE] List a component's public fields and properties with current runtime values, data types, and whether each is settable. " +
                "Use member_filter to narrow results. Returns [SETTABLE] tag on modifiable members. " +
                "To change a value, load scene_mutation group then use unity_set_component_value.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "component_name",
                        Description = "Name of the component type (e.g. 'Transform', 'MeshRenderer').",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "member_filter",
                        Description = "Optional comma-separated list of member names to show. IMPORTANT: When set, visibility expands to include non-public (private/internal) members in addition to public ones.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    }
                },
                InspectComponent_Callback,
                MainThread
            );
        }

        // =================================================================
        // 5. unity_get_component_value
        // =================================================================

        [Serializable]
        private struct GetComponentValueParams
        {
            public long game_object_id;
            public string component_name;
            public string member_name;
        }

        private static string GetComponentValue_Callback(string parameters)
        {
            GetComponentValueParams p;
            try { p = JsonUtility.FromJson<GetComponentValueParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345, \"component_name\": \"Transform\", \"member_name\": \"position\"}\nTip: Use unity_inspect_component to find member names."; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            var comp = SceneQueryHelper.FindComponent(obj, p.component_name);
            if (comp == null)
                return ComponentNotFoundError(obj, p.component_name);

            var member = SceneQueryHelper.FindMember(comp.GetType(), p.member_name);
            if (member == null)
                return MemberNotFoundError(p.component_name, p.member_name, p.game_object_id);

            var value = SceneQueryHelper.GetMemberValue(member, comp);
            var typeName = SceneQueryHelper.GetMemberType(member)?.Name ?? "Unknown";
            bool settable = SceneQueryHelper.CanBeChanged(member);

            return $"Data: {p.component_name}.{p.member_name} on '{obj.name}' (ID: {p.game_object_id}):\n" +
                   $"  Value: {SceneQueryHelper.FormatValue(value)}\n" +
                   $"  Type: {typeName}\n" +
                   $"  Settable: {(settable ? "Yes — use unity_set_component_value to modify (load 'scene_mutation' group via unity_load_tools if not available)" : "No (read-only)")}";
        }

        private static void RegisterGetComponentValue()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_get_component_value",
                "[CORE] Read the current runtime value of a single field or property on a component. " +
                "Returns the value with its type. Use when you know exactly which member you need. " +
                "For a full component overview, use unity_inspect_component instead.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "component_name",
                        Description = "Name of the component type.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "member_name",
                        Description = "Name of the property or field to read.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    }
                },
                GetComponentValue_Callback,
                MainThread
            );
        }

        // =================================================================
        // 6. unity_get_scene_hierarchy
        // =================================================================

        [Serializable]
        private struct GetSceneHierarchyParams
        {
            public int max_depth;
            public long root_object_id;
        }

        private static string GetSceneHierarchy_Callback(string parameters)
        {
            int maxDepth = 3;
            long rootObjectId = 0;

            if (!string.IsNullOrEmpty(parameters) && parameters != "{}")
            {
                try
                {
                    var p = JsonUtility.FromJson<GetSceneHierarchyParams>(parameters);
                    if (p.max_depth > 0) maxDepth = Math.Min(p.max_depth, 20);
                    rootObjectId = p.root_object_id;
                }
                catch
                {
                    return "Error: Invalid parameters. Expected: {\"max_depth\": 3, \"root_object_id\": 12345}\n" +
                           "Tip: Both parameters are optional. max_depth defaults to 3.";
                }
            }

            return SceneQueryHelper.BuildSceneHierarchy(maxDepth, rootObjectId);
        }

        private static void RegisterGetSceneHierarchy()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_get_scene_hierarchy",
                "[CORE] Get the scene hierarchy tree showing GameObject names, instance IDs, and active state. " +
                "Use to understand scene structure before inspecting specific objects. " +
                "Use max_depth to limit tree depth (default 3). Use root_object_id to explore a subtree.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "max_depth",
                        Description = "Maximum depth of hierarchy to show. Default 3. Increase for deeper views.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "root_object_id",
                        Description = "Optional: show hierarchy only under this GameObject ID (from unity_search_game_objects).",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    }
                },
                GetSceneHierarchy_Callback,
                MainThread
            );
        }

        // =================================================================
        // 7. unity_set_component_value
        // =================================================================

        [Serializable]
        private struct SetComponentValueParams
        {
            public long game_object_id;
            public string component_name;
            public string member_name;
            public string value;
        }

        private static string SetComponentValue_Callback(string parameters)
        {
            SetComponentValueParams p;
            try { p = JsonUtility.FromJson<SetComponentValueParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345, \"component_name\": \"Transform\", \"member_name\": \"position\", \"value\": \"1,2,3\"}\nTip: Use unity_get_component_value to see the current value and format."; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            var comp = SceneQueryHelper.FindComponent(obj, p.component_name);
            if (comp == null)
                return ComponentNotFoundError(obj, p.component_name);

            var member = SceneQueryHelper.FindMember(comp.GetType(), p.member_name);
            if (member == null)
                return MemberNotFoundError(p.component_name, p.member_name, p.game_object_id);

            if (!SceneQueryHelper.CanBeChanged(member))
                return $"Error: Member '{p.member_name}' is read-only.\n" +
                       $"Tip: Use unity_inspect_component to see which members are settable.";

            try
            {
                var currentValue = SceneQueryHelper.GetMemberValue(member, comp);
                var targetType = SceneQueryHelper.GetMemberType(member);
                var converted = SceneQueryHelper.ConvertStringToType(p.value, targetType);
                SceneQueryHelper.SetMemberValue(member, comp, converted);
                var newValue = SceneQueryHelper.GetMemberValue(member, comp);

                var sb = new StringBuilder();
                sb.AppendLine($"Success: {p.component_name}.{p.member_name} on '{obj.name}' (ID: {p.game_object_id})");
                sb.AppendLine($"  Changed from: {SceneQueryHelper.FormatValue(currentValue)}");
                sb.AppendLine($"  Changed to: {SceneQueryHelper.FormatValue(newValue)}");
                sb.AppendLine($"  Type: {targetType?.Name ?? "Unknown"}");
                AppendInactiveWarning(sb, obj);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                var currentValue = SceneQueryHelper.GetMemberValue(member, comp);
                return $"Error: Failed to set {p.member_name} = {p.value}:\n" +
                       $"  {ex.Message}\n" +
                       $"  Current value: {SceneQueryHelper.FormatValue(currentValue)}\n" +
                       $"Tip: Use the current value format as reference. " +
                       $"Formats: Numbers (42, 3.14), Booleans (true/false), " +
                       $"Vector3 (1,2,3), Vector2 (1,2), Color (1,0,0,1), Quaternion (0,0,0,1), Enums, " +
                       $"or a scene object's instance ID for GameObject/Component/Transform members.";
            }
        }

        private static void RegisterSetComponentValue()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_set_component_value",
                "Set a component property or field value. Supports int, float, bool, string, Vector3, Vector2, Color, Quaternion, enums, and references to scene objects (GameObject/Component/Transform) passed as their instance ID. Use unity_inspect_component first to see settable members.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "component_name",
                        Description = "Name of the component type.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "member_name",
                        Description = "Name of the property or field to set.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "value",
                        Description = "New value as a string. Formats: number (42), bool (true), Vector3 (1,2,3), Color (1,0,0,1), or a scene object's instance ID for GameObject/Component/Transform members.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    }
                },
                SetComponentValue_Callback,
                MainThread
            );
        }

        // =================================================================
        // 8. unity_set_game_object_active
        // =================================================================

        [Serializable]
        private struct SetActiveParams
        {
            public long game_object_id;
            public bool active;
        }

        private static string SetGameObjectActive_Callback(string parameters)
        {
            SetActiveParams p;
            try { p = JsonUtility.FromJson<SetActiveParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345, \"active\": true}"; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            bool previousState = obj.activeSelf;
            obj.SetActive(p.active);

            var sb = new StringBuilder();
            sb.AppendLine($"Success: '{obj.name}' (ID: {p.game_object_id}) active state changed from {previousState} to {p.active}.");
            if (p.active && !obj.activeInHierarchy)
                sb.Append("Warning: Object is enabled but still inactive in hierarchy — a parent object is disabled.");
            return sb.ToString();
        }

        private static void RegisterSetGameObjectActive()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_set_game_object_active",
                "Enable or disable a GameObject and all its components. Use unity_search_game_objects to find object IDs first.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "active",
                        Description = "True to enable, false to disable.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = true
                    }
                },
                SetGameObjectActive_Callback,
                MainThread
            );
        }

        // =================================================================
        // 9. unity_invoke_method
        // =================================================================

        [Serializable]
        private struct InvokeMethodParams
        {
            public long game_object_id;
            public string component_name;
            public string method_name;
            public string method_args;
        }

        private static string InvokeMethod_Callback(string parameters)
        {
            InvokeMethodParams p;
            try { p = JsonUtility.FromJson<InvokeMethodParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345, \"component_name\": \"MyComponent\", \"method_name\": \"Reset\"}\nTip: Use unity_inspect_component to find the component, then try invoking."; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            var comp = SceneQueryHelper.FindComponent(obj, p.component_name);
            if (comp == null)
                return ComponentNotFoundError(obj, p.component_name);

            // Parse optional comma-separated arguments (simple scalar/bool/enum values).
            string[] argStrings = string.IsNullOrEmpty(p.method_args)
                ? Array.Empty<string>()
                : p.method_args.Split(',').Select(s => s.Trim()).ToArray();

            // Match a public instance method by name and argument count (void or not).
            var candidates = comp.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Where(m => m.Name.Equals(p.method_name, StringComparison.OrdinalIgnoreCase) &&
                            !m.IsGenericMethod &&
                            m.GetParameters().Length == argStrings.Length)
                .ToArray();

            if (candidates.Length == 0)
            {
                var available = comp.GetType()
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => !m.IsSpecialName &&
                                !m.IsGenericMethod &&
                                m.DeclaringType != typeof(MonoBehaviour) &&
                                m.DeclaringType != typeof(Behaviour) &&
                                m.DeclaringType != typeof(Component) &&
                                m.DeclaringType != typeof(UnityEngine.Object))
                    .Select(m => $"{m.Name}({m.GetParameters().Length})")
                    .Distinct()
                    .ToArray();

                return $"Error: No method '{p.method_name}' taking {argStrings.Length} argument(s) found on '{p.component_name}'.\n" +
                       $"Available methods (name(argCount)): {(available.Length > 0 ? string.Join(", ", available) : "(none)")}\n" +
                       "Tip: Pass method_args as a comma-separated list whose count matches the method's parameters.";
            }

            if (candidates.Length > 1)
            {
                var overloads = candidates.Select(m =>
                    $"{m.Name}({string.Join(", ", m.GetParameters().Select(pi => pi.ParameterType.Name))})");
                return $"Error: {candidates.Length} overloads of '{p.method_name}' take " +
                       $"{argStrings.Length} argument(s): {string.Join("; ", overloads)}.\n" +
                       "Tip: This tool matches overloads by name and argument count only and cannot " +
                       "disambiguate these; the method was not invoked.";
            }

            var method = candidates[0];
            var paramInfos = method.GetParameters();
            object[] args = new object[argStrings.Length];
            try
            {
                for (int i = 0; i < argStrings.Length; i++)
                    args[i] = SceneQueryHelper.ConvertStringToType(argStrings[i], paramInfos[i].ParameterType);
            }
            catch (Exception ex)
            {
                return $"Error: Failed to convert arguments for {p.method_name}: {ex.Message}\n" +
                       "Tip: Formats: numbers (42, 3.14), booleans (true/false), enums by name, " +
                       "and scene objects by instance ID (from unity_search_game_objects). " +
                       "Comma-containing types (e.g. Vector3) are not supported as method arguments.";
            }

            try
            {
                object result = method.Invoke(comp, args.Length == 0 ? null : args);
                var sb = new StringBuilder();
                sb.AppendLine($"Success: Invoked {p.component_name}.{p.method_name}({string.Join(", ", argStrings)}) on '{obj.name}' (ID: {p.game_object_id}).");
                if (method.ReturnType != typeof(void))
                    sb.AppendLine($"Returned ({method.ReturnType.Name}): {SceneQueryHelper.FormatValue(result)}");
                AppendInactiveWarning(sb, obj);
                return sb.ToString();
            }
            catch (TargetInvocationException ex)
            {
                var inner = ex.InnerException ?? ex;
                return $"Error: {p.method_name}() threw {inner.GetType().Name}: {inner.Message}\n" +
                       "Tip: Use unity_diagnose for a full diagnostic report, or load 'diagnostics' group via unity_load_tools for unity_get_recent_logs.";
            }
            catch (Exception ex)
            {
                return $"Error: Failed to invoke {p.method_name}(): {ex.Message}\n" +
                       "Tip: Use unity_diagnose for a full diagnostic report, or load 'diagnostics' group via unity_load_tools for unity_get_recent_logs.";
            }
        }

        private static void RegisterInvokeMethod()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_invoke_method",
                "Invoke a public method on a component. Pass method_args as a comma-separated list for parameterized methods (scalars, bools, enums, or a scene object by its instance ID); omit for parameterless methods. Reports the return value for non-void methods. If no match is found, the error lists available methods with their argument counts.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "component_name",
                        Description = "Name of the component type.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "method_name",
                        Description = "Name of the method to invoke (matched by name and argument count).",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "method_args",
                        Description = "Optional comma-separated arguments for parameterized methods (e.g. \"5,true\"). Supports scalars, bools, enums, and scene objects passed by their instance ID (from unity_search_game_objects). Omit for parameterless methods.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    }
                },
                InvokeMethod_Callback,
                MainThread
            );
        }

        // =================================================================
        // 10. unity_debugger_set_visibility
        // =================================================================

        [Serializable]
        private struct DebuggerVisibilityParams
        {
            public bool visible;
        }

        private static string DebuggerSetVisibility_Callback(string parameters)
        {
            DebuggerVisibilityParams p;
            try { p = JsonUtility.FromJson<DebuggerVisibilityParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"visible\": true}"; }

            try
            {
                var debugInterface = UnityEngine.Object.FindFirstObjectByType<
                    ImmersiveDebugger.UserInterface.DebugInterface>(FindObjectsInactive.Include);
                if (debugInterface == null)
                    return "Error: Immersive Debugger interface not found.\n" +
                           "Tip: Ensure Immersive Debugger is enabled in Meta XR settings.";

                if (p.visible)
                    debugInterface.Show();
                else
                    debugInterface.Hide();

                return $"Success: Immersive Debugger is now {(p.visible ? "visible" : "hidden")}.";
            }
            catch (Exception ex)
            {
                return $"Error: Failed to change debugger visibility: {ex.Message}\n" +
                       "Tip: Immersive Debugger may not be fully initialized.";
            }
        }

        private static void RegisterDebuggerSetVisibility()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_debugger_set_visibility",
                "Show or hide the Immersive Debugger in-headset UI overlay. Use unity_debugger_status to check current state.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "visible",
                        Description = "True to show, false to hide the debugger UI.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = true
                    }
                },
                DebuggerSetVisibility_Callback,
                MainThread
            );
        }

        // =================================================================
        // 11. unity_debugger_status
        // =================================================================

        private static string DebuggerStatus_Callback(string parameters)
        {
            var sb = new StringBuilder();
            sb.AppendLine("Immersive Debugger Status:");

            var debugInterface = UnityEngine.Object.FindFirstObjectByType<
                ImmersiveDebugger.UserInterface.DebugInterface>(FindObjectsInactive.Include);
            if (debugInterface == null)
            {
                sb.AppendLine("  Interface: NOT FOUND");
                sb.Append("Tip: Ensure Immersive Debugger is enabled in Meta XR settings.");
                return sb.ToString();
            }
            sb.AppendLine($"  Interface: {(debugInterface.Visibility ? "Visible" : "Hidden")}");
            sb.AppendLine($"  Opacity: {(debugInterface.OpacityOverride ? "Opaque" : "Transparent")}");

            // Console is public; report its status if present.
            var console = UnityEngine.Object.FindFirstObjectByType<
                ImmersiveDebugger.UserInterface.Console>(FindObjectsInactive.Include);
            sb.AppendLine($"  Console Panel: {(console != null ? (console.Visibility ? "Visible" : "Hidden") : "Not Found")}");

            // InspectorPanel and DebugManager are assembly-internal, so we cannot
            // reference them directly. Use the debugInterface's own Visibility as the
            // primary indicator for the overall debugger state.
            sb.AppendLine();
            sb.Append("Tip: Use unity_debugger_set_visibility to show/hide, unity_debugger_set_opacity to toggle transparency.");

            return sb.ToString();
        }

        private static void RegisterDebuggerStatus()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_debugger_status",
                "Get the current status of the Immersive Debugger panels (visibility, availability). Check this before toggling visibility.",
                null,
                DebuggerStatus_Callback,
                MainThread
            );
        }

        // =================================================================
        // 11b. unity_debugger_set_opacity
        // =================================================================

        [Serializable]
        private struct DebuggerOpacityParams
        {
            public bool opaque;
        }

        private static string DebuggerSetOpacity_Callback(string parameters)
        {
            DebuggerOpacityParams p;
            try { p = JsonUtility.FromJson<DebuggerOpacityParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"opaque\": true}\nTip: Set opaque=true for full opacity, opaque=false for transparent panels."; }

            var debugInterface = UnityEngine.Object.FindFirstObjectByType<
                ImmersiveDebugger.UserInterface.DebugInterface>(FindObjectsInactive.Include);
            if (debugInterface == null)
                return "Error: Immersive Debugger interface not found.\n" +
                       "Tip: Ensure Immersive Debugger is enabled in Meta XR settings.";

            debugInterface.OpacityOverride = p.opaque;

            return $"Success: Immersive Debugger panels are now {(p.opaque ? "opaque" : "transparent")}.\n" +
                   "Tip: Use unity_debugger_status to verify current debugger state.";
        }

        private static void RegisterDebuggerSetOpacity()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_debugger_set_opacity",
                "Set the Immersive Debugger panel opacity. Opaque=true for full visibility, opaque=false for see-through panels. Use unity_debugger_status to check current state.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "opaque",
                        Description = "True for fully opaque panels, false for transparent/see-through panels.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = true
                    }
                },
                DebuggerSetOpacity_Callback,
                MainThread
            );
        }

        // =================================================================
        // 12. unity_get_recent_logs
        // =================================================================

        [Serializable]
        private struct GetRecentLogsParams
        {
            public int max_entries;
            public string log_type;
        }

        private static string GetRecentLogs_Callback(string parameters)
        {
            int maxEntries = 20;
            string logType = null;

            if (!string.IsNullOrEmpty(parameters) && parameters != "{}")
            {
                try
                {
                    var p = JsonUtility.FromJson<GetRecentLogsParams>(parameters);
                    if (p.max_entries > 0) maxEntries = p.max_entries;
                    logType = p.log_type;
                }
                catch
                {
                    return "Error: Invalid parameters. Expected: {\"max_entries\": 20, \"log_type\": \"Error\"}\n" +
                           "Tip: log_type can be: Error, Warning, Info, Exception, Assert. Omit to see all types.";
                }
            }

            return LogCaptureService.GetRecentLogs(maxEntries, logType);
        }

        private static void RegisterGetRecentLogs()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_get_recent_logs",
                "Get recent Unity console log entries. Filter by type (Error, Warning, Info). Use this to investigate runtime issues.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "max_entries",
                        Description = "Maximum number of log entries to return. Default 20.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "log_type",
                        Description = "Filter by log type: Error, Warning, Info, Exception, Assert. Default: all types.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    }
                },
                GetRecentLogs_Callback,
                MainThread
            );
        }

        // =================================================================
        // 13. unity_get_error_summary
        // =================================================================

        private static string GetErrorSummary_Callback(string parameters)
        {
            return LogCaptureService.GetErrorSummary();
        }

        private static void RegisterGetErrorSummary()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_get_error_summary",
                "Get a deduplicated summary of errors and exceptions with occurrence counts. Use this to quickly assess what's broken.",
                null,
                GetErrorSummary_Callback,
                MainThread
            );
        }

        // =================================================================
        // 14. unity_get_health_status
        // =================================================================

        private static string GetHealthStatus_Callback(string parameters)
        {
            return LogCaptureService.GetHealthStatus();
        }

        private static void RegisterGetHealthStatus()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_get_health_status",
                "[CORE] Quick single-line health check: returns one of HEALTHY/NEEDS_ATTENTION/CRITICAL with error and warning counts only (no details). Use this for fast status polling or conditional checks. For a full diagnostic report with error messages, stack traces, and recommendations, use unity_diagnose instead.",
                null,
                GetHealthStatus_Callback,
                MainThread
            );
        }

        // =================================================================
        // 14d. unity_diagnose (derivative — combines health + errors + warnings + recent logs)
        // =================================================================

        [Serializable]
        private struct DiagnoseParams
        {
            public bool include_warnings;
            public bool include_recent_logs;
        }

        private static string Diagnose_Callback(string parameters)
        {
            // Parse optional parameters (defaults: include everything)
            bool includeWarnings = true;
            bool includeRecentLogs = true;
            if (!string.IsNullOrEmpty(parameters) && parameters != "{}")
            {
                try
                {
                    var p = JsonUtility.FromJson<DiagnoseParams>(parameters);
                    // JsonUtility defaults bools to false, so only override if explicitly set
                    // Since we want defaults to be true, we check the raw JSON
                    if (parameters.Contains("\"include_warnings\""))
                        includeWarnings = p.include_warnings;
                    if (parameters.Contains("\"include_recent_logs\""))
                        includeRecentLogs = p.include_recent_logs;
                }
                catch { /* Use defaults */ }
            }

            const int MaxOutputChars = 12000;
            var sb = new StringBuilder();

            // 1. Health status
            sb.AppendLine("=== DIAGNOSTIC REPORT ===");
            sb.AppendLine();
            sb.AppendLine(LogCaptureService.GetHealthStatus());
            sb.AppendLine();

            // 2. Error summary
            sb.AppendLine("--- Errors ---");
            string errors = LogCaptureService.GetErrorSummary();
            sb.AppendLine(errors);
            sb.AppendLine();

            // 3. Warning summary (optional)
            if (includeWarnings)
            {
                sb.AppendLine("--- Warnings ---");
                string warnings = LogCaptureService.GetWarningSummary();
                sb.AppendLine(warnings);
                sb.AppendLine();
            }

            // 4. Recent error logs (optional)
            if (includeRecentLogs)
            {
                sb.AppendLine("--- Recent Error Logs ---");
                string recentErrors = LogCaptureService.GetRecentLogs(5, "Error");
                sb.AppendLine(recentErrors);
                sb.AppendLine();
            }

            // 5. Recommended actions based on health status
            sb.AppendLine("--- Recommended Actions ---");
            if (errors.Contains("No errors"))
            {
                sb.AppendLine("No errors detected. Scene appears healthy.");
                sb.AppendLine("Tip: Use unity_search_game_objects to explore the scene.");
            }
            else
            {
                sb.AppendLine("1. Use unity_deep_inspect with error-related object names to examine component state.");
                sb.AppendLine("2. Use unity_search_game_objects to locate relevant GameObjects.");
                sb.AppendLine("3. Use unity_load_tools(action='load', group='diagnostics') then unity_search_logs for detailed log search.");
                sb.AppendLine("4. Use unity_load_tools(action='load', group='scene_mutation') to modify values.");
            }

            string result = sb.ToString();
            if (result.Length > MaxOutputChars)
                result = result.Substring(0, MaxOutputChars) + "\n... (truncated)";

            return result;
        }

        private static void RegisterDiagnose()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_diagnose",
                "[CORE] Comprehensive diagnostic report: health status + detailed error messages + warning summary + recent log entries, all in one call. " +
                "Use this as the FIRST tool when starting a debugging session. Returns actionable information including stack traces and recommendations. " +
                "For a quick pass/fail check without details, use unity_get_health_status instead.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "include_warnings",
                        Description = "Include warning summary. Default true.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "include_recent_logs",
                        Description = "Include recent error log entries. Default true.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = false
                    }
                },
                Diagnose_Callback,
                MainThread
            );
        }

        // =================================================================
        // 14e. unity_deep_inspect (derivative — search + inspect object + inspect components)
        // =================================================================

        [Serializable]
        private struct DeepInspectParams
        {
            public string search_pattern;
            public long game_object_id;
            public string component_filter;
            public int max_components;
        }

        private static string DeepInspect_Callback(string parameters)
        {
            DeepInspectParams p;
            try { p = JsonUtility.FromJson<DeepInspectParams>(parameters); }
            catch
            {
                return "Error: Invalid parameters. Expected: {\"search_pattern\": \"PlayerController\"} or {\"game_object_id\": 12345}\n" +
                           "Tip: Provide either search_pattern (name) or game_object_id (instance ID from previous search).";
            }

            const int MaxOutputChars = 14000;
            int maxComponents = p.max_components > 0 ? Math.Min(p.max_components, 10) : 5;

            // 1. Resolve the target GameObject
            GameObject obj = null;
            List<GameObject> results = null;

            if (p.game_object_id != 0)
            {
                obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
                if (obj == null)
                    return GameObjectNotFoundError(p.game_object_id);
            }
            else if (!string.IsNullOrEmpty(p.search_pattern))
            {
                results = SceneQueryHelper.SearchGameObjects(p.search_pattern, false, false, 5);
                if (results.Count == 0)
                    return $"Error: No GameObjects found matching '{p.search_pattern}'.\n" +
                           "Tip: Try a shorter or different search pattern. Use unity_get_scene_hierarchy to browse the scene.";

                obj = results[0]; // Use best match
            }
            else
            {
                return "Error: Provide either 'search_pattern' (name) or 'game_object_id' (instance ID).\n" +
                       "Tip: Use unity_search_game_objects to find objects, then pass the ID here.";
            }

            var sb = new StringBuilder();

            // Note if multiple matches were found (reuse the results from above)
            if (!string.IsNullOrEmpty(p.search_pattern) && results != null && results.Count > 1)
            {
                sb.AppendLine($"Note: {results.Count} objects matched '{p.search_pattern}'. Showing best match.");
                sb.AppendLine($"Other matches: {string.Join(", ", results.Skip(1).Select(r => $"'{r.name}' (ID: {SceneQueryHelper.GetObjectId(r)})"))}");
                sb.AppendLine();
            }

            // 2. Object overview (same as inspect_game_object)
            sb.AppendLine($"=== DEEP INSPECTION: {obj.name} (ID: {SceneQueryHelper.GetObjectId(obj)}) ===");
            sb.AppendLine();
            sb.AppendLine($"Path: {SceneQueryHelper.GetGameObjectPath(obj)}");
            sb.AppendLine($"Active: {obj.activeSelf} (in hierarchy: {obj.activeInHierarchy})");
            sb.AppendLine($"Layer: {LayerMask.LayerToName(obj.layer)} ({obj.layer})");
            sb.AppendLine($"Tag: {obj.tag}");
            sb.AppendLine($"Position: {obj.transform.position}");
            sb.AppendLine($"Rotation: {obj.transform.rotation.eulerAngles} (euler)");
            sb.AppendLine($"Scale: {obj.transform.localScale}");
            sb.AppendLine();

            // 3. Component list
            var components = obj.GetComponents<Component>().Where(c => c != null).ToArray();
            sb.AppendLine($"Components ({components.Length}):");
            foreach (var comp in components)
            {
                string enabled = SceneQueryHelper.GetComponentEnabledStatus(comp);
                sb.AppendLine($"  - {comp.GetType().Name} {enabled}");
            }
            sb.AppendLine();

            // 4. Deep inspect each user component (skip Transform, skip Unity internals)
            var bindingFlags = BindingFlags.Public | BindingFlags.Instance;
            int inspectedCount = 0;
            string compFilter = string.IsNullOrEmpty(p.component_filter) ? null : p.component_filter;

            foreach (var comp in components)
            {
                if (inspectedCount >= maxComponents) break;
                if (sb.Length > MaxOutputChars - 500) break;

                var type = comp.GetType();
                string typeName = type.Name;

                // Skip Transform (already shown above) and Unity internal components unless filtered
                if (compFilter == null)
                {
                    if (typeName == "Transform" || typeName == "RectTransform") continue;
                    if (type.Namespace != null && type.Namespace.StartsWith("UnityEngine.")) continue;
                }
                else if (typeName.IndexOf(compFilter, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                sb.AppendLine($"--- {typeName} ---");

                try
                {
                    var members = type.GetMembers(bindingFlags)
                        .Where(m => (m.MemberType & (MemberTypes.Property | MemberTypes.Field)) != 0)
                        .Where(m => m.DeclaringType != typeof(MonoBehaviour) &&
                                    m.DeclaringType != typeof(Behaviour) &&
                                    m.DeclaringType != typeof(Component) &&
                                    m.DeclaringType != typeof(UnityEngine.Object))
                        .Take(20)
                        .ToArray();

                    foreach (var member in members)
                    {
                        try
                        {
                            object value = SceneQueryHelper.GetMemberValue(member, comp);
                            string formatted = SceneQueryHelper.FormatValue(value);
                            bool settable = SceneQueryHelper.CanBeChanged(member);
                            string settableTag = settable ? " [SETTABLE]" : "";
                            string memberType = SceneQueryHelper.GetMemberType(member)?.Name ?? "?";
                            sb.AppendLine($"  {member.Name} ({memberType}): {formatted}{settableTag}");
                        }
                        catch
                        {
                            sb.AppendLine($"  {member.Name}: <unable to read>");
                        }
                    }

                    if (members.Length == 0)
                        sb.AppendLine("  (no public fields/properties)");
                }
                catch
                {
                    sb.AppendLine("  (unable to inspect — type load error)");
                }

                sb.AppendLine();
                inspectedCount++;
            }

            // 5. Summary
            int settableCount = 0;
            foreach (var comp in components)
            {
                try
                {
                    var members = comp.GetType().GetMembers(bindingFlags)
                        .Where(m => (m.MemberType & (MemberTypes.Property | MemberTypes.Field)) != 0)
                        .Where(m => SceneQueryHelper.CanBeChanged(m));
                    settableCount += members.Count();
                }
                catch { /* Skip types that fail to load */ }
            }

            sb.AppendLine($"Settable members: {settableCount} across {components.Length} components.");
            sb.AppendLine("Tip: Use unity_set_component_value to modify settable members. " +
                          "Use unity_load_tools(action='load', group='scene_mutation') if set tools are not loaded.");

            string result = sb.ToString();
            if (result.Length > MaxOutputChars)
                result = result.Substring(0, MaxOutputChars) + "\n... (truncated)";

            return result;
        }

        private static void RegisterDeepInspect()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_deep_inspect",
                "[CORE] All-in-one object analysis: searches by name or accepts an ID, then shows transform, components, and all public fields/properties with values and settability in a single call. " +
                "Prefer this over chaining search + inspect_game_object + inspect_component calls. " +
                "Use component_filter to focus on specific component types.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "search_pattern",
                        Description = "Name or partial name to search for (substring match, NOT regex). Mutually exclusive with game_object_id.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Instance ID from a previous search. Mutually exclusive with search_pattern.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "component_filter",
                        Description = "Only inspect components matching this name (case-insensitive partial match).",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "max_components",
                        Description = "Max components to fully inspect. Default 5, max 10.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    }
                },
                DeepInspect_Callback,
                MainThread
            );
        }

        // =================================================================
        // 14a. unity_get_warning_summary
        // =================================================================

        private static string GetWarningSummary_Callback(string parameters)
        {
            return LogCaptureService.GetWarningSummary();
        }

        private static void RegisterGetWarningSummary()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_get_warning_summary",
                "Get deduplicated summary of warnings with occurrence counts. Complements unity_get_error_summary for non-critical issues. Warnings are often early indicators of problems.",
                null,
                GetWarningSummary_Callback,
                MainThread
            );
        }

        // =================================================================
        // 14b. unity_search_logs
        // =================================================================

        [Serializable]
        private struct SearchLogsParams
        {
            public string keyword;
            public int max_results;
        }

        private static string SearchLogs_Callback(string parameters)
        {
            SearchLogsParams p;
            try { p = JsonUtility.FromJson<SearchLogsParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"keyword\": \"NullReference\"}"; }

            int maxResults = p.max_results > 0 ? p.max_results : 20;
            return LogCaptureService.SearchLogs(p.keyword, maxResults);
        }

        private static void RegisterSearchLogs()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_search_logs",
                "Search log buffer by keyword (case-insensitive). Returns matching entries with full stack traces. Use to filter logs by component name, error type, or specific message.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "keyword",
                        Description = "Search keyword (case-insensitive substring match in message and stack trace).",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "max_results",
                        Description = "Maximum entries to return. Default 20.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    }
                },
                SearchLogs_Callback,
                MainThread
            );
        }

        // =================================================================
        // 14c. unity_clear_diagnostic_data
        // =================================================================

        private static string ClearDiagnosticData_Callback(string parameters)
        {
            return LogCaptureService.ClearDiagnosticData();
        }

        private static void RegisterClearDiagnosticData()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_clear_diagnostic_data",
                "Clear all captured log entries and reset error/warning/info counters. Use between test runs to get clean diagnostic state.",
                null,
                ClearDiagnosticData_Callback,
                MainThread
            );
        }

        // =================================================================
        // 15. unity_toggle_game_object_active
        // =================================================================

        [Serializable]
        private struct ToggleActiveParams
        {
            public long game_object_id;
        }

        private static string ToggleGameObjectActive_Callback(string parameters)
        {
            ToggleActiveParams p;
            try { p = JsonUtility.FromJson<ToggleActiveParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345}"; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            bool previousState = obj.activeSelf;
            obj.SetActive(!previousState);

            var sb = new StringBuilder();
            sb.AppendLine($"Success: '{obj.name}' (ID: {p.game_object_id}) toggled from {previousState} to {!previousState}.");
            if (!previousState && !obj.activeInHierarchy)
                sb.Append("Warning: Object is now enabled but still inactive in hierarchy — a parent object is disabled.");
            return sb.ToString();
        }

        private static void RegisterToggleGameObjectActive()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_toggle_game_object_active",
                "Toggle a GameObject's active state without needing to read it first. Simpler than unity_set_game_object_active for quick enable/disable.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    }
                },
                ToggleGameObjectActive_Callback,
                MainThread
            );
        }

        // =================================================================
        // 16. unity_get_settable_overview
        // =================================================================

        [Serializable]
        private struct GetSettableOverviewParams
        {
            public long game_object_id;
            public int max_members_per_component;
        }

        private static string GetSettableOverview_Callback(string parameters)
        {
            GetSettableOverviewParams p;
            try { p = JsonUtility.FromJson<GetSettableOverviewParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345}"; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            try
            {
                const int MaxOutputChars = 8000;
                int maxPerComp = p.max_members_per_component > 0 ? p.max_members_per_component : 5;

                var sb = new StringBuilder();
                sb.AppendLine($"Settable Overview for '{obj.name}' (ID: {p.game_object_id}):");
                sb.AppendLine();

                const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public |
                                           BindingFlags.GetField | BindingFlags.GetProperty;
                int totalSettable = 0;

                foreach (var comp in obj.GetComponents<Component>())
                {
                    if (comp == null) continue;
                    if (sb.Length > MaxOutputChars - 300) { sb.AppendLine("  ... (truncated — too many components)"); break; }

                    var compType = comp.GetType();
                    var members = compType.GetMembers(flags);

                    var settable = new List<MemberInfo>();
                    foreach (var m in members)
                    {
                        if ((m.MemberType & (MemberTypes.Property | MemberTypes.Field)) == 0) continue;
                        if (SceneQueryHelper.CanBeChanged(m))
                            settable.Add(m);
                    }

                    if (settable.Count == 0) continue;

                    sb.AppendLine($"  {compType.Name} ({settable.Count} settable):");
                    int shown = 0;
                    foreach (var m in settable)
                    {
                        if (shown >= maxPerComp)
                        {
                            sb.AppendLine($"    ... ({settable.Count - maxPerComp} more)");
                            break;
                        }
                        var val = SceneQueryHelper.GetMemberValue(m, comp);
                        var typeName = SceneQueryHelper.GetMemberType(m)?.Name ?? "?";
                        sb.AppendLine($"    {m.Name} ({typeName}): {SceneQueryHelper.FormatValue(val)}");
                        shown++;
                    }
                    totalSettable += settable.Count;
                }


                if (totalSettable == 0)
                {
                    sb.AppendLine("  No settable members found on any component.");
                    sb.Append("Tip: Use unity_inspect_component with member_filter for non-public members.");
                }
                else
                {
                    sb.AppendLine();
                    sb.AppendLine($"Total: {totalSettable} settable members across all components.");
                    sb.Append("Tip: Use unity_set_component_value to modify any of these.");
                }

                string result = sb.ToString();
                if (result.Length > MaxOutputChars)
                    result = result.Substring(0, MaxOutputChars) + "\n... (truncated)";

                return result;
            }
            catch (Exception ex)
            {
                return $"Error: Unexpected failure getting settable overview: {ex.Message}\n" +
                       "Tip: The object may have been destroyed. Use unity_search_game_objects to find it again.";
            }
        }

        private static void RegisterGetSettableOverview()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_get_settable_overview",
                "Get all settable properties/fields across ALL components on a GameObject. Shows top members per component. One call instead of inspecting each component.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "max_members_per_component",
                        Description = "Max settable members to show per component. Default 5.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    }
                },
                GetSettableOverview_Callback,
                MainThread
            );
        }

        // =================================================================
        // 17. unity_add_gizmo
        // =================================================================

        /// <summary>Tracks gizmos added via RuntimeAPIs (add-only, no public removal API).</summary>
        private static readonly List<string> _addedGizmos = new List<string>();

        [Serializable]
        private struct AddGizmoParams
        {
            public long game_object_id;
            public string component_name;
            public string member_name;
            public string gizmo_type;
            public string color;
            public string category;
        }

        private static string AddGizmo_Callback(string parameters)
        {
            AddGizmoParams p;
            try { p = JsonUtility.FromJson<AddGizmoParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345, \"component_name\": \"MyComponent\", \"member_name\": \"position\"}\nTip: Use unity_inspect_component to find component members."; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            if (!obj.activeInHierarchy)
                return $"Error: '{obj.name}' (ID: {p.game_object_id}) is not active in hierarchy.\n" +
                       "Tip: RuntimeAPIs requires active GameObjects. Use unity_set_game_object_active to enable it first.";

            if (string.IsNullOrEmpty(p.component_name) || string.IsNullOrEmpty(p.member_name))
                return "Error: component_name and member_name are required.\n" +
                       "Tip: Use unity_inspect_component to find member names on the target component.";

            string gizmoType = string.IsNullOrEmpty(p.gizmo_type) ? "Axis" : p.gizmo_type;
            string color = string.IsNullOrEmpty(p.color) ? "red" : p.color;
            string category = string.IsNullOrEmpty(p.category) ? "Meta XR Operator" : p.category;

            try
            {
                var result = ImmersiveDebugger.RuntimeAPIs.AddInspectorItemWithGizmo(
                    category, gizmoType, color, obj.name, p.component_name, p.member_name);

                string description = $"{gizmoType} gizmo ({color}) on {obj.name}.{p.component_name}.{p.member_name}";

                if (!result.IsSuccess)
                    return $"Error: RuntimeAPIs rejected gizmo: {result.Message}\n" +
                           $"Context: {result.Context}\n" +
                           "Tip: Ensure the member type is compatible with gizmo type. " +
                           "Axis requires Pose/Transform, Point requires Vector3/Transform.";

                _addedGizmos.Add(description);

                // Evict oldest descriptions to prevent unbounded growth
                while (_addedGizmos.Count > MaxGizmoListSize)
                    _addedGizmos.RemoveAt(0);

                var sb = new StringBuilder();
                sb.AppendLine($"Success: Added {description}.");
                sb.AppendLine($"Category: {category}");
                sb.AppendLine($"RuntimeAPIs result: {result}");
                sb.AppendLine();
                sb.Append("Note: Gizmos added via RuntimeAPIs are permanent for the session (no public removal API). " +
                    "Use unity_debugger_set_visibility to show the debugger overlay to see the gizmo.");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: Failed to add gizmo: {ex.Message}\n" +
                       $"Tip: Ensure the member type is compatible with gizmo type '{gizmoType}'. " +
                       "Axis requires Pose/Transform, Point requires Vector3/Transform.";
            }
        }

        private static void RegisterAddGizmo()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_add_gizmo",
                "Add a visual gizmo (axis, point, cube, etc.) to a component member in the Immersive Debugger. Gizmos are permanent for the session.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "component_name",
                        Description = "Component type name (e.g. 'Transform').",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "member_name",
                        Description = "Member name whose value the gizmo visualizes (e.g. 'position').",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "gizmo_type",
                        Description = "Gizmo type: Axis, Point, Line, Cube, Box. Default: Axis.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "color",
                        Description = "Color name (red, green, blue, yellow, cyan) or hex (#ff0000ff). Default: red.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "category",
                        Description = "Debugger panel category name. Default: Meta XR Operator.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    }
                },
                AddGizmo_Callback,
                MainThread
            );
        }

        // =================================================================
        // 18. unity_list_added_gizmos
        // =================================================================

        private static string ListAddedGizmos_Callback(string parameters)
        {
            if (_addedGizmos.Count == 0)
                return "No gizmos have been added in this session.\n" +
                       "Tip: Use unity_add_gizmo to add visual gizmos to component members.";

            var sb = new StringBuilder();
            sb.AppendLine($"Added gizmos ({_addedGizmos.Count}):");
            for (int i = 0; i < _addedGizmos.Count; i++)
                sb.AppendLine($"  {i + 1}. {_addedGizmos[i]}");
            sb.AppendLine();
            sb.Append("Note: Gizmos are permanent for the session. Use unity_debugger_set_visibility " +
                "to show/hide the debugger overlay.");
            return sb.ToString();
        }

        private static void RegisterListAddedGizmos()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_list_added_gizmos",
                "List all gizmos added in this session via unity_add_gizmo. Gizmos are permanent (no removal API).",
                null,
                ListAddedGizmos_Callback,
                MainThread
            );
        }

        // =================================================================
        // 19. unity_debugger_add_inspector
        // =================================================================

        [Serializable]
        private struct AddInspectorParams
        {
            public long game_object_id;
            public string component_name;
            public string members;
            public string category;
        }

        private static string DebuggerAddInspector_Callback(string parameters)
        {
            AddInspectorParams p;
            try { p = JsonUtility.FromJson<AddInspectorParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345, \"component_name\": \"MyComponent\"}\nTip: Use unity_inspect_game_object to find component names."; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            if (!obj.activeInHierarchy)
                return $"Error: '{obj.name}' (ID: {p.game_object_id}) is not active in hierarchy.\n" +
                       "Tip: RuntimeAPIs requires active GameObjects. Use unity_set_game_object_active to enable it first.";

            if (string.IsNullOrEmpty(p.component_name))
                return "Error: component_name is required.\n" +
                       "Tip: Use unity_inspect_game_object to see available components.";

            string category = string.IsNullOrEmpty(p.category) ? "Meta XR Operator" : p.category;
            string members = p.members ?? "";

            try
            {
                var result = ImmersiveDebugger.RuntimeAPIs.AddInspector(
                    category, obj.name, p.component_name, members);

                if (!result.IsSuccess)
                    return $"Error: RuntimeAPIs rejected inspector: {result.Message}\n" +
                           $"Context: {result.Context}\n" +
                           $"Tip: Ensure '{p.component_name}' exists on '{obj.name}' and the object is active.";

                var sb = new StringBuilder();
                sb.AppendLine($"Success: Added live inspector for {p.component_name} on '{obj.name}' (ID: {p.game_object_id}).");
                sb.AppendLine($"Category: {category}");
                if (!string.IsNullOrEmpty(members))
                    sb.AppendLine($"Members: {members}");
                else
                    sb.AppendLine("Members: all public members");
                sb.AppendLine($"RuntimeAPIs result: {result}");
                sb.AppendLine();
                sb.Append("Tip: Use unity_debugger_set_visibility true to show the debugger and see live values in headset.");
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: Failed to add inspector: {ex.Message}\n" +
                       $"Tip: Ensure '{p.component_name}' exists on '{obj.name}' and the object is active.";
            }
        }

        private static void RegisterDebuggerAddInspector()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_debugger_add_inspector",
                "Add a live inspector item to the in-headset Immersive Debugger panel for a component. Shows live values. Use unity_debugger_set_visibility to show the panel.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "component_name",
                        Description = "Component type name to inspect (e.g. 'Transform', 'MeshRenderer').",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "members",
                        Description = "Comma-separated member names to show (e.g. 'position,rotation'). Default: all public members.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "category",
                        Description = "Debugger panel category name. Default: Meta XR Operator.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    }
                },
                DebuggerAddInspector_Callback,
                MainThread
            );
        }

        // =================================================================
        // Phase 9d: Developer Discovery Tools
        // =================================================================

        [Serializable]
        private struct FindDebugMembersParams
        {
            public string component_filter;
            public int max_results;
        }

        private static string FindDebugMembers_Callback(string parameters)
        {
            FindDebugMembersParams p;
            try { p = JsonUtility.FromJson<FindDebugMembersParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {} or {\"max_results\": 50}\nTip: Call with no parameters to scan all scene objects for [DebugMember] annotations."; }

            int maxResults = p.max_results > 0 ? Math.Min(p.max_results, 100) : 50;
            string componentFilter = string.IsNullOrEmpty(p.component_filter) ? null : p.component_filter;

            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic
                             | BindingFlags.Instance | BindingFlags.Static;

            var sb = new StringBuilder();
            int totalComponents = 0;
            int totalMembers = 0;
            int resultCount = 0;
            var seenTypes = new HashSet<Type>();
            bool timedOut = false;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Scan all MonoBehaviours in the scene (including inactive, since developers
            // often disable prefab roots while keeping [DebugMember] annotations)
            var allBehaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var behaviour in allBehaviours)
            {
                if (behaviour == null) continue;

                var type = behaviour.GetType();
                // Skip Unity internal types and our own tools
                if (type.Namespace != null && (
                    type.Namespace.StartsWith("UnityEngine") ||
                    type.Namespace.StartsWith("UnityEditor") ||
                    type.Namespace == "Meta.XR" ||
                    type.Namespace.StartsWith("Meta.XR.ImmersiveDebugger.XROperator")))
                    continue;

                // Apply component filter if specified
                if (componentFilter != null &&
                    type.Name.IndexOf(componentFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // Skip already-processed types (avoid duplicates from multiple instances)
                if (!seenTypes.Add(type)) continue;

                // Time guard: stay within the 5-second deferred callback limit
                if (stopwatch.ElapsedMilliseconds > 4000)
                {
                    timedOut = true;
                    break;
                }

                // Scan members for [DebugMember] attribute
                MemberInfo[] members;
                try { members = type.GetMembers(bindingFlags); }
                catch (TypeLoadException) { continue; }
                catch (ReflectionTypeLoadException) { continue; }

                var debugMembers = new List<(MemberInfo member, ImmersiveDebugger.DebugMember attr, bool isNested)>();

                foreach (var member in members)
                {
                    ImmersiveDebugger.DebugMember attr;
                    try { attr = member.GetCustomAttribute<ImmersiveDebugger.DebugMember>(); }
                    catch { continue; }
                    if (attr == null) continue;

                    debugMembers.Add((member, attr, false));

                    // Check for nested class members
                    Type memberType = null;
                    if (member is FieldInfo fi) memberType = fi.FieldType;
                    else if (member is PropertyInfo pi) memberType = pi.PropertyType;

                    if (memberType != null && !memberType.IsPrimitive && memberType != typeof(string)
                        && !memberType.IsEnum && !IsUnityValueType(memberType))
                    {
                        MemberInfo[] nestedMembers;
                        try { nestedMembers = memberType.GetMembers(bindingFlags); }
                        catch { continue; }

                        foreach (var nested in nestedMembers)
                        {
                            ImmersiveDebugger.DebugMember nestedAttr;
                            try { nestedAttr = nested.GetCustomAttribute<ImmersiveDebugger.DebugMember>(); }
                            catch { continue; }
                            if (nestedAttr != null)
                                debugMembers.Add((nested, nestedAttr, true));
                        }
                    }
                }

                if (debugMembers.Count == 0) continue;

                totalComponents++;

                // Find an instance of this component to get the GameObject name
                string goName = behaviour.gameObject.name;
                long goId = SceneQueryHelper.GetObjectId(behaviour.gameObject);
                string activeMarker = behaviour.gameObject.activeInHierarchy ? "" : " [INACTIVE]";

                sb.AppendLine($"--- {type.Name} on '{goName}' (ID: {goId}){activeMarker} ---");

                foreach (var (member, attr, isNested) in debugMembers)
                {
                    if (resultCount >= maxResults)
                    {
                        sb.AppendLine($"\n... (truncated at {maxResults} results)");
                        goto done;
                    }

                    string prefix = isNested ? "  └ " : "  ";
                    string memberKind = member is MethodInfo ? "method" :
                                       member is PropertyInfo ? "property" : "field";
                    string memberTypeName = "";
                    if (member is FieldInfo field) memberTypeName = field.FieldType.Name;
                    else if (member is PropertyInfo prop) memberTypeName = prop.PropertyType.Name;
                    else if (member is MethodInfo) memberTypeName = "void";

                    var parts = new List<string>();
                    if (attr.GizmoType != ImmersiveDebugger.DebugGizmoType.None)
                        parts.Add($"gizmo:{attr.GizmoType}");
                    if (attr.Tweakable && memberKind != "method")
                        parts.Add("tweakable");
                    if (!string.IsNullOrEmpty(attr.Category))
                        parts.Add($"category:{attr.Category}");
                    if (!string.IsNullOrEmpty(attr.DisplayName))
                        parts.Add($"display:{attr.DisplayName}");
                    if (!string.IsNullOrEmpty(attr.Description))
                        parts.Add($"desc:\"{attr.Description}\"");

                    string flags = parts.Count > 0 ? $" [{string.Join(", ", parts)}]" : "";
                    sb.AppendLine($"{prefix}{member.Name} ({memberKind}, {memberTypeName}){flags}");

                    totalMembers++;
                    resultCount++;
                }

                sb.AppendLine();
            }

            done:

            if (totalComponents == 0)
            {
                string filterMsg = componentFilter != null
                    ? $" matching '{componentFilter}'"
                    : "";
                return $"No components{filterMsg} with [DebugMember]-annotated members found in the scene.\n" +
                       "Tip: [DebugMember] is an ImmersiveDebugger attribute that developers use to mark fields, " +
                       "properties, and methods for in-headset debugging. " +
                       "Use unity_inspect_component to see all members regardless of annotation.";
            }

            var header = new StringBuilder();
            header.AppendLine($"Debug Members Found: {totalMembers} annotated member(s) across {totalComponents} component type(s)" +
                (componentFilter != null ? $" (filtered by '{componentFilter}')" : "") + ":");
            header.AppendLine();
            header.Append(sb);

            if (timedOut)
                header.AppendLine($"\nWarning: Scan timed out after 4 seconds ({seenTypes.Count} types scanned). " +
                    "Use component_filter to narrow the search.");

            header.AppendLine("Tip: These are developer-marked inspection points. " +
                "Use unity_debugger_add_inspector to add them to the live debugger panel, " +
                "or unity_get_component_value to read their values.");

            return header.ToString();
        }

        /// <summary>
        /// Returns true for Unity value types that should NOT be scanned for nested [DebugMember] attributes.
        /// Mirrors the exclusion list in ImmersiveDebugger's ManagerUtils.HasNestedDebugMembers().
        /// </summary>
        private static bool IsUnityValueType(Type type)
        {
            return type == typeof(Vector2) || type == typeof(Vector3) || type == typeof(Vector4)
                || type == typeof(Quaternion) || type == typeof(Color) || type == typeof(Rect)
                || type == typeof(Bounds) || type == typeof(Matrix4x4) || type == typeof(Texture2D);
        }

        private static void RegisterFindDebugMembers()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_find_debug_members",
                "Discover [DebugMember]-annotated fields, properties, and methods in the scene. " +
                "These are values the developer intentionally exposed for debugging via ImmersiveDebugger. " +
                "Shows gizmo type, tweakability, category, and nested class members.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "component_filter",
                        Description = "Filter by component type name (case-insensitive substring match). Default: all components.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "max_results",
                        Description = "Maximum number of annotated members to return. Default: 50, max: 100.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    }
                },
                FindDebugMembers_Callback,
                MainThread
            );
        }

        // =================================================================
        // Phase 9c: Enhanced Visualization Tools
        // =================================================================

        /// <summary>Drawing container for persistent gizmos. Survives scene loads.</summary>
        private static GameObject _drawingContainer;

        /// <summary>Tracks all drawings: key=label, value=gizmo GameObject.</summary>
        private static readonly Dictionary<string, GameObject> _activeDrawings =
            new Dictionary<string, GameObject>();

        private static void EnsureDrawingContainer()
        {
            if (_drawingContainer == null)
            {
                _drawingContainer = new GameObject("[MetaXROperator_Drawings]");
                UnityEngine.Object.DontDestroyOnLoad(_drawingContainer);
            }
        }

        // --- unity_draw_bounding_box ---

        [Serializable]
        private struct DrawBoundingBoxParams
        {
            public long game_object_id;
            public string label;
            public string color;
            public float scale;
            public float padding;
        }

        private static string DrawBoundingBox_Callback(string parameters)
        {
            DrawBoundingBoxParams p;
            try { p = JsonUtility.FromJson<DrawBoundingBoxParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345}\nTip: Use unity_search_game_objects to find objects by name."; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            string label = string.IsNullOrEmpty(p.label)
                ? $"BBox_{obj.name}_{p.game_object_id}"
                : p.label;
            string color = string.IsNullOrEmpty(p.color) ? "red" : p.color;

            // Remove existing drawing with same label
            if (_activeDrawings.TryGetValue(label, out var existing) && existing != null)
                UnityEngine.Object.Destroy(existing);

            EnsureDrawingContainer();

            // Create provider GameObject
            var gizmoGO = new GameObject(label);
            gizmoGO.transform.SetParent(_drawingContainer.transform);
            var provider = gizmoGO.AddComponent<BoundingBoxProvider>();
            provider.Setup(p.game_object_id, p.scale, p.padding);

            _activeDrawings[label] = gizmoGO;

            // Register with Immersive Debugger for in-headset visualization
            try
            {
                var gizmoResult = ImmersiveDebugger.RuntimeAPIs.AddInspectorItemWithGizmo(
                    "Meta XR Operator", "Box", color, label,
                    nameof(BoundingBoxProvider), "BoundingBoxData");
                if (!gizmoResult.IsSuccess)
                    Debug.LogWarning($"[MetaXROperator] Debugger gizmo registration failed: {gizmoResult.Message}");
            }
            catch (Exception ex)
            {
                // Non-fatal — drawing exists even if debugger registration fails
                Debug.LogWarning($"[MetaXROperator] Debugger gizmo registration failed: {ex.Message}");
            }

            var bounds = provider.GetBounds();
            var sb = new StringBuilder();
            sb.AppendLine($"Success: Bounding box drawn for '{obj.name}' (ID: {p.game_object_id}).");
            sb.AppendLine($"Label: {label}");
            sb.AppendLine($"Color: {color}");
            if (provider.Scale != 1f)
                sb.AppendLine($"Scale: {provider.Scale:F2}x");
            if (provider.Padding > 0f)
                sb.AppendLine($"Padding: {provider.Padding:F2}m");
            sb.AppendLine($"Bounds center: ({bounds.center.x:F2}, {bounds.center.y:F2}, {bounds.center.z:F2})");
            sb.AppendLine($"Bounds size: ({bounds.size.x:F2}, {bounds.size.y:F2}, {bounds.size.z:F2})");
            sb.AppendLine();
            sb.Append("Tip: Use unity_debugger_set_visibility true to see the gizmo in headset. " +
                "Use unity_clear_drawings to remove it.");
            return sb.ToString();
        }

        private static void RegisterDrawBoundingBox()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_draw_bounding_box",
                "Draw a persistent bounding box gizmo around a GameObject, visible in the Immersive Debugger. " +
                "Calculates bounds from renderers, then colliders, then children. " +
                "Use unity_clear_drawings to remove.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the target GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "label",
                        Description = "Unique label for this drawing. Default: auto-generated from object name. Used for removal with unity_clear_drawings.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "color",
                        Description = "Gizmo color (red, green, blue, yellow, white, cyan, magenta, or #RRGGBB hex). Default: red.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "scale",
                        Description = "Optional bounds multiplier around the center. Values <= 0 are treated as 1.0.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "padding",
                        Description = "Optional meters added to each side of the box after scaling. Values < 0 are treated as 0.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = false
                    }
                },
                DrawBoundingBox_Callback,
                MainThread
            );
        }

        // --- unity_clear_drawings ---

        [Serializable]
        private struct ClearDrawingsParams
        {
            public string label;
        }

        private static string ClearDrawings_Callback(string parameters)
        {
            ClearDrawingsParams p;
            try { p = JsonUtility.FromJson<ClearDrawingsParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"label\": \"my_label\"} or {} to clear all.\nTip: Use unity_get_drawing_status to see active drawing labels."; }

            if (!string.IsNullOrEmpty(p.label))
            {
                // Clear specific drawing
                if (!_activeDrawings.TryGetValue(p.label, out var gizmo))
                    return $"Error: No drawing with label '{p.label}' found.\n" +
                           "Tip: Use unity_get_drawing_status to see all active drawings.";

                if (gizmo != null)
                    UnityEngine.Object.Destroy(gizmo);
                _activeDrawings.Remove(p.label);
                return $"Success: Removed drawing '{p.label}'.";
            }

            // Clear all drawings
            int count = 0;
            foreach (var kvp in _activeDrawings)
            {
                if (kvp.Value != null)
                {
                    UnityEngine.Object.Destroy(kvp.Value);
                    count++;
                }
            }
            _activeDrawings.Clear();

            if (_drawingContainer != null)
            {
                UnityEngine.Object.Destroy(_drawingContainer);
                _drawingContainer = null;
            }

            return count > 0
                ? $"Success: Cleared {count} drawing(s)."
                : "No active drawings to clear.";
        }

        private static void RegisterClearDrawings()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_clear_drawings",
                "Remove gizmo drawings created by unity_draw_bounding_box. " +
                "Specify a label to remove one, or omit to clear all.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "label",
                        Description = "Label of the specific drawing to remove. Omit to clear all drawings.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    }
                },
                ClearDrawings_Callback,
                MainThread
            );
        }

        // --- unity_get_drawing_status ---

        private static string GetDrawingStatus_Callback(string parameters)
        {
            // Remove stale entries (destroyed GameObjects)
            var stale = new List<string>();
            foreach (var kvp in _activeDrawings)
            {
                if (kvp.Value == null)
                    stale.Add(kvp.Key);
            }
            foreach (var key in stale)
                _activeDrawings.Remove(key);

            if (_activeDrawings.Count == 0)
                return "No active drawings.\n" +
                       "Tip: Use unity_draw_bounding_box to create visual gizmos around GameObjects.";

            var sb = new StringBuilder();
            sb.AppendLine($"Active Drawings ({_activeDrawings.Count}):");
            sb.AppendLine();

            foreach (var kvp in _activeDrawings)
            {
                var provider = kvp.Value.GetComponent<BoundingBoxProvider>();
                if (provider == null)
                {
                    sb.AppendLine($"  {kvp.Key}: [unknown type]");
                    continue;
                }

                bool targetExists = provider.TargetExists;
                var bounds = provider.GetBounds();
                string status = targetExists ? "TRACKING" : "TARGET_LOST";

                sb.AppendLine($"  {kvp.Key}: [{status}]");
                sb.AppendLine($"    Target ID: {provider.TargetInstanceId}");
                if (targetExists)
                {
                    sb.AppendLine($"    Center: ({bounds.center.x:F2}, {bounds.center.y:F2}, {bounds.center.z:F2})");
                    sb.AppendLine($"    Size: ({bounds.size.x:F2}, {bounds.size.y:F2}, {bounds.size.z:F2})");
                }
            }

            return sb.ToString();
        }

        private static void RegisterGetDrawingStatus()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_get_drawing_status",
                "List all active bounding box drawings with their tracking status, position, and bounds size. Use unity_draw_bounding_box to create new drawings.",
                new AgenticToolParameter[] { },
                GetDrawingStatus_Callback,
                MainThread
            );
        }

        // =================================================================
        // Debugger UI panel controls
        // =================================================================

        private static ImmersiveDebugger.UserInterface.DebugInterface FindDebugInterface()
        {
            return UnityEngine.Object.FindFirstObjectByType<
                ImmersiveDebugger.UserInterface.DebugInterface>(FindObjectsInactive.Include);
        }

        private static void RefreshDebugPanelLayout(ImmersiveDebugger.UserInterface.DebugInterface debugInterface)
        {
            var updatePositions = typeof(ImmersiveDebugger.UserInterface.DebugInterface).GetMethod(
                "UpdateDynamicPanelPositions",
                BindingFlags.Instance | BindingFlags.NonPublic);
            updatePositions?.Invoke(debugInterface, null);
        }

        private static void ApplyDebugPanelVisibility(
            ImmersiveDebugger.UserInterface.DebugPanel panel,
            bool visible)
        {
            if (panel.Visibility == visible && panel.gameObject.activeSelf == visible)
            {
                // Already in the requested state; skip to avoid re-firing visibility events + layout.
                return;
            }

            if (panel.Visibility == visible && panel.gameObject.activeSelf != visible)
            {
                // Repair state left by callers that bypassed DebugPanel.Show/Hide.
                if (visible)
                {
                    panel.Hide();
                    panel.Show();
                }
                else
                {
                    panel.Show();
                    panel.Hide();
                }
                return;
            }

            if (visible) panel.Show(); else panel.Hide();
        }

        private static ImmersiveDebugger.UserInterface.DebugPanel FindDebugPanel(string objectName, string title)
        {
            var panels = UnityEngine.Object.FindObjectsByType<ImmersiveDebugger.UserInterface.DebugPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            return panels.FirstOrDefault(panel =>
                string.Equals(panel.name, objectName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(panel.Title, title, StringComparison.OrdinalIgnoreCase));
        }

        // --- unity_toggle_debugger_visibility ---

        private static string ToggleDebuggerVisibility_Callback(string parameters)
        {
            var debugInterface = FindDebugInterface();
            if (debugInterface == null)
                return "Error: Immersive Debugger interface not found.\n" +
                       "Tip: Ensure Immersive Debugger is enabled in Meta XR settings.";

            debugInterface.ToggleVisibility();
            return $"Success: Immersive Debugger is now {(debugInterface.Visibility ? "visible" : "hidden")}.";
        }

        private static void RegisterToggleDebuggerVisibility()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_toggle_debugger_visibility",
                "Toggle the Immersive Debugger in-headset overlay on/off without reading its state first. Use unity_debugger_status to check current state.",
                null,
                ToggleDebuggerVisibility_Callback,
                MainThread
            );
        }

        // --- unity_debugger_set_panel_visibility ---

        [Serializable]
        private struct SetPanelVisibilityParams
        {
            public string panel;
            public bool visible;
        }

        private static string DebuggerSetPanelVisibility_Callback(string parameters)
        {
            SetPanelVisibilityParams p;
            try { p = JsonUtility.FromJson<SetPanelVisibilityParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"panel\": \"inspector\", \"visible\": true}\nTip: panel must be 'inspector' or 'console'."; }

            var debugInterface = FindDebugInterface();
            if (debugInterface == null)
                return "Error: Immersive Debugger interface not found.\n" +
                       "Tip: Ensure Immersive Debugger is enabled in Meta XR settings.";

            string panel = (p.panel ?? "").Trim().ToLowerInvariant();

            if (panel == "inspector")
            {
                var inspector = UnityEngine.Object.FindFirstObjectByType<
                    ImmersiveDebugger.UserInterface.InspectorPanel>(FindObjectsInactive.Include);
                if (inspector == null)
                    return "Error: Inspector panel not found.";
                ApplyDebugPanelVisibility(inspector, p.visible);
                RefreshDebugPanelLayout(debugInterface);
                return $"Success: Inspector panel is now {(p.visible ? "visible" : "hidden")}.";
            }

            if (panel == "console")
            {
                var console = UnityEngine.Object.FindFirstObjectByType<
                    ImmersiveDebugger.UserInterface.Console>(FindObjectsInactive.Include);
                if (console == null)
                    return "Error: Console panel not found.";
                ApplyDebugPanelVisibility(console, p.visible);
                RefreshDebugPanelLayout(debugInterface);
                return $"Success: Console panel is now {(p.visible ? "visible" : "hidden")}.";
            }

            if (panel == "llm" || panel == "llmdialog")
            {
                var llmDialog = FindDebugPanel("llmDialog", "Assistant");
                if (llmDialog == null)
                    return "Error: LLM dialog panel not found.";
                ApplyDebugPanelVisibility(llmDialog, p.visible);
                RefreshDebugPanelLayout(debugInterface);
                return $"Success: LLM dialog panel is now {(p.visible ? "visible" : "hidden")}.";
            }

            return $"Error: Unknown panel '{p.panel}'. Use 'inspector', 'console', or 'llm'.";
        }

        private static void RegisterDebuggerSetPanelVisibility()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_debugger_set_panel_visibility",
                "Show or hide a specific Immersive Debugger panel: 'inspector' (component details / custom inspectors), 'console' (logs), or 'llm' (LLM dialog). Use unity_debugger_set_visibility for the whole overlay.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "panel",
                        Description = "Which panel: 'inspector', 'console', or 'llm'.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "visible",
                        Description = "True to show, false to hide.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = true
                    }
                },
                DebuggerSetPanelVisibility_Callback,
                MainThread
            );
        }

        // --- unity_debugger_set_mode ---

        [Serializable]
        private struct SetModeParams
        {
            public string mode;
        }

        private static string DebuggerSetMode_Callback(string parameters)
        {
            SetModeParams p;
            try { p = JsonUtility.FromJson<SetModeParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"mode\": \"hierarchy\"}\nTip: mode must be 'hierarchy' or 'category'."; }

            string mode = (p.mode ?? "").Trim().ToLowerInvariant();
            if (mode != "hierarchy" && mode != "category")
                return $"Error: Unknown mode '{p.mode}'. Use 'hierarchy' or 'category'.";

            var inspector = UnityEngine.Object.FindFirstObjectByType<
                ImmersiveDebugger.UserInterface.InspectorPanel>(FindObjectsInactive.Include);
            if (inspector == null)
                return "Error: Inspector panel not found.\n" +
                       "Tip: Ensure Immersive Debugger is enabled and the Inspector panel is available.";

            string methodName = mode == "hierarchy" ? "SelectHierarchyMode" : "SelectCategoryMode";
            try
            {
                var method = typeof(ImmersiveDebugger.UserInterface.InspectorPanel).GetMethod(
                    methodName, BindingFlags.NonPublic | BindingFlags.Instance);
                if (method == null)
                    return $"Error: Could not access '{methodName}' on the Inspector panel.";
                method.Invoke(inspector, null);
                return $"Success: Inspector panel switched to {mode} mode.";
            }
            catch (Exception ex)
            {
                return $"Error: Failed to switch to {mode} mode: {ex.Message}";
            }
        }

        private static void RegisterDebuggerSetMode()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_debugger_set_mode",
                "Switch the Immersive Debugger Inspector panel between 'hierarchy' mode (browse scene objects) and 'category' mode (custom inspectors grouped by category).",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "mode",
                        Description = "'hierarchy' or 'category'.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    }
                },
                DebuggerSetMode_Callback,
                MainThread
            );
        }

        // --- unity_debugger_set_follow ---

        [Serializable]
        private struct SetFollowParams
        {
            public bool follow_translation;
            public bool follow_rotation;
        }

        private static string DebuggerSetFollow_Callback(string parameters)
        {
            var debugInterface = FindDebugInterface();
            if (debugInterface == null)
                return "Error: Immersive Debugger interface not found.\n" +
                       "Tip: Ensure Immersive Debugger is enabled in Meta XR settings.";

            // Only apply the fields explicitly present in the JSON (both are optional).
            // Match the JSON key (quoted name + colon), not a bare substring, so a value elsewhere
            // in the payload containing the name can't produce a false positive.
            bool hasTranslation = !string.IsNullOrEmpty(parameters) &&
                System.Text.RegularExpressions.Regex.IsMatch(parameters, "\"follow_translation\"\\s*:");
            bool hasRotation = !string.IsNullOrEmpty(parameters) &&
                System.Text.RegularExpressions.Regex.IsMatch(parameters, "\"follow_rotation\"\\s*:");
            if (!hasTranslation && !hasRotation)
                return "Error: Provide 'follow_translation' and/or 'follow_rotation'.\n" +
                       "Tip: {\"follow_translation\": true, \"follow_rotation\": false}";

            SetFollowParams p;
            try { p = JsonUtility.FromJson<SetFollowParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"follow_translation\": true, \"follow_rotation\": true}"; }

            var applied = new List<string>();
            try
            {
                var type = typeof(ImmersiveDebugger.UserInterface.DebugInterface);
                if (hasTranslation)
                {
                    var prop = type.GetProperty("FollowOverride", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop == null) return "Error: Could not access follow-translation control.";
                    prop.SetValue(debugInterface, p.follow_translation);
                    applied.Add($"translation={(p.follow_translation ? "on" : "off")}");
                }
                if (hasRotation)
                {
                    var prop = type.GetProperty("RotateOverride", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (prop == null) return "Error: Could not access follow-rotation control.";
                    prop.SetValue(debugInterface, p.follow_rotation);
                    applied.Add($"rotation={(p.follow_rotation ? "on" : "off")}");
                }
            }
            catch (Exception ex)
            {
                return $"Error: Failed to set follow behavior: {ex.Message}";
            }

            return $"Success: Immersive Debugger follow updated ({string.Join(", ", applied)}).";
        }

        private static void RegisterDebuggerSetFollow()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_debugger_set_follow",
                "Configure whether the Immersive Debugger panels follow the user's head. Set follow_translation (position) and/or follow_rotation independently; omit a field to leave it unchanged.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "follow_translation",
                        Description = "True to make panels follow head position. Omit to leave unchanged.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = false
                    },
                    new AgenticToolParameter
                    {
                        Name = "follow_rotation",
                        Description = "True to make panels follow head rotation. Omit to leave unchanged.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Boolean,
                        IsRequired = false
                    }
                },
                DebuggerSetFollow_Callback,
                MainThread
            );
        }

        // =================================================================
        // Scene mutation: component add / remove
        // =================================================================

        [Serializable]
        private struct AddComponentParams
        {
            public long game_object_id;
            public string component_type;
        }

        private static string AddComponent_Callback(string parameters)
        {
            AddComponentParams p;
            try { p = JsonUtility.FromJson<AddComponentParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345, \"component_type\": \"UnityEngine.BoxCollider\"}\nTip: Use the full type name including namespace."; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            if (string.IsNullOrEmpty(p.component_type))
                return "Error: component_type is required.\n" +
                       "Tip: Use the full type name, e.g. 'UnityEngine.Rigidbody'.";

            var type = SceneQueryHelper.ResolveType(p.component_type);
            if (type == null)
                return $"Error: Type '{p.component_type}' not found.\n" +
                       "Tip: Use the full name with namespace (e.g. 'UnityEngine.BoxCollider'). Only types deriving from Component can be added.";

            if (!typeof(Component).IsAssignableFrom(type))
                return $"Error: '{p.component_type}' is not a Component type and cannot be added to a GameObject.";

            try
            {
                var comp = obj.AddComponent(type);
                if (comp == null)
                    return $"Error: Failed to add '{type.Name}' to '{obj.name}'. It may be abstract or conflict with an existing component.";
                var sb = new StringBuilder();
                sb.AppendLine($"Success: Added {type.Name} to '{obj.name}' (ID: {p.game_object_id}).");
                AppendInactiveWarning(sb, obj);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: Failed to add component '{p.component_type}': {ex.Message}";
            }
        }

        private static void RegisterAddComponent()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_add_component",
                "Add a component of a given type to a GameObject at runtime. Use the full type name with namespace (e.g. 'UnityEngine.BoxCollider').",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "component_type",
                        Description = "Full component type name including namespace (e.g. 'UnityEngine.Rigidbody').",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    }
                },
                AddComponent_Callback,
                MainThread
            );
        }

        // --- unity_remove_component ---

        [Serializable]
        private struct RemoveComponentParams
        {
            public long game_object_id;
            public string component_name;
        }

        private static string RemoveComponent_Callback(string parameters)
        {
            RemoveComponentParams p;
            try { p = JsonUtility.FromJson<RemoveComponentParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345, \"component_name\": \"BoxCollider\"}"; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            var comp = SceneQueryHelper.FindComponent(obj, p.component_name);
            if (comp == null)
                return ComponentNotFoundError(obj, p.component_name);

            if (comp is Transform)
                return "Error: The Transform component cannot be removed.";

            try
            {
                string removed = comp.GetType().Name;
                UnityEngine.Object.Destroy(comp);
                return $"Success: Removed {removed} from '{obj.name}' (ID: {p.game_object_id}).";
            }
            catch (Exception ex)
            {
                return $"Error: Failed to remove component '{p.component_name}': {ex.Message}\n" +
                       "Tip: Some components are required by others (e.g. a Collider needed by a Joint) and cannot be removed while depended upon.";
            }
        }

        private static void RegisterRemoveComponent()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_remove_component",
                "Remove a component from a GameObject at runtime by type name. The Transform cannot be removed.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "component_name",
                        Description = "Component type name to remove (e.g. 'BoxCollider').",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    }
                },
                RemoveComponent_Callback,
                MainThread
            );
        }

        // =================================================================
        // unity_duplicate_game_object
        // =================================================================

        [Serializable]
        private struct DuplicateGameObjectParams
        {
            public int game_object_id;
        }

        private static string DuplicateGameObject_Callback(string parameters)
        {
            DuplicateGameObjectParams p;
            try { p = JsonUtility.FromJson<DuplicateGameObjectParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345}\nTip: Use the instance ID from unity_search_game_objects."; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            if (IsImmersiveDebuggerInfrastructure(obj))
                return $"Error: '{obj.name}' (ID: {p.game_object_id}) is part of the Immersive Debugger and cannot be duplicated.\n" +
                       "Tip: Cloning it would spawn a second AI Assistant binder that overwrites the shared root and destabilizes the tools.";

            try
            {
                // instantiateInWorldSpace: false keeps the copy's local transform, so it overlaps
                // the original under the same parent (the expected "duplicate" behavior).
                var clone = UnityEngine.Object.Instantiate(obj, obj.transform.parent, false);
                var sb = new StringBuilder();
                sb.AppendLine($"Success: Duplicated '{obj.name}' (ID: {p.game_object_id}) → '{clone.name}' (ID: {SceneQueryHelper.GetObjectId(clone)}).");
                sb.AppendLine($"Tip: Use unity_inspect_game_object with ID {SceneQueryHelper.GetObjectId(clone)} to work with the copy.");
                AppendInactiveWarning(sb, clone);
                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: Failed to duplicate '{obj.name}': {ex.Message}";
            }
        }

        private static void RegisterDuplicateGameObject()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_duplicate_game_object",
                "Duplicate a GameObject at runtime (like Instantiate). The copy is placed under the same parent overlapping the original. Returns the new GameObject's instance ID. Use unity_search_game_objects to find the source object ID first. The Immersive Debugger's own objects are protected.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject to duplicate.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    }
                },
                DuplicateGameObject_Callback,
                MainThread
            );
        }

        // =================================================================
        // unity_destroy_game_object
        // =================================================================

        [Serializable]
        private struct DestroyGameObjectParams
        {
            public int game_object_id;
        }

        private static string DestroyGameObject_Callback(string parameters)
        {
            DestroyGameObjectParams p;
            try { p = JsonUtility.FromJson<DestroyGameObjectParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"game_object_id\": 12345}\nTip: Use the instance ID from unity_search_game_objects."; }

            var obj = SceneQueryHelper.FindGameObjectById(p.game_object_id);
            if (obj == null)
                return GameObjectNotFoundError(p.game_object_id);

            if (IsImmersiveDebuggerInfrastructure(obj))
                return $"Error: '{obj.name}' (ID: {p.game_object_id}) is part of the Immersive Debugger and cannot be destroyed.\n" +
                       "Tip: Destroying it would tear down the AI Assistant and its tools.";

            string name = obj.name;
            try
            {
                UnityEngine.Object.Destroy(obj);
                return $"Success: Destroyed '{name}' (ID: {p.game_object_id}).";
            }
            catch (Exception ex)
            {
                return $"Error: Failed to destroy '{name}': {ex.Message}";
            }
        }

        private static void RegisterDestroyGameObject()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_destroy_game_object",
                "Destroy a GameObject at runtime (like Destroy). This removes it and all its children from the scene. Use unity_search_game_objects to find the object ID first. The Immersive Debugger's own objects are protected.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "game_object_id",
                        Description = "Unity instance ID of the GameObject to destroy.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.Number,
                        IsRequired = true
                    }
                },
                DestroyGameObject_Callback,
                MainThread
            );
        }

        /// <summary>
        /// True if the GameObject is the Immersive Debugger host, a descendant of it, or an
        /// ancestor of it — destroying or duplicating any of those tears down or clones the
        /// debugger subtree.
        /// </summary>
        private static bool IsImmersiveDebuggerInfrastructure(GameObject obj)
        {
            if (_debuggerRoot == null || obj == null)
                return false;

            // IsChildOf returns true for self, so this covers the root, its descendants (target
            // under the root), and its ancestors (root under the target) in one check.
            var objTransform = obj.transform;
            var rootTransform = _debuggerRoot.transform;
            return objTransform.IsChildOf(rootTransform) || rootTransform.IsChildOf(objTransform);
        }

        private void OnDestroy()
        {
            // Clear the static host reference when this binder is torn down (e.g. scene unload) so a
            // later binder re-establishes it and the destroy/duplicate guard never consults a stale object.
            if (_debuggerRoot == gameObject)
            {
                _debuggerRoot = null;
            }
        }

        // =================================================================
        // Diagnostics: write a console log line
        // =================================================================

        [Serializable]
        private struct DebugLogParams
        {
            public string message;
            public string level;
        }

        private static string DebugLog_Callback(string parameters)
        {
            DebugLogParams p;
            try { p = JsonUtility.FromJson<DebugLogParams>(parameters); }
            catch { return "Error: Invalid parameters. Expected: {\"message\": \"checkpoint reached\"}\nTip: Optional 'level' is 'info', 'warning', or 'error'."; }

            if (string.IsNullOrEmpty(p.message))
                return "Error: message is required.";

            string level = (p.level ?? "info").Trim().ToLowerInvariant();
            switch (level)
            {
                case "warning": Debug.LogWarning(p.message); break;
                case "error": Debug.LogError(p.message); break;
                default: level = "info"; Debug.Log(p.message); break;
            }
            return $"Success: Wrote {level} message to the Unity console. Retrieve it via unity_get_recent_logs.";
        }

        private static void RegisterDebugLog()
        {
            MetaXROperatorExternalTool.RegisterAgenticTool(
                "unity_debug_log",
                "Write a message to the Unity console (Debug.Log / LogWarning / LogError). Useful to mark checkpoints; retrievable via unity_get_recent_logs.",
                new AgenticToolParameter[]
                {
                    new AgenticToolParameter
                    {
                        Name = "message",
                        Description = "The message to log.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = true
                    },
                    new AgenticToolParameter
                    {
                        Name = "level",
                        Description = "Log level: 'info' (default), 'warning', or 'error'.",
                        ParamType = XrAgenticExternalToolParameterTypeMETAX1.String,
                        IsRequired = false
                    }
                },
                DebugLog_Callback,
                MainThread
            );
        }
    }

    /// <summary>
    /// Lightweight MonoBehaviour that tracks a target GameObject and provides its bounding box data
    /// for the Immersive Debugger gizmo system. Uses instance ID lookup via SceneQueryHelper
    /// for efficient, cached access.
    ///
    /// Bounds calculation priority: direct Renderer → direct Collider → children → 1x1x1 fallback.
    /// </summary>
    internal class BoundingBoxProvider : MonoBehaviour
    {
        private long _targetId;
        private float _scale = 1f;
        private float _padding;
        private GameObject _cachedTarget;
        private Bounds _cachedBounds;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private Vector3 _lastScale;
        private bool _boundsValid;

        public long TargetInstanceId => _targetId;
        public float Scale => _scale;
        public float Padding => _padding;

        public void Setup(long targetInstanceId, float scale = 1f, float padding = 0f)
        {
            _targetId = targetInstanceId;
            _scale = scale > 0f ? scale : 1f;
            _padding = Mathf.Max(0f, padding);
            RefreshTarget();
        }

        public bool TargetExists
        {
            get
            {
                if (_cachedTarget == null)
                    RefreshTarget();
                return _cachedTarget != null;
            }
        }

        /// <summary>
        /// Returns bounding box data in the format expected by Immersive Debugger's Box gizmo:
        /// Tuple(Pose, width, height, depth).
        /// </summary>
        public Tuple<Pose, float, float, float> BoundingBoxData
        {
            get
            {
                var bounds = GetBounds();
                if (_cachedTarget == null)
                    return new Tuple<Pose, float, float, float>(Pose.identity, 1f, 1f, 1f);

                var pose = new Pose(bounds.center, _cachedTarget.transform.rotation);
                return new Tuple<Pose, float, float, float>(
                    pose, bounds.size.x, bounds.size.y, bounds.size.z);
            }
        }

        public Bounds GetBounds()
        {
            if (_cachedTarget == null)
                RefreshTarget();
            if (_cachedTarget == null)
                return new Bounds(Vector3.zero, Vector3.one);

            if (!_boundsValid || HasTransformChanged())
            {
                _cachedBounds = ApplyScaleAndPadding(CalculateBounds(_cachedTarget));
                _boundsValid = true;
                _lastPosition = _cachedTarget.transform.position;
                _lastRotation = _cachedTarget.transform.rotation;
                _lastScale = _cachedTarget.transform.localScale;
            }
            return _cachedBounds;
        }

        private void RefreshTarget()
        {
            _cachedTarget = SceneQueryHelper.FindGameObjectById(_targetId);
            _boundsValid = false;
        }

        private bool HasTransformChanged()
        {
            if (_cachedTarget == null) return false;
            return _cachedTarget.transform.position != _lastPosition
                || _cachedTarget.transform.rotation != _lastRotation
                || _cachedTarget.transform.localScale != _lastScale;
        }

        /// <summary>
        /// Calculate bounds using priority: direct Renderer → direct Collider → children → fallback.
        /// </summary>
        private static Bounds CalculateBounds(GameObject target)
        {
            // Try direct renderer first
            var renderer = target.GetComponent<Renderer>();
            if (renderer != null && renderer.bounds.size.sqrMagnitude > 0f)
                return renderer.bounds;

            // Try direct collider
            var collider = target.GetComponent<Collider>();
            if (collider != null)
                return collider.bounds;

            // Try children renderers
            var childRenderers = target.GetComponentsInChildren<Renderer>(true);
            if (childRenderers.Length > 0)
            {
                Bounds combined = childRenderers[0].bounds;
                for (int i = 1; i < childRenderers.Length; i++)
                {
                    if (childRenderers[i].bounds.size.sqrMagnitude > 0f)
                        combined.Encapsulate(childRenderers[i].bounds);
                }
                if (combined.size.sqrMagnitude > 0f)
                    return combined;
            }

            // Try children colliders
            var childColliders = target.GetComponentsInChildren<Collider>(true);
            if (childColliders.Length > 0)
            {
                Bounds combined = childColliders[0].bounds;
                for (int i = 1; i < childColliders.Length; i++)
                    combined.Encapsulate(childColliders[i].bounds);
                return combined;
            }

            // Fallback: 1x1x1 box at object position
            return new Bounds(target.transform.position, Vector3.one);
        }

        private Bounds ApplyScaleAndPadding(Bounds bounds)
        {
            var size = bounds.size * _scale;
            if (_padding > 0f)
                size += Vector3.one * (_padding * 2f);
            bounds.size = size;
            return bounds;
        }
    }
}

#endif // USING_XR_SDK_OPENXR
