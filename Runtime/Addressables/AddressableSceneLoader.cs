#if ENABLE_ADDRESSABLES
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
        readonly AddressableLoadSceneInfo _loadingSceneInfo;

        AsyncOperationHandle<SceneInstance> _activeSceneHandle;

        public AddressableSceneLoader(AssetReference loadingSceneReference)
        {
            _loadedSceneHandles = new List<AsyncOperationHandle<SceneInstance>>();
            _loadingSceneInfo = new AddressableLoadSceneInfo(loadingSceneReference);
        }

        public Task<SceneInstance> TransitionToSceneAsync(AssetReference sceneReference) => TransitionToSceneFlowAsync(new AddressableLoadSceneInfo(sceneReference));

        public Task<SceneInstance> TransitionToSceneAsync(string sceneRuntimeKey) => TransitionToSceneFlowAsync(new AddressableLoadSceneInfo(sceneRuntimeKey));

        public Task<SceneInstance> SwitchToSceneAsync(AssetReference sceneReference) => SwitchToSceneFlowAsync(new AddressableLoadSceneInfo(sceneReference));

        public Task<SceneInstance> SwitchToSceneAsync(string sceneRuntimeKey) => SwitchToSceneFlowAsync(new AddressableLoadSceneInfo(sceneRuntimeKey));

        public Task<SceneInstance> LoadSceneAsync(AssetReference sceneReference, bool setActive = false) => LoadSceneFlowAsync(new AddressableLoadSceneInfo(sceneReference), setActive);

        public Task<SceneInstance> LoadSceneAsync(string sceneRuntimeKey, bool setActive = false) => LoadSceneFlowAsync(new AddressableLoadSceneInfo(sceneRuntimeKey), setActive);

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

        async Task<AsyncOperationHandle<SceneInstance>> LoadSceneAsyncWithReport(AddressableLoadSceneInfo sceneInfo, SceneLoadProgressDelegate progressCallback)
        {
            var operation = sceneInfo.LoadSceneAsync();
            while (!operation.IsDone)
            {
                progressCallback?.Invoke(operation.PercentComplete);
                await Task.Yield();
            }
            _loadedSceneHandles.Add(operation);
            return operation;
        }

        async Task<SceneInstance> LoadSceneFlowAsync(AddressableLoadSceneInfo sceneInfo, bool setActive)
        {
            var operation = sceneInfo.LoadSceneAsync();
            await operation.Task;
            if (setActive)
                _activeSceneHandle = operation;
            _loadedSceneHandles.Add(operation);
            return operation.Result;
        }

        async Task<SceneInstance> TransitionToSceneFlowAsync(AddressableLoadSceneInfo sceneInfo)
        {
            var currentSceneHandle = _activeSceneHandle;

            var loadingScene = await _loadingSceneInfo.LoadSceneAsync().Task;

            var loadingBehavior = Object.FindObjectOfType<LoadingBehavior>();
            while (!loadingBehavior.Active)
                await Task.Yield();

            _activeSceneHandle = await LoadSceneAsyncWithReport(sceneInfo, loadingBehavior.Report);
            loadingBehavior.CompleteLoading();

            if (currentSceneHandle.IsValid())
                _ = UnloadSceneAsync(currentSceneHandle);

            while (loadingBehavior.Active)
                await Task.Yield();
            _ = UnloadSceneAsync(loadingScene);

            return _activeSceneHandle.Result;
        }

        async Task<SceneInstance> SwitchToSceneFlowAsync(AddressableLoadSceneInfo sceneInfo)
        {
            var currentSceneHandle = _activeSceneHandle;

            var operation = sceneInfo.LoadSceneAsync();
            await operation.Task;
            _activeSceneHandle = operation;
            _loadedSceneHandles.Add(operation);

            if (currentSceneHandle.IsValid())
                _ = UnloadSceneAsync(currentSceneHandle);

            return _activeSceneHandle.Result;
        }
    }
}
#endif