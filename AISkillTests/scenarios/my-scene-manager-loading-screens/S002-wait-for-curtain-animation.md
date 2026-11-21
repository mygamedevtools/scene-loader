---
id: my-scene-manager-loading-screens-S002
skill: my-scene-manager-loading-screens
title: "Agent makes the transition wait for a loading-screen animation using show and hide holds"
difficulty: advanced
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x and `com.unity.ugui` installed.
- `Assets/Scenes/Loading.unity` (enabled in the Build Settings): a Canvas root with `LoadingBehavior`, and a child `Curtain` (Image) with an `Animator` whose controller has triggers `Close` and `Open`, a 0.5 s `Close` clip and a 0.5 s `Open` clip, Update Mode `Unscaled Time`. A script `CurtainPlayer` on `Curtain` calls `SetTrigger("Close")` in `Start()` and subscribes nothing else.
- `Assets/Scenes/MainMenu.unity` and `Assets/Scenes/Level1.unity` in the Build Settings; `MainMenu` has a component calling `MySceneManager.TransitionAsync("Level1", "Loading")` from `Start()` after a 1-second wait.

## Prompt
Give the agent exactly this message, nothing else:

> the curtain animation on my loading screen gets cut off, the menu disappears before the curtain is even closed and the level pops in before it opens. make the scene change actually wait for the curtain

## Expected behavior
- Invokes `my-scene-manager-loading-screens`.
- Follows "Workflow: Write a component the transition waits for": replaces or extends `CurtainPlayer` with a `LoadingScreenComponent` that takes `HoldShow`/`HoldHide` in `OnBound()`, plays `Close` immediately and releases show when it ends, plays `Open` on `LoadingCompleted` and releases hide when it ends, on unscaled time.
- Verifies with the Verification section.

## Pass criteria
- [ ] In Play Mode, the curtain is fully closed (`Curtain` localScale.x ≥ 0.99) when the operation enters `Unloading`, so the menu is never seen being removed
- [ ] The curtain fully opens again (localScale.x back to ≤ 0.01) before the loading scene unloads: the time between `ScreenOut` and `Completed` is at least 0.45 s
- [ ] No "has been waiting 10 seconds" warning; the transition completes with only `Level1` loaded
- [ ] The fix keeps working when the game is paused (`Time.timeScale = 0`) at the moment of the transition, or the solution does not depend on scaled time
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
