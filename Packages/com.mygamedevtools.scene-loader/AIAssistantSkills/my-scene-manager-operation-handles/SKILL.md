---
name: my-scene-manager-operation-handles
description: "Use this skill whenever the user wants to track, wait for, or cancel a My Scene Manager scene operation — e.g. 'show a progress bar for the scene load', 'await the scene load', 'do something after the level finishes loading', 'cancel the load if the player quits', 'wait for these two loads together', 'log every scene load for analytics', 'OperationCanceledException from TransitionAsync'. Covers the SceneOperation handle returned by LoadAsync, UnloadAsync, TransitionAsync and ReloadActiveSceneAsync: await, ToCoroutine, AsTask, Progress and its events, State phases, Result, Cancel and CancelWith, WhenAll and WhenAny, and global OperationStarted listeners. Do NOT use for loading-screen visuals (see my-scene-manager-loading-screens). When in doubt whether a scene-loading progress or async request could involve My Scene Manager, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "My Scene Manager"
  publisher: "My GameDev Tools"
  asset-version: "5.1.1"
  skill-version: "1.0.5"
  unity: "6000.0+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/utilities"
  asset-store-url: "https://assetstore.unity.com/packages/slug/313159"
  documentation-url: "https://scene-loader.mygamedevtools.com/"
  support-url: "https://github.com/mygamedevtools/scene-loader/issues"
  last-verified: "2026-11-20"
---

# Track, Await and Cancel My Scene Manager Operations

Every My Scene Manager call — `LoadAsync`, `UnloadAsync`, `TransitionAsync`, `ReloadActiveSceneAsync` — returns a `SceneOperation` immediately, before any work starts. That handle carries the operation's phase, progress, result and failure, and is how you wait for it (`await`, coroutine or `Task`), cancel it, or combine it with others. None of the four methods take a progress or `CancellationToken` parameter; you attach those to the handle. This skill lets an agent write correct waiting, progress, cancellation and instrumentation code around those handles.

## When to use this skill

- "fill a progress bar while the scene loads"
- "after the level loads, spawn the player" / "await the transition"
- "I use coroutines — how do I wait for the load?"
- "cancel the load when the object is destroyed or the game quits"
- "load two areas and continue when both are done"
- "log every scene operation and how long it took"
- "why do I get `OperationCanceledException` when awaiting?"

Not for:

- Switching scenes or reloading → `my-scene-manager-scene-transitions`.
- A progress bar **inside a loading screen** → `my-scene-manager-loading-screens` (use the feedback components there).

## Prerequisites

- My Scene Manager 5.x on Unity 6000.0+, any render pipeline.
- **Install check:** `System.Type.GetType("MyGameDevTools.SceneLoading.SceneOperation, MyGameDevTools.SceneLoading") != null` in the Editor. If `null`, tell the user the package is missing: https://assetstore.unity.com/packages/slug/313159. If `MySceneManager` does not exist (error CS0103), the project defines `DISABLE_STATIC_SCENE_MANAGER` on purpose: keep the define and call the same methods on the project's own `ISceneManager` instance (search its code for `new CoreSceneManager`).
- Code using the handle lives in a script that has `using MyGameDevTools.SceneLoading;`.
- Operations are started from `Start` or later, never from `Awake`/`OnEnable` of the first scene.
- Testing from an agent: an unfocused Editor stops advancing Play Mode, so for the test session set `Application.runInBackground = true` (in the Editor this also changes `PlayerSettings.runInBackground`; restore it afterwards). Register any logging or sampling hook before the operation starts (for example from a `[RuntimeInitializeOnLoadMethod]` method), because short operations finish before a probe attached afterwards sees them.

## Quick start

Goal: load `Level1`, show its progress on a progress bar in a persistent HUD scene, then log the loaded scene.

