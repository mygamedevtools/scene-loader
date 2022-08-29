#if ENABLE_ADDRESSABLES
/**
 * AddressableLoadSceneInfoName.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/29/2022 (en-US)
 */

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public readonly struct AddressableLoadSceneInfoName : IAddressableLoadSceneInfo
    {
        readonly string _sceneName;

        public AddressableLoadSceneInfoName(string sceneName)
        {
            _sceneName = sceneName;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(IAddressableSceneManager sceneManager) => sceneManager.UnloadSceneAsync(_sceneName);
    }
}
#endif