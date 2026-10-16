---
name: Horizon Platform SDK — Implement Application (Versions, Self-Update, Launch Other Apps)
description: Use this skill when implementing app version queries, self-update download/install flows, or cross-app launching in a Meta Quest Unity app using the Horizon Platform SDK. Covers GetVersion (current vs latest), StartAppDownload + CheckAppDownloadProgress + InstallAppUpdateAndRelaunch, LaunchOtherApp with deeplink options, and the auto-relaunch behavior.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Application Implementation Guide

You are an expert in implementing app version, self-update, and cross-app launch features for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). The Application API lets your app query its installed and available versions, download and install updates from inside the app, and launch other Quest apps.

## What This API Does

| | |
|---|---|
| **Query app version** | Compare installed version with the latest available; show "Update available" |
| **In-app self-update** | Download an update, monitor progress, install it (which exits and relaunches your app) |
| **Cross-app launch** | Take the user to another Quest app, optionally with a deeplink message |

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Note your App ID** (and the App IDs of any apps you want to launch from yours)
3. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

The `Application` static class lives in `Oculus.Platform`. Models (`ApplicationVersion`, `AppDownloadResult`, `AppDownloadProgressResult`) live in `Oculus.Platform.Models`. The `ApplicationOptions` builder lives in `Oculus.Platform`. Status enums (`AppStatus`, `AppInstallResult`) live in `Oculus.Platform`.

## Step 1: Initialize the Platform

```csharp
async void Start()
{
#if UNITY_EDITOR
    var msg = await Core.AsyncInitialize(appId, "standalone");
#else
    var msg = await Core.AsyncInitialize(appId);
#endif
    if (!msg.IsError) isInitialized = true;
}
```

Always check `Core.IsInitialized()` before any Application call.

## Step 2: Check for Updates

```csharp
public async Task<bool> IsUpdateAvailable()
{
    if (!Core.IsInitialized()) return false;

    var msg = await Application.GetVersion();
    if (msg.IsError)
    {
        Debug.LogError($"GetVersion failed: {msg.GetError().Message}");
        return false;
    }

    ApplicationVersion v = msg.Data;
    Debug.Log($"Installed: {v.CurrentName} ({v.CurrentCode}), Latest: {v.LatestName} ({v.LatestCode})");
    return v.LatestCode > v.CurrentCode;
}
```

`ReleaseDate` is Unix epoch seconds. Convert before displaying:

```csharp
DateTime releasedAt = DateTimeOffset.FromUnixTimeSeconds(v.ReleaseDate).UtcDateTime;
```

## Step 3: In-App Self-Update Flow

The full flow is **download → monitor → install (which exits and relaunches your app)**. Build it as a state machine in your UI.

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

public class SelfUpdateController : MonoBehaviour
{
    public async Task<bool> DownloadAndInstall(string deeplinkOnReturn = null)
    {
        if (!Core.IsInitialized()) return false;

        // 1) Confirm an update is available
        var verMsg = await Application.GetVersion();
        if (verMsg.IsError) return false;
        if (verMsg.Data.LatestCode <= verMsg.Data.CurrentCode)
        {
            Debug.Log("Already up to date.");
            return false;
        }

        // 2) Start the download
        var startMsg = await Application.StartAppDownload();
        if (startMsg.IsError)
        {
            Debug.LogError($"StartAppDownload: {startMsg.GetError().Message}");
            return false;
        }

        // 3) Poll progress and update UI
        StartCoroutine(PollDownloadProgress());

        // 4) Wait for the start request to fully complete
        // (StartAppDownload returns once the download is finished)
        // 5) Install the downloaded update — this EXITS your app
        var opts = new ApplicationOptions();
        if (!string.IsNullOrEmpty(deeplinkOnReturn))
            opts.SetDeeplinkMessage(deeplinkOnReturn);

        var installMsg = await Application.InstallAppUpdateAndRelaunch(opts);
        if (installMsg.IsError)
        {
            Debug.LogError($"Install: {installMsg.GetError().Message}");
            return false;
        }
        // App will exit after this. Code below this line generally won't run.
        return true;
    }

