---
id: my-scene-manager-scene-transitions-S004
skill: my-scene-manager-scene-transitions
title: "Agent stops a level's extra scene from surviving transitions and piling up on replay"
difficulty: advanced
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x installed.
- Scenes, all enabled in the Build Settings: `MainMenu` (Main Camera), `Level1` (Main Camera, cube `Level1Marker`), `Level1_Lighting` (a Directional Light named `LevelLight`). `MainMenu` is open.
- `MainMenu` has `MenuController.cs` with `public void Play() => MySceneManager.TransitionAsync(new[] { "Level1", "Level1_Lighting" });`.
- `Level1` has `LevelExit.cs` with `public void ExitToMenu() => MySceneManager.TransitionAsync("MainMenu");`.

## Prompt
Give the agent exactly this message, nothing else:

> when i go back to the main menu from level 1 the level lighting is still there, and when i play again i get two of it. make the level's lighting go away with the level

## Expected behavior
- Invokes `my-scene-manager-scene-transitions`.
- Identifies that a transition unloads only the active scene, so `Level1_Lighting` (loaded alongside, not active) survives `TransitionAsync("MainMenu")`.
- Unloads `Level1_Lighting` as part of leaving the level (e.g. `UnloadAsync` before/after the transition), without breaking `Play()`.

## Pass criteria
- [ ] Running the game's own methods in Play Mode — `MenuController.Play()`, then `LevelExit.ExitToMenu()`, then `Play()` again, then `ExitToMenu()` again, waiting for each to finish — after every `ExitToMenu`: `MainMenu` is active and `Level1_Lighting` is not loaded
- [ ] After every `Play`: exactly one `Level1` and exactly one `Level1_Lighting` are loaded, and `Level1` is active
- [ ] The agent's explanation names the cause: a transition unloads only the active scene
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode after the final `ExitToMenu` completes):

```csharp
bool ok = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "MainMenu"
       && !UnityEngine.SceneManagement.SceneManager.GetSceneByName("Level1_Lighting").isLoaded
       && UnityEngine.Object.FindObjectsByType<UnityEngine.Light>(UnityEngine.FindObjectsInactive.Include, UnityEngine.FindObjectsSortMode.None).Length == 0;
UnityEngine.Debug.Log(ok ? "PASS" : "FAIL");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
