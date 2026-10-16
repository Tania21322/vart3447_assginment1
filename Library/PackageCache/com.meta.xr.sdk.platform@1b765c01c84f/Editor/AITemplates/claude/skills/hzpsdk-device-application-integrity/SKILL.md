---
name: Horizon Platform SDK — Implement Device & Application Integrity (Attestation)
description: Use this skill when implementing device and application integrity attestation in a Meta Quest Unity app using the Horizon Platform SDK. Covers GetIntegrityToken with a server-supplied nonce, the JWT (PS256) format, the server-side verification flow against Meta's public keys, and the security model for protecting against tampering and emulators.
apply_to_regex: '.*\.(cs|unity|asmdef)$'
---

# Horizon Platform SDK — Unity Device & Application Integrity Implementation Guide

You are an expert in implementing device and application integrity attestation for Meta Quest apps using the Horizon Platform SDK (HzPSDK) Unity package (`com.meta.xr.sdk.platform`). The Attestation API gives your backend cryptographic proof that a request is coming from a legitimate Meta Quest device running an unmodified copy of your app — critical for cheat prevention, anti-fraud, and protecting paid features.

## What This API Does

| | |
|---|---|
| **Single API** | `GetIntegrityToken(challenge_nonce)` — returns a signed JWT |
| **Server flow** | Backend issues a nonce → client requests token with that nonce → backend verifies the JWT against Meta's public keys → backend trusts the request |
| **Format** | JWT with header `{"alg":"PS256","typ":"JWT"}`, payload includes the nonce, app ID, device attestation claims |
| **Use cases** | Anti-cheat, anti-fraud, protecting backend endpoints for paid features, leaderboards anti-tamper |

## Prerequisites

