---
name: my-scene-manager-loading-screens
description: "Use this skill whenever the user wants to create, change or fix a loading screen for My Scene Manager transitions — e.g. 'make a loading screen', 'add a progress bar while the level loads', 'fade to black between scenes', 'keep the loading screen up for at least 2 seconds', 'my loading screen flashes too fast', 'wait for my animation before the scene switches'. Covers loading screens in UI Toolkit or uGUI (following the project's UI framework, or asking when it is unclear), LoadingBehavior, MinimumDisplayTime, the uGUI LoadingFader and LoadingFeedback components, prefab and UI Toolkit document screens, custom LoadingScreenComponent scripts, and the holds that gate a transition. Do NOT use for starting the transition itself (see my-scene-manager-scene-transitions) or a progress bar outside a loading screen (see my-scene-manager-operation-handles). When in doubt whether a loading-screen request could involve My Scene Manager, use this skill — Prerequisites shows how to confirm the asset is installed."
metadata:
  asset: "My Scene Manager"
  publisher: "My GameDev Tools"
  asset-version: "5.1.1"
  skill-version: "1.1.5"
  unity: "6000.0+"
  render-pipelines: "Built-in, URP, HDRP"
  category: "tools/utilities"
  asset-store-url: "https://assetstore.unity.com/packages/slug/313159"
  documentation-url: "https://scene-loader.mygamedevtools.com/"
  support-url: "https://github.com/mygamedevtools/scene-loader/issues"
  last-verified: "2026-11-20"
---

# Build Loading Screens for My Scene Manager

A loading screen is whatever a transition shows between leaving one scene and arriving in the next. With My Scene Manager it can be a **scene** (named as the second argument of `TransitionAsync`), a **prefab** or a **UI Toolkit document** (both as `LoadingScreen` subclasses), built with **UI Toolkit or uGUI** — the package's gating works the same for both. Every screen gates the transition through a `LoadingProgress`: anything that needs the transition to wait — a fade, an animation, a minimum display time — takes a *hold* and releases it when done. This skill lets an agent build a loading screen in the project's own UI framework, write custom components the transition waits for, and use non-scene screens.

## When to use this skill

- "make a loading screen with a progress bar"
- "fade in and out between scenes"
- "show 0–100% while the level loads"
- "the loading screen flashes for one frame — keep it up for 2 seconds"
- "use this prefab as the loading screen instead of a scene"
- "build the loading screen with UI Toolkit" / "…with a Canvas"
- "don't switch scenes until my curtain animation finishes"

Not for:

- Calling `TransitionAsync` / `ReloadActiveSceneAsync` → `my-scene-manager-scene-transitions`.
- A progress bar in a HUD that is not a loading screen → `my-scene-manager-operation-handles`.

## Prerequisites

- My Scene Manager 5.x on Unity 6000.0+, any render pipeline.
- **Install check:** `System.Type.GetType("MyGameDevTools.SceneLoading.CoreSceneManager, MyGameDevTools.SceneLoading") != null` in the Editor. If `null`, tell the user the package is missing: https://assetstore.unity.com/packages/slug/313159. If `MySceneManager` does not exist (error CS0103), the project defines `DISABLE_STATIC_SCENE_MANAGER` on purpose: keep the define and call the same methods on the project's own `ISceneManager` instance (search its code for `new CoreSceneManager`).
- **Pick the UI framework before writing any UI.** Follow the project, not a default:
  1. If the user named one ("UI Toolkit", "UXML", "Canvas", "uGUI"), use it.
  2. Otherwise inspect the project, ignoring `Assets/Samples/` and `Assets/TextMesh Pro/`: UI Toolkit is in use if `Assets/` has `.uxml` files or `PanelSettings` assets; uGUI is in use if scenes or prefabs contain a `Canvas` (YAML class `!u!223`). The snippet below reports both.
  3. Exactly one in use → use it without asking. Both or neither → **ask the user** which to use before creating UI, and say that the package's ready-made fader and feedback components are uGUI-only while UI Toolkit uses a small script from this skill.

  ```csharp
  // Editor: which UI framework does the project use?
  static bool Ignored(string path) => path.StartsWith("Assets/Samples/") || path.StartsWith("Assets/TextMesh Pro/");
  int uitk = 0, ugui = 0;
  foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:VisualTreeAsset t:PanelSettings", new[] { "Assets" }))
      if (!Ignored(UnityEditor.AssetDatabase.GUIDToAssetPath(guid))) uitk++;
  foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:Scene t:Prefab", new[] { "Assets" }))
  {
      string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
      if (!Ignored(path) && System.IO.File.ReadAllText(path).Contains("--- !u!223 ")) ugui++;
  }
  UnityEngine.Debug.Log($"UI Toolkit assets: {uitk}, files with a Canvas: {ugui}");
  ```
