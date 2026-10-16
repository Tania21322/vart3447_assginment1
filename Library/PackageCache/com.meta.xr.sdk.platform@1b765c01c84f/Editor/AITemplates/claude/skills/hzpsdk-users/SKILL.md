---
name: Horizon Platform SDK — Implement Users & Friends
description: Use this skill when implementing user identity, profile lookup, friends list, access tokens, or server-side identity verification in a Meta Quest Unity app using the Horizon Platform SDK. Covers GetLoggedInUser, Get(userID), friends, access tokens, user proof (nonce), org-scoped IDs, and the block/friend-request flows.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Users Implementation Guide

You are an expert in implementing user identity and friends features for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). The Users API gives you access to the signed-in user's ID, the friends list, profile details, access tokens for REST calls, and proof-of-identity for backend verification.

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Complete Data Use Checkup (DUC)** — required to access user platform features (friends, presence, etc.). Read about DUC at [developer.oculus.com/resources/publish-data-use](https://developer.oculus.com/resources/publish-data-use/).
3. **Note your App ID**
4. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

> **Important**: User IDs are **app-scoped**. The same physical user has a different `User.ID` in different apps. To identify a user across apps within the same org, use `Users.GetOrgScopedID(userID)`.

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

The `Users` static class lives in `Oculus.Platform`. Models (`User`, `UserList`, `OrgScopedID`, `UserProof`, `LinkedAccountList`, `BlockedUserList`, `SdkAccountList`) live in `Oculus.Platform.Models`. The `UserPresenceStatus` enum lives in `Oculus.Platform`.

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

Always check `Core.IsInitialized()` before any Users call.

## Step 2: Get the Logged-In User

`GetLoggedInUser` is **available offline** and is the single most common call — use it to get the player's app-scoped ID, Oculus ID alias, and profile picture URL.

```csharp
public async Task<User> GetCurrentUser()
{
    if (!Core.IsInitialized()) return null;

    var msg = await Users.GetLoggedInUser();
    if (msg.IsError)
    {
        Debug.LogError($"GetLoggedInUser failed: {msg.GetError().Message}");
        return null;
    }
    User me = msg.Data;
    Debug.Log($"Signed in as {me.OculusID} (ID: {me.ID})");
    return me;
}
```

> **`GetLoggedInUser` returns limited data**: `OculusID` (alias), `ID` (app-scoped), `ImageURL`. It does **not** return presence info. To get presence, pass the `ID` to `Users.Get(userId)`.

## Step 3: Get a User by ID

Use this for any user other than the current player, or to get the current player's full presence data.

```csharp
public async Task<User> GetUserById(ulong userId)
{
    if (!Core.IsInitialized()) return null;

    var msg = await Users.Get(userId);
    if (msg.IsError) return null;

    User u = msg.Data;
    Debug.Log($"User: {u.DisplayName ?? u.OculusID}, status: {u.PresenceStatus}, doing: {u.Presence}");
    return u;
}
```

`User.PresenceStatus` is `Online`, `Offline`, or `Unknown`. The human-readable `Presence` string is locale-dependent — display it as-is, don't parse it.

## Step 4: Friends List

```csharp
public async Task<List<User>> GetFriends()
{
    if (!Core.IsInitialized()) return new();

    var msg = await Users.GetLoggedInUserFriends();
    if (msg.IsError) return new();

    var friends = new List<User>(msg.Data);

    // Walk pages
    var page = msg.Data;
    while (page.HasNextPage)
    {
        var nextMsg = await Users.GetNextUserListPage(page);
        if (nextMsg.IsError) break;
        friends.AddRange(nextMsg.Data);
        page = nextMsg.Data;
    }
    return friends;
}
```

> "Friends" means **bidirectional followers** — both users must follow each other. One-way follows are not returned here.

## Step 5: Access Token (REST API Calls)

For server-to-server calls to `graph.oculus.com`, fetch an access token. Pass it as a Bearer token in your REST requests.

```csharp
public async Task<string> GetAccessToken()
{
    if (!Core.IsInitialized()) return null;
    var msg = await Users.GetAccessToken();
    if (msg.IsError) return null;
    return msg.Data;
}
```

> **Never log or persist** the access token. Treat it like a session credential — fetch fresh each time you need it.

## Step 6: Server-Side Identity Verification (User Proof)

Use this when your backend needs to confirm the player's identity. The flow:

1. Client calls `Users.GetUserProof()` to get a one-time `nonce`
2. Client sends `nonce` + `userID` to your backend
3. Your backend calls `https://graph.oculus.com/user_nonce_validate?nonce=NONCE&user_id=USER_ID&access_token=APP_ACCESS_TOKEN` to verify
4. Backend stores the verified user mapping

```csharp
public async Task<(string nonce, ulong userId)?> GetIdentityProof()
{
    var meMsg = await Users.GetLoggedInUser();
    if (meMsg.IsError) return null;

    var proofMsg = await Users.GetUserProof();
    if (proofMsg.IsError) return null;

    return (proofMsg.Data.Value, meMsg.Data.ID);
}

// Then POST { nonce, userId } to your backend.
```

> The nonce is **single-use**. Each call to `GetUserProof` returns a fresh one. The platform invalidates it after one validation attempt.

## Step 7: Org-Scoped IDs (Cross-App Within Same Org)

If your org publishes multiple apps and you want to recognize the same user across them, use the org-scoped ID instead of the app-scoped ID.

```csharp
public async Task<string> GetOrgScopedId(ulong appScopedUserId)
{
    var msg = await Users.GetOrgScopedID(appScopedUserId);
    if (msg.IsError) return null;
    return msg.Data.ID;
}
```

## Step 8: Launch Block / Unblock / Friend-Request Flows

These open the system UI for the user to confirm — your app can't block someone silently.

```csharp
await Users.LaunchBlockFlow(userId);
await Users.LaunchUnblockFlow(userId);
await Users.LaunchFriendRequestFlow(userId);

// List users blocked by the signed-in user
var blockedMsg = await Users.GetBlockedUsers();
```

## Complete Users Manager Example

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class UsersManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";

    private bool isInitialized;
    private User cachedMe;
    private List<User> cachedFriends = new();

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        if (msg.IsError) { Debug.LogError(msg.GetError().Message); return; }
        isInitialized = true;
        await LoadProfileAndFriends();
    }

    private async Task LoadProfileAndFriends()
    {
        var meMsg = await Users.GetLoggedInUser();
        if (!meMsg.IsError) cachedMe = meMsg.Data;

        var friendsMsg = await Users.GetLoggedInUserFriends();
        if (!friendsMsg.IsError)
        {
            cachedFriends = new List<User>(friendsMsg.Data);
            var page = friendsMsg.Data;
            while (page.HasNextPage)
            {
                var nextMsg = await Users.GetNextUserListPage(page);
                if (nextMsg.IsError) break;
                cachedFriends.AddRange(nextMsg.Data);
                page = nextMsg.Data;
            }
        }
    }

    public User Me => cachedMe;
    public IReadOnlyList<User> Friends => cachedFriends;

    public async Task<string> RequestServerIdentityProof()
    {
        if (!isInitialized) return null;
        var msg = await Users.GetUserProof();
        return msg.IsError ? null : msg.Data.Value;
    }

    public async Task SendFriendRequest(ulong userId)
    {
        if (!isInitialized) return;
        await Users.LaunchFriendRequestFlow(userId);
    }
}
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `Users.GetLoggedInUser()` | `Request<User>` | Current player (limited fields, available offline) |
| `Users.Get(userId)` | `Request<User>` | Full user record including presence |
| `Users.GetLoggedInUserFriends()` | `Request<UserList>` | Bidirectional followers |
| `Users.GetAccessToken()` | `Request<string>` | OAuth-style access token for REST calls |
| `Users.GetUserProof()` | `Request<UserProof>` | One-time nonce for backend identity verification |
| `Users.GetOrgScopedID(userId)` | `Request<OrgScopedID>` | Cross-app ID within same org |
| `Users.GetSdkAccounts()` | `Request<SdkAccountList>` | All accounts (Oculus + linked x-users) |
| `Users.GetLinkedAccounts(options)` | `Request<LinkedAccountList>` | Linked external service accounts |
| `Users.GetBlockedUsers()` | `Request<BlockedUserList>` | Users blocked by the signed-in user |
| `Users.LaunchBlockFlow(userId)` | `Request<LaunchBlockFlowResult>` | System UI for blocking a user |
| `Users.LaunchUnblockFlow(userId)` | `Request<LaunchUnblockFlowResult>` | System UI for unblocking |
| `Users.LaunchFriendRequestFlow(userId)` | `Request<LaunchFriendRequestFlowResult>` | System UI for sending follow request |
| `Users.GetLoggedInUserManagedInfo()` | `Request<User>` | MMA-only managed-account info |
| `Users.GetNextUserListPage(list)` | `Request<UserList>` | Paginate friends/users list |

