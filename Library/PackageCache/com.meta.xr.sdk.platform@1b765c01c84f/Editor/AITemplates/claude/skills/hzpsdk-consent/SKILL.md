---
name: Horizon Platform SDK — Implement Consent Flows
description: Use this skill when implementing user consent flows (GDPR, age-gating, telemetry opt-in, custom legal screens) in a Meta Quest Unity app using the Horizon Platform SDK. Covers GetConsentStatus, LaunchConsentIfRequired, the ConsentStatus and ConsentLaunchOutcome enums, version bumps for re-consent, and the recommended app-start gating pattern.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Consent Implementation Guide

You are an expert in implementing user consent flows for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). The Consent API surfaces system-level consent dialogs (legal disclosures, telemetry opt-in, GDPR-style data prompts) and returns a structured outcome you can act on.

## What This API Does

| | |
|---|---|
| **Check status** | `GetConsentStatus` — has the user already seen / consented / declined? |
| **Launch on demand** | `LaunchConsentIfRequired` — show the dialog only if status warrants it |
| **Bump versions** | Pass a `version` string to force re-consent when terms change |

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Configure consent flows** in the Developer Dashboard. Each flow gets a **name** (e.g., `data_sharing_v1`) — this is what your code passes.
3. **Note your App ID** and consent flow name(s)
4. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
```

The `Consent` static class lives in `Oculus.Platform`. Models (`ConsentStatusResult`, `ConsentLaunchResult`) live in `Oculus.Platform.Models`. Enums (`ConsentStatus`, `ConsentLaunchOutcome`) live in `Oculus.Platform`.

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

Always check `Core.IsInitialized()` before any Consent call.

## Step 2: Check Consent Status

```csharp
public async Task<ConsentStatus?> GetStatus(string flowName, string version = null)
{
    if (!Core.IsInitialized()) return null;

    var msg = await Consent.GetConsentStatus(flowName, version);
    if (msg.IsError) return null;
    if (msg.Data == null || msg.Data.Length == 0) return null;

    var result = msg.Data[0];
    Debug.Log($"Consent '{flowName}' status: {result.Status}");
    return result.Status;
}
```

### `ConsentStatus` Values

| Value | Meaning |
|-------|---------|
| `DefaultNotSeen` | User has never been shown this consent |
| `Seen` | User saw it but hasn't approved or declined |
| `Withdrawn` | User declined initially or withdrew later via settings |
| `Consented` | User approved |

## Step 3: Launch Consent Only If Required

This is the recommended primary entry point. The platform compares the current status to what's needed and only shows the dialog when appropriate.

```csharp
public async Task<ConsentLaunchOutcome> EnsureConsent(string flowName, string version = null)
{
    if (!Core.IsInitialized()) return ConsentLaunchOutcome.Unknown;

    var msg = await Consent.LaunchConsentIfRequired(flowName, version);
    if (msg.IsError)
    {
        Debug.LogError($"LaunchConsentIfRequired: {msg.GetError().Message}");
        return ConsentLaunchOutcome.Unknown;
    }
    return msg.Data.Outcome;
}
```

### `ConsentLaunchOutcome` Values

| Value | Meaning |
|-------|---------|
| `NotRequired` | Consent already complete (Approved or previously Withdrawn). No dialog shown. |
| `Approved` | User approved the consent in this dialog |
| `Denied` | User declined |
| `Dismissed` | User dismissed without choosing |
| `Unknown` | Reserved |

## Step 4: Gate App Startup on Required Consent

The most common pattern: show a required consent at app start; gate access to features based on the outcome.

```csharp
async void Start()
{
    var initMsg = await Core.AsyncInitialize(appId);
    if (initMsg.IsError) return;

    var outcome = await EnsureConsent("data_sharing_v1");
    if (outcome == ConsentLaunchOutcome.Approved || outcome == ConsentLaunchOutcome.NotRequired)
    {
        EnableTelemetry();
    }
    else
    {
        DisableTelemetry();
    }

    LoadMainMenu();
}
```

> **Don't block app launch** — show the consent dialog asynchronously so the user can see your splash UI behind it. Only gate the *features* that depend on the consent.

## Step 5: Force Re-Consent After Terms Change

When you update your privacy policy or terms, bump the `version` string and the platform will re-prompt:

```csharp
const string PRIVACY_FLOW = "data_sharing";
const string CURRENT_VERSION = "v2";  // bumped from v1 when terms changed

