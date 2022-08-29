#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
/**
 * AddressableSceneLoaderUniTask.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/5/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport.UniTaskSupport
{
    public class AddressableSceneLoaderUniTask : IAddressableSceneLoaderUniTask
    {
        public IAddressableSceneManager SceneManager => _sceneManager;

        readonly IAddressableSceneManager _sceneManager;

        public AddressableSceneLoaderUniTask(IAddressableSceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public void TransitionToScene(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference) => TransitionToSceneAsync(targetSceneReference, intermediateSceneReference);

        public void LoadScene(IAddressableLoadSceneReference sceneReference, bool setActive) => LoadSceneAsync(sceneReference, setActive).Forget();

        public void UnloadScene(IAddressableLoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo);

        public async UniTask<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive)
        {
            var operation = sceneReference.LoadSceneAsync(_sceneManager);
            await operation.ToUniTask();
            if (setActive)
                _sceneManager.SetActiveSceneHandle(operation);
            return operation.Result;
        }

        public UniTask<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference) => intermediateSceneReference == null ? TransitionDirectlyAsync(targetSceneReference) : TransitionToSceneFlowAsync(targetSceneReference, intermediateSceneReference);

        public UniTask UnloadSceneAsync(IAddressableLoadSceneInfo sceneInfo) => sceneInfo.UnloadSceneAsync(_sceneManager).ToUniTask();

        async UniTask<AsyncOperationHandle<SceneInstance>> LoadSceneAsyncWithReport(IAddressableLoadSceneReference sceneReference, System.IProgress<float> progress)
        {
            var operation = sceneReference.LoadSceneAsync(_sceneManager);
            await operation.ToUniTask(progress);
            _sceneManager.SetActiveSceneHandle(operation);
            return operation;
        }

        async UniTask<SceneInstance> TransitionToSceneFlowAsync(IAddressableLoadSceneReference sceneReference, IAddressableLoadSceneReference intermediateSceneReference)
        {
            var currentSceneHandle = _sceneManager.GetActiveSceneHandle();
            var loadingScene = await intermediateSceneReference.LoadSceneAsync(_sceneManager).Task;
            var loadingBehavior = Object.FindObjectOfType<LoadingBehavior>();

            SceneInstance result;

            if (loadingBehavior)
            {
                await UniTask.WaitWhile(() => !loadingBehavior.Active);

                var operation = await LoadSceneAsyncWithReport(sceneReference, loadingBehavior);
                loadingBehavior.CompleteLoading();

                if (currentSceneHandle.IsValid())
                    UnloadSceneAsync(new AddressableLoadSceneInfoOperationHandle(currentSceneHandle)).Forget();

                await UniTask.WaitWhile(() => loadingBehavior.Active);

                UnloadSceneAsync(new AddressableLoadSceneInfoInstance(loadingScene)).Forget();

                result = operation.Result;
            }
            else
            {
                result = await LoadSceneAsync(sceneReference, true);
                UnloadSceneAsync(new AddressableLoadSceneInfoOperationHandle(currentSceneHandle)).Forget();
                UnloadSceneAsync(new AddressableLoadSceneInfoInstance(loadingScene)).Forget();
            }
            return result;
        }

        async UniTask<SceneInstance> TransitionDirectlyAsync(IAddressableLoadSceneReference sceneReference)
        {
            var currentSceneHandle = _sceneManager.GetActiveSceneHandle();
            var loadedScene = await LoadSceneAsync(sceneReference, true);

            if (currentSceneHandle.IsValid())
                _ = UnloadSceneAsync(new AddressableLoadSceneInfoOperationHandle(currentSceneHandle));

            return loadedScene;
        }
    }
}
#endif