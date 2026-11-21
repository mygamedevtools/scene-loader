# My Scene Manager messages — complete list

Read this when a Console message or exception from My Scene Manager is not covered in the skill's Common issues. Messages logged through `SceneManagerLog` start with `[MySceneManager] `. Exceptions that fault an operation also produce `[MySceneManager] <Kind> operation faulted during <State>: <exception message>` at `Error`.

`<…>` marks values filled in at runtime.

## Thrown at the call site (before an operation exists)

| Message | Type | Cause | Fix |
|---|---|---|---|
| `[MySceneManager] The static Scene Manager instance is not available before the first scene is fully loaded. Try moving the call to `Start()`, or ask TryGetDefault first.` | `InvalidOperationException` | `MySceneManager` used before the first scene finished loading, or after Play Mode teardown | Call from `Start` or later; use `TryGetDefault` in teardown code |
| `Cannot create a SceneRef from an empty string.` | `ArgumentException` | Null/empty/whitespace scene string | Validate inputs; assign serialized fields |
| `Cannot create a SceneRef from an empty address.` | `ArgumentException` | `SceneRef.Address` with an empty string | As above |
| `Cannot create a SceneRef with a build index lower than 0.` | `ArgumentException` | Negative build index | Use a valid index |
| `Cannot create a SceneRef from an invalid scene.` | `ArgumentException` | A default or unloaded `Scene` value | Pass a loaded scene |
| `Cannot create a SceneRef from an Asset Reference with an invalid Runtime Key: '<key>'.` | `ArgumentException` | Unassigned `AssetReference` field | Assign the reference in the Inspector |
| `Cannot create a SceneParameters from a null or empty SceneRef array.` / `…from a null array.` | `ArgumentException` | Empty or null array | Pass at least one scene |
| `The index to activate is beyond the <n> provided scenes.` | `ArgumentOutOfRangeException` | `setIndexActive >= array length` | Use an index inside the array, or negative for none |
| `[CoreSceneManager] Cannot set active the scene "<name>" (<handle>) that has not been loaded through this CoreSceneManager.` | `InvalidOperationException` | `SetActiveScene` on an untracked scene | Load it through the manager first |
| `[CoreSceneManager] Cannot reload the active scene because it is null or not loaded. Make sure to load a scene before trying to reload it.` | `InvalidOperationException` | `ReloadActiveSceneAsync` while the manager's active scene is invalid or no longer loaded | Load a scene through the manager with activation; do not unload manager scenes through `SceneManager` |
| `Trying to create an CoreSceneManager with a null or empty array of initialization scenes…` | `ArgumentException` | `new CoreSceneManager(Scene[])` with nothing | Use `new CoreSceneManager()` for an empty manager |
| `Cannot combine a null or empty set of operations.` | `ArgumentException` | `WhenAll`/`WhenAny` with nothing | Pass at least one operation |
| `A LoadingProgress hold has to name its owner, so it can be released and reported.` | `ArgumentNullException` | `Hold*(null)` | Pass `this` as the owner |

## Faulting an operation

| Exception message | Cause | Fix |
|---|---|---|
| `Could not resolve the scene '<key>'. It was not found in the build settings or the Addressables catalog. Add it to the build settings, register it as an Addressables entry, or pass an explicit reference.` | String matched no enabled Build Settings name/full path and no Addressables key | Register the scene or correct the string |
| `…It was not found in the build settings (Addressables is not installed, so no address lookup was attempted)…` | Same, without Addressables | Register in the Build Settings or install Addressables |
| `[StandardSceneBackend] Cannot load <ref>. Only a resolved build index can start a non-addressable load.` | Loading with a `Scene` reference | Load by name, path, index or address |
| `[StandardSceneBackend] The Unity Scene Manager refused to load <ref>. Is the scene enabled in the build settings?` | `SceneManager.LoadSceneAsync` returned null | Enable the scene in the Build Settings |
| `[StandardSceneBackend] Cannot unload <handle>, because it was never linked to a loaded scene.` | Internal state lost track of the scene | Avoid unloading through `SceneManager` directly; report if reproducible |
| `[StandardSceneBackend] The Unity Scene Manager refused to unload <handle>.` | The scene was already unloaded elsewhere, or is the last loaded scene | Do not unload manager scenes through `SceneManager`; keep another scene loaded |
| `[AddressablesSceneBackend] Cannot unload <handle>, because its addressable operation is no longer valid.` | The Addressables handle was released elsewhere | Unload addressable scenes only through the manager |
| `Unable to match a managed scene with <ref>. Is the scene loaded?` | Unload target not tracked by the manager | Unload manager-loaded, fully loaded scenes; prefer the `Scene` |
| `Unable to link all scenes to loaded scenes. Linked <x>/<y>. Unlinked: <…>.` | A newly loaded scene could not be identified — usually other code loaded or unloaded scenes through `SceneManager` at the same moment | Route all scene loading through the manager; report with `Verbose` logs if it persists |
| `[CoreSceneManager] Provided scene group is null or empty.` | Unload with no scenes | Pass at least one scene |

