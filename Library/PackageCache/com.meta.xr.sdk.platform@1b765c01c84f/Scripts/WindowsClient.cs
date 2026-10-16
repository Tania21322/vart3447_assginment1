// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Oculus.Platform
{
    public class WindowsClient
    {
        private const string DllName = "LibOVRPlatformImpl64_1";

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int hzpsdk_Initialize(string appId, string platform, string extraSettingsJson);


        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern ulong hzpsdk_MakeRequest(
            string module,
            string requestName,
            int apiVersion,
            string requestData,
            int cookie);


        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr hzpsdk_PopMessage(ulong sessionID, bool yield);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
        public static extern int hzpsdk_GetMessageCount(ulong sessionID);

        private static TaskCompletionSource<HorizonStatus> initTaskCompletionSource;
        private static HorizonStatus initStatus;
        private static volatile bool runtimeIncompatible;

        private const string IncompatibleRuntimeMessage =
            "Meta XR Platform SDK v203+ is not compatible with the current PCLink/Skyline runtime. "
            + "Initialization succeeded but the runtime cannot process platform requests. "
            + "To unblock Play-In-Editor immediately, switch PCLink to the v201 PTC release channel."
            + "Otherwise: downgrade to Platform SDK v201, or wait for PCLink v205. ";

        // Sends a lightweight request and waits for any response to confirm the
        // runtime can actually process requests.  Returns false when the runtime
        // accepts init but silently drops requests (PCLink v85 + SDK v203+).
        private static bool ProbeRuntime(int timeoutMs)
        {
            try
            {
                while (hzpsdk_GetMessageCount(0) > 0)
                    hzpsdk_PopMessage(0, false);

                hzpsdk_MakeRequest("application", "get_version", 1, "{}", 0);

                var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
                while (DateTime.UtcNow < deadline)
                {
                    if (hzpsdk_GetMessageCount(0) > 0)
                    {
                        hzpsdk_PopMessage(0, false);
                        return true;
                    }
                    Thread.Sleep(100);
                }
                return false;
            }
            catch (Exception e) when (e is EntryPointNotFoundException || e is DllNotFoundException)
            {
                return false;
            }
        }

        public static HorizonStatus Initialize(string appId, string runtimeMode, string accessToken)
        {
            if (initStatus != null && initStatus.IsSuccess())
            {
                return initStatus;
            }

            string jsonRequest = JsonConvert.SerializeObject(new
            {
                access_token = accessToken
            });
            int statusCode = hzpsdk_Initialize(appId, runtimeMode, jsonRequest);
            initStatus = new HorizonStatus(statusCode, "Initialize");
            initStatus.ThrowIfError();

            // Reset before probing so a successful retry after a prior incompatible
            // runtime (e.g. dev switched PCLink to v201 PTC) clears the stale flag.
            runtimeIncompatible = false;
            if (!ProbeRuntime(5000))
            {
                runtimeIncompatible = true;
                Debug.LogError(IncompatibleRuntimeMessage);
                throw new NotSupportedException(IncompatibleRuntimeMessage);
            }

            return initStatus;
        }

        public static async Task<HorizonStatus> AsyncInitialize(string appId, string runtimeMode, string accessToken)
        {
            if (initTaskCompletionSource != null)
            {
                if (!initTaskCompletionSource.Task.IsCompleted)
                    return await initTaskCompletionSource.Task;
                // A faulted task has IsCompleted == true; accessing .Result would throw
                // AggregateException. Fall through to retry init in that case rather than
                // surfacing the original exception via .Result.
                if (!initTaskCompletionSource.Task.IsFaulted &&
                    initTaskCompletionSource.Task.Result.IsSuccess())
                    return initTaskCompletionSource.Task.Result;
            }

            initTaskCompletionSource = new TaskCompletionSource<HorizonStatus>();

            await Task.Run(() =>
            {
                try
                {
                    string jsonRequest = JsonConvert.SerializeObject(new
                    {
                        access_token = accessToken
                    });
                    hzpsdk_Initialize(appId, runtimeMode, jsonRequest);

                    // Reset before probing so a successful retry after a prior incompatible
                    // runtime (e.g. dev switched PCLink to v201 PTC) clears the stale flag.
                    runtimeIncompatible = false;
                    if (!ProbeRuntime(10000))
                    {
                        runtimeIncompatible = true;
                        Debug.LogError(IncompatibleRuntimeMessage);
                        initTaskCompletionSource.SetException(new NotSupportedException(IncompatibleRuntimeMessage));
                        return;
                    }

                    initTaskCompletionSource.SetResult(new HorizonStatus(0, "InitAsync"));
                }
                catch (Exception e)
                {
                    initTaskCompletionSource.SetException(e);
                }
            });

            var status = await initTaskCompletionSource.Task;
            status.ThrowIfError();
            return status;
        }

        private static void ThrowIfRuntimeIncompatible()
        {
            if (runtimeIncompatible)
                throw new NotSupportedException(IncompatibleRuntimeMessage);
        }

        public static ulong MakeRequest(string module, string requestName, int apiVersion, string requestData, int cookie)
        {
            ThrowIfRuntimeIncompatible();
            return hzpsdk_MakeRequest(module, requestName, apiVersion, requestData, cookie);
        }
        static public Message PopResponse(bool yield)
        {
            return PopMessage(0, yield);
        }

        public static Message PopMessage(ulong sessionID, bool yield)
        {
            if (runtimeIncompatible) return null;

            IntPtr ptr = hzpsdk_PopMessage(sessionID, yield);
            string messageString = GetStringFromIntPtr(ptr);

            if (messageString == null)
            {
                return null;
            }

            var messageObj = JsonConvert.DeserializeObject<JObject>(messageString);
            ulong requestId = (ulong)messageObj["requestId"];
            string response = (string)messageObj["response"];
            int statusCode = (int)messageObj["statusCode"];
            string statusMessage = (string)messageObj["statusMessage"] ?? "";

            Message m = new Message(requestId, 0, 0, response, new HorizonStatus(statusCode, statusMessage));
            return m;
        }

        static public int GetResponseCount()
        {
            return GetMessageCount(0);
        }

        public static int GetMessageCount(ulong sessionID)
        {
            if (runtimeIncompatible) return 0;
            return hzpsdk_GetMessageCount(sessionID);
        }

        public static string GetStringFromIntPtr(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero)
                return null;
            return Marshal.PtrToStringAnsi(ptr);
        }
    }
}
