#if ENABLE_ADDRESSABLES
/**
 * IAddressableSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/23/2022 (en-US)
 */

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public interface IAddressableSceneLoader
    {
        void TransitionToScene(IAddressableLoadSceneInfo sceneInfo);

        void SwitchToScene(IAddressableLoadSceneInfo sceneInfo);

        void LoadScene(IAddressableLoadSceneInfo sceneInfo, bool setActive = false); 
        
        void UnloadScene(AsyncOperationHandle<SceneInstance> sceneHandle);
        void UnloadScene(SceneInstance scene);
        void UnloadScene(string sceneName);

        AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(SceneInstance sceneInstance);
        AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(string sceneName);
    }
}
#endif