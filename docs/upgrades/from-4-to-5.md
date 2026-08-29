---
sidebar_position: 3
title: From 4.x to 5.x
description: Upgrade from version 4.x to 5.x
---

# Upgrading from 4.x to 5.x

**Start here: the headline call has not changed.**

```cs
MySceneManager.TransitionAsync("my-target-scene", "my-loading-scene");   // 4.x and 5.x, identical
```

If that is most of what your project does, your migration is small. Most projects face renames
and dropped arguments — find-and-replace scale, not a rearchitecture.

**Addressable calls now look identical to non-addressable ones.** A bare string resolves itself,
so the `*AddressableAsync` family is gone rather than renamed:

```cs
MySceneManager.TransitionAsync("target", "loading");                     // build settings
MySceneManager.TransitionAsync("target-address", "loading-address");     // Addressables
MySceneManager.TransitionAsync(SceneRef.Address("target"), "loading");   // forced, and the fast path
```

There is **no compatibility layer** — no `[Obsolete]` shims, no forwarding methods. That matches
what 3.0 and 4.0 both did, and it means every call site that needs changing produces a plain
compile error at exactly the line to change. **4.x receives no further maintenance**; the answer
to a 4.x bug report is to upgrade.

:::warning[Asset Store users]
Remove the previous version completely before importing 5.0. This has always been true, but a
major version makes it more likely to bite.
:::

## Key changes

* **64 public async methods became 4.** Every reference kind, arity and host is reachable through
  `SceneParameters`' implicit conversions instead of its own method.
* **`SceneRef` replaces `ILoadSceneInfo`** and its five implementing structs — one non-boxing
  value type for names, paths, addresses, build indices, `AssetReference`s and `Scene`s.
* **A bare `string` resolves itself**, against the build settings first and Addressables second.
* **Every operation returns a `SceneOperation`** instead of a `Task<SceneResult>`: progress,
  cancellation, phase and per-scene events all live on the handle.
* **`CancellationToken` and `IProgress<float>` are gone from the public API.**
* **`ISceneBackend` replaces `ISceneData` and `IAsyncSceneOperation`**, so backend selection
  happens once per operation rather than at every call site.
* **Loading screens no longer have to be scenes** — `LoadingScreen` covers prefabs and UI Toolkit
  documents too.
* **Loading screen gates are holds, not toggles.** `waitForScriptedStart` / `waitForScriptedEnd`
  and `StartTransition()` / `EndTransition()` are gone; a component that needs the transition to
  wait takes a hold on the `LoadingProgress` and releases it when done.
* **`LoadingScreenComponent`** is the base for everything that lives on a loading screen. The
  `LoadingBehavior` reference is optional and found on the parents.
* **`SceneManagerLog`** gives the package one configurable, routable logging layer.
* **Fixed:** `LoadingProgress` no longer throws when a transition is started twice — releasing a
  hold twice is harmless.

## Removed types and their replacements

This is the table to read first. Method renames are one IntelliSense keystroke away; removed
types are not — `LoadSceneInfoName` does not autocomplete to `SceneRef`.

### `ILoadSceneInfo` and the `LoadSceneInfo*` structs → `SceneRef`

Also covers `LoadSceneInfoType`.

```cs
// 4.x
ILoadSceneInfo byName    = new LoadSceneInfoName("sceneA");
ILoadSceneInfo byPath    = new LoadSceneInfoName("Assets/Scenes/sceneA.unity");
ILoadSceneInfo byIndex   = new LoadSceneInfoIndex(1);
ILoadSceneInfo byScene   = new LoadSceneInfoScene(someScene);
ILoadSceneInfo byAddress = new LoadSceneInfoAddress("sceneA");
ILoadSceneInfo byAsset   = new LoadSceneInfoAssetReference(assetReference);

// 5.x
SceneRef byName    = "sceneA";                          // implicit
SceneRef byPath    = "Assets/Scenes/sceneA.unity";      // implicit
SceneRef byIndex   = 1;                                 // implicit
SceneRef byScene   = someScene;                         // implicit
SceneRef byAddress = SceneRef.Address("sceneA");        // explicit: forces Addressables
SceneRef byAsset   = assetReference;                    // implicit
```

