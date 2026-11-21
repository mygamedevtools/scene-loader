---
sidebar_position: 2
---

# My Scene Manager

`MySceneManager` is a static wrapper to the `CoreSceneManager` class, that exists to simplify the usage experience of the **Scene Operations**.
It manages an internal reference to a Core Scene Manager that is created during the `RuntimeInitializeOnLoadMethod` callback, which is executed after the first scene has loaded and after the first `Awake()` cycle.
That means that `MySceneManager` will not be initialized until the first `Start()` cycle.

```cs
[RuntimeInitializeOnLoadMethod]
internal static void Initialize()
{
  _instance = new CoreSceneManager(true);
}
```

## Static API

You can optionally disable the `MySceneManager` static class entirely if you wish to manually handle the `CoreSceneManager` lifecycle and/or extend any functionality.
To do it, simply define the scripting symbol `DISABLE_STATIC_SCENE_MANAGER` on your scripting compilation settings.

## The four methods

It does not expose the internal `CoreSceneManager` instance, so it mirrors the same four operations statically:

```cs
MySceneManager.LoadAsync(sceneParameters);
MySceneManager.UnloadAsync(sceneParameters);
MySceneManager.TransitionAsync(sceneParameters, loadingScreen);
MySceneManager.ReloadActiveSceneAsync(loadingScreen);
```

One signature per operation covers every kind of reference, because `SceneParameters` and `LoadingScreen` both convert implicitly:

```cs
MySceneManager.LoadAsync("my-scene");                     // string
MySceneManager.LoadAsync(1);                              // build index
MySceneManager.LoadAsync(new[] { "scene-a", "scene-b" }); // several
MySceneManager.TransitionAsync("target", "loading");      // with a loading screen
```

## Events

`MySceneManager` forwards the same events as the instance API:

| Event | |
|---|---|
| `SceneLoaded` / `SceneUnloaded` | Once per scene |
| `ActiveSceneChanged` | The previous and current active scene |
| `OperationStarted` | Every operation this manager starts, **before** it runs |

:::warning[`SceneUnloaded` passes a scene that is gone]
`SceneUnloaded` is raised **after** the unload has finished, so the `Scene` it passes is no longer valid: its `name` and `path` are empty and `IsValid()` returns `false`.
Its handle is kept, so compare it with `==` to a `Scene` you saved at load time instead of checking its name:

```cs
Scene hud = (await MySceneManager.LoadAsync("hud")).GetScene();

MySceneManager.SceneUnloaded += scene =>
{
  if (scene == hud)            // works: the handle is kept
    OnHudUnloaded();

  // scene.name == "hud" is never true here: the name is already empty
};
```
:::

`OperationStarted` is the attach point for global instrumentation — it hands you the `SceneOperation` before its first state change, which is the only moment from which you can observe the whole lifecycle:

```cs
MySceneManager.OperationStarted += op =>
{
  op.StateChanged += o => Analytics.Track(o.Kind, o.State);
};
```
