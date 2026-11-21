---
name: my-scene-manager-scene-transitions
description: "Use this skill whenever the user wants to move from one scene to another with My Scene Manager — e.g. 'go to the next level', 'switch from the main menu to the game scene', 'change scene with a loading screen', 'restart the level', 'reload the current scene', 'keep my HUD scene loaded between levels', 'do something after the new scene is up', 'MySceneManager.TransitionAsync'. Covers TransitionAsync and ReloadActiveSceneAsync, choosing which target scene becomes active, multi-scene targets, persistent scenes that survive transitions, and the exact order a transition loads and unloads things. Do NOT use for building the loading screen itself (see my-scene-manager-loading-screens), additive loads that keep the current scene (see my-scene-manager-load-and-unload-scenes) or progress bars, awaiting and cancelling (see my-scene-manager-operation-handles). When in doubt whether a scene-change request could involve My Scene Manager, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "My Scene Manager"
  publisher: "My GameDev Tools"
  asset-version: "5.1.1"
  skill-version: "1.0.3"
  unity: "6000.0+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/utilities"
  asset-store-url: "https://assetstore.unity.com/packages/slug/313159"
  documentation-url: "https://scene-loader.mygamedevtools.com/"
  support-url: "https://github.com/mygamedevtools/scene-loader/issues"
  last-verified: "2026-11-19"
---

# Transition Between Scenes with My Scene Manager

A transition replaces the **active** scene with one or more target scenes, optionally through a loading screen, in one call: `MySceneManager.TransitionAsync(target, loadingScreen)`. A reload is the same transition with the active scene as its own target. This skill lets an agent write those calls correctly — which scene ends up active, what gets unloaded and what survives, and how to run code at the right moment.

## When to use this skill

- "go from the main menu to Level1" / "load the next level"
- "change scene with a loading screen in between"
- "restart the level" / "reload the current scene"
- "enter the level with its lighting scene loaded too"
- "my UI scene should stay loaded across levels"
- "start the intro once the new scene is ready"

Not for:

- Designing the loading screen (fades, progress bars, minimum time) → `my-scene-manager-loading-screens`.
- Loading a scene **on top of** the current one, or unloading one → `my-scene-manager-load-and-unload-scenes`.
- Progress values, awaiting, coroutines, cancelling → `my-scene-manager-operation-handles`.
- Errors and hangs → `my-scene-manager-debug-scene-loading`.

## Prerequisites

- My Scene Manager 5.x on Unity 6000.0+, any render pipeline.
- **Install check:** `System.Type.GetType("MyGameDevTools.SceneLoading.CoreSceneManager, MyGameDevTools.SceneLoading") != null` in the Editor. If it is `null`, tell the user the package is missing and point them to https://assetstore.unity.com/packages/slug/313159 (full check: `my-scene-manager-setup-and-overview`).
- Every target scene **and** every loading scene must be resolvable: an enabled Build Settings entry (by scene name or full `Assets/…/Name.unity` path) or an Addressables entry.
- Transitions are started from `Start`, a button handler or later — never from `Awake`/`OnEnable` of the first scene, where `MySceneManager` does not exist yet.

## Quick start

Goal: a button in `MainMenu` that goes to `Level1` through the loading scene `Loading`.

1. Make sure `MainMenu`, `Level1` and `Loading` are enabled in the Build Settings (`File > Build Profiles` > **Scene List**). The loading scene is optional: if the project has none and the user did not ask for one, call `TransitionAsync(_targetScene)` with no second argument (or build one with `my-scene-manager-loading-screens`). Never pass the name of a scene that does not exist — the transition faults with "Could not resolve the scene".
2. Create `Assets/Scripts/PlayButton.cs`:

   ```csharp
   using MyGameDevTools.SceneLoading;
   using UnityEngine;

   public class PlayButton : MonoBehaviour
   {
       [SerializeField] string _targetScene = "Level1";
       [SerializeField] string _loadingScene = "Loading";

       // Called by the menu's button; see step 3 for the wiring.
       public void Play() => MySceneManager.TransitionAsync(_targetScene, _loadingScene);
   }
   ```
