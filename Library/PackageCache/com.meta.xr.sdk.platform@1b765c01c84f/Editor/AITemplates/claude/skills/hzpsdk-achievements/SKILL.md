---
name: Horizon Platform SDK — Implement Achievements
description: Use this skill when implementing Achievements in a Meta Quest Unity app using the Horizon Platform SDK. Covers initialization, the three achievement types (simple, count, bitfield), unlocking, incrementing counters, setting bitfield bits, fetching definitions and progress, and Meta Quest Home display.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Achievements Implementation Guide

You are an expert in implementing Achievements for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). Achievements appear in Meta Quest Home and drive engagement by giving players visible progress milestones.

## Prerequisites

Before implementing Achievements:
1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Define your achievements** in the Developer Dashboard under your app's "Platform Services > Achievements" section. For each achievement decide:
   - **API Name** (case-sensitive, used in code)
   - **Type**: `Simple`, `Count`, or `Bitfield`
   - For `Count`: the **target value** the counter must reach to unlock
   - For `Bitfield`: the **bitfield length** and the **target** number of bits that must be set
3. **Note your App ID** and the achievement API names
4. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

## Achievement Types — Cheat Sheet

| Type | When to use | API to call |
|------|-------------|-------------|
| `Simple` | One-shot events ("Reached the boss", "Completed tutorial") | `Achievements.Unlock(name)` |
| `Count` | Cumulative progress ("Defeat 100 enemies") | `Achievements.AddCount(name, count)` — counter is monotonically increasing on the server |
| `Bitfield` | Collect-them-all sets ("Find all 7 hidden gems") | `Achievements.AddFields(name, "0010001")` |

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

The `Achievements` static class lives in `Oculus.Platform`. Models (`AchievementDefinition`, `AchievementProgress`, `AchievementUpdate`) and the lists live in `Oculus.Platform.Models`. The `AchievementType` enum lives in `Oculus.Platform`.

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

Always check `Core.IsInitialized()` before any Achievements call. See the `hzpsdk-iap` skill for full Editor-mode setup details.

## Step 2: Unlock a Simple Achievement

```csharp
public async Task UnlockAchievement(string apiName)
{
    if (!Core.IsInitialized()) return;

    try
    {
        Message<AchievementUpdate> msg = await Achievements.Unlock(apiName);
        if (msg.IsError)
        {
            Debug.LogError($"Achievements.Unlock({apiName}) failed: {msg.GetError().Message}");
            return;
        }
        AchievementUpdate update = msg.Data;
        if (update.JustUnlocked)
        {
            Debug.Log($"Just unlocked '{apiName}' for the first time!");
            ShowUnlockToast(apiName);
        }
    }
    catch (Exception e)
    {
        Debug.LogException(e);
    }
}
```

`Unlock` is **idempotent** — calling it on an already-unlocked achievement is safe and just returns `JustUnlocked = false`. You don't need to track unlock state locally to avoid duplicate calls.

## Step 3: Increment a Count Achievement

`AddCount` adds to the **server-side** running counter. The achievement unlocks automatically when the counter reaches the target you defined in the dashboard.

```csharp
public async Task AddProgress(string apiName, ulong increment = 1)
{
    if (!Core.IsInitialized()) return;

    var msg = await Achievements.AddCount(apiName, increment);
    if (msg.IsError) return;

    AchievementUpdate update = msg.Data;
    if (update.JustUnlocked)
    {
        Debug.Log($"Reached count target on '{apiName}'!");
    }
}
```

> **Don't pass the cumulative total** — the API adds to the existing server value. Pass only the delta.

## Step 4: Set Bitfield Achievement Bits

`AddFields` accepts a string of `'0'` and `'1'` characters representing which bits to set. The string length must equal the achievement's `BitfieldLength`. The platform OR's the bits with the existing value.

