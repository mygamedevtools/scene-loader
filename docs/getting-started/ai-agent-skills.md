---
sidebar_position: 4
description: Teach AI agents to use My Scene Manager with the skills the package ships.
---

# AI Agent Skills

The package ships **AI Skills**: instructions that teach AI agents how to use My Scene Manager correctly, in the `SKILL.md` format most agents read.
They live in `Packages/com.mygamedevtools.scene-loader/AIAssistantSkills/`, one folder per skill:

| Skill | Covers |
|---|---|
| `my-scene-manager-scene-transitions` | Moving between scenes: `TransitionAsync`, `ReloadActiveSceneAsync`, which scene becomes active, persistent scenes |
| `my-scene-manager-loading-screens` | Building and fixing loading screens in UI Toolkit or uGUI, the loading components, minimum display time and holds |
| `my-scene-manager-operation-handles` | The `SceneOperation` handle: awaiting, progress, states, cancelling, `WhenAll` / `WhenAny` |

Each skill describes the requests it applies to, so an agent reads it only when the work involves scene loading.
How the agent finds the skills depends on which one you use.

## Unity AI Assistant

**Unity AI Assistant** (a paid Unity feature) finds the skills on its own.

New skills start as **Deny**. Allow them in `Preferences > AI > Skills` (`Unity > Settings > AI > Skills` on macOS).

## Coding agents

Coding agents such as **Claude Code**, **Codex**, **GitHub Copilot**, **Cursor** and **Gemini CLI** read skills from folders at your project root.

Click `Tools > My Scene Manager > Install AI Skills for Coding Agents` to copy the skills to `.agents/skills/` and `.claude/skills/`.

:::info[After updating the package]
Click it again after every package update. It replaces the older copies and removes any skill that a newer version dropped.
It only touches folders named `my-scene-manager-*`, so other skills in those folders are left alone.
:::

## Any other agent

Add this line to your project's `AGENTS.md` (or your agent's instructions file):

```
For scene loading, transitions and loading screens, read the skills in Packages/com.mygamedevtools.scene-loader/AIAssistantSkills/ before writing code.
```
