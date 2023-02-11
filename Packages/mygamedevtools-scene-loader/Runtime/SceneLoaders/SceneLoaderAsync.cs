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
    public class SceneLoaderAsync : ISceneLoaderAsync<ValueTask<Scene>>
    {
        public ISceneManager Manager => _manager;

        readonly ISceneManager _manager;

        public SceneLoaderAsync(ISceneManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException("Cannot create a scene loader with a null Scene Manager");
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => _ = UnloadSceneAsync(sceneInfo);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => _ = LoadSceneAsync(sceneInfo, setActive);

        public ValueTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene) => intermediateSceneInfo == null ? TransitionDirectlyAsync(targetSceneInfo, externalOriginScene) : TransitionWithIntermediateAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene);

        public ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null) => _manager.LoadSceneAsync(sceneInfo, setActive, progress);

        public ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo) => _manager.UnloadSceneAsync(sceneInfo);

        async ValueTask<Scene> TransitionDirectlyAsync(ILoadSceneInfo loadSceneInfo, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();
            await UnloadCurrentScene(currentScene, externalOrigin);
            return await LoadSceneAsync(loadSceneInfo, true);
        }

        async ValueTask<Scene> TransitionWithIntermediateAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();
            await _manager.LoadSceneAsync(intermediateSceneInfo);

            var loadingBehavior = Object.FindObjectOfType<LoadingBehavior>();
            return loadingBehavior
                ? await TransitionWithIntermediateLoadingAsync(targetSceneInfo, intermediateSceneInfo, loadingBehavior, currentScene, externalOrigin)
                : await TransitionWithIntermediateNoLoadingAsync(targetSceneInfo, intermediateSceneInfo, currentScene, externalOrigin);
        }

        async ValueTask<Scene> TransitionWithIntermediateLoadingAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, LoadingBehavior loadingBehavior, Scene currentScene, bool externalOrigin)
        {
            var progress = loadingBehavior.Progress;
            while (progress.State != LoadingState.Loading)
                await Task.Yield();

            await UnloadCurrentScene(currentScene, externalOrigin);

            var loadedScene = await _manager.LoadSceneAsync(targetSceneInfo, true, progress);
            progress.SetState(LoadingState.TargetSceneLoaded);

            while (progress.State != LoadingState.TransitionComplete)
                await Task.Yield();

            _ = UnloadSceneAsync(intermediateSceneInfo);
            return loadedScene;
        }

        async ValueTask<Scene> TransitionWithIntermediateNoLoadingAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, Scene currentScene, bool externalOrigin)
        {
            await UnloadCurrentScene(currentScene, externalOrigin);
            var loadedScene = await LoadSceneAsync(targetSceneInfo, true);
            _ = UnloadSceneAsync(intermediateSceneInfo);
            return loadedScene;
        }

        async ValueTask UnloadCurrentScene(Scene currentScene, bool externalOrigin)
        {
            if (!currentScene.IsValid())
                return;

            if (externalOrigin)
            {
                var operation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);
                while (!operation.isDone)
                    await Task.Yield();
            }
            else
                await _manager.UnloadSceneAsync(new LoadSceneInfoScene(currentScene));
        }
    }
}