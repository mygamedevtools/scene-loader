/**
 * SceneLoaderAsync.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading
{
    public class SceneLoaderAsync : ISceneLoaderAsync
    {
        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => _ = UnloadSceneAsync(sceneInfo);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => _ = LoadSceneAsync(sceneInfo, setActive);

        public Task TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => intermediateSceneInfo == null ? TransitionDirectlyAsync(targetSceneInfo) : TransitionWithIntermediateAsync(targetSceneInfo, intermediateSceneInfo);

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

        async Task TransitionWithIntermediateAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfoIndex(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(intermediateSceneInfo, true);

            var loadingBehavior = Object.FindObjectOfType<LoadingBase>();
            if (loadingBehavior)
            {
                while (!loadingBehavior.Active)
                    await Task.Yield();

                await LoadSceneAsyncWithReport(targetSceneInfo, loadingBehavior);
                loadingBehavior.CompleteLoading();
                _ = UnloadSceneAsync(currentSceneInfo);

                while (loadingBehavior.Active)
                    await Task.Yield();
                _ = UnloadSceneAsync(intermediateSceneInfo);
            }
            else
            {
                await LoadSceneAsync(targetSceneInfo, true);
                _ = UnloadSceneAsync(currentSceneInfo);
                _ = UnloadSceneAsync(intermediateSceneInfo);
            }
        }

        async Task TransitionDirectlyAsync(ILoadSceneInfo loadSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfoIndex(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(loadSceneInfo, true);
            _ = UnloadSceneAsync(currentSceneInfo);
        }

        async Task LoadSceneAsyncWithReport(ILoadSceneInfo loadSceneInfo, System.IProgress<float> progress)
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