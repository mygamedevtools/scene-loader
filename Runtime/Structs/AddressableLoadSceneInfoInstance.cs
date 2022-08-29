#if ENABLE_ADDRESSABLES
/**
 * AddressableLoadSceneInfoInstance.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/29/2022 (en-US)
 */

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public readonly struct AddressableLoadSceneInfoInstance : IAddressableLoadSceneInfo
    {
        readonly SceneInstance _sceneInstance;

        public AddressableLoadSceneInfoInstance(SceneInstance sceneInstance)
        {
            _sceneInstance = sceneInstance;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(IAddressableSceneManager sceneManager) => sceneManager.UnloadSceneAsync(_sceneInstance);
    }
}
#endif