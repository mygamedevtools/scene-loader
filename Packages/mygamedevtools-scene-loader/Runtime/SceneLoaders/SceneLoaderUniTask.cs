#if ENABLE_UNITASK
/**
 * SceneLoaderUniTask.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/1/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using System;
using System.Linq;
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

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo).Forget();

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false) => LoadSceneAsync(sceneInfo, setActive).Forget();

        public UniTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default) => intermediateSceneInfo == null ? TransitionDirectlyAsync(targetSceneInfo, externalOriginScene) : TransitionWithIntermediateAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene);

        public async UniTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null) => await _manager.LoadSceneAsync(sceneInfo, setActive, progress);

        public async UniTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo) => await _manager.UnloadSceneAsync(sceneInfo);

        async UniTask<Scene> TransitionDirectlyAsync(ILoadSceneInfo loadSceneInfo, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();
            await UnloadCurrentScene(currentScene, externalOrigin);
            return await LoadSceneAsync(loadSceneInfo, true);
        }

        async UniTask<Scene> TransitionWithIntermediateAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();

            var loadingScene = await _manager.LoadSceneAsync(intermediateSceneInfo);
            intermediateSceneInfo = new LoadSceneInfoScene(loadingScene);

            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();

            var loadingBehavior = Object.FindObjectsOfType<LoadingBehavior>().FirstOrDefault(l => l.gameObject.scene == loadingScene);
            return loadingBehavior
                ? await TransitionWithIntermediateLoadingAsync(targetSceneInfo, intermediateSceneInfo, loadingBehavior, currentScene, externalOrigin)
                : await TransitionWithIntermediateNoLoadingAsync(targetSceneInfo, intermediateSceneInfo, currentScene, externalOrigin);
        }

        async UniTask<Scene> TransitionWithIntermediateLoadingAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, LoadingBehavior loadingBehavior, Scene currentScene, bool externalOrigin)
        {
            var progress = loadingBehavior.Progress;
            await UniTask.WaitUntil(() => progress.State == LoadingState.Loading);

            if (!externalOrigin)
                currentScene = _manager.GetActiveScene();

            await UnloadCurrentScene(currentScene, externalOrigin);

            var loadedScene = await _manager.LoadSceneAsync(targetSceneInfo, true, progress);
            progress.SetState(LoadingState.TargetSceneLoaded);

            await UniTask.WaitUntil(() => progress.State == LoadingState.TransitionComplete);

            UnloadSceneAsync(intermediateSceneInfo).Forget();
            return loadedScene;
        }

        async UniTask<Scene> TransitionWithIntermediateNoLoadingAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, Scene currentScene, bool externalOrigin)
        {
            await UnloadCurrentScene(currentScene, externalOrigin);
            var loadedScene = await LoadSceneAsync(targetSceneInfo, true);
            _ = UnloadSceneAsync(intermediateSceneInfo);
            return loadedScene;
        }

        async UniTask UnloadCurrentScene(Scene currentScene, bool externalOrigin)
        {
            if (!currentScene.IsValid())
                return;

            if (externalOrigin)
                await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);
            else
                await _manager.UnloadSceneAsync(new LoadSceneInfoScene(currentScene));
        }

        public override string ToString()
        {
            return $"Scene Loader [UniTask] with {_manager.GetType().Name}";
        }
    }
}
#endif