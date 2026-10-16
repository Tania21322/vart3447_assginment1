// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using Oculus.Platform;
using Oculus.Platform.Models;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

namespace Oculus.Platform
{
    public sealed class Core
    {
        private static bool IsPlatformInitialized = false;

        /// If true, the contents of each request response will be printed using Debug.Log. This can allocate a lot of heap memory, so it should only be used for testing and debugging purposes.
        public static bool LogMessages = false;

        /// The error message that will be displayed to the user if the platform is not initialized.
        public static string PlatformUninitializedError = "This function requires an initialized Oculus Platform. Run Oculus.Platform.Core.[Initialize|AsyncInitialize] and try again.";

        public static HorizonStatus Initialize(string appId = null, string runtimeMode = null)
        {
            string finalRuntimeMode = GetRuntimeMode();
            string finalAppId = GetAppId(appId, finalRuntimeMode);
            string accessToken = GetAccessToken(finalRuntimeMode);

            var metadata = new Dictionary<string, object>
            {
                { "appId", finalAppId },
                { "platformName", GetPlatformName(finalRuntimeMode) }
            };
            EventManager.SendUnifiedEvent(true, "platform_sdk", "HzPSDK_Initialize", metadata);

            HorizonStatus status = PlatformClient.Initialize(finalAppId, finalRuntimeMode, accessToken);

            if (status.IsSuccess())
            {
                CreateCallbackRunner();
                OnInitializationSuccess();
            }

            return status;
        }

        public static Request<PlatformInitialize> AsyncInitialize(string appId = null, string runtimeMode = null)
        {
            var request = new Request<PlatformInitialize>(0);
            var unityContext = SynchronizationContext.Current;

            string finalRuntimeMode = GetRuntimeMode();
            string finalAppId = GetAppId(appId, finalRuntimeMode);
            string accessToken = GetAccessToken(finalRuntimeMode);

            var metadata = new Dictionary<string, object>
            {
                { "appId", finalAppId },
                { "platformName", GetPlatformName(finalRuntimeMode) }
            };
            EventManager.SendUnifiedEvent(true, "platform_sdk", "HzPSDK_AsyncInitialize", metadata);

            Task.Run(async () =>
            {
                HorizonStatus status = await PlatformClient.InitAsync(finalAppId, finalRuntimeMode, accessToken);
                Message<PlatformInitialize> message = null;

                if (status.IsSuccess())
                {
                    var request_dict = new Dictionary<string, object>
                    {
                        { "Result", PlatformInitializeResult.Success.GetDescription() }
                    };

                    string jsonRequest = JsonConvert.SerializeObject(request_dict);

                    try
                    {
                        message = new Message<PlatformInitialize>(request.RequestID, 0, 0, jsonRequest, status);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Core] EXCEPTION creating Message: {ex.Message}\n{ex.StackTrace}");
                    }

                    unityContext.Send(_ =>
                    {
                        CreateCallbackRunner();
                    }, null);

                    OnInitializationSuccess();
                }
                else
                {
                    var request_dict = new Dictionary<string, object>
                    {
                        { "Result", PlatformInitializeResult.Unknown.GetDescription() }
                    };

                    string jsonRequest = JsonConvert.SerializeObject(request_dict);
                    message = new Message<PlatformInitialize>(request.RequestID, 0, 0, jsonRequest, status);
                }

                // Always deliver the result. The caller attaches its handler after this
                // background task is launched (await sets the TaskCompletionSource,
                // OnComplete sets the callback), so gating on a handler being present here
                // would drop the await path. Request<T> buffers until a handler attaches.
                unityContext.Post(_ =>
                {
                    request.HandleMessage(message);
                }, null);

            });
            return request;
        }