- uGUI path only: `LoadingFader`, `LoadingFeedbackSlider` and `LoadingFeedbackText` need `com.unity.ugui`; `LoadingFeedbackTextMeshPro` needs `com.unity.ugui` 2.x (or `com.unity.textmeshpro` 2.2+).
- UI Toolkit path only: a `PanelSettings` asset. Reuse the project's; if there is none, create one with `Assets > Create > UI Toolkit > Panel Settings Asset` and check that its **Theme Style Sheet** is assigned.
- A loading **scene** must be resolvable like any scene: an enabled Build Settings entry or an Addressables entry.
- Testing from an agent: an unfocused Editor stops advancing Play Mode, so for the test session set `Application.runInBackground = true` (in the Editor this also changes `PlayerSettings.runInBackground`; restore it afterwards). Register any logging or sampling hook before the operation starts (for example from a `[RuntimeInitializeOnLoadMethod]` method), because short operations finish before a probe attached afterwards sees them.

## Quick start

Goal: a loading scene `Assets/Scenes/Loading.unity` that fades in, shows progress, stays up at least one second and fades out, in the project's UI framework.

1. Pick the framework with the rule in Prerequisites (ask the user if it is ambiguous).
2. Build the scene with the matching workflow below:
   - UI Toolkit → **Workflow: Create a UI Toolkit loading scene**.
   - uGUI → **Workflow: Create a uGUI loading scene**.
3. Use it: `MySceneManager.TransitionAsync("Level1", "Loading");`.
4. Enter Play Mode and trigger the transition.

Expected result: `Assets/Scenes/Loading.unity` is in the Build Settings and has exactly one `LoadingBehavior` plus a `MinimumDisplayTime`. In Play Mode the screen fades in over the old scene, shows progress, holds for at least a second, and fades out before the operation reaches `Completed`; afterwards only `Level1` (plus any persistent scenes) is loaded.

## Workflows

### Workflow: Create a UI Toolkit loading scene

**Goal:** a loading scene built from a UXML document, gated like any other screen.

The package's `LoadingBehavior` and `MinimumDisplayTime` work with UI Toolkit as-is; fading and progress come from one small `LoadingScreenComponent` (the same pattern as the sample's `Loading_Animated` scene).

**Steps:**
1. Create `Assets/UI/Loading/LoadingScreen.uxml`:

   ```xml
   <ui:UXML xmlns:ui="UnityEngine.UIElements">
       <ui:VisualElement name="root" style="position: absolute; left: 0; top: 0; right: 0; bottom: 0; background-color: rgb(0, 0, 0); justify-content: center; align-items: center;">
           <ui:ProgressBar name="progress" low-value="0" high-value="1" style="width: 50%;" />
       </ui:VisualElement>
   </ui:UXML>
   ```
