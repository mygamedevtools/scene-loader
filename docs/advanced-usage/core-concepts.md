---
sidebar_position: 1
description: An introduction to the advanced usage of My Scene Manager.
---

# Core Concepts

There are some key structures that need to be understood in order to dive deep into the logic of the My Scene Manager.

## Architecture

This is an overview of the My Scene Manager architecture. We will dive into each individual component in the next pages.
Consider this flowchart:

```mermaid
flowchart TB
  asm(My Scene Manager)
  sd(Core Scene Manager)
  isd([ISceneManager])
  so{{Load, Unload or Transition}}
  sp(SceneParameters)
  sr([SceneRef])
  res(SceneRefResolver)

  asm ==> sd
  sd o--o isd
  sd === so
  sr o--o sp
  sp o==o so
  sr -.- res

  reg(SceneBackendRegistry)
  be([ISceneBackend])
  h(SceneBackendHandle)
  op(SceneOperation)
  pump(SceneOperationPump)
  result(SceneResult)

  so === reg
  reg ==> be
  be ==> h
  h -.- pump
  so ==> op
  op === result
```

- The `MySceneManager` is a static implementation of a `CoreSceneManager`, which contains all the logic to perform **Scene Operations**.
- The `CoreSceneManager` is an implementation of the `ISceneManager` interface, which defines exactly **four** async methods — `LoadAsync`, `UnloadAsync`, `TransitionAsync` and `ReloadActiveSceneAsync`. Everything else is reachable through implicit conversions rather than through more overloads.
- The `SceneParameters` struct is an abstraction to handle a single `SceneRef` or multiple (`SceneRef[]`), plus which one to activate.
- The `SceneRef` struct is a reference to a scene. It is a single struct with a `SceneRefKind` discriminator rather than a family of types, which is what keeps build indices from boxing.
- A bare string is a **`Key`**, which the `SceneRefResolver` settles into a build index or an address before the operation runs.
- The `SceneBackendRegistry` picks an `ISceneBackend` for each resolved kind — the standard Unity Scene Manager or Addressables — and the backend hands back a `SceneBackendHandle`.
- The `SceneOperationPump` ticks live handles on the player loop, which is what reports progress and resumes awaiters without a `Task` round-trip.
- Every operation returns a `SceneOperation` **immediately**, synchronously, as a live handle on the work. A completed one carries a `SceneResult`, that can hold a single or multiple scenes.

:::info
**Scene Operations** refer to the Load, Unload and Transition operations.
A Reload operation is considered a Transition operation.
:::

## What changed from 4.x

If you are coming from `4.x`, three of these are new names for things you already know:

| 4.x | 5.x |
|---|---|
| `ILoadSceneInfo` and its five implementations | [`SceneRef`](./scene-ref.md), one struct |
| `ISceneData`, `SceneDataBuilder` | [`ISceneBackend`](./scene-backend.md) and `SceneBackendHandle` |
| `IAsyncSceneOperation`, `Task<SceneResult>` | [`SceneOperation`](./scene-operation.md) |

The 64 public methods of `4.x` collapse to the four above. See the [upgrade guide](../upgrades/from-4-to-5.md) for the full mapping.

We will cover each of these structures in the next pages.
