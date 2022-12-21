#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
/**
 * AddressableSceneLoaderUniTask.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/5/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using System;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;

namespace MyGameDevTools.SceneLoading.AddressablesSupport.UniTaskSupport
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

        public void UnloadScene(IAddressableLoadSceneReference sceneInfo) => UnloadSceneAsync(sceneInfo).Forget();

        public async UniTask<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive = false, IProgress<float> progress = null) => await _sceneManager.LoadSceneAsync(sceneReference, setActive, progress);

        public UniTask<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference) => intermediateSceneReference == null ? TransitionDirectlyAsync(targetSceneReference) : TransitionToSceneFlowAsync(targetSceneReference, intermediateSceneReference);

        public async UniTask UnloadSceneAsync(IAddressableLoadSceneReference sceneInfo) => await _sceneManager.UnloadSceneAsync(sceneInfo);

        async UniTask<SceneInstance> TransitionToSceneFlowAsync(IAddressableLoadSceneReference sceneReference, IAddressableLoadSceneReference intermediateSceneReference)
        {
            var currentScene = _sceneManager.GetActiveScene();
            var loadingScene = await _sceneManager.LoadSceneAsync(intermediateSceneReference);
            var loadingBehavior = Object.FindObjectOfType<LoadingBase>();

            SceneInstance result;

            if (loadingBehavior)
            {
                await UniTask.WaitWhile(() => !loadingBehavior.Active);

                if (currentScene.Scene.IsValid())
                    await UnloadSceneAsync(new AddressableLoadSceneInfoInstance(currentScene));

                result = await LoadSceneAsync(sceneReference, true, loadingBehavior);
                loadingBehavior.CompleteLoading();

                await UniTask.WaitWhile(() => loadingBehavior.Active);

                UnloadSceneAsync(new AddressableLoadSceneInfoInstance(loadingScene)).Forget();
            }
            else
            {
                if (currentScene.Scene.IsValid())
                    await UnloadSceneAsync(new AddressableLoadSceneInfoInstance(currentScene));
                result = await LoadSceneAsync(sceneReference, true);
                UnloadSceneAsync(new AddressableLoadSceneInfoInstance(loadingScene)).Forget();
            }
            return result;
        }

        async UniTask<SceneInstance> TransitionDirectlyAsync(IAddressableLoadSceneReference sceneReference)
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