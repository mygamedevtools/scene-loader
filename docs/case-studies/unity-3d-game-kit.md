---
sidebar_position: 1
description: A real integration, step by step — replacing the 3D Game Kit's coroutine loader with My Scene Manager.
---

# Unity 3D Game Kit

Unity's [3D Game Kit](https://assetstore.unity.com/packages/templates/tutorials/3d-game-kit-115747) is a complete small game with its own scene loading: a coroutine in `SceneController` that fades to a loading overlay, calls `SceneManager.LoadSceneAsync` in single mode, teleports the player to the right entrance and fades back in. It is the shape most projects arrive at on their own, which makes it a good subject for showing what an integration actually touches — and what it does not.

This page walks through that integration on Unity `6000.5`, built-in render pipeline, uGUI, no Addressables. Every scene is in the Build Settings, so a bare `string` is all a reference needs.

:::tip
Two of the package's 5.0 fixes came out of this exact integration: the fader running on scaled, unclamped time, and `MinimumDisplayTime` living in the sample instead of the package. The page describes the result on 5.0, where neither needs working around.
:::

The result, before the details — `Start → Level1` through the real transition point, with the sample HUD ticking through the phases on top of the loading screen:

<video controls muted loop playsInline width="100%" src="/img/3d-game-kit.mp4" />

## What the Game Kit had

`ScreenFader.prefab` lives in `Resources`, instantiates itself, is marked `DontDestroyOnLoad` and carries three overlay canvases: `BlackFader`, `GameOverCanvas` and `LoadingCanvas`. `SceneController.Transition` drove them from a coroutine:

```
SaveAllData → ReleaseControl → ScreenFader.FadeSceneOut(Loading) → ClearPersisters
→ SceneManager.LoadSceneAsync(name)                       (single mode)
→ LoadAllData → teleport player to the entrance → ScreenFader.FadeSceneIn → GainControl
```

`Scenes/UI/Loading.unity` was in the Build Settings but unused — a camera, an `EventSystem`, post-processing and stale copies of the canvases.

Three things stand out. The loading look lives in an overlay that has to survive scenes, so it is `DontDestroyOnLoad`. Every step after the fade has to be sequenced by hand. And single-mode loading destroys everything, so anything meant to persist has to be `DontDestroyOnLoad` too.

## Installing

`Packages/manifest.json` gets the OpenUPM registry and the dependency:

```json
"dependencies": { "com.mygamedevtools.scene-loader": "5.0.0", … },
"scopedRegistries": [
  { "name": "Open UPM", "url": "https://package.openupm.com", "scopes": [ "com.mygamedevtools" ] }
]
```

The runtime assembly is auto-referenced, so the Game Kit's `Assembly-CSharp` scripts can `using MyGameDevTools.SceneLoading;` with no asmdef work. See [Installation](../getting-started/installation.mdx) for the other ways in.

## The loading screen as a scene

The `LoadingCanvas` subtree was pulled out of `ScreenFader.prefab` into its own prefab, `LoadingScreen.prefab`, and `Loading.unity` was emptied down to a single instance of it. It keeps the Game Kit's visuals — the background, the black bars, the `LoadingText` and the sprite-animated `LoadingChomper` — and gains the package components:

| Component | Role |
|---|---|
| `Canvas` — Screen Space Overlay | Renders without a camera, so the scene has none. It is loaded additively on top of the outgoing scene, exactly like the sample's `Loading_Screen`. |
| `CanvasGroup` | What the fader drives. |
| `LoadingBehavior` | **Required.** Anchors the `LoadingProgress` the transition waits on. Without it a loading scene is shown for exactly as long as the load takes. |
| `LoadingFader` — `fadeInTime` and `fadeOutTime` at `0.5` | Holds both gates for the length of each fade. |
| `MinimumDisplayTime` — `seconds` at `1.5` | Holds completion, so a fast load — `Level2 → Start` — does not flash the screen on and off. |

Nothing is wired between them in the Inspector: every component finds the `LoadingBehavior` on its parents. The `Canvas` sorting order was set to `50`, below the sample HUD's panel at `100`, so the HUD (below) stays on top of the screen while it is showing.

With the loading look owned by the scene, `ScreenFader` loses its `Loading` fade. The `Black` and `GameOver` fades stay: the initial fade-in on the first scene and the death screen are not scene transitions.

