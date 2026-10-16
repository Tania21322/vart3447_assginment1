---
name: Horizon Platform SDK — Implement Asset Files (DLC / On-Demand Downloads)
description: Use this skill when implementing downloadable asset files (DLC, expansion packs, optional content) in a Meta Quest Unity app using the Horizon Platform SDK. Covers GetList for inventory, Status/Download/Delete by ID or name, batch downloads, the DownloadUpdate notification for progress, and the cancel flow.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Asset Files Implementation Guide

You are an expert in implementing downloadable asset files (DLC) for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). The Asset File API lets you ship optional content separately from your main APK — keep the install size small, and let users download expansion packs, alternate language packs, or premium content on demand.

## What This API Does

| | |
|---|---|
| **Inventory** | `GetList` returns every asset configured for your app and whether it's installed |
| **Per-asset status** | `StatusByName` / `StatusById` for a single asset's `DownloadStatus` |
| **Download** | `DownloadByName` / `DownloadById` (or batch `DownloadByNameList`) |
| **Progress** | Subscribe to `SetDownloadUpdateNotificationCallback` for live updates |
| **Cancel & delete** | `DownloadCancelByName` / `DeleteByName` to free space |

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Configure your assets** in the Developer Dashboard under your app's "Builds > Asset Files" section. Each asset gets an **API Name** (case-sensitive) and **ID**. Upload the actual file payload there.
3. **Note your App ID** and asset names
4. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

The `AssetFile` static class lives in `Oculus.Platform`. Models (`AssetDetails`, `AssetDetailsList`, `AssetFileDownloadResult`, `AssetFileDownloadUpdate`, `AssetFileDeleteResult`, `AssetFileDownloadCancelResult`) live in `Oculus.Platform.Models`.

## Step 1: Initialize and Subscribe to Download Updates

Subscribe to `SetDownloadUpdateNotificationCallback` immediately after init.

```csharp
async void Start()
{
    var msg = await Core.AsyncInitialize(appId);
    if (msg.IsError) return;

    AssetFile.SetDownloadUpdateNotificationCallback(OnDownloadUpdate);
    isInitialized = true;
}

private void OnDownloadUpdate(Message<AssetFileDownloadUpdate> msg)
{
    if (msg.IsError) return;
    var u = msg.Data;
    Debug.Log($"Asset {u.AssetId}: {u.BytesTransferredLong}/{u.BytesTotalLong} bytes, status={u.TransferState}");
    UpdateProgressUi(u);
}
```

## Step 2: List All Assets

```csharp
public async Task<List<AssetDetails>> ListAssets()
{
    if (!Core.IsInitialized()) return new();

    var msg = await AssetFile.GetList();
    if (msg.IsError) return new();

    var result = new List<AssetDetails>(msg.Data);
    var page = msg.Data;
    while (page.HasNextPage)
    {
        var nextMsg = await AssetFile.GetNextAssetDetailsListPage(page);
        if (nextMsg.IsError) break;
        result.AddRange(nextMsg.Data);
        page = nextMsg.Data;
    }
    return result;
}
```

Each `AssetDetails` includes:

| Field | Meaning |
|-------|---------|
| `AssetId` | Numeric ID |
| `Filepath` | Local install path (when installed) — your runtime loads from here |
| `DownloadStatus` | `INSTALLED`, `AVAILABLE`, `IN_PROGRESS`, etc. |
| `IapStatus` | `FREE`, `ENTITLED`, `NOT_ENTITLED` (for paid DLC) |
| `Metadata` | Developer-defined string |
| `AssetType` | `DEFAULT`, `STORE`, `LANGUAGE_PACK` |

## Step 3: Check Status of a Specific Asset

```csharp
public async Task<bool> IsAssetInstalled(string assetName)
{
    var msg = await AssetFile.StatusByName(assetName);
    if (msg.IsError) return false;
    return msg.Data.DownloadStatus == "installed";
}
```

## Step 4: Download an Asset

