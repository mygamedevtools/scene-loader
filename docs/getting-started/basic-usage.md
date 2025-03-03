---
sidebar_position: 3
description: Basic introduction to the usage of the Advanced Scene Manager.
---

# Basic Usage

Loading scenes with this package implies that the scenes **will always be loaded as Additive**. That is simply because there is no advantage in loading scenes in the **Single** load scene mode when you expect to work with multiple scenes. 

You will be using the `AdvancedSceneManager` static class to perform the scene operations.

## Loading scenes

You can load scenes by using any of these references:

```cs
// Name
AdvancedSceneManager.LoadAsync("my-scene");
// Path (relative to the Assets folder)
AdvancedSceneManager.LoadAsync("Scenes/my-scene");
// Build Index
AdvancedSceneManager.LoadAsync(1);
// Address
AdvancedSceneManager.LoadAddressableAsync("my-scene-address");
// Asset Reference
AdvancedSceneManager.LoadAddressableAsync(mySceneAssetReference);
```

Additionally, you can also pass an array of scenes (given the same type of reference):

```cs
// Array of build indexes
AdvancedSceneManager.LoadAsync(new int[] { 1, 2, 3});
```

The loaded scene can be marked to be set as the active scene:

```cs
// Loads a scene and sets it as the active scene
AdvancedSceneManager.LoadAsync("my-scene", true);

// Loads a list of scenes and set the scene at index 1 as the active scene
AdvancedSceneManager.LoadAsync(new int[] { 1, 2, 3 }, 1);
```

You can get the progress of the loading operation by passing an `IProgress<float>` implementation, for example:

```cs
public class SimpleProgress : IProgress<float>
{
    public float Value;

    public void Report(float value) => Value = value;
}
// [...]

SimpleProgress progress = new SimpleProgress();
AdvancedSceneManager.LoadAsync("my-scene", true, progress);
```

## Unloading scenes

You can unload scenes by using any reference, including the scene itself.

```cs
// Name
AdvancedSceneManager.UnloadAsync("my-scene");
// Path (relative to the Assets folder)
AdvancedSceneManager.UnloadAsync("Scenes/my-scene");
// Build Index
AdvancedSceneManager.UnloadAsync(1);
// Address
AdvancedSceneManager.UnloadAddressableAsync("my-scene-address");
// Asset Reference
AdvancedSceneManager.UnloadAddressableAsync(mySceneAssetReference);
// Scene
AdvancedSceneManager.UnloadAsync(AdvancedSceneManager.GetActiveScene());
```

You can also unload multiple scenes:

```cs
// Array of build indexes
AdvancedSceneManager.UnloadAsync(new int[] { 1, 2, 3});
```

## Scene Transitions

To perform scene transitions, first you pass the target scene(s) and then the intermediate scene (optional).
You can use the same references from the `LoadAsync` method.

```cs
// Name
AdvancedSceneManager.TransitionAsync("my-target-scene", "my-loading-scene");

// Array of AssetReference
AdvancedSceneManager.TransitionAddressableAsync(new AssetReference[] { scene1, scene2, scene3 });
```

:::info
The reference type must be the same for the target scene and the intermediate scene.
:::

Check the [Loading Scene Examples](../samples/loading-scene-examples.md) Sample to try different loading scenes when performing **Scene Transitions**.

## Async Programming

All scene operations are awaitable and can be used in coroutines as well. For example:

```cs
await AdvancedSceneManager.TransitionAsync("my-target-scene", "my-loading-scene");
// Do something after the transition
```

For coroutines, you must convert the `Task` into a `WaitTask`, which is a helper struct to support coroutines:

```cs
yield return AdvancedSceneManager.TransitionAsync("my-target-scene", "my-loading-scene").ToWaitTask();
// Do something after the transition
```