# My Scene Manager

A Unity package (`com.mygamedevtools.scene-loader`, namespace `MyGameDevTools.SceneLoading`) for loading, unloading and transitioning scenes, with loading screens and one API for Addressables and Build Settings scenes. The repository root is the Unity project used to develop and test it.

- User documentation: https://scene-loader.mygamedevtools.com/ (source on the `docs` branch; documentation PRs target that branch, not `main`)
- Overview, features and install options: `README.md`
- Contributing guidelines: https://github.com/mygamedevtools/.github/blob/main/CONTRIBUTING.md

## Layout

| Path | What it is |
|---|---|
| `Packages/com.mygamedevtools.scene-loader/` | The package, the only thing that ships |
| `…/Runtime/` | `MySceneManager` (static entry point), `CoreSceneManager`, `SceneOperation`, `Backends/` (Build Settings and Addressables), `Loading/` (loading-screen components), `Diagnostics/` (`SceneManagerLog`) |
| `…/Editor/` | Editor-only tooling, such as the AI skills installer menu item |
| `…/Tests/Runtime/` | Play mode tests, the main suite (`Performance/` needs `com.unity.test-framework.performance`) |
| `…/Tests/Editor/` | Edit mode tests |
| `…/Samples/LoadingSceneExamples/` | The importable sample, declared in `package.json` |
| `…/AIAssistantSkills/` | AI skills shipped to users (see below) |
| `Packages/com.unity.asset-store-tools/` | Vendored Asset Store Tools; don't modify |
| `Assets/Scripts/Editor/PackageExporter.cs` | Exports the package as a `.unitypackage` for the Asset Store |
| `AISkillTests/` | AI skill benchmark scenarios and results; never ships |

The dev project runs on the Unity version in `ProjectSettings/ProjectVersion.txt`; the package supports Unity `6000.0`+ (`package.json`).

## Working on the package

- **Optional dependencies:** uGUI, TextMesh Pro, Addressables and Performance Testing are optional. Code that uses them must sit behind the version defines in the asmdefs (`ENABLE_UGUI`, `ENABLE_TMP`, `ENABLE_ADDRESSABLES`, `ENABLE_PERFORMANCE_TESTING`): the package must compile in a new project that has none of them, which is how the Asset Store validator checks it.
- **`DISABLE_STATIC_SCENE_MANAGER`** compiles `MySceneManager` out for projects that own a `CoreSceneManager`; nothing outside `MySceneManager` may reference it unguarded.
- **`.meta` files:** commit one for every new file and folder under `Packages/` and `Assets/`.
- **Tests:** run them with the Unity Test Runner. CI runs the play mode suite with coverage on `MyGameDevTools.SceneLoading` across several Unity versions (`.github/workflows/test.yml`, shared steps in `mygamedevtools/unity-package-ci`).

## Commits and releases

- Use Conventional Commits (`feat:`, `fix:`, `chore:`, `test:`, `ci:`, `docs:`). Releases are automatic from `main`: the commit type sets the version bump, and CI writes `CHANGELOG.md`, the `package.json` version and the `upm` branch. Don't edit those by hand.
- PRs are squash-merged, so the PR title becomes the commit message.

## AI skills

The package ships `SKILL.md` skills in `AIAssistantSkills/` (where Unity AI Assistant finds them); **Tools > My Scene Manager > Install AI Skills for Coding Agents** copies them for other agents.

- When code changes something a skill documents (API, menu item, default value, workflow), update that `SKILL.md` in the same change, bump its `skill-version` and set `last-verified` to today.
- If a skill turns out wrong or incomplete while you use the package, fix it and add a note to `AISkillTests/RESULTS.md`.
- Keep the required frontmatter fields and section headings; they are a shared Asset Store format.
- A skill must earn its place: if benchmark runs show it neither improves results nor saves time, remove it and note why in `RESULTS.md`.
- Before an Asset Store submission, re-run the scenarios in `AISkillTests/` (protocol in its `README.md`).
