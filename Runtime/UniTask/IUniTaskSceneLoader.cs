#if ENABLE_UNITASK
/**
* IUniTaskSceneLoader.cs
* Created by: João Borks [joao.borks@gmail.com]
* Created on: 8/15/2022 (en-US)
*/

using Cysharp.Threading.Tasks;

namespace MyUnityTools.SceneLoading.UniTaskSupport
{
    public interface IUniTaskSceneLoader
    {
        public UniTask TransitionToSceneAsync(LoadSceneInfo sceneInfo);

        public UniTask SwitchToSceneAsync(LoadSceneInfo sceneInfo);

        public UniTask UnloadSceneAsync(LoadSceneInfo sceneInfo);

        public UniTask LoadSceneAsync(LoadSceneInfo sceneInfo, bool setActive = false);
    }
}
#endif