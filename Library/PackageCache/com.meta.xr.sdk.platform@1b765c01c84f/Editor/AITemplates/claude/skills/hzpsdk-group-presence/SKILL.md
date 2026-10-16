---
name: Horizon Platform SDK — Implement Group Presence (Multiplayer/Invites)
description: Use this skill when implementing multiplayer presence, joinability, invites, sessions, destinations, or rejoin/error dialogs in a Meta Quest Unity app using the Horizon Platform SDK. Covers GroupPresence.Set with destination/lobby/match session IDs, the invite panel, join intent callbacks, and the relationship between the platform's "travel" feature and your app's matchmaking.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Group Presence Implementation Guide

You are an expert in implementing Group Presence for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). Group Presence is what makes your app **socially discoverable** on the Quest platform: it tells the system where the user is in your app, whether others can join them, and powers invites and the "Recently Played With" list.

## Why This Matters

| | |
|---|---|
| **Powers cross-app travel** | Friends can see "X is playing the Boss Arena" in your app and tap to join. |
| **Required for invites** | Without setting presence, `LaunchInvitePanel` has nothing to invite to. |
| **Drives discoverability** | Users with the same lobby session ID show up to each other and as "Recently Played With". |
| **Immersive apps only** | Currently supported in immersive mode; not yet for 2D panel apps. |

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Create at least one Destination** in the Developer Dashboard. A Destination is a named, deep-linkable location in your app (e.g., `lobby`, `boss_arena`, `tutorial`). Note the **API Name**.
3. **Note your App ID**
4. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

> **Concept**: A Destination is a top-level location. A `lobby_session_id` is a specific session at that destination (e.g., a specific lobby instance). A `match_session_id` further narrows users into the same gameplay instance (e.g., a match within a lobby).

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

The `GroupPresence` static class lives in `Oculus.Platform`. Models (`Destination`, `ApplicationInvite`, `GroupPresenceJoinIntent`, `LaunchInvitePanelFlowResult`, etc.) and lists live in `Oculus.Platform.Models`. Options (`GroupPresenceOptions`, `InviteOptions`, `RosterOptions`, `MultiplayerErrorOptions`) live in `Oculus.Platform`.

## Step 1: Initialize and Register Notification Callbacks

Notification callbacks **must be registered immediately after init** (before the platform delivers any pending events). The most important ones:

- `SetJoinIntentReceivedNotificationCallback` — fires when a user accepts an invite or taps "Join" on the platform
- `SetInvitationsSentNotificationCallback` — fires when the user finishes the invite panel
- `SetLeaveIntentReceivedNotificationCallback` — fires when the user leaves a session via the platform UI

```csharp
async void Start()
{
#if UNITY_EDITOR
    var msg = await Core.AsyncInitialize(appId, "standalone");
#else
    var msg = await Core.AsyncInitialize(appId);
#endif
    if (msg.IsError) return;

    GroupPresence.SetJoinIntentReceivedNotificationCallback(OnJoinIntent);
    GroupPresence.SetInvitationsSentNotificationCallback(OnInvitationsSent);
    GroupPresence.SetLeaveIntentReceivedNotificationCallback(OnLeaveIntent);

    isInitialized = true;
}

private void OnJoinIntent(Message<GroupPresenceJoinIntent> msg)
{
    if (msg.IsError) return;
    var intent = msg.Data;
    Debug.Log($"User wants to join: dest={intent.DestinationApiName}, lobby={intent.LobbySessionId}, match={intent.MatchSessionId}");
    // Take the user to the requested destination/lobby/match
    TravelTo(intent.DestinationApiName, intent.LobbySessionId, intent.MatchSessionId, intent.DeeplinkMessage);
}
```

> **Critical**: respond to `OnJoinIntent` immediately. If the user can't be taken there (e.g., lobby is full), show a clear error message — don't silently ignore.

## Step 2: Set Group Presence

The recommended pattern is to call `GroupPresence.Set` with **all fields at once** (atomic update). Avoid the individual setters (`SetDestination`, `SetIsJoinable`, etc.) — they're only there for backward compat and can produce inconsistent intermediate states.

