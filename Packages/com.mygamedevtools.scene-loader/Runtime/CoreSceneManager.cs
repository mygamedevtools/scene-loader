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

        readonly List<ISceneData> _unloadingScenes = new();
        readonly List<ISceneData> _loadedScenes = new();
        readonly CancellationTokenSource _lifetimeTokenSource = new();

        ISceneData _activeScene;

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
                    _loadedScenes.Add(SceneDataBuilder.BuildFromScene(scene));
                }
            }

            if (loadedSceneCount > 0 && SceneDataUtilities.TryGetSceneDataByLoadedScene(SceneManager.GetActiveScene(), _loadedScenes, out ISceneData sceneData))
            {
                _activeScene = sceneData;
            }
            else if (loadedSceneCount == 0 && SceneManagerLog.IsEnabled(SceneLogLevel.Warning))
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
                    _loadedScenes.Add(SceneDataBuilder.BuildFromScene(scene));
                }
            }
            if (loadedSceneCount > 0)
            {
                _activeScene = _loadedScenes[0];
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
            ISceneData sceneData = null;
            bool isTargetSceneValid = scene.IsValid();
            if (isTargetSceneValid && !SceneDataUtilities.TryGetSceneDataByLoadedScene(scene, _loadedScenes, out sceneData))
                throw new InvalidOperationException($"[{GetType().Name}] Cannot set active the scene \"{scene.name}\" that has not been loaded through this {GetType().Name}.");

            ISceneData previousScene = _activeScene;
            _activeScene = sceneData;
            if (isTargetSceneValid)
                SceneManager.SetActiveScene(scene);

            ActiveSceneChanged?.Invoke(previousScene != null ? previousScene.SceneReference : default, scene);
        }

        public Scene GetActiveScene() => _activeScene != null ? _activeScene.SceneReference : default;

        public Scene GetLastLoadedScene()
        {
            if (LoadedSceneCount == 0)
                return default;

            for (int i = LoadedSceneCount - 1; i >= 0; i--)
                if (!_unloadingScenes.Contains(_loadedScenes[i]) && _loadedScenes[i].SceneReference.isLoaded)
                    return _loadedScenes[i].SceneReference;

            return default;
        }

        public Scene GetLoadedSceneAt(int index) => _loadedScenes[index].SceneReference;

        public Scene GetLoadedSceneByName(string name)
        {
            foreach (ISceneData sceneData in _loadedScenes)
                if (sceneData.SceneReference.name == name)
                    return sceneData.SceneReference;
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
            if (_activeScene == null || !_activeScene.SceneReference.IsValid() || !_activeScene.SceneReference.isLoaded)
                throw new InvalidOperationException($"[{GetType().Name}] Cannot reload the active scene because it is null or not loaded. Make sure to load a scene before trying to reload it.");

            SceneRef targetScene = _activeScene.SceneRef;
            if (targetScene.Kind == SceneRefKind.Scene)
            {
                // The active scene was handed to this manager already loaded, so its reference
                // can only unload. Fall back to its asset path, which resolves like any other
                // key — this is what makes reloading the very first scene work at all.
                targetScene = SceneRef.FromKey(_activeScene.SceneReference.path);
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
            // a single switch on an already-decided kind.
            SceneRef[] sceneRefs = await SceneRefResolver.ResolveAllAsync(sceneParameters.GetSceneRefs());

            int setIndexActive = sceneParameters.GetIndexToActivate();
            int scenesToLoad = sceneRefs.Length;

            ISceneData[] sceneDataArray = new ISceneData[scenesToLoad];
            int i;
            for (i = 0; i < scenesToLoad; i++)
            {
                sceneDataArray[i] = SceneDataBuilder.BuildFromSceneRef(sceneRefs[i]);
                sceneDataArray[i].LoadSceneAsync();
            }

            await PollProgressAsync(sceneDataArray, progress, token);

            token.ThrowIfCancellationRequested();

            SceneDataUtilities.LinkLoadedScenesWithSceneDataArray(sceneDataArray, _loadedScenes);

            _loadedScenes.AddRange(sceneDataArray);
            for (i = 0; i < scenesToLoad; i++)
            {
                SceneLoaded?.Invoke(sceneDataArray[i].SceneReference);
            }

            if (setIndexActive >= 0)
                SetActiveScene(sceneDataArray[setIndexActive].SceneReference);

            return new SceneResult(SceneDataUtilities.GetScenesFromSceneDataArray(sceneDataArray));
        }

        async Task<SceneResult> UnloadScenesAsync_Internal(SceneRef[] sceneRefs, CancellationToken token)
        {
            if (sceneRefs == null || sceneRefs.Length == 0)
                throw new ArgumentException($"[{GetType().Name}] Provided scene group is null or empty.", nameof(sceneRefs));

            // Unload resolves too, so that unloading by the same string that loaded a scene
            // matches it — an address and the scene's name are not required to be the same word.
            sceneRefs = await ResolveForUnloadAsync(sceneRefs);

            int sceneCount = sceneRefs.Length;
            ISceneData[] sceneDataArray = SceneDataUtilities.GetLoadedSceneDatasWithSceneRefs(sceneRefs, _loadedScenes);
            Task[] unloadTasks = new Task[sceneCount];

            ISceneData tempSceneData;
            int i;
            for (i = 0; i < sceneCount; i++)
            {
                tempSceneData = sceneDataArray[i];
                _loadedScenes.Remove(tempSceneData);
                _unloadingScenes.Add(tempSceneData);
                unloadTasks[i] = UnityTaskUtilities.FromAsyncOperation(sceneDataArray[i].UnloadSceneAsync(), token);
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
                    tempSceneData = sceneDataArray[i];
                    _unloadingScenes.Remove(tempSceneData);
                    if (_activeScene == tempSceneData)
                        SetActiveScene(GetLastLoadedScene());
                }
                throw;
            }

            for (i = 0; i < sceneCount; i++)
            {
                tempSceneData = sceneDataArray[i];
                _unloadingScenes.Remove(tempSceneData);
                SceneUnloaded?.Invoke(tempSceneData.SceneReference);
                if (_activeScene == tempSceneData)
                    SetActiveScene(GetLastLoadedScene());
            }

            return new SceneResult(SceneDataUtilities.GetScenesFromSceneDataArray(sceneDataArray));
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
                IAsyncSceneOperation unloadOperation = new AsyncSceneOperationStandard(SceneManager.UnloadSceneAsync(tempScene));
                await UnityTaskUtilities.FromAsyncOperation(unloadOperation, token);
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
            LoadingBehavior[] loadingBehaviors = UnityEngine.Object.FindObjectsByType<LoadingBehavior>(FindObjectsSortMode.None);
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

        async Task PollProgressAsync(ISceneData[] sceneDataArray, IProgress<float> progress, CancellationToken token = default)
        {
            bool isDone = false;
            while (!isDone && !token.IsCancellationRequested)
            {
                await Task.Yield();
                isDone = SceneDataUtilities.HasCompletedAllSceneLoadOperations(sceneDataArray);
                progress?.Report(SceneDataUtilities.GetAverageSceneLoadOperationProgress(sceneDataArray));
            }
        }

        Task<SceneResult> UnloadSourceSceneAsync(CancellationToken token)
        {
            Scene sourceScene = GetActiveScene();
            if (!sourceScene.IsValid())
                return Task.FromResult<SceneResult>(default);

            return UnloadAsync(new SceneParameters(SceneRef.FromScene(sourceScene), false), token);
        }
    }
}