```csharp
public async Task<string> DownloadAsset(string assetName)
{
    if (!Core.IsInitialized()) return null;

    var msg = await AssetFile.DownloadByName(assetName);
    if (msg.IsError)
    {
        Debug.LogError($"Download {assetName}: {msg.GetError().Message}");
        return null;
    }
    Debug.Log($"Downloaded {assetName} → {msg.Data.Filepath}");
    return msg.Data.Filepath;
}
```

The returned `Filepath` is the absolute path on the device — load it via standard Unity APIs (e.g., `File.ReadAllBytes`, `AssetBundle.LoadFromFile`).

### Batch Download

```csharp
public async Task DownloadMany(string[] assetNames)
{
    var msg = await AssetFile.DownloadByNameList(assetNames);
    if (msg.IsError)
    {
        Debug.LogError($"Batch download: {msg.GetError().Message}");
        return;
    }
    Debug.Log($"Batch session ID: {msg.Data}");
    // Track progress via the SetDownloadUpdateNotificationCallback you registered
}
```

> **Atomic semantics**: For batch downloads, **all assets must succeed or fail together**. There's no partial success.

## Step 5: Cancel a Download

```csharp
public async Task<bool> CancelDownload(string assetName)
{
    var msg = await AssetFile.DownloadCancelByName(assetName);
    return !msg.IsError && msg.Data.Success;
}
```

## Step 6: Delete an Installed Asset

To free space when the user no longer needs an asset:

```csharp
public async Task<bool> DeleteAsset(string assetName)
{
    var msg = await AssetFile.DeleteByName(assetName);
    return !msg.IsError && msg.Data.Success;
}
```

## Loading Asset Bundles from Downloaded Assets

A common pattern: ship Unity AssetBundles as Asset Files, then load at runtime.

```csharp
public async Task<AssetBundle> LoadAssetBundle(string assetName)
{
    var statusMsg = await AssetFile.StatusByName(assetName);
    if (statusMsg.IsError) return null;

    string filepath = statusMsg.Data.Filepath;

    // Download if not yet installed
    if (statusMsg.Data.DownloadStatus != "installed")
    {
        var dlMsg = await AssetFile.DownloadByName(assetName);
        if (dlMsg.IsError) return null;
        filepath = dlMsg.Data.Filepath;
    }

    var bundleRequest = AssetBundle.LoadFromFileAsync(filepath);
    while (!bundleRequest.isDone) await Task.Yield();
    return bundleRequest.assetBundle;
}
```

## Complete Asset File Manager

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class AssetFileManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";

    public event Action<ulong, long, long, string> ProgressUpdated; // (assetId, transferred, total, state)

    private bool isInitialized;

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        if (msg.IsError) { Debug.LogError(msg.GetError().Message); return; }

        AssetFile.SetDownloadUpdateNotificationCallback(OnDownloadUpdate);
        isInitialized = true;
    }

    private void OnDownloadUpdate(Message<AssetFileDownloadUpdate> msg)
    {
        if (msg.IsError) return;
        var u = msg.Data;
        ProgressUpdated?.Invoke(u.AssetId, u.BytesTransferredLong, u.BytesTotalLong, u.TransferState.ToString());
    }

    public async Task<List<AssetDetails>> ListAssetsAsync()
    {
        if (!isInitialized) return new();
        var msg = await AssetFile.GetList();
        if (msg.IsError) return new();
        return new List<AssetDetails>(msg.Data);
    }

    public async Task<string> EnsureDownloadedAsync(string assetName)
    {
        if (!isInitialized) return null;

        var statusMsg = await AssetFile.StatusByName(assetName);
        if (!statusMsg.IsError && statusMsg.Data.DownloadStatus == "installed")
            return statusMsg.Data.Filepath;

        var dlMsg = await AssetFile.DownloadByName(assetName);
        return dlMsg.IsError ? null : dlMsg.Data.Filepath;
    }

    public async Task<bool> CancelAsync(string assetName)
    {
        if (!isInitialized) return false;
        var msg = await AssetFile.DownloadCancelByName(assetName);
        return !msg.IsError && msg.Data.Success;
    }

    public async Task<bool> DeleteAsync(string assetName)
    {
        if (!isInitialized) return false;
        var msg = await AssetFile.DeleteByName(assetName);
        return !msg.IsError && msg.Data.Success;
    }
}
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `AssetFile.GetList()` | `Request<AssetDetailsList>` | List all assets configured for the app |
| `AssetFile.StatusByName(name)` | `Request<AssetDetails>` | Status of one asset by name |
| `AssetFile.StatusById(id)` | `Request<AssetDetails>` | Status of one asset by ID |
| `AssetFile.DownloadByName(name)` | `Request<AssetFileDownloadResult>` | Download one asset by name |
| `AssetFile.DownloadById(id)` | `Request<AssetFileDownloadResult>` | Download one asset by ID |
| `AssetFile.DownloadByNameList(names)` | `Request<int>` | Batch download (all-or-nothing) |
| `AssetFile.DownloadByIdList(ids)` | `Request<int>` | Batch download by IDs |
| `AssetFile.DownloadCancelByName(name)` | `Request<AssetFileDownloadCancelResult>` | Cancel a download |
| `AssetFile.DeleteByName(name)` | `Request<AssetFileDeleteResult>` | Delete an installed asset |
| `AssetFile.DeleteById(id)` | `Request<AssetFileDeleteResult>` | Delete by ID |
| `AssetFile.SetDownloadUpdateNotificationCallback(cb)` | (void) | Subscribe to download progress events |
| `AssetFile.GetNextAssetDetailsListPage(list)` | `Request<AssetDetailsList>` | Paginate the asset list |

