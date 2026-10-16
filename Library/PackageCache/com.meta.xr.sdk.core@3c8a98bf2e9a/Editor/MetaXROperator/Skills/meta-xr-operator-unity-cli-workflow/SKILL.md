---
name: hz-meta-xr-operator-unity-cli-workflow
description: End-to-end develop-test loop for Unity XR on Meta Quest and Horizon OS via the XR Simulator. Edits the running Editor with the `unity` CLI (Unity Editor Pipeline bridge) for scene edits, live C# eval, compilation, and Play Mode control, and validates at runtime with the Meta XR Operator (openxr_*) MCP tools.
allowed-tools:
  - Bash(unity:*)
  - Bash(metavr:*)
  - Bash(hzdb:*)
---

# Meta XR Operator + Unity CLI Development

Full develop-test cycle for Unity XR projects on the **XR Simulator** (Play Mode):

- **Edit the project** with the `unity` CLI, which drives the running Editor through the **Unity Editor Pipeline** bridge (scene queries, GameObject/component edits, live C# `eval`, compilation, Play Mode).
- **Validate at runtime** with the **Meta XR Operator** MCP tools (`openxr_*`, `unity_*`) once the app is in Play Mode.
- **Verify visually** with `openxr_capture_composited_image`.

The `unity` CLI replaces the old MCP-based scene-*editing* tools, but runtime perception and interaction still go through the **Meta XR Operator** MCP tools — `openxr_*` for the head/controllers and `unity_*` for reading the live scene. Two tool families, two phases: **edit** with the CLI (Editor), then **validate** with Meta XR Operator (running app).

Related skills:

- **hz-meta-xr-operator** — general runtime interaction and scene navigation (`get_scene_root_objects`, `get_children`, `get_world_pose`).
- **hz-meta-xr-operator-coordinates** — Unity ↔ OpenXR coordinate math and UI-element targeting.
- **hz-meta-xr-operator-interaction-poke** — poking UI buttons/toggles and physical pokeables; state verification.
- **hz-meta-xr-operator-interaction-grab** — grabbing and manipulating grabbables.
- **hz-meta-xr-operator-unity-test-mechanics** — bounded pass/fail test-attempt policy.

## The `unity` CLI Bridge

The CLI (`unity`, on PATH at `~/.unity/bin/unity`) talks to the live Editor over the Pipeline package's local HTTP server.

**One-time setup (per project):**

```bash
unity pipeline install          # installs com.unity.pipeline into the project
unity pipeline list             # confirm Running=true, Server Reachable=true
```

**Invocation form — global flags MUST come before the subcommand:**

```bash
unity --proxy-disable command <tool> [--key value ...]
```

- **`--proxy-disable` is required in this environment.** An HTTP proxy is set (`HTTP(S)_PROXY`), and the CLI routes the localhost Pipeline request through it and reports the server as *unreachable* unless the proxy is disabled. Placed **after** the subcommand it is swallowed as a tool argument — always put it (and `--no-banner`, `--format json`) first.
- **Structured args use `--key value` flags** (mapped to JSON params). A bare JSON positional is **not** parsed as filters.
- Results return as a table with a JSON `Result` column; add `--format json` for machine-readable output.
- `unity list` (also needs `--proxy-disable`) prints all ~140 available Pipeline tools.

**Check the bridge before editing:**

```bash
unity --proxy-disable command editor_status
# → {"status":"ready","compiling":false,"playMode":"stopped",...}
```

## Iteration Loop

### 1. Inspect Scene

```bash
unity --proxy-disable command get_scene_hierarchy
unity --proxy-disable command find_gameobjects --name "MyObject"
unity --proxy-disable command get_component_properties --target <handle>
```

`find_gameobjects` / `get_scene_hierarchy` return `instanceId` / handles you pass to later commands.

> **Editor-time vs runtime inspection.** These `unity` CLI tools read the **Editor** scene. Once you enter Play Mode, inspect the *running* scene with the Meta XR Operator `unity_*` tools instead (`unity_get_world_pose`, `unity_find_canvases`, `unity_find_interactables`, `get_children`) — see step 8.

### 2. Create / Modify GameObjects

```bash
unity --proxy-disable command create_gameobject --name "Panel"
unity --proxy-disable command add_component --target <handle> --type "UnityEngine.BoxCollider"
unity --proxy-disable command set_transform --target <handle> --position "0,1,-2"
unity --proxy-disable command set_component_properties --target <handle> --properties '{"enabled":true}'
```

### 3. Live C# `eval` (the universal edit path)

`eval` runs arbitrary C# in the live Editor and **replaces the old "temporary [MenuItem] editor-script" workaround entirely.** Use it whenever a structured command doesn't cover the edit — e.g. setting **TextMeshPro** text, which the structured setters don't reliably apply.

- A bare expression needs a `return`; append `;` for void/multi-statement bodies.

```bash
# Read a value
unity --proxy-disable command eval 'return UnityEngine.Application.unityVersion;'

# Set TextMeshPro text (guaranteed path)
unity --proxy-disable command eval '
  var go = UnityEngine.GameObject.Find("TARGET_NAME");
  var tmp = go.GetComponent<TMPro.TextMeshPro>();
  tmp.text = "TEXT_CONTENT"; tmp.fontSize = 10;
  tmp.alignment = TMPro.TextAlignmentOptions.Center;
  UnityEditor.EditorUtility.SetDirty(go);
  return "done";'
```

For a whole file of C#, use `eval_file --path <file.cs>`.

### 4. Compile & Check

After `create_script` (or an `eval` that adds new types), trigger a recompile and wait before using the new types:

```bash
unity --proxy-disable command create_script --path "Assets/Scripts/Foo.cs" --contents "..."
unity --proxy-disable command recompile
unity --proxy-disable command recompile_status      # poll until "completed"/"up_to_date"
unity --proxy-disable command get_console_logs      # verify no errors
```

`recompile` works even while the Editor is unfocused. `set_autotick` keeps the Editor ticking in the background if needed.

### 5. Save and Enter Play Mode

```bash
unity --proxy-disable command save_scene            # or save_all
unity --proxy-disable command editor_play           # enter Play Mode → openxr_* becomes available
```

**Exit Play Mode (`editor_stop`) before making persistent changes** to scripts, scenes, or settings — edits made in Play Mode are discarded.

### 6. Wait for XR Runtime

The XR session must reach `XR_SESSION_STATE_FOCUSED` before interaction is possible.

- Wait ~10 seconds after Play Mode starts.
- If an `openxr_*` tool returns "Server unavailable", wait and retry (5s interval, up to ~10 times).

### 7. Validate XR Session

```
openxr_get_session_info(include_history=true)
```

Expected lifecycle: IDLE → READY → SYNCHRONIZED → VISIBLE → FOCUSED.

```
openxr_get_head_pose()
```

Confirm all tracking flags (`position_valid`, `orientation_valid`, `position_tracked`, `orientation_tracked`) are true.

### 8. Navigate & Interact at Runtime

In Play Mode, read and drive the *running* scene through the **Meta XR Operator** MCP tools — `unity_*` for scene data, `openxr_*` for the head/controllers. These are a different family from the `unity` CLI (which targets the Editor) and only work while the app runs.

- **Navigate the hierarchy:** `get_scene_root_objects` → `get_children("Canvas/Panel/StartButton")` to drill down; `unity_get_world_pose("path")` for coordinates (then convert to OpenXR). See **hz-meta-xr-operator**.
- **Discover UI:** `unity_find_canvases` → `unity_find_interactables("CanvasPath")` to locate buttons, toggles, and sliders.
- **Interact through the controller, not a game script** — drive the real input path:
  - Position the controller **aim** pose at the target (`openxr_set_controller_pose(pose_type="aim")`), then act. If rotating the controller suffices, don't move its position; if you must move it, keep it in front of the head, ≤1.5m away.
  - Poke UI buttons/toggles and pokeables (fires on **release**; verify with `unity_get_toggle_state` or a side effect, not screenshots alone) → **hz-meta-xr-operator-interaction-poke**.
  - Grab and manipulate grabbables (objects, lids, doors, drawers) → **hz-meta-xr-operator-interaction-grab**.
  - Controller positioning math (Unity ↔ OpenXR, aim vs grip, `openxr_set_controller_input`) → **hz-meta-xr-operator-coordinates**.

### 9. Visual Verification

Reposition the head if needed, then capture:

```
openxr_set_head_pose(position=[x,y,z], orientation=[qx,qy,qz,qw])
# wait ~2s
openxr_capture_composited_image(eye="left")
```

Common head orientations (quaternion `[x, y, z, w]`):

- Forward (+Z): `[0, 0, 0, 1]`
- Backward (-Z): `[0, 1, 0, 0]`
- Left (-X): `[0, -0.707, 0, 0.707]`
- Right (+X): `[0, 0.707, 0, 0.707]`
- Tilt head down: rotate around -X, e.g. `[-0.707, 0, 0, 0.707]`

Confirm results with **both** scene data (`unity_get_world_pose`, `unity_get_toggle_state`) and a composited screenshot — data is authoritative, vision confirms.

### 10. Stop the App

```bash
unity --proxy-disable command editor_stop
```

Then repeat from step 1.

## Validation Strategy

**Start focused, then broaden when it's meaningful.** Verifying the exact thing you changed first is faster to write, faster to run, and pinpoints failures precisely.

1. **Start from a focused test.** Validate the specific behavior you just changed — the one GameObject, the one interaction, the one value. Prove that path works end to end before doing anything else.
2. **Extend to broad coverage when meaningful.** Once the focused test passes, widen it — related interactions, edge poses, adjacent scene state, regression-prone neighbors — *when the added coverage is worth it* (the feature is user-facing, the area is fragile, or a regression here would be costly). Don't pad with broad checks that exercise nothing your change touched.

Reach a clear pass/fail conclusion quickly rather than iterating endlessly — see the bounded 3-attempt retry policy in **hz-meta-xr-operator-unity-test-mechanics**. Report what you tested, what you expected, what happened, and any console errors.

## Unity Gotchas

### Scripts & Assets

- **Script types aren't available immediately** — after `create_script` or an `eval` that adds new types, run `recompile` and poll `recompile_status` until done before you `add_component` / `attach_script` the new type.
- **Update serialized values in the Editor, not just code defaults** — public fields are serialized in the scene. Set them via `eval` and call `EditorUtility.SetDirty(go)` so the live values update.
- **Reimport shaders/scripts after editing** — `AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate)` then `SceneView.RepaintAll()`. `Shader.Find()` returns null for unimported shaders. Reassign the shader to the material after adding/removing properties to avoid stale bindings.

### Colliders & Physics

- **Check `isTrigger`** — `OnCollisionEnter` only fires on non-trigger colliders; triggers require `OnTriggerEnter`. When in doubt, implement both.
- **Use `ContinuousDynamic` for fast projectiles** — the default `Discrete` mode lets fast objects tunnel through colliders.
- **Never spawn GameObjects from `OnDestroy`** — Unity calls it during scene teardown. Move spawn logic to the caller.

## Key Facts

- **`unity` CLI:** global flags (`--proxy-disable`, `--no-banner`, `--format json`) go **before** `command`; structured args are `--key value`; `eval` is the universal escape hatch and needs `return ...;` for values.
- **Two runtimes, two tool families:** the `unity` CLI edits the **Editor**; the `openxr_*` / `unity_*` MCP tools drive the **running app** and only work in Play Mode.
- XR head default height: ~1.7m.
- OpenXR quaternion format: `[x, y, z, w]`; `base_space` defaults to `local_floor` for the pose tools.
- Meta XR Operator (`openxr_*`) uses the **OpenXR** coordinate system (X right, Y up, **Z backward** / -Z forward, right-handed). Unity uses **Y up, Z forward** (left-handed). Convert with `convert_unity_pose_to_openxr` / `convert_openxr_pose_to_unity` (and the `*_position_*` / `*_rotation_*` variants). See **hz-meta-xr-operator-coordinates**.

## Using HZDB (if available)

Query HZDB for Oculus VR documentation on VR-specific features (hand tracking, passthrough, spatial anchors, etc.). If unavailable, proceed with general VR knowledge.

## Troubleshooting

| Symptom | Fix |
|---------|-----|
| `unity command` says "No ... reachable Pipeline servers" but `unity pipeline list` shows the server running | Add `--proxy-disable` **before** `command` — the proxy env is intercepting localhost. |
| `error: unknown option '--proxy-disable'` | You put the global flag after the subcommand; move it before `command`. |
| `unity pipeline list` shows Pipeline=false | Run `unity pipeline install` (adds `com.unity.pipeline`), then retry. |
| `eval` returns `"; expected"` (CS1002) | Bare expression needs a `return`, or end statements with `;`. |
| `set_component_properties` didn't apply (e.g. TextMeshPro) | Use `eval` (Step 3) — set the value in C# and `EditorUtility.SetDirty`. |
| New script type not found (`attach_script`/`add_component`) | Run `recompile`, poll `recompile_status` until done, then retry. |
| `openxr_*` "Server unavailable" | Confirm Play Mode (`editor_status`), wait 5+s after Play, then retry. |
| Captured image is gray | `editor_stop` then `editor_play` again. |
| Editor unresponsive to CLI | It's likely compiling — poll `recompile_status`; commands resume when idle. |
