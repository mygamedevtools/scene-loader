using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct SceneLoaderCoroutine : ISceneLoaderCoroutine
    {
        public ISceneManager Manager => _sceneLoaderAsync.Manager;

        readonly ISceneLoaderAsync _sceneLoaderAsync;

        public SceneLoaderCoroutine(ISceneManager sceneManager) : this(new SceneLoaderAsync(sceneManager)) { }
        public SceneLoaderCoroutine(ISceneLoaderAsync baseSceneLoader)
        {
            _sceneLoaderAsync = baseSceneLoader ?? throw new ArgumentNullException($"Cannot create a {nameof(SceneLoaderCoroutine)} with a null {nameof(ISceneLoaderAsync)}.", nameof(baseSceneLoader));
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

        public WaitTask<Scene[]> TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null, Scene externalOriginScene = default)
        {
            return new WaitTask<Scene[]>(_sceneLoaderAsync.TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneReference, externalOriginScene).AsTask());
        }

        public WaitTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = null, Scene externalOriginScene = default)
        {
            return new WaitTask<Scene>(_sceneLoaderAsync.TransitionToSceneAsync(targetSceneReference, intermediateSceneReference, externalOriginScene).AsTask());
        }

        public WaitTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneReferences)
        {
            return new WaitTask<Scene[]>(_sceneLoaderAsync.UnloadScenesAsync(sceneReferences).AsTask());
        }

        public WaitTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneReference)
        {
            return new WaitTask<Scene>(_sceneLoaderAsync.UnloadSceneAsync(sceneReference).AsTask());
        }

        public WaitTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null)
        {
            return new WaitTask<Scene[]>(_sceneLoaderAsync.LoadScenesAsync(sceneReferences, setIndexActive, progress).AsTask());
        }

        public WaitTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneReference, bool setActive = false, IProgress<float> progress = null)
        {
            return new WaitTask<Scene>(_sceneLoaderAsync.LoadSceneAsync(sceneReference, setActive, progress).AsTask());
        }

        public override string ToString()
        {
            return $"Scene Loader [Coroutine Alt] with {_sceneLoaderAsync.Manager.GetType().Name}";
        }
    }
}