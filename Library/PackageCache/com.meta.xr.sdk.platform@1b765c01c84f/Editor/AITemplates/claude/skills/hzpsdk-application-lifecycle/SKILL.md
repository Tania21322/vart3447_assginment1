---
name: Horizon Platform SDK — Implement Application Lifecycle (Launch Details / Deeplinks)
description: Use this skill when handling app launch types, deeplinks, invite-driven launches, or warm-start intent changes in a Meta Quest Unity app using the Horizon Platform SDK. Covers GetLaunchDetails for cold start, the launch_intent_changed callback for warm start, the LaunchType enum (Normal, Invite, Deeplink), and LogDeeplinkResult for analytics.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Application Lifecycle Implementation Guide

You are an expert in implementing app lifecycle handling for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). The Application Lifecycle API tells you **how the user got into your app** — a normal launch from their library, accepting an invite from a friend, or following a deeplink from another app.

## Why This Matters

| | |
|---|---|
| **Resume into the right place** | When a user accepts an invite from outside your app, take them straight to that lobby — don't dump them at your main menu. |
| **Track deeplink success** | The platform wants to know if your app honored the deeplink. Report success/failure via `LogDeeplinkResult`. |
| **Warm-start intent changes** | When the app is already running and the user accepts a new invite, the platform delivers a `launch_intent_changed` event. Subscribe to it. |

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Set up Destinations** in the Developer Dashboard (used for deeplinks). See the `hzpsdk-group-presence` skill.
3. **Note your App ID**
4. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

The `ApplicationLifecycle` static class lives in `Oculus.Platform`. The `LaunchDetails` model lives in `Oculus.Platform.Models`. The `LaunchType` and `LaunchResult` enums live in `Oculus.Platform`.

## Step 1: Initialize and Subscribe to Intent Changes

Subscribe to `launch_intent_changed` **immediately after init**, before fetching launch details, so you don't miss any pending events.

```csharp
async void Start()
{
#if UNITY_EDITOR
    var msg = await Core.AsyncInitialize(appId, "standalone");
#else
    var msg = await Core.AsyncInitialize(appId);
#endif
    if (msg.IsError) return;

    // Warm-start hook: fires when a new launch intent arrives while the app is running
    ApplicationLifecycle.SetLaunchIntentChangedNotificationCallback(OnLaunchIntentChanged);

    // Cold-start: read the launch details that brought us here
    await ProcessLaunchDetails();
    isInitialized = true;
}
```

## Step 2: Read Launch Details (Cold Start)

```csharp
private async Task ProcessLaunchDetails()
{
    var msg = await ApplicationLifecycle.GetLaunchDetailsRequest();
    if (msg.IsError) return;

    LaunchDetails details = msg.Data;
    Debug.Log($"Launched as {details.LaunchType}, dest={details.DestinationApiName}, deeplink={details.DeeplinkMessage}");

    HandleLaunchDetails(details);
}

private void HandleLaunchDetails(LaunchDetails details)
{
    switch (details.LaunchType)
    {
        case LaunchType.Normal:
            // Standard launch from the user's library — go to main menu
            GoToMainMenu();
            break;

        case LaunchType.Invite:
            // User accepted an invite — take them to the lobby
            TravelToLobby(
                details.DestinationApiName,
                details.LobbySessionID,
                details.MatchSessionID,
                details.DeeplinkMessage,
                details.UsersOptional);
            ReportDeeplinkResult(details.TrackingID, success: true);
            break;

        case LaunchType.Deeplink:
            // Launched from another app's `Application.LaunchOtherApp` call
            HandleDeeplink(details.DeeplinkMessage, details.LaunchSource);
            ReportDeeplinkResult(details.TrackingID, success: true);
            break;

        case LaunchType.Coordinated:  // deprecated
        case LaunchType.Unknown:
        default:
            GoToMainMenu();
            break;
    }
}
```

