#if ENABLE_ADDRESSABLES
#if ENABLE_UNITASK && !OVERRIDE_DISABLE_UNITASK
#define USE_UNITASK
#endif
/**
 * SceneManagerAddressable.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2023-01-21
 */

#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace MyGameDevTools.SceneLoading
{
    public class SceneManagerAddressable : ISceneManager
    {
        public event Action<Scene, Scene> ActiveSceneChanged;
        public event Action<Scene> SceneUnloaded;
        public event Action<Scene> SceneLoaded;

        public int SceneCount => _loadedScenes.Count;

        readonly List<SceneInstance> _unloadingScenes = new List<SceneInstance>();
        readonly List<SceneInstance> _loadedScenes = new List<SceneInstance>();

        SceneInstance _activeSceneInstance;

        public void SetActiveScene(Scene scene)
        {
            var validScene = scene.IsValid();
            var isSceneLoaded = TryGetInstanceFromScene(scene, out var sceneInstance);
            if (validScene && !isSceneLoaded)
                throw new InvalidOperationException($"Cannot set active the scene \"{scene.name}\" that has not been loaded through this {GetType().Name}.");

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
            throw new ArgumentException($"Could not find any loaded scene with the name '{name}'.", nameof(name));
        }

        public async ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null)
        {
            var operation = GetLoadSceneOperation(sceneInfo);

#if USE_UNITASK
            await operation.ToUniTask(progress);
#else
            while (!operation.IsDone)
            {
                await Task.Yield();
                progress?.Report(operation.PercentComplete);
            }
#endif

            if (operation.Status == AsyncOperationStatus.Failed)
                throw operation.OperationException;

            var loadedSceneInstance = operation.Result;
            var loadedScene = loadedSceneInstance.Scene;

            _loadedScenes.Add(loadedSceneInstance);
            SceneLoaded?.Invoke(loadedScene);

            if (setActive)
                SetActiveScene(loadedScene);

            return loadedScene;
        }

        public async ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo)
        {
            var sceneInstance = GetLastLoadedSceneByInfo(sceneInfo);
            if (!_loadedScenes.Contains(sceneInstance))
                throw new InvalidOperationException($"Cannot unload the scene \"{sceneInstance.Scene.name}\" that has not been loaded through this {GetType().Name}.");
            if (_unloadingScenes.Contains(sceneInstance))
                return await WaitForSceneUnload(sceneInstance);

            _unloadingScenes.Add(sceneInstance);
            var operation = GetUnloadSceneOperation(sceneInfo);
#if USE_UNITASK
            await operation.ToUniTask();
#else
            while (!operation.IsDone)
                await Task.Yield();
#endif

            if (operation.Status == AsyncOperationStatus.Failed)
                throw operation.OperationException;

            var loadedScene = operation.Result.Scene;

            _unloadingScenes.Remove(sceneInstance);
            _loadedScenes.Remove(sceneInstance);
            if (_activeSceneInstance.Scene == sceneInstance.Scene)
                SetActiveScene(GetLastLoadedScene());

            SceneUnloaded?.Invoke(loadedScene);

            return loadedScene;
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

        AsyncOperationHandle<SceneInstance> GetLoadSceneOperation(ILoadSceneInfo sceneInfo)
        {
            if (sceneInfo.Reference is AssetReference assetReference)
                return assetReference.LoadSceneAsync(LoadSceneMode.Additive);
            else if (sceneInfo.Reference is string name)
            {
                if (!ValidateAssetReference(name))
                    throw new Exception($"Scene '{name}' couldn't be loaded because its address found no Addressable Assets.");
                return Addressables.LoadSceneAsync(name, LoadSceneMode.Additive);
            }
            else
                throw new Exception($"Unexpected {nameof(ILoadSceneInfo.Reference)} type.");
        }

        AsyncOperationHandle<SceneInstance> GetUnloadSceneOperation(ILoadSceneInfo sceneInfo)
        {
            if (sceneInfo.Reference is string name)
            {
                if (TryGetInstanceFromScene(GetLoadedSceneByName(name), out var nameInstance))
                    return Addressables.UnloadSceneAsync(nameInstance);
            }
            else if (sceneInfo.Reference is Scene scene)
            {
                if (TryGetInstanceFromScene(scene, out var sceneInstance))
                    return Addressables.UnloadSceneAsync(sceneInstance);
            }
            else
                throw new Exception($"Unexpected {nameof(ILoadSceneInfo.Reference)} type.");

            throw new Exception($"Could not find any loaded scene with the scene info reference '{sceneInfo.Reference}'.");
        }

        SceneInstance GetLastLoadedSceneByInfo(ILoadSceneInfo sceneInfo)
        {
            var sceneCount = SceneCount;
            for (int i = sceneCount - 1; i >= 0; i--)
            {
                var sceneInstance = _loadedScenes[i];
                if (sceneInfo.IsReferenceToScene(sceneInstance.Scene))
                    return sceneInstance;
            }
            throw new ArgumentException($"Could not find any loaded scene with the provided ILoadSceneInfo: {sceneInfo.Reference}");
        }

        bool ValidateAssetReference(object reference)
        {
            var operation = Addressables.LoadResourceLocationsAsync(reference);
            operation.WaitForCompletion();

            return operation.Result.Count > 0;
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
    }
}
#endif