---
id: my-scene-manager-load-and-unload-scenes-S003
skill: my-scene-manager-load-and-unload-scenes
title: "Agent finds that a Build Settings scene shadows the Addressables scene of the same name"
difficulty: advanced
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x and `com.unity.addressables` 1.19+ installed, Play Mode Script "Use Asset Database".
- `Assets/Scenes/Old/Arena.unity` (cube `OldArenaMarker`) is enabled in the Build Settings — a leftover from before the rework.
- `Assets/Addressables/Arena.unity` (cube `NewArenaMarker`) is an Addressables entry with address `Arena`, not in the Build Settings.
- `Assets/Scenes/Game.unity` (open, first in the Build Settings) has `ArenaLoader.cs`, which loads the arena 1 s after `Start` with `MySceneManager.LoadAsync("Arena")`.

## Prompt
Give the agent exactly this message, nothing else:

> i reworked the arena scene and made it addressable, but the game still loads the old arena. why? fix it

## Expected behavior
- Invokes `my-scene-manager-load-and-unload-scenes` (or `my-scene-manager-debug-scene-loading`).
- Identifies that a bare string resolves against the Build Settings **before** Addressables, and the old `Arena` is still in the Build Settings.
- Fixes it by loading `SceneRef.Address("Arena")` and/or removing the old scene from the Build Settings.

## Pass criteria
- [ ] In Play Mode from `Game`, after `ArenaLoader` runs: `NewArenaMarker` exists and `OldArenaMarker` does not, and exactly one `Arena` scene is loaded
- [ ] The agent's explanation names the cause: the Build Settings entry takes precedence over the Addressables entry with the same name
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode 3 s after entering from `Game`):

```csharp
bool ok = UnityEngine.GameObject.Find("NewArenaMarker") != null && UnityEngine.GameObject.Find("OldArenaMarker") == null;
UnityEngine.Debug.Log(ok ? "PASS" : "FAIL");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