```diff title="ScreenFader.cs"
 public enum FadeType
 {
-    Black, Loading, GameOver,
+    Black, GameOver,
 }

 public CanvasGroup faderCanvasGroup;
-public CanvasGroup loadingCanvasGroup;
 public CanvasGroup gameOverCanvasGroup;

 public static IEnumerator FadeSceneOut(FadeType fadeType = FadeType.Black)
 {
     CanvasGroup canvasGroup;
     switch (fadeType)
     {
-        case FadeType.Black:
-            canvasGroup = Instance.faderCanvasGroup;
-            break;
         case FadeType.GameOver:
             canvasGroup = Instance.gameOverCanvasGroup;
             break;
         default:
-            canvasGroup = Instance.loadingCanvasGroup;
+            canvasGroup = Instance.faderCanvasGroup;
             break;
     }
```

:::info
Delete the old path rather than leaving it dormant. One source of truth for the loading look is the point; two paths that each work is how the next person picks the wrong one.
:::

## The transition

`SceneController.Transition` stays a coroutine — the Game Kit's callers are coroutines — but the sequencing moved onto the operation. Every line of the original is still there; what changed is *who* runs it and *when*:

```diff title="SceneController.cs"
+public const string LoadingSceneName = "Loading";
+
+// One place, so zone changes, restarts and the timeline reload all use the same screen.
+public static LoadingScreen CreateLoadingScreen() => new SceneLoadingScreen(LoadingSceneName);
+
 protected IEnumerator Transition(string newSceneName, DestinationTag destinationTag, TransitionType transitionType)
 {
     m_Transitioning = true;
     PersistentDataManager.SaveAllData();

     if (m_PlayerInput == null)
         m_PlayerInput = FindObjectOfType<PlayerInput>();
     if (m_PlayerInput) m_PlayerInput.ReleaseControl();
-    yield return StartCoroutine(ScreenFader.FadeSceneOut(ScreenFader.FadeType.Loading));
-    PersistentDataManager.ClearPersisters();
-    yield return SceneManager.LoadSceneAsync(newSceneName);
-    m_PlayerInput = FindObjectOfType<PlayerInput>();
-    if (m_PlayerInput) m_PlayerInput.ReleaseControl();
-    PersistentDataManager.LoadAllData();
-    SceneTransitionDestination entrance = GetDestination(destinationTag);
-    SetEnteringGameObjectLocation(entrance);
-    SetupNewScene(transitionType, entrance);
-    if (entrance != null)
-        entrance.OnReachDestination.Invoke();
-    yield return StartCoroutine(ScreenFader.FadeSceneIn());
+
+    SceneOperation operation = MySceneManager.TransitionAsync(newSceneName, CreateLoadingScreen());
+
+    // The screen is opaque and the old scene is about to go — the moment the fade-out used to mark.
+    operation.StateChanged += op =>
+    {
+        if (op.State == SceneOperationState.Unloading)
+            PersistentDataManager.ClearPersisters();
+    };
+
+    // Fires while the screen is still opaque, so the player is in place before it fades out.
+    void OnSceneLoaded(Scene scene)
+    {
+        if (scene.name != newSceneName)
+            return;
+
+        m_PlayerInput = FindObjectOfType<PlayerInput>();
+        if (m_PlayerInput) m_PlayerInput.ReleaseControl();
+        PersistentDataManager.LoadAllData();
+        SceneTransitionDestination entrance = GetDestination(destinationTag);
+        SetEnteringGameObjectLocation(entrance);
+        SetupNewScene(transitionType, entrance);
+        if (entrance != null)
+            entrance.OnReachDestination.Invoke();
+    }
+    operation.SceneLoaded += OnSceneLoaded;
+
+    try
+    {
+        // Rethrows if the operation faults — a scene missing from the Build Settings, say.
+        yield return operation.ToCoroutine();
+    }
+    finally
+    {
+        operation.SceneLoaded -= OnSceneLoaded;
+        m_Transitioning = false;
+    }
+
     if (m_PlayerInput)
         m_PlayerInput.GainControl();
-
-    m_Transitioning = false;
 }
```

Each of the old steps has a phase it belongs to:

