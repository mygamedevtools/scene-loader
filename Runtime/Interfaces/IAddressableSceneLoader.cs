#if ENABLE_ADDRESSABLES
/**
 * IAddressableSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/23/2022 (en-US)
 */

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public interface IAddressableSceneLoader
    {
        IAddressableSceneManager SceneManager { get; }

        void TransitionToScene(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference = null);

        void LoadScene(IAddressableLoadSceneReference sceneReference, bool setActive = false); 
        
        void UnloadScene(IAddressableLoadSceneInfo sceneInfo);
    }
}
#endif