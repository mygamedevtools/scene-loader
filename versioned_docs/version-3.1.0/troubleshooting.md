---
sidebar_position: 6
---

# Troubleshooting

## Error when creating an `AdvancedSceneManager`

When creating an `AdvancedSceneManager` passing a `true` value to its constructor, as `new AdvancedSceneManager(true)`, it attempts to add all loaded scenes to its list of tracked scenes.
However, if you called that during `Awake()`, you might see the error:

```
ArgumentException: Attempted to get an {nameof(ISceneData)} through an invalid or unloaded scene.
```

This error is thrown because during `Awake()` the scene is not fully loaded and cannot be added to the list of tracked scenes.


Move your call to `Start()` instead.

## Cannot unload a scene with a different `ILoadSceneInfo`

In a case where you have loaded a scene via one type of `ILoadSceneInfo`, you can only unload it by using the same type or explicitly a `LoadSceneInfoScene`. For example:

```cs
ILoadSceneInfo nameInfo = new LoadSceneInfoName("MyScene");
ILoadSceneInfo indexInfo = new LoadSceneInfoIndex(3);

sceneManager.LoadSceneAsync(nameInfo);

// You **cannot** do this:
sceneManager.UnloadSceneAsync(indexInfo);

// But you can do this:
sceneManager.UnoadSceneAsync(nameInfo);

// Or, build a `LoadSceneInfoScene`.
// Alternatives: GetLoadedSceneByName(name), GetLoadedSceneAt(index), GetLastLoadedScene() or GetActiveScene()
ILoadSceneInfo sceneInfo = sceneManager.GetLoadedSceneByName("MyScene");
sceneManager.UnloadSceneAsync(sceneInfo);
```

Sometimes this issue can also be avoided by performing a **Scene Transition**. If you're trying to unload the active scene to transition between scenes, you can execute the transition through the **Scene Manager** and let it handle the internal complexity. For example:

```cs
// Instead of unloading the source scene directly:
sceneManager.LoadSceneAsync(targetSceneInfo)
sceneManager.UnloadSceneAsync(sourceSceneInfo);

// Perform a scene transition:
sceneManager.TransitionToScene(targetSceneInfo);
```