1. **Register your app** at [developer.oculus.com/manage](https://developer.oculus.com/manage/)
2. **Backend that can issue nonces and verify JWT signatures** — the integrity check is only meaningful when verified server-side
3. **Note your App ID**
4. **Install the package**: `com.meta.xr.sdk.platform` via Unity Package Manager

> **Critical**: Verifying the token client-side defeats the purpose. The token must be sent to your server, which fetches Meta's public keys and verifies the JWT signature.

## Namespace & Imports

```csharp
using Oculus.Platform;
using Oculus.Platform.Models;
```

The `DeviceApplicationIntegrity` static class lives in `Oculus.Platform`.

## Step 1: Initialize the Platform

```csharp
async void Start()
{
    var msg = await Core.AsyncInitialize(appId);
    if (!msg.IsError) isInitialized = true;
}
```

Always check `Core.IsInitialized()` before any Integrity call.

## Step 2: The Full Attest-Verify Flow

### Client side (Unity)

```csharp
public async Task<string> RequestIntegrityToken()
{
    if (!Core.IsInitialized()) return null;

    // 1) Get a nonce from your backend (NOT from the client)
    string nonce = await FetchNonceFromBackend();
    if (string.IsNullOrEmpty(nonce)) return null;

    // 2) Pass the nonce to the platform to mint a JWT
    var msg = await DeviceApplicationIntegrity.GetIntegrityToken(nonce);
    if (msg.IsError)
    {
        Debug.LogError($"GetIntegrityToken failed: {msg.GetError().Message}");
        return null;
    }

    string jwt = msg.Data;
    // 3) Send the JWT back to your backend, which verifies the signature
    return jwt;
}
```

### Backend side (pseudocode)

```
function attestRequest():
    nonce = generate_random_string(32)
    store_nonce_for_user(nonce, expiry=60s)
    return nonce

function verifyToken(jwt, expected_nonce):
    public_keys = fetch_meta_public_keys()  # cache and rotate
    payload = verify_jwt_signature(jwt, public_keys)  # PS256
    assert payload.nonce == expected_nonce
    assert payload.exp > now()
    assert payload.app_id == YOUR_APP_ID
    # Additional integrity claims available — check the docs for current shape
    return payload
```

> The exact backend verification details (Meta's public-key endpoint, the full claim set) are documented at [developer.oculus.com/documentation/unity/ps-attestation-api](https://developer.oculus.com/documentation/unity/ps-attestation-api/). Always reference the current docs — the verification details are server-side and out of scope for this client skill.

## Step 3: Use the Attestation to Gate Backend Endpoints

A typical pattern for a leaderboard write that should be cheat-resistant:

### Client

```csharp
public async Task<bool> SubmitVerifiedScore(string leaderboard, long score)
{
    if (!Core.IsInitialized()) return false;

    // 1) Backend issues a nonce
    string nonce = await FetchNonceFromBackend();
    if (nonce == null) return false;

    // 2) Mint integrity token
    var tokMsg = await DeviceApplicationIntegrity.GetIntegrityToken(nonce);
    if (tokMsg.IsError) return false;
    string integrityJwt = tokMsg.Data;

    // 3) Send score + integrity JWT to backend
    var success = await PostScoreToBackend(leaderboard, score, integrityJwt, nonce);
    return success;
}
```

### Backend

The backend verifies the JWT, then writes the score (or rejects). The leaderboard write through PSDK can still happen client-side, but the backend has cryptographic proof the user is on a legit device when it grants secondary entitlements (e.g., "tournament eligibility").

## Complete Integrity Manager

```csharp
using Oculus.Platform;
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class IntegrityManager : MonoBehaviour
{
    [SerializeField] private string appId = "YOUR_APP_ID";
    [SerializeField] private string backendBaseUrl = "https://your-backend.example.com";

    private bool isInitialized;

    async void Start()
    {
        var msg = await Core.AsyncInitialize(appId);
        isInitialized = !msg.IsError;
    }

    public async Task<string> GetVerifiedTokenForRequest()
    {
        if (!isInitialized) return null;

        // Fetch a fresh nonce per request
        string nonce = await FetchNonce();
        if (string.IsNullOrEmpty(nonce)) return null;

        var tokMsg = await DeviceApplicationIntegrity.GetIntegrityToken(nonce);
        return tokMsg.IsError ? null : tokMsg.Data;
    }

    private async Task<string> FetchNonce()
    {
        using var req = UnityWebRequest.Get($"{backendBaseUrl}/attest/nonce");
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();
        if (req.result != UnityWebRequest.Result.Success) return null;
        return req.downloadHandler.text.Trim();
    }
}
```

## API Reference

| Method | Returns | Description |
|--------|---------|-------------|
| `DeviceApplicationIntegrity.GetIntegrityToken(challengeNonce)` | `Request<string>` | Returns a JWT (PS256) signed by the platform with the nonce embedded in claims |

## Common Mistakes

| Mistake | Fix |
|---------|-----|
| Verifying the JWT in Unity | Defeats the purpose. Always verify server-side. |
| Generating the nonce client-side | The nonce must come from your backend so it can be stored and matched. Client-side nonces don't prove anything. |
| Reusing the same nonce across requests | One nonce per request. Backend should expire after ~60s. |
| Caching the integrity token | Tokens are short-lived. Mint per request when you need attestation. |
| Skipping the integrity check on "low-stakes" endpoints | If the endpoint matters at all (leaderboards, IAP fulfillment), use attestation. The cost is low. |
| Hardcoding Meta's public keys | Fetch and cache them server-side; rotate per Meta's published policy. |
| Logging the JWT | It contains user/device claims. Treat as sensitive. |
| Calling DeviceApplicationIntegrity APIs before init | Always check `Core.IsInitialized()`. |

## Coding Rules

When implementing Device & Application Integrity using the Horizon Platform SDK:

### Initialization
- Always call `Core.AsyncInitialize(appId)` first.
- Gate every API call with `Core.IsInitialized()`.

### Server-Side Truth
- **The integrity check is only meaningful when verified server-side.** Client-side verification is theatre.
- The nonce **must** be issued by your backend, stored, and matched on verification.
- Meta's public keys for verification rotate — fetch and cache them per the docs' guidance.

### Token Lifecycle
- Mint a fresh token per backend request that needs attestation.
- Don't cache or reuse tokens client-side.
- Treat the JWT as sensitive — don't log it.

### When to Use Attestation
- High-value endpoints: leaderboard writes that affect tournaments, IAP fulfillment, account changes, anti-cheat.
- Low-stakes telemetry doesn't need it.

### Coordination with Other Skills
- Pair with `hzpsdk-users` — `GetUserProof` proves identity (who); `GetIntegrityToken` proves device legitimacy (where). Both are usually needed for high-trust flows.

### Namespace
- Use `Oculus.Platform` (kept for backward compatibility).

## Useful Links

- [Meta Quest Attestation API Documentation (Unity)](https://developer.oculus.com/documentation/unity/ps-attestation-api/)
- [Meta Quest Developer Dashboard](https://developer.oculus.com/manage/)
- [Platform SDK Overview](https://developer.oculus.com/documentation/unity/ps-platform-intro/)
- Sample tester: `samples/unity/Baremetal/Assets/SamplesInternal/device_application_integrity/`
- Related skills: `hzpsdk-users` (for identity verification via UserProof)
