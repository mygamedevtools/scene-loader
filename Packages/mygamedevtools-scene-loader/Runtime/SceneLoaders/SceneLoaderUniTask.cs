#if ENABLE_UNITASK
using System;
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct SceneLoaderUniTask : ISceneLoaderUniTask
    {
        public ISceneManager Manager => _sceneLoaderAsync.Manager;

        readonly ISceneLoaderAsync _sceneLoaderAsync;

        public SceneLoaderUniTask(ISceneManager sceneManager) : this(new SceneLoaderAsync(sceneManager)) { }
        public SceneLoaderUniTask(ISceneLoaderAsync baseSceneLoader)
        {
            _sceneLoaderAsync = baseSceneLoader ?? throw new ArgumentNullException($"Cannot create a {nameof(SceneLoaderUniTask)} with a null {nameof(ISceneLoaderAsync)}.", nameof(baseSceneLoader));
        }

        public void Dispose()
        {
            _sceneLoaderAsync.Dispose();
        }

        public void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default)
        {
            TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneInfo, externalOriginScene);
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default)
        {
            TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene);
        }

        public void UnloadScenes(ILoadSceneInfo[] sceneInfos)
        {
            UnloadScenesAsync(sceneInfos);
        }

        public void UnloadScene(ILoadSceneInfo sceneInfo)
        {
            UnloadSceneAsync(sceneInfo);
        }

        public void LoadScenes(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1)
        {
            LoadScenesAsync(sceneInfos, setIndexActive);
        }

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false)
        {
            LoadSceneAsync(sceneInfo, setActive);
        }

        public UniTask<Scene[]> TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null, Scene externalOriginScene = default)
        {
            return _sceneLoaderAsync.TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneReference, externalOriginScene).AsTask().AsUniTask();
        }

        public UniTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = null, Scene externalOriginScene = default)
        {
            return _sceneLoaderAsync.TransitionToSceneAsync(targetSceneReference, intermediateSceneReference, externalOriginScene).AsTask().AsUniTask();
        }

        public UniTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneReferences)
        {
            return _sceneLoaderAsync.UnloadScenesAsync(sceneReferences).AsTask().AsUniTask();
        }

        public UniTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneReference)
        {
            return _sceneLoaderAsync.UnloadSceneAsync(sceneReference).AsTask().AsUniTask();
        }

        public UniTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null)
        {
            return _sceneLoaderAsync.LoadScenesAsync(sceneReferences, setIndexActive, progress).AsTask().AsUniTask();
        }

        public UniTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneReference, bool setActive = false, IProgress<float> progress = null)
        {
            return _sceneLoaderAsync.LoadSceneAsync(sceneReference, setActive, progress).AsTask().AsUniTask();
        }

        public override string ToString()
        {
            return $"Scene Loader [UniTask Alt] with {_sceneLoaderAsync.Manager.GetType().Name}";
        }
    }
}
#endif