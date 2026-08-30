---
sidebar_position: 3
description: How to create loading screens with the package.
---

# Creating Loading Screens

During scene transitions, you have the option to provide a loading screen — an animated splash screen or a loading progress bar, for example.

A loading screen is a `LoadingScreen`. The simplest one is a scene, and naming a scene gives you that for free:

```cs
MySceneManager.TransitionAsync("target", "loading");                                  // a scene
MySceneManager.TransitionAsync("target", new PrefabLoadingScreen(prefab));            // a prefab
MySceneManager.TransitionAsync("target", new UIDocumentLoadingScreen(uxml, panel));   // a UI Toolkit document
```

A scene name, path, address, build index, `Scene` or `AssetReference` all convert into a scene-based loading screen implicitly, so you only write a `LoadingScreen` yourself when you want something that is *not* a scene.

Whatever the screen is, it gates the transition the same way, through a `LoadingProgress`. This page starts with a loading scene built from the package's components, explains the gates those components use, and then shows how the same gates drive a prefab or a UI Toolkit document.

## A loading scene

Take the following loading scene hierarchy as an example — it is the `Loading_Screen` scene from the [Loading Scene Examples](../samples/loading-scene-examples.md) sample:

* Loading Screen - ([Canvas], [CanvasScaler], [CanvasGroup], `LoadingBehavior`, `LoadingFader`, `MinimumDisplayTime`)
  * Backdrop - ([Image])
  * Card - ([Image])
    * Value - ([Text], `LoadingFeedbackText`)
    * Track - ([Slider], `LoadingFeedbackSlider`)
      * Fill - ([Image])

By having this hierarchy in your loading scene, it fades in, displays both a progress bar and a progress percentage, stays up for at least a couple of seconds, and fades out once the target scene has loaded.

Nothing is wired in the Inspector: every component below the `LoadingBehavior` finds it on its parents, and each one that needs the transition to wait — the fader, the minimum display time — takes a hold of its own. The transition waits for whoever releases last.

You can test this scene by passing its name, path or build index as the second argument to `TransitionAsync`.

:::tip
The loading scene does not have to be uGUI. The sample's `Loading_Animated` scene is a UI Toolkit `UIDocument` with the same `LoadingBehavior` on it — see [Custom components](#custom-components) below.
:::

## Loading Components

### The Loading Behavior

The `LoadingBehavior` is a [MonoBehaviour] component that anchors the screen's `LoadingProgress`. Put one on the root of your loading screen and everything else — feedback, fades, animations — hangs off its `Progress`:

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

The `Progressed` event sends a `float` parameter, ranging from 0 to 1, to report the progress of the scene loading operation.
The `LoadingCompleted` event notifies when the scene load operation is completed, but the loading screen is still active — it is the screen's cue to start hiding itself.

:::info[How it is found]
A `LoadingBehavior` registers itself when it is **enabled**, under the scene it lives in — or, for a prefab screen, under the hierarchy it was instantiated into. Two consequences worth knowing:

* A `LoadingBehavior` on a **disabled** GameObject is never found, and the transition runs with no feedback and no waiting rather than reporting a problem.
* **One per loading screen.** If a scene contains two, the transition logs a warning and drives the first one registered.
:::

:::note
A `LoadingBehavior` is **optional**. A loading scene without one still works as a loading screen — you simply get no progress feedback, and the screen shows for exactly as long as the load takes.
:::

### Gates and holds

The transition waits at two **gates**: the *show* gate before it unloads the scene you came from, and the *hide* gate before it considers the loading screen gone. Both are **open unless something is holding them closed**.

Anything that needs the transition to wait — a fade, an animation, a script — calls `HoldShow` or `HoldHide` with itself as the owner, and releases when it is done. The gate opens when the last holder lets go, which is what lets several components gate the same transition without any of them knowing about the others.

```cs
void Awake()
{
    // Take the holds before the transition can read the gates.
    _loadingBehavior.Progress.HoldShow(this);
    _loadingBehavior.Progress.HoldHide(this);
    _loadingBehavior.Progress.LoadingCompleted += PlayOut;

    PlayIn();
}

void OnPlayInFinished()  => _loadingBehavior.Progress.ReleaseShow(this);
void OnPlayOutFinished() => _loadingBehavior.Progress.ReleaseHide(this);
```

