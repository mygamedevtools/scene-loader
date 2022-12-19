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
        public int SceneCount => _loadedScenes.Count;

        readonly List<SceneInstance> _loadedScenes = new List<SceneInstance>();

        SceneInstance _activeScene;

        public void SetActiveScene(SceneInstance scene)
        {
            if (!_loadedScenes.Contains(scene))
                throw new InvalidOperationException($"Cannot set active the scene \"{scene.Scene.name}\" that has not been loaded through this {nameof(AddressableSceneManager)}.");
            _activeScene = scene;
        }
        public void SetActiveScene(string sceneName)
        {
            var sceneInstance = GetLoadedSceneByName(sceneName);
            if (!sceneInstance.Scene.IsValid())
                throw new InvalidOperationException($"Cannot set active the scene \"{sceneName}\" that has not been loaded through this {nameof(AddressableSceneManager)}.");
            _activeScene = sceneInstance;
        }

        public async Task<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive = false, IProgress<float> progress = null)
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
            if (setActive)
                SetActiveScene(scene);

            return operation.Result;
        }

        public async Task UnloadSceneAsync(IAddressableLoadSceneInfo sceneInfo)
        {
            var scene = GetLoadedSceneByInfo(sceneInfo);
            if (!_loadedScenes.Contains(scene))
                throw new InvalidOperationException($"Cannot unload the scene \"{scene.Scene.name}\" that has not been loaded through this {nameof(AddressableSceneManager)}.");

            var operation = Addressables.UnloadSceneAsync(scene, true);
            _loadedScenes.Remove(scene);
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

        async Task ValidateSceneReferenceAsync(IAddressableLoadSceneReference sceneReference)
        {
            var validateOperation = Addressables.LoadResourceLocationsAsync(sceneReference.RuntimeKey);
            await validateOperation.Task;
            if (validateOperation.Result == null || validateOperation.Result.Count == 0)
                throw new InvalidKeyException(sceneReference.RuntimeKey);
        }

        SceneInstance GetLoadedSceneByInfo(IAddressableLoadSceneInfo sceneInfo)
        {
            var info = sceneInfo.Info;
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