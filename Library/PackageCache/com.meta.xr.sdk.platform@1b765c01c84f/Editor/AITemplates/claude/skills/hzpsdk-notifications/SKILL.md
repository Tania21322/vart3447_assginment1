---
name: Horizon Platform SDK — Implement Device Notifications
description: Use this skill when implementing on-device notifications (toast + notification feed) in a Meta Quest Unity app using the Horizon Platform SDK. Covers DeviceNotificationConfig, action buttons, icons, deeplink intents, the ndid uniqueness rule, toast-only vs feed-persistent display, and the difference between device notifications and push notifications.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Notifications Implementation Guide

You are an expert in implementing device notifications for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). Device notifications surface as a system toast on the Quest and (optionally) persist in the notification feed.

## Device Notifications vs Push Notifications

| Type | Trigger | Use case |
|------|---------|----------|
| **Device notifications** (this skill) | Your app code calls `Notifications.DeviceNotification` while running | "Achievement unlocked!", "Friend joined your lobby", "Quest available" — surfaced *from your app* while it's running |
| **Push notifications** (separate `push_notification` package) | Server-sent, delivered while your app is **not** running | "Your friend invited you to play", "New event tonight" — re-engagement from server |

This skill covers **device notifications**. For server-side push, see the Push Notifications package separately.

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Note your App ID**
3. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

The `Notifications` static class lives in `Oculus.Platform`. The `DeviceNotificationConfig` builder, plus the `ActionDisplayType` and `ActionIcon` enums, live in `Oculus.Platform`.

## Step 1: Initialize the Platform

Same pattern as every other PSDK feature.

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

## Step 2: Send a Simple Notification

```csharp
public async Task NotifyAchievementUnlocked(string achievementName)
{
    if (!Core.IsInitialized()) return;

    var config = new DeviceNotificationConfig();
    config.SetTitle("Achievement Unlocked!");
    config.SetMessage($"You earned: {achievementName}");
    config.SetNdid($"achievement_{achievementName}_{DateTime.UtcNow.Ticks}"); // unique
    config.SetIsToastOnly(false); // also persist in notification feed

    var msg = await Notifications.DeviceNotification(config);
    if (msg.IsError)
    {
        Debug.LogError($"Notification failed: {msg.GetError().Message}");
    }
}
```

### The `ndid` Rule (Critical)

`Ndid` is the **Notification Delivery ID**. Each notification you send **must** have a unique `Ndid`. If you reuse one, the new notification is silently suppressed.

> **Pattern**: include a timestamp or counter in the `Ndid` for uniqueness:
> ```csharp
> config.SetNdid($"my_event_{DateTime.UtcNow.Ticks}");
> ```

## Step 3: Toast-Only vs Feed-Persistent

| `IsToastOnly` | Behavior |
|----------------|----------|
| `true` | Shows the toast, does NOT add to notification feed (transient) |
| `false` | Shows the toast AND persists in feed until dismissed (default) |

Use `true` for ephemeral status updates ("Score submitted!"); use `false` for things the user might want to revisit ("Daily quest complete").

## Step 4: Add a Media Attachment

```csharp
config.SetMediaAttachmentUri("https://your-cdn.com/badge.png");
```

The URI must be reachable from the device. Local file URIs are not supported — host the asset.

## Step 5: Add an Action Button

```csharp
public async Task NotifyWithJoinAction(string lobbyName)
{
    var config = new DeviceNotificationConfig();
    config.SetTitle("Lobby ready");
    config.SetMessage($"Your friends are waiting in {lobbyName}");
    config.SetNdid($"lobby_ready_{DateTime.UtcNow.Ticks}");

    config.SetActionTitle("Join");
    config.SetActionIcon(ActionIcon.Play);
    config.SetActionDisplayType(ActionDisplayType.Iconable);

    // Tapping the action launches your app via intent
    config.SetActionPackageName("com.yourstudio.yourapp");
    config.SetActionIntentData("yourapp://lobby/" + lobbyName);

    await Notifications.DeviceNotification(config);
}
```

### Action Icon Options