```csharp
// Bitfield length is 7 — set bit index 2 to mark the 3rd item collected
public async Task CollectItem(int itemIndex, int bitfieldLength = 7)
{
    var bits = new char[bitfieldLength];
    for (int i = 0; i < bitfieldLength; i++) bits[i] = '0';
    bits[itemIndex] = '1';
    string fields = new string(bits);

    var msg = await Achievements.AddFields("collect_all_gems", fields);
    if (!msg.IsError && msg.Data.JustUnlocked)
    {
        Debug.Log("Found all the gems!");
    }
}
```

## Step 5: Fetch Definitions and User Progress

### All definitions

```csharp
public async Task LoadAllDefinitions()
{
    var msg = await Achievements.GetAllDefinitions();
    if (msg.IsError) return;

    foreach (AchievementDefinition def in msg.Data)
    {
        Debug.Log($"{def.Name} ({def.Type}) target={def.Target}");
    }

    // Pagination
    if (msg.Data.HasNextPage)
    {
        var nextMsg = await Achievements.GetNextAchievementDefinitionListPage(msg.Data);
        // ...
    }
}
```

### Specific definitions by name

```csharp
string[] names = { "first_kill", "collect_all_gems", "defeat_100_enemies" };
var msg = await Achievements.GetDefinitionsByName(names);
```

### All user progress

```csharp
var msg = await Achievements.GetAllProgress();
foreach (AchievementProgress p in msg.Data)
{
    if (p.IsUnlocked)
        Debug.Log($"{p.Name}: unlocked at {p.UnlockTime:u}");
    else
        Debug.Log($"{p.Name}: count={p.Count}, bitfield={p.Bitfield}");
}
```

### Progress for specific achievements

```csharp
var msg = await Achievements.GetProgressByName(new[] { "first_kill", "defeat_100_enemies" });
```

## Joining Definitions and Progress for UI

A common UI pattern: show every achievement's name, target, and the current player's progress. Fetch both in parallel, then join by `Name`.

```csharp
public async Task<List<(AchievementDefinition def, AchievementProgress progress)>> LoadAchievementUiData()
{
    var defsTask = Achievements.GetAllDefinitions();
    var progressTask = Achievements.GetAllProgress();

    var defsMsg = await defsTask;
    var progressMsg = await progressTask;

    if (defsMsg.IsError || progressMsg.IsError) return new();

    var progressByName = new Dictionary<string, AchievementProgress>();
    foreach (var p in progressMsg.Data) progressByName[p.Name] = p;

    var result = new List<(AchievementDefinition, AchievementProgress)>();
    foreach (var def in defsMsg.Data)
    {
        progressByName.TryGetValue(def.Name, out var prog);
        result.Add((def, prog)); // prog may be null if user has no progress yet
    }
    return result;
}
```

