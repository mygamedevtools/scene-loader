---
name: my-scene-manager-load-and-unload-scenes
description: "Use this skill whenever the user wants to load or unload scenes additively with My Scene Manager without leaving the current scene — e.g. 'load the UI scene on top of the game', 'stream in the next area', 'load these three scenes at once', 'unload the dungeon scene', 'load an addressable scene by its address', 'load a scene from an AssetReference', 'make the loaded scene active', 'MySceneManager.LoadAsync'. Covers LoadAsync, UnloadAsync, SceneParameters, every SceneRef kind (name, path, build index, Scene, Addressables address, AssetReference), how a string resolves between the Build Settings and Addressables, setting the active scene, and querying loaded scenes. Do NOT use for replacing or reloading the current scene (see my-scene-manager-scene-transitions) or for progress, awaiting and cancelling (see my-scene-manager-operation-handles). When in doubt whether an additive scene request could involve My Scene Manager, use this skill — Prerequisites shows how to confirm the asset is installed."
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

# Load and Unload Scenes with My Scene Manager

`MySceneManager.LoadAsync` and `MySceneManager.UnloadAsync` add scenes to, and remove scenes from, what is already loaded — always additively, never replacing the current scene. One method each covers a single scene or a group, referenced by name, path, build index, loaded `Scene`, Addressables address or `AssetReference`, because all of those convert implicitly to `SceneParameters`. This skill lets an agent pick the right reference, control which scene becomes active, unload exactly the scene it meant, and query what the manager has loaded.

## When to use this skill

- "load the pause menu scene on top of the level"
- "stream in the next chunk of the world and unload the previous one"
- "load scenes 1, 2 and 3 together and make 2 active"
- "load the addressable scene `Dungeon`" / "load this AssetReference scene"
- "unload the scene I loaded earlier"
- "which scene is active?" / "is the HUD scene loaded?"
- "react whenever a scene is loaded or unloaded"

Not for:

- Leaving the current scene for another, or restarting it → `my-scene-manager-scene-transitions`.
- Progress bars, `await`, coroutines, cancellation → `my-scene-manager-operation-handles`.
- Installing Addressables or registering scenes for the first time → `my-scene-manager-setup-and-overview`.
- Errors such as "Could not resolve the scene" → `my-scene-manager-debug-scene-loading`.

## Prerequisites

- My Scene Manager 5.x on Unity 6000.0+, any render pipeline.
- **Install check:** `System.Type.GetType("MyGameDevTools.SceneLoading.CoreSceneManager, MyGameDevTools.SceneLoading") != null` in the Editor. If `null`, tell the user the package is missing: https://assetstore.unity.com/packages/slug/313159.
- Address and `AssetReference` loading needs `com.unity.addressables` 1.19+. Code that declares `AssetReference` fields in an asmdef needs a reference to `Unity.Addressables`.
- Scenes referenced by string must be enabled Build Settings entries or Addressables entries.
- Call the manager from `Start` or later, never from `Awake`/`OnEnable` of the first scene.

## Quick start

Goal: load a `PauseMenu` scene over the current level and unload it again.

1. Ensure `PauseMenu` is an enabled Build Settings entry (`File > Build Profiles` > **Scene List**).
2. Create `Assets/Scripts/PauseMenuLoader.cs`:

   ```csharp
   using MyGameDevTools.SceneLoading;
   using UnityEngine;
   using UnityEngine.SceneManagement;

   public class PauseMenuLoader : MonoBehaviour
   {
       [SerializeField] string _pauseScene = "PauseMenu";

       Scene _loaded;
       SceneOperation _loading;

       public void Open()
       {
           if (_loading != null && !_loading.IsDone) return;   // already on its way
           if (_loaded.IsValid() && _loaded.isLoaded) return; // already open

           _loading = MySceneManager.LoadAsync(_pauseScene);
           _loading.Completed += op =>
           {
               if (op.State == SceneOperationState.Completed)
                   _loaded = op.Result.GetScene();
           };
       }

       public void Close()
       {
           if (!_loaded.IsValid()) return;
           MySceneManager.UnloadAsync(_loaded); // unload by the Scene itself: exact match
           _loaded = default;
       }
   }
   ```
3. Call `Open()` and `Close()` from buttons or input in Play Mode.

Expected result: after `Open()`, the Hierarchy shows the level and `PauseMenu`; the level stays the active scene. After `Close()`, only the level remains.

## Workflows

### Workflow: Load one scene, optionally making it active