1. Use the project's UI framework for the bar. UI Toolkit if the project's UI is UXML/`UIDocument`, uGUI if it is Canvas-based; if it uses both or neither, ask the user (the detection rule is in `my-scene-manager-loading-screens` Prerequisites). The package's handle does not care which.
2. Put this component in a scene that stays loaded during the operation (not the scene being unloaded), with the `ShowProgress` body matching the framework:

   ```csharp
   using System;
   using MyGameDevTools.SceneLoading;
   using UnityEngine;
   using UnityEngine.UIElements; // UI Toolkit variant

   public class LevelLoader : MonoBehaviour
   {
       public async void LoadLevel(string scene)
       {
           SceneOperation op = MySceneManager.LoadAsync(new SceneParameters(scene, true));
           op.Progressed += ShowProgress; // 0..1, raised only when it changes

           try
           {
               SceneResult result = await op.CancelWith(destroyCancellationToken);
               Debug.Log($"Loaded {result.GetScene().name}");
           }
           catch (OperationCanceledException)
           {
               // This object was destroyed before the load finished.
           }
       }

       // UI Toolkit: a <ui:ProgressBar name="loading-bar" high-value="1" /> in this object's UIDocument.
       void ShowProgress(float value) =>
           GetComponent<UIDocument>().rootVisualElement.Q<ProgressBar>("loading-bar").value = value;

       // uGUI alternative: [SerializeField] UnityEngine.UI.Slider _bar;  void ShowProgress(float value) => _bar.value = value;
   }
   ```
3. Call `LoadLevel("Level1")` from the HUD's button or from `Start`.

Expected result: the bar moves from 0 to 1, `Level1` becomes active, and the Console logs `Loaded Level1`.

## Workflows

### Workflow: Await an operation

**Goal:** continue code after the operation finishes.

**Steps:**
1. `await` the handle directly. Continuations resume on the main thread (the package drives operations from the player loop), so Unity APIs are safe after the `await`.
2. From a MonoBehaviour event or button handler, use `async void` and catch exceptions — nothing awaits an `async void`:

   ```csharp
   async void OnPlayClicked()
   {
       try
       {
           SceneResult result = await MySceneManager.TransitionAsync("Level1", "Loading");
           Scene[] all = result.GetScenes();
       }
       catch (OperationCanceledException) { /* Cancel() was called */ }
       catch (Exception e) { Debug.LogException(e); /* the operation faulted */ }
   }
   ```
3. Outcomes: success returns the `SceneResult`; a canceled operation throws `OperationCanceledException`; a faulted one rethrows its `Exception`.
4. The handle is re-awaitable and keeps `Result` after completion; awaiting a finished operation returns immediately.

**Expected result:** code after `await` runs once, after the operation reaches `Completed` (or the catch runs).

### Workflow: Wait from a coroutine

**Goal:** wait inside an `IEnumerator`.

**Steps:**
1. `yield return op.ToCoroutine();`

   ```csharp
   IEnumerator LoadThenSpawn()
   {
       SceneOperation op = MySceneManager.LoadAsync("Level1");
       yield return op.ToCoroutine();

       if (op.State == SceneOperationState.Completed)
           SpawnPlayer(op.Result.GetScene());
   }
   ```
2. A fault rethrows inside the coroutine. A cancellation simply ends the wait — check `op.State` afterwards.
3. Run the coroutine on an object that outlives the operation; a coroutine on an object in a scene being unloaded stops with it.

**Expected result:** the coroutine resumes once `op.IsDone` is `true`.

### Workflow: Bridge to a Task

**Goal:** hand the operation to an API that needs `Task<SceneResult>`.

**Steps:**
1. `Task<SceneResult> task = op.AsTask();`
2. Canceled → the task is canceled; faulted → the task faults with the operation's exception.
3. Prefer `await op` and `SceneOperation.WhenAll` / `WhenAny` otherwise: `AsTask` allocates a `TaskCompletionSource` per call.

**Expected result:** the task completes when the operation does.

### Workflow: Show progress

**Goal:** drive UI from the operation's progress.

**Steps:**
1. Subscribe to `op.Progressed` (raised only when the value changes) or poll `op.Progress` (0 to 1) in `Update`.
2. Progress is the average across the operation's target scenes during `Loading`; it reaches 1 on completion. A transition's loading screen scene is not counted.
3. Mixing Build Settings and Addressables scenes in one group advances unevenly, because Addressables progress includes download time.
4. Keep the UI in a scene that is not unloaded by the operation (see the persistent-scene workflow in `my-scene-manager-scene-transitions`), or use the loading-screen feedback components instead.

**Expected result:** the UI reaches 1 when the operation completes.

### Workflow: React to phases

**Goal:** run code at a specific point of an operation.

