![License](https://img.shields.io/github/license/joaoborks/myunitytools-sceneloader)
![Release](https://img.shields.io/github/v/release/joaoborks/myunitytools-sceneloader?sort=semver)
![Last Commit](https://img.shields.io/github/last-commit/joaoborks/myunitytools-sceneloader)

My Unity Tools - Scene Loading
===

_A package that standardizes the scene loading process among many different possibilities, including support for Coroutines, C# Tasks, UniTask and Addressables._

Installation
---

#### - For 2019.1+: [Installing from a git URL](https://docs.unity3d.com/Manual/upm-ui-giturl.html) _(requires [Git](https://git-scm.com/) installed and added to the PATH)_
You can open the Package Manager and then click on the `+` button on the top left corner. 
From there select `Add package from git URL...`, type `https://github.com/joaoborks/myunitytools-sceneloader.git` and click `Add`. 
The package will be imported by the Package Manager.

#### - Other Package Manager supported versions: Add manually to manifest
You should add this to your `manifest.json` under the `Packages` folder on the root of your Unity Project:
```
{
  "dependencies": {
	  "com.myunitytools.sceneloader": "https://github.com/joaoborks/myunitytools-sceneloader.git"
  }
}
```

Overview
---

Loading scenes in Unity is very simple, but developing games sometimes require more flexible implementations. This package aims to simplify common use cases for scene loading.
Additionally, it offers support for [Unity Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html) and for [UniTask](https://github.com/Cysharp/UniTask) with no additional setup required.

Aside from the ordinary **Load** and **Unload** actions, the Scene Loading tools introduce the **Transition** as a new standard to control transitions between scenes with an optional intermediate "loading scene" in between.

:information_source: You don't need to understand what **Addressables** or **UniTask** do in order to use this package. There are scene loaders that only rely on basic Unity Engine functionalities.

Usage
---

Loading scenes with this package implies that the scenes **will always be loaded as Additive**. That is simply because there is no advantage in loading scenes in the **Single** load scene mode when you expect to work with multiple scenes. There are **six** scene loaders that you can use, depending on your project necessities:
1. `SceneLoaderAsync`: simple scene loader with `awaitable` instructions.
2. `SceneLoaderCoroutine`: simple scene loader with `Coroutine` instructions.
3. `SceneLoaderUniTask`: the `SceneLoaderAsync` with `UniTask` instead of `Task`.
4. `AddressableSceneLoaderAsync`: a scene loader that handles Addressable scenes with `awaitable` instructions.
5. `AddressableSceneLoaderCoroutine`: a scene loader that handles Addressable scenes with `Coroutine` instructions.
6. `AddressableSceneLoaderUniTask`: the `AddressableSceneLoaderAsync` with `UniTask` instead of `Task`.

Before we go into each one, let's understand how we got there. There are some core differences between loading scenes with the `SceneManager` and via Addressables.

x | SceneManager | Addressables
 --- | --- | ---
Scene Reference | Build Index, Scene Name or Path | Asset Reference, Address Runtime Key
Active Scene | Managed through `SceneManager` | None
Loaded Scenes | Managed through `SceneManager` | No high level API available

Due to those differences, the Addressable scene loaders had to be split into their own logic. However, in order to simplify the usability, even though the Addressable does not work entirely with the `SceneManager`, the way you interact with its scene loaders is very similar to how you would interact with the others.

### The Scene Loader

The most basic usability you'll get from a scene loader is to **Load** a scene, **Unload** it, or **Transition** to another scene. That's what the `ISceneLoader` interface will define:

```cs
public interface ISceneLoader
{
  void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);

  void UnloadScene(ILoadSceneInfo sceneInfo);

  void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false);
}
```

Observe that instead of defining `int` or `string` parameters for the scene's build index or name, it defines the `ILoadSceneInfo` interface instead. That is just a way of standardizing the scene information that we'll be working with, instead of creating multiple methods, each with their own implementation.

The `ILoadSceneInfo` defines:

```cs
public interface ILoadSceneInfo
{
  AsyncOperation UnloadSceneAsync();

  AsyncOperation LoadSceneAsync();

  Scene GetScene();
}
```

Then you'll be able to use whatever fits your use case: the `LoadSceneInfoIndex` or the `LoadSceneInfoName` which you can create just as you would expect:

```cs
// Create an ILoadSceneInfo by the scene's build index:
ILoadSceneInfo sceneInfo = new LoadSceneInfoIndex(1);

// Create an ILoadSceneInfo by the scene's name:
ILoadSceneInfo sceneInfo = new LoadSceneInfoName("MainMenu");
```

As a final example, let's suppose you're currently at the **"Main Menu"** scene and you want to transition to the **"Level 1"** scene with the **"Loading Tips"** loading scene. You could for example:

```cs
sceneLoader.TransitionToScene(new LoadSceneInfoName("Level 1"), new LoadSceneInfoName("Loading Tips"));
```

This would trigger the scene transition by loading the **"Loading Tips"** scene first, and then starting to load the **"Level 1"** scene while showing its load progress in the loading scene. Then, when the **"Level 1"** scene is done loading, the **"Loading Tips"** scene will get unloaded and the transition will be complete.

Now, what if you wanted to _await_ this call?

### Awaitable Scene Loaders

You have three options of _awaitable_ scene loaders to choose: **Coroutine**, **C# Task (Async)** and **UniTask**. Coroutine is not C# awaitable, but it works similarly if you `yield return` it inside another Coroutine. If you want to use **UniTask**, make sure you [install its package](https://github.com/Cysharp/UniTask#upm-package) in your project.

Since each implementation changes what you'll get as the return types, they all have their own interfaces:

```cs
public interface ISceneLoaderAsync : ISceneLoader
{
  Task TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);

  Task UnloadSceneAsync(ILoadSceneInfo sceneInfo);

  Task LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false);
}

public interface ISceneLoaderCoroutine : ISceneLoader
{
  Coroutine TransitionToSceneRoutine(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);

  Coroutine UnloadSceneRoutine(ILoadSceneInfo sceneInfo);

  Coroutine LoadSceneRoutine(ILoadSceneInfo sceneInfo, bool setActive = false);
}

public interface ISceneLoaderUniTask : ISceneLoader
{
  UniTask TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);

  UniTask UnloadSceneAsync(ILoadSceneInfo sceneInfo);

  UniTask LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false);
}
```

In the end however, they all also implement `ISceneLoader`, so even though you'll only use the basic `ISceneLoader` methods, you can still take advantage of the `UniTask` implementation, for example. Otherwise, if you're going to await the operation, then you can choose the system that fits you best.

### Addressable Scene Loading

The Addressable Scene Loading introduces a few more concepts in order to keep the simplicity of the regular scene loading process. Take for example the `IAddressableSceneLoader` interface:

```cs
public interface IAddressableSceneLoader
{
  IAddressableSceneManager SceneManager { get; }

  void TransitionToScene(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference = null);

  void UnloadScene(IAddressableLoadSceneInfo sceneInfo);

  void LoadScene(IAddressableLoadSceneReference sceneReference, bool setActive = false);
}
```

You can see that it has two main differences from the `ISceneLoader`: the presence of the `IAddressableSceneManager` and the different parameter types. Other than that, the usability is exactly the same.

Instead of a single `ILoadSceneInfo` parameter type, the Addressable implementation has two different interfaces: the `IAddressableLoadSceneReference` and the `IAddressableLoadSceneInfo`. This is due to nature of the **Load** and **Unload** scene operations of the Addressables System, in which you need different parameters for each operation.

Take a look for example in the definition of the `IAddressableLoadSceneReference`:

```cs
public interface IAddressableLoadSceneReference
{
    AsyncOperationHandle<SceneInstance> LoadSceneAsync(IAddressableSceneManager sceneManager);
}
```

It only defines the **Load** operation that returns an `AsyncOperationHandle<SceneInstance>`. To create an object that defines this interface, you can either use the scene's `AssetReference` or its runtime key, which is how you normally reference assets with Addressables.

```cs
// Create an IAddressableLoadSceneReference by the scene's AssetReference:
IAddressableLoadSceneReference sceneReference = new AddressableLoadSceneReferenceAsset(sceneAssetReference);

// Create an IAddressableLoadSceneReference by the scene's runtime key:
IAddressableLoadSceneReference sceneReference = new AddressableLoadSceneReferenceKey("MainMenu");
```

Now, when unloading scenes, you'll use the `IAddressableLoadSceneInfo` interface:

```cs
public interface IAddressableLoadSceneInfo
{
  AsyncOperationHandle<SceneInstance> UnloadSceneAsync(IAddressableSceneManager sceneManager, bool autoReleaseHandle = true);
}
```

Although very similar to the **Load** operation, it's important to note here that you'll need different scene information in order to create an object that implements this method. You'll need either the `AsyncOperationHandle<SceneInstance>` that was used to load the scene, or the `SceneInstance` itself, or in last case the loaded scene name.

```cs
// Create an IAddressableLoadSceneInfo by the scene's AsyncOperationHandle<SceneInstance>:
IAddressableLoadSceneInfo sceneInfo = new AddressableLoadSceneInfoOperationHandle(sceneOperationHandle);

// Create an IAddressableLoadSceneInfo by the scene's SceneInstance:
IAddressableLoadSceneInfo sceneInfo = new AddressableLoadSceneInfoInstance(sceneInstance);

// Create an IAddressableLoadSceneInfo by the scene's name:
IAddressableLoadSceneInfo sceneInfo = new AddressableLoadSceneInfoName("Main Menu");
```

As you could see, both `IAddressableLoadSceneReference` and `IAddressableLoadSceneInfo` require the `IAddressableSceneManager` as a parameter of their methods.

### The Addressable Scene Manager

Loading scenes through Addressables does not exactly pass through the `SceneManager`. It does fire callbacks for loading and unloading scenes, but there's no way to get the active scene or to get any of the loaded scenes, if they have been loaded through the Addressables System. For that reason, the `IAddressableSceneManager` defines a standard for an Addressable Scene Manager that keeps track of the loaded scenes and of an active scene as well, just like you'd expect with the regular `SceneManager`.

It's only going to be used internally by the scene loaders, but take a look at its definition:

```cs
public interface IAddressableSceneManager
{
  void SetActiveSceneHandle(AsyncOperationHandle<SceneInstance> sceneHandle);

  AsyncOperationHandle<SceneInstance> GetActiveSceneHandle();

  AsyncOperationHandle<SceneInstance> LoadSceneAsync(AssetReference sceneReference);
  AsyncOperationHandle<SceneInstance> LoadSceneAsync(string runtimeKey);

  AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle, bool autoReleaseHandle = true);
  AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle = true);
  AsyncOperationHandle<SceneInstance> UnloadSceneAsync(string sceneName, bool autoReleaseHandle = true);

  AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(SceneInstance sceneInstance);
  AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(string sceneName);
}
```

Not only it provides methods for loading and unloading scenes with the many ways to reference them, but it also keeps track of the loaded scenes and manages the current active scene. The `IAddressableLoadSceneReference` and `IAddressableLoadSceneInfo` implementations require the scene manager for calling these methods, just like in the non-addressable implementations, they also do it but statically via the `SceneManager`.

Now we can load addressable scenes, but what about _awaiting_ them too?

### Awaitable Addressables

The Addressables System has much better support for awaiting operations than the other Unity Engine systems. While it can be easier to implement, we still need to adapt it to work with the defined standards in the previous topics. So, not so different from the non-addressable implementation, we have the three options for awaitable scene loaders:

```cs
public interface IAddressableSceneLoaderAsync : IAddressableSceneLoader
{
  Task<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference = null);

  Task<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive = false);

  Task UnloadSceneAsync(IAddressableLoadSceneInfo sceneInfo);
}

public interface IAddressableSceneLoaderCoroutine : IAddressableSceneLoader
{
  Coroutine TransitionToSceneRoutine(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference = null);

  Coroutine LoadSceneRoutine(IAddressableLoadSceneReference sceneReference, bool setActive = false);

  Coroutine UnloadSceneRoutine(IAddressableLoadSceneInfo sceneInfo);
}

public interface IAddressableSceneLoaderUniTask : IAddressableSceneLoader
{
  UniTask<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference = null);

  UniTask<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive = false);

  UniTask UnloadSceneAsync(IAddressableLoadSceneInfo sceneInfo);
}
```

Just like before, they also implement the `IAddressableSceneLoader` so you can use what you prefer.

### Why so many interfaces?

The idea behind the interfaces is first to decouple things and second to allow you to build your own systems if you require something very different from the provided content. Sometimes projects require very specific implementations, and instead of making the system extremely complex and detailed, I'd rather have it broken into many different pieces that you can replace to fit with whatever works best in each use case.

I am always open to suggestions, so please if you have any, don't hesistate to share!

Samples
---

This package offers samples with each of the scene loaders for you to use as a starting point. To use them, simply import the desired sample through the Package Manager.

### For non-Addressable scene loaders:

Make sure you add all scenes to the build settings with the following indexes:

0. SceneA
1. SceneB
2. SceneLoading
3. SceneC_add
4. SceneD_add

You can try out the sample by loading either the SceneA or SceneB and hitting play in the Unity Editor.

### For Addressable scene loaders:

Make sure you mark all scenes as addressables and simplify their names in the Addressable Groups window. To test, open up the **Bootstrap** scene and hit play.

Check if your Addressables Play Mode Script is `Use Asset Database`, otherwise you may be required to build the Addressable groups.

---

Don't hesitate to create [issues](https://github.com/joaoborks/myunitytools-sceneloader/issues) for suggestions and bugs. Have fun!
