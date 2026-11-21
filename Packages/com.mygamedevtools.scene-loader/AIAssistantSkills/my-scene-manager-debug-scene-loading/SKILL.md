---
name: my-scene-manager-debug-scene-loading
description: "Use this skill whenever a My Scene Manager load, unload, transition or loading screen fails or misbehaves — e.g. 'my scene won't load', 'Could not resolve the scene', 'MySceneManager not available before the first scene is fully loaded', 'the transition is stuck on the loading screen', 'loading screen never goes away', 'it loaded the wrong copy of my scene', 'my addressable scene loads from the build settings instead', 'Unable to match a managed scene', 'turn on scene loader logs'. Covers SceneManagerLog levels and handlers, reading [MySceneManager] console messages, inspecting faulted operations, and a symptom-to-fix map for every error and warning the package emits. Do NOT use for first-time installation (see my-scene-manager-setup-and-overview) or for designing a new loading screen (see my-scene-manager-loading-screens). When in doubt whether a scene loading problem could involve My Scene Manager, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "My Scene Manager"
  publisher: "My GameDev Tools"
  asset-version: "5.1.1"
  skill-version: "1.0.2"
  unity: "6000.0+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/utilities"
  asset-store-url: "https://assetstore.unity.com/packages/slug/313159"
  documentation-url: "https://scene-loader.mygamedevtools.com/"
  support-url: "https://github.com/mygamedevtools/scene-loader/issues"
  last-verified: "2026-11-19"
---

# Diagnose Scene Loading Problems in My Scene Manager

My Scene Manager reports everything through one sink, `SceneManagerLog`, with every message prefixed `[MySceneManager]`. Failures inside an operation do not throw at the call site; they **fault the operation** (`State == Faulted`, `Exception` set, an error logged), and awaiting it rethrows. Hangs are almost always a loading-screen hold that is never released, and the package names the holder after 10 seconds. This skill lets an agent turn the diagnostics up, read what they say, and map each message to its fix.

## When to use this skill

- "my scene won't load" / "nothing happens when I call TransitionAsync"
- "Could not resolve the scene 'X'"
- "the static Scene Manager instance is not available before the first scene is fully loaded"
- "the loading screen stays up forever"
- "it loaded the build settings copy, not my addressable scene"
- "Unable to match a managed scene with … Is the scene loaded?"
- "turn on verbose logs for scene loading" / "route scene logs to my in-game console"

Not for:

- Installing the package, Addressables or uGUI → `my-scene-manager-setup-and-overview`.
- Designing a loading screen from scratch → `my-scene-manager-loading-screens`.
- Normal handling of cancellation and results → `my-scene-manager-operation-handles`.

## Prerequisites

- My Scene Manager 5.x on Unity 6000.0+, any render pipeline.
- **Install check:** `System.Type.GetType("MyGameDevTools.SceneLoading.SceneManagerLog, MyGameDevTools.SceneLoading") != null` in the Editor. If `null`, the package is missing or older than 5.0 — tell the user and point to https://assetstore.unity.com/packages/slug/313159 (and to the 4→5 upgrade guide for 4.x projects).
- Access to the Console (Editor or a development build). The 10-second wait warnings appear only when `Debug.isDebugBuild` is true (the Editor and development players).

## Quick start

Goal: find out why `MySceneManager.LoadAsync("Level2")` does nothing visible.

1. Raise the log level before the operation starts, from a component in the first scene:

   ```csharp
   using MyGameDevTools.SceneLoading;
   using UnityEngine;

   public class SceneDiagnostics : MonoBehaviour
   {
       void Awake() => SceneManagerLog.Level = SceneLogLevel.Verbose; // SceneManagerLog is usable in Awake
   }
   ```
2. Reproduce in Play Mode and read the `[MySceneManager]` lines in the Console:
   - `Load operation started.` → the call ran.
   - `Load operation entered Resolving.` / `Resolved 'Level2' to …` → how the string was interpreted.
   - `Load operation faulted during <State>: <message>` → the failure; match the message in Common issues.
3. Fix the cause, then restore the default level (remove the component, or set `SceneLogLevel.Warning`).

Expected result: the Console names the exact failing step, and after the fix the log shows `entered Completed` for the operation.

## Workflows

### Workflow: Turn diagnostics up and capture them

**Goal:** see every step the manager takes.

**Steps:**
1. Set `SceneManagerLog.Level` (`Off`, `Error`, `Warning`, `Info`, `Verbose`). Default: `Warning` in the Editor and development builds, `Error` in release builds. It is runtime-settable, so it can be raised inside a shipped build (for example from a debug menu).
2. What each level adds:
   - `Error` — faulted operations, contained subscriber exceptions, components that disabled themselves.
   - `Warning` — Build Settings/Addressables double matches, 10-second waits, multiple `LoadingBehavior`s, destroyed holders.
   - `Info` — every operation start and every cancellation.
   - `Verbose` — every state change, every string resolution, every scene-to-handle link, holder-scene creation, `LoadingBehavior` registration.