### Models

| Type | Key fields |
|------|------------|
| `User` | `ID`, `OculusID`, `DisplayName`, `ImageURL`, `SmallImageUrl`, `Presence`, `PresenceStatus`, `PresenceDestinationApiName`, `PresenceLobbySessionId`, `PresenceMatchSessionId`, `ManagedInfoOptional` |
| `UserProof` | `Value` (the nonce) |
| `OrgScopedID` | `ID` |

### Enums

| Enum | Values |
|------|--------|
| `UserPresenceStatus` | `Online`, `Offline`, `Unknown` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Sharing `User.ID` across apps and expecting the same value | IDs are **app-scoped**. Use `GetOrgScopedID` for cross-app identity within the same org. |
| Calling `GetLoggedInUser` and expecting presence data | Use `Users.Get(loggedInUser.ID)` after `GetLoggedInUser` to get presence fields. |
| Persisting access tokens to disk | Treat tokens as session credentials. Fetch fresh each time you need to call REST. |
| Re-using a `UserProof` nonce | Nonces are single-use. Each backend verification needs a fresh `GetUserProof` call. |
| Skipping DUC | Many user APIs require Data Use Checkup approval. Without it you'll get permission errors. |
| Treating one-way followers as friends | `GetLoggedInUserFriends` returns bidirectional followers only. |
| Parsing `Presence` strings | `Presence` is locale-dependent and may change at any time. Display as-is. |
| Forgetting nullability of `DisplayName`, `ManagedInfoOptional` | Both are nullable. Fall back to `OculusID` for display name. |
| Calling Users methods before init | Always check `Core.IsInitialized()`. |

## Coding Rules

When implementing Users using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` before any Users call.
- Gate every API call with `Core.IsInitialized()`.

### Identity
- `GetLoggedInUser` is available offline — safe to call at app start before network is up.
- IDs are app-scoped. Don't share `User.ID` with other apps in the same org without going through `GetOrgScopedID`.
- For backend identity verification, always pair `GetUserProof` (nonce) with `GetLoggedInUser` (ID) and validate via `https://graph.oculus.com/user_nonce_validate`.

### Friends
- "Friends" means bidirectional followers. Walk paginated results via `GetNextUserListPage`.
- Cache the friends list at app start; refresh sparingly (it changes infrequently).

### Privacy & Security
- Treat access tokens and user proofs as ephemeral credentials. Never log them.
- Use the system Block/Unblock/FriendRequest flows — never silently mutate relationships.

### Display
- Prefer `DisplayName ?? OculusID` for user-facing labels.
- Display the human-readable `Presence` string verbatim — do not parse it.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest User & Friends Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-presence/#user-and-friends)
- [Data Use Checkup (DUC)](https://developer.oculus.com/resources/publish-data-use/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/users/UsersTester.cs`
