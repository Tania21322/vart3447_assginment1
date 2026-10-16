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

#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || (UNITY_ANDROID && !UNITY_EDITOR))
#define OVRPLUGIN_UNSUPPORTED_PLATFORM
#endif

#if !(UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || (UNITY_ANDROID && !UNITY_EDITOR))
#define OVRPLUGIN_QPL_UNSUPPORTED_PLATFORM
#endif

#if OVRROPLUGIN_TESTING && UNITY_EDITOR && OVRPLUGIN_UNSUPPORTED_PLATFORM
#define OVRPLUGIN_EDITOR_MOCK_ENABLED
#undef OVRPLUGIN_UNSUPPORTED_PLATFORM
#endif

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using Debug = UnityEngine.Debug;
using RO = Meta.XR.RuntimeOptimizer.Core;

namespace Meta.XR.RuntimeOptimizer.Core
{
    public static partial class RuntimeOptimizerPlugin
    {
#if OVRPLUGIN_UNSUPPORTED_PLATFORM && OVRPLUGIN_QPL_UNSUPPORTED_PLATFORM
        public const bool isSupportedPlatform = false;
#else
    public const bool isSupportedPlatform = true;
#endif

        public static readonly System.Version minVersion = new System.Version(1, 97, 0);

        private static string sessionId = "";

#if !(OVRPLUGIN_UNSUPPORTED_PLATFORM && OVRPLUGIN_QPL_UNSUPPORTED_PLATFORM)
    private static System.Version _version;
#endif

        public static string Initialize(string sessionid, string scriptGUID)
        {

            string newSessionid = "";
            try
            {
                RO.Util.DebugLog("RuntimeOptimizer_Plugin pguid: " + scriptGUID);
                IntPtr ptr = OVRROP_0_0_1.ovrro_Init("Unity", minVersion.ToString(), sessionid, scriptGUID);
                newSessionid = Marshal.PtrToStringAnsi(ptr);
                sessionId = newSessionid;
                RO.Util.DebugLog("RuntimeOptimizer_Plugin initialized: " + newSessionid);
            }
            catch (DllNotFoundException)
            {
                RO.Util.DebugLogError("RuntimeOptimizer_Plugin not found");
            }
            return newSessionid;
        }

        public static bool Shutdown()
        {
            try
            {
                OVRROP_0_0_1.ovrro_Shutdown("Unity", minVersion.ToString(), sessionId);
                RO.Util.DebugLog("RuntimeOptimizer_Plugin Shutdown");
            }
            catch (DllNotFoundException)
            {
                RO.Util.DebugLogError("RuntimeOptimizer_Plugin not found");
            }
            return true;
        }

        public static bool SendEvent(string eventName, string jsonData)
        {

            try
            {
                OVRROP_0_0_1.ovrro_SendEvent(eventName, jsonData, sessionId);
            }
            catch (Exception e)
            {
                RO.Util.DebugLog("SendEvent failed: " + e.Message);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Returns the leading run of real samples from a NaN-prefilled native buffer.
        /// </summary>
        /// <remarks>
        /// Native writes from index 0 upwards and leaves the remainder untouched, so the first NaN
        /// marks the end of the data. An empty result means "the profiler produced no samples", which
        /// callers must not confuse with "the GPU measured 0 ms".
        /// </remarks>
        internal static float[] ExtractWrittenSamples(float[] buffer)
        {
            if (buffer == null)
            {
                return new float[0];
            }

            int sampleCount = 0;
            while (sampleCount < buffer.Length && !float.IsNaN(buffer[sampleCount]))
            {
                sampleCount++;
            }

            if (sampleCount == buffer.Length)
            {
                return buffer;
            }

            var samples = new float[sampleCount];
            Array.Copy(buffer, samples, sampleCount);
            return samples;
        }

#if UNITY_ANDROID
    public static bool InitializeGpuProfiling()
    {
        bool result = OVRROP_0_0_1.ovrro_InitializeGpuProfiling();
        return result;
    }

    public static bool StartRealtimeMetrics(uint[] metricIds, int intervalMs)
    {
        bool result = OVRROP_0_0_1.ovrro_StartRealtimeMetrics(metricIds, metricIds.Length, intervalMs);
        UnityEngine.Debug.Log("RuntimeOptimizer_Plugin realtime metrics started: " + result);
        return result;
    }

    public static bool GetMetrics(uint[] metricId, out float[] value, int metricCount)
    {
        // Pre-allocate the array for the values
        float[] buffer = new float[metricCount];

        // Call the native function with the pre-allocated buffer
        bool result = OVRROP_0_0_1.ovrro_GetMetrics(metricId, buffer, metricCount);

        // Assign the buffer to the out parameter
        value = buffer;

        return result;
    }

    public static bool GetFrameTime(int frameCount, out float[] frameTimes, string processName = "", bool stop = false)
    {
        // Allocate buffer for frame times.
        //
        // The native call reports success for three different outcomes: it started a trace, it
        // stopped one, or it polled one. Only the poll writes samples, and a poll that matched no
        // frames writes nothing while still returning true. Pre-filling with NaN lets us tell
        // "no samples" apart from "measured 0 ms" -- without it the caller receives a zero-filled
        // buffer of the requested length and records a fabricated 0 for every GameObject.
        float[] buffer = new float[frameCount];
        for (int i = 0; i < frameCount; i++)
        {
            buffer[i] = float.NaN;
        }

        bool result = OVRROP_0_0_1.ovrro_GetFrameTime(frameCount, buffer, processName, stop);

        if (!result)
        {
            frameTimes = new float[0];
            return false;
        }

        frameTimes = ExtractWrittenSamples(buffer);
        return true;
    }
#endif

#if (UNITY_ANDROID && !UNITY_EDITOR)
    private const string pluginName = "LibRuntimeOptimizer";
#else
        private const string pluginName = "RuntimeOptimizer_Plugin_dll";
#endif
        private static System.Version _versionZero = new System.Version(0, 0, 0);

#if OVRROPLUGIN_TESTING
    private static class OVRROP_0_0_1_PROD
#else
        private static class OVRROP_0_0_1
#endif // OVRPlugin_Testing
        {
            public static readonly System.Version version = new System.Version(0, 0, 1);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr ovrro_Init(string engineName, string engineVersion, string sessionid, string pguid);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void ovrro_Shutdown(string engineName, string engineVersion, string sessionid);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern void ovrro_SendEvent(string eventName, string jsonData, string sessionid);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern int ovrro_test(string engineName, string engineVersion, string sessionid);
#if UNITY_ANDROID
            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool ovrro_InitializeGpuProfiling();

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool ovrro_StartRealtimeMetrics([In] uint[] metricIds, int metricCount, int intervalMs);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool ovrro_GetMetrics([In] uint[] metricId, [Out] float[] value, int metricCount);

            [DllImport(pluginName, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool ovrro_GetFrameTime(int frameTimesCapacity, [Out] float[] frameTimes, string processName, bool stop = false);
#endif
        }
    }
}