await Consent.LaunchConsentIfRequired(PRIVACY_FLOW, CURRENT_VERSION);
```

If the user previously consented to `v1` but the version is now `v2`, the dialog re-launches.

## Complete Consent Manager

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class ConsentManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";
    [SerializeField] private string telemetryFlowName = "telemetry_consent";
    [SerializeField] private string telemetryVersion = "v1";

    public bool TelemetryEnabled { get; private set; }

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        if (msg.IsError) { Debug.LogError(msg.GetError().Message); return; }

        await EnsureTelemetryConsent();
    }

    private async Task EnsureTelemetryConsent()
    {
        var outcomeMsg = await Consent.LaunchConsentIfRequired(telemetryFlowName, telemetryVersion);
        if (outcomeMsg.IsError)
        {
            TelemetryEnabled = false;
            return;
        }

        switch (outcomeMsg.Data.Outcome)
        {
            case ConsentLaunchOutcome.Approved:
            case ConsentLaunchOutcome.NotRequired:
                // NotRequired could mean previously approved OR previously withdrawn —
                // we should re-check status to be sure.
                await UpdateTelemetryFromStatus();
                break;
            case ConsentLaunchOutcome.Denied:
            case ConsentLaunchOutcome.Dismissed:
            case ConsentLaunchOutcome.Unknown:
            default:
                TelemetryEnabled = false;
                break;
        }
    }

    private async Task UpdateTelemetryFromStatus()
    {
        var statusMsg = await Consent.GetConsentStatus(telemetryFlowName, telemetryVersion);
        if (statusMsg.IsError || statusMsg.Data == null || statusMsg.Data.Length == 0)
        {
            TelemetryEnabled = false;
            return;
        }
        TelemetryEnabled = statusMsg.Data[0].Status == ConsentStatus.Consented;
    }
}
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `Consent.GetConsentStatus(flowName, version, extraParams)` | `Request<ConsentStatusResult[]>` | Current consent status (no UI) |
| `Consent.LaunchConsentIfRequired(flowName, version, extraParams)` | `Request<ConsentLaunchResult>` | Show dialog if needed; return outcome |

### Models

| Type | Key fields |
|------|------------|
| `ConsentStatusResult` | `Status` (`ConsentStatus`), `Version`, `FlowName` |
| `ConsentLaunchResult` | `Outcome` (`ConsentLaunchOutcome`), `Status` |

### Enums

| Enum | Values |
|------|--------|
| `ConsentStatus` | `DefaultNotSeen`, `Seen`, `Withdrawn`, `Consented` |
| `ConsentLaunchOutcome` | `NotRequired`, `Dismissed`, `Denied`, `Approved`, `Unknown` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Treating `NotRequired` as "Approved" | `NotRequired` only means the dialog wasn't shown. The user could have withdrawn previously. Re-check `GetConsentStatus` to be sure. |
| Calling `GetConsentStatus` instead of `LaunchConsentIfRequired` for the primary flow | Use `LaunchConsentIfRequired` — it handles the show-or-skip decision internally. |
| Showing a custom Unity dialog instead of the system one | The Consent API is designed to surface the system dialog with platform-correct branding and accessibility. Use it. |
| Forgetting to bump `version` after terms change | Without a version bump, users who previously consented won't see the new terms. |
| Blocking the entire app behind the consent dialog | Show your splash screen / loading scene; let the dialog appear over it. Only gate the *features* that need the consent. |
| Treating `Dismissed` as `Denied` | Dismissed means "user closed without deciding" — design intent matters; don't punish. Keep `Denied` semantics for actual decline. |
| Calling Consent APIs before init | Always check `Core.IsInitialized()`. |
| Hardcoding flow names without checking the dashboard | Flow names are case-sensitive and must match the dashboard configuration. |

## Coding Rules

When implementing Consent flows using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` first.
- Gate every API call with `Core.IsInitialized()`.

### Primary Pattern
- Use `LaunchConsentIfRequired` as the primary entry point — it decides whether to show the dialog.
- After `Approved` or `NotRequired`, call `GetConsentStatus` if you need to distinguish "user previously approved" from "user previously withdrew" (both produce `NotRequired`).

### Versioning
- Bump the `version` string every time you materially change the terms / disclosure content.
- Keep version strings stable per terms revision (e.g., `v1`, `v2`, `v2.1`).

### UX
- Don't block app launch behind the consent dialog. Show splash; let dialog appear over.
- Gate only the *features* that depend on consent (telemetry, sharing, age-gated content).
- Don't replace the system dialog with a custom one.

### Outcome Semantics
- `Approved` = user agreed in this session.
- `NotRequired` = no dialog shown. Re-check `GetConsentStatus` if you need the underlying state.
- `Denied` = user actively declined.
- `Dismissed` = user closed without deciding — generally treat as "not consented" but don't penalize.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest Consent Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-consent-management/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/consent/`
