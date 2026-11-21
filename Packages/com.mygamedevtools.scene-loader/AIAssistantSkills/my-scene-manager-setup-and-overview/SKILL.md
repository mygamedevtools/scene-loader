---
name: my-scene-manager-setup-and-overview
description: "Use this skill whenever the user wants to install, set up, verify, or get oriented with My Scene Manager (package com.mygamedevtools.scene-loader, namespace MyGameDevTools.SceneLoading) — e.g. 'set up the scene loader', 'is my scene manager package installed', 'import the loading scene sample', 'add addressables support to scene loading', 'MySceneManager says it is not available before the first scene is fully loaded', 'where do I start with MySceneManager'. Covers install checks, optional Addressables, uGUI and TextMeshPro support, registering scenes in the Build Settings, the Loading Scene Examples sample, and an index of the other My Scene Manager skills. Do NOT use for writing transitions (see my-scene-manager-scene-transitions), additive loads (see my-scene-manager-load-and-unload-scenes) or loading screens (see my-scene-manager-loading-screens). When in doubt whether a scene loading request could involve My Scene Manager, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "My Scene Manager"
  publisher: "My GameDev Tools"
  asset-version: "5.1.1"
  skill-version: "1.0.4"
  unity: "6000.0+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/utilities"
  asset-store-url: "https://assetstore.unity.com/packages/slug/313159"
  documentation-url: "https://scene-loader.mygamedevtools.com/"
  support-url: "https://github.com/mygamedevtools/scene-loader/issues"
  last-verified: "2026-11-19"
---

# Set Up My Scene Manager

My Scene Manager replaces hand-written `SceneManager.LoadSceneAsync` / `UnloadSceneAsync` / `SetActiveScene` sequences with four calls — `LoadAsync`, `UnloadAsync`, `TransitionAsync` and `ReloadActiveSceneAsync` — on the static `MySceneManager` class. One string reaches a scene whether it lives in the Build Settings or in Addressables, and every call returns a `SceneOperation` handle for progress, phases, awaiting and cancellation. This skill gets a project to the point where those calls work: the package is confirmed installed, optional integrations are in place, scenes are reachable by name, and the agent knows where everything lives.

## When to use this skill

- "add My Scene Manager to my project" / "is the scene loader package installed?"
- "set things up so I can call `MySceneManager.TransitionAsync`"
- "make my scenes loadable by name" / "it can't find my scene"
- "add Addressables support to my scene loading"
- "import the loading scene examples and run them"
- "`MySceneManager` throws 'not available before the first scene is fully loaded'"
- "where are the scene loader's scripts and components?"

Not for — the other skills in this set:

- `my-scene-manager-scene-transitions` — switch from the current scene to another, reload the current scene, keep a persistent scene across levels.
- `my-scene-manager-load-and-unload-scenes` — additive loads and unloads, every way to reference a scene, the active scene.
- `my-scene-manager-loading-screens` — loading scenes, prefab and UI Toolkit screens, fades, progress bars, minimum display time.
- `my-scene-manager-operation-handles` — await, coroutines, progress, phases, cancellation, combining operations.
- `my-scene-manager-debug-scene-loading` — errors, hangs, wrong scene resolved, logging.

## Prerequisites

