---
sidebar_position: 6
description: Understand the SceneOperation handle returned by every operation.
---

# Scene Operation

Every operation returns a `SceneOperation` — **synchronously**, before the work starts. It is a live handle on that work: what phase it is in, how far along it is, what it produced, and how to wait for it.

## Why a handle and not a Task

A `Task` gives you one thing: the eventual result. Anything else you want to know about a scene load — how far along it is, which phase it is in, whether you can still stop it — has to be decided *before* the call, as extra parameters.

A `SceneOperation` is something you hold instead, so all of that attaches after the call:

```cs
SceneOperation op = MySceneManager.TransitionAsync("target", "loading");

op.Progressed   += progress => bar.value = progress;
op.StateChanged += o => { if (o.State == SceneOperationState.ScreenOut) BeginIntro(); };

SceneResult result = await op;
```

This is why none of the four methods take a progress or cancellation parameter: there is somewhere better to put them.

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

So "the loading screen has finished fading out, start the cutscene" is a state you subscribe to:

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
**The underlying Unity operations keep running.** A scene the engine has started loading cannot be aborted, so it will finish. What stops is this operation's reporting, its remaining phases, and its waiters.
:::

## Combining

```cs
SceneOperation both = SceneOperation.WhenAll(first, second);
SceneOperation any  = SceneOperation.WhenAny(first, second);
```

Prefer these over `Task.WhenAll` on `AsTask()`: they run over the operations' own continuation lists, so they do not allocate a `Task` per operation.

## Progress

`Progress` is the average across every scene in the operation.

:::warning
A group mixing backends advances **unevenly**. Addressables includes download time in its progress and the standard path does not, so a mixed group is not a straight line. Treat it as a progress bar, not a clock.
:::

:::note
`SceneOperation` is deliberately **not pooled**. This API encourages you to keep the handle — `op.Result` after completion and awaiting twice are both supported — so nothing can know when it is free. That is one small allocation per operation, against the tens of kilobytes a scene load costs. The per-operation buffers *are* pooled.
:::
