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
    public class AsyncSceneLoader : ISceneLoader, IAsyncSceneLoader
    {
        readonly LoadSceneInfo _loadingSceneInfo;

        public AsyncSceneLoader(int loadingSceneBuildIndex = -1)
        {
            _loadingSceneInfo = new LoadSceneInfo(loadingSceneBuildIndex);
        }

        public void TransitionToScene(string name) => TransitionToSceneAsync(new LoadSceneInfo(name));
        public void TransitionToScene(int index) => TransitionToSceneAsync(new LoadSceneInfo(index));

        public void SwitchToScene(string name) => SwitchToSceneAsync(new LoadSceneInfo(name));
        public void SwitchToScene(int index) => SwitchToSceneAsync(new LoadSceneInfo(index));

        public void UnloadScene(string name) => _ = UnloadSceneAsync(new LoadSceneInfo(name));
        public void UnloadScene(int index) => _ = UnloadSceneAsync(new LoadSceneInfo(index));

        public void LoadScene(string name, bool setActive) => _ = LoadSceneAsync(new LoadSceneInfo(name), setActive);
        public void LoadScene(int index, bool setActive) => _ = LoadSceneAsync(new LoadSceneInfo(index), setActive);

        public Task TransitionToSceneAsync(LoadSceneInfo sceneInfo) => TransitionToSceneFlowAsync(sceneInfo);

        public Task SwitchToSceneAsync(LoadSceneInfo sceneInfo) => SwitchToSceneFlowAsync(sceneInfo);

        public async Task UnloadSceneAsync(LoadSceneInfo sceneInfo)
        {
            var operation = sceneInfo.UnloadSceneAsync();
            while (!operation.isDone)
                await Task.Yield();
        }

        public async Task LoadSceneAsync(LoadSceneInfo sceneInfo, bool setActive = false)
        {
            var operation = sceneInfo.LoadSceneAsync();
            while (!operation.isDone)
                await Task.Yield();

            if (setActive)
                SceneManager.SetActiveScene(sceneInfo.GetScene());
        }

        async Task TransitionToSceneFlowAsync(LoadSceneInfo loadSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfo(SceneManager.GetActiveScene().buildIndex);
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

        async Task SwitchToSceneFlowAsync(LoadSceneInfo loadSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfo(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(loadSceneInfo, true);
            _ = UnloadSceneAsync(currentSceneInfo);
        }

        async Task LoadSceneAsyncWithReport(LoadSceneInfo loadSceneInfo, IProgress<float> progress)
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