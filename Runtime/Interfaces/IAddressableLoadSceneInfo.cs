#if ENABLE_ADDRESSABLES
/**
 * IAddressableUnloadSceneInfo.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public interface IAddressableLoadSceneInfo
    {
        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(IAddressableSceneManager sceneManager);
    }
}
#endif