| Icon | Typical use |
|------|-------------|
| `Accept` | Accept invite/request |
| `Close` | Dismiss |
| `Destination` / `DestinationOutline` | Navigation |
| `Call` / `DismissCall` | Voice call accept/decline |
| `AddFriend` | Friend request |
| `Info` | More details |
| `Party` | Party/group action |
| `Play` | Start game/media |
| `FollowAccept` / `FollowReject` | Follow request |
| `Remove` | Delete |
| `Friends` | Friend-related |
| `Chat` | Open chat |
| `Travel` | Travel between apps |
| `Download` | Download content |
| `Check` | Confirm |
| `Share` | Share |

### Action Display Types

| `ActionDisplayType` | Visual |
|----------------------|--------|
| `Iconable` | Colored icon + label |
| `IconableColorless` | Monochrome icon + label |
| `TextOnly` | Label only, no icon |

### Action Targets

You can route the action button several ways. Pick one:

| Field set | Behavior |
|-----------|----------|
| `ActionPackageName` only | Opens that app's main activity |
| `ActionAppId` only | Opens that Quest app by App ID |
| `ActionPackageName` + `ActionIntentData` | Sends Android intent with data URI to that package |
| `ActionPackageName` + `ActionIntentData` + `ActionIntentExtras` (JSON) | Full intent with extras |

## Step 6: Custom App Icon (Cross-App Notifications)

When your app sends a notification on behalf of *another* app (rare; mostly for system-level integrations), set `AppPackageNameForAppIcon`:

```csharp
config.SetAppPackageNameForAppIcon("com.otherstudio.theirapp");
```

For most use cases, leave this unset — the notification uses your app's own icon.

## Complete Notifications Helper

```csharp
using Oculus.Platform;
using System;
using System.Threading.Tasks;
using UnityEngine;

public static class QuestNotifications
{
    private static int sequence;

    public static async Task<bool> Toast(string title, string message)
    {
        if (!Core.IsInitialized()) return false;

        var config = new DeviceNotificationConfig();
        config.SetTitle(title);
        config.SetMessage(message);
        config.SetNdid($"toast_{DateTime.UtcNow.Ticks}_{++sequence}");
        config.SetIsToastOnly(true);

        var msg = await Notifications.DeviceNotification(config);
        return !msg.IsError;
    }

    public static async Task<bool> Persistent(string title, string message, string mediaUri = null)
    {
        if (!Core.IsInitialized()) return false;

        var config = new DeviceNotificationConfig();
        config.SetTitle(title);
        config.SetMessage(message);
        config.SetNdid($"persist_{DateTime.UtcNow.Ticks}_{++sequence}");
        config.SetIsToastOnly(false);
        if (!string.IsNullOrEmpty(mediaUri)) config.SetMediaAttachmentUri(mediaUri);

        var msg = await Notifications.DeviceNotification(config);
        return !msg.IsError;
    }

    public static async Task<bool> WithAction(
        string title, string message,
        string actionLabel, ActionIcon icon, string targetPackage, string intentData = null)
    {
        if (!Core.IsInitialized()) return false;

        var config = new DeviceNotificationConfig();
        config.SetTitle(title);
        config.SetMessage(message);
        config.SetNdid($"action_{DateTime.UtcNow.Ticks}_{++sequence}");
        config.SetActionTitle(actionLabel);
        config.SetActionIcon(icon);
        config.SetActionDisplayType(ActionDisplayType.Iconable);
        config.SetActionPackageName(targetPackage);
        if (!string.IsNullOrEmpty(intentData)) config.SetActionIntentData(intentData);

        var msg = await Notifications.DeviceNotification(config);
        return !msg.IsError;
    }
}
```

Usage:

