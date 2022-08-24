#if ENABLE_UNITASK
/**
 * IUniTaskSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/15/2022 (en-US)
 */

using Cysharp.Threading.Tasks;

namespace MyUnityTools.SceneLoading.UniTaskSupport
{
    public interface IUniTaskSceneLoader : ISceneLoader
    {
        UniTask TransitionToSceneAsync(ILoadSceneInfo sceneInfo);

        UniTask SwitchToSceneAsync(ILoadSceneInfo sceneInfo);

        UniTask UnloadSceneAsync(ILoadSceneInfo sceneInfo);

        UniTask LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false);
    }
}
#endif