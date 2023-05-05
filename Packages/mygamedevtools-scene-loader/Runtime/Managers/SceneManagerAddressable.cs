#if ENABLE_ADDRESSABLES
#if ENABLE_UNITASK && !OVERRIDE_DISABLE_UNITASK
#define USE_UNITASK
#endif
/**
 * {nameof(SceneManagerAddressable)}.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2023-01-21
 */

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace MyGameDevTools.SceneLoading
{
    public class SceneManagerAddressable : ISceneManager, ISceneManagerReporter
    {
        public event Action<Scene, Scene> ActiveSceneChanged;
        public event Action<Scene> SceneUnloaded;
        public event Action<Scene> SceneLoaded;

        public bool IsUnloadingScenes => _unloadingScenes.Count > 0;
        public int SceneCount => _loadedScenes.Count;

        readonly List<SceneInstance> _unloadingScenes = new List<SceneInstance>();
        readonly List<SceneInstance> _loadedScenes = new List<SceneInstance>();

        SceneInstance _activeSceneInstance;

        public void SetActiveScene(Scene scene)
        {
            var validScene = scene.IsValid();
            var isSceneLoaded = TryGetInstanceFromScene(scene, out var sceneInstance);
            if (validScene && !isSceneLoaded)
                throw new InvalidOperationException($"[{GetType().Name}] Cannot set active the scene \"{scene.name}\" that has not been loaded through this {GetType().Name}.");

            var previousSceneInstance = _activeSceneInstance;
            _activeSceneInstance = sceneInstance;
            if (validScene)
                UnitySceneManager.SetActiveScene(scene);

            ActiveSceneChanged?.Invoke(previousSceneInstance.Scene, scene);
        }

        public Scene GetActiveScene() => _activeSceneInstance.Scene;

        public Scene GetLastLoadedScene()
        {
            if (SceneCount == 0)
                return default;

            for (int i = SceneCount - 1; i >= 0; i--)
                if (!_unloadingScenes.Contains(_loadedScenes[i]))
                    return _loadedScenes[i].Scene;

            return default;
        }

        public Scene GetLoadedSceneAt(int index) => _loadedScenes[index].Scene;

        public Scene GetLoadedSceneByName(string name)
        {
            foreach (var sceneInstance in _loadedScenes)
                if (sceneInstance.Scene.name == name)
                    return sceneInstance.Scene;
            throw new ArgumentException($"[{GetType().Name}] Could not find any loaded scene with the name '{name}'.", nameof(name));
        }

        public async ValueTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1, IProgress<float> progress = null)
        {
            if (sceneInfos == null || sceneInfos.Length == 0)
                throw new ArgumentException(nameof(sceneInfos), $"[{GetType().Name}] Provided scene group is null or empty.");
            if (setIndexActive >= sceneInfos.Length)
                throw new ArgumentException(nameof(setIndexActive), $"[{GetType().Name}] Provided index to set active is bigger than the provided scene group size.");

            var operationGroup = GetLoadSceneOperations(sceneInfos, ref setIndexActive);
            if (operationGroup.Operations.Count == 0)
                return Array.Empty<Scene>();

            while (!operationGroup.IsDone)
            {
#if USE_UNITASK
                await UniTask.Yield();
#else
                await Task.Yield();
#endif
                progress?.Report(operationGroup.Progress);
            }

            var loadedScenes = operationGroup.GetResult();

            _loadedScenes.AddRange(loadedScenes);
            foreach (var sceneInstance in loadedScenes)
                SceneLoaded?.Invoke(sceneInstance.Scene);

            if (setIndexActive >= 0)
                SetActiveScene(loadedScenes[setIndexActive].Scene);

            return ToSceneArray(loadedScenes);
        }

        public async ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null)
        {
            sceneInfo = sceneInfo ?? throw new NullReferenceException($"[{GetType().Name}] Provided scene info is null.");
            var loadedScenes = await LoadScenesAsync(new ILoadSceneInfo[] { sceneInfo }, setActive ? 0 : -1, progress);
            if (loadedScenes.Length == 0)
                return default;
            return loadedScenes[0];
        }

        public async ValueTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneInfos)
        {
            if (sceneInfos == null || sceneInfos.Length == 0)
                throw new ArgumentException($"[{GetType().Name}] Provided scene group is null or empty.", nameof(sceneInfos));

            var loadedScenes = GetLastLoadedScenesByInfos(sceneInfos, out var unloadingIndexes);
            if (loadedScenes.Count == 0)
                return Array.Empty<Scene>();

            int unloadingLength = unloadingIndexes.Length;
            var unloadingScenes = new SceneInstance[unloadingLength];
            int i;
            for (i = 0; i < unloadingLength; i++)
                unloadingScenes[i] = loadedScenes[unloadingIndexes[i]];

            for (i = 0; i < unloadingLength; i++)
                loadedScenes.Remove(unloadingScenes[i]);

            var operationGroup = new AsyncOperationHandleGroup(loadedScenes.Count);
            foreach (var sceneInstance in loadedScenes)
            {
                operationGroup.Operations.Add(Addressables.UnloadSceneAsync(sceneInstance));
                _loadedScenes.Remove(sceneInstance);
                _unloadingScenes.Add(sceneInstance);
            }

            while (!operationGroup.IsDone)
#if USE_UNITASK
                await UniTask.Yield();
#else
                await Task.Yield();
#endif

            foreach (var sceneInstance in loadedScenes)
            {
                _unloadingScenes.Remove(sceneInstance);
                SceneUnloaded?.Invoke(sceneInstance.Scene);
                if (_activeSceneInstance.Scene == sceneInstance.Scene)
                    SetActiveScene(GetLastLoadedScene());
            }

            var tasks = new Task[unloadingLength];
            for (i = 0; i < unloadingLength; i++)
                tasks[i] = WaitForSceneUnload(unloadingScenes[i]).AsTask();

            await Task.WhenAll(tasks);

            foreach (var scene in unloadingScenes)
                loadedScenes.Add(scene);

            return ToSceneArray(loadedScenes);
        }

        public async ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo)
        {
            sceneInfo = sceneInfo ?? throw new ArgumentNullException(nameof(sceneInfo), $"[{GetType().Name}] Provided scene info is null.");
            var unloadedScenes = await UnloadScenesAsync(new ILoadSceneInfo[] { sceneInfo });
            if (unloadedScenes.Length == 0)
                return default;
            return unloadedScenes[0];
        }

        async ValueTask<Scene> WaitForSceneUnload(SceneInstance sceneInstance)
        {
#if USE_UNITASK
            await UniTask.WaitUntil(() => !_unloadingScenes.Contains(sceneInstance));
#else
            while (_unloadingScenes.Contains(sceneInstance))
                await Task.Yield();
#endif

            return sceneInstance.Scene;
        }

        AsyncOperationHandleGroup GetLoadSceneOperations(ILoadSceneInfo[] sceneInfos, ref int setIndexActive)
        {
            var sceneLength = sceneInfos.Length;
            var operationGroup = new AsyncOperationHandleGroup(sceneLength);
            for (int i = 0; i < sceneLength; i++)
            {
                if (TryGetLoadSceneOperation(sceneInfos[i], out var operation))
                    operationGroup.Operations.Add(operation);
                else if (i == setIndexActive)
                    setIndexActive = -1;
            }
            return operationGroup;
        }

        IList<SceneInstance> GetLastLoadedScenesByInfos(ILoadSceneInfo[] sceneInfos, out int[] unloadingIndexes)
        {
            var unloadingIndexesList = new List<int>();
            var sceneInfosList = new List<ILoadSceneInfo>(sceneInfos);
            var scenes = new List<SceneInstance>(sceneInfos.Length);

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

            bool tryValidateSceneReference(SceneInstance sceneInstance, out int index)
            {
                foreach (var info in sceneInfosList)
                {
                    if (info.IsReferenceToScene(sceneInstance.Scene))
                    {
                        sceneInfosList.Remove(info);
                        scenes.Add(sceneInstance);
                        index = scenes.Count - 1;
                        return true;
                    }
                }
                index = -1;
                return false;
            }
        }

        Scene[] ToSceneArray(IList<SceneInstance> sceneInstances)
        {
            int sceneCount = sceneInstances.Count;
            var sceneArray = new Scene[sceneCount];
            for (int i = 0; i < sceneCount; i++)
                sceneArray[i] = sceneInstances[i].Scene;

            return sceneArray;
        }

        bool TryGetLoadSceneOperation(ILoadSceneInfo sceneInfo, out AsyncOperationHandle<SceneInstance> operationHandle)
        {
            operationHandle = default;
            if (sceneInfo.Reference is AssetReference assetReference)
                operationHandle = assetReference.LoadSceneAsync(LoadSceneMode.Additive);
            else if (sceneInfo.Reference is string name)
            {
                if (ValidateAssetReference(name))
                    operationHandle = Addressables.LoadSceneAsync(name, LoadSceneMode.Additive);
                else
                    Debug.LogWarning($"[{GetType().Name}] Scene '{name}' couldn't be loaded because its address found no Addressable Assets.");
            }

            bool isValid = operationHandle.IsValid();
            if (!isValid)
                Debug.LogWarning($"[{GetType().Name}] Unexpected {nameof(ILoadSceneInfo.Reference)} type.");
            return isValid;
        }

        bool TryGetInstanceFromScene(Scene scene, out SceneInstance sceneInstance)
        {
            foreach (var instance in _loadedScenes)
            {
                sceneInstance = instance;
                if (scene == instance.Scene)
                    return true;
            }

            sceneInstance = default;
            return false;
        }

        bool ValidateAssetReference(object reference)
        {
            var operation = Addressables.LoadResourceLocationsAsync(reference);
            operation.WaitForCompletion();

            return operation.Result.Count > 0;
        }
    }
}
#endif