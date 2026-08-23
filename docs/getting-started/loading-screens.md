---
sidebar_position: 3
description: How to create loading screens with the package.
---

# Creating Loading Screens

During scene transitions, you have the option to provide a loading screen — an animated splash screen or a loading progress bar, for example.

A loading screen is a `LoadingScreen`. The simplest one is a scene, and naming a scene gives you that for free:

```cs
MySceneManager.TransitionAsync("target", "loading");           // a scene
MySceneManager.TransitionAsync("target", new MyScreen());      // a prefab, a UI Toolkit document, anything
```

A scene name, path, address, build index, `Scene` or `AssetReference` all convert into a scene-based loading screen implicitly, so you only write `LoadingScreen` yourself when you want something that is *not* a scene.

## Loading Screen Example

Take the following loading screen scene hierarchy as an example:

* Canvas - ([Canvas](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-Canvas.html), [CanvasScaler](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-CanvasScaler.html), `LoadingBehavior`)
  * Group - ([CanvasGroup], `LoadingFader`)
    * Background - ([Image](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Image.html))
    * Text_Message - ([TextMeshProUGUI])
    * Slider_Progress - ([Slider], `LoadingFeedbackSlider`)
      * Text_Progress - ([TextMeshProUGUI], `LoadingFeedbackTextMeshPro`)

By having this hierarchy in your loading scene, it would be able to fade in/out and display both the loading progress bar and loading progress text feedback.
As this scene has the `LoadingFader` component, remember to enable both `WaitForScriptedStart` and `WaitForScriptedEnd` toggles in the `LoadingBehavior` component.

You can test this scene by passing its name, path or build index as the second argument to `TransitionAsync`.

## Loading Components

### The Loading Behavior

The Loading Behavior is a [MonoBehaviour] component, which you can attach to Unity [GameObjects], that receives the progress value from the scene manager.
Add a `LoadingBehavior` component to a [GameObject] in your loading scene to display scene loading feedback.
It exposes its `LoadingProgress` instance, which you can use to listen to the loading events:

```cs
public class LoadingProgress : IProgress<float>
{
  public event Action<float> Progressed;
  public event Action LoadingCompleted;

  public bool IsShown { get; }
  public bool IsHidden { get; }
}
```

The `LoadingCompleted` event notifies when the scene load operation is completed, but the loading screen is still active.
The `Progressed` event sends a `float` parameter, ranging from 0 to 1, to report the progress of the scene loading operation.

:::note
To wait on the screen's own transitions rather than the scene load, use `WaitForShowAsync()` and `WaitForHideAsync()`, or read the `IsShown` / `IsHidden` properties. These only observe the gates — you open them with `StartTransition()` and `EndTransition()`, and calling either twice is harmless.
:::

Back to the `LoadingBehavior`, it has a few options you can set on the Unity [Inspector](https://docs.unity3d.com/Manual/UsingTheInspector.html):

* **Wait For Scripted Start**: enable if the loading screen will have a **transition in** effect, such as a fade in.
* **Wait For Scripted End**: enable if the loading screen will have a **transition out** effect, such as a fade out.

You will use these controls to customize your loading screen behavior.

:::warning
If you enable one of these toggles and never call the matching trigger, the transition waits forever. It will not fail silently: after 10 seconds a development build names the `LoadingBehavior` holding it up, and keeps waiting.
:::

:::info[How it is found]
A `LoadingBehavior` registers itself when it is **enabled**, under the scene it lives in. Two consequences worth knowing:

* A `LoadingBehavior` on a **disabled** GameObject is never found, and the transition runs with no feedback and no waiting rather than reporting a problem.
* **One per loading scene.** If a scene contains two, the last one enabled is the one the transition drives.
:::

:::note
A `LoadingBehavior` is **optional**. A loading scene without one still works as a loading screen — you simply get no progress feedback, and the transition never waits for a scripted start or end.
:::

### The Loading Feedback

At this point, you should already have your loading scene with a `LoadingBehavior` attached to one of your [GameObjects].
Now you can also add some other components to display the loading progress feedback.
This package comes with **three** feedbacks:

* `LoadingFeedbackSlider`: attach on an [UI Slider] to display the loading progress feedback as a progress bar.
* `LoadingFeedbackTextMeshPro`: attach on an [UI Text Mesh Pro] to display the loading progress feedback as text normalized from 0 to 100.
* `LoadingFeedbackText` _(also known as Legacy)_: attach on an [UI Legacy Text](https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Text.html) to display the loading progress feedback as text normalized from 0 to 100.

You can use a combination of these feedback components in the loading scene.
Remember to assign the `LoadingBehavior` field of these components to the `LoadingBehavior` component you created before.

### Loading Fader

The `LoadingFader` component performs **fade in/out** transitions.
Add it to an [UI Canvas Group] [GameObject] to control the group's alpha value during the visual transitions.
You can also set the fade time and customize the fade in/out animation curves to suit your preference.

To use the `LoadingFader` effectively, you must **enable** both `WaitForScriptedStart` and `WaitForScriptedEnd` toggles in your `LoadingBehavior` component.

## Loading screens that are not scenes

A scene is a heavy way to show a progress bar. `LoadingScreen` is the abstraction that lets a prefab or a UI Toolkit document do the same job:

```cs
public abstract class LoadingScreen : IDisposable
{
  public abstract ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation);
  public abstract ConditionAwaiter ShowAsync(SceneOperation operation);
  public abstract void ReportProgress(float progress);
  public abstract ConditionAwaiter HideAsync(SceneOperation operation);
  public abstract void Dispose();
}
```

```cs
public class MyScreen : LoadingScreen
{
  public override ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation op) { /* instantiate into host */ }
  public override ConditionAwaiter ShowAsync(SceneOperation op)  { /* gate transition-in  */ }
  public override void ReportProgress(float progress)            { /* drive the UI       */ }
  public override ConditionAwaiter HideAsync(SceneOperation op)  { /* gate transition-out */ }
  public override void Dispose()                                 { /* clean up           */ }
}

await MySceneManager.TransitionAsync("target", new MyScreen());
```

`LoadingScreenHost` is a package-owned scene that exists for the length of one transition, so a prefab screen has somewhere to live that is not the scene being unloaded.

`SceneLoadingScreen` is the built-in implementation for scene-based screens — it is what every implicit conversion above produces, and it is what drives the `LoadingBehavior` components described earlier.

:::info
The `Loading Scene Examples` sample ships `PrefabLoadingScreen` and `UIDocumentLoadingScreen` as working implementations you can copy.
:::

## Loading Scene Sample

You can try multiple loading screens in the [Loading Scene Examples](../samples/loading-scene-examples.md) Sample.

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
