---
name: Horizon Platform SDK — Implement Abuse Report
description: Use this skill when implementing the in-app abuse report flow in a Meta Quest Unity app using the Horizon Platform SDK. Covers subscribing to the system report-button event, launching your in-app reporting UI, and reporting back the handled/unhandled/unavailable response so the platform knows whether the flow was honored.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Abuse Report Implementation Guide

You are an expert in implementing the in-app abuse report flow for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). The Abuse Report API lets your app receive notifications when the user taps the "Report" button in the system panel (after pressing the Oculus button), so you can show your own in-app reporting UI for content the user wants to flag.

## Why This Matters

| | |
|---|---|
| **Required for User-Generated Content (UGC) apps** | If your app shows user-created content, you must support reporting per the Quest VRC. |
| **System integration** | The Quest system panel always has a Report button — this API lets you handle it instead of falling back to the system flow. |
| **Tell the platform what you did** | After the event, call `ReportRequestHandled` so the platform knows whether to show its own follow-up UI. |

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Have an in-app reporting UI** ready (or be willing to build one) — the platform will call you, you decide how to handle it
3. **Note your App ID**
4. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager
5. **HzOS v85+** is required for the report-button-pressed callback

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

The `AbuseReport` static class lives in `Oculus.Platform`. The `ReportRequestResponse` enum lives in `Oculus.Platform`.

## Step 1: Initialize and Subscribe Immediately After

Subscribe to the report-button-pressed callback **right after init** so you don't miss any pending events.

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class AbuseReportHandler : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";
    private bool isInitialized;

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        if (msg.IsError) { Debug.LogError(msg.GetError().Message); return; }

        // Subscribe immediately so we don't miss the event
        AbuseReport.SetReportButtonPressedNotificationCallback(OnReportButtonPressed);
        isInitialized = true;
    }

    private async void OnReportButtonPressed(Message<string> msg)
    {
        if (msg.IsError)
        {
            Debug.LogError($"Report-button event error: {msg.GetError().Message}");
            await AbuseReport.ReportRequestHandled(ReportRequestResponse.Unavailable);
            return;
        }

        string reportId = msg.Data;
        Debug.Log($"User tapped Report (reportId={reportId})");

        bool handled = await ShowInAppReportingUI(reportId);
        var response = handled ? ReportRequestResponse.Handled : ReportRequestResponse.Unhandled;
        await AbuseReport.ReportRequestHandled(response);
    }

    private Task<bool> ShowInAppReportingUI(string reportId)
    {
        // Your in-app reporting UI implementation:
        //   - Show a dialog asking what's being reported
        //   - Capture user input
        //   - Submit to your backend with the reportId for traceability
        //   - Return true if you handled it; false if the user cancelled
        return Task.FromResult(true);
    }
}
```

## Step 2: Respond with `ReportRequestHandled`

After your UI is done (or you decide not to show it), tell the platform what happened.

| `ReportRequestResponse` | When to use |
|--------------------------|-------------|
| `Handled` | You showed your in-app reporting UI and the user completed it (or dismissed it after seeing it) |
| `Unhandled` | You chose not to show the in-app UI (e.g., the report button was pressed in a context where reporting doesn't apply) |
| `Unavailable` | Your app doesn't have an in-app reporting flow at all — falls back to system reporting |
| `Unknown` | Don't use; reserved |

> **Always respond.** If you don't call `ReportRequestHandled`, the platform may show its own fallback UI on a delay, and the user gets a confused experience.

## Complete Abuse Report Manager

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Threading.Tasks;
using UnityEngine;

public class AbuseReportManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";
    [SerializeField] private GameObject reportingDialogPrefab;

    private bool isInitialized;
    private GameObject activeDialog;
    private TaskCompletionSource<bool> activeTcs;

    public event Action<string> ReportFlowRequested;

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        if (msg.IsError) { Debug.LogError(msg.GetError().Message); return; }

        AbuseReport.SetReportButtonPressedNotificationCallback(OnReportButtonPressed);
        isInitialized = true;
    }

    private async void OnReportButtonPressed(Message<string> msg)
    {
        if (!isInitialized) return;
        if (msg.IsError)
        {
            await AbuseReport.ReportRequestHandled(ReportRequestResponse.Unavailable);
            return;
        }

        ReportFlowRequested?.Invoke(msg.Data);

        try
        {
            bool handled = await ShowReportingUI(msg.Data);
            await AbuseReport.ReportRequestHandled(handled
                ? ReportRequestResponse.Handled
                : ReportRequestResponse.Unhandled);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            await AbuseReport.ReportRequestHandled(ReportRequestResponse.Unhandled);
        }
    }

    private Task<bool> ShowReportingUI(string reportId)
    {
        // Complete any previous TCS so its OnReportButtonPressed caller
        // doesn't hang — ensures ReportRequestHandled is always called.
        activeTcs?.TrySetResult(false);

        activeTcs = new TaskCompletionSource<bool>();

        if (activeDialog != null) Destroy(activeDialog);
        activeDialog = Instantiate(reportingDialogPrefab);
        var dialog = activeDialog.GetComponent<ReportingDialog>();
        dialog.Open(reportId, completed => activeTcs.TrySetResult(completed));

        return activeTcs.Task;
    }
}
```

Pair this with a simple `ReportingDialog : MonoBehaviour` that has Submit/Cancel buttons and invokes the callback.

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `AbuseReport.SetReportButtonPressedNotificationCallback(cb)` | (void) | Subscribe to system report-button events |
| `AbuseReport.ReportRequestHandled(response)` | `Request` | Tell the platform whether you showed an in-app flow |

### Enums

| Enum | Values |
|------|--------|
| `ReportRequestResponse` | `Handled`, `Unhandled`, `Unavailable`, `Unknown` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Subscribing late (after init + other async work) | Subscribe immediately after init so you don't miss the event. |
| Not calling `ReportRequestHandled` | Always respond. The platform shows fallback UI on a delay if you don't. |
| Always responding `Handled` regardless of what you did | Use `Unhandled` if the user cancelled or you chose not to show the UI; `Unavailable` if your app doesn't support in-app reporting. |
| Calling Abuse Report APIs before init | Always check `Core.IsInitialized()`. |
| Treating the `reportId` as required | The reportId is a tracing handle — surface it to your backend for support correlation, but don't make it user-visible. |
| Building a heavy reporting UI | Keep it lightweight: what's being reported, why, optional notes. The system already captured the screenshot. |
| Targeting older HzOS versions | Requires HzOS v85+. On older versions the callback never fires. |

## Coding Rules

When implementing Abuse Report using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` first.
- **Subscribe to `SetReportButtonPressedNotificationCallback` immediately after init** — don't await other things first.
- Gate every API call with `Core.IsInitialized()`.

### Response Etiquette
- **Always call `ReportRequestHandled`** after each event, with the most accurate response value.
- `Handled` only when you genuinely showed your UI and the user reached an endpoint (submit or cancel).
- `Unhandled` when the user cancelled before your flow completed, or you decided not to show it.
- `Unavailable` if your app doesn't have an in-app reporting flow.

### UX Guidelines
- Open the dialog in a non-blocking way — don't pause gameplay if avoidable.
- Surface the `reportId` to your backend logs for traceability.
- Keep the reporting UI simple; the system already provides reason taxonomy if you fall back to `Unavailable`.

### VRC Compliance
- For UGC apps, this is essentially required. Skipping it can cause VRC review failures.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest Abuse Report Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-abuse-reporting/)
- [Virtual Reality Checks (VRC) — Quest](https://developer.oculus.com/resources/publish-quest-req/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/abuse_report/`
