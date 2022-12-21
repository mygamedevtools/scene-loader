#if ENABLE_ADDRESSABLES
/**
 * AddressableSceneLoaderAsync.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/26/2022 (en-US)
 */

using System;
using System.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;

namespace MyGameDevTools.SceneLoading.AddressablesSupport
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

        public void UnloadScene(IAddressableLoadSceneInfo sceneInfo) => _ = UnloadSceneAsync(sceneInfo);

        public async Task<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive = false, IProgress<float> progress = null) => await _sceneManager.LoadSceneAsync(sceneReference, setActive, progress);

        public Task<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference) => intermediateSceneReference == null ? TransitionDirectlyAsync(targetSceneReference) : TransitionToSceneFlowAsync(targetSceneReference, intermediateSceneReference);

        public async Task UnloadSceneAsync(IAddressableLoadSceneInfo sceneInfo) => await _sceneManager.UnloadSceneAsync(sceneInfo);

        async Task<SceneInstance> TransitionToSceneFlowAsync(IAddressableLoadSceneReference sceneReference, IAddressableLoadSceneReference intermediateSceneReference)
        {
            var currentScene = _sceneManager.GetActiveScene();
            var loadingScene = await LoadSceneAsync(intermediateSceneReference);
            var loadingBehavior = Object.FindObjectOfType<LoadingBase>();

            SceneInstance result;

            if (loadingBehavior)
            {
                while (!loadingBehavior.Active)
                    await Task.Yield();

                if (currentScene.Scene.IsValid())
                    await UnloadSceneAsync(new AddressableLoadSceneInfoInstance(currentScene));

                result = await LoadSceneAsync(sceneReference, true, loadingBehavior);
                loadingBehavior.CompleteLoading();

                while (loadingBehavior.Active)
                    await Task.Yield();
                _ = UnloadSceneAsync(new AddressableLoadSceneInfoInstance(loadingScene));
            }
            else
            {
                if (currentScene.Scene.IsValid())
                    await UnloadSceneAsync(new AddressableLoadSceneInfoInstance(currentScene));
                result = await LoadSceneAsync(sceneReference, true);
                _ = UnloadSceneAsync(new AddressableLoadSceneInfoInstance(loadingScene));
            }
            return result;
        }

        async Task<SceneInstance> TransitionDirectlyAsync(IAddressableLoadSceneReference sceneReference)
        {
            var currentScene = _sceneManager.GetActiveScene();
            if (currentScene.Scene.IsValid())
                await UnloadSceneAsync(new AddressableLoadSceneInfoInstance(currentScene));
            var loadedScene = await LoadSceneAsync(sceneReference, true);

            return loadedScene;
        }
    }
}
#endif