        public static Request<PlatformInitialize> AsyncInitialize(string accessToken, Dictionary<InitConfigOptions, bool> initConfigOptions, string appId = null)
        {
            var request = new Request<PlatformInitialize>(0);
            var unityContext = SynchronizationContext.Current;

            string finalRuntimeMode = GetRuntimeMode();
            string finalAppId = GetAppId(appId, finalRuntimeMode);

            var metadata = new Dictionary<string, object>
            {
                { "appId", finalAppId },
                { "platformName", GetPlatformName(finalRuntimeMode) }
            };
            EventManager.SendUnifiedEvent(true, "platform_sdk", "HzPSDK_AsyncInitialize", metadata);

            Task.Run(async () =>
            {
                HorizonStatus status = await PlatformClient.InitAsync(finalAppId, finalRuntimeMode, accessToken);
                Message<PlatformInitialize> message = null;

                if (status.IsSuccess())
                {
                    var request_dict = new Dictionary<string, object>
                    {
                        { "Result", PlatformInitializeResult.Success.GetDescription() }
                    };

                    string jsonRequest = JsonConvert.SerializeObject(request_dict);

                    try
                    {
                        message = new Message<PlatformInitialize>(request.RequestID, 0, 0, jsonRequest, status);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[Core] EXCEPTION creating Message: {ex.Message}\n{ex.StackTrace}");
                    }

                    unityContext.Send(_ =>
                    {
                        CreateCallbackRunner();
                    }, null);

                    OnInitializationSuccess();
                }
                else
                {
                    var request_dict = new Dictionary<string, object>
                    {
                        { "Result", PlatformInitializeResult.Unknown.GetDescription() }
                    };

                    string jsonRequest = JsonConvert.SerializeObject(request_dict);
                    message = new Message<PlatformInitialize>(request.RequestID, 0, 0, jsonRequest, status);
                }

                // Always deliver the result. The caller attaches its handler after this
                // background task is launched (await sets the TaskCompletionSource,
                // OnComplete sets the callback), so gating on a handler being present here
                // would drop the await path. Request<T> buffers until a handler attaches.
                unityContext.Post(_ =>
                {
                    request.HandleMessage(message);
                }, null);
            });

            return request;
        }

        public static bool IsInitialized()
        {
            return IsPlatformInitialized;
        }

        private static string GetRuntimeMode()
        {
            return PlatformSettings.UseStandalonePlatform ? "standalone" : "windows";
        }

        private static string GetAppId(string appId, string runtimeMode)
        {
            const string kMenuBarPath = "'Meta' > 'Platform' > 'Edit Settings'";

#if UNITY_ANDROID && !UNITY_EDITOR
            // On Android device, always use MobileAppID
            string configAppID = PlatformSettings.MobileAppID;
#else
            // On Windows/PC or in Editor, use runtime mode to determine app ID:
            // - "windows" runtime mode → Use AppID (Windows/PC app)
            // - "standalone" runtime mode → Use MobileAppID (testing mobile on PC)
            string configAppID = runtimeMode == "windows" ? PlatformSettings.AppID : PlatformSettings.MobileAppID;
#endif

            if (string.IsNullOrEmpty(appId))
            {
                if (string.IsNullOrEmpty(configAppID))
                {
                    throw new UnityException($"Update your app id by selecting {kMenuBarPath}");
                }
                return configAppID;
            }

            if (!string.IsNullOrEmpty(configAppID) && appId != configAppID)
            {
                Debug.LogWarning(
                    $"The App Id set in 'Resources/OculusPlatformSettings.asset' " +
                    $"({configAppID}) is being overridden by an App Id provided to " +
                    $"Platform.Core.Initialize ({appId})." +
                    $"  You should only specify this in one place." +
                    $"  Navigate to {kMenuBarPath} to enter an App Id."
                );
            }

            return appId;
        }

        private static string GetAccessToken(string runtimeMode)
        {
            return runtimeMode == "standalone" ? StandalonePlatformSettings.OculusPlatformTestUserAccessToken : null;
        }

        private static string GetPlatformName(string runtimeMode)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return "Android";
#else
            return EventManager.ToPascalCase(runtimeMode);
#endif
        }

        private static void CreateCallbackRunner()
        {
            var go = new GameObject("Oculus.Platform.CallbackRunner");
            go.AddComponent<CallbackRunner>();
        }

        private static void OnInitializationSuccess()
        {
            IsPlatformInitialized = true;
            Debug.Log("Horizon Platform Initialization Complete");
        }
    }
}
