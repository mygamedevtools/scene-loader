#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
/**
 * IAddressableUniTaskSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/15/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport.UniTaskSupport
{
    public interface IAddressableUniTaskSceneLoader : IAddressableSceneLoader
    {
        UniTask<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneInfo sceneInfo);

        UniTask<SceneInstance> SwitchToSceneAsync(IAddressableLoadSceneInfo sceneInfo);

        UniTask<SceneInstance> LoadSceneAsync(IAddressableLoadSceneInfo sceneInfo, bool setActive = false);

        UniTask UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle);
        UniTask UnloadSceneAsync(SceneInstance scene);
        UniTask UnloadSceneAsync(string sceneName);
    }
}
#endif