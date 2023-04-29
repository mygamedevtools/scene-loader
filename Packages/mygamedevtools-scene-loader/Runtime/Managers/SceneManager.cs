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

            var loadedScenes = GetLastUnityLoadedScenesByInfo(sceneInfos, ref setIndexActive);

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

        public ValueTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneInfos)
        {
            // Get loaded scenes matching scene infos
            // Check which of these are already unloading
            // Check which of these have not been loaded by this manager
            // Get unload operation for the remaining
            // unload
            throw new NotImplementedException();
        }

        public async ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo)
        {
            if (!TryGetLastSceneByInfo(sceneInfo, out var scene))
                return default;
            if (_unloadingScenes.Contains(scene))
                return await WaitForSceneUnload(scene);
            if (!_loadedScenes.Contains(scene))
            {
                Debug.LogError($"[SceneManager] Cannot unload the scene \"{scene.name}\" that has not been loaded through this {GetType().Name}.");
                return default;
            }

            if (!TryGetUnloadSceneOperation(sceneInfo, out var operation))
                return default;

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

        Scene[] GetLastUnityLoadedScenesByInfo(ILoadSceneInfo[] sceneInfos, ref int setIndexActive)
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

        bool TryGetLastSceneByInfo(ILoadSceneInfo sceneInfo, out Scene scene)
        {
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
                    return true;
            }

            scene = default;
            Debug.LogWarning($"[SceneManager] Could not find any scene with the provided ILoadSceneInfo: {sceneInfo}");
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

        bool TryGetUnloadSceneOperation(ILoadSceneInfo sceneInfo, out AsyncOperation operation)
        {
            operation = null;
            if (sceneInfo.Reference is int index)
                operation = UnitySceneManager.UnloadSceneAsync(index);
            else if (sceneInfo.Reference is string name)
                operation = UnitySceneManager.UnloadSceneAsync(name);
            else if (sceneInfo.Reference is Scene scene)
                operation = UnitySceneManager.UnloadSceneAsync(scene);
            else
                Debug.LogWarning($"[SceneManager] Unexpected {nameof(ILoadSceneInfo.Reference)} type.");

            return operation != null;
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