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
using System.Text;
using System.Threading;
using UnityEngine;

namespace Meta.XR.ImmersiveDebugger.XROperator
{
    /// <summary>
    /// Captures Unity console logs into a thread-safe ring buffer for agent access.
    /// Subscribes to Application.logMessageReceivedThreaded at runtime initialization
    /// (before any scene loads) to ensure no early logs are missed.
    /// </summary>
    internal static class LogCaptureService
    {
        /// <summary>Maximum number of log entries to retain in the ring buffer.</summary>
        private const int MaxBufferSize = 200;

        /// <summary>Maximum length of a single log message before truncation.</summary>
        private const int MaxMessageLength = 500;

        /// <summary>Maximum length of a stack trace before truncation.</summary>
        private const int MaxStackTraceLength = 300;

        /// <summary>A single captured log entry.</summary>
        internal struct LogEntry
        {
            public string Message;
            public string StackTrace;
            public LogType Type;
            /// <summary>Elapsed seconds since service initialization (thread-safe monotonic clock).</summary>
            public double TimestampSeconds;
            public int DuplicateCount;
            /// <summary>Pre-computed dedup key (hash of full message + type, computed before truncation).</summary>
            internal int DedupKey;
        }

        private static readonly object _lock = new object();
        private static readonly LinkedList<LogEntry> _buffer = new LinkedList<LogEntry>();
        private static readonly Dictionary<int, LinkedListNode<LogEntry>> _dedup =
            new Dictionary<int, LinkedListNode<LogEntry>>();
        private static bool _initialized;

        /// <summary>
        /// Thread-safe monotonic clock. Stopwatch uses QueryPerformanceCounter on Windows
        /// and clock_gettime(CLOCK_MONOTONIC) on Linux/Android — safe from any thread.
        /// </summary>
        private static readonly Stopwatch _clock = new Stopwatch();

        // Aggregate counters (not reset by buffer eviction)
        private static int _totalErrors;
        private static int _totalWarnings;
        private static int _totalLogs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            _clock.Start();
            Application.logMessageReceivedThreaded += OnLogMessage;
        }

        private static void OnLogMessage(string message, string stackTrace, LogType type)
        {
            // Track aggregate counts (Interlocked is safe from any thread)
            switch (type)
            {
                case LogType.Error:
                case LogType.Exception:
                case LogType.Assert:
                    Interlocked.Increment(ref _totalErrors);
                    break;
                case LogType.Warning:
                    Interlocked.Increment(ref _totalWarnings);
                    break;
                default:
                    Interlocked.Increment(ref _totalLogs);
                    break;
            }

            // C3 fix: compute dedup key BEFORE truncation so messages that differ only
            // past the truncation point get distinct keys
            int dedupKey = ComputeDedupKey(message, type);

            // Truncate for memory efficiency (after hashing)
            if (message != null && message.Length > MaxMessageLength)
                message = message.Substring(0, MaxMessageLength) + "...";
            if (stackTrace != null && stackTrace.Length > MaxStackTraceLength)
                stackTrace = stackTrace.Substring(0, MaxStackTraceLength) + "...";

            // C1 fix: use Stopwatch instead of Time.realtimeSinceStartup (not thread-safe on ARM)
            double now = _clock.Elapsed.TotalSeconds;

            lock (_lock)
            {
                // Deduplication: if we've seen this exact message+type before, increment count
                if (_dedup.TryGetValue(dedupKey, out var existingNode))
                {
                    var entry = existingNode.Value;
                    entry.DuplicateCount++;
                    entry.TimestampSeconds = now;
                    existingNode.Value = entry;
                    // Move to end (most recent)
                    _buffer.Remove(existingNode);
                    _buffer.AddLast(existingNode);
                    return;
                }

                // New entry
                var newEntry = new LogEntry
                {
                    Message = message ?? "",
                    StackTrace = stackTrace ?? "",
                    Type = type,
                    TimestampSeconds = now,
                    DuplicateCount = 1,
                    DedupKey = dedupKey
                };

                var node = _buffer.AddLast(newEntry);
                _dedup[dedupKey] = node;

                // Evict oldest if buffer full
                while (_buffer.Count > MaxBufferSize)
                {
                    var oldest = _buffer.First;
                    // Use stored key — message was truncated after hashing
                    _dedup.Remove(oldest.Value.DedupKey);
                    _buffer.RemoveFirst();
                }
            }
        }

