#if ENABLE_ADDRESSABLES
/**
 * AddressableSceneLoaderAsync.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/26/2022 (en-US)
 */

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public class AddressableSceneLoaderAsync : IAddressableSceneLoaderAsync
    {
        public IAddressableSceneManager SceneManager => _sceneManager;

        readonly IAddressableSceneManager _sceneManager;

        public AddressableSceneLoaderAsync(IAddressableSceneManager sceneManager)
        {
            _sceneManager = sceneManager;
        }

        public void TransitionToScene(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference) => TransitionToSceneAsync(targetSceneReference, intermediateSceneReference);

        public void LoadScene(IAddressableLoadSceneReference sceneReference, bool setActive) => _ = LoadSceneAsync(sceneReference, setActive);

        public void UnloadScene(IAddressableLoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo);

        public async Task<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive = false)
        {
            var operation = sceneReference.LoadSceneAsync(_sceneManager);
            await operation.Task;
            if (setActive)
                _sceneManager.SetActiveSceneHandle(operation);
            return operation.Result;
        }

        public Task<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference) => intermediateSceneReference == null ? TransitionDirectlyAsync(targetSceneReference) : TransitionToSceneFlowAsync(targetSceneReference, intermediateSceneReference);

        public Task UnloadSceneAsync(IAddressableLoadSceneInfo sceneInfo) => sceneInfo.UnloadSceneAsync(_sceneManager).Task;

        async Task<AsyncOperationHandle<SceneInstance>> LoadSceneAsyncWithReport(IAddressableLoadSceneReference sceneReference, SceneLoadProgressDelegate progressCallback)
        {
            var operation = sceneReference.LoadSceneAsync(_sceneManager);
            while (!operation.IsDone)
            {
                progressCallback?.Invoke(operation.PercentComplete);
                await Task.Yield();
            }
            _sceneManager.SetActiveSceneHandle(operation);
            return operation;
        }

        async Task<SceneInstance> TransitionToSceneFlowAsync(IAddressableLoadSceneReference sceneReference, IAddressableLoadSceneReference intermediateSceneReference)
        {
            var currentSceneHandle = _sceneManager.GetActiveSceneHandle();
            var loadingScene = await intermediateSceneReference.LoadSceneAsync(_sceneManager).Task;
            var loadingBehavior = Object.FindObjectOfType<LoadingBase>();

            SceneInstance result;

            if (loadingBehavior)
            {
                while (!loadingBehavior.Active)
                    await Task.Yield();

                var operation = await LoadSceneAsyncWithReport(sceneReference, loadingBehavior.Report);
                loadingBehavior.CompleteLoading();

                if (currentSceneHandle.IsValid())
                    _ = UnloadSceneAsync(new AddressableLoadSceneInfoOperationHandle(currentSceneHandle));

                while (loadingBehavior.Active)
                    await Task.Yield();
                _ = UnloadSceneAsync(new AddressableLoadSceneInfoInstance(loadingScene));

                result = operation.Result;
            }
            else
            {
                result = await LoadSceneAsync(sceneReference, true);
                if (currentSceneHandle.IsValid())
                    _ = UnloadSceneAsync(new AddressableLoadSceneInfoOperationHandle(currentSceneHandle));
                _ = UnloadSceneAsync(new AddressableLoadSceneInfoInstance(loadingScene));
            }
            return result;
        }

        async Task<SceneInstance> TransitionDirectlyAsync(IAddressableLoadSceneReference sceneReference)
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