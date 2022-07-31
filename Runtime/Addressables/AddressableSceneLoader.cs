/**
 * AddressableSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/26/2022 (en-US)
 */

using MyUnityTools.SceneLoading;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoader.Addressables
{
    public class AddressableSceneLoader
    {
        readonly List<AsyncOperationHandle<SceneInstance>> _loadedSceneHandles;
        readonly AssetReference _loadingSceneReference;

        AsyncOperationHandle<SceneInstance> _activeSceneHandle;

        public AddressableSceneLoader(AssetReference loadingSceneReference)
        {
            _loadedSceneHandles = new List<AsyncOperationHandle<SceneInstance>>();
            _loadingSceneReference = loadingSceneReference;
        }

        public async Task<AsyncOperationHandle<SceneInstance>> LoadSceneAsync(AssetReference sceneReference, bool setActive = false)
        {
            var operation = sceneReference.LoadSceneAsync(UnityEngine.SceneManagement.LoadSceneMode.Additive);
            await operation.Task;
            if (setActive)
                _activeSceneHandle = operation;
            _loadedSceneHandles.Add(operation);
            return operation;
        }

        public Task<SceneInstance> TransitionToSceneAsync(AssetReference sceneReference) => TransitionToSceneFlowAsync(sceneReference);

        public Task<SceneInstance> SwitchToSceneAsync(AssetReference sceneReference) => SwitchToSceneFlowAsync(sceneReference);

        public Task UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            var operation = UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(sceneHandle);
            _loadedSceneHandles.Remove(sceneHandle);
            return operation.Task;
        }
        public Task UnloadSceneAsync(SceneInstance scene)
        {
            var operation = UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(scene);
            _loadedSceneHandles.Remove(GetLoadedSceneHandle(scene));
            return operation.Task;
        }
        public Task UnloadSceneAsync(string sceneName)
        {
            var loadedSceneHandle = GetLoadedSceneHandle(sceneName);
            var operation = UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(loadedSceneHandle);
            _loadedSceneHandles.Remove(loadedSceneHandle);
            return operation.Task;
        }

        public AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(SceneInstance sceneInstance)
        {
            foreach (var operationHandle in _loadedSceneHandles)
                if (operationHandle.Result.Scene == sceneInstance.Scene)
                    return operationHandle;
            return default;
        }
        public AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(string sceneName)
        {
            foreach (var operationHandle in _loadedSceneHandles)
                if (operationHandle.Result.Scene.name == sceneName)
                    return operationHandle;
            return default;
        }

        async Task<SceneInstance> TransitionToSceneFlowAsync(AssetReference sceneReference)
        {
            var currentSceneHandle = _activeSceneHandle;

            var loadingHandle = await LoadSceneAsync(_loadingSceneReference);

            var loadingBehavior = Object.FindObjectOfType<LoadingBehavior>();
            while (!loadingBehavior.Active)
                await Task.Yield();

            _activeSceneHandle = await LoadSceneAsyncWithReport(sceneReference, loadingBehavior.UpdateLoadingProgress);
            loadingBehavior.CompleteLoading();

            if (currentSceneHandle.IsValid())
                _ = UnloadSceneAsync(currentSceneHandle);

            while (loadingBehavior.Active)
                await Task.Yield();
            _ = UnloadSceneAsync(loadingHandle);

            return _activeSceneHandle.Result;
        }

        async Task<SceneInstance> SwitchToSceneFlowAsync(AssetReference sceneReference)
        {
            var currentSceneHandle = _activeSceneHandle;

            var operation = sceneReference.LoadSceneAsync(UnityEngine.SceneManagement.LoadSceneMode.Additive);
            await operation.Task;
            _activeSceneHandle = operation;
            _loadedSceneHandles.Add(operation);

            if (currentSceneHandle.IsValid())
                _ = UnloadSceneAsync(currentSceneHandle);

            return _activeSceneHandle.Result;
        }

        async Task<AsyncOperationHandle<SceneInstance>> LoadSceneAsyncWithReport(AssetReference sceneReference, SceneLoadProgressDelegate progressCallback)
        {
            var operation = sceneReference.LoadSceneAsync(UnityEngine.SceneManagement.LoadSceneMode.Additive);
            while (!operation.IsDone)
            {
                progressCallback?.Invoke(operation.PercentComplete);
                await Task.Yield();
            }
            _loadedSceneHandles.Add(operation);
            return operation;
        }
    }
}