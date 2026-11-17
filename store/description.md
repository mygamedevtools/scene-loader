**My Scene Manager** loads, unloads and transitions scenes in one line.

`MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");`

That replaces the usual chain of additive loads, `SetActiveScene` and unloads. The same line works whether the scenes come from your Build Settings or from Addressables: the strings resolve themselves, so there is no separate addressable API to learn.

## Features

- **One call per operation:** load, unload, reload or transition one or many scenes.
- **Addressable and non-addressable scenes:** a plain string finds your scene wherever it lives.
- **A handle for every operation:** progress, phase, per-scene events and cancellation, after the call rather than before it.
- **Await it any way you like:** `await` directly, bridge to a `Task`, or `yield return` it from a coroutine.
- **Loading screens beyond scenes:** scenes, prefabs or UI Toolkit documents, with built-in components for each.
- **Modular:** use only the components you need.

Read [the documentation](https://scene-loader.mygamedevtools.com) for guides, the API and upgrade notes.

## Samples

**Loading Scene Examples** has two rooms and one line of code per example: loading screens from a scene, a prefab and a UI Toolkit document, multi-scene loads, reloads and awaited transitions. A persistent HUD scene reports every operation's phase and progress while it runs. Import it from the Package Manager's Samples tab.

Addressables support turns on automatically when the Addressables package is installed; it isn't required.

The game scenes in the screenshots are from Unity's free [3D Game Kit](https://assetstore.unity.com/packages/templates/tutorials/3d-game-kit-115747), running My Scene Manager with the sample's HUD.
