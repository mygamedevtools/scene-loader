# Scene references and parameters — full reference

Read this when you need a reference kind not shown in the main skill, or when a string resolves to an unexpected backend.

## SceneRef kinds

| Kind | Created by | Loads? | Unloads? | Backend |
|---|---|---|---|---|
| `Key` | `SceneRef.FromKey(string)` or an implicit `string` | Yes, after resolution | Yes | Resolved to `BuildIndex` or `Address` when the operation starts |
| `BuildIndex` | `SceneRef.FromBuildIndex(int)` or an implicit `int` | Yes | Yes | Standard (`SceneManager.LoadSceneAsync(…, LoadSceneMode.Additive)`) |
| `Scene` | `SceneRef.FromScene(Scene)` or an implicit `Scene` | **No** | Yes (exact match) | Standard |
| `Address` | `SceneRef.Address(string)` | Yes | Yes | Addressables (`Addressables.LoadSceneAsync(…, LoadSceneMode.Additive)`) |
| `AssetReference` | `SceneRef.FromAssetReference(AssetReference)` or an implicit `AssetReference` | Yes | Yes | Addressables |
| `None` | `default(SceneRef)` | No | No | — |

Factory validation (thrown at the call site, before any operation exists):

- `FromKey` / `Address` with a null, empty or whitespace string → `ArgumentException`.
- `FromBuildIndex` with a negative index → `ArgumentException`.
- `FromScene` with an invalid `Scene` → `ArgumentException`.
- `FromAssetReference` with `null` → `ArgumentNullException`; with an invalid runtime key → `ArgumentException`.

## How a string is resolved

1. Strings are looked up against the **enabled** Build Settings entries, under two keys per scene: the full asset path (`Assets/Scenes/Level1.unity`) and the file name without extension (`Level1`). A match becomes a `BuildIndex` that still remembers the original string, so unloading by the same string matches.
2. With Addressables installed, a string the Build Settings do not have is probed with `Addressables.LoadResourceLocationsAsync`. If it has a location, it becomes an `Address`. This is the only case that waits (catalog initialization on first use).
3. Otherwise the operation faults with `ArgumentException`: "Could not resolve the scene '…'. It was not found in the build settings or the Addressables catalog." Without Addressables the message says only the Build Settings were searched.
4. Answers are cached per string. The cache is dropped automatically when the number of Build Settings scenes changes; `SceneRefResolver.Invalidate()` drops it manually.
5. When a Build Settings match is also an Addressables key (in an already-loaded catalog), a warning is logged and the Build Settings win.

Resolution happens for unloads too, so `UnloadAsync("x")` matches a scene loaded by `LoadAsync("x")` even when the address and the scene name differ.

## SceneParameters

| Construct | Scenes | Active |
|---|---|---|
| implicit from `string`, `int`, `Scene`, `SceneRef`, `AssetReference` | one | none |
| implicit from `string[]`, `int[]`, `Scene[]`, `SceneRef[]`, `AssetReference[]` | many | none |
| `new SceneParameters(SceneRef sceneRef, bool setActive)` | one | that one if `true` |
| `new SceneParameters(params SceneRef[] sceneRefs)` | many | none |
| `new SceneParameters(SceneRef[] sceneRefs, int setIndexActive)` | many | index, or none if negative |
| `new SceneParameters(string[] / int[] / Scene[], int setIndexActive)` | many | index, or none if negative |
| `SceneParameters.FromAssetReferences(AssetReference[], int setIndexActive)` | many | index, or none if negative |

An empty or null array throws `ArgumentException`; `setIndexActive >= Length` throws `ArgumentOutOfRangeException`. `TransitionAsync` activates index 0 when the parameters name none.

Read-back: `Length`, `GetSceneRef()` (first), `GetSceneRefs()`, `ShouldSetActive()`, `GetIndexToActivate()`.

## SceneResult

Returned by awaiting an operation or read from `SceneOperation.Result` after completion. `GetScene()` returns the first scene, `GetScenes()` all of them in request order; it converts implicitly to `Scene` and `Scene[]`. Before completion `Result` is `default` and `GetScenes()` returns `null`.