Holds are identified by their owner, so taking one twice and releasing one twice are both harmless. Take them in `Awake` or `OnEnable`: one taken later may arrive after the transition has already read the gate.

There is a third hold, `HoldCompletion`, which delays the `LoadingCompleted` **cue** rather than a gate. Holding the hide gate delays the *transition* while the screen has already been told to go, so a fade-out runs to its end and the rest of the wait plays out on an empty screen. Holding completion keeps the screen up, and whatever plays it out starts when it should. This is what a [minimum display time](#minimum-display-time) wants.

:::note
To wait on the gates yourself, use `WaitForShowAsync()` and `WaitForHideAsync()`, or read the `IsShown` / `IsHidden` properties.
:::

:::warning
If you take a hold and never release it, the transition waits. It will not fail silently: after 10 seconds a development build names the holder, and keeps waiting. A holder that is destroyed without releasing is dropped rather than left blocking forever.
:::

### The Loading Feedback

With a `LoadingBehavior` in place, add feedback components to display the progress.
This package comes with **three** feedbacks:

* `LoadingFeedbackSlider`: attach on an [UI Slider] to display the loading progress feedback as a progress bar.
* `LoadingFeedbackTextMeshPro`: attach on an [UI Text Mesh Pro] to display the loading progress feedback as text normalized from 0 to 100.
* `LoadingFeedbackText` _(also known as Legacy)_: attach on an [UI Legacy Text] to display the loading progress feedback as text normalized from 0 to 100.

You can use a combination of these feedback components in the loading scene.
Their `LoadingBehavior` field is optional: when left empty, it is taken from the same object or its closest parent that has one. Assign it only when the feedback lives somewhere else in the hierarchy.

### The Loading Fader

The `LoadingFader` component performs **fade in/out** transitions.
Add it to an [UI Canvas Group] [GameObject] to control the group's alpha value during the visual transitions.
You can set the fade in and fade out times separately, cap how far a single frame may advance a fade with `maxFrameStep`, and customize the fade in/out animation curves to suit your preference.

It holds both gates for the length of each fade, so adding the component is itself the statement that the transition should wait for the fades — there is nothing to enable on the `LoadingBehavior`.

Both fades run on **unscaled, clamped** time. Unscaled, because a transition started from a paused game — quitting to the menu from a pause screen at `timeScale = 0` — would otherwise never advance the fade, and never open the gate it is holding. Clamped, because the frame a scene activates on is routinely long enough to spend an entire fade before anything is drawn, leaving the first frame the player sees already mostly transparent; `maxFrameStep` (1/30 s by default) is the most one frame may count for.

### Custom components

The feedbacks and the fader all extend `LoadingScreenComponent`, the base for anything that lives on a loading screen and drives, or waits on, its `LoadingProgress`. It resolves the `LoadingBehavior` for you and calls `OnBound` once the `Progress` is available — which is where you subscribe to events and take your holds.

A feedback is a few lines:

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

An animation that the transition has to wait for is the same shape plus the holds. This is the sample's `Loading_Animated` scene — a UI Toolkit document whose panels slide in to meet, and slide back out once loading finishes:

```cs
[RequireComponent(typeof(UIDocument))]
public class AnimatedLoadingScreen : LoadingScreenComponent
{
    protected override void OnBound()
    {
        Progress.HoldShow(this);
        Progress.HoldHide(this);

        Progress.Progressed += OnProgressed;
        Progress.LoadingCompleted += SlideOut;

        SlideIn();
    }

    void SlideIn()
    {
        // ...start the USS transition...
        _left.schedule.Execute(() => Progress.ReleaseShow(this)).StartingIn(Milliseconds());
    }

    void SlideOut()
    {
        // ...start it in reverse...
        _left.schedule.Execute(() => Progress.ReleaseHide(this)).StartingIn(Milliseconds());
    }
}
```

Each gate is released when its slide has **finished**, not when it starts, so the outgoing scene is never unloaded behind a curtain that is still opening.

### Minimum display time

A scene that loads in two frames produces a loading screen that flashes on and off, which reads as a glitch. The `MinimumDisplayTime` component keeps a screen up for at least a set time, measured on the unscaled clock, and it is the reason `HoldCompletion` exists:

```cs
[AddComponentMenu("Scene Loading/Minimum Display Time")]
public class MinimumDisplayTime : LoadingScreenComponent
{
    [Min(0)]
    public float seconds = 2f;

    float _shownAt;

    protected override void OnBound()
    {
        _shownAt = Time.unscaledTime;
        Progress.HoldCompletion(this);
    }

    void Update()
    {
        if (Progress == null || Time.unscaledTime - _shownAt < seconds)
            return;

        Progress.ReleaseCompletion(this);
        enabled = false;
    }
}
```

Drop it next to a `LoadingBehavior` and the `LoadingCompleted` cue waits for it — the fader, or whatever else plays the screen out, starts once both the load and the timer are done. A load that already ran longer than `seconds` is not delayed at all.

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

`LoadingScreenHost` is a package-owned scene that exists for the length of one transition. Adopt whatever you build into it, so it has somewhere to live that is not the scene being unloaded.

`SceneLoadingScreen` is the built-in implementation for scene-based screens — it is what every implicit conversion above produces, and it binds the `LoadingBehavior` found in the loaded scene.

### A prefab screen

The same hierarchy as the loading scene above, instantiated instead of loaded. A `LoadingBehavior` anywhere on the prefab is picked up through `LoadingBehaviorRegistry` and gates the transition; without one, the screen holds nothing up.

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

### A UI Toolkit document screen

No scene, no prefab, and no `LoadingBehavior` anywhere. The screen creates its own `LoadingProgress`, holds its gates while it fades, and holds completion for its minimum display time — everything a loading screen has to do is expressed through `LoadingProgress`, and a plain C# object can hold one.

```cs
public class UIDocumentLoadingScreen : LoadingScreen
{
  public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
  {
    _instance = new GameObject(nameof(UIDocumentLoadingScreen));
    host.Adopt(_instance);

    UIDocument document = _instance.AddComponent<UIDocument>();
    document.panelSettings = _panelSettings;
    document.visualTreeAsset = _visualTree;

    _root  = document.rootVisualElement;
    _value = _root.Q<Label>("value");
    _fill  = _root.Q<VisualElement>("fill");

    LoadingProgress progress = new();
    progress.Progressed += OnProgressed;
    progress.LoadingCompleted += FadeOut;
    BindProgress(progress);

    progress.HoldShow(this);
    progress.HoldHide(this);

    progress.HoldCompletion(this);
    _root.schedule.Execute(() => progress.ReleaseCompletion(this)).StartingIn((long)(_minimumSeconds * 1000f));

    Fade(0, 1, () => progress.ReleaseShow(this));

    return SceneOperationPump.Completed(operation);
  }

  void FadeOut() => Fade(1, 0, () => Progress.ReleaseHide(this));

  public override void Dispose()
  {
    if (_instance != null)
      Object.Destroy(_instance);
    base.Dispose();
  }
}

await MySceneManager.TransitionAsync("target", new UIDocumentLoadingScreen(uxml, panelSettings));
```

The fades run through UI Toolkit's own scheduler, so the screen needs no `MonoBehaviour` to run a coroutine.

## Loading Scene Sample

Every screen on this page is in the [Loading Scene Examples](../samples/loading-scene-examples.md) sample as a working, runnable reference: the uGUI `Loading_Screen` scene, the UI Toolkit `Loading_Animated` scene, `PrefabLoadingScreen` and `UIDocumentLoadingScreen`.

[MonoBehaviour]: https://docs.unity3d.com/Manual/class-MonoBehaviour.html
[GameObject]: https://docs.unity3d.com/Manual/class-GameObject.html
[Canvas]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-Canvas.html
[CanvasScaler]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-CanvasScaler.html
[Image]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Image.html
[Text]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Text.html
[UI Legacy Text]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Text.html
[UI Text Mesh Pro]: https://docs.unity3d.com/Packages/com.unity.textmeshpro@3.0/api/TMPro.TextMeshProUGUI.html
[UI Slider]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Slider.html
[Slider]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/script-Slider.html
[UI Canvas Group]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-CanvasGroup.html
[CanvasGroup]: https://docs.unity3d.com/Packages/com.unity.ugui@1.0/manual/class-CanvasGroup.html
