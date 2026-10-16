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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Meta.XR.ImmersiveDebugger.XROperator
{
    /// <summary>
    /// Shared helper for scene traversal, GameObject lookup, component inspection,
    /// and type conversion. Used by ImmersiveDebuggerBinder tool callbacks.
    /// </summary>
    internal static class SceneQueryHelper
    {
        private const BindingFlags MemberFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.GetField | BindingFlags.GetProperty;

        /// <summary>Max output size in characters before truncation.</summary>
        private const int MaxOutputChars = 16000;

        /// <summary>Max time in milliseconds for recursive operations before truncation.</summary>
        private const long MaxOperationTimeMs = 4000;

        // -----------------------------------------------------------------
        // Instance ID cache
        // -----------------------------------------------------------------

        private static readonly Dictionary<long, WeakReference<GameObject>> _cache =
            new Dictionary<long, WeakReference<GameObject>>();
        private static bool _sceneEventsRegistered;

        /// <summary>Object identity as exposed to the agent through the tool JSON.</summary>
        /// <remarks>
        /// Unity replaced instance IDs with the 64-bit <c>EntityId</c>, so IDs are carried as
        /// <c>long</c>: the <c>EntityId</c>-to-<c>int</c> cast is deprecated alongside
        /// <c>GetInstanceID()</c> itself. The guard is 6000.4 rather than 6000.5 because
        /// <c>GetInstanceID()</c> is already deprecated there, and 6000.4 is the first version
        /// exposing <c>EntityId.ToULong</c>.
        /// </remarks>
        internal static long GetObjectId(UnityEngine.Object obj)
        {
#if UNITY_6000_4_OR_NEWER
            return unchecked((long)EntityId.ToULong(obj.GetEntityId()));
#else
            return obj.GetInstanceID();
#endif
        }

        /// <summary>
        /// Register scene load/unload handlers to invalidate the cache.
        /// Safe to call multiple times — only registers once.
        /// </summary>
        public static void InitializeCache()
        {
            if (_sceneEventsRegistered) return;
            _sceneEventsRegistered = true;
            SceneManager.sceneLoaded += (scene, mode) => ClearCache();
            SceneManager.sceneUnloaded += (scene) => ClearCache();
        }

        /// <summary>
        /// Clear the instance ID to GameObject cache.
        /// </summary>
        public static void ClearCache()
        {
            _cache.Clear();
        }

        /// <summary>
        /// Add a GameObject to the cache. Called during search and hierarchy operations.
        /// </summary>
        private static void CacheObject(GameObject obj)
        {
            if (obj != null)
                _cache[GetObjectId(obj)] = new WeakReference<GameObject>(obj);
        }

        // -----------------------------------------------------------------
        // GameObject lookup
        // -----------------------------------------------------------------

        /// <summary>
        /// Find a GameObject by its Unity instance ID. Checks cache first,
        /// then falls back to full scene traversal.
        /// </summary>
        public static GameObject FindGameObjectById(long instanceId)
        {
            // Check cache first
            if (_cache.TryGetValue(instanceId, out var weakRef) &&
                weakRef.TryGetTarget(out var cached) && cached != null)
            {
                return cached;
            }

            // Cache miss — full traversal
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    var result = FindByIdRecursive(root, instanceId);
                    if (result != null)
                    {
                        CacheObject(result);
                        return result;
                    }
                }
            }
            // Remove stale entry if it existed
            _cache.Remove(instanceId);
            return null;
        }

        private static GameObject FindByIdRecursive(GameObject obj, long instanceId)
        {
            if (GetObjectId(obj) == instanceId)
                return obj;

            foreach (Transform child in obj.transform)
            {
                var result = FindByIdRecursive(child.gameObject, instanceId);
                if (result != null) return result;
            }
            return null;
        }

        // -----------------------------------------------------------------
        // Scene search
        // -----------------------------------------------------------------

        /// <summary>
        /// Search all loaded scenes for GameObjects whose names match the pattern.
        /// Stops at maxResults to prevent unbounded output.
        /// </summary>
        public static List<GameObject> SearchGameObjects(
            string pattern, bool exactMatch, bool includeInactive, int maxResults = 50)
        {
            var results = new List<GameObject>();
            int totalMatches = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    SearchRecursive(root, pattern, results, exactMatch, includeInactive,
                        maxResults, ref totalMatches);
                }
            }
            return results;
        }

        /// <summary>Returns true if results were truncated (more matches exist).</summary>
        public static int LastSearchTotalMatches { get; private set; }

        private static void SearchRecursive(
            GameObject obj, string pattern, List<GameObject> results,
            bool exactMatch, bool includeInactive, int maxResults, ref int totalMatches)
        {
            if (!includeInactive && !obj.activeSelf) return;

            bool matches = exactMatch
                ? obj.name.Equals(pattern, StringComparison.OrdinalIgnoreCase)
                : obj.name.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;

            if (matches)
            {
                totalMatches++;
                LastSearchTotalMatches = totalMatches;
                CacheObject(obj);
                if (results.Count < maxResults)
                    results.Add(obj);
            }

            foreach (Transform child in obj.transform)
                SearchRecursive(child.gameObject, pattern, results, exactMatch, includeInactive,
                    maxResults, ref totalMatches);
        }

        /// <summary>
        /// Find all GameObjects that have a component whose type name contains the query.
        /// Stops at maxResults.
        /// </summary>
        public static List<GameObject> FindByComponent(
            string componentTypeName, bool includeInactive, int maxResults = 50)
        {
            var results = new List<GameObject>();
            int totalMatches = 0;
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                foreach (var root in scene.GetRootGameObjects())
                {
                    FindByComponentRecursive(root, componentTypeName, results, includeInactive,
                        maxResults, ref totalMatches);
                }
            }
            LastSearchTotalMatches = totalMatches;
            return results;
        }

        private static void FindByComponentRecursive(
            GameObject obj, string typeName, List<GameObject> results,
            bool includeInactive, int maxResults, ref int totalMatches)
        {
            if (!includeInactive && !obj.activeSelf) return;

            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp != null &&
                    comp.GetType().Name.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    totalMatches++;
                    CacheObject(obj);
                    if (results.Count < maxResults)
                        results.Add(obj);
                    break;
                }
            }

            foreach (Transform child in obj.transform)
                FindByComponentRecursive(child.gameObject, typeName, results, includeInactive,
                    maxResults, ref totalMatches);
        }

        // -----------------------------------------------------------------
        // Scene hierarchy
        // -----------------------------------------------------------------

        /// <summary>
        /// Build a text representation of the scene hierarchy with instance IDs.
        /// Bounded by maxDepth and output size limits.
        /// </summary>
        public static string BuildSceneHierarchy(int maxDepth = 3, long rootObjectId = 0)
        {
            var sb = new StringBuilder();
            var sw = Stopwatch.StartNew();
            int totalObjects = 0;
            int renderedObjects = 0;
            bool truncated = false;

            if (rootObjectId != 0)
            {
                var root = FindGameObjectById(rootObjectId);
                if (root == null)
                {
                    return $"Error: GameObject with ID {rootObjectId} not found.\n" +
                           "Tip: Use unity_search_game_objects to find valid IDs.";
                }
                sb.AppendLine($"Hierarchy under: {root.name} (ID: {rootObjectId})");
                sb.AppendLine();
                AppendHierarchy(root, 0, sb, maxDepth, sw, ref totalObjects,
                    ref renderedObjects, ref truncated);
            }
            else
            {
                sb.AppendLine("Scene Hierarchy:");
                sb.AppendLine();

                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    var scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    sb.AppendLine($"Scene: {scene.name}");
                    foreach (var root in scene.GetRootGameObjects())
                    {
                        AppendHierarchy(root, 1, sb, maxDepth, sw, ref totalObjects,
                            ref renderedObjects, ref truncated);
                        if (truncated) break;
                    }
                    if (truncated) break;
                    sb.AppendLine();
                }
            }

            if (truncated || totalObjects > renderedObjects)
            {
                sb.AppendLine();
                sb.AppendLine($"... ({totalObjects - renderedObjects} more objects not shown)");
                sb.AppendLine($"Tip: Use root_object_id to explore a specific subtree, " +
                    "or increase max_depth for deeper traversal.");
            }

            sb.AppendLine();
            sb.AppendLine($"Showing {renderedObjects} of {totalObjects} objects (max_depth={maxDepth}).");
            return sb.ToString();
        }

        private static void AppendHierarchy(
            GameObject obj, int depth, StringBuilder sb, int maxDepth,
            Stopwatch sw, ref int totalObjects, ref int renderedObjects, ref bool truncated)
        {
            totalObjects++;

            if (truncated || sw.ElapsedMilliseconds > MaxOperationTimeMs ||
                sb.Length > MaxOutputChars)
            {
                truncated = true;
                return;
            }

            string indent = new string(' ', depth * 2);
            string activeMarker = obj.activeSelf ? "" : " [INACTIVE]";
            sb.AppendLine($"{indent}- {obj.name} (ID: {GetObjectId(obj)}){activeMarker}");
            renderedObjects++;
            CacheObject(obj);

            if (depth >= maxDepth)
            {
                int childCount = obj.transform.childCount;
                if (childCount > 0)
                {
                    sb.AppendLine($"{indent}  ... ({childCount} children, increase max_depth to see)");
                    totalObjects += childCount;
                }
                return;
            }

            foreach (Transform child in obj.transform)
                AppendHierarchy(child.gameObject, depth + 1, sb, maxDepth, sw,
                    ref totalObjects, ref renderedObjects, ref truncated);
        }

        // -----------------------------------------------------------------
        // Component helpers
        // -----------------------------------------------------------------

        /// <summary>
        /// Find a component on a GameObject by type name (case-insensitive).
        /// </summary>
        public static Component FindComponent(GameObject obj, string componentName)
        {
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp != null &&
                    comp.GetType().Name.Equals(componentName, StringComparison.OrdinalIgnoreCase))
                    return comp;
            }
            return null;
        }

        /// <summary>
        /// Find the closest matching component name (for typo suggestions).
        /// Returns null if no close match is found.
        /// </summary>
        public static string FindClosestComponentName(GameObject obj, string query)
        {
            string bestMatch = null;
            int bestDistance = 3; // max edit distance threshold
            foreach (var comp in obj.GetComponents<Component>())
            {
                if (comp == null) continue;
                string name = comp.GetType().Name;
                int dist = ComputeEditDistance(query.ToLower(), name.ToLower());
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestMatch = name;
                }
            }
            return bestMatch;
        }

        /// <summary>
        /// Simple Levenshtein edit distance for typo detection.
        /// </summary>
        private static int ComputeEditDistance(string a, string b)
        {
            if (a.Length > 20 || b.Length > 20) return 99; // skip long strings
            int[,] d = new int[a.Length + 1, b.Length + 1];
            for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
            for (int j = 0; j <= b.Length; j++) d[0, j] = j;
            for (int i = 1; i <= a.Length; i++)
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            return d[a.Length, b.Length];
        }

        /// <summary>
        /// Find a member (field or property) on a type by name (case-insensitive).
        /// </summary>
        public static MemberInfo FindMember(Type type, string memberName)
        {
            foreach (var member in type.GetMembers(MemberFlags))
            {
                if (member.Name.Equals(memberName, StringComparison.OrdinalIgnoreCase))
                    return member;
            }
            return null;
        }

        /// <summary>
        /// Resolve a Type by name across loaded assemblies. Accepts a full name
        /// (with namespace, preferred) or a simple name; returns the first match, or null.
        /// </summary>
        public static Type ResolveType(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            // Direct resolution (handles assembly-qualified and mscorlib names).
            var direct = Type.GetType(typeName, false, true);
            if (direct != null) return direct;

            // Full-name match in each loaded assembly.
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(typeName, false, true);
                if (t != null) return t;
            }

            // Fall back to a simple-name (last path segment) match. Accept it only when unambiguous;
            // multiple types sharing a simple name would otherwise resolve non-deterministically to
            // the wrong type. On ambiguity return null so the caller asks for the full type name.
            string simple = typeName;
            int dot = simple.LastIndexOf('.');
            if (dot >= 0) simple = simple.Substring(dot + 1);

            Type match = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e) { types = e.Types; }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t == null || !t.Name.Equals(simple, StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (match != null && match != t)
                        return null;
                    match = t;
                }
            }
            return match;
        }

        /// <summary>
        /// Get the full hierarchy path of a GameObject (e.g. "Canvas/Panel/Button").
        /// Uses a stack to avoid O(n²) string concatenation.
        /// </summary>
        public static string GetGameObjectPath(GameObject obj)
        {
            if (obj == null) return "";
            // Collect names bottom-up, then reverse
            var parts = new Stack<string>();
            var current = obj.transform;
            while (current != null)
            {
                parts.Push(current.name);
                current = current.parent;
            }
            var sb = new StringBuilder();
            bool first = true;
            foreach (var part in parts)
            {
                if (!first) sb.Append('/');
                sb.Append(part);
                first = false;
            }
            return sb.ToString();
        }

        /// <summary>
        /// Returns a short enabled status string for a component.
        /// </summary>
        public static string GetComponentEnabledStatus(Component component)
        {
            if (component == null) return "[NULL]";
            var prop = component.GetType().GetProperty("enabled",
                BindingFlags.Public | BindingFlags.Instance);
            if (prop != null && prop.PropertyType == typeof(bool))
            {
                try
                {
                    return (bool)prop.GetValue(component) ? "[ENABLED]" : "[DISABLED]";
                }
                catch
                {
                    return "[UNKNOWN]";
                }
            }
            return "[ALWAYS ACTIVE]";
        }

        // -----------------------------------------------------------------
        // Reflection value access
        // -----------------------------------------------------------------

        /// <summary>
        /// Safely read the value of a MemberInfo (field or property) on a component.
        /// </summary>
        public static object GetMemberValue(MemberInfo member, object instance)
        {
            try
            {
                if (member is FieldInfo fi)
                    return fi.GetValue(instance) ?? "null";
                if (member is PropertyInfo pi && pi.CanRead)
                    return pi.GetValue(instance) ?? "null";
                return "[unreadable]";
            }
            catch (Exception ex)
            {
                return $"[Error: {ex.Message}]";
            }
        }

        /// <summary>
        /// Get the data type of a MemberInfo.
        /// </summary>
        public static Type GetMemberType(MemberInfo member)
        {
            if (member is FieldInfo fi) return fi.FieldType;
            if (member is PropertyInfo pi) return pi.PropertyType;
            return null;
        }

        /// <summary>
        /// Check whether a MemberInfo is a settable field or writable property.
        /// </summary>
        public static bool CanBeChanged(MemberInfo member)
        {
            if (member is FieldInfo fi) return !fi.IsInitOnly && !fi.IsLiteral;
            if (member is PropertyInfo pi) return pi.CanWrite;
            return false;
        }

        /// <summary>
        /// Set the value of a MemberInfo (field or writable property).
        /// Throws on failure with a descriptive message.
        /// </summary>
        public static void SetMemberValue(MemberInfo member, object instance, object value)
        {
            try
            {
                if (member is FieldInfo fi)
                    fi.SetValue(instance, value);
                else if (member is PropertyInfo pi)
                    pi.SetValue(instance, value);
            }
            catch (TargetInvocationException ex)
            {
                // Unwrap the inner exception for a clearer error message
                throw ex.InnerException ?? ex;
            }
        }

        // -----------------------------------------------------------------
        // Type conversion
        // -----------------------------------------------------------------

        /// <summary>
        /// Convert a string value to the target type. Supports common Unity types.
        /// All numeric parsing uses InvariantCulture for cross-locale reliability.
        /// </summary>
        public static object ConvertStringToType(string value, Type targetType)
        {
            if (targetType == typeof(string)) return value;
            if (targetType == typeof(int)) return int.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(float)) return float.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return double.Parse(value, CultureInfo.InvariantCulture);
            if (targetType == typeof(bool)) return bool.Parse(value);
            if (targetType == typeof(long)) return long.Parse(value, CultureInfo.InvariantCulture);

            if (targetType == typeof(Vector3))
            {
                string clean = value.Trim('(', ')');
                string[] parts = clean.Split(',');
                if (parts.Length == 3)
                    return new Vector3(
                        float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture));
                throw new FormatException($"Vector3 requires 3 components, got {parts.Length}");
            }

            if (targetType == typeof(Vector2))
            {
                string clean = value.Trim('(', ')');
                string[] parts = clean.Split(',');
                if (parts.Length == 2)
                    return new Vector2(
                        float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture));
                throw new FormatException($"Vector2 requires 2 components, got {parts.Length}");
            }

            if (targetType == typeof(Quaternion))
            {
                string clean = value.Trim('(', ')');
                string[] parts = clean.Split(',');
                if (parts.Length == 4)
                    return new Quaternion(
                        float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture));
                if (parts.Length == 3)
                    return Quaternion.Euler(
                        float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture));
                throw new FormatException($"Quaternion requires 4 components (x,y,z,w) or 3 Euler angles (x,y,z), got {parts.Length}");
            }

            if (targetType == typeof(LayerMask))
            {
                if (int.TryParse(value, out int layerValue))
                    return (LayerMask)layerValue;
                throw new FormatException($"LayerMask requires an integer layer value, got '{value}'");
            }

            if (targetType == typeof(Rect))
            {
                string clean = value.Trim('(', ')');
                string[] parts = clean.Split(',');
                if (parts.Length == 4)
                    return new Rect(
                        float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture));
                throw new FormatException($"Rect requires 4 components (x, y, width, height), got {parts.Length}");
            }

            if (targetType == typeof(Bounds))
            {
                string clean = value.Trim('(', ')');
                string[] parts = clean.Split(',');
                if (parts.Length == 6)
                    return new Bounds(
                        new Vector3(
                            float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture)),
                        new Vector3(
                            float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(parts[4].Trim(), CultureInfo.InvariantCulture),
                            float.Parse(parts[5].Trim(), CultureInfo.InvariantCulture)));
                throw new FormatException($"Bounds requires 6 components (centerX, centerY, centerZ, sizeX, sizeY, sizeZ), got {parts.Length}");
            }

            if (targetType == typeof(Vector4))
            {
                string clean = value.Trim('(', ')');
                string[] parts = clean.Split(',');
                if (parts.Length == 4)
                    return new Vector4(
                        float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture));
                throw new FormatException($"Vector4 requires 4 components, got {parts.Length}");
            }

            if (targetType == typeof(Color))
            {
                string clean = value.Trim('(', ')');
                string[] parts = clean.Split(',');
                if (parts.Length == 4)
                    return new Color(
                        float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture));
                if (parts.Length == 3)
                    return new Color(
                        float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                        float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                        1.0f);
                throw new FormatException($"Color requires 3 or 4 components, got {parts.Length}");
            }

            if (targetType.IsEnum)
                return Enum.Parse(targetType, value, ignoreCase: true);

            if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                return ResolveObjectArgument(value, targetType);

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Resolve a scene object passed by its GameObject instance ID (the same ID returned by
        /// unity_search_game_objects) to a live <see cref="UnityEngine.Object"/> assignable to
        /// <paramref name="targetType"/>. Enables passing objects as method/property arguments. For a
        /// component-typed parameter, the component of that type on the GameObject is returned.
        /// Throws <see cref="FormatException"/> if the value is not an integer ID, no GameObject with
        /// that ID is loaded, or the GameObject cannot satisfy <paramref name="targetType"/>.
        /// </summary>
        public static UnityEngine.Object ResolveObjectArgument(string value, Type targetType)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int instanceId))
                throw new FormatException(
                    $"Expected an integer instance ID for the {targetType.Name} argument, got '{value}'.");

            var go = FindGameObjectById(instanceId);
            if (go == null)
                throw new FormatException($"No loaded GameObject has instance ID {instanceId}.");

            if (targetType.IsInstanceOfType(go))
                return go;

            // Let a GameObject's ID satisfy a component-typed parameter (e.g. Transform, Rigidbody).
            if (typeof(Component).IsAssignableFrom(targetType))
            {
                var comp = go.GetComponent(targetType);
                if (comp != null)
                    return comp;
            }

            throw new FormatException(
                $"GameObject with instance ID {instanceId} has no {targetType.Name} to pass as the argument.");
        }

        // -----------------------------------------------------------------
        // Display formatting
        // -----------------------------------------------------------------

        /// <summary>
        /// Format a value for human-readable display in tool results.
        /// </summary>
        public static string FormatValue(object value)
        {
            if (value == null) return "null";
            if (value is string s) return $"\"{s}\"";
            if (value is Vector3 v3) return $"({v3.x:F2}, {v3.y:F2}, {v3.z:F2})";
            if (value is Vector2 v2) return $"({v2.x:F2}, {v2.y:F2})";
            if (value is Quaternion q) return $"({q.x:F2}, {q.y:F2}, {q.z:F2}, {q.w:F2})";
            if (value is Color c) return $"RGBA({c.r:F2}, {c.g:F2}, {c.b:F2}, {c.a:F2})";
            if (value is float f) return f.ToString("F2");
            if (value is double d) return d.ToString("F2");
            return value.ToString();
        }
    }
}

#endif // USING_XR_SDK_OPENXR
