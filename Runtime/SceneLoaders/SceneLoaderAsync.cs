/**
 * SceneLoaderAsync.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/16/2022 (en-US)
 */

using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MyGameDevTools.SceneLoading
{
    public class SceneLoaderAsync : ISceneLoader<ValueTask, ValueTask<Scene>, Scene, ILoadSceneInfo>
    {
        public ISceneManager<Scene, ILoadSceneInfo> Manager => _manager;

        readonly ISceneManager<Scene, ILoadSceneInfo> _manager;

        public SceneLoaderAsync(ISceneManager<Scene, ILoadSceneInfo> manager)
        {
            _manager = manager ?? throw new ArgumentNullException("Cannot create a scene loader with a null Scene Manager");
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => _ = UnloadSceneAsync(sceneInfo);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => _ = LoadSceneAsync(sceneInfo, setActive);

        public ValueTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => intermediateSceneInfo == null ? TransitionDirectlyAsync(targetSceneInfo) : TransitionWithIntermediateAsync(targetSceneInfo, intermediateSceneInfo);

        public ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null) => _manager.LoadSceneAsync(sceneInfo, setActive, progress);

        public ValueTask UnloadSceneAsync(ILoadSceneInfo sceneInfo) => _manager.UnloadSceneAsync(sceneInfo);

        async ValueTask<Scene> TransitionWithIntermediateAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo)
        {
            var currentScene = _manager.GetActiveScene();
            await _manager.LoadSceneAsync(intermediateSceneInfo);

            var loadingBehavior = Object.FindObjectOfType<LoadingBase>();
            Scene loadedScene;
            if (loadingBehavior)
            {
                while (!loadingBehavior.Active)
                    await Task.Yield();

                if (currentScene.IsValid())
                    await UnloadSceneAsync(new LoadSceneInfoScene(currentScene));

                loadedScene = await _manager.LoadSceneAsync(targetSceneInfo, true, loadingBehavior);
                loadingBehavior.CompleteLoading();

                while (loadingBehavior.Active)
                    await Task.Yield();

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

        async ValueTask<Scene> TransitionDirectlyAsync(ILoadSceneInfo loadSceneInfo)
        {
            var currentScene = _manager.GetActiveScene();
            if (currentScene.IsValid())
                await UnloadSceneAsync(new LoadSceneInfoScene(currentScene));
            return await LoadSceneAsync(loadSceneInfo, true);
        }
    }
}