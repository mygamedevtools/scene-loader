---
id: my-scene-manager-loading-screens-S003
skill: my-scene-manager-loading-screens
title: "Agent follows the project's UI Toolkit standard when building a loading screen, without adding uGUI"
difficulty: intermediate
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x installed. `com.unity.ugui` may be installed (Unity templates often include it) but is unused.
- The project's UI is UI Toolkit only: `Assets/UI/Menu/MainMenu.uxml` (a `Label` titled "My Game" and a `Button` named `play`), `Assets/UI/GamePanelSettings.asset` (a `PanelSettings` with a theme style sheet assigned). No scene or prefab outside `Assets/Samples/` contains a `Canvas`.
- `Assets/Scenes/MainMenu.unity` (open, enabled in the Build Settings): a GameObject `Menu` with a `UIDocument` using `GamePanelSettings` and `MainMenu.uxml`, and a script `MenuController` whose `OnEnable` hooks the `play` button to `MySceneManager.TransitionAsync("Level1");`.
- `Assets/Scenes/Level1.unity` (enabled in the Build Settings) with a Main Camera and a cube named `LevelMarker`. No loading scene exists.

## Prompt
Give the agent exactly this message, nothing else:

> add a loading screen with a progress bar for when i hit play on the menu, it should fade in and out and stay up at least a second

## Expected behavior
- Invokes `my-scene-manager-loading-screens`.
- Applies the UI framework rule in Prerequisites, finds UI Toolkit only, and proceeds **without asking** and without creating any Canvas.
- Follows "Workflow: Create a UI Toolkit loading scene": a UXML document with a progress element, a `LoadingScreenComponent` that holds the show/hide gates while fading on unscaled time and updates the progress element, `LoadingBehavior` and `MinimumDisplayTime` (`seconds >= 1`) on a `UIDocument` GameObject reusing `GamePanelSettings`.
- Registers the loading scene and passes it to `TransitionAsync` in `MenuController`.
- Verifies with the Verification section.

## Pass criteria
- [ ] Pressing `play` reaches `Level1` through a loading screen: the transition passes `ScreenIn` and `ScreenOut` before `Completed`
- [ ] The screen visibly fades in and out (opacity ≤ 0.05 → ≥ 0.95 → ≤ 0.05), and the menu is not removed before the screen is fully opaque (opacity ≥ 0.95 when the operation enters `Unloading`)
- [ ] A progress element on the screen reaches full before the fade-out, and the screen stays fully visible for at least 1 s
- [ ] The solution stays in UI Toolkit: no `Canvas`, `CanvasGroup`, `LoadingFader` or `LoadingFeedback*` component was added anywhere, and no package was added to `Packages/manifest.json`
- [ ] Afterwards only `Level1` is loaded and nothing of the loading screen remains; any loading scene used is enabled in the Build Settings
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (add to a persistent object, or from a temporary `[RuntimeInitializeOnLoadMethod]`, before the transition starts; logs each phase with a timestamp):

```csharp
MyGameDevTools.SceneLoading.MySceneManager.OperationStarted += op =>
    op.StateChanged += o => UnityEngine.Debug.Log($"[CHECK] {o.Kind} {o.State} t={UnityEngine.Time.realtimeSinceStartup:0.000}");
```

Sample the loading screen's visible opacity (CanvasGroup alpha, UI Toolkit root `resolvedStyle.opacity`, or the overlay's alpha) and its progress element every frame while the transition runs; the criteria are judged from those samples and the phase log, not from which components were used.

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
