#if ENABLE_UNITASK && !OVERRIDE_DISABLE_UNITASK
#define USE_UNITASK
#endif
#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Text;
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
            if (isTargetSceneValid && !TryGetSceneDataByLoadedScene(scene, out sceneData))
                throw new InvalidOperationException($"[{GetType().Name}] Cannot set active the scene \"{scene.name}\" that has not been loaded through this {GetType().Name}.");

            ISceneData previousScene = _activeScene;
            _activeScene = sceneData;
            if (isTargetSceneValid)
                UnitySceneManager.SetActiveScene(scene);

            ActiveSceneChanged?.Invoke(previousScene != null ? previousScene.LoadedScene : default, scene);
        }

        public Scene GetActiveScene() => _activeScene.LoadedScene;

        public Scene GetLastLoadedScene()
        {
            if (SceneCount == 0)
                return default;

            for (int i = SceneCount - 1; i >= 0; i--)
                if (!_unloadingScenes.Contains(_loadedScenes[i]) && _loadedScenes[i].LoadedScene.isLoaded)
                    return _loadedScenes[i].LoadedScene;

            return default;
        }

        public Scene GetLoadedSceneAt(int index) => _loadedScenes[index].LoadedScene;

        public Scene GetLoadedSceneByName(string name)
        {
            foreach (ISceneData sceneData in _loadedScenes)
                if (sceneData.LoadedScene.name == name)
                    return sceneData.LoadedScene;
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

            LinkLoadedScenesWithSceneDataArray(sceneDataArray);

            _loadedScenes.AddRange(sceneDataArray);
            for (i = 0; i < scenesToLoad; i++)
            {
                SceneLoaded?.Invoke(sceneDataArray[i].LoadedScene);
            }

            if (setIndexActive >= 0)
                SetActiveScene(sceneDataArray[setIndexActive].LoadedScene);

            return SceneDataUtilities.GetLoadedScenesFromSceneDataArray(sceneDataArray);
        }

        async ValueTask<Scene[]> UnloadScenesAsync_Internal(ILoadSceneInfo[] sceneInfos, CancellationToken token)
        {
            if (sceneInfos == null || sceneInfos.Length == 0)
                throw new ArgumentException($"[{GetType().Name}] Provided scene group is null or empty.", nameof(sceneInfos));

            // TODO: instead of getting generic last loaded scenes by infos, get from the list of loaded scenes (ISceneData), by directly matching the load scene info
            var loadedScenes = GetLastLoadedScenesByInfos(sceneInfos, out var unloadingIndexes);
            if (loadedScenes.Count == 0)
                throw new InvalidOperationException($"[{GetType().Name} Provided scene group was not able to generate any valid unload scene operations.");

            int unloadingLength = unloadingIndexes.Length;
            var unloadingScenes = new Scene[unloadingLength];
            int i;
            for (i = 0; i < unloadingLength; i++)
                unloadingScenes[i] = loadedScenes[unloadingIndexes[i]];

            for (i = 0; i < unloadingLength; i++)
                loadedScenes.Remove(unloadingScenes[i]);

            var operationGroup = new AsyncOperationGroup(loadedScenes.Count);
            foreach (var scene in loadedScenes)
            {
                operationGroup.Operations.Add(UnitySceneManager.UnloadSceneAsync(scene));
                _loadedScenes.Remove(scene);
                _unloadingScenes.Add(scene);
            }

            while (!operationGroup.IsDone && !token.IsCancellationRequested)
            {
#if USE_UNITASK
                await UniTask.Yield(token);
#else
                await Task.Yield();
#endif
            }

            token.ThrowIfCancellationRequested();

            foreach (var scene in loadedScenes)
            {
                _unloadingScenes.Remove(scene);
                SceneUnloaded?.Invoke(scene);
                if (_activeScene == scene)
                    SetActiveScene(GetLastLoadedScene());
            }

            var tasks = new Task[unloadingLength];
            for (i = 0; i < unloadingLength; i++)
                tasks[i] = WaitForSceneUnload(unloadingScenes[i], token).AsTask();

            await Task.WhenAll(tasks);

            foreach (var scene in unloadingScenes)
                loadedScenes.Add(scene);
            return loadedScenes.ToArray();
        }

        async ValueTask<Scene> WaitForSceneUnload(Scene scene, CancellationToken token)
        {
#if USE_UNITASK
            await UniTask.WaitUntil(() => !_unloadingScenes.Contains(scene), cancellationToken: token);
#else
            while (_unloadingScenes.Contains(scene) && !token.IsCancellationRequested)
                await Task.Yield();
#endif
            token.ThrowIfCancellationRequested();

            return scene;
        }

        void LinkLoadedScenesWithSceneDataArray(ISceneData[] sceneDataArray)
        {
            // Fill this list with all loaded scenes from the Unity Scene Manager;
            int totalSceneCount = UnitySceneManager.sceneCount;
            List<Scene> unmatchedScenes = new(totalSceneCount);

            int i;
            for (i = 0; i < totalSceneCount; i++)
            {
                Scene scene = UnitySceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    unmatchedScenes.Add(scene);
            }

            // Remove scenes already tracked by the scene manager
            totalSceneCount = _loadedScenes.Count;
            for (i = 0; i < totalSceneCount; i++)
            {
                unmatchedScenes.Remove(_loadedScenes[i].LoadedScene);
            }

            // Loop through all ISceneData and update those that have direct reference to their scene
            // through their ILoadSceneOperation.
            int sceneDataCount = sceneDataArray.Length;
            List<ISceneData> unmatchedSceneDatas = new(sceneDataArray);

            ISceneData sceneData;
            for (i = 0; i < sceneDataCount; i++)
            {
                sceneData = sceneDataArray[i];
                if (sceneData.LoadOperation.HasDirectReferenceToScene)
                {
                    sceneData.UpdateSceneReference();
                    unmatchedScenes.Remove(sceneData.LoadedScene);
                    unmatchedSceneDatas.Remove(sceneData);
                }
            }

            // Then, loop through all remaining unmatched scenes and check if they can match one of the indirect reference loaded scenes.
            for (i = unmatchedScenes.Count - 1; i >= 0 && unmatchedSceneDatas.Count > 0; i--)
            {
                if (SceneDataUtilities.TryLinkLoadedSceneWithSceneData(unmatchedScenes[i], unmatchedSceneDatas, out ISceneData matchedData))
                {
                    unmatchedScenes.RemoveAt(i);
                    unmatchedSceneDatas.Remove(matchedData);
                }
            }

            if (unmatchedSceneDatas.Count > 0)
            {
                Debug.LogError($"Unable to link all scene datas to loaded scenes. Linked {sceneDataCount - unmatchedSceneDatas.Count}/{sceneDataCount}.");
            }
        }

        List<Scene> GetLastLoadedScenesByInfos(ILoadSceneInfo[] sceneInfos, out int[] unloadingIndexes)
        {
            var unloadingIndexesList = new List<int>();
            var sceneInfosList = new List<ILoadSceneInfo>(sceneInfos);
            var scenes = new List<Scene>(sceneInfos.Length);

            int sceneCount = SceneCount;
            int i;
            for (i = sceneCount - 1; i >= 0 && sceneInfosList.Count > 0; i--)
                tryValidateSceneReference(_loadedScenes[i], out _);

            sceneCount = _unloadingScenes.Count;
            for (i = 0; i < sceneCount; i++)
                if (tryValidateSceneReference(_unloadingScenes[i], out int index))
                    unloadingIndexesList.Add(index);

            if (sceneInfosList.Count > 0)
            {
                var builder = new StringBuilder($"[{GetType().Name}] Some of the scenes could not be found loaded in the Unity Scene Manager:\n");
                for (i = 0; i < sceneInfosList.Count; i++)
                    builder.AppendLine($" ({i}): {sceneInfosList[i]}");

                Debug.LogWarning(builder.ToString());
            }

            unloadingIndexes = unloadingIndexesList.ToArray();
            return scenes;

            bool tryValidateSceneReference(Scene scene, out int index)
            {
                foreach (var info in sceneInfosList)
                {
                    if (info.IsReferenceToScene(scene))
                    {
                        sceneInfosList.Remove(info);
                        scenes.Add(scene);
                        index = scenes.Count - 1;
                        return true;
                    }
                }
                index = -1;
                return false;
            }
        }

        bool TryGetSceneDataByLoadedScene(Scene scene, out ISceneData sceneData)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                Debug.LogWarning($"[{GetType().Name}] Attempted to get an ISceneData through an invalid or unloaded scene.");
                sceneData = default;
                return false;
            }

            foreach (ISceneData tempSceneData in _loadedScenes)
            {
                if (scene == tempSceneData.LoadedScene)
                {
                    sceneData = tempSceneData;
                    return true;
                }
            }

            Debug.LogWarning($"[{GetType().Name}] Unable to get an ISceneData with the loaded scene {scene.name} ({scene.handle}).");
            sceneData = default;
            return false;
        }
    }
}