## Warnings

| Message | Meaning | Fix |
|---|---|---|
| `The scene '<key>' matches both the build settings and an Addressables entry. The build settings take precedence. Use SceneRef.Address("<key>") to load the addressable one.` | Duplicate registration | Remove one copy, or use `SceneRef.Address` |
| `A transition has been waiting 10 seconds for <holders> to call ReleaseShow()` (or `ReleaseHide()`)`. It will keep waiting, but something is expected to release it.` | Unreleased loading-screen hold | Release the named holder's gate |
| `A transition has been waiting 10 seconds for a gate on <owner>…` | Gate closed with no named holder | Check custom `ShowAsync`/`HideAsync` overrides |
| `A <Kind> operation has been waiting 10 seconds on <scenes>.` | Unity's load/unload has not finished | Large scene or slow Addressables download; check engine/Addressables errors |
| `Found <n> LoadingBehaviors in <scene or object>, and a loading screen can only be driven by one. Using '<name>'.` | More than one `LoadingBehavior` | Keep one per screen |
| `A <Type> was destroyed while holding a loading screen gate. The gate is being released on its behalf.` | A holder was destroyed mid-transition | Usually harmless; release explicitly in `OnDestroy` to silence |
| `<Type> on '<name>' is already bound to a LoadingBehavior. Assign it before the component starts.` | `LoadingScreenComponent.LoadingBehavior` assigned after binding | Assign before `Start`, or not at all |
| `Tried to create a Scene Manager with all loaded scenes, but encoutered none. Did you create the Scene Manager on `Awake()`?…` | `new CoreSceneManager(true)` in `Awake` | Create it in `Start` |

## Errors (logged, not thrown)

| Message | Meaning | Fix |
|---|---|---|
| `<Kind> operation faulted during <State>: <message>` | The operation failed; see the tables above for `<message>` | Fix the underlying cause |
| `A <Kind> operation's <event> subscriber threw, and was contained: <exception>` | Your handler for `Progressed`/`StateChanged`/`Completed`/… threw (`Progressed` is reported once per operation) | Fix the handler; the operation itself continued |
| `<Type> on '<name>' (scene '<scene>') found no LoadingBehavior on itself or its parents, and none was assigned. Disabling it.` | Orphaned loading-screen component | Move under the `LoadingBehavior` or assign it |
| `<Kind> operation threw after it had already finished as <State>: <exception>` | An exception after completion or cancellation (often after a cancel) | Investigate the stack trace; usually a scene script reacting to an unload |
| `The assigned Handler threw, so this went to the console instead: <message> — <exception>` | Custom `ILogHandler` failed | Fix the handler |

## Info and Verbose (diagnostic only)

- `Info`: `<Kind> operation started.`, `<Kind> operation cancelled during <State>. The underlying Unity operations will still run to completion.`
- `Verbose`: `<Kind> operation entered <State>.`, `Resolved '<key>' to <ref>.`, `Linked '<scene>' (<handle>) to <ref> directly.` / `… by matching.`, `Registered a LoadingBehavior for scene '<scene>'.`, `Created the loading screen host scene (<handle>).`
