---
sidebar_position: 3
title: Scene Loader
---

# The Scene Loader

The **Scene Loader** is an interface that you will use to load scenes in your game, as it works like a wrapper layer to the **Scene Manager**, but adds the **Scene Transition** operations.
There are two base interfaces for **Scene Loaders**: one with a reference to the `ISceneManager` that will be used, and an `async` interface to be able to `await` the load operations.

## `ISceneLoader` interfaces

The `ISceneLoader` interface defines:

```cs
public interface ISceneLoader : IDisposable
{
  ISceneManager Manager { get; }

  void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null);

  void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);

  void UnloadScenes(ILoadSceneInfo[] sceneInfos);

  void UnloadScene(ILoadSceneInfo sceneInfo);

  void LoadScenes(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1);

  void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false);
}
```

And the `ISceneLoaderAsync`:

```cs
public interface ISceneLoaderAsync<TAsyncScene, TAsyncSceneArray> : ISceneLoader
{
  TAsyncSceneArray TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = default, CancellationToken token = default);
  
  TAsyncScene TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = default, CancellationToken token = default);

  TAsyncSceneArray LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null, CancellationToken token = default);

  TAsyncScene LoadSceneAsync(ILoadSceneInfo sceneReference, bool setActive = false, IProgress<float> progress = null, CancellationToken token = default);

  TAsyncSceneArray UnloadScenesAsync(ILoadSceneInfo[] sceneReferences, CancellationToken token = default);

  TAsyncScene UnloadSceneAsync(ILoadSceneInfo sceneReference, CancellationToken token = default);
}
```

Note that the `ISceneLoaderAsync` interface inherits from `ISceneLoader`.
The `TAsyncScene` type should return a `Scene` instance, and can be anything you mean to `await`, for example, `Task<Scene>`, `ValueTask<Scene>` or `UniTask<Scene>`, while the `TAsyncSceneArray` should return a `Scene[]` instance, such as `Task<Scene[]>`, `ValueTask<Scene[]>` or `UniTask<Scene[]>`.

## Scene Loader Implementations

The package comes with **one** base implementations and **two** wrappers:
* The `SceneLoaderAsync`, that just like the `ISceneManager` implementations, will return `ValueTask` values.
* The `SceneLoaderCoroutine`, that uses the `SceneLoaderAsync` but returns a `WaitTask` that can be used in coroutines.
* The `SceneLoaderUniTask`, that uses the `SceneLoaderAsync` but returns `UniTask` values.

All of them have interfaces to simplify your code:

```cs
public interface ISceneLoaderCoroutine : ISceneLoaderAsync<WaitTask<Scene>, WaitTask<Scene[]>> { }

public interface ISceneLoaderAsync : ISceneLoaderAsync<ValueTask<Scene>, ValueTask<Scene[]>> { }

public interface ISceneLoaderUniTask : ISceneLoaderAsync<UniTask<Scene>, UniTask<Scene[]>> { }
```

The `Manager` property can be used to listen to the `SceneLoaded`, `SceneUnloaded`, and `ActiveSceneChanged` events.
Both `LoadSceneAsync` and `UnloadSceneAsync` methods will simply call the `ISceneManager` equivalents, while the `LoadScene` and `UnloadScene` will do the same but without `await`.
It's important to understand that `LoadScene`, `UnloadScene`, and `TransitionToScene` will still invoke asynchronous operations, instead of blocking the execution until they are done.
You can use the `ISceneManager` events to react to the completion of those methods.