#if ENABLE_ADDRESSABLES
/**
 * AddressableSceneManager.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/26/2022 (en-US)
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading.AddressablesSupport
{
    public class AddressableSceneManager : IAddressableSceneManager
    {
        public event Action<SceneInstance, SceneInstance> ActiveSceneChanged;
        public event Action<SceneInstance> SceneUnloaded;
        public event Action<SceneInstance> SceneLoaded;

        public int SceneCount => _loadedScenes.Count;

        readonly List<SceneInstance> _loadedScenes = new List<SceneInstance>();

        SceneInstance _activeScene;

        public void SetActiveScene(SceneInstance scene)
        {
            if (!_loadedScenes.Contains(scene))
                throw new InvalidOperationException($"Cannot set active the scene \"{scene.Scene.name}\" that has not been loaded through this {GetType().Name}.");

            var previousScene = _activeScene;
            _activeScene = scene;
            ActiveSceneChanged?.Invoke(previousScene, scene);
        }

        public async ValueTask<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive = false, IProgress<float> progress = null)
        {
            await ValidateSceneReferenceAsync(sceneReference);
            var operation = Addressables.LoadSceneAsync(sceneReference.RuntimeKey, LoadSceneMode.Additive);

            while (!operation.IsDone)
            {
                await Task.Yield();
                progress?.Report(operation.PercentComplete);
            }

            if (operation.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"Unable to load scene by reference: {sceneReference.RuntimeKey}.", operation.OperationException);

            var scene = operation.Result;
            _loadedScenes.Add(scene);
            SceneLoaded?.Invoke(scene);

            if (setActive)
                SetActiveScene(scene);

            return operation.Result;
        }

        public async ValueTask UnloadSceneAsync(IAddressableLoadSceneReference sceneInfo)
        {
            var scene = GetLoadedSceneByInfo(sceneInfo);
            if (!_loadedScenes.Contains(scene))
                throw new InvalidOperationException($"Cannot unload the scene \"{scene.Scene.name}\" that has not been loaded through this {nameof(AddressableSceneManager)}.");

            var operation = Addressables.UnloadSceneAsync(scene, true);
            _loadedScenes.Remove(scene);
            SceneUnloaded?.Invoke(scene);
            await operation.Task;
        }

        public SceneInstance GetActiveScene() => _activeScene;

        public SceneInstance GetLoadedSceneAt(int index) => _loadedScenes[index];

        public SceneInstance GetLoadedSceneByName(string sceneName)
        {
            foreach (var sceneInstance in _loadedScenes)
                if (sceneInstance.Scene.name == sceneName)
                    return sceneInstance;
            return default;
        }

        async ValueTask ValidateSceneReferenceAsync(IAddressableLoadSceneReference sceneReference)
        {
            var validateOperation = Addressables.LoadResourceLocationsAsync(sceneReference.RuntimeKey);
            await validateOperation.Task;
            if (validateOperation.Result == null || validateOperation.Result.Count == 0)
                throw new InvalidKeyException(sceneReference.RuntimeKey);
        }

        SceneInstance GetLoadedSceneByInfo(IAddressableLoadSceneReference sceneInfo)
        {
            var info = sceneInfo.RuntimeKey;
            if (info is SceneInstance sceneInstance)
                return sceneInstance;
            else if (info is string sceneName)
                return GetLoadedSceneByName(sceneName);
            else
                throw new Exception($"Unexpected {nameof(IAddressableLoadSceneInfo.Info)} type.");
        }
    }
}
#endif