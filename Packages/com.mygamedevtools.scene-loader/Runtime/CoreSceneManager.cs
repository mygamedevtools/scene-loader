using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// The <see cref="CoreSceneManager"/> is capable of managing both addressable and non-addressable scene operations.
    /// </summary>
    public class CoreSceneManager : ISceneManager
    {
        public event Action<Scene, Scene> ActiveSceneChanged;
        public event Action<Scene> SceneUnloaded;
        public event Action<Scene> SceneLoaded;
        public event Action<SceneOperation> OperationStarted;

        public int LoadedSceneCount => _loadedScenes.Count;
        public int TotalSceneCount => _loadedScenes.Count + _unloadingScenes.Count;

        readonly List<SceneBackendHandle> _unloadingScenes = new();
        readonly List<SceneBackendHandle> _loadedScenes = new();
        // Live operations, so Dispose can cancel them — replacing v4's lifetime
        // CancellationTokenSource and the linked source, closure and registration per call.
        readonly List<SceneOperation> _liveOperations = new();

        // Identified by the Scene itself: handles are values now, so there is no reference to
        // compare, and the scene handle is the identity the engine uses.
        Scene _activeScene;

        /// <summary>
        /// Creates a <see cref="CoreSceneManager"/> with no initial scene references.
        /// </summary>
        public CoreSceneManager() : this(false) { }
        /// <summary>
        /// Creates a new <see cref="CoreSceneManager"/> with the option to add all loaded scenes to its management.
        /// The advantage is that you can manage those scenes through this <see cref="ISceneManager"/> instead of having to
        /// use the Unity <see cref="SceneManager"/>.
        /// </summary>
        public CoreSceneManager(bool addLoadedScenes)
        {
            if (!addLoadedScenes)
            {
                return;
            }

            int loadedSceneCount = SceneManager.sceneCount;
            for (int i = 0; i < loadedSceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded)
                {
                    _loadedScenes.Add(CreateHandleForLoadedScene(scene));
                }
            }

            if (loadedSceneCount > 0)
            {
                Scene activeScene = SceneManager.GetActiveScene();
                if (IsTracked(activeScene))
                    _activeScene = activeScene;
            }
            else
            {
                SceneManagerLog.Warning("Tried to create a Scene Manager with all loaded scenes, but encoutered none. Did you create the Scene Manager on `Awake()`? If so, try moving the call to `Start()` instead.");
            }
        }
        /// <summary>
        /// Creates a new <see cref="CoreSceneManager"/> with the option to add a list of loaded scenes to its management.
        /// The advantage is that you can manage those scenes through this <see cref="ISceneManager"/> instead of having to
        /// use the Unity <see cref="SceneManager"/>.
        /// </summary>
        public CoreSceneManager(Scene[] initializationScenes)
        {
            if (initializationScenes == null || initializationScenes.Length == 0)
            {
                throw new ArgumentException($"Trying to create an {nameof(CoreSceneManager)} with a null or empty array of initialization scenes. If you want to create it without any scenes, use the empty constructor instead.", nameof(initializationScenes));
            }

            for (int i = 0; i < initializationScenes.Length; i++)
            {
                Scene scene = initializationScenes[i];
                if (scene.IsValid() && scene.isLoaded)
                {
                    _loadedScenes.Add(CreateHandleForLoadedScene(scene));
                }
            }
            if (_loadedScenes.Count > 0)
            {
                _activeScene = _loadedScenes[0].Scene;
            }
        }

        /// <summary>
        /// Cancels everything in flight and forgets every tracked scene. The Unity operations keep
        /// running — they simply stop being this manager's.
        /// </summary>
        public void Dispose()
        {
            for (int i = _liveOperations.Count - 1; i >= 0; i--)
                _liveOperations[i].Cancel();

            _liveOperations.Clear();
            _unloadingScenes.Clear();
            _loadedScenes.Clear();
        }

        public void SetActiveScene(Scene scene)
        {
            bool isTargetSceneValid = scene.IsValid();
            if (isTargetSceneValid && !IsTracked(scene))
                throw new InvalidOperationException($"[{GetType().Name}] Cannot set active the scene \"{scene.name}\" ({scene.handle}) that has not been loaded through this {GetType().Name}.");

            Scene previousScene = _activeScene;
            _activeScene = isTargetSceneValid ? scene : default;
            if (isTargetSceneValid)
                SceneManager.SetActiveScene(scene);

            ActiveSceneChanged?.Invoke(previousScene, scene);
        }

        public Scene GetActiveScene() => _activeScene;

        public Scene GetLastLoadedScene()
        {
            for (int i = _loadedScenes.Count - 1; i >= 0; i--)
            {
                SceneBackendHandle handle = _loadedScenes[i];
                if (!_unloadingScenes.Contains(handle) && handle.Scene.isLoaded)
                    return handle.Scene;
            }

            return default;
        }

        public Scene GetLoadedSceneAt(int index) => _loadedScenes[index].Scene;

        public Scene GetLoadedSceneByName(string name)
        {
            foreach (SceneBackendHandle handle in _loadedScenes)
                if (handle.Scene.name == name)
                    return handle.Scene;
            throw new ArgumentException($"[{GetType().Name}] Could not find any loaded scene with the name '{name}'.", nameof(name));
        }

        public SceneOperation LoadAsync(SceneParameters sceneParameters)
        {
            SceneOperation operation = StartOperation(SceneOperationKind.Load);
            _ = RunAsync(operation, LoadInternalAsync(operation, sceneParameters));
            return operation;
        }

        public SceneOperation UnloadAsync(SceneParameters sceneParameters)
        {
            SceneOperation operation = StartOperation(SceneOperationKind.Unload);
            _ = RunAsync(operation, UnloadInternalAsync(operation, sceneParameters.GetSceneRefs()));
            return operation;
        }

        public SceneOperation TransitionAsync(SceneParameters sceneParameters, SceneRef loadingScene = default)
        {
            // A transition always has to activate something — it unloads the scene you came
            // from. Every v4 transition overload defaulted its `setIndexActive` to 0 for that
            // reason, and the conversions that make `TransitionAsync("target", "loading")`
            // compile cannot carry an index, so the default lives here now.
            if (!sceneParameters.ShouldSetActive())
                sceneParameters = new SceneParameters(sceneParameters.GetSceneRefs(), 0);

            SceneOperation operation = StartOperation(SceneOperationKind.Transition);
            _ = RunAsync(operation, TransitionInternalAsync(operation, sceneParameters, loadingScene));
            return operation;
        }

        public SceneOperation ReloadActiveSceneAsync(SceneRef loadingScene = default)
        {
            if (!_activeScene.IsValid() || !_activeScene.isLoaded || !TryGetTrackedHandle(_activeScene, out SceneBackendHandle activeHandle))
                throw new InvalidOperationException($"[{GetType().Name}] Cannot reload the active scene because it is null or not loaded. Make sure to load a scene before trying to reload it.");

            SceneRef targetScene = activeHandle.SceneRef;
            if (targetScene.Kind == SceneRefKind.Scene)
            {
                // The active scene was handed to this manager already loaded, so its reference
                // can only unload. Fall back to its asset path, which resolves like any other
                // key — this is what makes reloading the very first scene work at all.
                targetScene = SceneRef.FromKey(activeHandle.Scene.path);
            }

            SceneOperation operation = StartOperation(SceneOperationKind.Reload);
            _ = RunAsync(operation, TransitionInternalAsync(operation, new SceneParameters(targetScene, true), loadingScene));
            return operation;
        }

        SceneOperation StartOperation(SceneOperationKind kind)
        {
            SceneOperation operation = new(kind);
            _liveOperations.Add(operation);
            operation.Completed += RemoveLiveOperation;

            // Checked here, unlike the cold sites: this runs once per operation with Info
            // off by default, so the message would be built and dropped on the measured path.
            if (SceneManagerLog.Level >= SceneLogLevel.Info)
                SceneManagerLog.Info($"{kind} operation started.");

            OperationStarted?.Invoke(operation);
            return operation;
        }

        void RemoveLiveOperation(SceneOperation operation) => _liveOperations.Remove(operation);

        /// <summary>
        /// Runs an operation's body, funnelling anything it throws into the handle. Nothing awaits
        /// the returned task, so an unobserved exception would otherwise vanish.
        /// </summary>
        static async Task RunAsync(SceneOperation operation, Task body)
        {
            try
            {
                await body;
            }
            catch (Exception exception)
            {
                // Fault is a no-op once the operation has finished, so an exception thrown after
                // that point would leave no trace at all — nothing awaits this task.
                if (operation.IsDone)
                    SceneManagerLog.Error($"{operation.Kind} operation threw after it had already finished as {operation.State}: {exception}");
                else
                    operation.Fault(exception);
            }
        }

        async Task LoadInternalAsync(SceneOperation operation, SceneParameters sceneParameters)
        {
            SceneRef[] sceneRefs = await ResolveAsync(operation, sceneParameters.GetSceneRefs());
            if (operation.IsCancellationRequested)
                return;

            SceneBackendHandle[] handles = await LoadScenesAsync(operation, sceneRefs, sceneParameters.GetIndexToActivate());
            if (operation.IsCancellationRequested)
                return;

            operation.Complete(new SceneResult(SceneLinker.GetScenes(handles)));
        }

        async Task UnloadInternalAsync(SceneOperation operation, SceneRef[] sceneRefs)
        {
            if (sceneRefs == null || sceneRefs.Length == 0)
                throw new ArgumentException($"[{GetType().Name}] Provided scene group is null or empty.", nameof(sceneRefs));

            SceneBackendHandle[] handles = await UnloadScenesAsync(operation, sceneRefs);
            if (operation.IsCancellationRequested)
                return;

            operation.Complete(new SceneResult(SceneLinker.GetScenes(handles)));
        }

        async Task TransitionInternalAsync(SceneOperation operation, SceneParameters sceneParameters, SceneRef loadingScene)
        {
            // Resolve everything first, so a bad reference fails before any scene moves.
            SceneRef[] targetRefs = await ResolveAsync(operation, sceneParameters.GetSceneRefs());
            if (operation.IsCancellationRequested)
                return;

            LoadingProgress screen = null;
            SceneBackendHandle[] loadingSceneHandles = null;

            if (loadingScene.IsValid)
            {
                loadingSceneHandles = await LoadScenesAsync(operation, await ResolveAsync(operation, new[] { loadingScene }));
                if (operation.IsCancellationRequested)
                    return;

                screen = FindLoadingProgress(loadingSceneHandles[0].Scene);
            }

            // Unity cannot have zero loaded scenes, so a transition with no loading screen and a
            // single loaded scene needs somewhere to stand while the swap happens.
            Scene tempScene = default;
            if (loadingSceneHandles == null && LoadedSceneCount <= 1)
                tempScene = SceneManager.CreateScene("temp-transition-scene");

            if (screen != null)
            {
                operation.SetState(SceneOperationState.ScreenIn);
                await screen.WaitForShowAsync(operation);
                if (operation.IsCancellationRequested)
                    return;
            }

            await UnloadSourceSceneAsync(operation);
            if (operation.IsCancellationRequested)
                return;

            SceneBackendHandle[] handles = await LoadScenesAsync(operation, targetRefs, sceneParameters.GetIndexToActivate(), screen);
            if (operation.IsCancellationRequested)
                return;

            if (screen != null)
            {
                screen.SetLoadingCompleted();
                operation.SetState(SceneOperationState.ScreenOut);
                await screen.WaitForHideAsync(operation);
                if (operation.IsCancellationRequested)
                    return;
            }

            if (loadingSceneHandles != null)
            {
                await UnloadScenesAsync(operation, new[] { SceneRef.FromScene(loadingSceneHandles[0].Scene) });
                if (operation.IsCancellationRequested)
                    return;
            }

            if (tempScene.IsValid())
            {
                SceneBackendHandle tempHandle = CreateHandleForLoadedScene(tempScene);
                SceneBackendHandle[] tempHandles = { tempHandle.Backend.Unload(tempHandle) };
                await SceneOperationPump.WaitForAll(tempHandles, null);
            }

            operation.Complete(new SceneResult(SceneLinker.GetScenes(handles)));
        }

        async Task<SceneRef[]> ResolveAsync(SceneOperation operation, SceneRef[] sceneRefs)
        {
            if (SceneRefResolver.TryResolveAllImmediate(sceneRefs, out SceneRef[] immediate))
                return immediate;

            operation.SetState(SceneOperationState.Resolving);
            return await SceneRefResolver.ResolveAllAsync(sceneRefs);
        }

        async Task<SceneBackendHandle[]> LoadScenesAsync(SceneOperation operation, SceneRef[] sceneRefs, int setIndexActive = -1, LoadingProgress screen = null)
        {
            int scenesToLoad = sceneRefs.Length;
            SceneBackendHandle[] handles = new SceneBackendHandle[scenesToLoad];

            operation.SetState(SceneOperationState.Loading);
            for (int i = 0; i < scenesToLoad; i++)
                handles[i] = SceneBackendRegistry.GetBackend(sceneRefs[i].Kind).Load(sceneRefs[i]);

            if (screen != null)
                operation.Progressed += screen.Report;

            await SceneOperationPump.WaitForAll(handles, operation);

            if (screen != null)
                operation.Progressed -= screen.Report;

            if (operation.IsCancellationRequested)
                return handles;

            operation.SetState(SceneOperationState.Activating);
            SceneLinker.Link(handles, _loadedScenes);

            _loadedScenes.AddRange(handles);
            for (int i = 0; i < scenesToLoad; i++)
            {
                SceneLoaded?.Invoke(handles[i].Scene);
                operation.ReportSceneLoaded(handles[i].Scene);
            }

            if (setIndexActive >= 0)
                SetActiveScene(handles[setIndexActive].Scene);

            return handles;
        }

        async Task<SceneBackendHandle[]> UnloadScenesAsync(SceneOperation operation, SceneRef[] sceneRefs)
        {
            // Unload resolves too, so unloading by the same string that loaded a scene matches
            // it — an address and the scene's name need not be the same word.
            sceneRefs = await ResolveForUnloadAsync(operation, sceneRefs);

            int sceneCount = sceneRefs.Length;
            SceneBackendHandle[] handles = SceneLinker.GetTrackedHandles(sceneRefs, _loadedScenes);

            operation.SetState(SceneOperationState.Unloading);
            for (int i = 0; i < sceneCount; i++)
            {
                SceneBackendHandle handle = handles[i];
                _loadedScenes.Remove(handle);

                handle = handle.Backend.Unload(handle);
                handles[i] = handle;
                _unloadingScenes.Add(handle);
            }

            await SceneOperationPump.WaitForAll(handles, null);

            for (int i = 0; i < sceneCount; i++)
            {
                _unloadingScenes.Remove(handles[i]);
                SceneUnloaded?.Invoke(handles[i].Scene);
                operation.ReportSceneUnloaded(handles[i].Scene);
                if (_activeScene == handles[i].Scene)
                    SetActiveScene(GetLastLoadedScene());
            }

            return handles;
        }

        /// <summary>
        /// Resolves references for an unload, leaving unresolvable keys alone — the "no loaded
        /// scene matches this" error below says more than "not in the build settings" would.
        /// </summary>
        async Task<SceneRef[]> ResolveForUnloadAsync(SceneOperation operation, SceneRef[] sceneRefs)
        {
            try
            {
                return await ResolveAsync(operation, sceneRefs);
            }
            catch (ArgumentException)
            {
                return sceneRefs;
            }
        }

        async Task UnloadSourceSceneAsync(SceneOperation operation)
        {
            Scene sourceScene = GetActiveScene();
            if (!sourceScene.IsValid())
                return;

            await UnloadScenesAsync(operation, new[] { SceneRef.FromScene(sourceScene) });
        }

        static LoadingProgress FindLoadingProgress(Scene loadingScene)
        {
#if UNITY_6000_5_OR_NEWER
            LoadingBehavior[] loadingBehaviors = UnityEngine.Object.FindObjectsByType<LoadingBehavior>();
#else
            LoadingBehavior[] loadingBehaviors = UnityEngine.Object.FindObjectsByType<LoadingBehavior>(UnityEngine.FindObjectsSortMode.None);
#endif
            LoadingBehavior loadingBehavior = loadingBehaviors.FirstOrDefault(l => l.gameObject.scene == loadingScene);
            return loadingBehavior ? loadingBehavior.Progress : null;
        }

        /// <summary>Wraps an already-loaded scene the manager did not load itself.</summary>
        static SceneBackendHandle CreateHandleForLoadedScene(Scene scene)
        {
            SceneRef sceneRef = SceneRef.FromScene(scene);
            return SceneBackendHandle.ForStandard(SceneBackendRegistry.GetBackend(sceneRef.Kind), sceneRef, scene, null);
        }

        bool IsTracked(Scene scene) => TryGetTrackedHandle(scene, out _);

        bool TryGetTrackedHandle(Scene scene, out SceneBackendHandle handle)
        {
            foreach (SceneBackendHandle tracked in _loadedScenes)
            {
                if (tracked.Scene != scene)
                    continue;

                handle = tracked;
                return true;
            }

            handle = default;
            return false;
        }
    }
}