### `LaunchDetails` Fields You'll Use

| Field | When relevant |
|-------|---------------|
| `LaunchType` | Always — tells you how the user arrived |
| `DeeplinkMessage` | Opaque string your app set via `Application.LaunchOtherApp` or `GroupPresence.SetDeeplinkMessageOverride` |
| `DestinationApiName` | The Destination the user wants to go to |
| `LobbySessionID` | The lobby session for invite/deeplink |
| `MatchSessionID` | The match session for invite/deeplink |
| `LaunchSource` | Which surface the deeplink came from (events, rich presence, etc.) |
| `TrackingID` | Pass to `LogDeeplinkResult` to report success/failure |
| `UsersOptional` | If provided, users the launcher wants to be with |

## Step 3: Report Deeplink Result

After handling a deeplink, **always** call `LogDeeplinkResult` so the platform can track success rates.

```csharp
private async void ReportDeeplinkResult(string trackingId, bool success)
{
    if (string.IsNullOrEmpty(trackingId)) return;
    var result = success ? LaunchResult.Success : LaunchResult.FailedRoomFull;
    await ApplicationLifecycle.LogDeeplinkResultRequest(trackingId, result);
}
```

### `LaunchResult` Values (use the most accurate)

| Value | When |
|-------|------|
| `Success` | Took user where they wanted to go |
| `FailedRoomFull` | Lobby full |
| `FailedGameAlreadyStarted` | Match in progress, can't join |
| `FailedGameNotFound` | Lobby/match no longer exists |
| `FailedUserDeclined` | User chose not to follow the deeplink (rare) |
| `FailedOtherReason` | Anything else |

## Step 4: Handle Warm-Start Intent Changes

If the app is already running and the user accepts a new invite (or another app calls `LaunchOtherApp` targeting yours), the platform delivers a `launch_intent_changed` notification. Read updated details with `GetLaunchDetailsRequest`.

```csharp
private async void OnLaunchIntentChanged(Message<string> msg)
{
    if (msg.IsError) return;

    Debug.Log($"Warm-start intent changed: {msg.Data}");
    // Re-fetch the latest launch details to act on
    var detailsMsg = await ApplicationLifecycle.GetLaunchDetailsRequest();
    if (!detailsMsg.IsError)
    {
        HandleLaunchDetails(detailsMsg.Data);
    }
}
```

> The string payload is opaque — always re-fetch full details with `GetLaunchDetailsRequest`.

## Complete Application Lifecycle Manager

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class AppLifecycleManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";

    public event Action<LaunchDetails> LaunchProcessed;

    async void Start()
    {
        var initMsg = await Core.AsyncInitialize(appId);
        if (initMsg.IsError) { Debug.LogError(initMsg.GetError().Message); return; }

        ApplicationLifecycle.SetLaunchIntentChangedNotificationCallback(OnIntentChanged);
        await ProcessCurrentLaunch();
    }

    private async Task ProcessCurrentLaunch()
    {
        var msg = await ApplicationLifecycle.GetLaunchDetailsRequest();
        if (msg.IsError) return;
        LaunchProcessed?.Invoke(msg.Data);
        await ReportSuccessIfNeeded(msg.Data);
    }

    private async void OnIntentChanged(Message<string> msg)
    {
        if (msg.IsError) return;
        var detailsMsg = await ApplicationLifecycle.GetLaunchDetailsRequest();
        if (detailsMsg.IsError) return;
        LaunchProcessed?.Invoke(detailsMsg.Data);
        await ReportSuccessIfNeeded(detailsMsg.Data);
    }

    private async Task ReportSuccessIfNeeded(LaunchDetails d)
    {
        if (d.LaunchType == LaunchType.Invite || d.LaunchType == LaunchType.Deeplink)
        {
            if (!string.IsNullOrEmpty(d.TrackingID))
            {
                await ApplicationLifecycle.LogDeeplinkResultRequest(d.TrackingID, LaunchResult.Success);
            }
        }
    }
}
```

Then in your gameplay code, subscribe to `LaunchProcessed`:

```csharp
appLifecycle.LaunchProcessed += details =>
{
    switch (details.LaunchType)
    {
        case LaunchType.Invite: TravelToLobby(details); break;
        case LaunchType.Deeplink: HandleDeeplink(details); break;
        default: GoToMainMenu(); break;
    }
};
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `ApplicationLifecycle.GetLaunchDetailsRequest()` | `Request<LaunchDetails>` | Get the current launch intent (cold or last warm) |
| `ApplicationLifecycle.LogDeeplinkResultRequest(trackingId, result)` | `Request` | Report whether your app honored a deeplink |
| `ApplicationLifecycle.SetLaunchIntentChangedNotificationCallback(cb)` | (void) | Subscribe to warm-start intent changes |

