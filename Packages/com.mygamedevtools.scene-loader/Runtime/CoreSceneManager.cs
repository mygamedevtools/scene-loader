using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
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
            else if (loadedSceneCount == 0)
            {
                Debug.LogWarning("Tried to create a Scene Manager with all loaded scenes, but encoutered none. Did you create the Scene Manager on `Awake()`? If so, try moving the call to `Start()` instead.");
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

        public Task<SceneResult> TransitionAsync(SceneParameters sceneParameters, ILoadSceneInfo intermediateSceneReference = null, CancellationToken token = default)
        {
            if (!sceneParameters.ShouldSetActive())
                throw new ArgumentException($"[{GetType().Name}] You need to provide a SceneParameters object with a valid 'setIndexActive' value to perform scene transitions.", nameof(sceneParameters));

            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return intermediateSceneReference == null
                ? TransitionDirectlyAsync(sceneParameters, linkedSource.Token).RunAndDisposeToken(linkedSource)
                : TransitionWithIntermediateAsync(sceneParameters, intermediateSceneReference, linkedSource.Token).RunAndDisposeToken(linkedSource);
        }

        public Task<SceneResult> ReloadActiveSceneAsync(ILoadSceneInfo intermediateSceneReference = null, CancellationToken token = default)
        {
            if (_activeScene == null || !_activeScene.SceneReference.IsValid() || !_activeScene.SceneReference.isLoaded)
                throw new InvalidOperationException($"[{GetType().Name}] Cannot reload the active scene because it is null or not loaded. Make sure to load a scene before trying to reload it.");

            ILoadSceneInfo targetSceneInfo = _activeScene.LoadSceneInfo;
            return TransitionAsync(new SceneParameters(targetSceneInfo, true), intermediateSceneReference, token);
        }

        public Task<SceneResult> LoadAsync(SceneParameters sceneParameters, IProgress<float> progress = null, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return LoadScenesAsync_Internal(sceneParameters, progress, linkedSource.Token).RunAndDisposeToken(linkedSource);
        }

        public Task<SceneResult> UnloadAsync(SceneParameters sceneParameters, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return UnloadScenesAsync_Internal(sceneParameters.GetLoadSceneInfos(), linkedSource.Token).RunAndDisposeToken(linkedSource);
        }

        async Task<SceneResult> LoadScenesAsync_Internal(SceneParameters sceneParameters, IProgress<float> progress, CancellationToken token)
        {
            ILoadSceneInfo[] sceneInfos = sceneParameters.GetLoadSceneInfos();
            int setIndexActive = sceneParameters.GetIndexToActivate();
            int scenesToLoad = sceneInfos.Length;

            ISceneData[] sceneDataArray = new ISceneData[scenesToLoad];
            int i;
            for (i = 0; i < scenesToLoad; i++)
            {
                sceneDataArray[i] = SceneDataBuilder.BuildFromLoadSceneInfo(sceneInfos[i]);
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

        async Task<SceneResult> UnloadScenesAsync_Internal(ILoadSceneInfo[] sceneInfos, CancellationToken token)
        {
            if (sceneInfos == null || sceneInfos.Length == 0)
                throw new ArgumentException($"[{GetType().Name}] Provided scene group is null or empty.", nameof(sceneInfos));

            int sceneCount = sceneInfos.Length;
            ISceneData[] sceneDataArray = SceneDataUtilities.GetLoadedSceneDatasWithLoadSceneInfos(sceneInfos, _loadedScenes);
            Task[] loadTasks = new Task[sceneCount];

            ISceneData tempSceneData;
            int i;
            for (i = 0; i < sceneCount; i++)
            {
                tempSceneData = sceneDataArray[i];
                _loadedScenes.Remove(tempSceneData);
                _unloadingScenes.Add(tempSceneData);
                loadTasks[i] = UnityTaskUtilities.FromAsyncOperation(sceneDataArray[i].UnloadSceneAsync(), token);
            }

            try
            {
                await Task.WhenAll(loadTasks);
            }
            catch (OperationCanceledException exception)
            {
                for (i = 0; i < sceneCount; i++)
                {
                    _unloadingScenes.Remove(sceneDataArray[i]);
                }
                throw exception;
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

        async Task<SceneResult> TransitionWithIntermediateAsync(SceneParameters sceneParameters, ILoadSceneInfo intermediateSceneInfo, CancellationToken token)
        {
            Scene loadingScene = await LoadAsync(new SceneParameters(intermediateSceneInfo, false), token: token);
            intermediateSceneInfo = new LoadSceneInfoScene(loadingScene);

            LoadingBehavior loadingBehavior = UnityEngine.Object.FindObjectsByType<LoadingBehavior>(FindObjectsSortMode.None).FirstOrDefault(l => l.gameObject.scene == loadingScene);
            return loadingBehavior
                ? await TransitionWithIntermediateLoadingAsync(sceneParameters, intermediateSceneInfo, loadingBehavior, token)
                : await TransitionWithIntermediateNoLoadingAsync(sceneParameters, intermediateSceneInfo, token);
        }

        async Task<SceneResult> TransitionWithIntermediateLoadingAsync(SceneParameters sceneParameters, ILoadSceneInfo intermediateSceneInfo, LoadingBehavior loadingBehavior, CancellationToken token)
        {
            LoadingProgress progress = loadingBehavior.Progress;
            await progress.TransitionInTask.Task;
            await UnloadSourceSceneAsync(token);

            Scene[] loadedScenes = await LoadAsync(sceneParameters, progress, token);
            progress.SetLoadingCompleted();

            await progress.TransitionOutTask.Task;
            await UnloadAsync(new SceneParameters(intermediateSceneInfo), token);
            return new SceneResult(loadedScenes);
        }

        async Task<SceneResult> TransitionWithIntermediateNoLoadingAsync(SceneParameters sceneParameters, ILoadSceneInfo intermediateSceneInfo, CancellationToken token)
        {
            await UnloadSourceSceneAsync(token);
            Scene[] loadedScenes = await LoadAsync(sceneParameters, token: token);
            await UnloadAsync(new SceneParameters(intermediateSceneInfo), token);
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

            return UnloadAsync(new SceneParameters(new LoadSceneInfoScene(sourceScene)), token);
        }
    }
}