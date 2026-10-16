// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using UnityEngine;

namespace Oculus.Platform
{
    public class CallbackRunner : MonoBehaviour
    {
        public bool IsPersistantBetweenSceneLoads = true;

        void Awake()
        {
            var existingCallbackRunner = FindObjectOfType<CallbackRunner>();
            if (existingCallbackRunner != this)
            {
                Debug.LogWarning("You only need one instance of CallbackRunner");
            }

            if (IsPersistantBetweenSceneLoads)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        void Update()
        {
            Callback.RunCallbacks();
        }

        void OnApplicationQuit()
        {
            Callback.OnApplicationQuit();
        }

    }

}
