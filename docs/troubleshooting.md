---
sidebar_position: 6
---

# Troubleshooting

## Error when creating a `CoreSceneManager`

When creating an `CoreSceneManager` passing a `true` value to its constructor, as `new CoreSceneManager(true)`, it attempts to add all loaded scenes to its list of tracked scenes.
However, if you called that during `Awake()`, you might see the error:

```
ArgumentException: Attempted to get an {nameof(ISceneData)} through an invalid or unloaded scene.
```

This error is thrown because during `Awake()` the scene is not fully loaded and cannot be added to the list of tracked scenes.

Move your call to `Start()` instead.