3. The level resets to the default at the start of each Play Mode session, so set it again in each session that needs it.

**Expected result:** `[MySceneManager]` lines appear for each phase of each operation.

### Workflow: Route logs to a custom handler

**Goal:** send package diagnostics to an in-game console, a file or analytics.

**Steps:**
1. Implement `UnityEngine.ILogHandler` and assign it:

   ```csharp
   using System;
   using MyGameDevTools.SceneLoading;
   using UnityEngine;

   public class InGameConsoleHandler : ILogHandler
   {
       public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
           => InGameConsole.Add(logType, string.Format(format, args)); // your console

       public void LogException(Exception exception, UnityEngine.Object context)
           => InGameConsole.Add(LogType.Exception, exception.ToString());
   }

   // At startup:
   SceneManagerLog.Handler = new InGameConsoleHandler();
   ```
2. Assigning `null` restores the Unity console; it does **not** silence. Use `SceneLogLevel.Off` to silence.
3. A handler that throws is contained: the message goes to the Unity console instead, with the handler's exception attached.

**Expected result:** package messages appear in the custom sink; the scene operations are unaffected by handler failures.

### Workflow: Diagnose a transition stuck on its loading screen

**Goal:** find and release the hold that blocks the transition.

**Steps:**
1. Wait 10 seconds in the Editor. A development build logs one of:
   - `A transition has been waiting 10 seconds for <holder> to call ReleaseShow()` → stuck in `ScreenIn` (before the source scene unloads).
   - `… to call ReleaseHide()` → stuck in `ScreenOut` (after the target is active).
   - `A <Kind> operation has been waiting 10 seconds on <scenes>` → Unity's own load/unload is slow or stuck (very large scene, Addressables download).
2. The holder is named as `<Type> on '<object>' (scene '<scene>')`. Open that script and find its `HoldShow`/`HoldHide` call and the matching release.
3. Typical causes: the release is in an animation callback that never fires; the animation runs on scaled time while `Time.timeScale == 0`; the release is skipped by an early return; the hold was taken on a self-owned `LoadingProgress` by a plain C# object (never auto-released).
4. Fix the release (unscaled time, release when the animation ends) and re-test. To check gates live, read `IsShown` / `IsHidden` on the `LoadingProgress` (`LoadingBehavior.Progress`).

**Expected result:** no 10-second warning; the operation passes `ScreenIn` and `ScreenOut` and reaches `Completed`.

### Workflow: Diagnose a scene that resolves wrongly or not at all

**Goal:** make a string reach the intended scene and backend.

**Steps:**
1. With `Verbose` on, read `Resolved '<key>' to scene with name/path '<key>' (build index N)` (Build Settings) or `… to scene with addressable address '<key>'` (Addressables).
2. Build Settings keys are the **file name** (`Level1`) and the **full asset path** (`Assets/Scenes/Level1.unity`) of each **enabled** entry. Partial paths such as `Scenes/Level1` do not match.
3. Build Settings win over Addressables. To force Addressables, use `SceneRef.Address("Level1")` or remove the scene from the Build Settings.
4. List what the Build Settings offer at runtime:

   ```csharp
   for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings; i++)
       Debug.Log($"{i}: {UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i)}");
   ```
5. Changes to the Build Settings made during Play Mode are only seen after re-entering Play Mode.

**Expected result:** the `Resolved` line names the intended backend and the load completes.

### Workflow: Inspect a faulted operation

**Goal:** get the real exception from an operation that failed.

**Steps:**
1. Read it from the handle:

   ```csharp
   SceneOperation op = MySceneManager.LoadAsync("Level2");
   op.Completed += o =>
   {
       if (o.State == SceneOperationState.Faulted)
           Debug.LogException(o.Exception);
   };
   ```
2. Or `await` it inside `try/catch (Exception)`; the original exception is rethrown.
3. Distinguish where errors surface: invalid **arguments** (empty string, negative index, index to activate out of range, invalid `AssetReference`) throw at the call site; `ReloadActiveSceneAsync` with no active scene throws at the call site; everything else faults the operation.

**Expected result:** the exact exception type and message, matched against Common issues or `references/messages.md`.

## Verification

