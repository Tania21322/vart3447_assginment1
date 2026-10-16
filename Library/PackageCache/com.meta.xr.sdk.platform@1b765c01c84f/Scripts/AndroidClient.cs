// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Oculus.Platform
{
    public class AndroidClient
    {
        private static TaskCompletionSource<HorizonStatus> initTaskCompletionSource;
        private static HorizonStatus initStatus;
        private static ulong globalSessionID = 0;
        private static AndroidJavaObject javaBridge;

        public static HorizonStatus Initialize(string AppID)
        {
            if (initStatus != null && initStatus.IsSuccess())
            {
                return initStatus;
            }

            try
            {
                AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                javaBridge = new AndroidJavaObject("horizonos.supplement.hzplatformclientcore.HorizonPlatformCore");

                AndroidJavaObject config = new AndroidJavaObject(
                    "horizonos.supplement.hzplatformclientcore.AndroidHorizonPlatformConfig",
                    currentActivity,
                    AppID);

                javaBridge.Call("setup", config);
                globalSessionID = (ulong)javaBridge.Call<long>("getGlobalSessionId");
                initStatus = new HorizonStatus(0, "Initialize");
            }
            catch (Exception e)
            {
                Debug.LogError("Failed to initialize HorizonPlatformCore: " + e.Message);
                initStatus = new HorizonStatus(100, "Initialize");
            }

            initStatus.ThrowIfError();
            return initStatus;
        }

        public static async Task<HorizonStatus> InitAsync(string AppID)
        {
            if (initTaskCompletionSource != null)
            {
                if (!initTaskCompletionSource.Task.IsCompleted)
                    return await initTaskCompletionSource.Task;
                if (initTaskCompletionSource.Task.Result.IsSuccess())
                    return initTaskCompletionSource.Task.Result;
            }

            initTaskCompletionSource = new TaskCompletionSource<HorizonStatus>();

            await Task.Run(() =>
            {
                try
                {
                    AndroidJNI.AttachCurrentThread();

                    // Get the Unity Android context
                    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

                    javaBridge = new AndroidJavaObject("horizonos.supplement.hzplatformclientcore.HorizonPlatformCore");

                    AndroidJavaObject config = new AndroidJavaObject(
                        "horizonos.supplement.hzplatformclientcore.AndroidHorizonPlatformConfig",
                        currentActivity,
                        AppID);

                    javaBridge.Call("setup", config);
                    globalSessionID = (ulong)javaBridge.Call<long>("getGlobalSessionId");
                    initTaskCompletionSource.SetResult(new HorizonStatus(0, "InitAsync"));
                }
                catch (Exception e)
                {
                    Debug.LogError("Failed to initialize HorizonPlatformCore: " + e.Message);
                    initTaskCompletionSource.SetResult(new HorizonStatus(100, "InitAsync"));
                }
                finally
                {
                    AndroidJNI.DetachCurrentThread();
                }
            });

            var status = await initTaskCompletionSource.Task;
            status.ThrowIfError();
            return status;
        }

        static public ulong GlobalSession()
        {
            return globalSessionID;
        }

        static public int GetResponseCount()
        {
            return GetMessageCount(globalSessionID);
        }

        static public int GetMessageCount(ulong sessionID)
        {
            try
            {
                if (javaBridge == null)
                {
                    Debug.LogError("Java bridge is not initialized");
                    return -1;
                }

                return javaBridge.Call<int>("getMessageCount", (long)sessionID);
            }
            catch (Exception e)
            {
                Debug.LogError("Error in GetMessageCount: " + e.Message);
                throw;
            }
        }

        static public ulong MakeRequest(string module, string requestName, int apiVersion, string requestData, int cookie)
        {
            try
            {
                if (javaBridge == null)
                {
                    Debug.LogError("Java bridge is not initialized");
                    return 0;
                }

                return (ulong)javaBridge.Call<long>("makeRequest", module, requestName, apiVersion, requestData, cookie);
            }
            catch (Exception e)
            {
                Debug.LogError("Error in MakeRequest: " + e.Message);
                throw;
            }
        }

        static public ulong MakeSession(string module, string requestName, int apiVersion, string requestData, int cookie)
        {
            try
            {
                if (javaBridge == null)
                {
                    Debug.LogError("Java bridge is not initialized");
                    return 0;
                }

                return (ulong)javaBridge.Call<long>("makeSession", module, requestName, apiVersion, requestData, cookie);
            }
            catch (Exception e)
            {
                Debug.LogError("Error in MakeSession: " + e.Message);
                throw;
            }
        }

        static public void StopSession(ulong sessionID)
        {
            try
            {
                if (javaBridge == null)
                {
                    Debug.LogError("Java bridge is not initialized");
                    return;
                }

                javaBridge.Call("stopSession", (long)sessionID);
            }
            catch (Exception e)
            {
                Debug.LogError("Error in StopSession: " + e.Message);
                throw;
            }
        }

        static public Message PopResponse(bool yield)
        {
            return PopMessage(globalSessionID, yield);
        }

        static public Message PopMessage(ulong sessionID, bool yield)
        {
            HorizonStatus errorStatus = new HorizonStatus(100, "popMessage");
            Message errorMessage = new Message(ulong.MaxValue, sessionID, -1, "", errorStatus);

            try
            {
                if (javaBridge == null)
                {
                    Debug.LogError("Java bridge is not initialized");
                    return errorMessage;
                }

                AndroidJavaObject javaMessage = javaBridge.Call<AndroidJavaObject>("popMessage", (long)sessionID, yield);

                if (javaMessage == null)
                {
                    Debug.LogError("Received null message from HorizonPlatformCore popMessage");
                    return errorMessage;
                }

                try
                {
                    ulong requestID = (ulong)javaMessage.Get<long>("requestId");
                    int statusCode = javaMessage.Get<int>("statusCode");
                    string statusMessage = javaMessage.Get<string>("statusMessage");
                    string data = javaMessage.Get<string>("data");
                    int cookie = javaMessage.Get<int>("cookie");

                    Debug.Log($"Message data: {data}");

                    HorizonStatus status = new HorizonStatus(statusCode, statusMessage ?? $"Session {sessionID}");

                    return new Message(requestID, sessionID, cookie, data, status);
                }
                finally
                {
                    javaMessage?.Dispose();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error in PopMessage: " + e.Message);
                throw;
            }
        }
    }
}
