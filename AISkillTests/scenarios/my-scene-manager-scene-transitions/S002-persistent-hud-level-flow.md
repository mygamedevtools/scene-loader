---
id: my-scene-manager-scene-transitions-S002
skill: my-scene-manager-scene-transitions
title: "Agent builds a level flow with a persistent HUD scene and a loading screen"
difficulty: advanced
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x and `com.unity.ugui` installed.
- Scenes, all enabled in the Build Settings in this order: `Assets/Scenes/Boot.unity` (empty except a Main Camera), `Assets/Scenes/HUD.unity` (a Canvas with a Text reading "HUD"), `Assets/Scenes/Level1.unity`, `Assets/Scenes/Level2.unity` (each with a Main Camera and a cube named `Level1Marker` / `Level2Marker`).
- No loading scene exists. `Boot` is open.
- The project uses the legacy Input Manager or Both (Project Settings > Player > Active Input Handling).

## Prompt
Give the agent exactly this message, nothing else:

> i got a HUD scene that should stay loaded the whole game. when the game starts from Boot load the HUD and go to Level1. pressing N should go to the next level (1 -> 2 -> 1...) with a loading screen, theres no loading screen yet so just make a basic one

## Expected behavior
- Invokes `my-scene-manager-scene-transitions`, and `my-scene-manager-loading-screens` for the loading scene.
- Loads `HUD` additively once without activating it ("Workflow: Keep a scene loaded across transitions").
- Transitions `Boot → Level1` and between levels with `TransitionAsync(next, "<loading scene>")`, keeping a level as the active scene.
- Creates a loading scene with a `LoadingBehavior` (plus fader/feedback) and registers it in the Build Settings.
- Starts transitions from `Start` or `Update`, never `Awake`/`OnEnable` of `Boot`.
- Verifies loaded scenes after several transitions.

## Pass criteria
- [ ] A loading scene exists, is enabled in the Build Settings, and contains exactly one `LoadingBehavior`
- [ ] After entering Play Mode from `Boot`: `HUD` and `Level1` are loaded, `Level1` is active, `Boot` is not loaded
- [ ] After pressing N three times (waiting for each transition): `Level2` is active, `HUD` is loaded exactly once, `Level1`, `Boot` and the loading scene are not loaded
- [ ] Each level change passes through `ScreenIn` and `ScreenOut` (observable via `MySceneManager.OperationStarted` or Verbose logs)
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode after the third N press completes):

```csharp
int hud = 0;
for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
    if (UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).name == "HUD") hud++;
bool ok = hud == 1
       && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level2"
       && !UnityEngine.SceneManagement.SceneManager.GetSceneByName("Level1").isLoaded
       && !UnityEngine.SceneManagement.SceneManager.GetSceneByName("Boot").isLoaded;
UnityEngine.Debug.Log(ok ? "PASS" : $"FAIL (HUD count {hud}, active {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name})");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
