---
id: my-scene-manager-setup-and-overview-S001
skill: my-scene-manager-setup-and-overview
title: "Agent confirms the package is installed and reports its version and source without changing the project"
difficulty: basic
---

## Setup
- Unity 6000.0 or newer, with My Scene Manager 5.x installed through any method.
- Any open scene. Commit or stash the project state first so changes can be detected (`git status` clean).

## Prompt
Give the agent exactly this message, nothing else:

> hey is the scene manager thing installed in this project? what version is it

## Expected behavior
- Invokes `my-scene-manager-setup-and-overview`.
- Follows "Workflow: Verify the installation and version" (the install check in Prerequisites).
- Reports the version and the install source (Package Manager source, or Asset Store import), and whether the static `MySceneManager` API is available.
- Uses the Verification section to confirm, without modifying project files.

## Pass criteria
- [ ] The reported version equals `version` in the package's `package.json`
- [ ] The reported install source matches reality (e.g. Embedded/Registry/Git, or Asset Store)
- [ ] The agent says whether `MySceneManager` is available (it is, unless `DISABLE_STATIC_SCENE_MANAGER` is defined)
- [ ] `git status` shows no changes, other than an optional temporary check script the agent removed again
- [ ] No compile errors or unhandled exceptions in the Console
- [ ] Completed without human intervention

Optional scripted check (run in the Editor, e.g. as a temporary Editor script):

```csharp
var type = System.Type.GetType("MyGameDevTools.SceneLoading.CoreSceneManager, MyGameDevTools.SceneLoading");
var info = type == null ? null : UnityEditor.PackageManager.PackageInfo.FindForAssembly(type.Assembly);
UnityEngine.Debug.Log(type == null ? "FAIL: not installed" : $"PASS: installed, version {(info != null ? info.version : "see package.json (Asset Store)")} — compare with the agent's answer");
```

## Scoring
- **Pass** — all criteria met, no human intervention
- **Partial** — outcome achieved but a criterion missed, or one human assist needed
- **Fail** — wrong or no outcome, errors, or the agent gave up
