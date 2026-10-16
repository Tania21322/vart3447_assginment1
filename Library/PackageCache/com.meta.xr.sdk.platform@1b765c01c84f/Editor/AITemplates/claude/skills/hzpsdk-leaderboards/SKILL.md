---
name: Horizon Platform SDK — Implement Leaderboards
description: Use this skill when implementing Leaderboards in a Meta Quest Unity app using the Horizon Platform SDK. Covers initialization, retrieving leaderboard info, fetching paginated entries (top, friends, by user IDs, after rank), writing scores with optional supplementary metrics, and Challenges integration.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Leaderboards Implementation Guide

You are an expert in implementing Leaderboards for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). Leaderboards let players track scores, compare rankings against friends, and unlock Challenges automatically.

## Prerequisites

Before implementing Leaderboards:
1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Create one or more leaderboards** in the Developer Dashboard under your app's "Platform Services > Leaderboards" section. Note the **API Name** (case-sensitive) — this is what your code will reference, not the display name.
3. **Pick a sort order** (`HIGH_IS_BEST` or `LOW_IS_BEST`) and a **score type** (e.g., `NUMERIC`, `TIME`, `MILLISECONDS`). The score type only affects display formatting — `Score` is always a `long`.
4. **Note your App ID** and the leaderboard API name(s)
5. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

The `Leaderboards` static class lives in `Oculus.Platform`. Models (`Leaderboard`, `LeaderboardEntry`, `SupplementaryMetric`) and lists (`LeaderboardList`, `LeaderboardEntryList`) live in `Oculus.Platform.Models`. Filter and start-position enums (`LeaderboardFilterType`, `LeaderboardStartAt`) live in `Oculus.Platform`.

## Step 1: Initialize the Platform

You **must** initialize the Platform SDK before calling any Leaderboards method. `Core.IsInitialized()` gates every API call.

