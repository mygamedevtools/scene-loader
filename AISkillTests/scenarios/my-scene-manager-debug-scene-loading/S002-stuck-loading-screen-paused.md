---
id: my-scene-manager-debug-scene-loading-S002
skill: my-scene-manager-debug-scene-loading
title: "Agent finds an unreleased hide hold that hangs a transition started from a paused game"
difficulty: advanced
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x and `com.unity.ugui` installed.
- `Assets/Scenes/Level1.unity` (open), `Assets/Scenes/MainMenu.unity` and `Assets/Scenes/Loading.unity`, all enabled in the Build Settings.
- `Level1` has a script that, from `Start()`, sets `Time.timeScale = 0` and calls `MySceneManager.TransitionAsync("MainMenu", "Loading")` after `WaitForSecondsRealtime(1)` (simulating "quit to menu" from a pause screen).
- `Loading` has a Canvas root with `LoadingBehavior` and a child `Spinner` (Image) with this script:

  ```csharp
  using MyGameDevTools.SceneLoading;
  using UnityEngine;

  public class SpinnerOutro : LoadingScreenComponent
  {
      float _spun;
      bool _outro;

      protected override void OnBound()
      {
          Progress.HoldHide(this);
          Progress.LoadingCompleted += () => _outro = true;
      }

      void Update()
      {
          if (!_outro) return;
          _spun += 360f * Time.deltaTime;
          transform.rotation = Quaternion.Euler(0, 0, _spun);
          if (_spun >= 720f) { Progress.ReleaseHide(this); enabled = false; }
      }
  }
  ```

## Prompt
Give the agent exactly this message, nothing else:

> when i quit to the main menu from the pause screen the loading screen just stays there forever. it works fine from the main menu to the level

## Expected behavior
- Invokes `my-scene-manager-debug-scene-loading`.
- Follows "Workflow: Diagnose a transition stuck on its loading screen": waits for (or reproduces) `A transition has been waiting 10 seconds for SpinnerOutro on 'Spinner' (scene 'Loading') to call ReleaseHide()`.
- Identifies scaled time (`Time.deltaTime` at `timeScale == 0`) as the reason the hold is never released, and switches to unscaled time (`Time.unscaledDeltaTime`), consulting `my-scene-manager-loading-screens` as needed. May also restore `timeScale` in the menu, but the loading screen must work while paused.

## Pass criteria
- [ ] The agent names `SpinnerOutro`'s hide hold and scaled time as the cause
- [ ] `SpinnerOutro` advances on unscaled time (or otherwise releases the hide hold while `Time.timeScale == 0`)
- [ ] In Play Mode from `Level1`, the transition completes within 10 seconds: `MainMenu` is active, `Loading` and `Level1` are not loaded
- [ ] No "has been waiting 10 seconds" warning in the Console during the fixed run
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode 10 seconds after entering from `Level1`):

```csharp
bool ok = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu"
       && !UnityEngine.SceneManagement.SceneManager.GetSceneByName("Loading").isLoaded;
UnityEngine.Debug.Log(ok ? "PASS" : "FAIL: transition still blocked");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
