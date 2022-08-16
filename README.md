![License](https://img.shields.io/github/license/joaoborks/myunitytools-sceneloader)
![Release](https://img.shields.io/github/v/release/joaoborks/myunitytools-sceneloader?sort=semver)
![Last Commit](https://img.shields.io/github/last-commit/joaoborks/myunitytools-sceneloader)

My Unity Tools - Scene Loading
===

_Collection of tools to improve scene management and transitions in Unity._

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

Loading scenes in Unity is very simple, but developing games sometimes require more flexible implementations. This package has two main focuses:

1. Cover the most common uses of Scene Loading
2. Provide `awaitable` implementations

Additionally, it offers support for [Unity Addressables](https://docs.unity3d.com/Manual/com.unity.addressables.html) and for [UniTask](https://github.com/Cysharp/UniTask) with no additional setup required.

The paradigm for Scene Loading relies in the following actions:
* **Load**: loads a new scene on top of the current scene structure.
* **Unload**: unloads one of the scenes in the scene structure.
* **Switch**: replaces the current active scene with a new one (executes both unload and load operations).
* **Transition**: switches to another scene but with an intermediate "loading scene" in between.

Keep that in mind when using the scene loader utilities.

Usage
---

There are four scene loaders that you can use, depending on your project necessities:
1. `AsyncSceneLoader`: simple scene loader with `awaitable` instructions.
2. `UniTaskSceneLoader`: the `AsyncSceneLoader` with `UniTask` instead of `Task`.
3. `AddressableSceneLoader`: a scene loader that handles Addressable scenes with `awaitable` instructions.
4. `AddressableUniTaskSceneLoader`: the `AddressableSceneLoader` with `UniTask` instead of `Task`.

---

Don't hesitate to create [issues](https://github.com/joaoborks/myunitytools-sceneloader/issues) for suggestions and bugs. Have fun!