```csharp
public async Task EnterLobby(string lobbyId, bool isJoinable = true)
{
    if (!isInitialized) return;

    var options = new GroupPresenceOptions();
    options.SetDestinationApiName("main_lobby");
    options.SetLobbySessionId(lobbyId);
    options.SetIsJoinable(isJoinable);
    // Optional deeplink data your app understands
    options.SetDeeplinkMessageOverride($"lobby={lobbyId}");

    var msg = await GroupPresence.Set(options);
    if (msg.IsError)
    {
        Debug.LogError($"GroupPresence.Set failed: {msg.GetError().Message}");
    }
}

public async Task EnterMatch(string lobbyId, string matchId)
{
    var options = new GroupPresenceOptions();
    options.SetDestinationApiName("boss_arena");
    options.SetLobbySessionId(lobbyId);
    options.SetMatchSessionId(matchId);
    options.SetIsJoinable(false); // match in progress, not joinable mid-fight
    await GroupPresence.Set(options);
}

public async Task GoIdle()
{
    // Clear all presence when the user is in menus or idle
    await GroupPresence.Clear();
}
```

### When to use what

| Field | Purpose |
|-------|---------|
| `DestinationApiName` | Top-level location (must match a Destination configured in the Dashboard) |
| `LobbySessionId` | Identifies a specific lobby instance — same ID = same lobby visible to each other and "Recently Played With" |
| `MatchSessionId` | Identifies a specific match instance — same ID = playing together right now (does NOT show in roster) |
| `IsJoinable` | If false, others cannot invite the user. Set false when full / private. |
| `DeeplinkMessageOverride` | Opaque string your app understands. Use it to pass extra context for join intent. |

> **Lobby vs Match**: Two users in the same lobby can be in different matches (e.g., voice chat lobby with multiple matches running). Lobby drives the roster and "Recently Played With"; match does not.

## Step 3: Launch the Invite Panel

```csharp
public async Task OpenInvitePanel()
{
    if (!isInitialized) return;

    var options = new InviteOptions();
    // Optional: pre-suggest specific users
    // options.SetSuggestedUsers(new ulong[] { friendId1, friendId2 });

    var msg = await GroupPresence.LaunchInvitePanel(options);
    if (msg.IsError)
    {
        Debug.LogError($"LaunchInvitePanel: {msg.GetError().Message}");
    }
}
```

The `OnInvitationsSent` callback fires when the user finishes the panel and lists the invitees.

```csharp
private void OnInvitationsSent(Message<LaunchInvitePanelFlowResult> msg)
{
    if (msg.IsError) return;
    foreach (var user in msg.Data.InvitedUsers)
    {
        Debug.Log($"Invited: {user.DisplayName}");
    }
}
```

## Step 4: Direct-Send Invites (Programmatic)

If you have a custom UI and already know the user IDs (e.g., from `Users.GetLoggedInUserFriends`), bypass the panel:

```csharp
public async Task DirectInvite(ulong[] userIds)
{
    var msg = await GroupPresence.SendInvites(userIds);
    if (!msg.IsError)
    {
        Debug.Log($"Sent {msg.Data.InvitedUsers.Count} invites");
    }
}
```

> **Recommendation**: Prefer `LaunchInvitePanel` — it surfaces suggested friends and Recently Played With, which `SendInvites` doesn't.

## Step 5: Get Invitable Users

```csharp
var msg = await GroupPresence.GetInvitableUsers(new InviteOptions());
foreach (var user in msg.Data) Debug.Log(user.DisplayName);
```

## Step 6: Multiplayer Error Dialog

When something goes wrong (lobby full, network drop), use the platform's pre-localized error dialog rather than rolling your own.

```csharp
public async Task ShowLobbyFullError()
{
    var opts = new MultiplayerErrorOptions();
    opts.SetErrorKey(MultiplayerErrorErrorKey.DestinationUnavailable);
    await GroupPresence.LaunchMultiplayerErrorDialog(opts);
}
```

## Step 7: Rejoin Dialog

If a user disconnects from a match and you want to offer "Rejoin?", use the rejoin dialog.

```csharp
public async Task OfferRejoin(string lobbyId, string matchId, string destination)
{
    var msg = await GroupPresence.LaunchRejoinDialog(lobbyId, matchId, destination);
    if (!msg.IsError && msg.Data.RejoinSelected)
    {
        // User chose to rejoin — bring them back to the match
    }
}
```

