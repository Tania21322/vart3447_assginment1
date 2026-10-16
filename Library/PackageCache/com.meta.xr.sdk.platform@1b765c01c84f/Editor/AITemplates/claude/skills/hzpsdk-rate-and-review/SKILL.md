---
name: Horizon Platform SDK — Implement Rate and Review
description: Use this skill when implementing in-app store rating prompts in a Meta Quest Unity app using the Horizon Platform SDK. Covers CanLaunchRateAndReview eligibility check, RateAndReviewLauncher to show the system UI, the platform's built-in throttling, and the recommended timing for asking for reviews (after positive moments, never mid-task).
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Rate and Review Implementation Guide

You are an expert in implementing in-app rate-and-review prompts for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). The Rate and Review API surfaces the Meta Horizon Store's rating dialog from inside your app — no leaving the experience.

## What This API Does

| | |
|---|---|
| **Eligibility check** | `CanLaunchRateAndReview` — has the user already reviewed? Have you asked too recently? |
| **Launch UI** | `RateAndReviewLauncher` — shows the system rating dialog |
| **Platform throttling** | The platform tracks when the user last reviewed/dismissed and gates eligibility automatically. You don't need to track it yourself. |

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Note your App ID**
3. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
```

The `RateAndReview` static class lives in `Oculus.Platform`. The `ApplicationCanViewerRateAndReview` model lives in `Oculus.Platform.Models`.

## Step 1: Initialize the Platform

```csharp
async void Start()
{
    var msg = await Core.AsyncInitialize(appId);
    if (!msg.IsError) isInitialized = true;
}
```

Always check `Core.IsInitialized()` before any Rate and Review call.

## Step 2: Check Eligibility Before Asking

Always call `CanLaunchRateAndReview` before showing the prompt. The platform decides whether the user is eligible (hasn't recently reviewed, hasn't been asked too often).

```csharp
public async Task<bool> CanAskForReview()
{
    if (!Core.IsInitialized()) return false;

    var msg = await RateAndReview.CanLaunchRateAndReview();
    if (msg.IsError) return false;
    return msg.Data.Result;
}
```

## Step 3: Launch the Rating UI

```csharp
public async Task<bool> AskForReview()
{
    if (!Core.IsInitialized()) return false;

    if (!await CanAskForReview())
    {
        Debug.Log("User not eligible for review prompt right now");
        return false;
    }

    var msg = await RateAndReview.RateAndReviewLauncher();
    if (msg.IsError)
    {
        Debug.LogError($"RateAndReviewLauncher: {msg.GetError().Message}");
        return false;
    }
    Debug.Log("Rating UI launched");
    return true;
}
```

> **The platform handles the dialog** — it shows the rating UI, captures the rating, and sends it to the Store. You don't get the result back.

## Step 4: Pick the Right Moment

Don't ask for reviews mid-task or during a struggle. Ask **after** positive moments:

- After completing a level / boss fight
- After unlocking an achievement
- After a successful multiplayer session
- After the user manually shares something
- After N successful sessions over time

Avoid:
- During gameplay
- After errors or crashes
- On first launch
- Multiple times in one session

## Complete Rate and Review Trigger

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class ReviewPromptTrigger : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";
    [SerializeField] private int sessionsBeforeFirstAsk = 3;

    private bool isInitialized;
    private const string SessionCountKey = "review_session_count";
    private const string LastAskedKey = "review_last_asked_unix";

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        isInitialized = !msg.IsError;

        // Track session counts locally so we don't hit the platform's eligibility
        // endpoint every session
        int sessions = PlayerPrefs.GetInt(SessionCountKey, 0) + 1;
        PlayerPrefs.SetInt(SessionCountKey, sessions);
        PlayerPrefs.Save();
    }

    /// <summary>Call after a positive in-game moment (level complete, achievement, etc).</summary>
    public async Task TryPromptAfterPositiveMoment()
    {
        if (!isInitialized) return;

        int sessions = PlayerPrefs.GetInt(SessionCountKey, 0);
        if (sessions < sessionsBeforeFirstAsk) return;

        long lastAsked = long.Parse(PlayerPrefs.GetString(LastAskedKey, "0"));
        long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        if (now - lastAsked < 60 * 60 * 24 * 30) return; // don't re-ask within 30 days

        var canMsg = await RateAndReview.CanLaunchRateAndReview();
        if (canMsg.IsError || !canMsg.Data.Result) return;

        var launchMsg = await RateAndReview.RateAndReviewLauncher();
        if (!launchMsg.IsError)
        {
            PlayerPrefs.SetString(LastAskedKey, now.ToString());
            PlayerPrefs.Save();
        }
    }
}
```

> **Why local throttling on top of platform throttling?** The platform throttle is conservative. Adding your own "ask after a positive moment, never within 30 days" gate makes the prompt feel earned rather than automated. If your local gate says "yes" but the platform says "no", the platform wins — which is fine.

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `RateAndReview.CanLaunchRateAndReview()` | `Request<ApplicationCanViewerRateAndReview>` | Check if the user is eligible to be asked |
| `RateAndReview.RateAndReviewLauncher()` | `Request` | Show the system rating UI |

### Models

| Type | Key fields |
|------|------------|
| `ApplicationCanViewerRateAndReview` | `Result` (bool) |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Skipping `CanLaunchRateAndReview` | Always check first. The platform throttles aggressively to avoid spamming users. |
| Asking on first launch | Bad UX. Wait for a positive moment after the user has invested some time. |
| Asking during a struggle (after a death, error, etc.) | Negative bias on the rating. Ask after wins, not losses. |
| Asking multiple times in one session | The platform will likely throttle, but even if it doesn't, the user will. Add your own per-session gate. |
| Treating the launcher result as the user's rating | You don't get the rating. The platform handles it. |
| Pre-checking and immediately launching without waiting for the right moment | Couple the eligibility check with a moment-detection trigger (level complete, achievement, etc.). |
| Calling Rate and Review APIs before init | Always check `Core.IsInitialized()`. |

## Coding Rules

When implementing Rate and Review using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` first.
- Gate every API call with `Core.IsInitialized()`.

### Eligibility
- **Always call `CanLaunchRateAndReview` before launching.** The platform tracks when users last reviewed/dismissed and won't surface the dialog otherwise — you'll just be wasting an API call.

### Timing
- Trigger after positive moments: level complete, achievement unlock, win, share.
- Avoid: first launch, mid-task, after errors, after deaths.
- Add your own per-session and per-N-days gating on top of platform throttling.

### What You Get Back
- You don't get the user's rating from the API. The platform handles the UI and submits to the Store.
- Treat `RateAndReviewLauncher` as fire-and-forget once you confirm eligibility.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest Rate and Review Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-rate-and-review/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/rate_and_review/`