```csharp
await QuestNotifications.Toast("Saved", "Game saved successfully");
await QuestNotifications.Persistent("Daily Quest", "Defeat 10 enemies for a reward!", mediaUri: "https://cdn.example.com/quest.png");
await QuestNotifications.WithAction(
    "Friend Online",
    "Bob is now playing your game",
    actionLabel: "Invite",
    icon: ActionIcon.AddFriend,
    targetPackage: "com.yourstudio.yourapp",
    intentData: "yourapp://invite/bob");
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `Notifications.DeviceNotification(config)` | `Request` | Show a system toast and (optionally) add to notification feed |

### `DeviceNotificationConfig` Fields

| Field | Setter | Required | Notes |
|-------|--------|----------|-------|
| `Title` | `SetTitle` | Yes | Notification title |
| `Message` | `SetMessage` | Yes | Body text |
| `Ndid` | `SetNdid` | Yes | **Must be unique per notification** |
| `IsToastOnly` | `SetIsToastOnly` | No (default false) | True = toast only, no feed entry |
| `MediaAttachmentUri` | `SetMediaAttachmentUri` | No | Reachable URI for an image |
| `AppPackageNameForAppIcon` | `SetAppPackageNameForAppIcon` | No | Override app icon (rare) |
| `ActionDisplayType` | `SetActionDisplayType` | No | Iconable, IconableColorless, TextOnly |
| `ActionTitle` | `SetActionTitle` | No (required if any action set) | Action button label |
| `ActionIcon` | `SetActionIcon` | No | One of the `ActionIcon` enum values |
| `ActionAppId` | `SetActionAppId` | No | Open this Quest app on tap |
| `ActionPackageName` | `SetActionPackageName` | No | Open this Android package on tap |
| `ActionIntentData` | `SetActionIntentData` | No | Android intent data URI |
| `ActionIntentExtras` | `SetActionIntentExtras` | No | JSON string of intent extras |

### Enums

| Enum | Values |
|------|--------|
| `ActionDisplayType` | `Iconable`, `IconableColorless`, `TextOnly`, `Unknown` |
| `ActionIcon` | `Accept`, `Close`, `Destination`, `Call`, `DismissCall`, `AddFriend`, `Info`, `Party`, `Play`, `FollowAccept`, `FollowReject`, `Remove`, `Friends`, `Chat`, `DestinationOutline`, `Travel`, `Download`, `Check`, `Share`, `Unknown` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Reusing the same `Ndid` across notifications | Each `Ndid` is unique. The platform silently suppresses duplicates. Include a timestamp/counter. |
| Forgetting to set `Ndid` at all | Required. The notification will not be delivered. |
| Confusing device notifications with push notifications | This API fires **while your app is running**. For server-sent push to a not-running app, use the `push_notification` package. |
| Local file URIs for media attachments | Not supported. Host the image at an HTTPS URL. |
| Setting an action with no `ActionTitle` | The action button needs a label. |
| Setting both `ActionAppId` and `ActionPackageName` | Pick one. Both is undefined behavior. |
| Spamming notifications per frame | Rate-limit yourself; the user will hate you and the platform may throttle. |
| Calling Notifications before init | Always check `Core.IsInitialized()`. |
| Setting `IsToastOnly=true` for important persistent info | Toast-only means it disappears immediately. Use `false` for things the user should be able to revisit. |

## Coding Rules

When implementing Device Notifications using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` before any Notifications call.
- Gate every API call with `Core.IsInitialized()`.

### NDID
- **Always set a unique `Ndid`**. Use timestamps and/or counters to guarantee uniqueness.
- Treat duplicate `Ndid` as a silent failure — the notification will simply not appear.

### Display Mode
- Use `IsToastOnly = true` for ephemeral status (auto-save complete, score submitted).
- Use `IsToastOnly = false` for actionable items the user might revisit (achievements, quest unlocks).

### Action Buttons
- Set `ActionTitle` whenever you set any action field.
- Pick one routing target: `ActionAppId` OR `ActionPackageName` (with optional intent data/extras).
- Choose `ActionDisplayType` based on visual design — `Iconable` for colorful, `TextOnly` for minimal.

### Push vs Device
- This API delivers in-app notifications. For server-sent push to non-running apps, use the separate `push_notification` package.

### Rate Limiting
- Don't fire notifications per frame or per gameplay tick. Batch related events; show one summary.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).

## Useful Links

- [Meta Quest Notifications Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-notifications/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/notifications/NotificationsTester.cs`
