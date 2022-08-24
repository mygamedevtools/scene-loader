/**
 * AsyncSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading
{
    public class AsyncSceneLoader : IAsyncSceneLoader
    {
        readonly ILoadSceneInfo _loadingSceneInfo;

        public AsyncSceneLoader(int loadingSceneBuildIndex = -1)
        {
            _loadingSceneInfo = new LoadSceneInfoIndex(loadingSceneBuildIndex);
        }

        public void TransitionToScene(ILoadSceneInfo sceneInfo) => TransitionToSceneAsync(sceneInfo);

        public void SwitchToScene(ILoadSceneInfo sceneInfo) => SwitchToSceneAsync(sceneInfo);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => _ = UnloadSceneAsync(sceneInfo);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => _ = LoadSceneAsync(sceneInfo, setActive);

        public Task TransitionToSceneAsync(ILoadSceneInfo sceneInfo) => TransitionToSceneFlowAsync(sceneInfo);

        public Task SwitchToSceneAsync(ILoadSceneInfo sceneInfo) => SwitchToSceneFlowAsync(sceneInfo);

        public async Task UnloadSceneAsync(ILoadSceneInfo sceneInfo)
        {
            var operation = sceneInfo.UnloadSceneAsync();
            while (!operation.isDone)
                await Task.Yield();
        }

        public async Task LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive)
        {
            var operation = sceneInfo.LoadSceneAsync();
            while (!operation.isDone)
                await Task.Yield();

            if (setActive)
                SceneManager.SetActiveScene(sceneInfo.GetScene());
        }

        async Task TransitionToSceneFlowAsync(ILoadSceneInfo loadSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfoIndex(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(_loadingSceneInfo, true);

            var loadingBehavior = UnityEngine.Object.FindObjectOfType<LoadingBehavior>();
            while (!loadingBehavior.Active)
                await Task.Yield();

            await LoadSceneAsyncWithReport(loadSceneInfo, loadingBehavior);
            loadingBehavior.CompleteLoading();
            _ = UnloadSceneAsync(currentSceneInfo);

            while (loadingBehavior.Active)
                await Task.Yield();
            _ = UnloadSceneAsync(_loadingSceneInfo);
        }

        async Task SwitchToSceneFlowAsync(ILoadSceneInfo loadSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfoIndex(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(loadSceneInfo, true);
            _ = UnloadSceneAsync(currentSceneInfo);
        }

        async Task LoadSceneAsyncWithReport(ILoadSceneInfo loadSceneInfo, IProgress<float> progress)
        {
            var operation = loadSceneInfo.LoadSceneAsync();
            while (!operation.isDone)
            {
                progress.Report(operation.progress);
                await Task.Yield();
            }
            SceneManager.SetActiveScene(loadSceneInfo.GetScene());
        }
    }
}