---
name: Horizon Platform SDK — Implement IAP (In-App Purchases)
description: Use this skill when implementing In-App Purchases (IAP) in a Meta Quest Unity app using the Horizon Platform SDK. Covers initialization, product catalog, checkout flow, purchase verification, and consumable management.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity IAP Implementation Guide

You are an expert in implementing In-App Purchases for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`).

## Prerequisites

Before implementing IAP:
1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Create IAP products** in the Developer Dashboard under your app's "In-App Purchases" section
3. **Note your App ID** and product **SKUs** (case-sensitive)
4. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System.Threading.Tasks;
```

All Platform SDK types live under `Oculus.Platform`. Models (Product, Purchase, etc.) live under `Oculus.Platform.Models`.

## Step 1: Initialize the Platform

You **must** initialize the Platform SDK before calling any IAP methods. `Core.IsInitialized()` gates all API calls.

### Async/Await (Recommended)

```csharp
async void Start()
{
    try
    {
        Message<PlatformInitialize> msg = await Core.AsyncInitialize("YOUR_APP_ID");
        if (msg.IsError)
        {
            Debug.LogError($"Platform init failed: {msg.GetError().Message}");
            return;
        }
        Debug.Log("Platform initialized");
    }
    catch (Exception e)
    {
        Debug.LogException(e);
    }
}
```

### Callback Pattern

```csharp
void Start()
{
    Core.AsyncInitialize("YOUR_APP_ID").OnComplete(msg =>
    {
        if (msg.IsError)
        {
            Debug.LogError("Platform init failed");
            return;
        }
        Debug.Log("Platform initialized");
    });
}
```

### Editor Testing

In the Unity Editor, use **Standalone Platform** mode with test user credentials:
- Open **Meta > Platform > Edit Settings**
- Check "Use Standalone Platform"
- Enter test user email/password and click Login
- Set "Use Meta Quest App ID over Rift App ID in Editor" if needed

```csharp
void Start()
{
#if UNITY_EDITOR
    // Initialize with runtime mode for editor testing
    Core.AsyncInitialize("YOUR_APP_ID", "standalone").OnComplete(msg =>
    {
        if (!msg.IsError) isInitialized = true;
    });
#else
    Core.AsyncInitialize("YOUR_APP_ID").OnComplete(msg =>
    {
        if (!msg.IsError) isInitialized = true;
    });
#endif
}
```

## Step 2: Fetch Product Catalog

Retrieve product details (name, price, description) for your SKUs:

```csharp
public async Task LoadProducts()
{
    string[] skus = new string[] { "gem_pack_100", "premium_upgrade", "power_boost" };

    try
    {
        Message<ProductList> msg = await IAP.GetProductsBySKU(skus);
        if (msg.IsError)
        {
            Debug.LogError($"GetProductsBySKU failed: {msg.GetError().Message}");
            return;
        }

        foreach (Product product in msg.Data)
        {
            Debug.Log($"Product: {product.Name}, SKU: {product.Sku}, Price: {product.FormattedPrice}, Type: {product.Type}");
        }
    }
    catch (Exception e)
    {
        Debug.LogException(e);
    }
}
```

### Product Model Fields

| Field | Type | Description |
|-------|------|-------------|
| `Name` | string | Display name |
| `Sku` | string | Unique identifier (case-sensitive) |
| `FormattedPrice` | string | Locale-formatted price (e.g., "$9.99") |
| `Description` | string | Full description |
| `ShortDescription` | string | Brief description |
| `Type` | ProductType | `Consumable`, `Durable`, or `Subscription` |
| `Price` | Price | Structured price (CurrencyCode, Amount, FormattedPrice) |
| `IconUrl` | string | Product icon URI |
| `CoverUrl` | string | Product cover image URI |

## Step 3: Launch Checkout Flow

Open the system checkout UI for the user to purchase a product:

```csharp
public async Task PurchaseProduct(string sku)
{
    try
    {
        Message<Purchase> msg = await IAP.LaunchCheckoutFlow(sku);
        if (msg.IsError)
        {
            var error = msg.GetError();
            // Check for user cancellation
            if (error.Message.Contains("user_canceled"))
            {
                Debug.Log("User cancelled the purchase");
                return;
            }
            Debug.LogError($"Purchase failed: {error.Message}");
            return;
        }

        Purchase purchase = msg.Data;
        Debug.Log($"Purchase successful! SKU: {purchase.Sku}, ID: {purchase.ID}");

        // For consumables: grant the item and then consume the purchase
        if (purchase.Type == ProductType.Consumable)
        {
            GrantItemToPlayer(purchase.Sku);
            await ConsumeItem(purchase.Sku);
        }
    }
    catch (Exception e)
    {
        Debug.LogException(e);
    }
}
```

### Purchase Model Fields

| Field | Type | Description |
|-------|------|-------------|
| `Sku` | string | Product SKU (case-sensitive) |
| `ID` | string | Unique purchase ID |
| `Type` | ProductType | Consumable, Durable, or Subscription |
| `GrantTime` | DateTime | When the entitlement was granted |
| `ExpirationTime` | DateTime | Expiration (subscriptions only) |

## Step 4: Verify Existing Purchases

Check what the user has already purchased (for restoring entitlements on reinstall):

```csharp
public async Task RestorePurchases()
{
    try
    {
        Message<PurchaseList> msg = await IAP.GetViewerPurchases();
        if (msg.IsError)
        {
            Debug.LogError($"GetViewerPurchases failed: {msg.GetError().Message}");
            return;
        }

        foreach (Purchase purchase in msg.Data)
        {
            Debug.Log($"Owned: {purchase.Sku} (Type: {purchase.Type})");
            GrantEntitlement(purchase.Sku);
        }
    }
    catch (Exception e)
    {
        Debug.LogException(e);
    }
}
```