    private IEnumerator PollDownloadProgress()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.5f);
            Application.CheckAppDownloadProgress().OnComplete(msg =>
            {
                if (msg.IsError) return;
                AppDownloadProgressResult p = msg.Data;
                Debug.Log($"Download status: {p.StatusCode}, {p.DownloadBytes} bytes");
                // p.StatusCode is `AppStatus`: Downloading, Installing, Installed, etc.
                UpdateProgressBar(p);
            });
        }
    }

    private void UpdateProgressBar(AppDownloadProgressResult p)
    {
        // Hook into your UI
    }

    public async Task Cancel()
    {
        var msg = await Application.CancelAppDownload();
        if (!msg.IsError) Debug.Log("Download cancelled");
    }
}
```

> **Important**: `InstallAppUpdateAndRelaunch` causes your app to **exit**. Save user state to disk before calling it. Use the optional `deeplinkOnReturn` to drop the user back into the same place after the relaunch (read it via `ApplicationLifecycle.GetLaunchDetails` — see the `hzpsdk-application-lifecycle` skill).

## Step 4: Launch Another App

```csharp
public async Task<bool> LaunchApp(ulong otherAppId, string deeplinkMessage = null)
{
    if (!Core.IsInitialized()) return false;

    ApplicationOptions opts = null;
    if (!string.IsNullOrEmpty(deeplinkMessage))
    {
        opts = new ApplicationOptions();
        opts.SetDeeplinkMessage(deeplinkMessage);
    }

    var msg = await Application.LaunchOtherApp(otherAppId, opts);
    if (msg.IsError)
    {
        Debug.LogError($"LaunchOtherApp({otherAppId}): {msg.GetError().Message}");
        return false;
    }

    Debug.Log($"Launched app {otherAppId}, response: {msg.Data}");
    return true;
}
```

If the user doesn't have the target app installed, the platform **automatically takes them to that app's Store page** instead. No special handling needed.

The receiving app reads your `deeplinkMessage` via `ApplicationLifecycle.GetLaunchDetails()` (`LaunchType.Deeplink`).

## Complete Application Manager

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class ApplicationHelper : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";

    private bool isInitialized;

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        isInitialized = !msg.IsError;
    }

    public async Task<ApplicationVersion> GetVersion()
    {
        if (!isInitialized) return null;
        var msg = await Application.GetVersion();
        return msg.IsError ? null : msg.Data;
    }

    public async Task<bool> HasUpdate()
    {
        var v = await GetVersion();
        return v != null && v.LatestCode > v.CurrentCode;
    }

    public async Task<bool> LaunchApp(ulong otherAppId, string deeplink = null)
    {
        if (!isInitialized) return false;
        ApplicationOptions opts = null;
        if (!string.IsNullOrEmpty(deeplink))
        {
            opts = new ApplicationOptions();
            opts.SetDeeplinkMessage(deeplink);
        }
        var msg = await Application.LaunchOtherApp(otherAppId, opts);
        return !msg.IsError;
    }
}
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `Application.GetVersion()` | `Request<ApplicationVersion>` | Installed + latest available version info |
| `Application.StartAppDownload()` | `Request<AppDownloadResult>` | Start downloading the latest update |
| `Application.CheckAppDownloadProgress()` | `Request<AppDownloadProgressResult>` | Poll download progress |
| `Application.CancelAppDownload()` | `Request<AppDownloadResult>` | Cancel an in-progress download |
| `Application.InstallAppUpdateAndRelaunch(opts)` | `Request<AppDownloadResult>` | Install update; exits and relaunches |
| `Application.LaunchOtherApp(appId, opts)` | `Request<string>` | Launch another Quest app, with optional deeplink |

### Models

| Type | Key fields |
|------|------------|
| `ApplicationVersion` | `CurrentCode`, `CurrentName`, `LatestCode`, `LatestName`, `Size`, `ReleaseDate` (Unix epoch seconds) |
| `AppDownloadProgressResult` | `StatusCode` (`AppStatus`), `DownloadBytes` |
| `AppDownloadResult` | `AppInstallResult`, …  |
| `ApplicationOptions` | `DeeplinkMessage` |

### Enums

| Enum | Values |
|------|--------|
| `AppStatus` | `EntitledNotDownloaded`, `Downloading`, `Installing`, `Installed`, `Uninstalling`, …  |
| `AppInstallResult` | `Success`, `Failure`, …  |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Treating `ReleaseDate` as ISO string | It's Unix epoch seconds. Convert with `DateTimeOffset.FromUnixTimeSeconds`. |
| Comparing version names instead of codes | Always compare `CurrentCode` vs `LatestCode` (integers). Names are display-only. |
| Forgetting `InstallAppUpdateAndRelaunch` exits the app | Save state to disk first. Anything in-memory is lost. |
| Not checking download progress | Without polling `CheckAppDownloadProgress`, the user has no feedback during the (potentially long) download. |
| Catching the exit from install | You can't. Your code stops running when the platform tears down the process. |
| Special-casing "app not installed" for `LaunchOtherApp` | The platform handles it — takes the user to the Store page. No special code needed. |
| Skipping `ApplicationOptions` for `LaunchOtherApp` | If you want the receiving app to know why you launched it, set `DeeplinkMessage`. The receiver reads via `ApplicationLifecycle.GetLaunchDetails`. |
| Calling Application APIs before init | Always check `Core.IsInitialized()`. |

## Coding Rules

When implementing Application using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` first.
- Gate every API call with `Core.IsInitialized()`.

### Version Comparison
- Always compare `CurrentCode` vs `LatestCode` (integers). Names are user-facing only.
- Convert `ReleaseDate` from Unix epoch seconds for display.

### Self-Update Flow
- `StartAppDownload` returns when the download finishes — use `CheckAppDownloadProgress` for live UI updates while it's in flight.
- **Save user state to disk before calling `InstallAppUpdateAndRelaunch`** — your app exits.
- Use `ApplicationOptions.SetDeeplinkMessage` to drop the user back into the same context after relaunch. Read it on the other side via `ApplicationLifecycle.GetLaunchDetails` (see `hzpsdk-application-lifecycle`).

### Launch Other Apps
- Don't pre-check if the user has the target app installed — the platform takes them to the Store page if not.
- Pass a deeplink message via `ApplicationOptions.SetDeeplinkMessage` for app-to-app travel context.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest Application Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-application/)
- [App-to-App Travel](https://developer.oculus.com/documentation/unity/ps-app-to-app-travel/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/application/`
- Related skills: `hzpsdk-application-lifecycle`
