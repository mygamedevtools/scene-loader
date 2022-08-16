/**
 * IAsyncSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/15/2022 (en-US)
 */

using System.Threading.Tasks;

namespace MyUnityTools.SceneLoading
{
    public interface IAsyncSceneLoader
    {
        public Task TransitionToSceneAsync(LoadSceneInfo sceneInfo);

        public Task SwitchToSceneAsync(LoadSceneInfo sceneInfo);

        public Task UnloadSceneAsync(LoadSceneInfo sceneInfo);

        public Task LoadSceneAsync(LoadSceneInfo sceneInfo, bool setActive = false);
    }
}