**Goal:** add a scene and decide whether it becomes the active scene (lighting, new objects' default scene).

**Steps:**
1. Not active (default for every implicit conversion):

   ```csharp
   MySceneManager.LoadAsync("Level1_Props");                        // name
   MySceneManager.LoadAsync("Assets/Scenes/Level1_Props.unity");    // full asset path
   MySceneManager.LoadAsync(3);                                      // build index
   ```
2. Active once loaded:

   ```csharp
   MySceneManager.LoadAsync(new SceneParameters("Level1_Props", true));
   ```
3. A string is resolved when the operation starts: **enabled Build Settings entries first** (scene name or full asset path), then the Addressables catalog if it is installed. `SceneRef.Address("x")` skips the Build Settings and loads from Addressables directly.

**Expected result:** the scene is loaded; `MySceneManager.GetActiveScene()` is unchanged unless step 2 was used.

### Workflow: Load several scenes in one operation

**Goal:** load a group with one handle and one combined progress value.

**Steps:**
1. Pass an array of any single kind, or a `SceneRef[]` mixing kinds:

   ```csharp
   MySceneManager.LoadAsync(new[] { "Area2", "Area2_Audio" });
   MySceneManager.LoadAsync(new int[] { 4, 5, 6 });
   MySceneManager.LoadAsync(new SceneRef[] { "Area2", 5, SceneRef.Address("Area2_Fx") });
   ```
2. To activate one of them, add its index:

   ```csharp
   MySceneManager.LoadAsync(new SceneParameters(new SceneRef[] { "Area2_Audio", "Area2" }, 1));
   MySceneManager.LoadAsync(new SceneParameters(new[] { "Area2_Audio", "Area2" }, 1)); // string[] overload
   ```

   A negative index activates none; an index `>=` the array length throws `ArgumentOutOfRangeException` immediately.
3. The handle's `Result.GetScenes()` returns the scenes in the order requested.

**Expected result:** all scenes load; `op.Result.GetScenes().Length` equals the number requested.

### Workflow: Load Addressables scenes

**Goal:** load scenes that live in Addressables.

**Steps:**
1. By string — the same call as a Build Settings scene. It falls through to Addressables when the Build Settings have no match. The first lookup by string waits for the Addressables catalog; later ones are cached.
2. By explicit address — faster, and wins even if a Build Settings scene has the same name:

   ```csharp
   MySceneManager.LoadAsync(SceneRef.Address("Dungeon"));
   ```
3. By `AssetReference` from the Inspector:

   ```csharp
   using MyGameDevTools.SceneLoading;
   using UnityEngine;
   using UnityEngine.AddressableAssets;

   public class DungeonEntrance : MonoBehaviour
   {
       [SerializeField] AssetReference _dungeonScene;
       [SerializeField] AssetReference[] _dungeonExtras;

       public void Enter()
       {
           MySceneManager.LoadAsync(_dungeonScene); // converts implicitly
           MySceneManager.LoadAsync(SceneParameters.FromAssetReferences(_dungeonExtras, 0));
       }
   }
   ```

   An `AssetReference` with an invalid runtime key (unassigned field) throws `ArgumentException` at the call.
4. Unloading an Addressables scene releases it through `Addressables.UnloadSceneAsync` automatically.

**Expected result:** the scene loads in Play Mode (and in player builds once Addressables content is built).

### Workflow: Unload scenes

**Goal:** remove exactly the scenes intended.

**Steps:**
1. Prefer the `Scene` itself — it is an exact match:

   ```csharp
   SceneOperation load = MySceneManager.LoadAsync("Area1");
   // ...later, once load.State == SceneOperationState.Completed:
   MySceneManager.UnloadAsync(load.Result.GetScene());
   ```
2. Any other reference also works, matched against scenes the manager has loaded — by name, path, build index or the address/`AssetReference` that loaded it:

   ```csharp
   MySceneManager.UnloadAsync("Area1");
   MySceneManager.UnloadAsync(new[] { "Area1", "Area1_Audio" });
   ```

   With duplicates loaded, a non-`Scene` reference unloads the **most recently loaded** match.
3. Unloading the active scene makes the manager activate the most recently loaded remaining scene.
4. The handle's `Result` holds the unloaded scenes; their native handle may no longer be valid, so do not compare them to other `Scene` values.

**Expected result:** `SceneManager.GetSceneByName("Area1").isLoaded == false`; other scenes untouched.

### Workflow: Set the active scene and query loaded scenes

**Goal:** inspect and adjust the manager's view of the loaded scenes.

**Steps:**
1. Query:

   ```csharp
   Scene active = MySceneManager.GetActiveScene();          // invalid if none
   Scene newest = MySceneManager.GetLastLoadedScene();      // invalid if none
   if (MySceneManager.TryGetLoadedSceneByName("HUD", out Scene hud)) { /* finished loading */ }
   for (int i = 0; MySceneManager.TryGetLoadedSceneAt(i, out Scene scene); i++) { /* walk them */ }
   int loaded = MySceneManager.LoadedSceneCount;            // excludes scenes being unloaded
   int total = MySceneManager.TotalSceneCount;              // includes scenes being unloaded
   ```

   `TryGetLoadedSceneByName` only sees scenes that have **finished** loading; it is not a guard against starting the same load twice — keep the first `SceneOperation` for that.
2. Activate a scene the manager loaded: `MySceneManager.SetActiveScene(scene);`. A scene it does not track throws `InvalidOperationException`.
3. Subscribe to manager-wide events (from `Start` or later; unsubscribe through `TryGetDefault` in `OnDestroy`, because the manager may already be gone):

   ```csharp
   void Start()
   {
       MySceneManager.SceneLoaded += OnSceneLoaded;
       MySceneManager.ActiveSceneChanged += OnActiveChanged; // (previous, current), either may be invalid
   }

   void OnDestroy()
   {
       if (MySceneManager.TryGetDefault(out ISceneManager manager))
       {
           manager.SceneLoaded -= OnSceneLoaded;
           manager.ActiveSceneChanged -= OnActiveChanged;
       }
   }
   ```

**Expected result:** queries match the Hierarchy for scenes loaded through the manager.

For the full list of `SceneRef` factories, `SceneParameters` constructors and the resolution rules, read `references/scene-refs-and-parameters.md` — only needed for unusual reference kinds or when debugging which backend a string chose.

## Verification

In Play Mode, once the operation has completed:

- `op.State == SceneOperationState.Completed` and `op.Exception == null`.
- Loads: `SceneManager.GetSceneByName("<name>").isLoaded` is `true` and `MySceneManager.TryGetLoadedSceneByName("<name>", out _)` is `true`.
- Unloads: `SceneManager.GetSceneByName("<name>").isLoaded` is `false`.
- Activation: `MySceneManager.GetActiveScene().name` matches the intended scene, and `SceneManager.GetActiveScene()` matches it too.
- No `[MySceneManager]` errors in the Console.
- Testing from an agent: an unfocused Editor stops advancing Play Mode, so for the test session set `Application.runInBackground = true` (in the Editor this also changes `PlayerSettings.runInBackground`; restore it afterwards). Register any logging or sampling hook before the operation starts (for example from a `[RuntimeInitializeOnLoadMethod]` method), because short operations finish before a probe attached afterwards sees them.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MySceneManager.LoadAsync(SceneParameters)` | static method | Loads one or more scenes additively; activates one only if asked |
| `MySceneManager.UnloadAsync(SceneParameters)` | static method | Unloads one or more manager-tracked scenes |
| `SceneParameters(SceneRef, bool setActive)` / `(SceneRef[], int)` | constructors | One scene with activation, or a group plus the index to activate |
| `SceneParameters.FromAssetReferences(AssetReference[], int)` | static method | A group of `AssetReference`s plus the index to activate |
| `SceneRef.Address(string)` | static method | Forces an Addressables address, skipping the Build Settings |
| `MySceneManager.SetActiveScene(Scene)` / `GetActiveScene()` | static methods | Active scene, limited to manager-tracked scenes |
| `MySceneManager.TryGetLoadedSceneByName` / `TryGetLoadedSceneAt` / `GetLastLoadedScene` | static methods | Queries over fully loaded, manager-tracked scenes |
| `MySceneManager.SceneLoaded` / `SceneUnloaded` / `ActiveSceneChanged` | static events | Manager-wide notifications |

## Common issues

- **"Could not resolve the scene 'X'…"** → not an enabled Build Settings entry or Addressables entry, or a partial path such as `"Scenes/X"`. → Use the scene name or the full `Assets/…/X.unity` path, and register the scene.
- **The addressable copy is ignored and a warning says the scene "matches both the build settings and an Addressables entry"** → Build Settings take precedence. → Remove it from the Build Settings or load it with `SceneRef.Address("X")`.
- **`Unable to match a managed scene with … Is the scene loaded?`** → unloading a scene the manager did not load (loaded through `SceneManager` directly), one that has not finished loading, or one already unloaded. → Unload only what the manager loaded, after its load completes.
- **The same scene is loaded twice** → a second `LoadAsync` started before the first finished. → Keep the first handle and check `IsDone`, or walk `SceneManager.GetSceneAt` (it includes scenes still loading).
- **`ArgumentException: Cannot create a SceneRef from an empty string.`** → an unassigned string field. → Validate inputs before calling.
- **Loading a `Scene` value faults with "Only a resolved build index can start a non-addressable load"** → a `Scene` reference can only unload. → Load by name, path, index or address.
- **`InvalidOperationException` from `SetActiveScene`** → the scene was not loaded through this manager. → Load it through `MySceneManager` first.

## Boundaries

- Always additive. There is no `LoadSceneMode.Single` and no option to hold a loaded scene before activation; the package never sets `allowSceneActivation = false`.
- No download-size or download-status API for Addressables scenes; progress includes download time on the Addressables path only.
- Canceling an operation does not abort the Unity load underneath; the scene still finishes loading, and the manager never tracks it, so `UnloadAsync` cannot remove it. To back out of a load, keep the handle and unload the result once it completes (see `my-scene-manager-operation-handles`).
- The manager only knows scenes loaded at startup and scenes loaded through it. Do not mix in `SceneManager.LoadScene`/`UnloadSceneAsync` for the same scenes.
- Scene references resolve at runtime; a string's backend can change when the Build Settings change, with no code change.
