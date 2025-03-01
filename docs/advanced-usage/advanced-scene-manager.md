---
sidebar_position: 2
---

# Advanced Scene Manager

The `AdvancedSceneManager` is a static wrapper to the `CoreSceneManager` class, that exists to simplify the usage experience of the **Scene Operations**.
It manages an internal reference to a Core Scene Manager that is created during the `RuntimeInitializeOnLoadMethod` callback, which is executed after the first scene has loaded and after the first `Awake()` cycle.
That means that the `AdvancedSceneManager` will not be initialized until the first `Start()` cycle.

```cs
[RuntimeInitializeOnLoadMethod]
internal static void Initialize()
{
  _instance = new CoreSceneManager(true);
}
```

## Static API

You can optionally disable the `AdvancedSceneManager` static class entirely if you wish to manually handle the `CoreSceneManager` lifecycle and/or extend any functionality.
To do it, simply define the scripting symbol `DISABLE_STATIC_SCENE_MANAGER` on your scripting compilation settings.

## Extension Methods

As it doesn't expose the internal `CoreSceneManager` instance, it reimplements the extension methods so you have exactly the same usage options for the **Scene Operations** available statically.