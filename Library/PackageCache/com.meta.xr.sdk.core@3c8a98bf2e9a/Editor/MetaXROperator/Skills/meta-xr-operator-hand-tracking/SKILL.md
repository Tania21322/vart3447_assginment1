---
name: hz-meta-xr-operator-hand-tracking
description: "How to drive the Meta XR Operator synthetic hand (poke, pinch, grab, fist, aim, gaze+pinch) against targets in a Unity/ISDK scene via the openxr_* MCP tools — the target-solver model, per-gesture recipes, switching between controllers and hands, and pitfalls."
allowed-tools:
  - Bash(metavr:*)
  - Bash(hzdb:*)
---

# Meta XR Operator Hand Tracking

`openxr_hand_gesture` / `openxr_set_hand_pose` inject a full-synthetic 26-joint hand you drive over MCP — no physical hand needed, on XR Simulator or Quest. For coordinate math see **hz-meta-xr-operator-coordinates**; for aiming a grabbed object (gun/tool) see **hz-meta-xr-operator-grabbed-objects**; for ISDK grabbables see **hz-meta-xr-operator-interaction-grab**.

## Mental model — pass a `target`, the layer solves the wrist

Every gesture is positioned by the wrist, but the point that acts on the world (the effector) is offset from it. `openxr_hand_gesture` takes an optional **`target` [x,y,z]** (in `base_space`) and solves the wrist so the effector lands on the target:

- **grab / fist** → palm at target
- **poke** → index tip at target
- **pinch** → thumb–index midpoint at target
- **aim** → the hand ray passes through target

With `target` set the grasp HOLDS by default (so grab/pinch can carry an object); tune timing with `approach_seconds` / `close_seconds` / `hold_seconds` / `release_seconds`. Omit `wrist_orientation` and poke/pinch face the target head-on while grab/fist use a top-down palm. Use `openxr_set_hand_pose` to move the wrist while preserving the current shape — a grabbing hand keeps holding as it moves.

## Serialize hand calls

`openxr_hand_gesture` / `openxr_set_hand_pose` block until the morph completes; issuing two concurrently returns "another input gesture is already in progress." Drive one hand at a time.

## Response fields & oracles

- `openxr_hand_gesture` returns **`edge_produced`** — `false` + a `hint` means a no-op morph (re-issuing the same shape on an engaged hand produces no open→gesture edge, so an edge-triggered ISDK select won't fire; release with `gesture="open"` first).
- `openxr_get_hand_state` reports each hand's override, delivered generation, nearest **`shape`**, and aim ray — the primary way to confirm a pose actually reached the app and to diagnose a hand stuck in a sticky gesture.
- Prefer data deltas over eyeballing captures: an object snapping/tracking the hand, `unity_get_toggle_state` flipping, or a target `unity_get_world_pose` returning "not found".

## Gesture recipes

- **POKE** (button press): hover the index tip ~8 cm above the surface, then `openxr_set_hand_pose` to push ~2 cm through it — ISDK poke needs the fingertip to cross the surface downward, not teleport onto it.
- **GRAB / PINCH-GRAB**: `openxr_hand_gesture(grab, target=…)` (palm, larger objects) or `(pinch, target=…)` (small objects); the open→close morph at the target is the edge-trigger. Carry with `openxr_set_hand_pose`; release with `gesture="open"`.
- **AIM (hand ray)**: `openxr_hand_gesture(aim, target=…)` fits the ray through the target (read it from `openxr_get_hand_state`); capture once so the hover settles, then `pinch` → `open` — ray-select fires on release.

To aim a **held object** (e.g. a gun) at a world target, calibrate the muzzle offset once, then drive `openxr_set_hand_pose(wrist_orientation=…)` and fire with `openxr_hand_gesture(pinch)` — see **hz-meta-xr-operator-grabbed-objects**.

## Gaze + pinch (eye tracking)

Eye-tracked select/manipulate — aim the gaze ray, pinch to act. Requires the **Eye Gaze Interaction** profile enabled (see Switching). Aim by **direction from the eye**: `dir = target − CenterEyeAnchor`, `math_build_quat([dir.x, dir.y, −dir.z])`, passed as the gaze orientation in the scene's `base_space` (gaze is independent of head orientation).

- **Select** (buttons/toggles): either `openxr_gaze_and_pinch` (atomic; `keep_gaze=true` to verify the reticle landed) or `openxr_set_eye_gaze_pose` then `openxr_hand_gesture(pinch)` — equivalent; gaze aims, pinch selects on release. **Precision matters**: the gaze must land on the *specific* interactable (a near-miss — e.g. the panel instead of its play button — selects nothing); confirm with `unity_get_toggle_state` or the gaze-hover highlight.
- **Distance-grab** (move an object from afar): gaze at the object, then a **sticky** `openxr_hand_gesture(pinch, auto_release=false)` grabs it; `openxr_set_hand_pose` then drags it 1:1 with the wrist; `gesture="open"` releases. The hand can be anywhere — gaze picks the target, hand motion moves it.

## Switching between controllers and hands

Injecting a synthetic hand is delivered **per hand** and does not hang, even while a controller is present — you don't need to tear down controllers just to make a hand work, and one controller + one hand can be driven at once. **To switch a session's modality (controllers → hands or back), inject the new modality on _both_ hands** — apps commonly pick which modality is "active" by priority, so leaving one side on the old input can keep the whole app in the old mode. You do **not** need `openxr_release_input_devices` to switch (that only hands control back to the physical user). Which modality actually drives the app's UI/grabs is decided by the **app**, not the layer:

- **Override BOTH hands when two controllers are held** — the layer only suppresses the controller for a hand you actually simulate, so a controller you leave in place keeps that side in controller mode.
- Confirm the bound profile with `openxr_get_active_interaction_profile`; a modality silently no-ops if its OpenXR interaction profile is disabled in project settings (no Hand Interaction Profile → no hand ray; no Eye Gaze Interaction → no gaze+pinch).

## Quest vs XR Simulator

- On Quest, `openxr_set_head_pose` is unavailable (you can't reframe the camera) — aim from the real `openxr_get_head_pose` and verify via transform/toggle deltas, not by centering a capture.
- The grab/fist default approach orientation is anchored to the publish frame, which is yaw-rotated from `local_floor` on a real headset — pass an explicit `wrist_orientation` to keep it scene-consistent. The grab still snaps and carries 1:1.
