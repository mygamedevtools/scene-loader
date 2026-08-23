using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        public int LoadedSceneCount => _loadedScenes.Count;
        public int TotalSceneCount => _loadedScenes.Count + _unloadingScenes.Count;

        readonly List<SceneBackendHandle> _unloadingScenes = new();
        readonly List<SceneBackendHandle> _loadedScenes = new();
        readonly CancellationTokenSource _lifetimeTokenSource = new();

        // The active scene is identified by the Scene itself rather than by a tracked object.
        // Handles are values now, so there is no reference to compare — and the scene handle is
        // the identity the engine itself uses.
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

            int loadedSceneCount = initializationScenes.Length;
            for (int i = 0; i < loadedSceneCount; i++)
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

        public void Dispose()
        {
            _lifetimeTokenSource.Cancel();
            _lifetimeTokenSource.Dispose();

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

        public Task<SceneResult> TransitionAsync(SceneParameters sceneParameters, SceneRef loadingScene = default, CancellationToken token = default)
        {
            // A transition always has to activate something — it unloads the scene you came
            // from. Every v4 transition overload defaulted its `setIndexActive` to 0 for that
            // reason, and the conversions that make `TransitionAsync("target", "loading")`
            // compile cannot carry an index, so the default lives here now.
            if (!sceneParameters.ShouldSetActive())
                sceneParameters = new SceneParameters(sceneParameters.GetSceneRefs(), 0);

            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return loadingScene.IsValid
                ? TransitionWithIntermediateAsync(sceneParameters, loadingScene, linkedSource.Token).RunAndDisposeToken(linkedSource)
                : TransitionDirectlyAsync(sceneParameters, linkedSource.Token).RunAndDisposeToken(linkedSource);
        }

        public Task<SceneResult> ReloadActiveSceneAsync(SceneRef loadingScene = default, CancellationToken token = default)
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

            return TransitionAsync(new SceneParameters(targetScene, true), loadingScene, token);
        }

        public Task<SceneResult> LoadAsync(SceneParameters sceneParameters, IProgress<float> progress = null, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return LoadScenesAsync_Internal(sceneParameters, progress, linkedSource.Token).RunAndDisposeToken(linkedSource);
        }

        public Task<SceneResult> UnloadAsync(SceneParameters sceneParameters, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return UnloadScenesAsync_Internal(sceneParameters.GetSceneRefs(), linkedSource.Token).RunAndDisposeToken(linkedSource);
        }

        async Task<SceneResult> LoadScenesAsync_Internal(SceneParameters sceneParameters, IProgress<float> progress, CancellationToken token)
        {
            // Settle what every bare string actually means before anything is loaded. This is
            // the only place resolution happens on the load path, so backend selection below is
            // a single lookup on an already-decided kind.
            SceneRef[] sceneRefs = await SceneRefResolver.ResolveAllAsync(sceneParameters.GetSceneRefs());

            int setIndexActive = sceneParameters.GetIndexToActivate();
            int scenesToLoad = sceneRefs.Length;

            SceneBackendHandle[] handles = new SceneBackendHandle[scenesToLoad];
            int i;
            for (i = 0; i < scenesToLoad; i++)
            {
                handles[i] = SceneBackendRegistry.GetBackend(sceneRefs[i].Kind).Load(sceneRefs[i]);
            }

            await PollProgressAsync(handles, progress, token);

            token.ThrowIfCancellationRequested();

            SceneLinker.Link(handles, _loadedScenes);

            _loadedScenes.AddRange(handles);
            for (i = 0; i < scenesToLoad; i++)
            {
                SceneLoaded?.Invoke(handles[i].Scene);
            }

            if (setIndexActive >= 0)
                SetActiveScene(handles[setIndexActive].Scene);

            return new SceneResult(SceneLinker.GetScenes(handles));
        }

        async Task<SceneResult> UnloadScenesAsync_Internal(SceneRef[] sceneRefs, CancellationToken token)
        {
            if (sceneRefs == null || sceneRefs.Length == 0)
                throw new ArgumentException($"[{GetType().Name}] Provided scene group is null or empty.", nameof(sceneRefs));

            // Unload resolves too, so that unloading by the same string that loaded a scene
            // matches it — an address and the scene's name are not required to be the same word.
            sceneRefs = await ResolveForUnloadAsync(sceneRefs);

            int sceneCount = sceneRefs.Length;
            SceneBackendHandle[] handles = SceneLinker.GetTrackedHandles(sceneRefs, _loadedScenes);
            Task[] unloadTasks = new Task[sceneCount];

            int i;
            for (i = 0; i < sceneCount; i++)
            {
                SceneBackendHandle handle = handles[i];
                _loadedScenes.Remove(handle);

                handle = handle.Backend.Unload(handle);
                handles[i] = handle;
                _unloadingScenes.Add(handle);

                unloadTasks[i] = UnityTaskUtilities.FromBackendHandle(handle, token);
            }

            try
            {
                await Task.WhenAll(unloadTasks);
            }
            catch (OperationCanceledException)
            {
                // The scenes were already removed from `_loadedScenes` and their unload operations
                // cannot be cancelled, so they are gone either way. Clear the active scene as the
                // successful path does, otherwise it keeps pointing at a scene no longer managed.
                for (i = 0; i < sceneCount; i++)
                {
                    _unloadingScenes.Remove(handles[i]);
                    if (_activeScene == handles[i].Scene)
                        SetActiveScene(GetLastLoadedScene());
                }
                throw;
            }

            for (i = 0; i < sceneCount; i++)
            {
                _unloadingScenes.Remove(handles[i]);
                SceneUnloaded?.Invoke(handles[i].Scene);
                if (_activeScene == handles[i].Scene)
                    SetActiveScene(GetLastLoadedScene());
            }

            return new SceneResult(SceneLinker.GetScenes(handles));
        }

        /// <summary>
        /// Resolves references for an unload, leaving unresolvable keys as they are.
        /// <br/>
        /// A key that matches nothing is a caller error on the load path and throws there. Here
        /// it is only a failed match, and the "no loaded scene matches this" error further down
        /// says far more about what went wrong than "not in the build settings" would.
        /// </summary>
        async Task<SceneRef[]> ResolveForUnloadAsync(SceneRef[] sceneRefs)
        {
            try
            {
                return await SceneRefResolver.ResolveAllAsync(sceneRefs);
            }
            catch (ArgumentException)
            {
                return sceneRefs;
            }
        }

        async Task<SceneResult> TransitionDirectlyAsync(SceneParameters sceneParameters, CancellationToken token)
        {
            // If only one scene is loaded, create a temporary scene for transition.
            Scene tempScene = default;
            if (LoadedSceneCount <= 1)
            {
                tempScene = SceneManager.CreateScene("temp-transition-scene");
            }
            await UnloadSourceSceneAsync(token);

            Scene[] loadedScenes = await LoadAsync(sceneParameters, token: token);

            if (tempScene.IsValid())
            {
                SceneBackendHandle tempHandle = CreateHandleForLoadedScene(tempScene);
                await UnityTaskUtilities.FromBackendHandle(tempHandle.Backend.Unload(tempHandle), token);
            }
            return new SceneResult(loadedScenes);
        }

        async Task<SceneResult> TransitionWithIntermediateAsync(SceneParameters sceneParameters, SceneRef loadingSceneRef, CancellationToken token)
        {
            Scene loadingScene = await LoadAsync(new SceneParameters(loadingSceneRef, false), token: token);
            loadingSceneRef = SceneRef.FromScene(loadingScene);

#if UNITY_6000_5_OR_NEWER
            LoadingBehavior[] loadingBehaviors = UnityEngine.Object.FindObjectsByType<LoadingBehavior>();
#else
            LoadingBehavior[] loadingBehaviors = UnityEngine.Object.FindObjectsByType<LoadingBehavior>(UnityEngine.FindObjectsSortMode.None);
#endif
            LoadingBehavior loadingBehavior = loadingBehaviors.FirstOrDefault(l => l.gameObject.scene == loadingScene);
            return loadingBehavior
                ? await TransitionWithIntermediateLoadingAsync(sceneParameters, loadingSceneRef, loadingBehavior, token)
                : await TransitionWithIntermediateNoLoadingAsync(sceneParameters, loadingSceneRef, token);
        }

        async Task<SceneResult> TransitionWithIntermediateLoadingAsync(SceneParameters sceneParameters, SceneRef loadingSceneRef, LoadingBehavior loadingBehavior, CancellationToken token)
        {
            LoadingProgress progress = loadingBehavior.Progress;
            await progress.TransitionInTask.Task;
            await UnloadSourceSceneAsync(token);

            Scene[] loadedScenes = await LoadAsync(sceneParameters, progress, token);
            progress.SetLoadingCompleted();

            await progress.TransitionOutTask.Task;
            await UnloadAsync(new SceneParameters(loadingSceneRef, false), token);
            return new SceneResult(loadedScenes);
        }

        async Task<SceneResult> TransitionWithIntermediateNoLoadingAsync(SceneParameters sceneParameters, SceneRef loadingSceneRef, CancellationToken token)
        {
            await UnloadSourceSceneAsync(token);
            Scene[] loadedScenes = await LoadAsync(sceneParameters, token: token);
            await UnloadAsync(new SceneParameters(loadingSceneRef, false), token);
            return new SceneResult(loadedScenes);
        }

        async Task PollProgressAsync(SceneBackendHandle[] handles, IProgress<float> progress, CancellationToken token = default)
        {
            bool isDone = false;
            while (!isDone && !token.IsCancellationRequested)
            {
                await Task.Yield();
                isDone = SceneLinker.HasCompletedAll(handles);
                progress?.Report(SceneLinker.GetAverageProgress(handles));
            }
        }

        Task<SceneResult> UnloadSourceSceneAsync(CancellationToken token)
        {
            Scene sourceScene = GetActiveScene();
            if (!sourceScene.IsValid())
                return Task.FromResult<SceneResult>(default);

            return UnloadAsync(new SceneParameters(SceneRef.FromScene(sourceScene), false), token);
        }

        /// <summary>
        /// Wraps an already-loaded scene the manager did not load itself: a scene handed to a
        /// constructor, or the temporary scene a direct transition creates.
        /// </summary>
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