2. Create `Assets/Scripts/UIToolkitLoadingView.cs`:

   ```csharp
   using MyGameDevTools.SceneLoading;
   using UnityEngine;
   using UnityEngine.UIElements;

   [RequireComponent(typeof(UIDocument))]
   public class UIToolkitLoadingView : LoadingScreenComponent
   {
       [SerializeField] float _fadeSeconds = 0.3f;

       VisualElement _root;
       ProgressBar _bar;

       protected override void Awake()
       {
           _root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("root");
           _bar = _root?.Q<ProgressBar>("progress");
           if (_root != null)
               _root.style.opacity = 0;

           base.Awake(); // binds, and calls OnBound when a LoadingBehavior is found
       }

       protected override void OnBound()
       {
           // Hold both gates: no unload before the fade-in ends, no "gone" before the fade-out ends.
           Progress.HoldShow(this);
           Progress.HoldHide(this);
           Progress.Progressed += OnProgressed;
           Progress.LoadingCompleted += FadeOut;
           Fade(0, 1, () => Progress.ReleaseShow(this));
       }

       protected override void OnDestroy()
       {
           if (Progress != null)
           {
               Progress.Progressed -= OnProgressed;
               Progress.LoadingCompleted -= FadeOut;
           }
           base.OnDestroy();
       }

       void OnProgressed(float value)
       {
           if (_bar != null)
               _bar.value = value; // 0..1, matching the UXML low/high values
       }

       void FadeOut() => Fade(1, 0, () => Progress.ReleaseHide(this));

       // UI Toolkit scheduler on wall-clock unscaled time: works at timeScale 0 and needs no coroutine.
       void Fade(float from, float to, System.Action onDone)
       {
           if (_root == null || _fadeSeconds <= 0)
           {
               onDone();
               return;
           }

           float startedAt = Time.unscaledTime;
           IVisualElementScheduledItem item = null;
           item = _root.schedule.Execute(() =>
           {
               float t = Mathf.Clamp01((Time.unscaledTime - startedAt) / _fadeSeconds);
               _root.style.opacity = Mathf.Lerp(from, to, t);
               if (t < 1)
                   return;
               item.Pause();
               onDone();
           }).Every(16);
       }
   }
   ```
3. Create `Assets/Editor/CreateLoadingScene.cs` and run `Tools > My Scene Manager > Create UI Toolkit Loading Scene` (or `EditorApplication.ExecuteMenuItem(...)`). It replaces the open scenes without asking. If an open scene with a path has changes to keep, save it first with `EditorSceneManager.SaveScene(scene)`. Never call `SaveOpenScenes`, `SaveCurrentModifiedScenesIfUserWantsTo` or save an Untitled scene: those open a modal Save dialog that freezes an Editor driven by an agent.

   ```csharp
   using System.Collections.Generic;
   using MyGameDevTools.SceneLoading;
   using UnityEditor;
   using UnityEditor.SceneManagement;
   using UnityEngine;
   using UnityEngine.SceneManagement;
   using UnityEngine.UIElements;

   public static class CreateUIToolkitLoadingScene
   {
       const string ScenePath = "Assets/Scenes/Loading.unity";
       const string UxmlPath = "Assets/UI/Loading/LoadingScreen.uxml";

       [MenuItem("Tools/My Scene Manager/Create UI Toolkit Loading Scene")]
       public static void Create()
       {
           string[] panels = AssetDatabase.FindAssets("t:PanelSettings", new[] { "Assets" });
           if (panels.Length == 0)
           {
               Debug.LogError("No PanelSettings asset found. Create one (Assets > Create > UI Toolkit > Panel Settings Asset) and run again.");
               return;
           }

           Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

           // Load assets only AFTER NewScene: a Single-mode NewScene unloads unused assets, so an
           // asset loaded before it becomes a destroyed reference and saves as None.
           var panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>(AssetDatabase.GUIDToAssetPath(panels[0]));
           var go = new GameObject("Loading Screen", typeof(UIDocument));
           var document = go.GetComponent<UIDocument>();
           document.panelSettings = panelSettings;
           document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
           document.sortingOrder = 100; // above other documents on the same panel

           go.AddComponent<LoadingBehavior>();
           go.AddComponent<UIToolkitLoadingView>();
           // MinimumDisplayTime counts from when the screen appears, so the fade-in (0.3 s) counts toward it:
           // 1 s fully visible = 1 s + fade-in.
           go.AddComponent<MinimumDisplayTime>().seconds = 1f + 0.3f;

           System.IO.Directory.CreateDirectory("Assets/Scenes");
           EditorSceneManager.SaveScene(scene, ScenePath);

           var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
           if (!buildScenes.Exists(s => s.path == ScenePath))
               buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
           EditorBuildSettings.scenes = buildScenes.ToArray();
           AssetDatabase.SaveAssets(); // write ProjectSettings/EditorBuildSettings.asset to disk now
       }
   }
   ```

   If the project has several `PanelSettings`, pick the one its game UI uses rather than the first found. Whichever you pick, load it after `NewScene`.

