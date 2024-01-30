using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MyGameDevTools.SceneLoading
{
    public class SceneLoaderAsync : ISceneLoaderAsync
    {
        public ISceneManager Manager => _manager;

        readonly ISceneManager _manager;

        public SceneLoaderAsync(ISceneManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException("Cannot create a scene loader with a null Scene Manager");
        }

        public void Dispose()
        {
            _manager.Dispose();
        }

        public void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default) => TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneInfo, externalOriginScene);

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default) => _ = TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene);

        public void UnloadScenes(ILoadSceneInfo[] sceneInfos) => _ = UnloadScenesAsync(sceneInfos);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => _ = UnloadSceneAsync(sceneInfo);

        public void LoadScenes(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1) => _ = LoadScenesAsync(sceneInfos, setIndexActive);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false) => _ = LoadSceneAsync(sceneInfo, setActive);

        public ValueTask<Scene[]> TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null, Scene externalOriginScene = default) => intermediateSceneReference == null ? TransitionDirectlyAsync(targetScenes, setIndexActive, externalOriginScene) : TransitionWithIntermediateAsync(targetScenes, setIndexActive, intermediateSceneReference, externalOriginScene);

        public async ValueTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default)
        {
            var result = await TransitionToScenesAsync(new ILoadSceneInfo[] { targetSceneInfo }, 0, intermediateSceneInfo, externalOriginScene);
            return result == null || result.Length == 0 ? default : result[0];
        }

        public ValueTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null) => _manager.LoadScenesAsync(sceneReferences, setIndexActive, progress);

        public ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null) => _manager.LoadSceneAsync(sceneInfo, setActive, progress);

        public ValueTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneReferences) => _manager.UnloadScenesAsync(sceneReferences);

        public ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo) => _manager.UnloadSceneAsync(sceneInfo);

        async ValueTask<Scene[]> TransitionDirectlyAsync(ILoadSceneInfo[] targetScenes, int setActiveIndex, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();
            await UnloadCurrentScene(currentScene, externalOrigin);
            return await LoadScenesAsync(targetScenes, setActiveIndex);
        }

        async ValueTask<Scene[]> TransitionWithIntermediateAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            
            var loadingScene = await _manager.LoadSceneAsync(intermediateSceneInfo);
            if (!loadingScene.IsValid())
                return Array.Empty<Scene>();

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

        async ValueTask<Scene[]> TransitionWithIntermediateLoadingAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, LoadingBehavior loadingBehavior, Scene currentScene, bool externalOrigin)
        {
            var progress = loadingBehavior.Progress;
            while (progress.State != LoadingState.Loading)
                await Task.Yield();

            if (!externalOrigin)
                currentScene = _manager.GetActiveScene();

            await UnloadCurrentScene(currentScene, externalOrigin);

            var loadedScenes = await _manager.LoadScenesAsync(targetScenes, setIndexActive, progress);
            progress.SetState(LoadingState.TargetSceneLoaded);

            while (progress.State != LoadingState.TransitionComplete)
                await Task.Yield();

            _ = UnloadSceneAsync(intermediateSceneInfo);
            return loadedScenes;
        }

        async ValueTask<Scene[]> TransitionWithIntermediateNoLoadingAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, Scene currentScene, bool externalOrigin)
        {
            await UnloadCurrentScene(currentScene, externalOrigin);
            var loadedScenes = await LoadScenesAsync(targetScenes, setIndexActive);
            _ = UnloadSceneAsync(intermediateSceneInfo);
            return loadedScenes;
        }

        async ValueTask UnloadCurrentScene(Scene currentScene, bool externalOrigin)
        {
            if (!currentScene.IsValid())
                return;

            if (externalOrigin)
            {
                var operation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);
                while (operation != null && !operation.isDone)
                    await Task.Yield();
            }
            else
                await _manager.UnloadSceneAsync(new LoadSceneInfoScene(currentScene));
        }

        public override string ToString()
        {
            return $"Scene Loader [Async] with {_manager.GetType().Name}";
        }
    }
}