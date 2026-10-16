---
name: Horizon Platform SDK — Implement Rich Presence (DEPRECATED — use Group Presence)
description: Use this skill when maintaining legacy Rich Presence code in a Meta Quest Unity app using the Horizon Platform SDK. Covers the deprecated RichPresence.Set / Clear / GetDestinations APIs and how to migrate to Group Presence (the recommended replacement). New code should use Group Presence directly.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Rich Presence Implementation Guide

> ⚠️ **DEPRECATED** — Rich Presence is deprecated in favor of **Group Presence**. For all new code, use the `hzpsdk-group-presence` skill.
>
> Use this skill only if you're maintaining a legacy app that already uses Rich Presence and need to understand or migrate the existing calls.

The `RichPresence` API in Unity is now a **thin shim** that forwards to the underlying `group_presence` module — even the API endpoint names show this (`PlatformClient.MakeRequest("group_presence", ...)`). New code should call `GroupPresence.*` directly with `GroupPresenceOptions`.

## Why Rich Presence Was Deprecated

Group Presence supersedes it because:

| Capability | Rich Presence (legacy) | Group Presence (recommended) |
|------------|------------------------|-------------------------------|
| Set destination | `Set(RichPresenceOptions)` | `Set(GroupPresenceOptions)` |
| Lobby/match session IDs | Limited | Full support (`LobbySessionId`, `MatchSessionId`) |
| Joinability flag | Limited | `IsJoinable` field |
| Invite panel | Not supported | `LaunchInvitePanel` |
| Join intent callback | Not supported | `SetJoinIntentReceivedNotificationCallback` |
| Multiplayer error dialog | Not supported | `LaunchMultiplayerErrorDialog` |
| Rejoin dialog | Not supported | `LaunchRejoinDialog` |

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **For new code**: read the `hzpsdk-group-presence` skill instead
3. **For legacy maintenance**: continue with this skill, but plan a migration

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
```

The `RichPresence` static class lives in `Oculus.Platform`. The `RichPresenceOptions` builder lives in `Oculus.Platform`. The `Destination` and `DestinationList` models live in `Oculus.Platform.Models`.

## Legacy API Surface

### Set Rich Presence

```csharp
public async Task SetRichPresence(string destinationApiName)
{
    if (!Core.IsInitialized()) return;

    var opts = new RichPresenceOptions();
    opts.SetDestinationApiName(destinationApiName);
    // (other RichPresenceOptions setters depending on your legacy code)

    var msg = await RichPresence.Set(opts);
    if (msg.IsError)
    {
        Debug.LogError($"RichPresence.Set: {msg.GetError().Message}");
    }
}
```

### Clear Rich Presence

```csharp
await RichPresence.Clear();
```

### List Destinations

```csharp
var msg = await RichPresence.GetDestinations();
if (!msg.IsError)
{
    foreach (var dest in msg.Data) Debug.Log($"{dest.ApiName}: {dest.DisplayName}");
}
```

## Migration to Group Presence

The migration is mostly a **rename + builder swap**. The semantic meaning of "destination" is preserved.

### Before (Rich Presence)

```csharp
var opts = new RichPresenceOptions();
opts.SetDestinationApiName("main_lobby");
await RichPresence.Set(opts);
```

### After (Group Presence)

```csharp
var opts = new GroupPresenceOptions();
opts.SetDestinationApiName("main_lobby");
opts.SetIsJoinable(true);                  // new: explicit joinability
opts.SetLobbySessionId("abc123");          // new: lobby grouping
// opts.SetMatchSessionId("xyz");          // optional: gameplay grouping
await GroupPresence.Set(opts);
```

### Step-by-Step Migration

1. Find all uses of `RichPresence.Set` / `Clear` / `GetDestinations` in your codebase
2. Replace `RichPresenceOptions` → `GroupPresenceOptions`
3. Replace `RichPresence.X(...)` → `GroupPresence.X(...)`
4. Add explicit `SetIsJoinable(...)` and (if you have multiplayer) `SetLobbySessionId(...)`
5. Subscribe to `GroupPresence.SetJoinIntentReceivedNotificationCallback` to handle accepted invites — this didn't exist on Rich Presence
6. Test the invite flow end-to-end with another account

## Complete Migration Helper (legacy → modern)

If you have a wrapper that touches `RichPresence`, consolidate it:

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class PresenceManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";
    private bool isInitialized;

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        if (msg.IsError) return;

        // New: register the join-intent callback (required for invites to work)
        GroupPresence.SetJoinIntentReceivedNotificationCallback(OnJoinIntent);
        isInitialized = true;
    }

    public async Task SetPresence(string destinationApiName, string lobbyId = null, bool joinable = true)
    {
        if (!isInitialized) return;

        var opts = new GroupPresenceOptions();
        opts.SetDestinationApiName(destinationApiName);
        opts.SetIsJoinable(joinable);
        if (!string.IsNullOrEmpty(lobbyId)) opts.SetLobbySessionId(lobbyId);

        await GroupPresence.Set(opts);
    }

    public async Task ClearPresence() => await GroupPresence.Clear();

    private void OnJoinIntent(Message<GroupPresenceJoinIntent> msg)
    {
        if (msg.IsError) return;
        TravelTo(msg.Data.DestinationApiName, msg.Data.LobbySessionId);
    }
}
```

## API Reference (Legacy)

| Method | Returns | Description | Replacement |
|--------|---------|-------------|-------------|
| `RichPresence.Set(opts)` | `Request` | Set rich presence fields | `GroupPresence.Set(GroupPresenceOptions)` |
| `RichPresence.Clear()` | `Request` | Clear presence | `GroupPresence.Clear()` |
| `RichPresence.GetDestinations()` | `Request<DestinationList>` | List configured destinations | `GroupPresence.GetDestinations()` |
| `RichPresence.GetNextDestinationListPage(list)` | `Request<DestinationList>` | Pagination | `GroupPresence.GetNextDestinationListPage(list)` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Using Rich Presence for new code | Use Group Presence (`hzpsdk-group-presence`) instead. |
| Migrating `Set` but not setting `IsJoinable` / `LobbySessionId` | Group Presence requires explicit values for these. Defaults can break the invite flow. |
| Migrating but not adding `SetJoinIntentReceivedNotificationCallback` | Without this, accepted invites do nothing. |
| Calling Rich Presence APIs before init | Always check `Core.IsInitialized()`. |

## Coding Rules

When working with Rich Presence using the Horizon Platform SDK:

### For New Code
- **Don't use Rich Presence.** Use Group Presence (`hzpsdk-group-presence`) instead.

### For Legacy Code
- Plan a migration to Group Presence; the surface area is small.
- Add `IsJoinable` and `LobbySessionId` explicitly during migration — Group Presence needs these for the invite flow.
- Subscribe to `GroupPresence.SetJoinIntentReceivedNotificationCallback` after migration so accepted invites do something.

### Initialization
- Always call `Core.AsyncInitialize(appId)` first.
- Gate every API call with `Core.IsInitialized()`.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Group Presence Documentation (recommended replacement)](https://developers.meta.com/horizon/documentation/unity/ps-group-presence-overview/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/rich_presence/`
- Related skills: **`hzpsdk-group-presence` (recommended replacement)**
