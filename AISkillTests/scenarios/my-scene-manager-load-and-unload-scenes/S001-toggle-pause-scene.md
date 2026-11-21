---
id: my-scene-manager-load-and-unload-scenes-S001
skill: my-scene-manager-load-and-unload-scenes
title: "Agent loads and unloads a pause scene additively on a key toggle"
difficulty: basic
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x installed. Active Input Handling set to Input Manager (Old) or Both.
- `Assets/Scenes/Game.unity` (Main Camera, a cube named `Player`) and `Assets/Scenes/PauseMenu.unity` (a Canvas with a Text "Paused"), both enabled in the Build Settings, `Game` first.
- `Game` is open.

## Prompt
Give the agent exactly this message, nothing else:

> when i press escape in the game, load the PauseMenu scene on top. press escape again and it goes away

## Expected behavior
- Invokes `my-scene-manager-load-and-unload-scenes`.
- Follows the Quick start / "Workflow: Load one scene" and "Workflow: Unload scenes": `MySceneManager.LoadAsync("PauseMenu")` without activation, `MySceneManager.UnloadAsync(...)` preferably with the loaded `Scene`.
- Guards against double loads while a load is in flight.
- Verifies with the skill's Verification section.

## Pass criteria
- [ ] In Play Mode, after one Escape press and the load completing: `PauseMenu` is loaded, `Game` is still loaded and still the active scene
- [ ] After a second press: `PauseMenu` is not loaded, `Game` still loaded and active
- [ ] Pressing Escape twice within the same frame or before the load completes never results in two `PauseMenu` scenes loaded at once
- [ ] No `[MySceneManager]` errors
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode after the first press completes):

```csharp
int pause = 0;
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name == "PauseMenu") pause++;
bool ok = pause == 1 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game";
UnityEngine.Debug.Log(ok ? "PASS" : $"FAIL (PauseMenu count {pause})");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
