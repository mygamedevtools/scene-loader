#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
/**
 * AddressableUniTaskSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/5/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using MyUnityTools.SceneLoader.Addressables;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.Addressables.UniTaskSupport
{
    public class AddressableUniTaskSceneLoader
    {
        readonly List<AsyncOperationHandle<SceneInstance>> _loadedSceneHandles;
        readonly AddressableLoadSceneInfo _loadingSceneInfo;

        AsyncOperationHandle<SceneInstance> _activeSceneHandle;

        public AddressableUniTaskSceneLoader(AssetReference loadingSceneReference)
        {
            _loadedSceneHandles = new List<AsyncOperationHandle<SceneInstance>>();
            _loadingSceneInfo = new AddressableLoadSceneInfo(loadingSceneReference);
        }

        public UniTask<SceneInstance> TransitionToSceneAsync(AssetReference sceneReference) => TransitionToSceneFlowAsync(new AddressableLoadSceneInfo(sceneReference));

        public UniTask<SceneInstance> TransitionToSceneAsync(string sceneRuntimeKey) => TransitionToSceneFlowAsync(new AddressableLoadSceneInfo(sceneRuntimeKey));

        public UniTask<SceneInstance> SwitchToSceneAsync(AssetReference sceneReference) => SwitchToSceneFlowAsync(new AddressableLoadSceneInfo(sceneReference));

        public UniTask<SceneInstance> SwitchToSceneAsync(string sceneRuntimeKey) => SwitchToSceneFlowAsync(new AddressableLoadSceneInfo(sceneRuntimeKey));

        public UniTask<SceneInstance> LoadSceneAsync(AssetReference sceneReference, bool setActive = false) => LoadSceneFlowAsync(new AddressableLoadSceneInfo(sceneReference), setActive);

        public UniTask<SceneInstance> LoadSceneAsync(string sceneRuntimeKey, bool setActive = false) => LoadSceneFlowAsync(new AddressableLoadSceneInfo(sceneRuntimeKey), setActive);

        public UniTask UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            var operation = UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(sceneHandle);
            _loadedSceneHandles.Remove(sceneHandle);
            return operation.ToUniTask();
        }
        public UniTask UnloadSceneAsync(SceneInstance scene)
        {
            var operation = UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(scene);
            _loadedSceneHandles.Remove(GetLoadedSceneHandle(scene));
            return operation.ToUniTask();
        }
        public UniTask UnloadSceneAsync(string sceneName)
        {
            var loadedSceneHandle = GetLoadedSceneHandle(sceneName);
            var operation = UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(loadedSceneHandle);
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

        async UniTask<AsyncOperationHandle<SceneInstance>> LoadSceneAsyncWithReport(AddressableLoadSceneInfo sceneInfo, IProgress<float> progress)
        {
            var operation = sceneInfo.LoadSceneAsync();
            await operation.ToUniTask(progress);
            _loadedSceneHandles.Add(operation);
            return operation;
        }

        async UniTask<SceneInstance> LoadSceneFlowAsync(AddressableLoadSceneInfo sceneInfo, bool setActive)
        {
            var operation = sceneInfo.LoadSceneAsync();
            await operation.ToUniTask();

            if (setActive)
                _activeSceneHandle = operation;

            _loadedSceneHandles.Add(operation);
            return operation.Result;
        }

        async UniTask<SceneInstance> TransitionToSceneFlowAsync(AddressableLoadSceneInfo sceneInfo)
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

        async UniTask<SceneInstance> SwitchToSceneFlowAsync(AddressableLoadSceneInfo sceneInfo)
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