---
id: my-scene-manager-operation-handles-S002
skill: my-scene-manager-operation-handles
title: "Agent fixes a transition canceled by the wrong token and handles cancellation cleanly"
difficulty: intermediate
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x installed.
- `Assets/Scenes/MainMenu.unity` (open), `Assets/Scenes/Loading.unity` (a Canvas with `LoadingBehavior` and a `MinimumDisplayTime` of 1 s) and `Assets/Scenes/Level1.unity`, all enabled in the Build Settings.
- `MainMenu` contains a GameObject with this script, `Assets/Scripts/LevelLoader.cs`:

  ```csharp
  using MyGameDevTools.SceneLoading;
  using UnityEngine;

  public class LevelLoader : MonoBehaviour
  {
      async void Start()
      {
          await Awaitable.WaitForSecondsAsync(1f);
          await MySceneManager.TransitionAsync("Level1", "Loading").CancelWith(destroyCancellationToken);
          Debug.Log("level ready");
      }
  }
  ```

## Prompt
Give the agent exactly this message, nothing else:

> my level loader keeps throwing OperationCanceledException and "level ready" never prints, and Level1 never shows up. i do want it to cancel if the player quits the game mid load though. fix it

## Expected behavior
- Invokes `my-scene-manager-operation-handles`.
- Identifies, per "Workflow: Cancel an operation", that `destroyCancellationToken` belongs to a component in `MainMenu`, which the transition unloads, so the token cancels the transition halfway.
- Switches to `Application.exitCancellationToken` (or a persistent owner's token), and wraps the await in `try/catch (OperationCanceledException)` per "Workflow: Await an operation".
- Verifies with the Verification section.

## Pass criteria
- [ ] `LevelLoader` no longer passes `destroyCancellationToken` of a component in `MainMenu`
- [ ] The await is wrapped so a cancellation does not surface as an unhandled exception
- [ ] In Play Mode: the transition completes, `Level1` is active, `MainMenu` and `Loading` are not loaded, and "level ready" is logged once
- [ ] Exiting Play Mode during the loading screen produces no unhandled exception in the Console
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode after the transition):

```csharp
bool ok = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level1"
       && !UnityEngine.SceneManagement.SceneManager.GetSceneByName("MainMenu").isLoaded
       && !UnityEngine.SceneManagement.SceneManager.GetSceneByName("Loading").isLoaded;
UnityEngine.Debug.Log(ok ? "PASS" : "FAIL");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
