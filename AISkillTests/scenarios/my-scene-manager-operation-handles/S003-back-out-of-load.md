---
id: my-scene-manager-operation-handles-S003
skill: my-scene-manager-operation-handles
title: "Agent makes backing out of a load actually remove the scene, which Cancel() alone does not"
difficulty: advanced
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x installed. Active Input Handling: Input Manager (Old) or Both.
- `Assets/Scenes/Game.unity` (open, first in the Build Settings; Main Camera, cube `Player`) and `Assets/Scenes/NextArea.unity` (cube `NextAreaMarker`, enabled in the Build Settings).
- `Game` has a GameObject with `Assets/Scripts/AreaStreamer.cs`:

  ```csharp
  using MyGameDevTools.SceneLoading;
  using UnityEngine;

  public class AreaStreamer : MonoBehaviour
  {
      SceneOperation _op;

      public void StartLoading() => _op = MySceneManager.LoadAsync("NextArea");
      public void BackOut() => _op?.Cancel();

      void Update()
      {
          if (Input.GetKeyDown(KeyCode.L)) StartLoading();
          if (Input.GetKeyDown(KeyCode.Escape)) BackOut();
      }
  }
  ```

## Prompt
Give the agent exactly this message, nothing else:

> i can cancel streaming in the next area with escape, but the area still pops in a moment later anyway. backing out should make sure it doesnt stay loaded

## Expected behavior
- Invokes `my-scene-manager-operation-handles` (cancel workflow).
- Identifies that cancelling cannot abort Unity's load, and that a canceled operation's scene is not tracked by the manager, so it cannot simply be unloaded with `MySceneManager.UnloadAsync`.
- Implements backing out so the scene is removed (e.g. keep the handle and unload the result once it completes, or unload the untracked scene through `SceneManager`).

## Pass criteria
- [ ] Calling `StartLoading()` then `BackOut()` in the same frame (grader, Play Mode): 3 s later `NextArea` is not loaded, `Game` is loaded and active, and `NextArea` never became the active scene
- [ ] Backing out at other moments also works: `BackOut()` a few frames into the load, and `BackOut()` after the load has already finished, each leave `NextArea` not loaded 3 s later
- [ ] `StartLoading()` without backing out still loads `NextArea` (the feature still works)
- [ ] The fix is in the project's own code: the installed package's files are unchanged
- [ ] No compile errors, unhandled exceptions or "Unable to match a managed scene" errors in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode 3 s after the grader calls StartLoading(); BackOut()):

```csharp
bool ok = !UnityEngine.SceneManagement.SceneManager.GetSceneByName("NextArea").isLoaded
       && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game";
UnityEngine.Debug.Log(ok ? "PASS" : "FAIL");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