**Expected result:** `Loading.unity` has one GameObject `Loading Screen` with `UIDocument` (panel settings and the UXML assigned), `LoadingBehavior`, `UIToolkitLoadingView` and `MinimumDisplayTime`, and is in the Build Settings.

### Workflow: Create a uGUI loading scene

**Goal:** the same screen with a Canvas and the package's ready-made uGUI components.

**Steps:**
1. Create `Assets/Editor/CreateLoadingScene.cs` and run `Tools > My Scene Manager > Create uGUI Loading Scene`. It replaces the open scenes without asking. If an open scene with a path has changes to keep, save it first with `EditorSceneManager.SaveScene(scene)`. Never call `SaveOpenScenes`, `SaveCurrentModifiedScenesIfUserWantsTo` or save an Untitled scene: those open a modal Save dialog that freezes an Editor driven by an agent.

   ```csharp
   using System.Collections.Generic;
   using MyGameDevTools.SceneLoading;
   using UnityEditor;
   using UnityEditor.SceneManagement;
   using UnityEngine;
   using UnityEngine.SceneManagement;
   using UnityEngine.UI;

   public static class CreateUguiLoadingScene
   {
       const string ScenePath = "Assets/Scenes/Loading.unity";

       [MenuItem("Tools/My Scene Manager/Create uGUI Loading Scene")]
       public static void Create()
       {
           Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

           // Root: Canvas + CanvasGroup + the loading components. Feedback below finds the LoadingBehavior on its parents.
           var root = new GameObject("Loading Screen", typeof(Canvas), typeof(CanvasScaler), typeof(CanvasGroup));
           var canvas = root.GetComponent<Canvas>();
           canvas.renderMode = RenderMode.ScreenSpaceOverlay;
           canvas.sortingOrder = 100; // above the scenes being swapped

           root.AddComponent<LoadingBehavior>();
           LoadingFader fader = root.AddComponent<LoadingFader>();
           fader.fadeInTime = 0.5f;
           fader.fadeOutTime = 0.5f;
           // Counts from when the screen appears, fade-in included: 1 s fully visible = 1 s + fadeInTime.
           root.AddComponent<MinimumDisplayTime>().seconds = 1f + fader.fadeInTime;

           var backdrop = new GameObject("Backdrop", typeof(RectTransform), typeof(Image));
           backdrop.transform.SetParent(root.transform, false);
           var backdropRect = (RectTransform)backdrop.transform;
           backdropRect.anchorMin = Vector2.zero;
           backdropRect.anchorMax = Vector2.one;
           backdropRect.offsetMin = backdropRect.offsetMax = Vector2.zero;
           backdrop.GetComponent<Image>().color = Color.black;

           GameObject bar = DefaultControls.CreateSlider(new DefaultControls.Resources());
           bar.name = "Progress";
           bar.transform.SetParent(root.transform, false);
           var slider = bar.GetComponent<Slider>();
           slider.interactable = false;
           // Without sprites, DefaultControls draws the track and the fill both white: tint the track so progress shows.
           bar.transform.Find("Background").GetComponent<Image>().color = new Color(0.25f, 0.25f, 0.25f, 1f);
           // A progress bar needs no drag handle: remove it, and the inset kept for it, so the fill reaches the right edge.
           slider.handleRect = null;
           Object.DestroyImmediate(bar.transform.Find("Handle Slide Area").gameObject);
           var fillArea = (RectTransform)bar.transform.Find("Fill Area");
           fillArea.offsetMin = new Vector2(0, fillArea.offsetMin.y);
           fillArea.offsetMax = new Vector2(0, fillArea.offsetMax.y);
           var fill = (RectTransform)fillArea.Find("Fill");
           fill.sizeDelta = new Vector2(0, fill.sizeDelta.y);
           bar.AddComponent<LoadingFeedbackSlider>();

           System.IO.Directory.CreateDirectory("Assets/Scenes");
           EditorSceneManager.SaveScene(scene, ScenePath);

           var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
           if (!buildScenes.Exists(s => s.path == ScenePath))
               buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
           EditorBuildSettings.scenes = buildScenes.ToArray();
           AssetDatabase.SaveAssets(); // write ProjectSettings/EditorBuildSettings.asset to disk now
       }
   }
   ```
