---
name: Horizon Platform SDK — Implement Challenges
description: Use this skill when implementing Challenges in a Meta Quest Unity app using the Horizon Platform SDK. Covers initialization, creating/joining/leaving challenges, listing user challenges, fetching challenge entries, inviting users, and the relationship to Leaderboards.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Challenges Implementation Guide

You are an expert in implementing Challenges for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). Challenges turn any Leaderboard into a shareable, time-bound competition: users can create a challenge, invite friends, and compete on score.

## Prerequisites

Challenges require an existing **Leaderboard**. If you haven't set one up, follow the `hzpsdk-leaderboards` skill first.

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Create a leaderboard** in the Developer Dashboard. Note its **API Name** (case-sensitive).
3. **Note your App ID** and the leaderboard's API name
4. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

> **Free with Leaderboards**: Any app that uses Leaderboards automatically gets Challenges. They appear in the Scoreboards UI on the Quest, and `Leaderboards.WriteEntry` returns affected challenge IDs in its response.

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

The `Challenges` static class lives in `Oculus.Platform`. Models (`Challenge`, `ChallengeEntry`) and lists live in `Oculus.Platform.Models`. The `ChallengeOptions` builder, plus the `ChallengeVisibility`, `ChallengeViewerFilter`, and `ChallengeCreationType` enums, live in `Oculus.Platform`. The `LeaderboardFilterType` and `LeaderboardStartAt` enums are reused from Leaderboards.

## Step 1: Initialize the Platform

Same pattern as every other PSDK feature. Always check `Core.IsInitialized()` before any Challenges call.

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

See the `hzpsdk-iap` or `hzpsdk-leaderboards` skill for full Editor-mode setup details.

## Step 2: Create a Challenge

A Challenge is built around an existing Leaderboard. Set the time window with `SetStartDate` / `SetEndDate` (both `DateTime`, UTC), and pick a visibility level.

```csharp
public async Task<ulong> CreateChallenge(string leaderboardName, string title, TimeSpan duration)
{
    if (!Core.IsInitialized()) return 0;

    var options = new ChallengeOptions();
    options.SetTitle(title);
    options.SetDescription("Highest score wins!");
    options.SetLeaderboardName(leaderboardName);
    options.SetVisibility(ChallengeVisibility.InviteOnly);
    var now = DateTime.UtcNow;
    options.SetStartDate(now);
    options.SetEndDate(now.Add(duration));

    try
    {
        Message<Challenge> msg = await Challenges.Create(leaderboardName, options);
        if (msg.IsError)
        {
            Debug.LogError($"Challenges.Create failed: {msg.GetError().Message}");
            return 0;
        }
        Challenge ch = msg.Data;
        Debug.Log($"Created challenge {ch.ID} ('{ch.Title}'), ends {ch.EndDate:u}");
        return ch.ID;
    }
    catch (Exception e)
    {
        Debug.LogException(e);
        return 0;
    }
}
```

### Visibility Options

| `ChallengeVisibility` | Meaning |
|------------------------|---------|
| `Public` | Anyone can see and join |
| `InviteOnly` | Anyone can see; only invited users can join |
| `Private` | Only invited users can see and join |
| `Unknown` | Reserved |

## Step 3: List Challenges

Use `ChallengeOptions` as a filter, then call `Challenges.GetList`.

```csharp
public async Task<List<Challenge>> ListMyChallenges(int limit = 25)
{
    if (!Core.IsInitialized()) return new();

    var filter = new ChallengeOptions();
    filter.SetViewerFilter(ChallengeViewerFilter.ParticipatingOrInvited);
    filter.SetIncludeActiveChallenges = true;
    filter.IncludeFutureChallenges = true;
    filter.IncludePastChallenges = false;

    var msg = await Challenges.GetList(filter, limit);
    if (msg.IsError) return new();
    return new List<Challenge>(msg.Data);
}
```

### Viewer Filter Options

| `ChallengeViewerFilter` | Meaning |
|--------------------------|---------|
| `AllVisible` | All public + invited |
| `Participating` | Challenges the user has joined |
| `Invited` | Challenges the user has been invited to |
| `ParticipatingOrInvited` | Union of the two above |

> **Pagination**: `ChallengeList` exposes `HasNextPage` / `HasPreviousPage`. Use `Challenges.GetNextChallenges(list)` / `Challenges.GetPreviousChallenges(list)` to walk pages.

## Step 4: Get Challenge Details and Entries

```csharp
public async Task<Challenge> GetChallenge(ulong challengeId)
{
    var msg = await Challenges.Get(challengeId);
    if (msg.IsError) return null;
    return msg.Data;
}

public async Task<List<ChallengeEntry>> GetChallengeEntries(ulong challengeId, int limit = 25)
{
    var msg = await Challenges.GetEntries(
        challengeId,
        limit,
        LeaderboardFilterType.None,           // or .Friends
        LeaderboardStartAt.Top);              // or .CenteredOnViewerOrTop
    if (msg.IsError) return new();
    return new List<ChallengeEntry>(msg.Data);
}

public async Task<List<ChallengeEntry>> GetEntriesAfterRank(ulong challengeId, ulong afterRank, int limit = 25)
{
    var msg = await Challenges.GetEntriesAfterRank(challengeId, limit, afterRank);
    if (msg.IsError) return new();
    return new List<ChallengeEntry>(msg.Data);
}
```

