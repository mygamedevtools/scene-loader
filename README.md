<h1 align=center>
Advanced Scene Manager
</h1>

<p align=center>
  <a href=""><img src="https://img.shields.io/badge/Unity-2021.3+-57b9d3.svg?color=5189bd&logo=unity" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/github/license/mygamedevtools/scene-loader?color=5189bd" /></a>
  <a href="https://github.com/mygamedevtools/scene-loader/releases/latest"><img src="https://img.shields.io/github/v/release/mygamedevtools/scene-loader?color=5189bd&sort=semver" /></a>
  <a href="https://openupm.com/packages/com.mygamedevtools.scene-loader/"><img src="https://img.shields.io/npm/v/com.mygamedevtools.scene-loader?color=5189bd&label=openupm&registry_uri=https://package.openupm.com" /></a>
  <a href="https://openupm.com/packages/com.mygamedevtools.scene-loader/"><img src="https://img.shields.io/badge/dynamic/json?color=5189bd&label=downloads&query=%24.downloads&suffix=%2Fmonth&url=https%3A%2F%2Fpackage.openupm.com%2Fdownloads%2Fpoint%2Flast-month%2Fcom.mygamedevtools.scene-loader" /></a>
</p>

<p align=center>
  <a href="https://codecov.io/github/mygamedevtools/scene-loader"><img src="https://codecov.io/github/mygamedevtools/scene-loader/branch/main/graph/badge.svg?token=J4ISVSF390" /></a>
  <a href="https://github.com/mygamedevtools/scene-loader/actions/workflows/test.yml"><img src="https://github.com/mygamedevtools/scene-loader/actions/workflows/test.yml/badge.svg" /></a>
  <a href="https://github.com/mygamedevtools/scene-loader/actions/workflows/release.yml"><img src="https://github.com/mygamedevtools/scene-loader/actions/workflows/release.yml/badge.svg" /></a>
  <a href="https://github.com/semantic-release/semantic-release"><img src="https://img.shields.io/badge/semantic--release-angular-e10079?logo=semantic-release" /></a>
</p>

<p align=center><i>
Enhance your scene loading experience
</i></p>

## Overview

This is a Unity package to **simplify** scene operations: **load**, **unload** and **transition**. In a quick example:

```cs
// Unity Manager scene transition
yield return SceneManager.LoadSceneAsync("my-loading-scene", LoadSceneMode.Additive);
yield return SceneManager.LoadSceneAsync("my-target-scene", LoadSceneMode.Additive);
SceneManager.SetActiveScene(SceneManager.GetSceneByName("my-target-scene"));
SceneManager.UnloadSceneAsync("my-loading-scene");
SceneManager.UnloadSceneAsync("my-previous-scene");

// Advanced Scene Management scene transition
sceneLoader.TransitionToScene(new LoadSceneInfoName("my-target-scene"), new LoadSceneInfoName("my-loading-scene"));
```

You can also take advantage of these features:

- **Unified** API for **addressable** and **non-addressable** scenes.
- **Awaitable** scene operations.
- **Modular** implementation with interfaces.
- Load, unload or transition to **multiple scenes**.

## Summary