**Steps:**
1. Subscribe to `op.StateChanged` and read `o.State`:

   | State | Meaning |
   |---|---|
   | `Pending` | Created, not started |
   | `Resolving` | Looking a string up in the Addressables catalog (skipped when the Build Settings answer) |
   | `ScreenIn` | Waiting for the loading screen to finish showing (transitions with a screen) |
   | `Unloading` | Unloading the transition's source scene, or an unload's targets |
   | `Loading` | Target scenes loading; what `Progress` describes |
   | `Activating` | Linking loaded scenes and setting the active one |
   | `ScreenOut` | Targets are active; waiting for the loading screen to finish hiding |
   | `Completed` / `Canceled` / `Faulted` | Terminal |
2. Each phase occurs at most once, in this order, and operations skip phases they do not use (a plain load never sees `ScreenIn`).
3. "Target is active, screen is starting to hide" is `ScreenOut`; "screen fully gone" is `Completed` (or the `await` returning).
4. Per-scene events: `op.SceneLoaded` and `op.SceneUnloaded` fire once per scene. In `SceneUnloaded` the scene is already gone (`name` and `path` are empty), so compare it with a `Scene` you kept. `op.Completed` fires once on any terminal state; subscribing after completion invokes the handler immediately.
5. A subscriber that throws is logged and contained; it never faults the operation or blocks other subscribers.

**Expected result:** handlers fire once at the intended phase.

### Workflow: Cancel an operation

**Goal:** stop waiting for, and reporting, an operation.

**Steps:**
1. `op.Cancel();` — the state becomes `Canceled`, waiters are released (awaits throw `OperationCanceledException`), remaining phases are skipped.
2. Tie it to a token: `MySceneManager.LoadAsync("Area2").CancelWith(token);` (returns the same handle, so it chains).
3. Pick a token whose owner **outlives** the operation. `destroyCancellationToken` of a component in the scene a transition unloads fires mid-transition and cancels it. `Application.exitCancellationToken` (leaving Play Mode or quitting) or a persistent object's token are safe choices.
4. Cancellation does **not** abort the Unity load: a scene that started loading still finishes and stays loaded. Worse, a canceled load stops before the manager tracks the scene, so `MySceneManager.UnloadAsync` cannot find it afterwards ("Unable to match a managed scene").
5. **To back out of a load and get rid of the scene, do not cancel.** Keep the handle, remember that the player backed out, and unload the scene once the load completes:

   ```csharp
   SceneOperation _loading;
   bool _backedOut;

   public void StartLoading()
   {
       _backedOut = false;
       _loading = MySceneManager.LoadAsync("NextArea");
       _loading.Completed += op =>
       {
           if (_backedOut && op.State == SceneOperationState.Completed)
               MySceneManager.UnloadAsync(op.Result.GetScene()); // tracked, so this works
       };
   }

   public void BackOut() => _backedOut = true; // the area is removed as soon as it finishes loading
   ```

   If an operation was already canceled, the leftover scene can only be removed with `UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene)` once it has loaded (watch `SceneManager.sceneLoaded`); the manager never knew about it.