`ChallengeEntry` mirrors `LeaderboardEntry`: it has `Rank`, `Score`, `DisplayScore`, `User`, `Timestamp`, etc.

## Step 5: Join, Leave, Decline, Invite

```csharp
public async Task<bool> JoinChallenge(ulong challengeId)
{
    var msg = await Challenges.Join(challengeId);
    return !msg.IsError;
}

public async Task<bool> LeaveChallenge(ulong challengeId)
{
    var msg = await Challenges.Leave(challengeId);
    return !msg.IsError;
}

public async Task<bool> DeclineInvite(ulong challengeId)
{
    var msg = await Challenges.DeclineInvite(challengeId);
    return !msg.IsError;
}

public async Task<bool> InviteUsers(ulong challengeId, ulong[] userIds)
{
    var msg = await Challenges.InviteUsers(challengeId, userIds);
    return !msg.IsError;
}
```

`InviteUsers` requires user IDs you've already retrieved (e.g., via `GroupPresence.GetInvitableUsers`, `Users.GetLoggedInUserFriends`, or a roster you maintain).

## Step 6: Update or Delete a Challenge

The user must have permission (typically the creator) to mutate the challenge.

```csharp
public async Task<bool> UpdateChallenge(ulong challengeId, string newTitle, string newDescription)
{
    var options = new ChallengeOptions();
    options.SetTitle(newTitle);
    options.SetDescription(newDescription);
    var msg = await Challenges.UpdateInfo(challengeId, options);
    return !msg.IsError;
}

public async Task<bool> DeleteChallenge(ulong challengeId)
{
    var msg = await Challenges.Delete(challengeId);
    return !msg.IsError;
}
```

## Step 7: Hook into Leaderboard Score Submission

When `Leaderboards.WriteEntry` is called, the Platform automatically updates any Challenges tied to that leaderboard. The same flow that updates the global leaderboard updates active challenges — your code doesn't need to submit twice.

Use the result of `WriteEntry` to refresh affected challenge UI.

```csharp
public async Task SubmitScoreAndRefreshChallenges(string leaderboardName, long score)
{
    // 1) Submit score (auto-updates all relevant challenges)
    var writeMsg = await Leaderboards.WriteEntry(leaderboardName, score);
    if (writeMsg.IsError || !writeMsg.Data) return;

    // 2) Refresh the user's active challenges to pick up the new ranking
    var listMsg = await Challenges.GetList(BuildActiveChallengeFilter(leaderboardName), 25);
    if (!listMsg.IsError) RefreshChallengeUI(listMsg.Data);
}

private ChallengeOptions BuildActiveChallengeFilter(string leaderboardName)
{
    var f = new ChallengeOptions();
    f.SetLeaderboardName(leaderboardName);
    f.SetViewerFilter(ChallengeViewerFilter.Participating);
    f.IncludeActiveChallenges = true;
    return f;
}
```

## Complete Challenge Manager Example

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ChallengeManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";
    [SerializeField] private string leaderboardName = "high_scores";

    private bool isInitialized;

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        isInitialized = !msg.IsError;
    }

    public async Task<Challenge> CreateWeeklyChallenge(string title)
    {
        if (!isInitialized) return null;
        var opts = new ChallengeOptions();
        opts.SetTitle(title);
        opts.SetDescription($"Compete this week on {leaderboardName}!");
        opts.SetLeaderboardName(leaderboardName);
        opts.SetVisibility(ChallengeVisibility.Public);
        opts.SetStartDate(DateTime.UtcNow);
        opts.SetEndDate(DateTime.UtcNow.AddDays(7));

        var msg = await Challenges.Create(leaderboardName, opts);
        if (msg.IsError)
        {
            Debug.LogError($"Create: {msg.GetError().Message}");
            return null;
        }
        return msg.Data;
    }

    public async Task<List<Challenge>> LoadMyChallenges()
    {
        if (!isInitialized) return new();
        var f = new ChallengeOptions();
        f.SetViewerFilter(ChallengeViewerFilter.ParticipatingOrInvited);
        f.IncludeActiveChallenges = true;
        f.IncludeFutureChallenges = true;
        var msg = await Challenges.GetList(f, 25);
        return msg.IsError ? new() : new List<Challenge>(msg.Data);
    }

    public async Task<List<ChallengeEntry>> LoadFriendsLeaderboard(ulong challengeId)
    {
        if (!isInitialized) return new();
        var msg = await Challenges.GetEntries(
            challengeId, 25, LeaderboardFilterType.Friends, LeaderboardStartAt.CenteredOnViewerOrTop);
        return msg.IsError ? new() : new List<ChallengeEntry>(msg.Data);
    }
}
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `Challenges.Create(leaderboardName, options)` | `Request<Challenge>` | Create a new challenge bound to a leaderboard |
| `Challenges.Get(challengeId)` | `Request<Challenge>` | Fetch a single challenge by ID |
| `Challenges.GetList(options, limit)` | `Request<ChallengeList>` | List challenges matching filters |
| `Challenges.GetEntries(id, limit, filter, startAt)` | `Request<ChallengeEntryList>` | Fetch challenge entries |
| `Challenges.GetEntriesAfterRank(id, limit, afterRank)` | `Request<ChallengeEntryList>` | Page entries after a rank |
| `Challenges.GetEntriesByIds(id, limit, startAt, userIds)` | `Request<ChallengeEntryList>` | Entries for specific users |
| `Challenges.Join(id)` | `Request<Challenge>` | Join a challenge |
| `Challenges.Leave(id)` | `Request<Challenge>` | Leave a challenge |
| `Challenges.DeclineInvite(id)` | `Request<Challenge>` | Decline a challenge invite |
| `Challenges.InviteUsers(id, userIds)` | `Request<Challenge>` | Invite users by ID |
| `Challenges.UpdateInfo(id, options)` | `Request<Challenge>` | Update challenge metadata |
| `Challenges.Delete(id)` | `Request` | Delete a challenge |
| `Challenges.GetNextChallenges(list)` | `Request<ChallengeList>` | Next page of challenges |
| `Challenges.GetNextEntries(list)` | `Request<ChallengeEntryList>` | Next page of entries |