Most of the time you will not name `SceneRef` at all — the conversions mean you pass the string,
index or `AssetReference` straight to the operation.

### `ISceneData`, `SceneData*`, `SceneDataBuilder`, `SceneDataUtilities` → `ISceneBackend`

Also covers `IAsyncSceneOperation`, `AsyncSceneOperationStandard` and
`AsyncSceneOperationAddressable`.

```cs
// 4.x — half-implemented by design: each type warned when you called the wrong half
public interface ISceneData
{
    IAsyncSceneOperation AsyncOperation { get; }
    void SetSceneReferenceManually(Scene scene);   // warns on the addressable implementation
    void UpdateSceneReference();                   // warns on the standard implementation
    // ...
}

// 5.x — every method is meaningful on every implementation
public interface ISceneBackend
{
    bool CanHandle(SceneRefKind kind);
    SceneBackendHandle Load(SceneRef sceneRef);
    SceneBackendHandle Unload(SceneBackendHandle handle);
    float GetProgress(SceneBackendHandle handle);
    bool IsDone(SceneBackendHandle handle);
    bool TryResolveScene(SceneBackendHandle handle, out Scene scene);
}
```

Register your own with `SceneBackendRegistry.Register(backend)`; it takes precedence over the
built-in backends for the kinds it claims.

### `WaitTask<T>` and `TaskExtensions` → `SceneOperation.ToCoroutine()`

```cs
// 4.x
yield return MySceneManager.LoadAsync("sceneA").ToWaitTask();
yield return new WaitTask<SceneResult>(MySceneManager.LoadAsync("sceneA"));

// 5.x
yield return MySceneManager.LoadAsync("sceneA").ToCoroutine();
```

### `SceneManagerExtensions` → deleted

The 698 lines of extension methods existed to spell out every combination of operation, arity and
reference kind. `SceneParameters`' implicit conversions replace all of them; see the method table
below.

### `waitForScriptedStart` / `waitForScriptedEnd` and `StartTransition()` / `EndTransition()` → holds

In 4.x a loading screen that animated in or out ticked two toggles on the `LoadingBehavior` and
called two triggers on its `LoadingProgress` — and if two components both wanted to gate the same
transition, the first one to call `EndTransition()` released it for both. In 5.x the gates are
**open unless something holds them**: each participant takes a hold of its own and the gate opens
when the last one releases.

```cs
// 4.x — waitForScriptedStart and waitForScriptedEnd ticked in the Inspector
void Awake()
{
    _loadingBehavior.Progress.LoadingCompleted += PlayOut;
    PlayIn();
}
void OnPlayInFinished()  => _loadingBehavior.Progress.StartTransition();
void OnPlayOutFinished() => _loadingBehavior.Progress.EndTransition();

// 5.x — nothing to tick; the holds are the statement that the transition should wait
void Awake()
{
    _loadingBehavior.Progress.HoldShow(this);
    _loadingBehavior.Progress.HoldHide(this);
    _loadingBehavior.Progress.LoadingCompleted += PlayOut;
    PlayIn();
}
void OnPlayInFinished()  => _loadingBehavior.Progress.ReleaseShow(this);
void OnPlayOutFinished() => _loadingBehavior.Progress.ReleaseHide(this);
```

