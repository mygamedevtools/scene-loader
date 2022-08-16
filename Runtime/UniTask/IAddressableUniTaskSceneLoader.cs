#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
/**
 * IAddressableUniTaskSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/15/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.Addressables.UniTaskSupport
{
    public interface IAddressableUniTaskSceneLoader
    {
        public UniTask<SceneInstance> TransitionToSceneAsync(AssetReference sceneReference);
        public UniTask<SceneInstance> TransitionToSceneAsync(string sceneRuntimeKey);

        public UniTask<SceneInstance> SwitchToSceneAsync(AssetReference sceneReference);
        public UniTask<SceneInstance> SwitchToSceneAsync(string sceneRuntimeKey);

        public UniTask<SceneInstance> LoadSceneAsync(AssetReference sceneReference, bool setActive = false);
        public UniTask<SceneInstance> LoadSceneAsync(string sceneRuntimeKey, bool setActive = false);

        public UniTask UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle);
        public UniTask UnloadSceneAsync(SceneInstance scene);
        public UniTask UnloadSceneAsync(string sceneName);

        public AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(SceneInstance sceneInstance);
        public AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(string sceneName);
    }
}
#endif