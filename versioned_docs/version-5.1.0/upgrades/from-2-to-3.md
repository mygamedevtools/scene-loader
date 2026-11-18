---
sidebar_position: 1
title: From 2.x to 3.x
description: Upgrade from version 2.x to 3.x
---

# Upgrading from 2.x to 3.x
The `3.x` update has **unified** the addressable and non-addressable scene managers in a **single implementation**. This introduced a few small breaking changes to the `ISceneLoader`, `ISceneManager` and `ILoadSceneInfo` interfaces.
It also changed how the scene loader implementations work, specially the `SceneLoaderCoroutine` that has a different return type.

## Key Changes

* Merged `SceneManager` and `SceneManagerAddressable` into `AdvancedSceneManager`.
* Changed the `ISceneLoaderCoroutine` and `SceneLoaderCoroutine` return types for scene operations.
* Changed scene loader implementations to be immutable `readonly struct`.
* Added the `ISceneData` and `IAsyncSceneOperation` to handle the complexity between addressable and non-addressable scene operations.
* Added tests to assert addressable scene operations using `AssetReference`.
* Fixed not being able to transition directly to a scene if no loading scene provided.

## Scene Manager Changes

### `ISceneManager` Interface

The former `SceneCount` property has been split into two properties: `LoadedSceneCount` (count of scenes that are loaded) and `TotalSceneCount` (loaded + unloading scene count).

```diff
-    int SceneCount { get; }
+    int LoadedSceneCount { get; }
+    int TotalSceneCount { get; }
```

### Advanced Scene Manager

The `AdvancedSceneManager` combines the former `SceneManager` and `SceneManagerAddressable` implementation with the use of `ISceneData` to handle the complexity between addressable and non-addressable scene operations internally.

```diff
-ISceneManager sceneManager = new SceneManager();
-ISceneManager sceneManagerAddressable = new SceneManagerAddressable();
+ISceneManager sceneManager = new AdvancedSceneManager();
```

#### Constructors

You have additional options when creating an `AdvancedSceneManager`. You can choose to include all currently loaded scenes in its initialization, or to include a set of scenes that you want it to manage.

```cs
// Standard, empty constructor
ISceneManager emptyManager = new AdvancedSceneManager();

// Initialize with all loaded scenes
ISceneManager initializedManager = new AdvancedSceneManager(addLoadedScenes: true);

// Initialize with the scenes you want to include
ISceneManager customSceneManager = new AdvancedSceneManager(initializationScenes: mySceneArray);
```

### `ISceneManagerReporter` interface

This interface was used internally by the test assembly and has been **removed**. The overall test structure have been updated and no longer requires this interface to run.

## Scene Loader Changes

### `ISceneLoader` interface

With the addition of currently loaded scenes into the `AdvancedSceneManager` constructor, and the fix to direct scene transitions, the `externalOriginScene` parameter of the transition methods have been removed.

```diff
-void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default);
+void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null);

-void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default);
+void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);
```

### `ISceneLoaderAsync` interface

Also removed the `externalOriginScene` parameter from the transition methods.

```diff
-TAsyncSceneArray TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = default, Scene externalOriginScene = default);
+TAsyncSceneArray TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = default);

-TAsyncScene TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = default, Scene externalOriginScene = default);
+TAsyncScene TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = default);
```

### `ISceneLoaderCoroutine` interface

Changed the return type from `Coroutine` to `WaitTask<Scene>` and `WaitTask<Scene[]>`.
The `WaitTask` is a more flexible type that can be used inside coroutines with `yield return`, can return values and throw exceptions.
It removed the need for the `RoutineBehavior`, that was also removed.

```diff
-public interface ISceneLoaderCoroutine : ISceneLoaderAsync<Coroutine, Coroutine> { }
+public interface ISceneLoaderCoroutine : ISceneLoaderAsync<WaitTask<Scene>, WaitTask<Scene[]>> { }
```

To wait coroutine operations, you can simply use `yield return` from inside a coroutine:

```cs
public Coroutine TransitionToSceneAndExecute()
{
    return StartCoroutine(transitionToSceneAndExecuteRoutine());

    IEnumerator transitionToSceneAndExecuteRoutine()
    {
        yield return sceneLoaderCoroutine.TransitionToSceneAsync(targetSceneInfo, loadingSceneInfo);
        // Execute custom logic after the transition
    }
}
```

### Scene Loader implementations