| Old step | Where it went | Why there |
|---|---|---|
| `FadeSceneOut(Loading)` | `ScreenIn` — the package | The fader's fade-in. Its show hold keeps the old scene alive until the screen is opaque. |
| `ClearPersisters()` after the fade | `StateChanged` → `Unloading` | Same moment: opaque screen, old scene about to go. |
| `LoadAllData`, teleport, `OnReachDestination` | `SceneLoaded`, filtered to the target | Runs during `Loading`, before `ScreenOut`, so there is no pop when the screen lifts. |
| `FadeSceneIn()` | `ScreenOut` — the package | The fader's fade-out. Its hide hold keeps the screen until done. |
| `m_Transitioning = false` | `finally` | Also resets when the operation faults. |

`SceneLoaded` is filtered by name because a transition loads the loading scene too — it fires for both.

### Restarts and reloads

`RestartZone` already went through `Transition` with the current zone as the target, so it needed nothing. The timeline's `SceneReloaderBehaviour` loaded by build index in single mode, which would have destroyed every additively loaded scene:

```diff title="SceneReloaderBehaviour.cs"
 public void ReloadScene(GameObject sceneGameObject)
 {
-    SceneManager.LoadSceneAsync(sceneGameObject.scene.buildIndex);
+    MySceneManager.ReloadActiveSceneAsync(SceneController.CreateLoadingScreen());
 }
```

The reload now shows the same loading screen as every other transition, and keeps the HUD (below) alive through it.

## A persistent HUD without `DontDestroyOnLoad`

The sample's `SceneListenerHUD` scene is a UI Toolkit document that subscribes to `OperationStarted` and renders every operation's phase, progress and a cancel button. It has no camera, no `EventSystem` and no `DontDestroyOnLoad` — it survives `Start → Level1 → Level2 → Start` because a transition unloads **only the active scene**.

A small component on the menu scene guarantees it is there:

```cs title="MenuSceneHud.cs"
public class MenuSceneHud : MonoBehaviour
{
    [SceneName] public string hudSceneName = "SceneListenerHUD";

    // Start, not Awake: MySceneManager.Default is created after the first scene has loaded.
    void Start()
    {
        if (IsSceneLoadedOrLoading(hudSceneName))
            return;

        MySceneManager.LoadAsync(hudSceneName);
    }

    static bool IsSceneLoadedOrLoading(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
            if (SceneManager.GetSceneAt(i).name == sceneName)
                return true;
        return false;
    }
}
```

Two choices worth explaining. `Start` rather than `Awake`, because `MySceneManager.Default` is created by a `RuntimeInitializeOnLoadMethod` that runs after the first scene has loaded, so it does not exist yet in that scene's `Awake`. And the loop over `SceneManager.sceneCount` rather than `TryGetLoadedSceneByName`: the guard has to see a scene that is still **loading** — the menu can be re-entered while the HUD is on its way — and the manager's lookup only knows scenes that have finished. `SceneManager.GetSceneByName` is no good either: it returns a valid handle for any scene the Build Settings know about, loaded or not.

This is the general pattern for anything that must outlive transitions: load it additively once, never make it active, and let the manager leave it alone.

## Checklist for your project

1. Add the OpenUPM registry and the dependency; confirm the package resolved in `Library/PackageCache`.
2. Build the loading screen as one prefab: `Canvas` + `CanvasGroup` + **`LoadingBehavior`** + `LoadingFader`, plus `MinimumDisplayTime` and the feedback components you want. No camera, no `EventSystem`. Put its scene in the Build Settings.
3. Replace the load coroutine with one `MySceneManager.TransitionAsync(target, new SceneLoadingScreen("Loading"))`. Move "after the screen is up" work to `StateChanged == Unloading` and "before the screen lifts" work to `SceneLoaded`. `yield return op.ToCoroutine()` or `await op`, and reset the busy flag in `finally`.
4. Delete the old overlay path.
5. Anything that must outlive transitions: load it additively once and never make it active — no `DontDestroyOnLoad`.
6. Anything that reaches `MySceneManager.Default` from the first scene does so in `Start`, not `Awake`.
7. Watch the fades in a windowed editor, not a batchmode one — there is no backbuffer to see them in.

The sample page, [Loading Scene Examples](../samples/loading-scene-examples.md), has the HUD and every loading screen shape as runnable references, and [Creating Loading Screens](../getting-started/loading-screens.md) covers the gates and holds the components above are built on.