### Models

| Type | Key fields |
|------|------------|
| `AssetDetails` | `AssetId`, `Filepath`, `DownloadStatus`, `IapStatus`, `Metadata`, `AssetType` |
| `AssetFileDownloadResult` | `AssetId`, `Filepath` |
| `AssetFileDownloadUpdate` | `AssetId`, `BytesTransferredLong`, `BytesTotalLong`, `TransferState` |
| `AssetFileDeleteResult` | `AssetId`, `Filepath`, `Success` |
| `AssetFileDownloadCancelResult` | `AssetId`, `Filepath`, `Success` |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Subscribing to download updates *after* starting downloads | Subscribe immediately after init so you don't miss early progress events. |
| Polling `Status` instead of using the callback | The callback delivers progress in real time; polling is wasteful. |
| Assuming partial success on batch downloads | All-or-nothing. If one asset fails, the whole batch fails. |
| Hardcoding asset paths | Always call `Status` or `Download` to get the current `Filepath` — it's not stable across reinstalls. |
| Forgetting to handle `IapStatus` for paid DLC | If the asset is paid DLC, check `IapStatus == "ENTITLED"` before attempting download. |
| Mixing IDs and names | Use one consistently. Names are easier to read in code; IDs are more stable. |
| Skipping cancel on user back-out | Without calling `DownloadCancelByName`, the download continues in the background. |
| Calling Asset File APIs before init | Always check `Core.IsInitialized()`. |

## Coding Rules

When implementing Asset Files using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` first.
- **Subscribe to `SetDownloadUpdateNotificationCallback` immediately after init** — before issuing any download.
- Gate every API call with `Core.IsInitialized()`.

### Asset Lifecycle
- Call `GetList` at app start to inventory installed and available assets.
- Always call `StatusByName` (or use a cached value) before `DownloadByName` to avoid no-op downloads.
- For paid DLC, check `IapStatus` first — only entitled assets should be downloadable.

### Download Patterns
- For single assets, prefer `DownloadByName` for readability.
- For batch downloads (e.g., starting a level that needs multiple assets), use `DownloadByNameList` and remember it's all-or-nothing.
- Use `Filepath` from the response (or `Status` result) to load into Unity (`AssetBundle.LoadFromFile`, `File.ReadAllBytes`, etc.).

### Progress UI
- Subscribe to the download update callback once, in init, and route updates to your UI from there.
- Don't poll status — use the callback.

### Cleanup
- Provide a "Delete" affordance in your UI for users to free space.
- Call `DownloadCancelByName` on user-initiated back-outs from download dialogs.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).
- Models live in `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest Asset Files Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-assetfiles/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/asset_file/`
- Related skills: `hzpsdk-iap` (for paid DLC entitlements)
