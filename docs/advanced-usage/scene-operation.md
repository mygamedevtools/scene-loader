---
sidebar_position: 6
description: Understand the SceneOperation handle returned by every operation.
---

# Scene Operation

Every operation returns a `SceneOperation` — **synchronously**, before the work starts. It is a live handle on that work: what phase it is in, how far along it is, what it produced, and how to wait for it.

This replaces `4.x`'s `Task<SceneResult>` and its internal `IAsyncSceneOperation`.

## Why a handle instead of a Task

In `4.x` you had to decide up front whether you wanted progress or cancellation, because they were parameters:

```cs
// 4.x — decided at the call, and there was nothing to attach to afterwards
await MySceneManager.LoadAsync(sceneParameters, progress, token);
```

A `SceneOperation` is something you can hold, so those move off the signature and onto the handle:

```cs
SceneOperation op = MySceneManager.TransitionAsync("target", "loading");

op.Progressed   += progress => bar.value = progress;
op.StateChanged += o => { if (o.State == SceneOperationState.ScreenOut) BeginIntro(); };

SceneResult result = await op;
```

That is what let `IProgress<float>` and `CancellationToken` leave all 64 `4.x` signatures.

## Waiting for it

Three ways, all supported on the same handle:

```cs
SceneResult result = await op;             // direct, no Task allocated
yield return op.ToCoroutine();             // from a coroutine; faults rethrow
Task<SceneResult> task = op.AsTask();      // bridge for third-party interop
```

`await op` is the primary path. `GetAwaiter()` returns a `SceneOperationAwaiter` over the operation's own continuation list — no `Task`, no `Awaitable`. Because the pump runs on the player loop, continuations resume on the main thread by construction, with no `SynchronizationContext` round-trip.

It is also **re-awaitable** — awaiting twice returns the same result, and `op.Result` stays readable after completion. That is the specific reason `Awaitable` is not used internally: its objects return to a pool after a single await.

:::info
`AsTask()` is a convenience, not a design pillar. It costs a `TaskCompletionSource` per call and `await op` does not, so reach for it only when a third-party API demands a `Task`.
:::

## What it reports

| Member | |
|---|---|
| `Kind` | Which operation this is — `Load`, `Unload`, `Transition`, `Reload`, `Composite` |
| `State` | The phase it is in |
| `Progress` | 0 to 1 |
| `Result` | The scenes produced, empty until completed |
| `Exception` | Why it faulted, or `null` |
| `IsDone` | Whether it finished, successfully or not |

And the events:

| Event | |
|---|---|
| `Progressed` | Fires when `Progress` moves. Not raised for unchanged values. |
| `StateChanged` | Fires on every `State` change |
| `SceneLoaded` / `SceneUnloaded` | Once per scene |
| `Completed` | Once when it finishes — success, cancellation and fault alike. Subscribing after completion invokes it immediately. |

:::note
A subscriber that throws is reported through [`SceneManagerLog`](./logging.md) and contained. It will not fault the operation, and it will not prevent the other subscribers or the awaiters from running.
:::

### States

`Pending` → `Resolving` → `ScreenIn` → `Unloading` → `Loading` → `Activating` → `ScreenOut` → `Completed`, with `Canceled` and `Faulted` as terminal alternatives.

Which of these you see depends on the operation — a plain load never reaches `ScreenIn`. The order follows the transition flow, which is why `Unloading` comes before `Loading`: the source scene goes away once the loading screen is up, before the target is brought in.

Knowing when the loading screen is completely gone used to mean locating a `LoadingBehavior` and calling `ContinueWith` on a publicly exposed `TaskCompletionSource`. Now it is a state:

```cs
op.StateChanged += o =>
{
  if (o.State == SceneOperationState.ScreenOut)
    BeginIntroCutscene();
};
```

## Cancelling

```cs
op.Cancel();
op.CancelWith(destroyCancellationToken);   // the opt-in bridge
```

:::warning
**The underlying Unity operations keep running.** They cannot be aborted, which is why `4.x`'s tokens never cancelled the work either — they only cancelled the *await*. A scene already loading will finish; what stops is this operation's reporting, its remaining phases, and its waiters.
:::

## Combining

```cs
SceneOperation both = SceneOperation.WhenAll(first, second);
SceneOperation any  = SceneOperation.WhenAny(first, second);
```

These run over the same continuation lists, so they cost nothing per operation — unlike `Task.WhenAll` over `AsTask()`, which costs a `TaskCompletionSource` each.

## Progress

`Progress` is the average across every scene in the operation.

:::warning
A group mixing backends advances **unevenly**. Addressables includes download time in its progress and the standard path does not, so a mixed group is not a straight line. Treat it as a progress bar, not a clock.
:::

:::note
`SceneOperation` is deliberately **not pooled**. This API encourages you to keep the handle — `op.Result` after completion and awaiting twice are both supported — so nothing can know when it is free. That is one small allocation per operation, against the tens of kilobytes a scene load costs. The per-operation buffers *are* pooled.
:::
