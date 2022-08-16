#if ENABLE_ADDRESSABLES
/**
 * IAddressableSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/26/2022 (en-US)
 */

using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.Addressables
{
    public interface IAddressableSceneLoader
    {
        public Task<SceneInstance> TransitionToSceneAsync(AssetReference sceneReference);
        public Task<SceneInstance> TransitionToSceneAsync(string sceneRuntimeKey);
        
        public Task<SceneInstance> SwitchToSceneAsync(AssetReference sceneReference);
        public Task<SceneInstance> SwitchToSceneAsync(string sceneRuntimeKey);

        public Task<SceneInstance> LoadSceneAsync(AssetReference sceneReference, bool setActive = false);
        public Task<SceneInstance> LoadSceneAsync(string sceneRuntimeKey, bool setActive = false);

        public Task UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle);
        public Task UnloadSceneAsync(SceneInstance scene);
        public Task UnloadSceneAsync(string sceneName);
    }
}
#endif