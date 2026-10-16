---
name: Horizon Platform SDK — Implement Entitlements (Anti-Piracy Check)
description: Use this skill when implementing the entitlement check in a Meta Quest Unity app using the Horizon Platform SDK. Covers the mandatory 10-second window, the recommended quit-on-failure pattern, offline-friendly behavior, and why entitlement is required for every shipping Quest app.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Entitlements Implementation Guide

You are an expert in implementing the entitlement check for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). The entitlement check is **required for every Quest app published to the Meta Horizon Store** — it verifies the user legitimately owns the app and gates access for unauthorized installs.

## Why This Matters

| | |
|---|---|
| **Required by the Store** | Every Quest app submission must implement an entitlement check or it will fail VRC review. |
| **Anti-piracy** | Prevents sideloaded copies from running for users who didn't purchase. |
| **10-second SLA** | The check **must complete within 10 seconds** of app launch. |
| **Works offline** | The check does not require internet. The platform caches entitlement state locally. |
| **Single API call** | Only one method: `Entitlements.GetIsViewerEntitled()`. |

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Note your App ID** (this is the App ID you pass to `Core.AsyncInitialize`)
3. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
```

The `Entitlements` static class lives in `Oculus.Platform`.

## Implementation Pattern

The recommended pattern is to perform the entitlement check **in your very first `MonoBehaviour.Start()`** (or even in a `RuntimeInitializeOnLoadMethod` for the earliest possible execution), and quit the app immediately on failure.

### Recommended: Standalone Bootstrap MonoBehaviour

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using UnityEngine;

public class EntitlementGate : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";

    async void Awake()
    {
        // 1) Initialize the platform
        try
        {
            Message<PlatformInitialize> initMsg = await Core.AsyncInitialize(appId);
            if (initMsg.IsError)
            {
                Debug.LogError($"Platform init failed: {initMsg.GetError().Message}");
                FailEntitlement("Platform init failed");
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            FailEntitlement("Platform init threw");
            return;
        }

        // 2) Check entitlement (must complete within 10s of launch)
        try
        {
            Message msg = await Entitlements.GetIsViewerEntitled();
            if (msg.IsError)
            {
                Debug.LogError($"Entitlement check failed: {msg.GetError().Message}");
                FailEntitlement(msg.GetError().Message);
                return;
            }
            Debug.Log("User is entitled to this app.");
            // App is good to go — load your real start scene here, or set a flag.
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            FailEntitlement("Entitlement check threw");
        }
    }

    private void FailEntitlement(string reason)
    {
        // Show a brief UI message if you have one, then quit.
        Debug.LogError($"Entitlement failure: {reason}. Quitting.");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
```

Place this `EntitlementGate` on a GameObject in your **first loaded scene** (e.g., a "Boot" scene) so it runs before any gameplay code.

### Alternative: Callback Pattern

If you prefer the callback style for legacy compatibility:

```csharp
void Start()
{
    Core.AsyncInitialize(appId).OnComplete(initMsg =>
    {
        if (initMsg.IsError) { FailEntitlement(initMsg.GetError().Message); return; }
        Entitlements.GetIsViewerEntitled().OnComplete(checkMsg =>
        {
            if (checkMsg.IsError) FailEntitlement(checkMsg.GetError().Message);
            else Debug.Log("Entitled");
        });
    });
}
```

## The 10-Second Rule

The Horizon Store requires that **the entitlement check complete within 10 seconds of app launch**. If your app takes longer to load (e.g., a heavy first scene), perform the check on a lightweight bootstrap scene first, then load gameplay assets.

> **Never block the main thread waiting for entitlement.** Use `async/await` (or `OnComplete`) so the Unity update loop continues. The 10-second budget is wall-clock from app launch, not from your code.

## Failure Handling

When the entitlement check fails, the recommended behavior is:

1. **Display a brief message** explaining that the user is not entitled (optional, but improves UX over silent failure).
2. **Quit the app** with `Application.Quit()`.

> Don't try to "soft-fail" by hiding features — the Store policy requires the app exit on entitlement failure. Soft-failures will fail VRC review.

```csharp
private void FailEntitlement(string reason)
{
    // Optional: show a non-VR fallback message via PlayerPrefs / system toast
    PlayerPrefs.SetString("LastEntitlementError", reason);
    PlayerPrefs.Save();

    Application.Quit();
}
```

## Editor and Sideload Behavior

- **Unity Editor**: The check runs against the configured **Standalone Platform** test user. Configure via **Meta > Platform > Edit Settings**. If no test user is configured, the check fails — set up a test user before iterating.
- **Sideloaded debug builds**: The check runs against the signed-in Quest account. If you're a developer with the app under test, you'll be entitled automatically. If not, the check fails.
- **Store builds**: The check runs against the user's purchase records. Cached, so works offline.

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `Entitlements.GetIsViewerEntitled()` | `Request` | Returns a non-error result if the user is entitled to the current app |

> **Note**: `Request` (no generic type parameter) — the result has no payload. Success is signaled by `IsError == false`. Failure is signaled by `IsError == true` and an error message in `GetError()`.

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Skipping the entitlement check | **Required by Horizon Store**. Submission will fail VRC review without it. |
| Performing the check after gameplay loads | The 10-second budget is from app launch. Run the check from your bootstrap scene as early as possible (`Awake`/`Start` of the boot scene). |
| Soft-failing on entitlement failure (hiding features instead of quitting) | The Store policy requires `Application.Quit()`. Soft-fails fail VRC review. |
| Calling `Entitlements.GetIsViewerEntitled` before `Core.AsyncInitialize` | Always init first, then check entitlement. |
| Blocking the main thread waiting for the check | Use `async/await` or `OnComplete`. The 10s budget is wall-clock, not code-time. |
| Forgetting to handle init errors as entitlement failures | If `Core.AsyncInitialize` fails, treat it as an entitlement failure and quit. |
| Hardcoding the App ID without verifying it matches the dashboard | A wrong App ID always fails entitlement, even for legitimate users. |
| Testing only in the Editor | The Editor uses a test user. Always verify on a real device with a real signed-in account before submission. |

## Coding Rules

When implementing Entitlements using the Horizon Platform SDK:

### Required for Shipping
- **Every Quest app published to the Meta Horizon Store must implement this check.** Submitting without it will fail VRC review.

### Implementation
- Run the check from a dedicated bootstrap scene's `Awake` or `Start`, before any gameplay code loads.
- Always call `Core.AsyncInitialize(appId)` first; treat init failure as entitlement failure.
- Use `async/await` (or `OnComplete`) — never block the main thread.
- Quit the app on failure via `Application.Quit()`. Do not soft-fail.
- The check must complete within **10 seconds of app launch** (wall-clock).

### Editor & Test Iteration
- Configure a test user in **Meta > Platform > Edit Settings** for in-Editor iteration.
- Sideloaded debug builds use the signed-in Quest account — make sure the developer account is entitled.

### Offline
- The platform caches entitlement state locally. The check works without internet, so you do **not** need to special-case offline scenarios.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).

## Useful Links

- [Meta Quest Entitlement Check Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-entitlement-check/)
- [Virtual Reality Checks (VRC) — Quest](https://developer.oculus.com/resources/publish-quest-req/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/entitlements/EntitlementsTester.cs`
