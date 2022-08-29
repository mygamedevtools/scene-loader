#if ENABLE_UNITASK
/**
 * SceneLoaderUniTask.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/1/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading.UniTaskSupport
{
    public class SceneLoaderUniTask : ISceneLoaderUniTask
    {
        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo).Forget();

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => LoadSceneAsync(sceneInfo, setActive).Forget();

        public UniTask TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => intermediateSceneInfo == null ? TransitionDirectlyAsync(targetSceneInfo) : TransitionWithIntermediateAsync(targetSceneInfo, intermediateSceneInfo);

        public async UniTask UnloadSceneAsync(ILoadSceneInfo sceneInfo) => await sceneInfo.UnloadSceneAsync().ToUniTask();

        public async UniTask LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive)
        {
            await sceneInfo.LoadSceneAsync().ToUniTask();

            if (setActive)
                SceneManager.SetActiveScene(sceneInfo.GetScene());
        }

        async UniTask TransitionWithIntermediateAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfoIndex(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(intermediateSceneInfo, true);

            var loadingBehavior = Object.FindObjectOfType<LoadingBehavior>();
            if (loadingBehavior)
            {
                await UniTask.WaitWhile(() => !loadingBehavior.Active);

                await LoadSceneAsyncWithReport(targetSceneInfo, loadingBehavior);
                loadingBehavior.CompleteLoading();

                UnloadSceneAsync(currentSceneInfo).Forget();

                await UniTask.WaitWhile(() => loadingBehavior.Active);

                UnloadSceneAsync(intermediateSceneInfo).Forget();
            }
            else
            {
                await LoadSceneAsync(targetSceneInfo, true);
                UnloadSceneAsync(currentSceneInfo).Forget();
                UnloadSceneAsync(intermediateSceneInfo).Forget();
            }
        }

        async UniTask TransitionDirectlyAsync(ILoadSceneInfo loadSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfoIndex(SceneManager.GetActiveScene().buildIndex);
            await LoadSceneAsync(loadSceneInfo, true);
            _ = UnloadSceneAsync(currentSceneInfo);
        }

        async UniTask LoadSceneAsyncWithReport(ILoadSceneInfo loadSceneInfo, System.IProgress<float> progress)
        {            
            await loadSceneInfo.LoadSceneAsync().ToUniTask(progress);
            SceneManager.SetActiveScene(loadSceneInfo.GetScene());
        }
    }
}
#endif