---
id: my-scene-manager-loading-screens-S001
skill: my-scene-manager-loading-screens
title: "Agent creates a fading loading scene with a progress bar and uses it in a transition"
difficulty: basic
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x and `com.unity.ugui` installed.
- The project's UI is uGUI: `MainMenu` has a Canvas with a title Text; there are no `.uxml` files or `PanelSettings` assets outside `Assets/Samples/`.
- `Assets/Scenes/MainMenu.unity` with a component `MenuController` whose public method `StartGame()` calls `MySceneManager.TransitionAsync("Level1");` and is triggered from `Start()` after a 1-second `WaitForSeconds`.
- `Assets/Scenes/Level1.unity` with a Main Camera. Both scenes enabled in the Build Settings. No loading scene exists.

## Prompt
Give the agent exactly this message, nothing else:

> make me a simple loading screen that fades in and out and has a progress bar, and use it when going from the menu to Level1

## Expected behavior
- Invokes `my-scene-manager-loading-screens` (and possibly `my-scene-manager-scene-transitions` for the call).
- Detects that the project's UI is uGUI (Prerequisites rule) and builds the loading scene without asking, following "Workflow: Create a uGUI loading scene": `LoadingBehavior` + `LoadingFader` on a Canvas root with `CanvasGroup`, a `Slider` with `LoadingFeedbackSlider`.
- Registers the loading scene in the Build Settings and changes `StartGame()` to pass it as the second argument.
- Verifies with the Verification section.

## Pass criteria
- [ ] The menu-to-Level1 path (`MenuController.StartGame()`) goes through a loading screen: the transition passes `ScreenIn` and `ScreenOut` before `Completed`
- [ ] The screen visibly fades in (opacity rises from ≤ 0.05 to ≥ 0.95) and fades out (falls back to ≤ 0.05) during the transition
- [ ] A progress bar on the screen shows the load's progress and reaches full (1 / 100%) before the screen fades out
- [ ] Any loading scene the solution uses is enabled in the Build Settings; afterwards only `Level1` is loaded and nothing of the loading screen remains
- [ ] The solution stays in the project's UI framework (uGUI): no `.uxml` or `PanelSettings` were added, and no packages were added
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
