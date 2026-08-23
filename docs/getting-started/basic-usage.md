---
sidebar_position: 3
description: Basic introduction to the usage of My Scene Manager.
---

# Basic Usage

Loading scenes with this package implies that the scenes **will always be loaded as Additive**. That is simply because there is no advantage in loading scenes in the **Single** load scene mode when you expect to work with multiple scenes.

You will be using the `MySceneManager` static class to perform the scene operations.

## Loading scenes

You can load scenes by using any of these references:

```cs
// Name
MySceneManager.LoadAsync("my-scene");
// Path (relative to the Assets folder)
MySceneManager.LoadAsync("Scenes/my-scene");
// Build Index
MySceneManager.LoadAsync(1);
// Address
MySceneManager.LoadAsync(SceneRef.Address("my-scene-address"));
// Asset Reference
MySceneManager.LoadAsync(mySceneAssetReference);
```

:::info
There is no separate addressable API. A plain string is looked up in your **build settings first**, then in Addressables — so `LoadAsync("my-scene")` finds your scene wherever it lives.

`SceneRef.Address(...)` is the override, for when a name exists in both places or when you want to skip the lookup. See [Scene Ref](../advanced-usage/scene-ref.md#how-a-string-is-resolved).
:::

Additionally, you can also pass an array of scenes:

```cs
// Array of build indexes
MySceneManager.LoadAsync(new int[] { 1, 2, 3 });
// Mixed kinds are fine too
MySceneManager.LoadAsync(new SceneRef[] { "scene-a", 2, SceneRef.Address("scene-c") });
```

The loaded scene can be marked to be set as the active scene, through `SceneParameters`:

```cs
// Loads a scene and sets it as the active scene
MySceneManager.LoadAsync(new SceneParameters("my-scene", true));

// Loads a list of scenes and sets the scene at index 1 as the active scene
MySceneManager.LoadAsync(new SceneParameters(new SceneRef[] { 1, 2, 3 }, 1));
```

You get the progress from the returned handle, rather than by passing an `IProgress<float>` in:

```cs
SceneOperation op = MySceneManager.LoadAsync("my-scene");
op.Progressed += value => progressBar.value = value;
```

## Unloading scenes

You can unload scenes by using any reference, including the scene itself.

```cs
// Name
MySceneManager.UnloadAsync("my-scene");
// Path (relative to the Assets folder)
MySceneManager.UnloadAsync("Scenes/my-scene");
// Build Index
MySceneManager.UnloadAsync(1);
// Address
MySceneManager.UnloadAsync(SceneRef.Address("my-scene-address"));
// Asset Reference
MySceneManager.UnloadAsync(mySceneAssetReference);
// Scene
MySceneManager.UnloadAsync(MySceneManager.GetActiveScene());
```

You can also unload multiple scenes:

```cs
// Array of build indexes
MySceneManager.UnloadAsync(new int[] { 1, 2, 3 });
```

## Scene Transitions

To perform scene transitions, first you pass the target scene(s) and then the loading screen (optional).
You can use the same references from the `LoadAsync` method.

```cs
// Name
MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");

// Array of AssetReference
MySceneManager.TransitionAsync(new AssetReference[] { scene1, scene2, scene3 });
```

:::info
The target scenes and the loading screen **no longer have to be the same reference kind**. In `4.x` mixing them meant picking a different method; here each is resolved independently.

The loading screen does not even have to be a scene — see [Loading Screens](./loading-screens.md).
:::

Check the [Loading Scene Examples](../samples/loading-scene-examples.md) Sample to try different loading screens when performing **Scene Transitions**.

## Scene Reloading

You can reload the active scene using the `ReloadActiveSceneAsync` method.
A scene reload is also a **scene transition** internally.
It will load the active scene via the same reference it was loaded initially.

Just like with **Scene Transitions**, you can also pass a loading screen.

```cs
MySceneManager.ReloadActiveSceneAsync("my-loading-scene");

// No loading screen:
MySceneManager.ReloadActiveSceneAsync();
```

## Async Programming

Every operation returns a [`SceneOperation`](../advanced-usage/scene-operation.md) immediately — a handle on the work, which you can await directly:

```cs
await MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");
// Do something after the transition
```

For coroutines, use `ToCoroutine()`:

```cs
yield return MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene").ToCoroutine();
// Do something after the transition
```

And if a third-party API needs a `Task`, `AsTask()` bridges to one:

```cs
Task<SceneResult> task = MySceneManager.LoadAsync("my-scene").AsTask();
```

## Cancelling

There is no `CancellationToken` parameter. You cancel through the handle:

```cs
SceneOperation op = MySceneManager.LoadAsync("my-scene");
op.Cancel();

// Or bridge a token you already have:
MySceneManager.LoadAsync("my-scene").CancelWith(destroyCancellationToken);
```

:::warning
Cancelling stops **this operation's** reporting, its remaining phases and its waiters. The underlying Unity load still runs to completion — Unity scene operations cannot be aborted, which is why `4.x`'s tokens never cancelled the work either.
:::
