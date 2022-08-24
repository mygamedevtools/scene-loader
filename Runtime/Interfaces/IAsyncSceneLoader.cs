/**
 * IAsyncSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/15/2022 (en-US)
 */

using System.Threading.Tasks;

namespace MyUnityTools.SceneLoading
{
    public interface IAsyncSceneLoader : ISceneLoader
    {
        Task TransitionToSceneAsync(ILoadSceneInfo sceneInfo);

        Task SwitchToSceneAsync(ILoadSceneInfo sceneInfo);

        Task UnloadSceneAsync(ILoadSceneInfo sceneInfo);

        Task LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false);
    }
}