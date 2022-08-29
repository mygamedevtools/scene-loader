#if ENABLE_ADDRESSABLES
/**
 * IAddressableSceneLoaderAsync.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/26/2022 (en-US)
 */

using System.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public interface IAddressableSceneLoaderAsync : IAddressableSceneLoader
    {
        Task<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference = null);

        Task<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive = false);

        Task UnloadSceneAsync(IAddressableLoadSceneInfo sceneInfo);
    }
}
#endif