2. To build or adapt it by hand instead, on the Canvas root (with a `CanvasGroup`): `Add Component > Scene Loading > Loading Behavior`, then **Loading Fader** (fields `fadeInTime`/`fadeOutTime`, default 1 s, range 0.05–5, unscaled; `maxFrameStep`, default 1/30 s; fade curves). On a `Slider`: **Loading Slider**; on a legacy `Text`: **Loading Text (Legacy)**; on a `TextMeshProUGUI`: **Loading Text** (texts show `0`–`100`, no `%`). Optionally **Minimum Display Time**.
3. Leave each component's **Loading Behavior** field empty — it is filled from the object or its closest parent.

**Expected result:** root `Loading Screen` has `Canvas`, `CanvasGroup`, `LoadingBehavior`, `LoadingFader`, `MinimumDisplayTime`, and a child `Progress` slider with `LoadingFeedbackSlider`; the scene is in the Build Settings.

### Workflow: Start from the sample's loading screens

**Goal:** reuse a finished screen instead of building one.

**Steps:**
1. Import *Loading Scene Examples* (Package Manager > My Scene Manager > Samples).
2. Copy the one matching the project's framework out of the sample folder, so re-importing the sample does not overwrite edits:
   - UI Toolkit: `Scenes/Loading_Animated.unity` with `Scripts/Runtime/AnimatedLoadingScreen.cs`, `UI/LoadingScreenAnimated.uxml`/`.uss` (a sliding-panels screen), or `Scripts/Runtime/UIDocumentLoadingScreen.cs` for a document screen with no scene.
   - uGUI: `Scenes/Loading_Screen.unity` or `Prefabs/LoadingScreen.prefab` (`LoadingBehavior`, `LoadingFader`, `MinimumDisplayTime`, `LoadingFeedbackSlider`, `LoadingFeedbackText`).
3. Register the copied scene in the Build Settings.

**Expected result:** `MySceneManager.TransitionAsync("Level1", "<copied scene name>")` shows the sample's screen.

### Workflow: Keep the screen up for a minimum time

**Goal:** stop fast loads from flashing the screen on and off.

**Steps:**
1. Add `MinimumDisplayTime` next to the `LoadingBehavior` and set `seconds`. It has no UI dependency and works with either framework.
2. Do **not** implement a minimum time with `HoldHide`. Holding the hide gate delays the transition after the screen has already been told to go, so a fade-out finishes and the rest of the wait plays on an empty screen. `MinimumDisplayTime` holds `HoldCompletion` instead, which delays the `LoadingCompleted` cue itself.

3. `seconds` counts from when the screen **appears**, so a fade-in counts toward it. When the user asks for the screen to "stay up N seconds", they usually mean fully visible: set `seconds` to N plus the fade-in duration, and state that choice.

**Expected result:** the screen stays up for at least `seconds` (unscaled) from when it appeared; loads longer than that are not delayed.

### Workflow: Write a component the transition waits for

