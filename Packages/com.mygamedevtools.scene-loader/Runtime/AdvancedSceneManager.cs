#if ENABLE_UNITASK && !OVERRIDE_DISABLE_UNITASK
#define USE_UNITASK
#endif
#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// The <see cref="AdvancedSceneManager"/> is capable of managing both addressable and non-addressable scene operations.
    /// </summary>
    public class AdvancedSceneManager : ISceneManager
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
        /// Creates an <see cref="AdvancedSceneManager"/> with no initial scene references.
        /// </summary>
        public AdvancedSceneManager() : this(false) { }
        /// <summary>
        /// Creates a new <see cref="AdvancedSceneManager"/> with the option to add all loaded scenes to its management.
        /// The advantage is that you can manage those scenes through this <see cref="ISceneManager"/> instead of having to
        /// use the Unity <see cref="SceneManager"/>.
        /// </summary>
        public AdvancedSceneManager(bool addLoadedScenes)
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
                Debug.LogWarning("Tried to create an `AdvancedSceneManager` with all loaded scenes, but encoutered none. Did you create the scene manager on `Awake()`? If so, try moving the call to `Start()` instead.");
            }
        }
        /// <summary>
        /// Creates a new <see cref="AdvancedSceneManager"/> with the option to add a list of loaded scenes to its management.
        /// The advantage is that you can manage those scenes through this <see cref="ISceneManager"/> instead of having to
        /// use the Unity <see cref="SceneManager"/>.
        /// </summary>
        public AdvancedSceneManager(Scene[] initializationScenes)
        {
            if (initializationScenes == null || initializationScenes.Length == 0)
            {
                throw new ArgumentException($"Trying to create an {nameof(AdvancedSceneManager)} with a null or empty array of initialization scenes. If you want to create it without any scenes, use the empty constructor instead.", nameof(initializationScenes));
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

        public async ValueTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1, IProgress<float> progress = null, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return await LoadScenesAsync_Internal(sceneInfos, setIndexActive, progress, linkedSource.Token).RunAndDisposeToken(linkedSource);
        }

        public async ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null, CancellationToken token = default)
        {
            sceneInfo = sceneInfo ?? throw new NullReferenceException($"[{GetType().Name}] Provided scene info is null.");

            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            Scene[] loadedScenes = await LoadScenesAsync_Internal(new ILoadSceneInfo[] { sceneInfo }, setActive ? 0 : -1, progress, linkedSource.Token).RunAndDisposeToken(linkedSource);

            return loadedScenes != null && loadedScenes.Length > 0 ? loadedScenes[0] : default;
        }

        public async ValueTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneInfos, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return await UnloadScenesAsync_Internal(sceneInfos, linkedSource.Token).RunAndDisposeToken(linkedSource);
        }

        public async ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo, CancellationToken token = default)
        {
            sceneInfo = sceneInfo ?? throw new ArgumentNullException(nameof(sceneInfo), $"[{GetType().Name}] Provided scene info is null.");

            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            Scene[] unloadedScenes = await UnloadScenesAsync_Internal(new ILoadSceneInfo[] { sceneInfo }, linkedSource.Token).RunAndDisposeToken(linkedSource);

            return unloadedScenes != null && unloadedScenes.Length > 0 ? unloadedScenes[0] : default;
        }

        async ValueTask<Scene[]> LoadScenesAsync_Internal(ILoadSceneInfo[] sceneInfos, int setIndexActive, IProgress<float> progress, CancellationToken token)
        {
            int scenesToLoad = sceneInfos.Length;
            if (sceneInfos == null || scenesToLoad == 0)
                throw new ArgumentException(nameof(sceneInfos), $"[{GetType().Name}] Provided scene group is null or empty.");
            if (setIndexActive >= scenesToLoad)
                throw new ArgumentException(nameof(setIndexActive), $"[{GetType().Name}] Provided index to set active is bigger than the provided scene group size.");

            ISceneData[] sceneDataArray = new ISceneData[scenesToLoad];
            int i;
            for (i = 0; i < scenesToLoad; i++)
            {
                sceneDataArray[i] = SceneDataBuilder.BuildFromLoadSceneInfo(sceneInfos[i]);
                sceneDataArray[i].LoadSceneAsync();
            }

            while (!SceneDataUtilities.HasCompletedAllSceneLoadOperations(sceneDataArray) && !token.IsCancellationRequested)
            {
#if USE_UNITASK
                await UniTask.Yield(token);
#else
                await Task.Yield();
#endif
                progress?.Report(SceneDataUtilities.GetAverageSceneLoadOperationProgress(sceneDataArray));
            }

            token.ThrowIfCancellationRequested();

            SceneDataUtilities.LinkLoadedScenesWithSceneDataArray(sceneDataArray, _loadedScenes);

            _loadedScenes.AddRange(sceneDataArray);
            for (i = 0; i < scenesToLoad; i++)
            {
                SceneLoaded?.Invoke(sceneDataArray[i].SceneReference);
            }

            if (setIndexActive >= 0)
                SetActiveScene(sceneDataArray[setIndexActive].SceneReference);

            return SceneDataUtilities.GetScenesFromSceneDataArray(sceneDataArray);
        }

        async ValueTask<Scene[]> UnloadScenesAsync_Internal(ILoadSceneInfo[] sceneInfos, CancellationToken token)
        {
            if (sceneInfos == null || sceneInfos.Length == 0)
                throw new ArgumentException($"[{GetType().Name}] Provided scene group is null or empty.", nameof(sceneInfos));

            ISceneData[] sceneDataArray = SceneDataUtilities.GetLoadedSceneDatasWithLoadSceneInfos(sceneInfos, _loadedScenes);

            int sceneCount = sceneInfos.Length;
            ISceneData tempSceneData;
            int i;
            for (i = 0; i < sceneCount; i++)
            {
                tempSceneData = sceneDataArray[i];
                _loadedScenes.Remove(tempSceneData);
                _unloadingScenes.Add(tempSceneData);
                tempSceneData.UnloadSceneAsync();
            }

            while (!SceneDataUtilities.HasCompletedAllSceneLoadOperations(sceneDataArray) && !token.IsCancellationRequested)
            {
#if USE_UNITASK
                await UniTask.Yield(token);
#else
                await Task.Yield();
#endif
            }

            token.ThrowIfCancellationRequested();

            for (i = 0; i < sceneCount; i++)
            {
                tempSceneData = sceneDataArray[i];
                _unloadingScenes.Remove(tempSceneData);
                SceneUnloaded?.Invoke(tempSceneData.SceneReference);
                if (_activeScene == tempSceneData)
                    SetActiveScene(GetLastLoadedScene());
            }

            return SceneDataUtilities.GetScenesFromSceneDataArray(sceneDataArray);
        }
    }
}