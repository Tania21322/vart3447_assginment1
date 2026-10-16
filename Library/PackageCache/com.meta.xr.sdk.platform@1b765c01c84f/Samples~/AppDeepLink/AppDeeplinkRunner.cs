// (c) Meta Platforms, Inc. and affiliates. Confidential and proprietary.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class AppDeeplinkRunner : MonoBehaviour
{
    // YOUR APP IDS
    const ulong UNITY_COMPANION_APP_ID = 3535750239844224;
    const ulong UNREAL_COMPANION_APP_ID = 4055411724486843;

    public Text UILaunchType;
    public Text UILaunchSource;
    public Text UIDeeplinkMessage;
    public Text UIMessageToSend;

    // this is the message that will be sent to
    // the launched apps as a DeeplinkMessage
    const string MESSAGE = "MSG_UNITY_SAMPLE";

    Oculus.Platform.Models.LaunchDetails _details;

    // Track previous thumbstick state to detect "press" moments
    bool _wasThumbstickDown;
    bool _wasThumbstickUp;

    void Start()
    {
        // init ovr platform
        if (UnityEngine.Application.platform == RuntimePlatform.Android)
            if (!Oculus.Platform.Core.IsInitialized())
                Oculus.Platform.Core.Initialize();

        UIMessageToSend.text += $" {MESSAGE}";
    }

    void Update()
    {
        // Get current input states
        var gamepad = Gamepad.current;
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        // Touch Controller A, Keyboard Ctrl, Mouse LMB
        bool fire1Pressed = (gamepad != null && gamepad.buttonSouth.wasPressedThisFrame) ||
                            (keyboard != null && keyboard.ctrlKey.wasPressedThisFrame) ||
                            (mouse != null && mouse.leftButton.wasPressedThisFrame);
        if (fire1Pressed)
            LaunchSelf();

        // Check thumbstick vertical axis for up/down
        float vertical = gamepad != null ? gamepad.leftStick.y.ReadValue() : 0f;
        bool isThumbstickDown = vertical < -0.8f;
        bool isThumbstickUp = vertical > 0.8f;

        // Left Touch Controller Down, Keyboard Alt, Mouse RMB
        bool fire2Pressed = (gamepad != null && gamepad.buttonEast.wasPressedThisFrame) ||
                            (keyboard != null && keyboard.altKey.wasPressedThisFrame) ||
                            (mouse != null && mouse.rightButton.wasPressedThisFrame);
        bool thumbstickDownPressed = isThumbstickDown && !_wasThumbstickDown;
        if (thumbstickDownPressed || fire2Pressed)
            LaunchUnityDeeplinkSample();

        // Left Touch Controller Up, Keyboard Shift, Mouse Middle
        bool fire3Pressed = (gamepad != null && gamepad.buttonWest.wasPressedThisFrame) ||
                            (keyboard != null && keyboard.shiftKey.wasPressedThisFrame) ||
                            (mouse != null && mouse.middleButton.wasPressedThisFrame);
        bool thumbstickUpPressed = isThumbstickUp && !_wasThumbstickUp;
        if (thumbstickUpPressed || fire3Pressed)
            LaunchUnrealDeeplinkSample();

        _wasThumbstickDown = isThumbstickDown;
        _wasThumbstickUp = isThumbstickUp;

        if (Application.platform != RuntimePlatform.Android)
            return;

        var launchDetails = Oculus.Platform.ApplicationLifecycle.GetLaunchDetails();
        if (launchDetails == _details)
            return;

        UILaunchType.text = $"Launch Type: {launchDetails.LaunchType}";
        UILaunchSource.text = $"Launch Source: {launchDetails.LaunchSource}";
        UIDeeplinkMessage.text = $"Deeplink Message: {launchDetails.DeeplinkMessage}";

        _details = launchDetails;
    }

    public void LaunchUnrealDeeplinkSample()
    {
        Debug.Log($"LaunchUnrealApp({UNREAL_COMPANION_APP_ID})");
        Launch(UNREAL_COMPANION_APP_ID);
    }

    public void LaunchSelf()
    {
        if (ulong.TryParse(Oculus.Platform.PlatformSettings.MobileAppID, out ulong appId))
        {
            Debug.Log($"LaunchSelf({appId})");
            Launch(appId);
        }
    }

    public void LaunchUnityDeeplinkSample()
    {
        Debug.Log($"LaunchUnityApp({UNITY_COMPANION_APP_ID})");
        Launch(UNITY_COMPANION_APP_ID);
    }

    void Launch(ulong id)
    {
        var options = new Oculus.Platform.ApplicationOptions();
        options.SetDeeplinkMessage(MESSAGE);
        Oculus.Platform.Application.LaunchOtherApp(id, options);
    }
}