        private static int ComputeDedupKey(string message, LogType type)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + (message?.GetHashCode() ?? 0);
                hash = hash * 31 + (int)type;
                return hash;
            }
        }

        // -----------------------------------------------------------------
        // Public query API (called from tool callbacks on main thread)
        // -----------------------------------------------------------------

        /// <summary>
        /// Get the most recent log entries, optionally filtered by log type.
        /// </summary>
        /// <param name="maxEntries">Maximum entries to return.</param>
        /// <param name="typeFilter">If set, only return entries of this type. Null = all types.</param>
        public static string GetRecentLogs(int maxEntries = 20, string typeFilter = null)
        {
            LogType? filter = ParseLogTypeFilter(typeFilter);
            bool unrecognizedFilter = !string.IsNullOrEmpty(typeFilter) && !filter.HasValue;

            lock (_lock)
            {
                if (_buffer.Count == 0)
                    return "No log entries captured yet.";

                var sb = new StringBuilder();
                int count = 0;
                int skipped = 0;

                // Iterate from newest to oldest
                var node = _buffer.Last;
                while (node != null && count < maxEntries)
                {
                    var entry = node.Value;
                    if (filter.HasValue && entry.Type != filter.Value)
                    {
                        skipped++;
                        node = node.Previous;
                        continue;
                    }

                    string typeLabel = FormatLogType(entry.Type);
                    string dupSuffix = entry.DuplicateCount > 1
                        ? $" (x{entry.DuplicateCount})"
                        : "";
                    sb.AppendLine($"[{typeLabel}] {entry.Message}{dupSuffix}");
                    if (entry.Type == LogType.Error || entry.Type == LogType.Exception ||
                        entry.Type == LogType.Assert)
                    {
                        if (!string.IsNullOrEmpty(entry.StackTrace))
                            sb.AppendLine($"  Stack: {entry.StackTrace.Split('\n')[0]}");
                    }
                    count++;
                    node = node.Previous;
                }

                if (count == 0)
                {
                    return filter.HasValue
                        ? $"No {typeFilter} entries in the log buffer ({_buffer.Count} total entries of other types)."
                        : "No log entries captured yet.";
                }

                var header = new StringBuilder();
                if (unrecognizedFilter)
                    header.AppendLine($"Warning: Unknown log_type '{typeFilter}'. " +
                        "Valid values: Error, Warning, Info, Exception, Assert. Showing all types.");
                header.AppendLine($"Recent logs ({count} entries" +
                    (filter.HasValue ? $", filtered by {typeFilter}" : "") + "):");
                header.AppendLine();
                header.Append(sb);

                if (_buffer.Count > count + skipped)
                    header.AppendLine($"\n... ({_buffer.Count - count - skipped} older entries in buffer)");

                return header.ToString();
            }
        }

        /// <summary>
        /// Get a deduplicated summary of errors and exceptions with occurrence counts.
        /// </summary>
        public static string GetErrorSummary()
        {
            lock (_lock)
            {
                var errors = new List<LogEntry>();
                var node = _buffer.Last;
                while (node != null)
                {
                    var entry = node.Value;
                    if (entry.Type == LogType.Error || entry.Type == LogType.Exception ||
                        entry.Type == LogType.Assert)
                    {
                        errors.Add(entry);
                    }
                    node = node.Previous;
                }

                if (errors.Count == 0)
                    return "No errors or exceptions in the log buffer.\n" +
                           $"Total since session start: {Volatile.Read(ref _totalErrors)} errors, " +
                           $"{Volatile.Read(ref _totalWarnings)} warnings, {Volatile.Read(ref _totalLogs)} info.";

                var sb = new StringBuilder();
                sb.AppendLine($"Error Summary ({errors.Count} unique error(s) in buffer):");
                sb.AppendLine();

                int shown = 0;
                foreach (var entry in errors)
                {
                    if (shown >= 20) break; // Cap at 20 unique errors
                    string typeLabel = FormatLogType(entry.Type);
                    string dupSuffix = entry.DuplicateCount > 1
                        ? $" (occurred {entry.DuplicateCount} times)"
                        : "";
                    sb.AppendLine($"  [{typeLabel}] {entry.Message}{dupSuffix}");
                    if (!string.IsNullOrEmpty(entry.StackTrace))
                        sb.AppendLine($"    Stack: {entry.StackTrace.Split('\n')[0]}");
                    shown++;
                }

                if (errors.Count > 20)
                    sb.AppendLine($"\n... ({errors.Count - 20} more errors not shown)");

                sb.AppendLine();
                sb.AppendLine($"Session totals: {Volatile.Read(ref _totalErrors)} errors, " +
                    $"{Volatile.Read(ref _totalWarnings)} warnings, {Volatile.Read(ref _totalLogs)} info.");
                sb.Append("Tip: Use unity_get_recent_logs with log_type \"Error\" for detailed error messages.");

                return sb.ToString();
            }
        }

        /// <summary>
        /// Get a deduplicated summary of warnings with occurrence counts.
        /// </summary>
        public static string GetWarningSummary()
        {
            lock (_lock)
            {
                var warnings = new List<LogEntry>();
                var node = _buffer.Last;
                while (node != null)
                {
                    if (node.Value.Type == LogType.Warning)
                        warnings.Add(node.Value);
                    node = node.Previous;
                }

                if (warnings.Count == 0)
                    return "No warnings in the log buffer.\n" +
                           $"Total since session start: {Volatile.Read(ref _totalErrors)} errors, " +
                           $"{Volatile.Read(ref _totalWarnings)} warnings, {Volatile.Read(ref _totalLogs)} info.";

                var sb = new StringBuilder();
                sb.AppendLine($"Warning Summary ({warnings.Count} unique warning(s) in buffer):");
                sb.AppendLine();

                int shown = 0;
                foreach (var entry in warnings)
                {
                    if (shown >= 20) break;
                    string dupSuffix = entry.DuplicateCount > 1
                        ? $" (occurred {entry.DuplicateCount} times)"
                        : "";
                    sb.AppendLine($"  [WARNING] {entry.Message}{dupSuffix}");
                    shown++;
                }

                if (warnings.Count > 20)
                    sb.AppendLine($"\n... ({warnings.Count - 20} more warnings not shown)");

                sb.AppendLine();
                sb.AppendLine($"Session totals: {Volatile.Read(ref _totalErrors)} errors, " +
                    $"{Volatile.Read(ref _totalWarnings)} warnings, {Volatile.Read(ref _totalLogs)} info.");
                sb.Append("Tip: Use unity_get_recent_logs with log_type \"Warning\" for detailed warning messages.");

                return sb.ToString();
            }
        }

        /// <summary>
        /// Search the log buffer for entries matching a keyword (case-insensitive).
        /// Returns full details including stack traces for matching entries.
        /// </summary>
        /// <param name="keyword">Search keyword (case-insensitive substring match).</param>
        /// <param name="maxResults">Maximum entries to return.</param>
        public static string SearchLogs(string keyword, int maxResults = 20)
        {
            if (string.IsNullOrEmpty(keyword))
                return "Error: keyword is required.\n" +
                       "Tip: Provide a search term, e.g. \"NullReference\" or \"NetworkManager\".";

            lock (_lock)
            {
                if (_buffer.Count == 0)
                    return "No log entries captured yet.";

                var sb = new StringBuilder();
                int count = 0;
                int totalMatches = 0;

                var node = _buffer.Last;
                while (node != null)
                {
                    var entry = node.Value;
                    bool inMessage = entry.Message != null &&
                        entry.Message.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;
                    bool inStack = entry.StackTrace != null &&
                        entry.StackTrace.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0;

                    if (inMessage || inStack)
                    {
                        totalMatches++;
                        if (count < maxResults)
                        {
                            string typeLabel = FormatLogType(entry.Type);
                            string dupSuffix = entry.DuplicateCount > 1
                                ? $" (x{entry.DuplicateCount})"
                                : "";
                            sb.AppendLine($"[{typeLabel}] {entry.Message}{dupSuffix}");
                            if (!string.IsNullOrEmpty(entry.StackTrace))
                                sb.AppendLine($"  Stack: {entry.StackTrace}");
                            sb.AppendLine();
                            count++;
                        }
                    }
                    node = node.Previous;
                }

                if (count == 0)
                    return $"No log entries matching \"{keyword}\" found ({_buffer.Count} entries in buffer).";

                var header = new StringBuilder();
                header.AppendLine($"Log search results for \"{keyword}\" ({count} of {totalMatches} matches):");
                header.AppendLine();
                header.Append(sb);

                if (totalMatches > count)
                    header.AppendLine($"... ({totalMatches - count} more matches not shown)");

                return header.ToString();
            }
        }

        /// <summary>
        /// Clear the log buffer and reset aggregate counters.
        /// </summary>
        public static string ClearDiagnosticData()
        {
            lock (_lock)
            {
                int bufferCount = _buffer.Count;
                _buffer.Clear();
                _dedup.Clear();
                Interlocked.Exchange(ref _totalErrors, 0);
                Interlocked.Exchange(ref _totalWarnings, 0);
                Interlocked.Exchange(ref _totalLogs, 0);
                return $"Diagnostic data cleared ({bufferCount} log entries removed, counters reset).";
            }
        }

        /// <summary>
        /// Get an aggregate health status assessment.
        /// </summary>
        public static string GetHealthStatus()
        {
            // I4 fix: use Volatile.Read for correct ARM memory ordering when reading
            // counters that are incremented via Interlocked from other threads
            int errors = Volatile.Read(ref _totalErrors);
            int warnings = Volatile.Read(ref _totalWarnings);
            int logs = Volatile.Read(ref _totalLogs);

            string status;
            if (errors > 10)
                status = "CRITICAL";
            else if (errors > 0 || warnings > 20)
                status = "NEEDS_ATTENTION";
            else
                status = "HEALTHY";

            int recentErrors = 0;
            int recentWarnings = 0;
            lock (_lock)
            {
                // C1 fix: use Stopwatch-based clock instead of Time.realtimeSinceStartup
                double cutoff = _clock.Elapsed.TotalSeconds - 30.0;
                var node = _buffer.Last;
                while (node != null)
                {
                    if (node.Value.TimestampSeconds < cutoff) break;
                    if (node.Value.Type == LogType.Error || node.Value.Type == LogType.Exception ||
                        node.Value.Type == LogType.Assert)
                        recentErrors += node.Value.DuplicateCount;
                    else if (node.Value.Type == LogType.Warning)
                        recentWarnings += node.Value.DuplicateCount;
                    node = node.Previous;
                }
            }

            var sb = new StringBuilder();
            sb.AppendLine($"Health Status: {status}");
            sb.AppendLine();
            sb.AppendLine("Session totals:");
            sb.AppendLine($"  Errors: {errors}");
            sb.AppendLine($"  Warnings: {warnings}");
            sb.AppendLine($"  Info: {logs}");
            sb.AppendLine();
            sb.AppendLine("Last 30 seconds:");
            sb.AppendLine($"  Errors: {recentErrors}");
            sb.AppendLine($"  Warnings: {recentWarnings}");

            if (status != "HEALTHY")
            {
                sb.AppendLine();
                sb.Append("Tip: Use unity_diagnose for a full diagnostic report, " +
                    "or unity_load_tools(action='load', group='diagnostics') then unity_get_error_summary for error details.");
            }

            return sb.ToString();
        }

        // -----------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------

        private static LogType? ParseLogTypeFilter(string filter)
        {
            if (string.IsNullOrEmpty(filter)) return null;
            switch (filter.ToLower())
            {
                case "error": return LogType.Error;
                case "exception": return LogType.Exception;
                case "warning": return LogType.Warning;
                case "log":
                case "info": return LogType.Log;
                case "assert": return LogType.Assert;
                default: return null;
            }
        }

        private static string FormatLogType(LogType type)
        {
            switch (type)
            {
                case LogType.Error: return "ERROR";
                case LogType.Exception: return "EXCEPTION";
                case LogType.Warning: return "WARNING";
                case LogType.Assert: return "ASSERT";
                case LogType.Log: return "INFO";
                default: return type.ToString().ToUpper();
            }
        }
    }
}

#endif // USING_XR_SDK_OPENXR