3. Add `PlayButton` to an object in `MainMenu` and hook `Play()` to the menu's existing button, in the UI framework the menu already uses:
   - UI Toolkit: on the object with the menu's `UIDocument`, in `OnEnable`: `GetComponent<UnityEngine.UIElements.UIDocument>().rootVisualElement.Q<UnityEngine.UIElements.Button>("play").clicked += Play;` (use the button's actual `name`).
   - uGUI: the `Button`'s **On Click ()** list in the Inspector, or `button.onClick.AddListener(Play)` from `Start`.
   - No menu UI yet: build it in the framework the rest of the project uses; if the project has both or neither, ask the user which to use.
4. Enter Play Mode from `MainMenu` and press the button.

Expected result: `Loading` appears in the Hierarchy, `MainMenu` unloads, `Level1` loads and becomes the active scene (bold in the Hierarchy), then `Loading` unloads. Final Hierarchy: `Level1` only (plus any persistent scenes).

## Workflows

### Workflow: Transition without a loading screen

**Goal:** swap the active scene for another directly.

**Steps:**
1. Call `MySceneManager.TransitionAsync("Level1");` — the loading screen parameter defaults to `null`.
2. Nothing else is needed. When the active scene is the only loaded scene, the package briefly creates an empty scene named `loading-screen-host` so Unity never has zero scenes loaded; it is unloaded before the operation completes.

**Expected result:** the old active scene unloads, `Level1` loads and is active; the operation's states are `Unloading → Loading → Activating → Completed` (plus `Resolving` first if a string has to be looked up in Addressables).

### Workflow: Transition through a loading scene

**Goal:** show a loading scene while the target loads.

**Steps:**
1. Pass the loading scene as the second argument. A name, full path, build index, loaded `Scene` or (with Addressables) an `AssetReference` all convert to a scene-based loading screen:

   ```csharp
   MySceneManager.TransitionAsync("Level1", "Loading");
   MySceneManager.TransitionAsync(2, "Loading");                      // kinds can differ
   MySceneManager.TransitionAsync(levelReference, loadingReference);  // AssetReferences
   ```
2. The loading scene is resolved like any other scene — keep it in the Build Settings or Addressables.
3. To make it fade, show progress or stay up for a minimum time, give the loading scene a `LoadingBehavior` and friends (`my-scene-manager-loading-screens`). Without a `LoadingBehavior`, it is shown for exactly as long as the load takes.
4. A prefab or UI Toolkit loading screen is passed the same way, as an instance of a `LoadingScreen` subclass: `MySceneManager.TransitionAsync("Level1", new PrefabLoadingScreen(prefab));`.

The full order of a transition with a loading screen:

