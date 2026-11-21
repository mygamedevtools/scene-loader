# Custom `LoadingScreen` subclasses — detail

Read this when writing a loading screen that is not a scene (prefab, UI Toolkit, or anything procedural), or when changing how a screen shows and hides.

## The contract

```csharp
public abstract class LoadingScreen : IDisposable
{
    protected LoadingProgress Progress { get; }
    protected void BindProgress(LoadingProgress progress);

    public abstract SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation);
    public virtual SceneOperationPump.ConditionAwaiter ShowAsync(SceneOperation operation);   // waits for the show gate
    public virtual void ReportProgress(float progress);                                      // forwards to Progress.Report
    public virtual SceneOperationPump.ConditionAwaiter HideAsync(SceneOperation operation);   // raises LoadingCompleted, waits for the hide gate
    public virtual void Dispose();                                                            // always called: success, fault, cancel
}
```

- `PrepareAsync` is the only required member. Build the screen here and bind a `LoadingProgress` with `BindProgress`. Return `SceneOperationPump.Completed(operation)` when preparation is synchronous.
- Binding `null` (or nothing) means the screen gates nothing: show and hide return immediately.
- Do not override `ShowAsync` / `HideAsync` unless replacing the gate model; the defaults wait on `Progress.WaitForShowAsync` / `WaitForHideAsync`.
- `Dispose` must destroy whatever was created and call `base.Dispose()`. It runs on success, fault and cancellation.
- Create a new instance per transition. An instance holds per-transition state (`_instance`, the bound progress).

## Call order inside a transition

1. `PrepareAsync(host, operation)` — after target references resolve, before anything unloads.
2. Operation enters `ScreenIn`; `ShowAsync` waits for the show gate.
3. Source scene unloads; targets load; `ReportProgress` receives 0..1 while they load.
4. Targets are linked and activated.
5. Operation enters `ScreenOut`; `HideAsync` calls `LoadingCompleted` (after any completion holds release) and waits for the hide gate.
6. `Dispose`, holder scene unloads, operation `Completed`.

## The holder scene

`LoadingScreenHost` is a scene named `loading-screen-host`, created on demand for one transition. `host.Adopt(go)` moves a GameObject into it (creating it if needed), so the object survives the unload of the scene the transition leaves. It is also created when a transition without a scene-based screen would otherwise leave Unity with no loaded scene. It is unloaded before the operation completes.

## Holds on a self-owned `LoadingProgress`

```csharp
LoadingProgress progress = new();
progress.Progressed += OnProgressed;      // 0..1, clamped
progress.LoadingCompleted += FadeOut;     // cue to start hiding
BindProgress(progress);

progress.HoldShow(this);                  // released when the fade-in ends
progress.HoldHide(this);                  // released when the fade-out ends
progress.HoldCompletion(this);            // released after the minimum display time
```

- Take every hold before `PrepareAsync` returns; the transition reads the show gate right after.
- `owner` must be non-null (`ArgumentNullException` otherwise) and is compared by reference.
- A `UnityEngine.Object` owner that is destroyed while holding is dropped with a warning instead of blocking forever. A plain C# owner (like a `LoadingScreen` subclass) is never auto-dropped — always release.
- After `SceneOperationPump.GateWarningSeconds` (10 s) a development build logs which owner is blocking, and keeps waiting.

## The sample's UI Toolkit screen

`Samples/LoadingSceneExamples/Scripts/Runtime/UIDocumentLoadingScreen.cs` (copied to `Assets/Samples/My Scene Manager/<version>/Loading Scene Examples/Scripts/Runtime/` on import). Constructor: `UIDocumentLoadingScreen(VisualTreeAsset visualTree, PanelSettings panelSettings, float fadeSeconds = .25f, float minimumSeconds = 2f)`.

Structure of its `PrepareAsync`:

1. `new GameObject(...)`, `host.Adopt(it)`, add `UIDocument`, assign `panelSettings`, `visualTreeAsset`, `sortingOrder = 50` (above other documents on the same panel).
2. Query a `Label` named `value` and a `VisualElement` named `fill` for progress display.
3. Create and bind a `LoadingProgress`, hold show and hide, hold completion and release it with `rootVisualElement.schedule.Execute(...).StartingIn(minimumMs)`.
4. Fade opacity with the UI Toolkit scheduler on wall-clock `Time.unscaledTime` (not accumulated deltas, which under-count when the scheduler interval exceeds the frame time), releasing show at the end of the fade-in and hide at the end of the fade-out.

Copy it into the project's own folder and rename it when customizing, so re-importing the sample does not overwrite changes. Its UXML (`UI/LoadingScreenMinimal.uxml`) shows the expected `value` and `fill` element names.

## Scene-based screens

`SceneLoadingScreen` is what every implicit conversion produces (`string`, `int`, `Scene`, `SceneRef`, `AssetReference` → `LoadingScreen`). After the transition loads the scene, it binds the `LoadingBehavior` found in that scene through `LoadingBehaviorRegistry.TryGet(scene, …)`. It is `sealed`; to customize a scene screen, change the components in the scene rather than subclassing.
