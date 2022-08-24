#if ENABLE_UNITASK
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
    public class UniTaskSceneLoader : IUniTaskSceneLoader
    {
        readonly ILoadSceneInfo _loadingSceneInfo;

        public UniTaskSceneLoader(int loadingSceneBuildIndex = -1)
        {
            _loadingSceneInfo = new LoadSceneInfoIndex(loadingSceneBuildIndex);
        }

        public void TransitionToScene(ILoadSceneInfo sceneInfo) => TransitionToSceneAsync(sceneInfo);

        public void SwitchToScene(ILoadSceneInfo sceneInfo) => SwitchToSceneAsync(sceneInfo);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo).Forget();

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => LoadSceneAsync(sceneInfo, setActive).Forget();

        public UniTask TransitionToSceneAsync(ILoadSceneInfo sceneInfo) => TransitionToSceneFlowAsync(sceneInfo);

        public UniTask SwitchToSceneAsync(ILoadSceneInfo sceneInfo) => SwitchToSceneFlowAsync(sceneInfo);

        public async UniTask UnloadSceneAsync(ILoadSceneInfo sceneInfo) => await sceneInfo.UnloadSceneAsync().ToUniTask();

        public async UniTask LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive)
        {
            await sceneInfo.LoadSceneAsync().ToUniTask();

            if (setActive)
                SceneManager.SetActiveScene(sceneInfo.GetScene());
        }

        async UniTask TransitionToSceneFlowAsync(ILoadSceneInfo loadSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfoIndex(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(_loadingSceneInfo, true);

            var loadingBehavior = UnityEngine.Object.FindObjectOfType<LoadingBehavior>();

            await UniTask.WaitWhile(() => !loadingBehavior.Active);

            await LoadSceneAsyncWithReport(loadSceneInfo, loadingBehavior);
            loadingBehavior.CompleteLoading();

            UnloadSceneAsync(currentSceneInfo).Forget();

            await UniTask.WaitWhile(() => loadingBehavior.Active);

            UnloadSceneAsync(_loadingSceneInfo).Forget();
        }

        async UniTask SwitchToSceneFlowAsync(ILoadSceneInfo loadSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfoIndex(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(loadSceneInfo, true);
            _ = UnloadSceneAsync(currentSceneInfo);
        }

        async UniTask LoadSceneAsyncWithReport(ILoadSceneInfo loadSceneInfo, IProgress<float> progress)
        {            
            await loadSceneInfo.LoadSceneAsync().ToUniTask(progress);
            SceneManager.SetActiveScene(loadSceneInfo.GetScene());
        }
    }
}
#endif