### Models

| Type | Key fields |
|------|------------|
| `LaunchDetails` | `LaunchType`, `DeeplinkMessage`, `DestinationApiName`, `LobbySessionID`, `MatchSessionID`, `LaunchSource`, `TrackingID`, `UsersOptional` |

### Enums

| Enum | Values |
|------|--------|
| `LaunchType` | `Normal`, `Invite`, `Deeplink`, `Coordinated` (deprecated), `Unknown` |
| `LaunchResult` | `Success`, `FailedRoomFull`, `FailedGameAlreadyStarted`, `FailedGameNotFound`, `FailedUserDeclined`, `FailedOtherReason`, `Unknown` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Always going to main menu regardless of `LaunchType` | Branch on `LaunchType` and route invite/deeplink launches to the right destination. |
| Not subscribing to `launch_intent_changed` | Warm-start invites won't be handled. Subscribe immediately after init. |
| Skipping `LogDeeplinkResult` | The platform tracks deeplink success. Always log a result for `Invite` and `Deeplink` launches with a `TrackingID`. |
| Parsing the warm-start `string` payload | It's opaque. Always re-fetch with `GetLaunchDetailsRequest`. |
| Forgetting to null-check `UsersOptional` | Nullable. Check before iterating. |
| Calling Lifecycle APIs before init | Always check `Core.IsInitialized()`. |
| Treating `Coordinated` as active | Deprecated. Treat it like `Unknown`. |
| Calling `GetLaunchDetailsRequest` repeatedly hoping for updates | Cold-start details only change on warm-start; subscribe to `launch_intent_changed` instead. |

## Coding Rules

When implementing Application Lifecycle using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` first.
- **Subscribe to `SetLaunchIntentChangedNotificationCallback` immediately after init**, before any pending events deliver.
- Then call `GetLaunchDetailsRequest` to handle the cold-start launch.

### Routing
- Branch on `LaunchType`. Invite and Deeplink launches should bypass the main menu.
- For `Invite`: travel directly to the destination/lobby/match using the IDs from `LaunchDetails`.
- For `Deeplink`: parse the opaque `DeeplinkMessage` per your app's protocol and route accordingly.

### Reporting
- For any launch with a `TrackingID`, call `LogDeeplinkResult` with the most accurate `LaunchResult` enum value. The platform uses this to measure deeplink success rates.

### Warm Start
- The warm-start callback string payload is opaque. Always re-fetch via `GetLaunchDetailsRequest`.

### Coordination with Group Presence
- After processing an Invite/Deeplink launch, set `GroupPresence` to reflect the user's new location (see `hzpsdk-group-presence`).

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest App-to-App Travel & Deeplinks (Unity)](https://developer.oculus.com/documentation/unity/ps-app-to-app-travel/)
- [Destinations Overview](https://developer.oculus.com/documentation/unity/ps-destinations-overview/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/application_lifecycle/`
- Related skills: `hzpsdk-group-presence`, `hzpsdk-users`