Since all scene loaders are just `ISceneLoaderAsync` implementations with different return types, they have been simplified to rely on a single implementation.
The `SceneLoaderCoroutine` and `SceneLoaderUniTask` now use an internal instance of a `SceneLoaderAsync`.
This will _likely_ change in a future major version to better unify the return types.

#### `readonly struct`

As the scene loaders do not have any state and only operate their `readonly ISceneManager` reference, they have been converted to `readonly struct`.
That does not change how you use the scene loaders.

:::info[Important]
It is recommended that you only save a reference to the `ISceneLoader` or `ISceneLoaderAsync` interface instead in your systems of the implemented type such as `SceneLoaderAsync`.
:::

## Load Scene Info Changes

To unify the addressable and non-addressable workflows, the `ISceneInfo` had a few changes to reflect the expected behavior.
The `IsReferenceToScene` method has been refactored to `CanBeReferenceToScene` because:

1. Only the `LoadSceneInfoScene` directly references the loaded scene.
2. The `LoadSceneInfoName` and `LoadSceneInfoIndex` can reference a loaded scene, but cannot be a hard link if there are multiple loaded scenes with the same name or build index.
3. Addressable `ILoadSceneInfo` types can be linked to their loaded scene through their `AsyncOperationHandle`, not accessible through the `ILoadSceneInfo` interface.

### `LoadSceneInfoType` enum

The `LoadSceneInfoType` enum has been created to simplify the interpretation of an `ILoadSceneInfo`, allowing the replacement of mutiple object casts in the worst case scenario to just the cast of the `Reference` value.
If you need to work with custom implementations of `ISceneManager` and `ILoadSceneInfo`, you can use the `LoadSceneInfoType.Other` value.

### `LoadSceneInfoAddress`

The `LoadSceneInfoAddress` implementation has been added for addressable scene operations.
In the previous version, you could pass a `LoadSceneInfoName` with the address of the scene to the `SceneManagerAddressable`.
This is **no longer supported**.
You should use the `LoadSceneInfoAddress` to load a scene by its address in the `AdvancedSceneManager`.
The `LoadSceneInfoName` can **only** be used to load a scene by its name that has been added to the build settings.

## New content

The unification of addressable and non-addressable workflows was made possible by the addition of `ISceneData` and `IAsyncSceneOperation`, that aid in handling the complexity of these two workflows.
These new additions are only used internally by the `AdvancedSceneManager` and do not ever require you to interact with it, unless you need to build a custom `ISceneManager` implementation.

### `IAsyncSceneOperation`

This interface is used to hold either an [AsyncOperation](https://docs.unity3d.com/ScriptReference/AsyncOperation.html) (non-addressable) or an [AsyncOperationHandle](https://docs.unity3d.com/Packages/com.unity.addressables@2.1/manual/AddressableAssetsAsyncOperationHandle.html) (addressable) reference.

```cs
public interface IAsyncSceneOperation
{
    float Progress { get; }

    bool IsDone { get; }

    bool HasDirectReferenceToScene { get; }

    Scene GetResult();
}
```

The `HasDirectReferenceToScene` property determines if this `IAsyncSceneOperation` can be used to link a loaded scene with its `ILoadSceneInfo`.
It is `true` for addressable operations and `false` otherwise.
This also suggests if the `GetResult()` method can return a valid `Scene`.

### `ISceneData`

This interface is used to hold a reference of an `ILoadSceneInfo`, its `IAsyncSceneOperation`, and its loaded `Scene`.
It can trigger the loading and unloading of scenes.
The `AdvancedSceneManager` relies heavily on `ISceneData` to control the loaded scenes and how the load and unload modify the overall scene state.

```cs
public interface ISceneData
{
    IAsyncSceneOperation AsyncOperation { get; }

    ILoadSceneInfo LoadSceneInfo { get; }

    Scene SceneReference { get; }

    void SetSceneReferenceManually(Scene scene);

    void UpdateSceneReference();

    IAsyncSceneOperation LoadSceneAsync();

    IAsyncSceneOperation UnloadSceneAsync();
}
```

## Conclusion

In the transition from `1.x` to `2.x` there was the unification of non-addressable and addressable `ILoadSceneInfo` implementations.
Now we've also unified the `ISceneManager` implementations.
It's very likely to expect that the current `ISceneLoaderAsync` implementation should also change in the near future, to improve even further the user experience. We hope these changes improve the package's reliablity and make it more friendly to new users.