#if ENABLE_UNITASK && !OVERRIDE_DISABLE_UNITASK
#define USE_UNITASK
#endif
/**
 * SceneManager.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-21
 */

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

[assembly: InternalsVisibleTo("MyGameDevTools.SceneLoading.Tests")]

namespace MyGameDevTools.SceneLoading
{
    public class SceneManager : ISceneManager, ISceneManagerReporter
    {
        public event Action<Scene, Scene> ActiveSceneChanged;
        public event Action<Scene> SceneUnloaded;
        public event Action<Scene> SceneLoaded;

        public bool IsUnloadingScenes => _unloadingScenes.Count > 0;
        public int SceneCount => _loadedScenes.Count;

        readonly List<Scene> _unloadingScenes = new List<Scene>();
        readonly List<Scene> _loadedScenes = new List<Scene>();

        Scene _activeScene;

        public void SetActiveScene(Scene scene)
        {
            var validScene = scene.IsValid();
            if (validScene && !_loadedScenes.Contains(scene))
                throw new InvalidOperationException($"Cannot set active the scene \"{scene.name}\" that has not been loaded through this {GetType().Name}.");

            var previousScene = _activeScene;
            _activeScene = scene;
            if (validScene)
                UnitySceneManager.SetActiveScene(scene);

            ActiveSceneChanged?.Invoke(previousScene, scene);
        }

        public Scene GetActiveScene() => _activeScene;

        public Scene GetLastLoadedScene()
        {
            if (SceneCount == 0)
                return default;

            for (int i = SceneCount - 1; i >= 0; i--)
                if (!_unloadingScenes.Contains(_loadedScenes[i]) && _loadedScenes[i].isLoaded)
                    return _loadedScenes[i];

            return default;
        }

        public Scene GetLoadedSceneAt(int index) => _loadedScenes[index];

        public Scene GetLoadedSceneByName(string name)
        {
            foreach (var scene in _loadedScenes)
                if (scene.name == name)
                    return scene;
            throw new ArgumentException($"Could not find any loaded scene with the name '{name}'.", nameof(name));
        }

        public async ValueTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1, IProgress<float> progress = null)
        {
            var operationGroup = GetLoadSceneOperations(sceneInfos, ref setIndexActive);
            if (operationGroup.Operations.Count == 0)
                return Array.Empty<Scene>();

            while (!operationGroup.IsDone)
            {
                await Task.Yield();
                progress?.Report(operationGroup.Progress);
            }

            var loadedScenes = GetLastUnityLoadedScenesByInfos(sceneInfos, ref setIndexActive);

            _loadedScenes.AddRange(loadedScenes);
            foreach (var s in loadedScenes)
                SceneLoaded?.Invoke(s);

            if (setIndexActive >= 0)
                SetActiveScene(loadedScenes[setIndexActive]);

            return loadedScenes;
        }