- **Unity** 6000.0 or newer.
- **Asset version** 5.1.1. The 5.x API (`SceneRef`, `SceneParameters`, `SceneOperation`) is not compatible with 4.x; projects on 4.x follow the [upgrade guide](https://scene-loader.mygamedevtools.com/docs/upgrades/from-4-to-5).
- **Render pipeline** — any. The runtime has no pipeline-specific code; the sample offers to convert its materials when imported into a URP or HDRP project.
- **Dependencies** — none required. Optional packages switch on extra types through version defines on the package's own assembly:

| Package | Define (inside the package only) | Unlocks |
|---|---|---|
| `com.unity.addressables` >= 1.19 | `ENABLE_ADDRESSABLES` | Loading scenes by address or `AssetReference`; bare strings fall back to the Addressables catalog |
| `com.unity.ugui` >= 1.0 | `ENABLE_UGUI` | `LoadingFeedbackSlider`, `LoadingFeedbackText` |
| `com.unity.ugui` >= 2.0 (or `com.unity.textmeshpro` >= 2.2) | `ENABLE_TMP` | `LoadingFeedbackTextMeshPro` |

- **UI framework** — the package works with UI Toolkit and uGUI alike. uGUI is only needed for the ready-made uGUI loading-screen components above; a UI Toolkit project needs no extra package. Follow the framework the project already uses; if it uses both or neither, ask the user before building UI (detection rule: `my-scene-manager-loading-screens` Prerequisites).

**Install check** — run before doing anything else. Place it in an `Editor` folder as a temporary script, or run the body through your Editor code-execution tool:

```csharp
using UnityEditor;
using UnityEngine;

public static class MySceneManagerInstallCheck
{
    [MenuItem("Tools/My Scene Manager/Check Install")]
    public static void Check()
    {
        // CoreSceneManager always exists; MySceneManager is compiled out by DISABLE_STATIC_SCENE_MANAGER.
        System.Type core = System.Type.GetType("MyGameDevTools.SceneLoading.CoreSceneManager, MyGameDevTools.SceneLoading");
        if (core == null)
        {
            Debug.LogError("My Scene Manager is NOT installed.");
            return;
        }

        UnityEditor.PackageManager.PackageInfo info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(core.Assembly);
        Debug.Log(info != null
            ? $"My Scene Manager {info.version} installed through the Package Manager ({info.source}) at {info.assetPath}."
            : "My Scene Manager is installed from the Asset Store (imported under Assets/). Read 'version' from its package.json.");

        bool staticApi = System.Type.GetType("MyGameDevTools.SceneLoading.MySceneManager, MyGameDevTools.SceneLoading") != null;
        Debug.Log(staticApi
            ? "The static MySceneManager API is available."
            : "DISABLE_STATIC_SCENE_MANAGER is defined: use a CoreSceneManager instance instead of MySceneManager.");
    }
}
```

If the check fails, stop and tell the user: *"My Scene Manager (com.mygamedevtools.scene-loader) is not installed. It is free on the Asset Store: https://assetstore.unity.com/packages/slug/313159 — or install it from OpenUPM (`openupm add com.mygamedevtools.scene-loader`) or the git URL `https://github.com/mygamedevtools/scene-loader.git#upm`."* Do not install it without the user's agreement: installing edits `Packages/manifest.json`.

## Quick start

Goal: from an installed package to a working scene change, with two scenes `Assets/Scenes/MainMenu.unity` and `Assets/Scenes/Level1.unity`.

1. Run the install check above. Expected: a `My Scene Manager <version> installed…` log and no error.
2. Register both scenes in the Build Settings (`File > Build Profiles`, **Scene List**), or from an Editor script:

   ```csharp
   using System.Collections.Generic;
   using UnityEditor;

   public static class RegisterScenes
   {
       [MenuItem("Tools/My Scene Manager/Register Scenes")]
       public static void Register()
       {
           string[] paths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Level1.unity" };
           var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
           foreach (string path in paths)
               if (!scenes.Exists(s => s.path == path))
                   scenes.Add(new EditorBuildSettingsScene(path, true));
           EditorBuildSettings.scenes = scenes.ToArray();
           AssetDatabase.SaveAssets(); // write ProjectSettings/EditorBuildSettings.asset to disk now
       }
   }
   ```

   A bare string resolves against **enabled** Build Settings entries by scene name (`"Level1"`) or full asset path (`"Assets/Scenes/Level1.unity"`), then against Addressables.
3. Add a component to an object in `MainMenu` that starts the transition from a button or from `Start` — never from `Awake` or `OnEnable`:

   ```csharp
   using MyGameDevTools.SceneLoading;
   using UnityEngine;

   public class StartGameButton : MonoBehaviour
   {
       [SerializeField] string _targetScene = "Level1";

       // Call from Start(), or from the menu's button in the project's UI framework:
       // UI Toolkit: rootVisualElement.Q<UnityEngine.UIElements.Button>("start").clicked += StartGame;
       // uGUI: the Button's On Click () list, or button.onClick.AddListener(StartGame).
       public void StartGame() => MySceneManager.TransitionAsync(_targetScene);
   }
   ```
4. Open `MainMenu`, enter Play Mode and trigger `StartGame()`.

Expected result: the Hierarchy shows `Level1` loaded and active, `MainMenu` is gone, and the Console has no `[MySceneManager]` errors. For loading screens and other shapes of transition, continue with `my-scene-manager-scene-transitions`.

## Workflows

### Workflow: Verify the installation and version

**Goal:** know whether the package is present, which version, how it was installed, and whether the static API is compiled in.

**Steps:**
1. Run the install check from Prerequisites.
2. For a Package Manager install, the version comes from `PackageInfo.version`; `PackageInfo.source` says Registry, Git, LocalTarball or Embedded.
3. For an Asset Store install, the package is imported under `Assets/` and `FindForAssembly` returns `null`. Find its `package.json` with `AssetDatabase.FindAssets("MyGameDevTools.SceneLoading t:AssemblyDefinitionAsset")` — the asmdef sits in the package's `Runtime/` folder, and `package.json` is one level up.
4. If the static API is missing, the project defines `DISABLE_STATIC_SCENE_MANAGER` (Project Settings > Player > Scripting Define Symbols). That is deliberate; work through a `CoreSceneManager` instance the project owns rather than removing the define.

**Expected result:** the agent can state the version (5.x expected), the install source, and whether to call `MySceneManager` or an instance.

### Workflow: Register scenes so strings resolve

**Goal:** every scene the game loads by name is reachable.

**Steps:**
1. Decide per scene: Build Settings (always available, included in every build) or Addressables (see the next workflow). Do not put the same scene in both — the Build Settings win and a warning is logged.
2. Add Build Settings scenes with the Editor script from Quick start, or `File > Build Profiles` > **Scene List** > **Add Open Scenes**. Only **enabled** entries are resolvable.
3. If a scene is added while the Editor is in Play Mode, exit and re-enter Play Mode: the scene list is read when Play Mode starts.

**Expected result:** `EditorBuildSettings.scenes` lists each scene with `enabled == true`, and `MySceneManager.LoadAsync("<name>")` no longer faults with "Could not resolve the scene".

### Workflow: Enable Addressables scenes

**Goal:** load scenes that live in Addressables by string, address or `AssetReference`.

**Steps:**
1. Confirm with the user, then install Addressables: `Window > Package Manager` > Unity Registry > **Addressables** > Install, or `UnityEditor.PackageManager.Client.Add("com.unity.addressables");`. Version 1.19 or newer is required for the package to detect it.
2. Mark the scene addressable (Inspector **Addressable** checkbox on the scene asset), or from an Editor script:

   ```csharp
   using UnityEditor;
   using UnityEditor.AddressableAssets;
   using UnityEditor.AddressableAssets.Settings;

   public static class MakeSceneAddressable
   {
       [MenuItem("Tools/My Scene Manager/Make Level2 Addressable")]
       public static void Run()
       {
           AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.GetSettings(true);
           string guid = AssetDatabase.AssetPathToGUID("Assets/Scenes/Level2.unity");
           AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
           entry.address = "Level2"; // the string game code will pass
           AssetDatabase.SaveAssets();
       }
   }
   ```
3. Remove that scene from the Build Settings, or it keeps resolving to the Build Settings copy.
4. In game code, `MySceneManager.LoadAsync("Level2")` now finds it. `SceneRef.Address("Level2")` skips the Build Settings lookup entirely. An `AssetReference` field converts directly.
5. For player builds, build Addressables content as usual (`Window > Asset Management > Addressables > Groups`, **Build > New Build > Default Build Script**). In the Editor, the default Play Mode Script loads from the AssetDatabase without a content build.

**Expected result:** the Addressables Groups window lists the scene under the default group with the chosen address, and loading it by that string completes in Play Mode.

### Workflow: Enable uGUI and TextMeshPro feedback components

**Goal:** make `LoadingFeedbackSlider`, `LoadingFeedbackText` and `LoadingFeedbackTextMeshPro` available — only for projects whose UI is uGUI. UI Toolkit projects skip this workflow; their loading screens use `LoadingBehavior` plus a small `LoadingScreenComponent` (see `my-scene-manager-loading-screens`).

**Steps:**
1. Check `Packages/manifest.json` for `com.unity.ugui`. Do not add it to a UI Toolkit-only project just to get these components.
2. If missing, confirm with the user and add it: `UnityEditor.PackageManager.Client.Add("com.unity.ugui");`. In Unity 6, `com.unity.ugui` 2.x also provides TextMeshPro, which enables `LoadingFeedbackTextMeshPro`.
3. Wait for the recompile.

**Expected result:** `Add Component > Scene Loading` lists **Loading Slider**, **Loading Text (Legacy)** and **Loading Text**.

### Workflow: Import and run the Loading Scene Examples sample

**Goal:** a runnable reference for every transition shape and loading-screen type.

**Steps:**
1. First check whether the sample is already compiled: if `System.Type.GetType("TransitionExamples, Sample.LoadingScene")` is not null, the sample's files are already in the project (Asset Store installs, or a package that ships a visible `Samples/` folder). Use it where it is and skip to step 3 — importing a second copy duplicates the `Sample.LoadingScene` assembly and breaks compilation.
2. Otherwise, for Package Manager installs: `Window > Package Manager` > **In Project** > **My Scene Manager** > **Samples** tab > **Import** next to *Loading Scene Examples*. From script:

   ```csharp
   var info = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(MyGameDevTools.SceneLoading.CoreSceneManager).Assembly);
   foreach (UnityEditor.PackageManager.UI.Sample sample in UnityEditor.PackageManager.UI.Sample.FindByPackage(info.name, info.version))
       if (sample.displayName == "Loading Scene Examples")
           sample.Import();
   ```

   It is copied to `Assets/Samples/My Scene Manager/<version>/Loading Scene Examples`. In a URP or HDRP project, accept the dialog that offers to upgrade the sample materials.
3. Register its six scenes (`SceneA`, `SceneB`, `SceneListenerHUD`, `Extra`, `Loading_Screen`, `Loading_Animated`). When the user asked for a ready-to-play setup, add them from the Editor before Play Mode, with the Build Settings snippet from Quick start and the paths under the sample's `Scenes/` folder. Open a saved scene (such as `SceneA`) first: Editor scripts run with an Untitled scene open can trigger a modal save prompt that freezes the Editor. Otherwise leave it to the sample: on first Play it shows a prompt, and **Add them, and exit Play Mode** registers them (the sample never edits the Build Settings without that click).
4. Open `Scenes/SceneA.unity`, enter Play Mode and click any of the eight examples. The HUD in the top-right reports the operation's phases.

**Expected result:** the room list shows "8 examples", each one transitions between `SceneA` and `SceneB`, and the HUD reaches `Completed` for each.

## Verification

- The install check logs `My Scene Manager <version>` (5.x) and no error.
- `EditorBuildSettings.scenes` contains every scene the game loads by name, each with `enabled == true`, and none of them is also an Addressables entry.
- In Play Mode, from `Start` or later: `MySceneManager.TryGetDefault(out ISceneManager manager)` returns `true` and `manager.LoadedSceneCount >= 1`.
- After a test transition, `MySceneManager.GetActiveScene().name` equals the target scene's name.
- The Console has no errors prefixed `[MySceneManager]` and no compile errors.
- Testing from an agent: an unfocused Editor stops advancing Play Mode, so for the test session set `Application.runInBackground = true` (in the Editor this also changes `PlayerSettings.runInBackground`; restore it afterwards). Register any logging or sampling hook before the operation starts (for example from a `[RuntimeInitializeOnLoadMethod]` method), because short operations finish before a probe attached afterwards sees them.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `MyGameDevTools.SceneLoading.MySceneManager` | static class | The entry point: `LoadAsync`, `UnloadAsync`, `TransitionAsync`, `ReloadActiveSceneAsync`, scene queries, events |
| `MySceneManager.TryGetDefault(out ISceneManager)` | static method | Returns the manager if it exists yet, without throwing |
| `CoreSceneManager` | class (`ISceneManager`) | The implementation behind `MySceneManager`; construct your own when `DISABLE_STATIC_SCENE_MANAGER` is defined |
| `SceneRef` / `SceneParameters` | structs | What to load; strings, build indices, `Scene`s and `AssetReference`s convert implicitly |
| `SceneOperation` | class | The handle every operation returns |
| `LoadingBehavior`, `LoadingFader`, `MinimumDisplayTime`, `LoadingFeedback*` | components | Loading-screen building blocks, under `Add Component > Scene Loading` |
| `SceneManagerLog` | static class | Log level and routing for every package diagnostic |

Package layout: `Runtime/` (all code; `Loading/` holds the loading-screen components), `Samples/LoadingSceneExamples/` (sample scenes, prefabs, scripts including `PrefabLoadingScreen` and `UIDocumentLoadingScreen`), `Tests/`.

## Common issues

- **`InvalidOperationException: [MySceneManager] The static Scene Manager instance is not available before the first scene is fully loaded.`** → `MySceneManager` was touched from `Awake` or `OnEnable` in the first scene. The manager is created by a `[RuntimeInitializeOnLoadMethod]` that runs after the first scene's `Awake`/`OnEnable`. → Move the call to `Start`, or guard with `MySceneManager.TryGetDefault(out var manager)`.
- **"Could not resolve the scene 'X'. It was not found in the build settings…"** → the scene is not an enabled Build Settings entry, the name is misspelled, or it is a partial path. → Register it, pass the exact scene name or full `Assets/…/X.unity` path, or make it addressable.
- **A scene added during Play Mode still can't be found** → the scene list is read when Play Mode starts. → Exit and re-enter Play Mode.
- **`#if ENABLE_ADDRESSABLES` in project code never compiles in** → the defines are version defines on the package's assembly only. → Add your own version define to your asmdef, or reference Addressables directly.
- **`LoadingFeedbackSlider` or `LoadingFeedbackText` type not found** → `com.unity.ugui` is not installed. → Install it (see the uGUI workflow).
- **Duplicate type errors after an Asset Store update** → the previous version was not removed. → Delete the old imported folder completely, then import the new version.
- **Duplicate `Sample.LoadingScene` assembly after importing the sample** → the sample was already compiled from the package or the Asset Store import. → Delete the extra copy and use the existing one (step 1 of the sample workflow).
- **The Editor stops answering after an Editor script saved or switched scenes** → a modal dialog (usually "save the Untitled scene?") is open and blocks it until a person dismisses it. → Restart the Editor. Never save an Untitled scene or call `SaveCurrentModifiedScenesIfUserWantsTo` from automation; open a saved scene first.
- **Play Mode does not advance while testing from an agent** → an unfocused Editor stops ticking. → For the test session only, set `Application.runInBackground = true`; in the Editor this also changes `PlayerSettings.runInBackground`, so restore its original value afterwards.

## Boundaries

- Scenes are **always loaded additively**. There is no `LoadSceneMode.Single`; a transition is the replacement for single-mode loading.
- The managers work at **runtime and in Play Mode only**. To open or edit scenes in Edit Mode, use `UnityEditor.SceneManagement.EditorSceneManager`.
- The manager tracks the scenes loaded at startup and the scenes it loads itself. Scenes loaded or unloaded directly through `UnityEngine.SceneManagement.SceneManager` are invisible to it, and its state goes stale; use one or the other.
- Advanced extension points — a project-owned `CoreSceneManager` (with `DISABLE_STATIC_SCENE_MANAGER`), or a custom `ISceneBackend` registered through `SceneBackendRegistry.Register` — are documented at https://scene-loader.mygamedevtools.com/docs/advanced-usage/core-scene-manager and are not covered by these skills.
- The package is free and MIT-licensed; there are no editions or gated features.
