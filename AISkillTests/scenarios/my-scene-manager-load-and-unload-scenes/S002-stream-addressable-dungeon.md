---
id: my-scene-manager-load-and-unload-scenes-S002
skill: my-scene-manager-load-and-unload-scenes
title: "Agent streams in an Addressables scene and unloads the previous area after it loads"
difficulty: intermediate
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x and `com.unity.addressables` 1.19+ installed, Addressables settings created, Play Mode Script "Use Asset Database".
- `Assets/Scenes/Town.unity`: enabled in the Build Settings, contains a Main Camera, a cube named `TownMarker`, and a GameObject `CaveEntrance` with a `BoxCollider` (Is Trigger) and no scripts.
- `Assets/Scenes/Dungeon.unity`: **not** in the Build Settings, marked Addressable in the default group with address `Dungeon`, contains a cube named `DungeonMarker`.
- A GameObject `Player` in `Town` with a `Rigidbody` (kinematic) and a `CapsuleCollider`, tagged `Player`.
- `Town` is open.

## Prompt
Give the agent exactly this message, nothing else:

> when the player walks into the cave entrance trigger, stream in the dungeon (its an addressable scene) and once its loaded unload the town

## Expected behavior
- Invokes `my-scene-manager-load-and-unload-scenes`.
- Follows "Workflow: Load Addressables scenes" (string `"Dungeon"` or `SceneRef.Address("Dungeon")`) and "Workflow: Unload scenes", unloading `Town` only after the load completes (await, `Completed`, or coroutine), and ensures the trigger fires once.
- Verifies with the Verification section.

## Pass criteria
- [ ] A trigger script on `CaveEntrance` (or the player) starts `MySceneManager.LoadAsync` for `Dungeon`
- [ ] `Town` is unloaded only after the `Dungeon` load completes (code awaits/handles completion before calling `UnloadAsync`)
- [ ] In Play Mode, after moving `Player` into the trigger: `Dungeon` is loaded and active, `Town` is not loaded, `DungeonMarker` exists
- [ ] The operation does not fault (no "Could not resolve the scene" error) and `Dungeon` is loaded exactly once
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in Play Mode after the stream completes):

```csharp
bool ok = UnityEngine.SceneManagement.SceneManager.GetSceneByName("Dungeon").isLoaded
       && !UnityEngine.SceneManagement.SceneManager.GetSceneByName("Town").isLoaded
       && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Dungeon";
UnityEngine.Debug.Log(ok ? "PASS" : "FAIL");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
