---
id: my-scene-manager-debug-scene-loading-S001
skill: my-scene-manager-debug-scene-loading
title: "Agent diagnoses a load that faults because the scene cannot be resolved"
difficulty: basic
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x installed, Addressables **not** installed.
- `Assets/Scenes/MainMenu.unity` (open, enabled in the Build Settings) with a script whose `Start()` calls `MySceneManager.TransitionAsync("Level1");` after a 1-second wait.
- `Assets/Scenes/Level1.unity` exists but is **not** in the Build Settings.

## Prompt
Give the agent exactly this message, nothing else:

> my game wont go to level 1, i press play and nothing happens. figure out whats wrong

## Expected behavior
- Invokes `my-scene-manager-debug-scene-loading`.
- Reproduces in Play Mode (optionally with `SceneManagerLog.Level = SceneLogLevel.Verbose`), finds `Transition operation faulted during …: Could not resolve the scene 'Level1'. It was not found in the build settings (Addressables is not installed…)`.
- Follows "Workflow: Diagnose a scene that resolves wrongly or not at all": adds `Level1` to the Build Settings (enabled), explains the cause, and re-tests.

## Pass criteria
- [ ] The agent names the cause: `Level1` is not in the Build Settings, so the string cannot be resolved
- [ ] `Assets/Scenes/Level1.unity` is an enabled Build Settings entry afterwards
- [ ] In Play Mode the transition completes and `Level1` is the active scene
- [ ] Any temporary log-level change is reverted
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in the Editor):

```csharp
bool ok = System.Array.Exists(UnityEditor.EditorBuildSettings.scenes, s => s.enabled && s.path == "Assets/Scenes/Level1.unity");
UnityEngine.Debug.Log(ok ? "PASS" : "FAIL: Level1 not registered");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
