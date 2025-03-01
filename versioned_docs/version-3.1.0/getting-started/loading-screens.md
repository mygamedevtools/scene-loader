---
sidebar_position: 3
description: How to create loading screens with the package.
---

# Creating Loading Screens

During scene transitions, you have the option to provide an intermediate scene that can be used as loading screen.
This could be an animated splash screen or a loading progress bar, for example.
This package provides implementations to help you build your loading screens faster.

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
Also, if you're not using an addressable scene manager, enable the `ReducedLoadRatio` toggle.

You can test this scene by passing its `ILoadSceneInfo` reference as the `intermediateSceneInfo` parameter in an `ISceneLoader.TransitionToScene` method.

## Loading Components

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
Add it to an [UI Canvas Group] [GameObject] to control the group's alpha value during the visual transitions.
You can also set the fade time and customize the fade in/out animation curves to suit your preference.

To use the `LoadingFader` effectively, you must **enable** both `WaitForScriptedStart` and `WaitForScriptedEnd` toggles in your `LoadingBehavior` component.



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