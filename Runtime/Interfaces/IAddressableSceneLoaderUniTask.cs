#if ENABLE_ADDRESSABLES && ENABLE_UNITASK
/**
 * IAddressableSceneLoaderUniTask.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/15/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport.UniTaskSupport
{
    public interface IAddressableSceneLoaderUniTask : IAddressableSceneLoader
    {
        UniTask<SceneInstance> TransitionToSceneAsync(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference = null);

        UniTask<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive = false);

        UniTask UnloadSceneAsync(IAddressableLoadSceneInfo sceneInfo);
    }
}
#endif