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
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace MyGameDevTools.SceneLoading
{
    public class NewSceneManager : ISceneManager, ISceneManagerReporter
    {
        public event Action<Scene, Scene> ActiveSceneChanged;
        public event Action<Scene> SceneUnloaded;
        public event Action<Scene> SceneLoaded;

        public bool IsUnloadingScenes => _unloadingScenes.Count > 0;
        public int SceneCount => _loadedScenes.Count;

        readonly List<ISceneData> _unloadingScenes = new();
        readonly List<ISceneData> _loadedScenes = new();
        readonly CancellationTokenSource _lifetimeTokenSource = new();

        ISceneData _activeScene;

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
                UnitySceneManager.SetActiveScene(scene);

            ActiveSceneChanged?.Invoke(previousScene != null ? previousScene.SceneReference : default, scene);
        }

        public Scene GetActiveScene() => _activeScene != null ? _activeScene.SceneReference : default;

        public Scene GetLastLoadedScene()
        {
            if (SceneCount == 0)
                return default;

            for (int i = SceneCount - 1; i >= 0; i--)
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
            int i;
            for (i = 0; i < sceneCount; i++)
            {
                _loadedScenes.Remove(sceneDataArray[i]);
                sceneDataArray[i].UnloadSceneAsync();
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

            ISceneData tempSceneData;
            for (i = 0; i < sceneCount; i++)
            {
                tempSceneData = sceneDataArray[i];
                SceneUnloaded?.Invoke(tempSceneData.SceneReference);
                if (_activeScene == tempSceneData)
                    SetActiveScene(GetLastLoadedScene());
            }

            return SceneDataUtilities.GetScenesFromSceneDataArray(sceneDataArray);
        }
    }
}