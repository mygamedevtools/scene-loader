# AI skill tests — My Scene Manager

Benchmark scenarios for the AI Skills shipped at `Packages/com.mygamedevtools.scene-loader/AIAssistantSkills/`. This folder lives at the repository root, outside the package, so it is never published to the Asset Store, OpenUPM or the UPM tarball. Do not move it under `Assets/` or into the package folder.

## Layout

```
AISkillTests/
├── README.md        ← this protocol
├── RESULTS.md       ← one row per run
└── scenarios/
    └── <skill-name>/
        └── S<NNN>-<slug>.md
```

Every skill has at least two scenarios: one `basic` and one `intermediate` or `advanced`. Scenarios that build UI pin the project's UI framework in their Setup (uGUI in loading-screens S001, UI Toolkit in S003), because the skills follow the project's framework and only ask when it is ambiguous. Each scenario file states its **Setup**, the exact **Prompt** to give the agent, the **Expected behavior**, objectively checkable **Pass criteria**, an optional scripted check, and the **Scoring** rule.

| Skill | Scenarios |
|---|---|
| `my-scene-manager-setup-and-overview` | S001 check install (basic), S002 set up the sample (intermediate) |
| `my-scene-manager-scene-transitions` | S001 play button (basic), S002 persistent HUD level flow (advanced) |
| `my-scene-manager-load-and-unload-scenes` | S001 toggle pause scene (basic), S002 stream an Addressables dungeon (intermediate) |
| `my-scene-manager-loading-screens` | S001 fade and progress bar in a uGUI project (basic), S002 wait for a curtain animation (advanced), S003 loading screen in a UI Toolkit project (intermediate) |
| `my-scene-manager-operation-handles` | S001 progress bar (basic), S002 fix a self-canceling transition (intermediate) |
| `my-scene-manager-debug-scene-loading` | S001 scene not in the Build Settings (basic), S002 stuck loading screen while paused (advanced) |

## Test project

Run scenarios in a **separate, disposable Unity project** (Unity 6000.0+) with My Scene Manager installed from the package under test — for example as a local package pointing at `Packages/com.mygamedevtools.scene-loader` in this repository, so the skills under test are the ones the agent sees. Put the test project under version control so every run starts from the same commit.

Prepare each scenario's **Setup** exactly as written, commit it, and tag the commit with the scenario id (for example `my-scene-manager-loading-screens-S001`). Resetting is then `git reset --hard <tag> && git clean -fdx -e Library`.

## Benchmark protocol

Each scenario is run twice, from an identical starting state.

1. **Baseline run** — reset the test project to the scenario's tag (fresh scene, reverted state). Make the skills **unavailable**: rename `AIAssistantSkills` to `AIAssistantSkills~` inside the installed package (Unity ignores folders ending in `~`) and make sure no copies exist in the project's `.agents/skills/` or `.claude/skills/` (from **Tools > My Scene Manager > Install AI Skills for Coding Agents**), or use an agent session that has no access to them. Give the agent the scenario's **Prompt**, verbatim and nothing else. Do not help it. Record the result.
2. **Skill run** — reset to the same tag. Make the skills **available** (restore `AIAssistantSkills`). Give the same Prompt, verbatim. Record the result.
3. Log **both** rows in `RESULTS.md`. Reset the project state (revert through version control, reopen the Setup scene) between every run, including between the two runs of the same scenario.

Scoring per run:

- **Pass** — every pass criterion met, no human intervention.
- **Partial** — the outcome was achieved but a criterion was missed, or one human assist was needed.
- **Fail** — wrong or no outcome, errors, or the agent gave up.

Also record wall-clock time (first prompt to the agent's final message) and tokens or credits from the agent's usage report.

The **baseline-vs-skill delta** — pass rate, wall-clock time, tokens/credits — is the benchmark. It is the publisher's evidence that the skills make agents better at using My Scene Manager. A scenario where the skill run is not better than the baseline points at a skill that needs work.

## Checking pass criteria

- Prefer the scenario's optional scripted check: paste it into a temporary Editor script or an Editor code-execution tool and read the `PASS` / `FAIL` log. Checks marked "run in Play Mode" must run while the game is in Play Mode, after the described action.
- Criteria that need observation (for example "the transition passes `ScreenIn`") can be checked by adding a temporary `MySceneManager.OperationStarted` listener, or with `SceneManagerLog.Level = SceneLogLevel.Verbose`, which logs every `<Kind> operation entered <State>.` line.
- Remove any temporary check scripts before the next run.

## When to run

- Before every Asset Store / OpenUPM release: all scenarios in **skill** mode at least; fix failures first.
- After changing a skill: that skill's scenarios in both modes.
- After an API change in the package: every scenario whose skill documents the changed API.

When an agent finds a skill instruction wrong, unclear or missing during any run, fix the skill and add a note row to `RESULTS.md` describing the correction.
