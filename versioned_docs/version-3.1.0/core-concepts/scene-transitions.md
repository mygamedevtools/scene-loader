---
sidebar_position: 4
---

# Scene Transitions

A Scene Transition is a combination of **load** and **unload** operations to effectively transition between scenes, with or without an intermediate scene. For example, usually, if you'd want to go from scene A to scene B you would:

1. **Load** the scene B.
2. **Unload** the scene A.

That's only **two** operations, but what if you wanted to have a loading screen as well?
In this case, you would:

1. **Load** the loading scene.
2. **Load** the scene B.
4. **Unload** the scene A.
3. **Unload** the loading scene.

That's **four** operations now.
The `TransitionToScene` and `TransitionToSceneAsync` methods let you only provide where you want to go from the **current active scene** and if you want an intermediary scene (loading scene for example).

Also, aside from transitioning from the current active scene, you can also use the `TransitionToSceneFromScenes` and `TransitionToSceneFromAll` alternatives:

- `TransitionToSceneFromScenes` - unloads a given group of scenes during transition.
- `TransitionToSceneFromAll` - unloads all loaded scenes during transition.

Just like the regular `Transition` methods, its variants also have single/multiple scene options as well as async options.