## Step 8: List Configured Destinations

Useful at app start to confirm what destinations are configured server-side.

```csharp
var msg = await GroupPresence.GetDestinations();
foreach (var dest in msg.Data) Debug.Log($"{dest.ApiName}: {dest.DisplayName}");
```

## Complete Group Presence Manager Example

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class GroupPresenceManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";

    private bool isInitialized;
    public event Action<string, string, string, string> JoinIntentReceived; // (dest, lobby, match, deeplink)

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        if (msg.IsError) { Debug.LogError(msg.GetError().Message); return; }

        GroupPresence.SetJoinIntentReceivedNotificationCallback(OnJoinIntent);
        GroupPresence.SetInvitationsSentNotificationCallback(OnInvitationsSent);
        GroupPresence.SetLeaveIntentReceivedNotificationCallback(OnLeaveIntent);
        isInitialized = true;
    }

    public async Task EnterLobby(string destinationApiName, string lobbyId, bool joinable = true)
    {
        if (!isInitialized) return;
        var opts = new GroupPresenceOptions();
        opts.SetDestinationApiName(destinationApiName);
        opts.SetLobbySessionId(lobbyId);
        opts.SetIsJoinable(joinable);
        await GroupPresence.Set(opts);
    }

    public async Task EnterMatch(string destinationApiName, string lobbyId, string matchId)
    {
        var opts = new GroupPresenceOptions();
        opts.SetDestinationApiName(destinationApiName);
        opts.SetLobbySessionId(lobbyId);
        opts.SetMatchSessionId(matchId);
        opts.SetIsJoinable(false);
        await GroupPresence.Set(opts);
    }

    public async Task ClearPresence() => await GroupPresence.Clear();

    public async Task LaunchInvitePanel() =>
        await GroupPresence.LaunchInvitePanel(new InviteOptions());

    private void OnJoinIntent(Message<GroupPresenceJoinIntent> msg)
    {
        if (msg.IsError) return;
        var i = msg.Data;
        JoinIntentReceived?.Invoke(i.DestinationApiName, i.LobbySessionId, i.MatchSessionId, i.DeeplinkMessage);
    }

    private void OnInvitationsSent(Message<LaunchInvitePanelFlowResult> msg)
    {
        if (msg.IsError) return;
        Debug.Log($"Sent {msg.Data.InvitedUsers.Count} invites");
    }

    private void OnLeaveIntent(Message<GroupPresenceLeaveIntent> msg)
    {
        if (msg.IsError) return;
        Debug.Log("User wants to leave the current session");
    }
}
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `GroupPresence.Set(options)` | `Request` | Set all presence fields atomically (recommended) |
| `GroupPresence.Clear()` | `Request` | Clear current presence |
| `GroupPresence.LaunchInvitePanel(options)` | `Request<InvitePanelResultInfo>` | Open system invite UI |
| `GroupPresence.GetInvitableUsers(options)` | `Request<UserList>` | Friends + recently met, eligible to invite |
| `GroupPresence.SendInvites(userIds)` | `Request<SendInvitesResult>` | Programmatic invite (skip panel) |
| `GroupPresence.GetSentInvites()` | `Request<ApplicationInviteList>` | Invites the user has sent |
| `GroupPresence.LaunchMultiplayerErrorDialog(options)` | `Request` | System-localized error dialog |
| `GroupPresence.LaunchRejoinDialog(lobby, match, dest)` | `Request<RejoinDialogResult>` | "Rejoin?" prompt |
| `GroupPresence.LaunchRosterPanel(options)` | `Request` | Roster UI (rarely needed; system handles it) |
| `GroupPresence.GetDestinations()` | `Request<DestinationList>` | List configured Destinations |
| `GroupPresence.SetJoinIntentReceivedNotificationCallback(cb)` | (void) | Hook for "user wants to join" events |
| `GroupPresence.SetLeaveIntentReceivedNotificationCallback(cb)` | (void) | Hook for "user wants to leave" events |
| `GroupPresence.SetInvitationsSentNotificationCallback(cb)` | (void) | Hook for "user finished invite panel" |
| `GroupPresence.SetDestination(name)` | `Request` | Individual setter — prefer `Set()` |
| `GroupPresence.SetLobbySession(id)` | `Request` | Individual setter — prefer `Set()` |
| `GroupPresence.SetMatchSession(id)` | `Request` | Individual setter — prefer `Set()` |
| `GroupPresence.SetIsJoinable(bool)` | `Request` | Individual setter — prefer `Set()` |
| `GroupPresence.SetDeeplinkMessageOverride(msg)` | `Request` | Individual setter — prefer `Set()` |

