#if ENABLE_ADDRESSABLES
/**
 * AddressableSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/26/2022 (en-US)
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public class AddressableAsyncSceneLoader : IAddressableAsyncSceneLoader
    {
        readonly List<AsyncOperationHandle<SceneInstance>> _loadedSceneHandles;
        readonly IAddressableLoadSceneInfo _loadingSceneInfo;

        AsyncOperationHandle<SceneInstance> _activeSceneHandle;

        public AddressableAsyncSceneLoader(AssetReference loadingSceneReference)
        {
            _loadedSceneHandles = new List<AsyncOperationHandle<SceneInstance>>();
            _loadingSceneInfo = new AddressableLoadSceneInfoAsset(loadingSceneReference);
        }

        public void TransitionToScene(IAddressableLoadSceneInfo sceneInfo) => TransitionToSceneAsync(sceneInfo);

        public void SwitchToScene(IAddressableLoadSceneInfo sceneInfo) => SwitchToSceneAsync(sceneInfo);

        public void LoadScene(IAddressableLoadSceneInfo sceneInfo, bool setActive) => LoadSceneAsync(sceneInfo, setActive);

        public void UnloadScene(AsyncOperationHandle<SceneInstance> sceneHandle) => UnloadSceneAsync(sceneHandle);
        public void UnloadScene(SceneInstance scene) => UnloadSceneAsync(scene);
        public void UnloadScene(string sceneName) => UnloadSceneAsync(sceneName);

        public Task<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneInfo sceneInfo) => TransitionToSceneFlowAsync(sceneInfo);

        public Task<SceneInstance> SwitchToSceneAsync(IAddressableLoadSceneInfo sceneInfo) => SwitchToSceneFlowAsync(sceneInfo);

        public Task<SceneInstance> LoadSceneAsync(IAddressableLoadSceneInfo sceneInfo, bool setActive = false) => LoadSceneFlowAsync(sceneInfo, setActive);

        public Task UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            var operation = Addressables.UnloadSceneAsync(sceneHandle);
            _loadedSceneHandles.Remove(sceneHandle);
            return operation.Task;
        }
        public Task UnloadSceneAsync(SceneInstance scene)
        {
            var operation = Addressables.UnloadSceneAsync(scene);
            _loadedSceneHandles.Remove(GetLoadedSceneHandle(scene));
            return operation.Task;
        }
        public Task UnloadSceneAsync(string sceneName)
        {
            var loadedSceneHandle = GetLoadedSceneHandle(sceneName);
            var operation = Addressables.UnloadSceneAsync(loadedSceneHandle);
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

        async Task<AsyncOperationHandle<SceneInstance>> LoadSceneAsyncWithReport(IAddressableLoadSceneInfo sceneInfo, SceneLoadProgressDelegate progressCallback)
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

        async Task<SceneInstance> LoadSceneFlowAsync(IAddressableLoadSceneInfo sceneInfo, bool setActive)
        {
            var operation = sceneInfo.LoadSceneAsync();
            await operation.Task;
            if (setActive)
                _activeSceneHandle = operation;
            _loadedSceneHandles.Add(operation);
            return operation.Result;
        }

        async Task<SceneInstance> TransitionToSceneFlowAsync(IAddressableLoadSceneInfo sceneInfo)
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

        async Task<SceneInstance> SwitchToSceneFlowAsync(IAddressableLoadSceneInfo sceneInfo)
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