**Expected result:** `op.State == SceneOperationState.Canceled`; later phases (for example the unload of a transition's source) do not run. For the back-out pattern: the scene loads, then unloads, and never becomes active.

### Workflow: Combine operations

**Goal:** wait for several independent operations.

**Steps:**
1. `SceneOperation all = SceneOperation.WhenAll(opA, opB);` completes when every operation has finished; its `Result` holds every scene in order. `SceneOperation any = SceneOperation.WhenAny(opA, opB);` completes with the first finisher's result; the others keep running.
2. The composite faults as soon as any member faults. A **canceled** member counts as finished (contributing no scenes) — check members' states if that matters.
3. Canceling the composite does not cancel its members; cancel them individually.
4. The composite's `Kind` is `Composite`. Its `Progress` stays at 0 until it completes (then 1); average the members' `Progress` for a live value.

**Expected result:** code after `await all` runs once every member is done.

### Workflow: Observe every operation globally

**Goal:** one place for analytics, logging or a debug HUD.

**Steps:**
1. Subscribe to `MySceneManager.OperationStarted` from `Start` or later. It is raised before the operation's first state change, so the whole lifecycle is observable:

   ```csharp
   using MyGameDevTools.SceneLoading;
   using UnityEngine;

   public class SceneOperationLogger : MonoBehaviour
   {
       void Start() => MySceneManager.OperationStarted += OnStarted;

       void OnDestroy()
       {
           // The manager may already be torn down when leaving Play Mode.
           if (MySceneManager.TryGetDefault(out ISceneManager manager))
               manager.OperationStarted -= OnStarted;
       }

       void OnStarted(SceneOperation op)
       {
           float startedAt = Time.realtimeSinceStartup;
           op.StateChanged += o => Debug.Log($"{o.Kind} → {o.State}");
           op.Completed += o => Debug.Log($"{o.Kind} ended as {o.State} in {Time.realtimeSinceStartup - startedAt:0.00}s");
       }
   }
   ```
2. Put the logger in a persistent scene so transitions do not destroy it.

**Expected result:** every operation logs its phases and a final line with its terminal state.

## Verification

- After completion: `op.IsDone == true`, `op.State == SceneOperationState.Completed`, `op.Exception == null`, `op.Progress == 1`.
- `op.Result.GetScenes()` is non-null and its length equals the number of scenes requested.
- Cancellation paths: `op.State == SceneOperationState.Canceled` and the awaiting code took the `OperationCanceledException` branch.
- Global listeners: one `OperationStarted` call per operation started (count them in a test).
- Console: no "subscriber threw, and was contained" errors and no unhandled exceptions from `async void` methods.
- Exercise every path you wrote, not only the happy one: cancel or back out mid-load (and, for exit tokens, exit Play Mode while loading) and check the Console for unhandled exceptions and the scene list for leftovers a few seconds later.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `SceneOperation.State` / `Kind` / `Progress` / `Result` / `Exception` / `IsDone` | properties | Live status of the operation |
| `SceneOperation.Progressed` / `StateChanged` / `SceneLoaded` / `SceneUnloaded` / `Completed` | events | Change notifications; `Completed` fires on any terminal state |
| `await op` (`GetAwaiter()`) | awaiter | Returns `SceneResult`; throws on cancel or fault |
| `op.ToCoroutine()` | method | `IEnumerator` for `yield return`; faults rethrow, cancel ends the wait |
| `op.AsTask()` | method | `Task<SceneResult>` bridge |
| `op.Cancel()` / `op.CancelWith(CancellationToken)` | methods | Cancel now, or when a token fires |
| `SceneOperation.WhenAll(...)` / `WhenAny(...)` | static methods | Composite operations |
| `MySceneManager.OperationStarted` | static event | Every operation, before it runs |

## Common issues

- **`OperationCanceledException` from an awaited transition the code never canceled** → `CancelWith(destroyCancellationToken)` from a component in the scene being unloaded. → Use a token owned by something that outlives the operation.
- **Code after `await` never runs, and nothing is logged** → an exception escaped an `async void` without a `try`, or the awaiting object was destroyed. → Wrap awaits in `try/catch`; await from a persistent object.
- **The progress bar jumps or goes backwards between operations** → several operations share one bar, or Build Settings and Addressables scenes are mixed. → One bar per operation; treat progress as relative, not as time.
- **The progress bar disappears halfway through a transition** → it lived in the scene being unloaded. → Move it to a persistent scene or the loading screen.
- **"There are 2 audio listeners in the scene" floods the Console after an additive load** → both scenes have their own Main Camera with an `AudioListener`. → Disable the first scene's camera once the new scene is active, unload the first scene, or keep a single camera in a persistent scene.
- **A canceled load still left the scene loaded, and `UnloadAsync` says "Unable to match a managed scene"** → cancellation cannot abort Unity's load, and the canceled operation never tracked the scene. → Don't cancel to back out; use the back-out pattern in "Workflow: Cancel an operation". For a scene left by an earlier cancel, use `SceneManager.UnloadSceneAsync(scene)`.
- **`warning CS4014` on a `MySceneManager` call inside an `async` method** → `SceneOperation` is awaitable, so an unawaited call in an `async` method is flagged. → `await` it, keep the handle (`SceneOperation op = …`), or discard explicitly (`_ = MySceneManager.LoadAsync(…);`).
- **`WhenAll` completed although a member was canceled** → canceled members count as finished. → Inspect members' `State`.
- **`InvalidOperationException` touching `MySceneManager` in `OnDestroy`** → the manager was torn down when Play Mode ended. → Unsubscribe through `MySceneManager.TryGetDefault`.

## Boundaries

- Cancellation is cooperative: it stops reporting and remaining phases, never Unity's in-flight load.
- There is no pause/resume and no way to hold scene activation.
- Progress is engine-reported load progress, not a time estimate; no download-size API is exposed.
- `SceneOperation` objects are not pooled; keeping a handle around is supported.
- Thread usage: the handle is driven from Unity's player loop on the main thread; do not use it from worker threads.