### Async/Await (Recommended)

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class LeaderboardBootstrap : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";
    private bool isInitialized;

    async void Start()
    {
        try
        {
            Message<PlatformInitialize> msg = await Core.AsyncInitialize(appId);
            if (msg.IsError)
            {
                Debug.LogError($"Platform init failed: {msg.GetError().Message}");
                return;
            }
            isInitialized = true;
            Debug.Log("Platform initialized");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
}
```

### Editor Testing

In the Unity Editor, the Platform SDK routes requests through `WindowsClient` (P/Invoke to a native DLL) instead of `AndroidClient` (JNI). To test in the Editor:

- Open **Meta > Platform > Edit Settings**
- Check **Use Standalone Platform** and enter test user credentials, OR
- Initialize with a runtime mode: `Core.AsyncInitialize(appId, "standalone")`

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

## Step 2: Retrieve Leaderboard Info

Look up a leaderboard by its API name. Useful when you want to confirm the leaderboard exists and grab its destination for deep-linking.

```csharp
public async Task GetLeaderboardInfo(string leaderboardName)
{
    if (!Core.IsInitialized()) return;

    try
    {
        Message<LeaderboardList> msg = await Leaderboards.Get(leaderboardName);
        if (msg.IsError)
        {
            Debug.LogError($"Leaderboards.Get failed: {msg.GetError().Message}");
            return;
        }
        foreach (Leaderboard lb in msg.Data)
        {
            Debug.Log($"Leaderboard: {lb.ApiName}, ID: {lb.ID}");
        }
    }
    catch (Exception e)
    {
        Debug.LogException(e);
    }
}
```

## Step 3: Write a Score

`WriteEntry` only updates the user's entry if the new score beats their best, unless `forceUpdate` is `true`. The response indicates whether the leaderboard was updated and lists any Challenges affected.

```csharp
public async Task SubmitScore(string leaderboardName, long score)
{
    if (!Core.IsInitialized()) return;

    try
    {
        // forceUpdate = null means "only update if new score is better"
        Message<bool> msg = await Leaderboards.WriteEntry(leaderboardName, score, extraData: null, forceUpdate: null);
        if (msg.IsError)
        {
            Debug.LogError($"WriteEntry failed: {msg.GetError().Message}");
            return;
        }
        bool didUpdate = msg.Data;
        Debug.Log(didUpdate ? "New high score!" : "Score not better than current best.");
    }
    catch (Exception e)
    {
        Debug.LogException(e);
    }
}
```

### Score with Extra Data (≤ 2KB)

Attach a `byte[]` (max 2KB) to the entry — useful for replay data, ghost recordings, or contextual metadata.

```csharp
byte[] replayData = SerializeGhostReplay(); // your code; must be ≤ 2048 bytes
await Leaderboards.WriteEntry(leaderboardName, score, replayData, forceUpdate: false);
```

### Score with Supplementary Metric (Tiebreaker)

When two players tie on the primary score, the supplementary metric breaks the tie (e.g., time taken, items collected).

```csharp
public async Task SubmitScoreWithTiebreaker(string leaderboardName, long score, long tiebreakerMetric)
{
    var msg = await Leaderboards.WriteEntryWithSupplementaryMetric(
        leaderboardName,
        score,
        tiebreakerMetric,
        extraData: null,
        forceUpdate: null);
    if (!msg.IsError && msg.Data) Debug.Log("Score updated.");
}
```

## Step 4: Fetch Entries

### Top N Globally

```csharp
public async Task LoadTopScores(string leaderboardName, int limit = 25)
{
    if (!Core.IsInitialized()) return;

    var msg = await Leaderboards.GetEntries(
        leaderboardName,
        limit,
        LeaderboardFilterType.None,
        LeaderboardStartAt.Top);

    if (msg.IsError)
    {
        Debug.LogError($"GetEntries failed: {msg.GetError().Message}");
        return;
    }

    foreach (LeaderboardEntry entry in msg.Data)
    {
        Debug.Log($"#{entry.Rank} {entry.User.DisplayName}: {entry.DisplayScore ?? entry.Score.ToString()}");
    }
}
```

### Centered on Current User

`CenteredOnViewerOrTop` is the safe default — if the user has no entry yet, it falls back to the top instead of returning an error.

```csharp
var msg = await Leaderboards.GetEntries(
    leaderboardName,
    limit: 20,
    filter: LeaderboardFilterType.None,
    startAt: LeaderboardStartAt.CenteredOnViewerOrTop);
```

### Friends Only

Returns entries from bidirectional followers only.

```csharp
var msg = await Leaderboards.GetEntries(
    leaderboardName,
    limit: 25,
    filter: LeaderboardFilterType.Friends,
    startAt: LeaderboardStartAt.CenteredOnViewerOrTop);
```

### After a Specific Rank (Pagination)

Use this to implement "load next page" buttons or infinite scroll. Pass the highest rank from the previous page.

```csharp
ulong lastRank = 0;
public async Task LoadNextPage(string leaderboardName, int pageSize = 25)
{
    var msg = await Leaderboards.GetEntriesAfterRank(leaderboardName, pageSize, lastRank);
    if (msg.IsError) return;
    if (msg.Data.Count > 0)
    {
        lastRank = (ulong)msg.Data[msg.Data.Count - 1].Rank;
    }
    foreach (var entry in msg.Data) RenderEntry(entry);
}
```

### By Specific User IDs

Useful when you already know the IDs you want to look up (e.g., a friends-only leaderboard you build yourself, or showing a specific squad).

```csharp
ulong[] userIds = new ulong[] { 12345UL, 67890UL };
var msg = await Leaderboards.GetEntriesByIds(
    leaderboardName,
    limit: 10,
    startAt: LeaderboardStartAt.CenteredOnViewer,
    userIds);
```

> **Note**: When `startAt` is `CenteredOnViewer` or `CenteredOnViewerOrTop`, the current user is automatically included in the results, even if their ID isn't in `userIds`.

### Pagination via NextUrl / PreviousUrl

`LeaderboardEntryList` exposes `HasNextPage` and `HasPreviousPage`. Use the helper methods to fetch the next page without managing rank cursors yourself.

```csharp
var firstPage = await Leaderboards.GetEntries(name, 25, LeaderboardFilterType.None, LeaderboardStartAt.Top);
if (!firstPage.IsError && firstPage.Data.HasNextPage)
{
    var nextPage = await Leaderboards.GetNextEntries(firstPage.Data);
    // ...
}
```

## Step 5: Render Entries in Unity UI

Pattern from the Baremetal sample (`samples/unity/Baremetal/Assets/SamplesInternal/leaderboards/LeaderboardsTester.cs`): instantiate row prefabs into a `ScrollRect`'s content container, then force a layout rebuild.

```csharp
[SerializeField] private GameObject rowPrefab;
[SerializeField] private ScrollRect leaderboardScrollView;
[SerializeField] private Transform leaderboardContent;

private void RenderEntries(LeaderboardEntryList entries)
{
    foreach (var entry in entries)
    {
        GameObject row = Instantiate(rowPrefab, leaderboardContent);
        Text[] textComponents = row.GetComponentsInChildren<Text>();
        if (textComponents.Length >= 2)
        {
            textComponents[0].text = entry.User?.DisplayName ?? "Unknown";
            textComponents[1].text = entry.DisplayScore ?? entry.Score.ToString();
        }
    }
    LayoutRebuilder.ForceRebuildLayoutImmediate(leaderboardScrollView.content);
}
```

## Complete Leaderboard Manager Example

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class LeaderboardManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";
    [SerializeField] private string leaderboardName = "high_scores";

    private bool isInitialized;
    private List<LeaderboardEntry> cachedTopEntries = new();

    async void Start()
    {
        await InitializePlatform();
    }

    private async Task InitializePlatform()
    {
        try
        {
#if UNITY_EDITOR
            var msg = await Core.AsyncInitialize(appId, "standalone");
#else
            var msg = await Core.AsyncInitialize(appId);
#endif
            if (msg.IsError)
            {
                Debug.LogError($"Platform init failed: {msg.GetError().Message}");
                return;
            }
            isInitialized = true;
        }
        catch (Exception e) { Debug.LogException(e); }
    }

    public async Task<bool> SubmitScoreAsync(long score, byte[] extraData = null, bool forceUpdate = false)
    {
        if (!isInitialized || !Core.IsInitialized()) return false;
        try
        {
            var msg = await Leaderboards.WriteEntry(leaderboardName, score, extraData, forceUpdate ? true : (bool?)null);
            if (msg.IsError)
            {
                Debug.LogError($"SubmitScore: {msg.GetError().Message}");
                return false;
            }
            return msg.Data;
        }
        catch (Exception e) { Debug.LogException(e); return false; }
    }

    public async Task<List<LeaderboardEntry>> LoadTopEntriesAsync(int limit = 25)
    {
        if (!isInitialized || !Core.IsInitialized()) return new();
        try
        {
            var msg = await Leaderboards.GetEntries(
                leaderboardName, limit, LeaderboardFilterType.None, LeaderboardStartAt.Top);
            if (msg.IsError)
            {
                Debug.LogError($"LoadTopEntries: {msg.GetError().Message}");
                return new();
            }
            cachedTopEntries = new List<LeaderboardEntry>(msg.Data);
            return cachedTopEntries;
        }
        catch (Exception e) { Debug.LogException(e); return new(); }
    }

    public async Task<List<LeaderboardEntry>> LoadFriendEntriesAsync(int limit = 25)
    {
        if (!isInitialized || !Core.IsInitialized()) return new();
        try
        {
            var msg = await Leaderboards.GetEntries(
                leaderboardName, limit, LeaderboardFilterType.Friends, LeaderboardStartAt.CenteredOnViewerOrTop);
            if (msg.IsError) return new();
            return new List<LeaderboardEntry>(msg.Data);
        }
        catch (Exception e) { Debug.LogException(e); return new(); }
    }
}
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `Leaderboards.Get(name)` | `Request<LeaderboardList>` | Look up a leaderboard by API name |
| `Leaderboards.GetEntries(name, limit, filter, startAt)` | `Request<LeaderboardEntryList>` | Fetch entries with filter/start position |
| `Leaderboards.GetEntriesAfterRank(name, limit, afterRank)` | `Request<LeaderboardEntryList>` | Fetch a page of entries after a rank |
| `Leaderboards.GetEntriesByIds(name, limit, startAt, userIds)` | `Request<LeaderboardEntryList>` | Fetch entries for specific user IDs |
| `Leaderboards.WriteEntry(name, score, extraData, forceUpdate)` | `Request<bool>` | Submit a score (best-only by default) |
| `Leaderboards.WriteEntryWithSupplementaryMetric(name, score, suppMetric, extraData, forceUpdate)` | `Request<bool>` | Submit a score with a tiebreaker metric |
| `Leaderboards.GetNextEntries(list)` | `Request<LeaderboardEntryList>` | Next page of entries |
| `Leaderboards.GetPreviousEntries(list)` | `Request<LeaderboardEntryList>` | Previous page of entries |

### Models

| Type | Key fields |
|------|------------|
| `Leaderboard` | `ApiName`, `ID`, `Destination` |
| `LeaderboardEntry` | `Rank`, `Score`, `DisplayScore`, `ExtraData`, `User`, `Timestamp`, `SupplementaryMetricOptional` |
| `SupplementaryMetric` | `ID`, `Metric` |

### Enums

| Enum | Values |
|------|--------|
| `LeaderboardFilterType` | `None`, `Friends`, `UserIds`, `Unknown` |
| `LeaderboardStartAt` | `Top`, `CenteredOnViewer`, `CenteredOnViewerOrTop`, `Unknown` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Using leaderboard **display name** instead of **API name** | Always use the API name from the Developer Dashboard. It's case-sensitive. |
| Calling `WriteEntry` and expecting it to always update | By default it only updates if the new score is better. Pass `forceUpdate: true` to overwrite. |
| Calling Leaderboards before init | Always check `Core.IsInitialized()` first. |
| Using `CenteredOnViewer` and crashing on a new user | Use `CenteredOnViewerOrTop` to gracefully fall back to the top of the leaderboard when the user has no entry. |
| Ignoring `extraData` size limit | `extraData` must be ≤ 2KB. Larger payloads will be rejected. |
| Reading `DisplayScore` without null check | `DisplayScore` is `null` if the leaderboard isn't configured with a score type. Fall back to `Score.ToString()`. |
| Manually managing pagination cursors when not needed | Prefer `GetNextEntries(list)` / `GetPreviousEntries(list)` over re-running `GetEntriesAfterRank`. |
| Skipping the `User` null check | `entry.User` and `entry.User.DisplayName` can be null for guest accounts or users who blocked you. |

## Coding Rules

When implementing Leaderboards using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` before any Leaderboards call.
- Gate every API call with `Core.IsInitialized()`.
- For Editor testing, pass `"standalone"` as the runtime mode and configure test user credentials in **Meta > Platform > Edit Settings**.

### Score Submission
- Treat `WriteEntry` as best-effort: check `msg.Data` (the `didUpdate` flag) to know if the leaderboard actually changed.
- Only set `forceUpdate: true` when the design *requires* overwriting (e.g., last-attempt scores). Otherwise rely on the default best-score behavior.
- Cap `extraData` at 2KB. Validate before submitting.
- Use `WriteEntryWithSupplementaryMetric` when scores can tie and you want deterministic ranking.

### Entry Retrieval
- Default to `LeaderboardStartAt.CenteredOnViewerOrTop` for "around me" views — it gracefully handles unranked users.
- Use `LeaderboardFilterType.Friends` for social leaderboards (bidirectional followers only).
- For paginated UI, prefer `GetNextEntries(list)` over manually tracking ranks.
- Always handle nullability on `entry.User`, `entry.User.DisplayName`, and `entry.DisplayScore`.

### Challenges Integration
- Leaderboard-integrated apps automatically get **Challenges** (see the `hzpsdk-challenges` skill). When `WriteEntry` returns, the response includes any Challenge IDs that were affected — surface this in your UI.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility with the legacy SDK).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest Leaderboards Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-leaderboards/)
- [Server-to-Server Leaderboard API](https://developer.oculus.com/documentation/unity/ps-leaderboards-s2s/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/leaderboards/LeaderboardsTester.cs`
