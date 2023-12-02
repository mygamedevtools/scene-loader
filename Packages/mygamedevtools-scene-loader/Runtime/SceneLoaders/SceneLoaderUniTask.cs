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
    public class SceneLoaderUniTask : ISceneLoaderUniTask
    {
        public ISceneManager Manager => _manager;

        readonly ISceneManager _manager;

        public SceneLoaderUniTask(ISceneManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException("Cannot create a scene loader with a null Scene Manager");
        }

        public void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default) => TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneInfo, externalOriginScene);

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene).Forget();

        public void UnloadScenes(ILoadSceneInfo[] sceneInfos) => UnloadScenesAsync(sceneInfos).Forget();

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo).Forget();

        public void LoadScenes(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1) => LoadScenesAsync(sceneInfos, setIndexActive).Forget();

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false) => LoadSceneAsync(sceneInfo, setActive).Forget();

        public UniTask<Scene[]> TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null, Scene externalOriginScene = default) => intermediateSceneReference == null ? TransitionDirectlyAsync(targetScenes, setIndexActive, externalOriginScene) : TransitionWithIntermediateAsync(targetScenes, setIndexActive, intermediateSceneReference, externalOriginScene);

        public async UniTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default)
        {
            var result = await TransitionToScenesAsync(new ILoadSceneInfo[] { targetSceneInfo }, 0, intermediateSceneInfo, externalOriginScene);
            return result == null || result.Length == 0 ? default : result[0];
        }

        public async UniTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null) => await _manager.LoadScenesAsync(sceneReferences, setIndexActive, progress);

        public async UniTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null) => await _manager.LoadSceneAsync(sceneInfo, setActive, progress);

        public async UniTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneReferences) => await _manager.UnloadScenesAsync(sceneReferences);

        public async UniTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo) => await _manager.UnloadSceneAsync(sceneInfo);

        async UniTask<Scene[]> TransitionDirectlyAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();
            await UnloadCurrentScene(currentScene, externalOrigin);
            return await LoadScenesAsync(targetScenes, setIndexActive);
        }

        async UniTask<Scene[]> TransitionWithIntermediateAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();

            var loadingScene = await _manager.LoadSceneAsync(intermediateSceneInfo);
            intermediateSceneInfo = new LoadSceneInfoScene(loadingScene);

            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();

#if UNITY_2023_2_OR_NEWER
            var loadingBehavior = Object.FindObjectsByType<LoadingBehavior>(UnityEngine.FindObjectsSortMode.None).FirstOrDefault(l => l.gameObject.scene == loadingScene);
#else
            var loadingBehavior = Object.FindObjectsOfType<LoadingBehavior>().FirstOrDefault(l => l.gameObject.scene == loadingScene);
#endif
            return loadingBehavior
                ? await TransitionWithIntermediateLoadingAsync(targetScenes, setIndexActive, intermediateSceneInfo, loadingBehavior, currentScene, externalOrigin)
                : await TransitionWithIntermediateNoLoadingAsync(targetScenes, setIndexActive, intermediateSceneInfo, currentScene, externalOrigin);
        }

        async UniTask<Scene[]> TransitionWithIntermediateLoadingAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, LoadingBehavior loadingBehavior, Scene currentScene, bool externalOrigin)
        {
            var progress = loadingBehavior.Progress;
            await UniTask.WaitUntil(() => progress.State == LoadingState.Loading);

            if (!externalOrigin)
                currentScene = _manager.GetActiveScene();

            await UnloadCurrentScene(currentScene, externalOrigin);

            var loadedScenes = await _manager.LoadScenesAsync(targetScenes, setIndexActive, progress);
            progress.SetState(LoadingState.TargetSceneLoaded);

            await UniTask.WaitUntil(() => progress.State == LoadingState.TransitionComplete);

            UnloadSceneAsync(intermediateSceneInfo).Forget();
            return loadedScenes;
        }

        async UniTask<Scene[]> TransitionWithIntermediateNoLoadingAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, Scene currentScene, bool externalOrigin)
        {
            await UnloadCurrentScene(currentScene, externalOrigin);
            var loadedScenes = await LoadScenesAsync(targetScenes, setIndexActive);
            _ = UnloadSceneAsync(intermediateSceneInfo);
            return loadedScenes;
        }

        async UniTask UnloadCurrentScene(Scene currentScene, bool externalOrigin)
        {
            if (!currentScene.IsValid())
                return;

            if (externalOrigin)
#if UNITY_2023_2_OR_NEWER
                await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene).ToUniTask();
#else
                await UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);
#endif
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