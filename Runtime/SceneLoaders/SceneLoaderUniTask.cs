#if ENABLE_UNITASK
/**
 * SceneLoaderUniTask.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/1/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using System;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MyGameDevTools.SceneLoading.UniTaskSupport
{
    public class SceneLoaderUniTask : ISceneLoaderAsync<UniTask<Scene>>
    {
        public ISceneManager Manager => _manager;

        readonly ISceneManager _manager;

        public SceneLoaderUniTask(ISceneManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException("Cannot create a scene loader with a null Scene Manager");
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo).Forget();

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => LoadSceneAsync(sceneInfo, setActive).Forget();

        public UniTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => intermediateSceneInfo == null ? TransitionDirectlyAsync(targetSceneInfo) : TransitionWithIntermediateAsync(targetSceneInfo, intermediateSceneInfo);

        public async UniTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null) => await _manager.LoadSceneAsync(sceneInfo, setActive, progress);

        public async UniTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo) => await _manager.UnloadSceneAsync(sceneInfo);

        async UniTask<Scene> TransitionWithIntermediateAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo)
        {
            var currentScene = _manager.GetActiveScene();
            await _manager.LoadSceneAsync(intermediateSceneInfo);

            var loadingBehavior = Object.FindObjectOfType<LoadingBase>();
            Scene loadedScene;
            if (loadingBehavior)
            {
                while (!loadingBehavior.Active)
                    await UniTask.Yield();

                if (currentScene.IsValid())
                    await UnloadSceneAsync(new LoadSceneInfoScene(currentScene));

                loadedScene = await _manager.LoadSceneAsync(targetSceneInfo, true, loadingBehavior);
                loadingBehavior.CompleteLoading();

                while (loadingBehavior.Active)
                    await UniTask.Yield();

                _ = UnloadSceneAsync(intermediateSceneInfo);
            }
            else
            {
                if (currentScene.IsValid())
                    await UnloadSceneAsync(new LoadSceneInfoScene(currentScene));
                loadedScene = await LoadSceneAsync(targetSceneInfo, true);
                _ = UnloadSceneAsync(intermediateSceneInfo);
            }

            return loadedScene;
        }

        async UniTask<Scene> TransitionDirectlyAsync(ILoadSceneInfo loadSceneInfo)
        {
            var currentScene = _manager.GetActiveScene();
            if (currentScene.IsValid())
                await UnloadSceneAsync(new LoadSceneInfoScene(currentScene));
            return await LoadSceneAsync(loadSceneInfo, true);
        }
    }
}
#endif