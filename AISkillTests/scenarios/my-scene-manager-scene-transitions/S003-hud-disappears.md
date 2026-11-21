---
id: my-scene-manager-scene-transitions-S003
skill: my-scene-manager-scene-transitions
title: "Agent fixes a persistent HUD scene that the first transition unloads because it was made the active scene"
difficulty: intermediate
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x and `com.unity.ugui` installed.
- Scenes, all enabled in the Build Settings in this order: `Boot` (a Main Camera), `HUD` (a Canvas with a Text "HUD"), `Level1` and `Level2` (a Main Camera and a cube `Level1Marker` / `Level2Marker`). `Boot` is open.
- `Boot` has a GameObject with `Assets/Scripts/GameBoot.cs`:

  ```csharp
  using MyGameDevTools.SceneLoading;
  using UnityEngine;

  public class GameBoot : MonoBehaviour
  {
      async void Start()
      {
          // HUD is active so its lighting settings apply.
          await MySceneManager.LoadAsync(new SceneParameters("HUD", true));
          MySceneManager.TransitionAsync("Level1");
      }
  }
  ```

## Prompt
Give the agent exactly this message, nothing else:

> the HUD disappears as soon as the game goes into Level1 and the Boot scene just stays there. the HUD should stay loaded the whole game

## Expected behavior
- Invokes `my-scene-manager-scene-transitions` (persistent-scene workflow).
- Identifies that `TransitionAsync` unloads only the **active** scene, so making `HUD` active means the transition unloads `HUD` instead of `Boot`.
- Loads `HUD` without activating it (or otherwise keeps a non-HUD scene active), so `Boot` is what the first transition unloads.

## Pass criteria
- [ ] After entering Play Mode from `Boot` and waiting for startup: `HUD` and `Level1` are loaded, `Level1` is active, `Boot` is not loaded
- [ ] After a further `MySceneManager.TransitionAsync("Level2")` (run by the grader): `HUD` is loaded exactly once, `Level2` is active, `Level1` is not loaded
- [ ] The agent's explanation names the cause: a transition unloads the active scene, and HUD was the active scene
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode after the grader's Level2 transition completes):

```csharp
int hud = 0;
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name == "HUD") hud++;
bool ok = hud == 1 && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level2"
       && !UnityEngine.SceneManagement.SceneManager.GetSceneByName("Boot").isLoaded
       && !UnityEngine.SceneManagement.SceneManager.GetSceneByName("Level1").isLoaded;
UnityEngine.Debug.Log(ok ? "PASS" : $"FAIL (HUD count {hud}, active {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name})");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