        public async ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null)
        {
            if (!TryGetLoadSceneOperation(sceneInfo, out var operation))
                return default;

#if USE_UNITASK
            await operation.ToUniTask(progress);
#else
            while (!operation.isDone)
            {
                await Task.Yield();
                progress?.Report(operation.progress);
            }
#endif

            if (!TryGetLastUnityLoadedSceneByInfo(sceneInfo, out var loadedScene))
                return default;

            _loadedScenes.Add(loadedScene);
            SceneLoaded?.Invoke(loadedScene);

            if (setActive)
                SetActiveScene(loadedScene);

            return loadedScene;
        }

        public async ValueTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneInfos)
        {
            var loadedScenes = GetLastLoadedScenesByInfos(sceneInfos, out var unloadingIndexes);
            if (loadedScenes.Count == 0)
                return Array.Empty<Scene>();

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

            while (!operationGroup.IsDone)
                await Task.Yield();

            foreach (var scene in loadedScenes)
            {
                _unloadingScenes.Remove(scene);
                if (_activeScene == scene)
                    SetActiveScene(GetLastLoadedScene());
            }

            var tasks = new Task[unloadingLength];
            for (i = 0; i < unloadingLength; i++)
                tasks[i] = WaitForSceneUnload(unloadingScenes[i]).AsTask();

            await Task.WhenAll(tasks);

            foreach (var scene in unloadingScenes)
                loadedScenes.Add(scene);
            return loadedScenes.ToArray();
        }

        public async ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo)
        {
            if (!TryGetLastLoadedSceneByInfo(sceneInfo, out var scene, out bool isUnloading))
                return default;
            if (isUnloading)
                return await WaitForSceneUnload(scene);

            var operation = UnitySceneManager.UnloadSceneAsync(scene);

            _loadedScenes.Remove(scene);
            _unloadingScenes.Add(scene);
#if USE_UNITASK
            await operation.ToUniTask();
#else
            while (!operation.isDone)
                await Task.Yield();
#endif

            _unloadingScenes.Remove(scene);
            if (_activeScene == scene)
                SetActiveScene(GetLastLoadedScene());

            SceneUnloaded?.Invoke(scene);

            return scene;
        }

        async ValueTask<Scene> WaitForSceneUnload(Scene scene)
        {
#if USE_UNITASK
            await UniTask.WaitUntil(() => !_unloadingScenes.Contains(scene));
#else
            while (_unloadingScenes.Contains(scene))
                await Task.Yield();
#endif
            return scene;
        }

        AsyncOperationGroup GetLoadSceneOperations(ILoadSceneInfo[] sceneInfos, ref int setIndexActive)
        {
            var sceneLength = sceneInfos.Length;
            var operationGroup = new AsyncOperationGroup(sceneLength);
            for (int i = 0; i < sceneLength; i++)
            {
                if (TryGetLoadSceneOperation(sceneInfos[i], out var operation))
                    operationGroup.Operations.Add(operation);
                else if (i == setIndexActive)
                    setIndexActive = -1;
            }
            return operationGroup;
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
                var builder = new StringBuilder("[SceneManager] Some of the scenes could not be found loaded in the Unity Scene Manager:\n");
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

        Scene[] GetLastUnityLoadedScenesByInfos(ILoadSceneInfo[] sceneInfos, ref int setIndexActive)
        {
            var sceneInfosList = new List<ILoadSceneInfo>(sceneInfos);
            var scenes = new List<Scene>(sceneInfos.Length);

            var sceneCount = UnitySceneManager.sceneCount;
            for (int i = sceneCount - 1; i >= 0 && sceneInfosList.Count > 0; i--)
            {
                var scene = UnitySceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    validateSceneReference(scene, ref setIndexActive);
            }

            if (sceneInfosList.Count > 0)
            {
                var builder = new StringBuilder("[SceneManager] Some of the scenes could not be found loaded in the Unity Scene Manager:\n");
                for (int i = 0; i < sceneInfosList.Count; i++)
                    builder.AppendLine($" ({i}): {sceneInfosList[i]}");

                Debug.LogWarning(builder.ToString());

                if (setIndexActive >= 0 && sceneInfosList.Contains(sceneInfos[setIndexActive]))
                    setIndexActive = -1;
            }

            return scenes.ToArray();

            void validateSceneReference(Scene scene, ref int setIndexActive)
            {
                foreach (var info in sceneInfosList)
                {
                    if (info.IsReferenceToScene(scene))
                    {
                        sceneInfosList.Remove(info);
                        scenes.Add(scene);
                        if (setIndexActive >= 0 && sceneInfos[setIndexActive] == info)
                            setIndexActive = scenes.Count - 1;
                        return;
                    }
                }
            }
        }

        bool TryGetLastLoadedSceneByInfo(ILoadSceneInfo sceneInfo, out Scene scene, out bool isUnloading)
        {
            isUnloading = false;
            var sceneCount = SceneCount;
            int i;
            for (i = sceneCount - 1; i >= 0; i--)
            {
                scene = _loadedScenes[i];
                if (sceneInfo.IsReferenceToScene(scene))
                    return true;
            }

            sceneCount = _unloadingScenes.Count;
            for (i = 0; i < sceneCount; i++)
            {
                scene = _unloadingScenes[i];
                if (sceneInfo.IsReferenceToScene(scene))
                {
                    isUnloading = true;
                    return true;
                }
            }

            Debug.LogWarning($"[SceneManager] Could not find any loaded scene with the provided ILoadSceneInfo: {sceneInfo}");
            scene = default;
            return false;
        }

        bool TryGetLastUnityLoadedSceneByInfo(ILoadSceneInfo sceneInfo, out Scene scene)
        {
            var sceneCount = UnitySceneManager.sceneCount;
            for (int i = sceneCount - 1; i >= 0; i--)
            {
                scene = UnitySceneManager.GetSceneAt(i);
                if (scene.isLoaded && sceneInfo.IsReferenceToScene(scene))
                    return true;
            }

            Debug.LogWarning($"[SceneManager] Could not find any loaded scene in the Unity Scene Manager with the provided ILoadSceneInfo: {sceneInfo}");
            scene = default;
            return false;
        }

        bool TryGetLoadSceneOperation(ILoadSceneInfo sceneInfo, out AsyncOperation operation)
        {
            operation = null;
            if (sceneInfo.Reference is int index)
                operation = UnitySceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
            else if (sceneInfo.Reference is string name)
                operation = UnitySceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            else if (sceneInfo.Reference is Scene scene)
                operation = UnitySceneManager.LoadSceneAsync(scene.buildIndex, LoadSceneMode.Additive);
            else
                Debug.LogWarning($"[SceneManager] Unexpected {nameof(ILoadSceneInfo.Reference)} type.");

            return operation != null;
        }
    }
}