* [Installation](#installation)
  * [OpenUPM](#openupm)
  * [Installing from Git](#installing-from-git)
  * [Installing from tarball](#installing-from-tarball)
* [Dependencies](#dependencies)
* [Description](#description)
* [Usage](#usage)
  * [The Scene Manager](#the-scene-manager)
  * [Load Scene Info](#load-scene-info)
  * [The Scene Loaders](#the-scene-loaders)
  * [Disposable and CancellationTokens](#disposable-and-cancellationtokens)
* [Practical examples](#practical-examples)
  * [Creating your scene loader](#creating-your-scene-loader)
  * [Loading scenes with load scene info](#loading-scenes-with-load-scene-info)
* [Creating Loading Screens](#creating-loading-screens)
  * [The Loading Behavior](#the-loading-behavior)
  * [The Loading States](#the-loading-states)
  * [The Loading Feedback](#the-loading-feedback)
  * [Loading Screen Example](#loading-screen-example)
* [Why so many interfaces?](#why-so-many-interfaces)
* [Tests](#tests)

## Installation

### Open UPM

This package is available on the [OpenUPM](https://openupm.com/packages/com.mygamedevtools.scene-loader) registry. Add the package via the [openupm-cli](https://github.com/openupm/openupm-cli):

```
openupm add com.mygamedevtools.scene-loader
```

### [Installing](https://docs.unity3d.com/Manual/upm-ui-giturl.html) from Git

> [!NOTE]
> Requires [Git](https://git-scm.com/) installed and added to the PATH

1. Open `Window/Package Manager`.
2. Click <kbd>+</kbd>.
3. Select `Install package from git URL...`.
4. Paste `https://github.com/mygamedevtools/scene-loader.git#upm` into url.
5. Click `Add`.

### [Installing](https://docs.unity3d.com/Manual/upm-ui-tarball.html) from tarball

1. Choose the [release](https://github.com/mygamedevtools/scene-loader/releases) you want to install and download the `com.mygamedevtools.scene-loader-<release>.tgz` asset.
2. Open `Window/Package Manager`.
3. Click <kbd>+</kbd>.
4. Select `Install package from tarball...`.
5. Select the `com.mygamedevtools.scene-loader-<release>.tgz` file you downloaded.

## Dependencies

The package has **no dependencies** but supports integration with some packages.
If you wish to use it with `Addressables`, `UniTask`, or `TextMeshPro`, make sure you install the packages:

* `com.unity.addressables` >= 1.19.0
* `com.unity.textmeshpro` >= 2.2.0
* `com.cysharp.unitask`* >= 2.0.0

_*Installed via UPM or OpenUPM. Check the [package documentation](https://github.com/Cysharp/UniTask) for more details._

[_[back to top]_](#advanced-scene-manager)

## Description

Loading scenes in Unity is very simple, mostly, but when you start to deal with other systems such as [Unity Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html), it can get messy. Also, there are some common scene load scenarios that you'd usually reimplement in every project, like scene transitions.

In this package, you'll have the possibility to standardize the scene loading process between the standard **Unity Scene Manager** and **Addressables**, while still being able to choose how to await (if you want) the operations, be it Coroutines, standard Async (through ValueTasks) or [UniTask](https://github.com/Cysharp/UniTask).

Aside from the ordinary **Load** and **Unload** actions, the Scene Loading tools introduce the **Transition** as a new standard to control transitions between scenes with an optional intermediate "loading scene" in between. Also, starting from version `2.2` you can **Load**, **Unload**, and **Transition** to **multiple scenes** in parallel!

[_[back to top]_](#advanced-scene-manager)

## Usage

Loading scenes with this package implies that the scenes **will always be loaded as Additive**. That is simply because there is no advantage in loading scenes in the **Single** load scene mode when you expect to work with multiple scenes. 

To standardize how the scenes are loaded, you'll be using `ISceneLoader`, `ISceneManager`, and `ILoadSceneInfo` objects.

```mermaid
flowchart BT
  sm([Scene Manager])
  sl([Scene Loader])
  lsi([Load Scene Info])

  lsi -->|Load| sl
  lsi -->|Unload| sl
  lsi -->|Transition| sl
  sl -->|Load| sm
  sl -->|Unload| sm
```

These structures are meant to be used together. If you do not plan to use scene transitions or to have custom _awaitable_ types, you don't need to use the `ISceneLoader`.

### The Scene Manager

The `ISceneManager` interface exposes a few methods and events to standardize the scene load operations:

```cs
public interface ISceneManager : IDisposable
{
  event Action<Scene, Scene> ActiveSceneChanged;
  event Action<Scene> SceneUnloaded;
  event Action<Scene> SceneLoaded;

  int LoadedSceneCount { get; }
  int TotalSceneCount { get; }

  void SetActiveScene(Scene scene);

  ValueTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1, IProgress<float> progress = null, CancellationToken token = default);

  ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null, CancellationToken token = default);

  ValueTask<Scene[]> UnloadSceneAsync(ILoadSceneInfo[] sceneInfos, CancellationToken token = default);

  ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo, CancellationToken token = default);

  Scene GetActiveScene();

  Scene GetLoadedSceneAt(int index);

  Scene GetLastLoadedScene();

  Scene GetLoadedSceneByName(string name);
}
```

You can find many similarities between Unity's [SceneManager](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.html) class, and that's both for maintaining an easy learning curve as well as because some of these operations will end up calling the _Unity Scene Manager_ internally (like `SetActiveScene` for instance).
The `ILoadSceneInfo` interface is also showing up there, but we will get to that in a moment.

The package includes the `AdvancedSceneManager` that is capable of handling both **addressable** and **non-addressable** scene operations. You can use its implementation as a reference to **build your own** Scene Manager if you need.

The `AdvancedSceneManager` is expected to be used as a wrapper for the Unity `SceneManager`. When creating an `AdvancedSceneManager` you can decide whether you want it to manage scenes that have been loaded already or not.

```mermaid
flowchart TB
    usm([Unity Scene Manager])

    usm --> asm([Advanced Scene Manager])
    asm --> s_a["Scene [0]"]
    asm --> s_b["Scene [1]"]
    asm --> s_n["Scene [n]"]
```

The `ISceneManager` interface defines that both `LoadSceneAsync` and `UnloadSceneAsync` methods return a `ValueTask<Scene>`.
This means you can _await_ those methods if they are implemented with the _async_ keyword, or you can also subscribe to the `SceneLoaded` or `SceneUnloaded` events to receive the same `Scene` you would via the _async_ methods.

Both these methods also receive an `ILoadSceneInfo` object.
So, instead of having multiple methods for receiving the scene's build index or the scene's name, we simply have an object instead.

Alternatively, you can also use the `LoadScenesAsync` and `UnloadScenesAsync` methods, to perform the operations on multiple scenes in parallel. These will return a `ValueTask<Scene[]>`.

You can create an `AdvancedSceneManager` using three constructors:

```cs
// Creates an advanced scene manager including all currently loaded scenes. Useful for most cases.
new AdvancedSceneManager(addLoadedScenes: true);

// Creates an empty advanced scene manager. Useful if you are doing this before any scene loads or in a bootstrap scene.
new AdvancedSceneManager();

// Creates an advanced scene manager including an array of scenes. Useful when you want to include only a specific set of scenes to it.
new AdvancedSceneManager(initializationScenes: new Scene[]);
```

[_[back to top]_](#advanced-scene-manager)

### Load Scene Info

As its name states, it holds a reference to a scene to be loaded (or unloaded) and validates whether they _can_ reference a loaded scene.

The `ILoadSceneInfo` interface simply defines:

```cs
public interface ILoadSceneInfo
{
  LoadSceneInfoType Type { get; }

  object Reference { get; }

  bool CanBeReferenceToScene(Scene scene);
}
```

Since the `Reference` field can hold any type of reference, the scene manager will be responsible for deciding what to do with its value.
The `LoadSceneInfoType` is a simple enum that helps converting the `Reference` value without casting the `ILoadSceneInfo` object.
The load scene info simply holds these references, and that's why the implementations included with the package are all **readonly structs**.

You can choose to work with **five** load scene infos:

```mermaid
flowchart
  subgraph Addressable
    direction TB
    assetref(LoadSceneInfoAssetReference)
    address(LoadSceneInfoAddress)
  end

  subgraph Unload only
    scene(LoadSceneInfoScene)
  end
  
  subgraph Non-Addressable
    direction TB
    name(LoadSceneInfoName)
    index(LoadSceneInfoIndex)
  end
```

* The `LoadSceneInfoName`, referencing a scene's name.
* The `LoadSceneInfoIndex`, referencing a scene's build index.
* The `LoadSceneInfoScene`, referencing a loaded scene's struct (used for unloading scenes only).
* The `LoadSceneInfoAssetReference`, referencing a scene's Addressable Asset Reference.
* The `LoadSceneInfoAddress`, referencing a scene's Addressable Address.

You can also build your own `ILoadSceneInfo` implementation if have special needs, but that will probably require you to build a scene manager to interpret its `Reference` value as well.

When **unloading** a scene, the `AdvancedSceneManager` will look for any of its loaded scenes that (in order of priority):
1. Have the same loaded scene handle (in the case of `LoadSceneInfoScene`).
2. Have the same `ILoadSceneInfo`.

That means that the **preferable** way to unload scenes is through `LoadSceneInfoScene`, as it holds a **direct reference** to the target scene.
Assuming you don't have multiple scenes loaded with the same reference, it's safe to assume that the scene you want to unload is the one with the same `ILoadSceneInfo` you provided.
Except from `ILoadSceneInfoScene`, you **cannot** unload a scene with a different `ILoadSceneInfo` type.

> [!NOTE]
> If you do have multiple scenes loaded by the same reference, unloading by its `ILoadSceneInfo` will unload the last loaded scene of that reference.

> [!IMPORTANT]
> When unloading addressable scenes, their resources will be released by calling `Addressables.UnloadSceneAsync` internally.

[_[back to top]_](#advanced-scene-manager)

### The Scene Loaders

The scene loader is an interface that you will use to load scenes in your game, as it works like a wrapper layer to the scene manager, but adds the **Scene Transition** operations.
There are two base interfaces for scene loaders: one with a reference to the `ISceneManager` that will be used, and an `async` interface to be able to `await` the load operations.

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

The **Transition** is a combination of load and unload operations to effectively perform scene transitions, with or without an intermediate scene. For example, usually, if you'd want to go from scene A to scene B you would:

1. Load the scene B.
2. Unload the scene A.

That's only two operations, right?
What if you wanted to have a loading screen as well?
In this case, you would:

1. Load the loading scene.
2. Load the scene B.
4. Unload the scene A.
3. Unload the loading scene.

That's four operations now.
The `TransitionToScene` and `TransitionToSceneAsync` methods let you only provide where you want to go from the currently active scene and if you want an intermediary scene (loading scene for example).

[_[back to top]_](#advanced-scene-manager)

### Disposable and CancellationTokens

Both the `ISceneManager` and the `ISceneLoader` interfaces implement `IDisposable`, meaning that the Scene Managers and Loaders should implement the `Dispose()` method.
This is used with the `CancellationToken` parameters in `ISceneManager` methods to ensure that it will clear its internal data and stop async code execution during disposal.
Note that even when its methods get canceled by the `CancellationToken`, the Unity Scene Manager methods are not cancellable and therefore will continue to operate when called.

The disposal of the implemented Scene Managers will clear its data and stop any running logic.
This is useful for shutting down the application, for example.
If you are going to manually dispose of your scene loaders or managers, prefer the following scenarios:
* You can ensure that there are no load/unload/transition operations in progress.
* You are quitting/shutting down the application or an application module.

> [!WARNING]
> It's **not recommended** to manually cancel the `ISceneManager` operations via its `CancellationToken` parameters.
> It may result in unexpected issues such as unwanted scenes being loaded/unloaded after cancellation.

[_[back to top]_](#advanced-scene-manager)

## Practical Examples

When creating your scene loader, you must first create your scene manager.
Ideally, you will not need to store the scene manager anywhere as it will be accessible through the `ISceneLoader` interface.
Also, you will need to build your scene info objects to hold references to scenes.

### Creating your scene loader

For the first example, let's build a scene manager and a Coroutine scene loader:

```cs
// Make sure to add 'using MyGameDevTools.SceneLoading;' on the top of the script
ISceneManager sceneManager = new AdvancedSceneManager();
ISceneLoader sceneLoader = new SceneLoaderCoroutine(sceneManager);
```

You can also define the scene loader types as their `ISceneLoaderAsync` implementations:

```cs
ISceneManager sceneManager = new SceneManager();

ISceneLoaderCoroutine coroutineSceneLoader = new SceneLoaderCoroutine(sceneManager);
// Or
ISceneLoaderAsync asyncSceneLoader = new SceneLoaderAsync(sceneManager);
// Or
ISceneLoaderUniTask unitaskSceneLoader = new SceneLoaderUniTask(sceneManager);
```

> [!NOTE]
> It is recommended that you store these object references as its interfaces to reduce coupling in your code.

### Loading scenes with load scene info

You'll use the load scene info objects to reference scenes. You can use these objects to define both addressable and non-addressable scenes.

Let's assume you have included the following scenes in your **Build Settings**:

0. Main Menu
1. Loading

And you have the following scenes in **addressable groups**:

* Shop
* Level 1

You can load the scenes by using the appropriate `ILoadSceneInfo`:

```cs
ILoadSceneInfo mainMenuSceneInfo = new LoadSceneInfoName("Main Menu");
ILoadSceneInfo loadingSceneInfo = new LoadSceneInfoIndex(1);
ILoadSceneInfo shopSceneInfo = new LoadSceneInfoAddress("Shop");
// You should be able to get the scene's Asset Reference from the inspector by exposing an
// AssetReference on a MonoBehaviour or ScriptableObject.
ILoadSceneInfo levelSceneInfo = new LoadSceneInfoAssetReference(levelSceneAssetReference);

// Loading the scene calls the same method, regardless of the load scene info type
sceneLoader.LoadScene(mainMenuSceneInfo);
sceneLoader.LoadScene(loadingSceneInfo);
sceneLoader.LoadScene(shopSceneInfo);
sceneLoader.LoadScene(levelSceneInfo);

// Or the async alternatives
await sceneLoader.LoadSceneAsync(mainMenuSceneInfo);
await sceneLoader.LoadSceneAsync(loadingSceneInfo);
await sceneLoader.LoadSceneAsync(shopSceneInfo);
await sceneLoader.LoadSceneAsync(levelSceneInfo);
```

The same logic applies for unloading scenes. Additionally, you can use the `LoadSceneInfoScene` to unload non-addressable scenes using the scene returned from `ISceneLoaderAsync.LoadSceneAsync`.

```cs
ILoadSceneInfo mainMenuSceneInfo = new LoadSceneInfoName("Main Menu");
ILoadSceneInfo loadingSceneInfo = new LoadSceneInfoIndex(1);
ILoadSceneInfo shopSceneInfo = new LoadSceneInfoAddress("Shop");

Scene levelScene = await sceneLoader.LoadSceneAsync(LoadSceneInfoAssetReference(levelSceneAssetReference));
ILoadSceneInfo levelSceneInfo = new LoadSceneInfoScene(levelScene);

sceneLoader.UnloadScene(mainMenuSceneInfo);
sceneLoader.UnloadScene(loadingSceneInfo);
sceneLoader.UnloadScene(shopSceneInfo);
sceneLoader.LoadScene(levelSceneInfo);

// Or the async alternatives
await sceneLoader.UnloadSceneAsync(mainMenuSceneInfo);
await sceneLoader.UnloadSceneAsync(loadingSceneInfo);
await sceneLoader.UnloadSceneAsync(shopSceneInfo);
await sceneLoader.LoadSceneAsync(levelSceneInfo);
```

Instead of using the async method, you can also register to the `ISceneManager.SceneLoaded` event:

```cs
sceneLoader.Manager.SceneLoaded += loadedScene => 
{
  ILoadSceneInfo loadedSceneInfo = new LoadSceneInfoScene(loadedScene);
  sceneLoader.UnloadScene(loadedSceneInfo);
}
```

For transitions, you can combine different load scene info objects on the transition method:

```cs
ILoadSceneInfo stageSceneInfo = new LoadSceneInfoName("Stage 1");
ILoadSceneInfo loadingSceneInfo = new LoadSceneInfoIndex(1);

sceneLoader.TransitionToScene(stageSceneInfo, loadingSceneInfo);

// Or the async alternative
await sceneLoader.TransitionToSceneAsync(stageSceneInfo, loadingSceneInfo);
```

Whenever you need to perform scene operations on multiple scenes, you can also build `ILoadSceneInfo` arrays:

```cs
ILoadSceneInfo[] sceneInfoGroup = new ILoadSceneInfo[]
{
  new LoadSceneInfoName("Main Menu"),
  new LoadSceneInfoIndex(1),
  new LoadSceneInfoAddress("Shop"),
  new LoadSceneInfoAssetReference(levelSceneAssetReference)
}

// Load
sceneLoader.LoadScenes(sceneInfoGroup);
// Unload
sceneLoader.UnloadScenes(sceneInfoGroup);
// Transition
// For multiple scenes, you must provide which scene you want to be the active scene by
// providing its index in the array.
sceneLoader.TransitionToScenes(sceneInfoGroup, 0);

// Awaitable alternatives
await sceneLoader.LoadScenes(sceneInfoGroup);
await sceneLoader.UnloadScenes(sceneInfoGroup);
await sceneLoader.TransitionToScenes(sceneInfoGroup, 0);
```

[_[back to top]_](#advanced-scene-manager)

## Creating Loading Screens

During scene transitions, you have the option to provide an intermediate scene that can be used as loading screen.
This could be an animated splash screen or a loading progress bar, for example.
This package provides implementations to help you build your loading screens faster.

### The Loading Behavior

The Loading Behavior is a [MonoBehaviour] component, which you can attach to Unity [GameObjects], that receives the progress value from the scene manager.
You **need** to add a `LoadingBehavior` component to a [GameObject] in your loading scene to be able to display scene loading feedback.
It exposes its `LoadingProgress` instance, which you can use to listen to the loading events:

```cs
public class LoadingProgress : IProgress<float>
{
  public event LoadingStateChangeDelegate StateChanged;
  public event SceneLoadProgressDelegate Progressed;

  public LoadingState State { get; }
}
```

The `StateChanged` event expects a `LoadingState` parameter, to report the current state of the scene loading operation, and you can query the active state at any time by retrieving the value in the `State` property.
The `Progressed` event expects a `float` parameter, ranging from 0 to 1 to report the progress of the scene loading operation.

Back to the `LoadingBehavior`, it has a few options you can set on the Unity [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html):

* **Wait For Scripted Start**: enable if the loading screen will have a **transition in** effect, such as a fade in.
* **Wait For Scripted End**: enable if the loading screen will have a **transition out** effect, such as a fade out.

### The Loading States

The loading scene transition can be customized to delay some parts of the operation to deliver a smooth visual experience for the user.
That means we can fade in/out or use other transition effects and wait for them to complete to continue the scene loading operations.
The `LoadingState` enum reflects those states:

```cs
public enum LoadingState
{
  WaitingToStart,
  Loading,
  TargetSceneLoaded,
  TransitionComplete
}
```

These states are ordered, which means that the first state will always be `WaitingToStart` and the last will be `TransitionComplete`.
They mean:

* `WaitingToStart`: it's waiting for a trigger to allow the scene loading to start loading. This could be if the loading scene does not instantly appear, otherwise causing weird experiences with things simply disappearing. You can transition the loading screen with a fade in or a similar effect, for example.
* `Loading`: the loading screen transition has occurred and the scene loading operation is running. During this state, the `LoadingProgress` instance will receive the progress value from the scene manager.
* `TargetSceneLoaded`: the target scene has been loaded, but the loading screen is still displaying. You can use this state to transition the loading screen out, such as a fade out or a similar effect.
* `TransitionComplete`: the target scene has been loaded and the loading screen is already out of the way. Shortly after this state, the loading scene will be unloaded.

### The Loading Feedback

At this point, you should already have your loading scene with a `LoadingBehavior` attached to one of your [GameObjects].
Now you can also add some other components to display the loading progress feedback.
This package comes with **three** feedbacks:

* `LoadingFeedbackSlider`: attach on an [UI Slider] to display the loading progress feedback as a progress bar.
* `LoadingFeedbackTextMeshPro`: attach on an [UI Text Mesh Pro] to display the loading progress feedback as text normalized from 0 to 100.
* `LoadingFeedbackText` _(also known as Legacy)_: attach on an [UI Legacy Text](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Text.html) to display the loading progress feedback as text normalized from 0 to 100.

You can use a combination of these feedback components in the loading scene.
Remember to assign the `LoadingBehavior` field of these components to the `LoadingBehavior` component you created before.

Another feedback that you could make is a **fade in/out** effect.
The `LoadingFader` component does just that.
Add it to an [UI CanvasGroup] [GameObject] to control the group's alpha value during the visual transitions.
You can also set the fade time and customize the fade in/out animation curves to suit your preference.

To use the `LoadingFader` effectively, you must **enable** both `WaitForScriptedStart` and `WaitForScriptedEnd` toggles in your `LoadingBehavior` component.

### Loading Screen Example

Take the following loading screen scene hierarchy as an example:

* Canvas - ([Canvas](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-Canvas.html), [CanvasScaler](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-CanvasScaler.html), `LoadingBehavior`)
  * Group - ([CanvasGroup], `LoadingFader`)
    * Background - ([Image](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Image.html))
    * Text_Message - ([TextMeshProUGUI])
    * Slider_Progress - ([Slider], `LoadingFeedbackSlider`)
      * Text_Progress - ([TextMeshProUGUI], `LoadingFeedbackTextMeshPro`)

By having this hierarchy in your loading scene, it would be able to fade in/out and display both the loading progress bar and loading progress text feedback.
As this scene has the `LoadingFader` component, remember to enable both `WaitForScriptedStart` and `WaitForScriptedEnd` toggles in the `LoadingBehavior` component.
Also, if you're not using an addressable scene manager, enable the `ReducedLoadRatio` toggle.

You can test this scene by passing its `ILoadSceneInfo` reference as the `intermediateSceneInfo` in an `ISceneLoader.TransitionToScene` method.

[_[back to top]_](#advanced-scene-manager)

## Why so many interfaces?

The idea behind the interfaces is first to decouple things and second to allow you to build your custom implementations if you require something very different from the included content.
Sometimes projects require very specific implementations, and instead of making the system extremely complex and detailed, I'd rather have it broken into many different pieces that you can replace to fit with whatever works best for you.

## Tests

This package includes tests to assert most use cases of the Scene Managers and Scene Loaders.
The tests do not have any effect on a runtime build of the game, they only mean to work in a development environment.

[_[back to top]_](#advanced-scene-manager)

---

Don't hesitate to create [issues](https://github.com/mygamedevtools/scene-loader/issues) for suggestions and bugs. Have fun!

[MonoBehaviour]: https://docs.unity3d.com/Manual/class-MonoBehaviour.html
[MonoBehaviours]: https://docs.unity3d.com/Manual/class-MonoBehaviour.html
[ScriptableObject]: https://docs.unity3d.com/Manual/class-ScriptableObject.html
[GameObject]: https://docs.unity3d.com/Manual/class-GameObject.html
[GameObjects]: https://docs.unity3d.com/Manual/class-GameObject.html
[UI Text Mesh Pro]: https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/api/TMPro.TextMeshProUGUI.html
[TextMeshProUGUI]: https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/api/TMPro.TextMeshProUGUI.html
[UI Slider]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Slider.html
[Slider]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Slider.html
[UI Canvas Group]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-CanvasGroup.html
[CanvasGroup]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-CanvasGroup.html