## Complete Achievements Manager Example

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AchievementsManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";

    private bool isInitialized;
    private readonly Dictionary<string, AchievementDefinition> defsByName = new();

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        if (msg.IsError) { Debug.LogError(msg.GetError().Message); return; }
        isInitialized = true;
        await LoadDefinitions();
    }

    private async Task LoadDefinitions()
    {
        var msg = await Achievements.GetAllDefinitions();
        if (msg.IsError) return;
        foreach (var d in msg.Data) defsByName[d.Name] = d;
    }

    public async Task UnlockSimple(string name)
    {
        if (!isInitialized) return;
        var msg = await Achievements.Unlock(name);
        if (!msg.IsError && msg.Data.JustUnlocked) ShowToast(name);
    }

    public async Task IncrementCount(string name, ulong delta = 1)
    {
        if (!isInitialized) return;
        var msg = await Achievements.AddCount(name, delta);
        if (!msg.IsError && msg.Data.JustUnlocked) ShowToast(name);
    }

    public async Task SetBitfieldBit(string name, int bitIndex)
    {
        if (!isInitialized || !defsByName.TryGetValue(name, out var def)) return;
        if (def.Type != AchievementType.Bitfield) return;

        char[] bits = new char[def.BitfieldLength];
        for (int i = 0; i < bits.Length; i++) bits[i] = '0';
        if (bitIndex >= 0 && bitIndex < bits.Length) bits[bitIndex] = '1';

        var msg = await Achievements.AddFields(name, new string(bits));
        if (!msg.IsError && msg.Data.JustUnlocked) ShowToast(name);
    }

    private void ShowToast(string name)
    {
        Debug.Log($"Unlocked: {name}");
        // Hook into your in-game toast/notification system
    }
}
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `Achievements.Unlock(name)` | `Request<AchievementUpdate>` | Unlock a Simple achievement (or any type) |
| `Achievements.AddCount(name, count)` | `Request<AchievementUpdate>` | Increment a Count achievement |
| `Achievements.AddFields(name, fields)` | `Request<AchievementUpdate>` | Set bits on a Bitfield achievement |
| `Achievements.GetAllDefinitions()` | `Request<AchievementDefinitionList>` | List all achievement definitions |
| `Achievements.GetDefinitionsByName(names)` | `Request<AchievementDefinitionList>` | Definitions for specific names |
| `Achievements.GetAllProgress()` | `Request<AchievementProgressList>` | Current user's progress on all achievements |
| `Achievements.GetProgressByName(names)` | `Request<AchievementProgressList>` | Progress for specific achievements |
| `Achievements.GetNextAchievementDefinitionListPage(list)` | `Request<AchievementDefinitionList>` | Next page of definitions |
| `Achievements.GetNextAchievementProgressListPage(list)` | `Request<AchievementProgressList>` | Next page of progress |

### Models

| Type | Key fields |
|------|------------|
| `AchievementDefinition` | `Name`, `Type`, `Target`, `BitfieldLength` |
| `AchievementProgress` | `Name`, `IsUnlocked`, `UnlockTime`, `Count`, `Bitfield` |
| `AchievementUpdate` | `Name`, `JustUnlocked` |

### Enums

| Enum | Values |
|------|--------|
| `AchievementType` | `Simple`, `Count`, `Bitfield`, `Unknown` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Passing the cumulative total to `AddCount` | Pass only the delta. The platform tracks the running total server-side. |
| Calling `Unlock` defensively to prevent dupes | `Unlock` is idempotent. Just call it; check `JustUnlocked` if you want to fire UI only on first unlock. |
| Bitfield string length mismatch | The `fields` string must be exactly `BitfieldLength` characters of `'0'` / `'1'`. |
| Calling Achievements before init | Always check `Core.IsInitialized()`. |
| Tracking unlock state locally | The server is authoritative. Fetch progress with `GetAllProgress` on app start. |
| Confusing display name with API name | Always use the **API Name** from the dashboard. Case-sensitive. |
| Ignoring nullability of `AchievementProgress.UnlockTime` | If `IsUnlocked == false`, treat `UnlockTime` as meaningless. |
| Spamming `AddCount` per frame | Batch increments client-side and submit periodically (e.g., every 5 seconds or at checkpoints) to reduce API churn. |

## Coding Rules

When implementing Achievements using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` before any Achievements call.
- Gate every API call with `Core.IsInitialized()`.
- Cache `AchievementDefinition` results on app start so you can look up `BitfieldLength` and `Target` without re-querying.

### Unlock Patterns
- `Unlock` for one-shot events. Check `JustUnlocked` to drive a single UI animation.
- `AddCount` for cumulative goals. Always pass the **delta**, never the cumulative total. Batch multiple small increments when possible.
- `AddFields` for collect-them-all goals. The string length must equal `BitfieldLength`.

### Progress Display
- Fetch `GetAllDefinitions` + `GetAllProgress` in parallel and join by `Name` for UI.
- Use server-side progress as source of truth. Don't store unlock state locally.
- Handle pagination (`HasNextPage`) when an app has many achievements.

### Quest Home Integration
- Achievements automatically appear in Meta Quest Home. No extra integration needed.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest Achievements Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-achievements/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/achievements/AchievementsTester.cs`
