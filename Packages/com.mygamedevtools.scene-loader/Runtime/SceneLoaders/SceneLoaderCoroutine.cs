using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct SceneLoaderCoroutine : ISceneLoaderCoroutine
    {
        public ISceneManager Manager => _sceneLoaderAsync.Manager;

        readonly ISceneLoaderAsync _sceneLoaderAsync;

        public SceneLoaderCoroutine(ISceneManager sceneManager)
        {
            if (sceneManager == null)
            {
                throw new ArgumentNullException($"Cannot create a {nameof(SceneLoaderCoroutine)} with a null {nameof(ISceneManager)}.", nameof(sceneManager));
            }
            _sceneLoaderAsync = new SceneLoaderAsync(sceneManager);
        }

        public void Dispose()
        {
            _sceneLoaderAsync.Dispose();
        }

        public void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneInfo);
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo);
        }

        public void TransitionToScenesFromScenes(ILoadSceneInfo[] targetScenes, ILoadSceneInfo[] fromScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToScenesFromScenesAsync(targetScenes, fromScenes, setIndexActive, intermediateSceneInfo);
        }

        public void TransitionToSceneFromScenes(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo[] fromScenes, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToSceneFromScenesAsync(targetSceneInfo, fromScenes, intermediateSceneInfo);
        }

        public void TransitionToScenesFromAll(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToScenesFromAllAsync(targetScenes, setIndexActive, intermediateSceneInfo);
        }

        public void TransitionToSceneFromAll(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToSceneFromAllAsync(targetSceneInfo, intermediateSceneInfo);
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

        public WaitTask<Scene[]> TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null)
        {
            return new WaitTask<Scene[]>(_sceneLoaderAsync.TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneReference).AsTask());
        }

        public WaitTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = null)
        {
            return new WaitTask<Scene>(_sceneLoaderAsync.TransitionToSceneAsync(targetSceneReference, intermediateSceneReference).AsTask());
        }

        public WaitTask<Scene[]> TransitionToScenesFromScenesAsync(ILoadSceneInfo[] targetScenes, ILoadSceneInfo[] fromScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null)
        {
            return new WaitTask<Scene[]>(_sceneLoaderAsync.TransitionToScenesFromScenesAsync(targetScenes, fromScenes, setIndexActive, intermediateSceneReference).AsTask());
        }

        public WaitTask<Scene> TransitionToSceneFromScenesAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo[] fromScenes, ILoadSceneInfo intermediateSceneReference = null)
        {
            return new WaitTask<Scene>(_sceneLoaderAsync.TransitionToSceneFromScenesAsync(targetSceneReference, fromScenes, intermediateSceneReference).AsTask());
        }

        public WaitTask<Scene[]> TransitionToScenesFromAllAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null)
        {
            return new WaitTask<Scene[]>(_sceneLoaderAsync.TransitionToScenesFromAllAsync(targetScenes, setIndexActive, intermediateSceneReference).AsTask());
        }

        public WaitTask<Scene> TransitionToSceneFromAllAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = null)
        {
            return new WaitTask<Scene>(_sceneLoaderAsync.TransitionToSceneFromAllAsync(targetSceneReference, intermediateSceneReference).AsTask());
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
            return $"Scene Loader [Coroutine] with {_sceneLoaderAsync.Manager.GetType().Name}";
        }
    }
}