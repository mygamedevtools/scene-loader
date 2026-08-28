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
Nothing needs wiring: every component below the `LoadingBehavior` finds it on its parents, and the `LoadingFader` holds the transition for the length of each fade on its own.

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

  public void HoldShow(object owner);
  public void ReleaseShow(object owner);
  public void HoldHide(object owner);
  public void ReleaseHide(object owner);
  public void HoldCompletion(object owner);
  public void ReleaseCompletion(object owner);
}
```

The `LoadingCompleted` event notifies when the scene load operation is completed, but the loading screen is still active — it is the screen's cue to start hiding itself.
The `Progressed` event sends a `float` parameter, ranging from 0 to 1, to report the progress of the scene loading operation.

#### Gates and holds

The transition waits at two **gates**: the *show* gate before it unloads the scene you came from, and the *hide* gate before it considers the loading screen gone. Both are **open unless something is holding them closed**.

Anything that needs the transition to wait — a fade, an animation, a script — calls `HoldShow` or `HoldHide` with itself as the owner, and releases when it is done. The gate opens when the last holder lets go, which is what lets several components gate the same transition without any of them knowing about the others.

```cs
void Awake()
{
    // Take the hold before the transition can read the gate.
    _loadingBehavior.Progress.HoldShow(this);
    _loadingBehavior.Progress.HoldHide(this);
    _loadingBehavior.Progress.LoadingCompleted += PlayOut;

    PlayIn();
}

void OnPlayInFinished()  => _loadingBehavior.Progress.ReleaseShow(this);
void OnPlayOutFinished() => _loadingBehavior.Progress.ReleaseHide(this);
```

Holds are identified by their owner, so taking one twice and releasing one twice are both harmless. Take them in `Awake` or `OnEnable`: one taken later may arrive after the transition has already read the gate.

There is a third hold, `HoldCompletion`, which delays the `LoadingCompleted` **cue** rather than a gate. This is what a minimum display time wants: holding the hide gate would let a fade-out run to its end and leave the rest of the wait on an empty screen, while holding completion keeps the screen up and starts the fade-out when it should.

:::note
To wait on the gates yourself, use `WaitForShowAsync()` and `WaitForHideAsync()`, or read the `IsShown` / `IsHidden` properties.
:::

:::warning
If you take a hold and never release it, the transition waits. It will not fail silently: after 10 seconds a development build names the holder, and keeps waiting. A holder that is destroyed without releasing is dropped rather than left blocking forever.
:::

:::info[How it is found]
A `LoadingBehavior` registers itself when it is **enabled**, under the scene it lives in. Two consequences worth knowing:

* A `LoadingBehavior` on a **disabled** GameObject is never found, and the transition runs with no feedback and no waiting rather than reporting a problem.
* **One per loading scene.** If a scene contains two, the transition logs a warning and drives the first one registered.
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
Their `LoadingBehavior` field is optional: when left empty, it is taken from the same object or its closest parent that has one. Assign it only when the feedback lives somewhere else in the hierarchy.

All of them extend `LoadingScreenComponent`, the base for anything that lives on a loading screen and drives, or waits on, its `LoadingProgress`. Extend it yourself to write your own — `OnBound` is called once the `Progress` is available:

```cs
public class LoadingFeedbackImageFill : LoadingScreenComponent
{
    Image _image;

    protected override void Awake()
    {
        _image = GetComponent<Image>();
        base.Awake();
    }

    protected override void OnBound()
    {
        Progress.Progressed += progress => _image.fillAmount = progress;
    }
}
```

### Loading Fader

The `LoadingFader` component performs **fade in/out** transitions.
Add it to an [UI Canvas Group] [GameObject] to control the group's alpha value during the visual transitions.
You can also set the fade time and customize the fade in/out animation curves to suit your preference.

It holds both gates for the length of each fade, so adding the component is itself the statement that the transition should wait for the fades — there is nothing to enable on the `LoadingBehavior`.

## Loading screens that are not scenes

A scene is a heavy way to show a progress bar. `LoadingScreen` is the abstraction that lets a prefab or a UI Toolkit document do the same job:

```cs
public abstract class LoadingScreen : IDisposable
{
  protected LoadingProgress Progress { get; }
  protected void BindProgress(LoadingProgress progress);

  public abstract SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation);
  public virtual SceneOperationPump.ConditionAwaiter ShowAsync(SceneOperation operation);
  public virtual void ReportProgress(float progress);
  public virtual SceneOperationPump.ConditionAwaiter HideAsync(SceneOperation operation);
  public virtual void Dispose();
}
```

`PrepareAsync` is the only member a screen has to write, plus `Dispose` if it built anything. Showing, hiding and reporting are driven by the `LoadingProgress` the screen binds while preparing — one found on a `LoadingBehavior`, or one it creates for itself — so every screen gates the same way rather than reimplementing it. A screen that binds nothing gates on nothing.

```cs
public class PrefabLoadingScreen : LoadingScreen
{
  readonly GameObject _prefab;
  GameObject _instance;

  public PrefabLoadingScreen(GameObject prefab) => _prefab = prefab;

  public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
  {
    _instance = Object.Instantiate(_prefab);
    host.Adopt(_instance);   // into the holder scene, so it survives the outgoing scene being unloaded

    BindProgress(LoadingBehaviorRegistry.TryGet(_instance, out LoadingBehavior behavior) ? behavior.Progress : null);
    return SceneOperationPump.Completed(operation);
  }

  public override void Dispose()
  {
    if (_instance != null)
      Object.Destroy(_instance);
    base.Dispose();
  }
}

await MySceneManager.TransitionAsync("target", new PrefabLoadingScreen(prefab));
```

`LoadingScreenHost` is a package-owned scene that exists for the length of one transition, so a prefab screen has somewhere to live that is not the scene being unloaded.

`SceneLoadingScreen` is the built-in implementation for scene-based screens — it is what every implicit conversion above produces, and it is what drives the `LoadingBehavior` components described earlier.

:::info
The `Loading Scene Examples` sample ships `PrefabLoadingScreen` and `UIDocumentLoadingScreen` — a screen that creates its own `LoadingProgress` and needs no `LoadingBehavior` at all — as working implementations you can copy, along with `MinimumDisplayTime`, a `LoadingScreenComponent` that keeps any screen up for a set time.
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
