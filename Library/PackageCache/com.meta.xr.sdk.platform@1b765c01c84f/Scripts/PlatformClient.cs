// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Threading.Tasks;
using UnityEngine;

namespace Oculus.Platform
{
    public class PlatformClient
    {
        public static HorizonStatus Initialize(string AppID, string runtimeMode = null, string accessToken = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidClient.Initialize(AppID);
#else
            return WindowsClient.Initialize(AppID, runtimeMode, accessToken);
#endif
        }

        public static async Task<HorizonStatus> InitAsync(string AppID, string runtimeMode = null, string accessToken = null)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return await AndroidClient.InitAsync(AppID);
#else
            return await WindowsClient.AsyncInitialize(AppID, runtimeMode, accessToken);
#endif
        }

        public static ulong GlobalSession()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidClient.GlobalSession();
#else
            Debug.Log("GlobalSession called on Windows platform.");
            return 0;
#endif
        }

        public static int GetResponseCount()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidClient.GetResponseCount();
#else
            return WindowsClient.GetResponseCount();
#endif
        }

        public static int GetMessageCount(ulong sessionID)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidClient.GetMessageCount(sessionID);
#else
            return WindowsClient.GetMessageCount(sessionID);
#endif
        }

        public static ulong MakeRequest(string module, string requestName, int apiVersion, string requestData, int cookie)
        {
            string eventName = "HzPSDK_" + EventManager.ToPascalCase(module) + "_" + EventManager.ToPascalCase(requestName);
            EventManager.SendUnifiedEvent(true, "platform_sdk", eventName);

#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidClient.MakeRequest(module, requestName, apiVersion, requestData, cookie);
#else
            return WindowsClient.MakeRequest(module, requestName, apiVersion, requestData, cookie);
#endif
        }

        public static ulong MakeSession(string module, string requestName, int apiVersion, string requestData, int cookie)
        {
            string eventName = "HzPSDK_" + EventManager.ToPascalCase(module) + "_" + EventManager.ToPascalCase(requestName) + "NotificationCallback";
            EventManager.SendUnifiedEvent(true, "platform_sdk", eventName);

#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidClient.MakeSession(module, requestName, apiVersion, requestData, cookie);
#else
            Debug.Log("MakeSession called on Windows platform.");
            return 0;
#endif
        }

        public static void StopSession(ulong sessionID)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidClient.StopSession(sessionID);
#else
            Debug.Log("StopSession called on Windows platform.");
#endif
        }

        public static Message PopResponse(bool yield)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidClient.PopResponse(yield);
#else
            return WindowsClient.PopResponse(yield);
#endif
        }

        public static Message PopMessage(ulong sessionID, bool yield)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return AndroidClient.PopMessage(sessionID, yield);
#else
            return WindowsClient.PopMessage(sessionID, yield);
#endif
        }
    }
}