- With `SceneManagerLog.Level = SceneLogLevel.Verbose`, the operation's log ends with `<Kind> operation entered Completed.`
- `op.State == SceneOperationState.Completed` and `op.Exception == null`.
- No `[MySceneManager]` warnings or errors in the Console for the reproduced scenario.
- For hangs: no `has been waiting 10 seconds` warning, and the Verbose log shows `entered ScreenOut.` followed by `entered Completed.`
- `SceneManagerLog.Level` is back to its default (or the diagnostics component is removed) when done.
- Testing from an agent: an unfocused Editor stops advancing Play Mode, so for the test session set `Application.runInBackground = true` (in the Editor this also changes `PlayerSettings.runInBackground`; restore it afterwards). Register any logging or sampling hook before the operation starts (for example from a `[RuntimeInitializeOnLoadMethod]` method), because short operations finish before a probe attached afterwards sees them.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `SceneManagerLog.Level` | static property | Severity threshold (`SceneLogLevel.Off`…`Verbose`); runtime-settable |
| `SceneManagerLog.Handler` | static property | `ILogHandler` sink; `null` restores the Unity console |
| `SceneOperation.State` / `Exception` | properties | `Faulted` plus the exception that caused it |
| `LoadingProgress.IsShown` / `IsHidden` | properties | Whether the show/hide gates are open |
| `SceneOperationPump.GateWarningSeconds` | const (10) | Wait before a blocked gate or engine operation is reported |
| `SceneRefResolver.Invalidate()` | static method | Drops cached string resolutions (automatic when the Build Settings count changes) |
| `MySceneManager.TryGetDefault(out ISceneManager)` | static method | Safe access when the manager may not exist yet or anymore |

## Common issues

- **`InvalidOperationException: [MySceneManager] The static Scene Manager instance is not available before the first scene is fully loaded…`** → `MySceneManager` used in `Awake`/`OnEnable` of the first scene, or in `OnDestroy` after Play Mode ended. → Use `Start`, or `MySceneManager.TryGetDefault`.
- **`… operation faulted during …: Could not resolve the scene 'X'. It was not found in the build settings or the Addressables catalog.`** (or "…build settings (Addressables is not installed…)") → not an enabled Build Settings entry, misspelled, partial path, or not an Addressables key. → Fix the name/path, register the scene, or mark it addressable.
- **Warning `The scene 'X' matches both the build settings and an Addressables entry. The build settings take precedence.`** → duplicate registration. → Remove one, or load with `SceneRef.Address("X")`.
- **Warning `A transition has been waiting 10 seconds for … to call ReleaseShow()` / `ReleaseHide()`** → an unreleased hold. → Fix the named component's release (Workflow above).
- **`… faulted during …: Unable to match a managed scene with … Is the scene loaded?`** → unloading a scene the manager does not track (loaded via `SceneManager` directly, left behind by a canceled load, still loading, or already unloaded). → Unload only manager-loaded, fully loaded scenes; prefer passing the `Scene`.
- **`InvalidOperationException: [CoreSceneManager] Cannot set active the scene "X" (…) that has not been loaded through this CoreSceneManager.`** → `SetActiveScene` on an untracked scene. → Load it through the manager.
- **`InvalidOperationException: [CoreSceneManager] Cannot reload the active scene because it is null or not loaded.`** → the manager's active scene is gone (unloaded through `SceneManager` directly, or every tracked scene was unloaded). → Load a scene through the manager with activation first; never unload manager scenes through `SceneManager`.
- **Error `<Component> on '<object>' (scene '<scene>') found no LoadingBehavior on itself or its parents, and none was assigned. Disabling it.`** → loading-screen component outside the behaviour's hierarchy. → Move it or assign the field.
- **Warning `Found N LoadingBehaviors in scene '…'`** → more than one per screen. → Keep one.
- **Error `A <Kind> operation's <event> subscriber threw, and was contained: …`** → a bug in your event handler, not in the load. → Fix the handler (the stack trace is included).
- **Warning `Tried to create a Scene Manager with all loaded scenes, but encoutered none.`** → `new CoreSceneManager(true)` constructed in `Awake`. → Construct it in `Start`.

Every other message, with its cause and fix, is listed in `references/messages.md` — read it when a message is not in this list.

## Boundaries

- The package diagnoses its own operations. Engine-level problems (a scene missing from a build, Addressables content not built, a scene's own scripts throwing in `Awake`) surface as Unity or Addressables errors; read those too.
- There is no compile-time logging switch; filtering is runtime only, and messages are built at the call site.
- The 10-second wait warnings are development-build only and never abort the wait.
- Scenes loaded or unloaded through `UnityEngine.SceneManagement.SceneManager` are invisible to the manager; mixing the two is a common root cause rather than a package bug. Report genuine package bugs at https://github.com/mygamedevtools/scene-loader/issues.