### Models

| Type | Key fields |
|------|------------|
| `Challenge` | `ID`, `Title`, `Description`, `Leaderboard`, `StartDate`, `EndDate`, `Visibility`, `CreationType`, `ParticipantsOptional`, `InvitedUsersOptional` |
| `ChallengeEntry` | `Rank`, `Score`, `DisplayScore`, `User`, `Timestamp`, `ExtraData` |

### Enums

| Enum | Values |
|------|--------|
| `ChallengeVisibility` | `Public`, `InviteOnly`, `Private`, `Unknown` |
| `ChallengeViewerFilter` | `AllVisible`, `Participating`, `Invited`, `ParticipatingOrInvited`, `Unknown` |
| `ChallengeCreationType` | `UserCreated`, `DeveloperCreated`, `Unknown` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Trying to create a Challenge without a Leaderboard | Challenges always reference an existing Leaderboard's API name. Set up the Leaderboard first. |
| Calling `Challenges.Create` before init | Always check `Core.IsInitialized()`. |
| Using local time instead of UTC for start/end dates | Use `DateTime.UtcNow` and add UTC offsets. The platform stores dates in Unix epoch seconds. |
| Submitting scores to a Challenge directly | Don't — call `Leaderboards.WriteEntry` on the underlying leaderboard. Challenges are updated automatically. |
| Forgetting that `ParticipantsOptional` / `InvitedUsersOptional` can be null | They are nullable. Null-check before iterating. |
| Inviting users without their IDs | Get IDs first via `Users.GetLoggedInUserFriends` or `GroupPresence.GetInvitableUsers`. |
| Not checking visibility before showing a "Join" button | If `Visibility == Private`, only invited users can join. Hide the button otherwise. |
| Setting all three `IncludeActive/Future/Past` to false | Will return zero results. At least one must be true. |

## Coding Rules

When implementing Challenges using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` before any Challenges call.
- Gate every API call with `Core.IsInitialized()`.

### Challenge Lifecycle
- Treat Challenges as a UI layer over Leaderboards. Always submit scores via `Leaderboards.WriteEntry` — the platform fans out to active challenges automatically.
- After `Leaderboards.WriteEntry` returns successfully, refresh the user's active challenge list to update rankings in your UI.
- Use UTC for all `StartDate` / `EndDate` values.
- Default to `ChallengeVisibility.Public` for community challenges, `InviteOnly` for friend challenges, `Private` for closed groups.

### Listing
- Use `ChallengeViewerFilter.ParticipatingOrInvited` for "My Challenges" UI.
- Always set at least one of `IncludeActiveChallenges`, `IncludeFutureChallenges`, `IncludePastChallenges` to `true`.
- Prefer `GetNextChallenges(list)` over manual pagination.

### Invites
- Resolve user IDs first via `Users.GetLoggedInUserFriends` (`hzpsdk-users`) or `GroupPresence.GetInvitableUsers` (`hzpsdk-group-presence`).
- Don't surface a Join/Decline button without first checking the user's relationship to the challenge.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest Challenges Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-challenges/)
- [Meta Quest Leaderboards Documentation](https://developer.oculus.com/documentation/unity/ps-leaderboards/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/challenges/ChallengesTester.cs`
- Related skills: `hzpsdk-leaderboards`, `hzpsdk-users`, `hzpsdk-group-presence`
