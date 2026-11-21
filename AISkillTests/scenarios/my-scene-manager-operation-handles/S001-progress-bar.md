---
id: my-scene-manager-operation-handles-S001
skill: my-scene-manager-operation-handles
title: "Agent drives a HUD progress bar from a load operation and logs on completion"
difficulty: basic
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x and `com.unity.ugui` installed.
- `Assets/Scenes/Boot.unity` (open, first in the Build Settings) containing a Canvas with a uGUI `Slider` named `LoadingBar` (min 0, max 1, value 0) and a GameObject `Loader` with no scripts.
- `Assets/Scenes/Level1.unity` enabled in the Build Settings, containing a cube named `LevelMarker`.

## Prompt
Give the agent exactly this message, nothing else:

> on Boot, start loading Level1 right away and show the progress on the LoadingBar slider. when its done print "level ready" in the console

## Expected behavior
- Invokes `my-scene-manager-operation-handles`.
- Follows the Quick start / "Workflow: Show progress" and "Workflow: Await an operation": a script on `Loader` that calls `MySceneManager.LoadAsync("Level1")` from `Start` (not `Awake`), binds `Progressed` (or polls `Progress`) to the slider, and logs after `await`/`Completed`, with exception handling for `async void`.
- Verifies with the Verification section.

## Pass criteria
- [ ] A script on `Loader` starts the load from `Start()` or later
- [ ] `LoadingBar.value` is driven by the operation's `Progressed` event or `Progress` property
- [ ] In Play Mode: `Level1` loads, `LoadingBar.value == 1` afterwards, and "level ready" is logged exactly once
- [ ] No `InvalidOperationException` about the static manager
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode after the load completes):

```csharp
var bar = UnityEngine.GameObject.Find("LoadingBar").GetComponent<UnityEngine.UI.Slider>();
bool ok = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Level1").isLoaded && UnityEngine.Mathf.Approximately(bar.value, 1f);
UnityEngine.Debug.Log(ok ? "PASS" : $"FAIL (bar {bar.value})");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