### Models

| Type | Key fields |
|------|------------|
| `GroupPresenceOptions` | `DestinationApiName`, `LobbySessionId`, `MatchSessionId`, `IsJoinable`, `DeeplinkMessageOverride` |
| `GroupPresenceJoinIntent` | `DestinationApiName`, `LobbySessionId`, `MatchSessionId`, `DeeplinkMessage` |
| `Destination` | `ApiName`, `DisplayName`, `DeeplinkMessage`, `ShareableUri` |
| `ApplicationInvite` | `ID`, `Recipient`, `IsActive`, `LobbySessionId`, `MatchSessionId`, `DestinationOptional` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Using individual setters (`SetDestination`, `SetLobbySession`, …) for a multi-field update | Use `GroupPresence.Set(options)` with all fields at once for atomic updates. |
| Not registering `OnJoinIntent` immediately after init | Register all notification callbacks right after init, before any pending events deliver. |
| Treating `match_session_id` like `lobby_session_id` | They're distinct. Lobby drives roster/recently-played-with; match drives gameplay grouping. |
| Setting `IsJoinable=true` when the lobby is full | Update presence to `IsJoinable=false` as soon as the lobby fills. |
| Using a Destination API name that isn't in the Dashboard | Will silently fail. Always verify with `GetDestinations()` during dev. |
| Silently ignoring `OnJoinIntent` errors (lobby full, etc.) | Show a clear error via `LaunchMultiplayerErrorDialog` so the user knows why the join failed. |
| Calling Group Presence methods before init | Always check `Core.IsInitialized()`. |
| Forgetting to `Clear()` when the user goes idle/menu | Stale presence shows the user still "in lobby" to friends. Clear on menu/idle. |

## Coding Rules

When implementing Group Presence using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` before any GroupPresence call.
- **Register all notification callbacks immediately after init** (`SetJoinIntentReceivedNotificationCallback`, `SetInvitationsSentNotificationCallback`, `SetLeaveIntentReceivedNotificationCallback`).
- Gate every API call with `Core.IsInitialized()`.

### Setting Presence
- Always use `GroupPresence.Set(options)` with all fields at once. Don't call individual setters in sequence — that produces inconsistent intermediate states visible to other users' UIs.
- Update presence on every meaningful state transition: enter lobby, match start, lobby fills, leave to menu.
- Call `GroupPresence.Clear()` when the user returns to a non-multiplayer screen.

### Joinability Semantics
- `LobbySessionId`: same value = same lobby = visible roster + "Recently Played With".
- `MatchSessionId`: same value = same gameplay instance, but does NOT add to roster.
- `IsJoinable`: gates whether others can be invited via the system. Set false when full, private, or in cutscenes.

### Invites
- Prefer `LaunchInvitePanel` over `SendInvites` — the panel surfaces friends and recently played with.
- React to `OnInvitationsSent` to show a "Sent!" confirmation in your UI.

### Join Intent Handling
- `OnJoinIntent` fires when the user accepts an invite or taps Join from the platform. Respond immediately.
- If the user can't be taken there (full, ended, etc.), use `LaunchMultiplayerErrorDialog` for system-localized messaging.

### Rejoin Flow
- If a user disconnects from a match they want back into, use `LaunchRejoinDialog` to offer the rejoin prompt.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest Group Presence Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-group-presence-overview/)
- [Destinations Overview](https://developer.oculus.com/documentation/unity/ps-destinations-overview/)
- [Invokable Error Dialogs](https://developer.oculus.com/documentation/unity/ps-multiplayer-error-dialog/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/group_presence/GroupPresenceTester.cs`
- Related skills: `hzpsdk-users`, `hzpsdk-application-lifecycle`
