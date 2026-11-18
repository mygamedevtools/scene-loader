---
sidebar_position: 5
description: Understand how backends dispatch scene operations.
---

# Scene Backend

A **backend** is the thing that actually loads a scene. There are two in the box — the Unity Scene Manager and Addressables — and `ISceneBackend` is what makes that choice a lookup rather than a branch.

You will not touch this to load a scene. It matters when you want to know *how* a scene gets loaded, or when you want to add a way of your own.

## The interface

```cs
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

The addressable-or-not decision happens **once** per operation, when the resolved `SceneRefKind` is handed to the registry:

```mermaid
flowchart LR
  sr([SceneRef])
  res(SceneRefResolver)
  reg(SceneBackendRegistry)
  std(StandardSceneBackend)
  add(AddressablesSceneBackend)
  h(SceneBackendHandle)

  sr --> res
  res -->|resolved kind| reg
  reg --> std
  reg --> add
  std --> h
  add --> h
```

| Kind | Backend |
|---|---|
| `BuildIndex`, `Scene` | `StandardSceneBackend` |
| `Address`, `AssetReference` | `AddressablesSceneBackend` |
| `Key`, `None` | Rejected — reaching selection with an unresolved key means the resolver was skipped |

## The one honest difference between them

`TryResolveScene` is the only method whose answer genuinely differs per backend, and it differs by **returning `false`** rather than by warning and handing back a default:

- `AddressablesSceneBackend` gets a `SceneInstance` back from Addressables, so it can name its own scene directly.
- `StandardSceneBackend` cannot. The Unity Scene Manager has no API that says "this `AsyncOperation` produced that `Scene`", so the honest answer is "no", and the scene is matched afterwards by the linker.

Returning `false` rather than a default `Scene` is what keeps that matching step explicit instead of silent.

## Handles

`SceneBackendHandle` is a **readonly struct** — a value, not an object — carrying the backend that owns it, the `SceneRef` it came from, the `Scene` once known, and the underlying Unity operation.

Handles are ticked by the `SceneOperationPump`, a single player-loop pass over every live operation. That is what raises `Progressed` — and only when the value has actually moved past a small epsilon, so a bar bound to it does not churn every frame.

## Writing your own backend

`SceneBackendRegistry.Register` puts yours ahead of the defaults — registration order decides precedence, and the last registered wins:

```cs
public class MyBackend : ISceneBackend
{
  public bool CanHandle(SceneRefKind kind) => kind == SceneRefKind.Address;
  // ...
}

SceneBackendRegistry.Register(new MyBackend());
```

:::info
This is the extension point for a different asset-delivery system — a custom bundle pipeline, or a storefront SDK that hands you scenes. Implement six methods, register the backend, and every existing call site keeps working.
:::

:::warning
The registry is static, so a registration made in the editor survives a disabled Domain Reload. Register from a `[RuntimeInitializeOnLoadMethod]` rather than from arbitrary code, and be aware that tests which register a backend need to reset it afterwards.
:::