1. Resolve all target references (a bad reference fails here, before anything moves).
2. Load the loading scene (not reported as the operation's `Loading` phase).
3. State `ScreenIn`: wait for the screen's **show** gate (e.g. a fade-in).
4. State `Unloading`: unload the active (source) scene.
5. State `Loading`: load the targets; progress is forwarded to the loading screen.
6. State `Activating`: link the loaded scenes and set the active one.
7. State `ScreenOut`: tell the screen loading is complete, wait for its **hide** gate (e.g. a fade-out).
8. Unload the loading scene, then `Completed`.

**Expected result:** the operation reaches `Completed`; neither the source scene nor the loading scene is loaded afterwards.

### Workflow: Transition to several scenes and choose the active one

**Goal:** enter a level made of multiple scenes in one operation.

**Steps:**
1. Pass an array. The first scene becomes active by default — a transition always activates something, because it unloads the scene you came from:

   ```csharp
   MySceneManager.TransitionAsync(new[] { "Level1", "Level1_Lighting" }, "Loading");
   ```
2. To activate a different one, name its index with `SceneParameters`:

   ```csharp
   var parameters = new SceneParameters(new SceneRef[] { "Level1_Lighting", "Level1" }, 1);
   MySceneManager.TransitionAsync(parameters, "Loading");
   ```

   Mixed kinds are fine: `new SceneRef[] { "Level1", 3, SceneRef.Address("Level1_Audio") }`. An index outside the array throws `ArgumentOutOfRangeException` when the `SceneParameters` is constructed.
3. Remember that the **next** transition only unloads the active scene. The other scenes from this group stay loaded unless you unload them (see the persistent-scene workflow).

**Expected result:** all scenes in the group are loaded; `MySceneManager.GetActiveScene()` is the one at the chosen index.

### Workflow: Keep a scene loaded across transitions

**Goal:** a HUD, audio or game-state scene that survives level changes, without `DontDestroyOnLoad`.

**Steps:**
1. Load the persistent scene additively once, without making it active:

   ```csharp
   using MyGameDevTools.SceneLoading;
   using UnityEngine;
   using UnityEngine.SceneManagement;

   public class Bootstrap : MonoBehaviour
   {
       [SerializeField] string _hudScene = "HUD";

       void Start()
       {
           // SceneManager.sceneCount includes scenes still loading, so this never starts a second load.
           for (int i = 0; i < SceneManager.sceneCount; i++)
               if (SceneManager.GetSceneAt(i).name == _hudScene)
                   return;

           MySceneManager.LoadAsync(_hudScene);
       }
   }
   ```
2. Transition between levels as usual. `TransitionAsync` unloads only the **active** scene, so the HUD scene stays.
3. Do not make the persistent scene active. If it ever becomes active, the next transition unloads it.
4. When a level brought extra non-active scenes with it (previous workflow), unload them explicitly before or after the next transition: `MySceneManager.UnloadAsync("Level1_Lighting");`.

**Expected result:** after several transitions the persistent scene is still loaded exactly once, and only the current level is otherwise loaded.

### Workflow: Reload the active scene

**Goal:** restart the current level, with or without a loading screen.

**Steps:**
1. Call `MySceneManager.ReloadActiveSceneAsync();` or `MySceneManager.ReloadActiveSceneAsync("Loading");`.
2. It is a transition whose target is the active scene, reloaded through the same reference that loaded it. The very first scene (loaded by Unity, not by the package) is reloaded through its asset path, so it has to be in the Build Settings or addressable under that path.
3. If there is no active scene the manager tracks, the call throws `InvalidOperationException` **synchronously** ("Cannot reload the active scene because it is null or not loaded"), before returning a handle.

**Expected result:** the operation's `Kind` is `Reload`; the same scene is loaded and active afterwards, with fresh object state.

### Workflow: Run code when the new scene is ready

**Goal:** start gameplay, a cutscene or analytics at the right point of a transition.

**Steps:**
1. **After everything, including the loading screen being gone** — await the operation (or use its `Completed` event):

   ```csharp
   async void Play()
   {
       try
       {
           SceneResult result = await MySceneManager
               .TransitionAsync("Level1", "Loading")
               .CancelWith(Application.exitCancellationToken);
           Debug.Log($"Entered {result.GetScene().name}");
       }
       catch (System.OperationCanceledException) { /* left Play Mode mid-transition */ }
   }
   ```
2. **As soon as the targets are loaded and active, while the screen starts hiding** — watch for `SceneOperationState.ScreenOut`:

   ```csharp
   SceneOperation op = MySceneManager.TransitionAsync("Level1", "Loading");
   op.StateChanged += o =>
   {
       if (o.State == SceneOperationState.ScreenOut)
           Debug.Log("Target is active; loading screen is now hiding.");
   };
   ```
3. **From inside the target scene** — its own `Awake`/`Start` run when it loads, which needs no reference to the operation.
4. Do not tie the operation to `destroyCancellationToken` of a component in the scene being left: that scene is unloaded mid-transition, the token fires, and the transition is canceled. Use `Application.exitCancellationToken` or a token owned by a persistent object.

**Expected result:** the code runs once per transition at the intended moment.

## Verification

Run in Play Mode after the transition finishes (e.g. from a `Completed` handler or your Editor code-execution tool):

- `op.State == SceneOperationState.Completed` and `op.Exception == null`.
- `MySceneManager.GetActiveScene().name` equals the target (or the chosen index for multi-scene targets).
- `SceneManager.GetSceneByName("<source>").isLoaded == false` and `SceneManager.GetSceneByName("<loading scene>").isLoaded == false`.
- `SceneManager.GetSceneByName("loading-screen-host").IsValid() == false` once complete.
- Persistent scenes are loaded exactly once: count matches of `SceneManager.GetSceneAt(i).name` over `SceneManager.sceneCount`.
- No `[MySceneManager]` errors or warnings in the Console.
- Testing from an agent: an unfocused Editor stops advancing Play Mode, so for the test session set `Application.runInBackground = true` (in the Editor this also changes `PlayerSettings.runInBackground`; restore it afterwards). Register any logging or sampling hook before the operation starts (for example from a `[RuntimeInitializeOnLoadMethod]` method), because short operations finish before a probe attached afterwards sees them.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MySceneManager.TransitionAsync(SceneParameters, LoadingScreen = null)` | static method | Unloads the active scene and loads the targets, optionally through a loading screen; always activates one target (index 0 by default) |
| `MySceneManager.ReloadActiveSceneAsync(LoadingScreen = null)` | static method | Transition back to the active scene; throws `InvalidOperationException` if there is none |
| `SceneParameters(SceneRef[] refs, int setIndexActive)` | constructor | Several targets plus which one to activate; also `(string[], int)`, `(int[], int)`, `(SceneRef, bool)` |
| `LoadingScreen` | abstract class | Second argument; strings, build indices, `Scene`s and `AssetReference`s convert to a `SceneLoadingScreen` |
| `SceneOperation.StateChanged` / `State` | event / property | Phase of the transition: `Resolving`, `ScreenIn`, `Unloading`, `Loading`, `Activating`, `ScreenOut`, `Completed` |
| `MySceneManager.GetActiveScene()` | static method | The active scene as the manager tracks it |

## Common issues

- **The transition is canceled halfway** → `CancelWith(destroyCancellationToken)` from a component in the source scene. → Use `Application.exitCancellationToken` or a persistent owner's token.
- **A scene from an earlier multi-scene transition is still loaded** → transitions only unload the active scene. → `UnloadAsync` the extra scenes explicitly.
- **The persistent scene got unloaded** → it was the active scene when the transition started (it was loaded with `setActive`, or `SetActiveScene` was called on it). → Load it without activation; keep a level scene active.
- **`InvalidOperationException` from `ReloadActiveSceneAsync`** → the manager has no loaded active scene, e.g. its active scene was unloaded through `UnityEngine.SceneManagement.SceneManager` directly, or every scene it tracked was unloaded. → Load, activate and unload scenes through `MySceneManager` only.
- **"Could not resolve the scene" for the loading scene** → the loading scene is not in the Build Settings or Addressables. → Register it like any other scene.
- **The loading screen never goes away / the transition stays in `ScreenIn` or `ScreenOut`** → a show or hide hold was never released. → See `my-scene-manager-debug-scene-loading`.
- **Two transitions overlap** → the package does not queue or block concurrent transitions. → Keep the returned `SceneOperation` and ignore new requests while `!op.IsDone`.

## Boundaries

- There is no single-mode load; a transition is the replacement for it, and everything is loaded additively.
- Canceling a transition stops its remaining phases and its waiters, but scenes Unity has started loading still finish loading; nothing is rolled back.
- A transition unloads exactly one scene (the active one). Unloading arbitrary sets of scenes is a separate `UnloadAsync` call.
- Visual effects between scenes come from the loading screen; the package has no built-in shader or camera transitions.
- Scenes loaded through `UnityEngine.SceneManagement.SceneManager` directly are not tracked and are never unloaded by a transition.
