---
id: my-scene-manager-scene-transitions-S001
skill: my-scene-manager-scene-transitions
title: "Agent wires a menu button to transition from MainMenu to Level1"
difficulty: basic
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x and `com.unity.ugui` installed.
- `Assets/Scenes/MainMenu.unity` containing a Canvas, an EventSystem, and a uGUI Button named `PlayButton` with no OnClick listeners.
- `Assets/Scenes/Level1.unity` containing a Main Camera and a cube named `LevelMarker`.
- Both scenes enabled in the Build Settings, `MainMenu` first. `MainMenu` is open.

## Prompt
Give the agent exactly this message, nothing else:

> make the play button on the main menu take me to Level1

## Expected behavior
- Invokes `my-scene-manager-scene-transitions`.
- Follows the Quick start / "Workflow: Transition without a loading screen": a component calling `MySceneManager.TransitionAsync("Level1")` (or a serialized scene name), hooked to `PlayButton`.
- Does not call `MySceneManager` from `Awake`/`OnEnable`.
- Verifies with the skill's Verification section in Play Mode.

## Pass criteria
- [ ] A script in the project calls `MySceneManager.TransitionAsync` with `Level1` (directly or through a serialized field set to `Level1`)
- [ ] `PlayButton` triggers it (a persistent OnClick listener in `MainMenu.unity`, or `onClick.AddListener` from `Start`)
- [ ] In Play Mode, after clicking `PlayButton`: `SceneManager.GetActiveScene().name == "Level1"` and `MainMenu` is not loaded
- [ ] No `[MySceneManager]` errors
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode after clicking the button, e.g. through an Editor code-execution tool):

```csharp
bool ok = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Level1"
       && !UnityEngine.SceneManagement.SceneManager.GetSceneByName("MainMenu").isLoaded
       && UnityEngine.GameObject.Find("LevelMarker") != null;
UnityEngine.Debug.Log(ok ? "PASS" : "FAIL");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
