/**
 * AsyncSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading
{
    public class AsyncSceneLoader : ISceneLoader
    {
        readonly LoadSceneInfo _loadingSceneInfo;

        public AsyncSceneLoader(int loadingSceneBuildIndex = -1)
        {
            _loadingSceneInfo = new LoadSceneInfo(loadingSceneBuildIndex);
        }

        public void TransitionToSceneByIndex(int index) => TransitionToSceneByIndexAsync(index);

        public void TransitionToSceneByName(string name) => TransitionToSceneByNameAsync(name);

        public void SwitchToSceneByIndex(int index) => SwitchToSceneByIndexAsync(index);

        public void SwitchToSceneByName(string name) => SwitchToSceneByNameAsync(name);

        public void UnloadSceneByIndex(int index) => UnloadSceneByIndexAsync(index);

        public void UnloadSceneByName(string name) => UnloadSceneByNameAsync(name);

        public void LoadSceneByIndex(int index, bool setActive) => LoadSceneByIndexAsync(index, setActive);

        public void LoadSceneByName(string name, bool setActive) => LoadSceneByNameAsync(name, setActive);

        public Task TransitionToSceneByIndexAsync(int index, CancellationToken token = default) => TransitionToSceneFlowAsync(new LoadSceneInfo(index), token);

        public Task TransitionToSceneByNameAsync(string name, CancellationToken token = default) => TransitionToSceneFlowAsync(new LoadSceneInfo(name), token);

        public Task SwitchToSceneByIndexAsync(int index, CancellationToken token = default) => SwitchToSceneFlowAsync(new LoadSceneInfo(index), token);

        public Task SwitchToSceneByNameAsync(string name, CancellationToken token = default) => SwitchToSceneFlowAsync(new LoadSceneInfo(name), token);

        public Task UnloadSceneByIndexAsync(int index, CancellationToken token = default) => UnloadSceneAsync(new LoadSceneInfo(index), token);

        public Task UnloadSceneByNameAsync(string name, CancellationToken token = default) => UnloadSceneAsync(new LoadSceneInfo(name), token);

        public Task LoadSceneByIndexAsync(int index, bool setActive, CancellationToken token = default) => LoadSceneAsync(new LoadSceneInfo(index), setActive, token);

        public Task LoadSceneByNameAsync(string name, bool setActive, CancellationToken token = default) => LoadSceneAsync(new LoadSceneInfo(name), setActive, token);

        async Task TransitionToSceneFlowAsync(LoadSceneInfo loadSceneInfo, CancellationToken token)
        {
            var currentSceneInfo = new LoadSceneInfo(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(_loadingSceneInfo, true, token);

            var loadingBehavior = Object.FindObjectOfType<LoadingBehavior>();
            while (!loadingBehavior.Active)
                await Task.Yield();

            await LoadSceneAsyncWithReport(loadSceneInfo, loadingBehavior.UpdateLoadingProgress, token);
            loadingBehavior.CompleteLoading();
            _ = UnloadSceneAsync(currentSceneInfo, token);

            while (loadingBehavior.Active)
                await Task.Yield();
            _ = UnloadSceneAsync(_loadingSceneInfo, token);
        }

        async Task SwitchToSceneFlowAsync(LoadSceneInfo loadSceneInfo, CancellationToken token)
        {
            var currentSceneInfo = new LoadSceneInfo(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(loadSceneInfo, true, token);
            _ = UnloadSceneAsync(currentSceneInfo, token);
        }

        async Task LoadSceneAsyncWithReport(LoadSceneInfo loadSceneInfo, SceneLoadProgressDelegate progressCallback, CancellationToken token)
        {
            var operation = loadSceneInfo.LoadSceneAsync();
            while (!operation.isDone)
            {
                progressCallback?.Invoke(operation.progress);
                await Task.Yield();
            }
            SceneManager.SetActiveScene(loadSceneInfo.GetScene());
        }

        async Task LoadSceneAsync(LoadSceneInfo loadSceneInfo, bool setActive, CancellationToken token)
        {
            var operation = loadSceneInfo.LoadSceneAsync();
            while (!operation.isDone)
                await Task.Yield();

            if (setActive)
                SceneManager.SetActiveScene(loadSceneInfo.GetScene());
        }

        async Task UnloadSceneAsync(LoadSceneInfo loadSceneInfo, CancellationToken token)
        {
            var operation = loadSceneInfo.UnloadSceneAsync();
            while (!operation.isDone)
                await Task.Yield();
        }
    }
}