**Goal:** a custom animation, effect or feedback that gates the transition.

How the gates work: a transition waits at the **show** gate before it unloads the source scene, and at the **hide** gate before it considers the screen gone. Both are open unless something holds them. `LoadingBehavior` holds both from `Awake`, releases show in `Start` and hide when loading completes. Any other participant takes its own hold with itself as owner; the gate opens when the last holder releases. Holds are per owner, so double holds or releases are harmless, and a destroyed holder is dropped.

**Steps:**
1. Derive from `LoadingScreenComponent`. It resolves the `LoadingBehavior` and calls `OnBound()` once `Progress` exists. If you override `Awake`, `Start` or `OnDestroy`, call the base method. `UIToolkitLoadingView` above is a complete example.
2. Take holds in `OnBound()` — that runs during `Awake` when the behaviour is found — and release them when the animation **finishes**. For an `Animator`-driven animation (any UI framework):

   ```csharp
   using System.Collections;
   using MyGameDevTools.SceneLoading;
   using UnityEngine;

   public class CurtainLoadingAnimation : LoadingScreenComponent
   {
       [SerializeField] Animator _animator;
       [SerializeField] float _closeSeconds = 0.6f;
       [SerializeField] float _openSeconds = 0.6f;

       protected override void OnBound()
       {
           Progress.HoldShow(this);
           Progress.HoldHide(this);
           Progress.Progressed += OnProgressed;      // 0..1
           Progress.LoadingCompleted += OnLoaded;    // cue to start hiding
           StartCoroutine(Play("Close", _closeSeconds, () => Progress.ReleaseShow(this)));
       }

       protected override void OnDestroy()
       {
           if (Progress != null)
           {
               Progress.Progressed -= OnProgressed;
               Progress.LoadingCompleted -= OnLoaded;
           }
           base.OnDestroy();
       }

       void OnProgressed(float value) { /* update a bar or label */ }

       void OnLoaded() => StartCoroutine(Play("Open", _openSeconds, () => Progress.ReleaseHide(this)));

       IEnumerator Play(string trigger, float seconds, System.Action onDone)
       {
           _animator.SetTrigger(trigger);
           yield return new WaitForSecondsRealtime(seconds); // unscaled: works from a paused game
           onDone();
       }
   }
   ```
3. Use unscaled time. A transition started at `Time.timeScale = 0` would otherwise never finish the animation, and never release the gate.
4. A pure feedback (no waiting) only subscribes in `OnBound()`. For uGUI, deriving from `LoadingFeedback` and overriding `OnProgressed(float)` is the shortest form.

**Expected result:** the transition's `ScreenIn` phase lasts until the close animation ends and `ScreenOut` until the open animation ends; no 10-second gate warning appears.

### Workflow: Use a prefab as the loading screen

**Goal:** no extra scene and no Build Settings entry for the screen.

