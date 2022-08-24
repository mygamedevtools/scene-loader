/**
 * ISceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

namespace MyUnityTools.SceneLoading
{
    public interface ISceneLoader
    {
        void TransitionToScene(ILoadSceneInfo sceneInfo);

        void SwitchToScene(ILoadSceneInfo sceneInfo);

        void UnloadScene(ILoadSceneInfo sceneInfo);

        void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false);
    }
}