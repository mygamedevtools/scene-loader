#if ENABLE_ADDRESSABLES
/**
 * IAddressableSceneManager.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/29/2022 (en-US)
 */

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public interface IAddressableSceneManager
    {
        void SetActiveSceneHandle(AsyncOperationHandle<SceneInstance> sceneHandle);

        AsyncOperationHandle<SceneInstance> GetActiveSceneHandle();

        AsyncOperationHandle<SceneInstance> LoadSceneAsync(AssetReference sceneReference);
        AsyncOperationHandle<SceneInstance> LoadSceneAsync(string runtimeKey);

        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle);
        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene);
        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(string sceneName);

        AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(SceneInstance sceneInstance);
        AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(string sceneName);
    }
}
#endif