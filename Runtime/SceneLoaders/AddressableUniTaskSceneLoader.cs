#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
/**
 * AddressableUniTaskSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/5/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport.UniTaskSupport
{
    public class AddressableUniTaskSceneLoader : IAddressableSceneLoaderUniTask
    {
        readonly List<AsyncOperationHandle<SceneInstance>> _loadedSceneHandles;
        readonly IAddressableLoadSceneInfo _loadingSceneInfo;

        AsyncOperationHandle<SceneInstance> _activeSceneHandle;

        public AddressableUniTaskSceneLoader(AssetReference loadingSceneReference)
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

        public UniTask<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneInfo sceneInfo) => TransitionToSceneFlowAsync(sceneInfo);

        public UniTask<SceneInstance> SwitchToSceneAsync(IAddressableLoadSceneInfo sceneInfo) => SwitchToSceneFlowAsync(sceneInfo);

        public UniTask<SceneInstance> LoadSceneAsync(IAddressableLoadSceneInfo sceneInfo, bool setActive) => LoadSceneFlowAsync(sceneInfo, setActive);

        public UniTask UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            var operation = Addressables.UnloadSceneAsync(sceneHandle);
            _loadedSceneHandles.Remove(sceneHandle);
            return operation.ToUniTask();
        }
        public UniTask UnloadSceneAsync(SceneInstance scene)
        {
            var operation = Addressables.UnloadSceneAsync(scene);
            _loadedSceneHandles.Remove(GetLoadedSceneHandle(scene));
            return operation.ToUniTask();
        }
        public UniTask UnloadSceneAsync(string sceneName)
        {
            var loadedSceneHandle = GetLoadedSceneHandle(sceneName);
            var operation = Addressables.UnloadSceneAsync(loadedSceneHandle);
            _loadedSceneHandles.Remove(loadedSceneHandle);
            return operation.ToUniTask();
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

        async UniTask<AsyncOperationHandle<SceneInstance>> LoadSceneAsyncWithReport(IAddressableLoadSceneInfo sceneInfo, IProgress<float> progress)
        {
            var operation = sceneInfo.LoadSceneAsync();
            await operation.ToUniTask(progress);
            _loadedSceneHandles.Add(operation);
            return operation;
        }

        async UniTask<SceneInstance> LoadSceneFlowAsync(IAddressableLoadSceneInfo sceneInfo, bool setActive)
        {
            var operation = sceneInfo.LoadSceneAsync();
            await operation.ToUniTask();

            if (setActive)
                _activeSceneHandle = operation;

            _loadedSceneHandles.Add(operation);
            return operation.Result;
        }

        async UniTask<SceneInstance> TransitionToSceneFlowAsync(IAddressableLoadSceneInfo sceneInfo)
        {
            var currentSceneHandle = _activeSceneHandle;

            var loadingScene = await _loadingSceneInfo.LoadSceneAsync().ToUniTask();

            var loadingBehavior = UnityEngine.Object.FindObjectOfType<LoadingBehavior>();

            await UniTask.WaitWhile(() => !loadingBehavior.Active);

            _activeSceneHandle = await LoadSceneAsyncWithReport(sceneInfo, loadingBehavior);
            loadingBehavior.CompleteLoading();

            if (currentSceneHandle.IsValid())
                UnloadSceneAsync(currentSceneHandle).Forget();

            await UniTask.WaitWhile(() => loadingBehavior.Active);

            UnloadSceneAsync(loadingScene).Forget();

            return _activeSceneHandle.Result;
        }

        async UniTask<SceneInstance> SwitchToSceneFlowAsync(IAddressableLoadSceneInfo sceneInfo)
        {
            var currentSceneHandle = _activeSceneHandle;

            var operation = sceneInfo.LoadSceneAsync();
            await operation.ToUniTask();

            _activeSceneHandle = operation;
            _loadedSceneHandles.Add(operation);

            if (currentSceneHandle.IsValid())
                UnloadSceneAsync(currentSceneHandle).Forget();

            return _activeSceneHandle.Result;
        }
    }
}
#endif