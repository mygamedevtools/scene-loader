---
id: my-scene-manager-setup-and-overview-S002
skill: my-scene-manager-setup-and-overview
title: "Agent imports the Loading Scene Examples sample and makes it runnable"
difficulty: intermediate
---

## Setup
- Unity 6000.0 or newer, My Scene Manager 5.x installed through the Package Manager (not embedded from source; for an embedded install, delete any previous `Assets/Samples/My Scene Manager` folder first).
- `com.unity.ugui` installed.
- The sample is not imported yet and none of its scenes are in the Build Settings.

## Prompt
Give the agent exactly this message, nothing else:

> i want to try the loading screen examples that come with my scene manager. set it up so i can just open it and hit play

## Expected behavior
- Invokes `my-scene-manager-setup-and-overview`.
- Follows "Workflow: Import and run the Loading Scene Examples sample".
- Imports the sample, registers its six scenes in the Build Settings (the user asked for a ready-to-play setup), and tells the user which scene to open.
- Verifies with the Verification section (Build Settings entries, no errors).

## Pass criteria
- [ ] A folder `Assets/Samples/My Scene Manager/<version>/Loading Scene Examples` exists
- [ ] `SceneA`, `SceneB`, `SceneListenerHUD`, `Extra`, `Loading_Screen` and `Loading_Animated` from that folder are enabled in the Build Settings
- [ ] Opening `SceneA` and entering Play Mode shows the examples list (not the "The sample needs its scenes" prompt)
- [ ] Clicking the "Loading scene" example reaches `SceneB` and the HUD shows `Completed`
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in the Editor, e.g. as a temporary Editor script):

```csharp
string[] required = { "SceneA", "SceneB", "SceneListenerHUD", "Extra", "Loading_Screen", "Loading_Animated" };
var registered = new System.Collections.Generic.HashSet<string>();
foreach (var s in UnityEditor.EditorBuildSettings.scenes)
    if (s.enabled && s.path.Contains("Loading Scene Examples")) registered.Add(System.IO.Path.GetFileNameWithoutExtension(s.path));
bool ok = System.Array.TrueForAll(required, registered.Contains);
UnityEngine.Debug.Log(ok ? "PASS: all sample scenes registered" : "FAIL: missing sample scenes in Build Settings");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