**Steps:**
1. Copy this class into the project (it is the sample's `PrefabLoadingScreen`, which the sample marks as meant for copying):

   ```csharp
   using MyGameDevTools.SceneLoading;
   using UnityEngine;

   public class PrefabLoadingScreen : LoadingScreen
   {
       readonly GameObject _prefab;
       GameObject _instance;

       public PrefabLoadingScreen(GameObject prefab)
       {
           _prefab = prefab != null ? prefab : throw new System.ArgumentNullException(nameof(prefab));
       }

       public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
       {
           _instance = Object.Instantiate(_prefab);
           host.Adopt(_instance); // moves it into the package's holder scene, so it survives the source scene's unload
           BindProgress(LoadingBehaviorRegistry.TryGet(_instance, out LoadingBehavior behavior) ? behavior.Progress : null);
           return SceneOperationPump.Completed(operation);
       }

       public override void Dispose()
       {
           if (_instance != null)
               Object.Destroy(_instance);
           _instance = null;
           base.Dispose();
       }
   }
   ```
2. Build the prefab the same way as a loading scene in the project's framework: a `LoadingBehavior` on its root, plus either `UIDocument` + `UIToolkitLoadingView` (UI Toolkit) or Canvas + fader + feedback (uGUI), and optionally `MinimumDisplayTime`.
3. Start the transition with a new instance each time: `MySceneManager.TransitionAsync("Level1", new PrefabLoadingScreen(_loadingPrefab));`.

**Expected result:** during the transition a `loading-screen-host` scene holds the instantiated prefab; both are gone after `Completed`.

### Workflow: Use a UI Toolkit document as the loading screen

**Goal:** a UXML loading screen with no scene, prefab or `LoadingBehavior`.

**Steps:**
1. Subclass `LoadingScreen`. In `PrepareAsync`, create a `GameObject`, `host.Adopt` it, add a `UIDocument`, create your own `new LoadingProgress()`, subscribe to `Progressed` and `LoadingCompleted`, call `BindProgress(progress)`, and take `HoldShow` / `HoldHide` (and `HoldCompletion` for a minimum time) **before returning**. Release them as fades finish. Destroy the object in `Dispose` and call `base.Dispose()`.
2. A complete, working implementation is the sample's `UIDocumentLoadingScreen` — read `references/custom-loading-screens.md` for its structure and copy it from the sample's `Scripts/Runtime` folder.
3. Start it with `MySceneManager.TransitionAsync("Level1", new UIDocumentLoadingScreen(uxml, panelSettings));`.

**Expected result:** the document fades in over the old scene, shows progress, and fades out after the target is active.

## Verification

- The screen uses the framework chosen in Prerequisites (or the one the user picked when asked); no Canvas was added to a UI Toolkit-only project, and no UXML/`PanelSettings` to a uGUI-only one.
- Edit-time: the loading scene or prefab has exactly one `LoadingBehavior`, on an active GameObject. With the scene open as the active scene:

  ```csharp
  int count = 0;
  foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
      count += root.GetComponentsInChildren<MyGameDevTools.SceneLoading.LoadingBehavior>(true).Length;
  Debug.Log(count == 1 ? "PASS: one LoadingBehavior" : $"FAIL: {count} LoadingBehaviors");
  ```
- Every `LoadingScreenComponent` has a `LoadingBehavior` on itself or a parent, or its **Loading Behavior** field assigned. UI Toolkit: the `UIDocument` has `panelSettings` and `visualTreeAsset` assigned.
- Play Mode (a loading scene must be an enabled Build Settings entry): the transition's `StateChanged` sequence contains `ScreenIn` and `ScreenOut`, then `Completed`; afterwards the loading scene is not loaded and no `loading-screen-host` scene remains.
- Console: no "found no LoadingBehavior", "Found N LoadingBehaviors" or "has been waiting 10 seconds" messages.
- The progress element is visible: its fill and track colors differ, and its value reaches 1 before `ScreenOut`. Judge fades and minimum time from per-frame samples of the screen's opacity, not from the scene list alone.

## API quick reference

| Entry point | Type | What it does |
|---|---|---|
| `LoadingBehavior` (`Scene Loading/Loading Behavior`) | component | Owns the screen's `LoadingProgress`; one per screen; any UI framework |
| `MinimumDisplayTime` (`Scene Loading/Minimum Display Time`) | component | Holds the completion cue for `seconds`; any UI framework |
| `LoadingScreenComponent` | abstract component | Base for custom screen components (UI Toolkit views, animations); override `OnBound()` |
| `LoadingFader` (`Scene Loading/Loading Fader`) | component (uGUI) | Fades a `CanvasGroup` in and out, holding both gates during the fades |
| `LoadingFeedbackSlider` / `LoadingFeedbackText` / `LoadingFeedbackTextMeshPro` | components (uGUI) | Show progress on a `Slider`, legacy `Text`, or `TextMeshProUGUI` |
| `LoadingProgress` | class | `HoldShow`/`ReleaseShow`, `HoldHide`/`ReleaseHide`, `HoldCompletion`/`ReleaseCompletion`, `Progressed`, `LoadingCompleted`, `IsShown`, `IsHidden` |
| `LoadingScreen` | abstract class | Base for prefab/UI Toolkit document screens; implement `PrepareAsync`, override `Dispose` |
| `LoadingScreenHost.Adopt(GameObject)` | method | Moves screen objects into the per-transition holder scene |
| `LoadingBehaviorRegistry.TryGet(GameObject, out LoadingBehavior)` | static method | Finds the behaviour under an instantiated hierarchy |

## Common issues

- **A Canvas-based screen was added to a UI Toolkit project (or the reverse)** → the framework was not checked. → Rebuild it with the project's framework; ask the user when the project uses both or neither.
- **The screen shows only while loading, with no progress or fades** → the loading scene has no `LoadingBehavior`, or it is on a disabled GameObject (never registered). → Add one to an active root.
- **Error "… found no LoadingBehavior on itself or its parents, and none was assigned. Disabling it."** → a view, fader or feedback is outside the behaviour's hierarchy. → Move it under the root or assign the field.
- **Warning "Found 2 LoadingBehaviors in scene …"** → only one drives the screen. → Remove the extra.
- **Transition stuck in `ScreenIn` or `ScreenOut`; after 10 s "A transition has been waiting 10 seconds for … to call ReleaseShow()"** → a hold is never released (animation never ends, release in an unreachable path, scaled time at `timeScale` 0). → Release when the animation ends, on unscaled time.
- **UI Toolkit screen renders behind the game's UI** → same panel, lower order. → Raise `UIDocument.sortingOrder` on the loading screen, or give it a `PanelSettings` with a higher sort order.
- **The loading scene's UIDocument has no Panel Settings after the builder ran** → the `PanelSettings` asset was loaded before `EditorSceneManager.NewScene`, which unloaded it. → Load assets after `NewScene`, as the builder above does.
- **The Editor stops answering right after a scene save** → a modal Save dialog opened (saving an Untitled scene, or `SaveCurrentModifiedScenesIfUserWantsTo`), and it blocks the Editor until a person dismisses it. → Restart the Editor; never trigger save prompts from automation.
- **UI Toolkit screen never fades / stays at opacity 0** → the `root` element name in the UXML does not match the `Q<VisualElement>("root")` query, so `_root` is null and the view skips the fade. → Match the names.
- **The screen flashes for a frame** → fast load, no minimum time. → Add `MinimumDisplayTime`.
- **The fade-out finishes and the screen is empty for a while** → a minimum time implemented with `HoldHide`. → Use `HoldCompletion` / `MinimumDisplayTime`.
- **A custom hold seems ignored** → it was taken late (after the transition read the gate, e.g. in `Start` or later). → Take holds in `OnBound()`, `Awake` or `OnEnable`.
- **A prefab or UI Toolkit document screen disappears when the old scene unloads** → its objects were not adopted. → Call `host.Adopt(gameObject)` in `PrepareAsync`.
- **uGUI feedback component types are missing** → uGUI/TextMeshPro not installed. → Install `com.unity.ugui` (2.x also covers TextMeshPro).

## Boundaries

- The package's gating (`LoadingBehavior`, `LoadingProgress`, `MinimumDisplayTime`, `LoadingScreenComponent`) is UI-framework-agnostic. Its ready-made fader and progress components are uGUI-only; UI Toolkit screens use a small `LoadingScreenComponent` such as `UIToolkitLoadingView` above, or the sample's UI Toolkit scripts.
- The prefab and UI Toolkit document screen classes are sample code to copy, not runtime types.
- One `LoadingBehavior` drives one screen; additional ones are ignored with a warning.
- Progress is the average of the target scenes' engine progress; Addressables progress includes download time, so mixed groups advance unevenly. It is not a time estimate.
- Loading screens apply to transitions and reloads only; `LoadAsync`/`UnloadAsync` take no loading screen.
- Visual design, panel/canvas sorting against the rest of the game's UI, and camera setup are ordinary Unity UI work outside this package.