### Durable Cache Fallback

If the network call fails, use the on-device cache for durable (non-consumable) purchases:

```csharp
public async Task RestorePurchasesWithFallback()
{
    Message<PurchaseList> msg = await IAP.GetViewerPurchases();
    if (msg.IsError)
    {
        Debug.LogWarning("Network failed, checking device cache...");
        msg = await IAP.GetViewerPurchasesDurableCache();
    }

    if (!msg.IsError && msg.Data != null)
    {
        foreach (Purchase purchase in msg.Data)
        {
            GrantEntitlement(purchase.Sku);
        }
    }
}
```

## Step 5: Consume Purchases (Consumables Only)

After granting a consumable item to the player, consume it so it can be purchased again:

```csharp
public async Task ConsumeItem(string sku)
{
    try
    {
        Message msg = await IAP.ConsumePurchase(sku);
        if (msg.IsError)
        {
            Debug.LogError($"ConsumePurchase failed for {sku}: {msg.GetError().Message}");
            return;
        }
        Debug.Log($"Consumed: {sku}");
    }
    catch (Exception e)
    {
        Debug.LogException(e);
    }
}
```

**Important**: Always grant the item to the player **before** consuming. If the app crashes between consume and grant, the player loses the item with no recourse.

## Complete IAP Manager Example

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class IAPManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";
    [SerializeField] private string[] productSkus = { "gem_pack_100", "premium_upgrade" };

    private bool isInitialized;
    private Dictionary<string, Product> productCatalog = new();

    async void Start()
    {
        await InitializePlatform();
        if (isInitialized)
        {
            await LoadProductCatalog();
        }
    }

    private async Task InitializePlatform()
    {
        try
        {
            var msg = await Core.AsyncInitialize(appId);
            if (msg.IsError)
            {
                Debug.LogError($"Platform init failed: {msg.GetError().Message}");
                return;
            }
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private async Task LoadProductCatalog()
    {
        try
        {
            var msg = await IAP.GetProductsBySKU(productSkus);
            if (msg.IsError)
            {
                Debug.LogError($"Failed to load products: {msg.GetError().Message}");
                return;
            }
            foreach (var product in msg.Data)
            {
                productCatalog[product.Sku] = product;
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async Task Purchase(string sku)
    {
        if (!isInitialized || !Core.IsInitialized()) return;

        try
        {
            var msg = await IAP.LaunchCheckoutFlow(sku);
            if (msg.IsError) return;

            var purchase = msg.Data;
            GrantItem(purchase.Sku);

            if (purchase.Type == ProductType.Consumable)
            {
                await IAP.ConsumePurchase(purchase.Sku);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    public async Task RestoreEntitlements()
    {
        if (!isInitialized) return;

        try
        {
            var msg = await IAP.GetViewerPurchases();
            if (msg.IsError) return;

            foreach (var purchase in msg.Data)
            {
                GrantItem(purchase.Sku);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void GrantItem(string sku)
    {
        // Implement your game-specific item granting logic here
        Debug.Log($"Granted item: {sku}");
    }
}
```

## IAP API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `IAP.GetProductsBySKU(string[] skus)` | `Request<ProductList>` | Fetch product details for given SKUs |
| `IAP.LaunchCheckoutFlow(string sku)` | `Request<Purchase>` | Open checkout UI for a product |
| `IAP.GetViewerPurchases()` | `Request<PurchaseList>` | Get all purchases (consumable + durable) |
| `IAP.GetViewerPurchasesDurableCache()` | `Request<PurchaseList>` | Get durable purchases from device cache |
| `IAP.ConsumePurchase(string sku)` | `Request` | Consume a consumable purchase |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Calling IAP methods before init | Always check `Core.IsInitialized()` first |
| SKU case mismatch | SKUs are case-sensitive — must match Dashboard exactly |
| Consuming before granting | Grant the item first, then consume |
| Not restoring purchases | Call `GetViewerPurchases()` on app start to restore entitlements |
| Ignoring user cancellation | Check error message for `"user_canceled"` — this is normal |
| Not handling pagination | Use `IAP.GetNextProductListPage(list)` if `list.HasNextPage` |

## Coding Rules

When implementing In-App Purchases using the Horizon Platform SDK (`com.meta.xr.sdk.platform`):

### Initialization

- Always call `Core.AsyncInitialize("YOUR_APP_ID")` before any Platform API call.
- Gate all API calls with `Core.IsInitialized()`.
- In the Unity Editor, use Standalone Platform mode with test user credentials via **Meta > Platform > Edit Settings**.

### Request Handling

- Prefer `async/await` over the callback pattern for new code.
- Always check `msg.IsError` before accessing `msg.Data`.
- Wrap async calls in `try/catch` to handle exceptions.
- For paginated results, check `list.HasNextPage` and use `GetNext*ListPage(list)`.

### IAP Best Practices

- SKUs are **case-sensitive** — must match the Developer Dashboard exactly.
- Always **grant the item before consuming** a consumable purchase.
- Restore purchases on app start with `IAP.GetViewerPurchases()`.
- Use `IAP.GetViewerPurchasesDurableCache()` as a fallback when network is unavailable.
- Handle user cancellation by checking the error message for `"user_canceled"`.

### Namespace

- Use `Oculus.Platform` namespace (backward compatible with legacy SDK).
- Models live under `Oculus.Platform.Models`.

## Useful Links

- [Meta Quest IAP Documentation](https://developer.oculus.com/documentation/unity/ps-iap/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
