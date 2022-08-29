#if ENABLE_ADDRESSABLES
/**
 * AddressableLoadSceneInfoOperationHandle.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public readonly struct AddressableLoadSceneInfoOperationHandle : IAddressableLoadSceneInfo
    {
        readonly AsyncOperationHandle<SceneInstance> _sceneHandle;

        public AddressableLoadSceneInfoOperationHandle(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            _sceneHandle = sceneHandle;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(IAddressableSceneManager sceneManager) => sceneManager.UnloadSceneAsync(_sceneHandle);
    }
}
#endif