Take the holds in `Awake` or `OnEnable`, before the transition reads the gates. A new
`HoldCompletion` / `ReleaseCompletion` pair delays the `LoadingCompleted` cue itself, which is what
a minimum display time needs. See [Gates and holds](../getting-started/loading-screens.md#gates-and-holds).

The `LoadingFader` takes its own holds now, so a scene that only used it works with no changes
beyond the toggles disappearing from the Inspector.

### `LoadingProgress.TransitionInTask` / `TransitionOutTask` → `WaitForShowAsync()` / `WaitForHideAsync()`

These were public `TaskCompletionSource<bool>` fields, so any consumer could complete them and
desynchronise the transition. If you were reading them to find out when a transition finished a
phase, use `SceneOperation.StateChanged` instead — see [Watching a transition](#watching-a-transition).

```cs
// 4.x
await loadingBehavior.Progress.TransitionInTask.Task;

// 5.x
await loadingBehavior.Progress.WaitForShowAsync();
bool shown = loadingBehavior.Progress.IsShown;
```

### Feedback components' `loadingBehavior` field → `LoadingScreenComponent.LoadingBehavior`

`LoadingFader`, `LoadingFeedbackSlider`, `LoadingFeedbackText` and `LoadingFeedbackTextMeshPro`
now extend `LoadingScreenComponent`. Their public `loadingBehavior` field is a `LoadingBehavior`
property, kept serialized under the old name so existing scenes keep their wiring — and it is
optional, resolved from the same object or its closest parent when left empty.

```cs
// 4.x
slider.loadingBehavior = behavior;

// 5.x — or leave it empty and put the LoadingBehavior on a parent
slider.LoadingBehavior = behavior;
```

If you wrote your own feedback against `LoadingBehavior.Progress`, extend `LoadingScreenComponent`
instead and move the subscription into `OnBound`:

```cs
// 4.x
public class LoadingFeedbackImageFill : MonoBehaviour
{
    public LoadingBehavior loadingBehavior;
    void Start() => loadingBehavior.Progress.Progressed += p => _image.fillAmount = p;
}

// 5.x
public class LoadingFeedbackImageFill : LoadingScreenComponent
{
    protected override void OnBound() => Progress.Progressed += p => _image.fillAmount = p;
}
```

## Every 4.x method and its 5.x equivalent

Each group leads with the case that does not change.

### Load

| 4.x | 5.x |
|---|---|
| `LoadAsync(sceneParameters, progress, token)` | `LoadAsync(sceneParameters)` + `op.Progressed` / `op.CancelWith(token)` |
| `LoadAsync(string sceneName, bool setActive, ...)` | `LoadAsync(sceneName)` — or `LoadAsync(new SceneParameters(sceneName, setActive: true))` |
| `LoadAsync(string[] sceneNames, int setIndexActive, ...)` | `LoadAsync(sceneNames)` — or `LoadAsync(new SceneParameters(sceneNames, setIndexActive))` |
| `LoadAsync(int buildIndex, bool setActive, ...)` | `LoadAsync(buildIndex)` — or `LoadAsync(new SceneParameters((SceneRef)buildIndex, true))` |
| `LoadAsync(int[] buildIndices, int setIndexActive, ...)` | `LoadAsync(buildIndices)` — or `LoadAsync(new SceneParameters(buildIndices, setIndexActive))` |
| `LoadAddressableAsync(string address, bool setActive, ...)` | `LoadAsync(SceneRef.Address(address))` |
| `LoadAddressableAsync(string[] addresses, int setIndexActive, ...)` | `LoadAsync(new SceneParameters(addresses.Select(SceneRef.Address).ToArray(), setIndexActive))` |
| `LoadAddressableAsync(AssetReference assetReference, bool setActive, ...)` | `LoadAsync(assetReference)` |
| `LoadAddressableAsync(AssetReference[] assetReferences, int setIndexActive, ...)` | `LoadAsync(assetReferences)` — or `LoadAsync(new SceneParameters(assetReferences, setIndexActive))` |

A bare address only needs `SceneRef.Address(...)` when the same name also exists in your build
settings; otherwise `LoadAsync(address)` resolves to Addressables on its own.

### Unload

| 4.x | 5.x |
|---|---|
| `UnloadAsync(sceneParameters, token)` | `UnloadAsync(sceneParameters)` |
| `UnloadAsync(string sceneName, token)` | `UnloadAsync(sceneName)` |
| `UnloadAsync(string[] sceneNames, token)` | `UnloadAsync(sceneNames)` |
| `UnloadAsync(int buildIndex, token)` | `UnloadAsync(buildIndex)` |
| `UnloadAsync(int[] buildIndices, token)` | `UnloadAsync(buildIndices)` |
| `UnloadAsync(Scene scene, token)` | `UnloadAsync(scene)` |
| `UnloadAsync(Scene[] scenes, token)` | `UnloadAsync(scenes)` |
| `UnloadAddressableAsync(string address, token)` | `UnloadAsync(SceneRef.Address(address))` |
| `UnloadAddressableAsync(string[] addresses, token)` | `UnloadAsync(addresses.Select(SceneRef.Address).ToArray())` |
| `UnloadAddressableAsync(AssetReference assetReference, token)` | `UnloadAsync(assetReference)` |
| `UnloadAddressableAsync(AssetReference[] assetReferences, token)` | `UnloadAsync(assetReferences)` |

### Transition

| 4.x | 5.x |
|---|---|
| `TransitionAsync(sceneParameters, intermediateSceneReference, token)` | `TransitionAsync(sceneParameters, loadingScreen)` |
| `TransitionAsync(string target, string loading, token)` | `TransitionAsync(target, loading)` — **unchanged** |
| `TransitionAsync(string[] targets, string loading, int setIndexActive, token)` | `TransitionAsync(new SceneParameters(targets, setIndexActive), loading)` |
| `TransitionAsync(int target, int loading, token)` | `TransitionAsync(target, loading)` — **unchanged** |
| `TransitionAsync(int[] targets, int loading, int setIndexActive, token)` | `TransitionAsync(new SceneParameters(targets, setIndexActive), loading)` |
| `TransitionAddressableAsync(string target, string loading, token)` | `TransitionAsync(SceneRef.Address(target), SceneRef.Address(loading))` |
| `TransitionAddressableAsync(string[] targets, string loading, int setIndexActive, token)` | `TransitionAsync(new SceneParameters(targets.Select(SceneRef.Address).ToArray(), setIndexActive), SceneRef.Address(loading))` |
| `TransitionAddressableAsync(AssetReference target, AssetReference loading, token)` | `TransitionAsync(target, loading)` |
| `TransitionAddressableAsync(AssetReference[] targets, AssetReference loading, int setIndexActive, token)` | `TransitionAsync(new SceneParameters(targets, setIndexActive), loading)` |

`setIndexActive` defaulted to `0` on every 4.x transition overload, and a transition still
activates index 0 unless you say otherwise — so dropping the argument keeps the same behaviour.

### Reload

| 4.x | 5.x |
|---|---|
| `ReloadActiveSceneAsync(intermediateSceneReference, token)` | `ReloadActiveSceneAsync(loadingScreen)` |
| `ReloadActiveSceneAsync(string loadingSceneName, token)` | `ReloadActiveSceneAsync(loadingSceneName)` — **unchanged** |
| `ReloadActiveSceneAsync(int loadingBuildIndex, token)` | `ReloadActiveSceneAsync(loadingBuildIndex)` — **unchanged** |
| `ReloadActiveSceneAddressableAsync(string loadingAddress, token)` | `ReloadActiveSceneAsync(SceneRef.Address(loadingAddress))` |
| `ReloadActiveSceneAddressableAsync(AssetReference loadingAssetReference, token)` | `ReloadActiveSceneAsync(loadingAssetReference)` |

## Awaiting, progress and cancellation

Everything that used to be a constructor argument is now something you attach to the handle.

```cs
// 4.x
var progress = new Progress<float>(p => bar.value = p);
var cts = new CancellationTokenSource();
Task<SceneResult> task = MySceneManager.LoadAsync("sceneA", progress: progress, token: cts.Token);
SceneResult result = await task;

// 5.x
SceneOperation op = MySceneManager.LoadAsync("sceneA");
op.Progressed += p => bar.value = p;
SceneResult result = await op;
```

`await op` needs no `Task`. If you need one for third-party interop, `op.AsTask()` gives you one.

Cancellation has one mechanism now:

```cs
op.Cancel();                              // stops this operation
op.CancelWith(destroyCancellationToken);  // opt-in bridge for structured concurrency
```

:::note
Unity scene operations cannot be aborted — 4.x's own documentation said so on all 64 methods, and
the token only ever cancelled the *await*. `Cancel()` stops progress reporting, skips the
remaining phases and completes the operation in `Canceled`; the underlying load still finishes.
:::

## Watching a transition

A `SceneOperation` reports which phase it is in, which is what previously required reaching into
a `LoadingBehavior` and calling `ContinueWith` on a publicly exposed `TaskCompletionSource`:

```cs
SceneOperation op = MySceneManager.TransitionAsync("target", "loading");

op.StateChanged += o =>
{
    if (o.State == SceneOperationState.ScreenOut)
        BeginIntroAnimation();    // the loading screen has finished hiding
};

await op;
```

States run `Pending → Resolving → ScreenIn → Unloading → Loading → Activating → ScreenOut →
Completed`, and an operation skips the phases its kind has no use for.

## Custom loading screens

A loading screen no longer has to be a scene. Everything that worked in 4.x still works — a scene
name, path, address, build index, `Scene` or `AssetReference` all convert to a scene-based screen
— and you can now write your own:

```cs
public class MyScreen : LoadingScreen
{
    public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation op)
    {
        /* instantiate into host, then BindProgress(...) the LoadingProgress that gates it */
        return SceneOperationPump.Completed(op);
    }

    public override void Dispose() { /* tear it down */ base.Dispose(); }
}

await MySceneManager.TransitionAsync("target", new MyScreen());
```

`PrepareAsync` is the only member a screen has to write, plus `Dispose` if it built anything.
Showing, hiding and reporting are driven by the `LoadingProgress` the screen binds — one found on a
`LoadingBehavior`, or one it creates for itself — so every screen gates the same way.

`LoadingScreenHost` is a package-owned scene that exists for the length of one transition, so a
screen that instantiates something has somewhere to put it that survives the outgoing scene being
unloaded. It also replaces 4.x's internal `temp-transition-scene`.

The [Loading Scene Examples](../samples/loading-scene-examples.md) sample ships `PrefabLoadingScreen`
and `UIDocumentLoadingScreen` as reference implementations to copy.

:::note[The sample was rebuilt]
The 4.x sample's `Loading_Fade`, `Loading_Custom` scenes and the `SceneTransitionTrigger`,
`AnimatedTrigger` and `LoadingFeedbackImageFill` scripts are gone. If you copied any of them into
your project, they were written against the removed toggles and triggers — re-import the sample
and start from its 5.x scripts instead.
:::

## String resolution and its precedence

A bare string is resolved when the operation starts:

1. **Build settings**, by name or path. One dictionary lookup, synchronous, and the common case.
2. **Addressables**, if the build settings do not have it and Addressables is installed. This
   needs the catalog, so it is asynchronous — the first addressable-by-string load pays
   catalog-initialisation latency, and later loads of any key hit a cache.
3. **Neither** → an exception naming both places we looked.

**The build settings win.** If `Level1` exists in both, `LoadAsync("Level1")` loads the build
settings one, and `SceneRef.Address("Level1")` is the override.

:::warning[Resolution is observable behaviour]
Adding a scene to the build settings later can flip a string from the addressable backend to the
standard one, with no code change. A key matching both is reported at `Warning` level, and every
first resolution is logged at `Verbose`, so this is diagnosable rather than mysterious.
:::

## Logging

The package now has one logging layer instead of nine scattered `Debug.LogWarning` calls.

```cs
SceneManagerLog.Level = SceneLogLevel.Verbose;   // Off | Error | Warning | Info | Verbose
SceneManagerLog.Handler = myLogHandler;          // route into an in-game console or analytics
```

It defaults to `Warning` in development builds and `Error` in release, and is settable at runtime
so a shipped build can be raised to diagnose a live problem. Define `MSM_DISABLE_LOGGING` to strip
the layer entirely.

`Verbose` is where the scene-linking layer narrates itself — which reference resolved to what, and
which loaded scene got linked to which reference. That is historically the sharpest part of the
package, so it is worth turning on when something links wrongly.

## A note on progress

Progress means slightly different things per backend, and always has. Addressables' progress spans
download, load and activation; the standard path covers load only. A group mixing the two
therefore advances unevenly. This is documented rather than corrected — rescaling one to match the
other would be inventing a number neither backend reports.
