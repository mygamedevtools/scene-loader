#if ENABLE_ADDRESSABLES
/**
 * IAddressableSceneLoaderAsync.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/26/2022 (en-US)
 */

using System.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public interface IAddressableSceneLoaderAsync : IAddressableSceneLoader
    {
        Task<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneInfo sceneInfo);
        
        Task<SceneInstance> SwitchToSceneAsync(IAddressableLoadSceneInfo sceneInfo);

        Task<SceneInstance> LoadSceneAsync(IAddressableLoadSceneInfo sceneInfo, bool setActive = false);

        Task UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle);
        Task UnloadSceneAsync(SceneInstance scene);
        Task UnloadSceneAsync(string sceneName);
    }
}
#endif