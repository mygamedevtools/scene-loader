/**
 * UniTaskSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/1/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading.UniTaskSupport
{
    public class UniTaskSceneLoader : ISceneLoader
    {
        readonly LoadSceneInfo _loadingSceneInfo;

        public UniTaskSceneLoader(int loadingSceneBuildIndex = -1)
        {
            _loadingSceneInfo = new LoadSceneInfo(loadingSceneBuildIndex);
        }

        public void TransitionToScene(string name) => TransitionToSceneAsync(new LoadSceneInfo(name));
        public void TransitionToScene(int index) => TransitionToSceneAsync(new LoadSceneInfo(index));

        public void SwitchToScene(string name) => SwitchToSceneAsync(new LoadSceneInfo(name));
        public void SwitchToScene(int index) => SwitchToSceneAsync(new LoadSceneInfo(index));

        public void UnloadScene(string name) => UnloadSceneAsync(new LoadSceneInfo(name)).Forget();
        public void UnloadScene(int index) => UnloadSceneAsync(new LoadSceneInfo(index)).Forget();

        public void LoadScene(string name, bool setActive) => _ = LoadSceneAsync(new LoadSceneInfo(name), setActive);
        public void LoadScene(int index, bool setActive) => _ = LoadSceneAsync(new LoadSceneInfo(index), setActive);

        public UniTask TransitionToSceneAsync(LoadSceneInfo sceneInfo) => TransitionToSceneFlowAsync(sceneInfo);

        public UniTask SwitchToSceneAsync(LoadSceneInfo sceneInfo) => SwitchToSceneFlowAsync(sceneInfo);

        public async UniTask UnloadSceneAsync(LoadSceneInfo sceneInfo) => await sceneInfo.UnloadSceneAsync().ToUniTask();

        public async UniTask LoadSceneAsync(LoadSceneInfo sceneInfo, bool setActive = false)
        {
            await sceneInfo.LoadSceneAsync().ToUniTask();

            if (setActive)
                SceneManager.SetActiveScene(sceneInfo.GetScene());
        }

        async UniTask TransitionToSceneFlowAsync(LoadSceneInfo loadSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfo(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(_loadingSceneInfo, true);

            var loadingBehavior = UnityEngine.Object.FindObjectOfType<LoadingBehavior>();
            while (!loadingBehavior.Active)
                await UniTask.Yield();

            await LoadSceneAsyncWithReport(loadSceneInfo, loadingBehavior);
            loadingBehavior.CompleteLoading();
            _ = UnloadSceneAsync(currentSceneInfo);

            while (loadingBehavior.Active)
                await UniTask.Yield();
            _ = UnloadSceneAsync(_loadingSceneInfo);
        }

        async UniTask SwitchToSceneFlowAsync(LoadSceneInfo loadSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfo(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(loadSceneInfo, true);
            _ = UnloadSceneAsync(currentSceneInfo);
        }

        async UniTask LoadSceneAsyncWithReport(LoadSceneInfo loadSceneInfo, IProgress<float> progress)
        {            
            await loadSceneInfo.LoadSceneAsync().ToUniTask(progress);
            SceneManager.SetActiveScene(loadSceneInfo.GetScene());
        }
    }
}