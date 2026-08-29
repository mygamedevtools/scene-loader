---
sidebar_position: 6
---

# Troubleshooting

## Nothing is tracked when creating a `CoreSceneManager`

When creating a `CoreSceneManager` passing a `true` value to its constructor, as `new CoreSceneManager(true)`, it attempts to add all loaded scenes to its list of tracked scenes.
However, if you call that during `Awake()`, the scene is not fully loaded yet and there is nothing to add, so you will see:

```
[MySceneManager] Tried to create a Scene Manager with all loaded scenes, but encoutered none.
Did you create the Scene Manager on `Awake()`? If so, try moving the call to `Start()` instead.
```

Move your call to `Start()` instead.

## A scene resolves to the wrong backend

A bare string is resolved by looking at the **build settings first**, then Addressables. If a scene is in both, the build settings win and you will see:

```
[MySceneManager] The scene 'my-scene' matches both the build settings and an Addressables entry.
The build settings take precedence. Use SceneRef.Address("my-scene") to load the addressable one.
```

Use `SceneRef.Address("my-scene")` to force the addressable one. See [Scene Ref](./advanced-usage/scene-ref.md#how-a-string-is-resolved).

## A scene cannot be found at all

```
Could not resolve the scene 'my-scene'. It was not found in the build settings or the Addressables catalog.
```

Add it to the build settings, register it as an Addressables entry, or pass an explicit reference. If Addressables is not installed, the message says so — only the build settings were searched.

## An operation appears to hang

After 10 seconds waiting on the same engine operation, a development build reports what it is waiting on and keeps waiting:

```
[MySceneManager] A Transition operation has been waiting 10 seconds on ...
```

This usually means a loading screen gate was never released — a component that called `HoldShow` or `HoldHide` on its `LoadingProgress` and never called the matching `ReleaseShow` / `ReleaseHide`. The warning names the holder. A holder that is destroyed without releasing is dropped automatically, so the culprit is one that is still alive.

## Turning the diagnostics up

Everything above is emitted through `SceneManagerLog`, which defaults to `Warning` in development builds and `Error` in release. Raise it at runtime — including inside a shipped build, which is when it is worth having:

```cs
SceneManagerLog.Level = SceneLogLevel.Verbose;
SceneManagerLog.Handler = myHandler;   // route to an in-game console or analytics
```

`SceneLogLevel.Off` silences it. Assigning `null` to `Handler` restores the Unity console rather than silencing.

See [Logging](./advanced-usage/logging.md) for the levels, routing and what each costs.
