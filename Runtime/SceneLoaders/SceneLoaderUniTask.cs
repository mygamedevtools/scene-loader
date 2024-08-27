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

        public SceneLoaderUniTask(ISceneManager sceneManager)
        {
            if (sceneManager == null)
            {
                throw new ArgumentNullException($"Cannot create a {nameof(SceneLoaderUniTask)} with a null {nameof(ISceneManager)}.", nameof(sceneManager));
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

        public UniTask<Scene[]> TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null)
        {
            return _sceneLoaderAsync.TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneReference).AsTask().AsUniTask();
        }

        public UniTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = null)
        {
            return _sceneLoaderAsync.TransitionToSceneAsync(targetSceneReference, intermediateSceneReference).AsTask().AsUniTask();
        }

        public UniTask<Scene[]> TransitionToScenesFromScenesAsync(ILoadSceneInfo[] targetScenes, ILoadSceneInfo[] fromScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null)
        {
            return _sceneLoaderAsync.TransitionToScenesFromScenesAsync(targetScenes, fromScenes, setIndexActive, intermediateSceneReference).AsTask().AsUniTask();
        }

        public UniTask<Scene> TransitionToSceneFromScenesAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo[] fromScenes, ILoadSceneInfo intermediateSceneReference = null)
        {
            return _sceneLoaderAsync.TransitionToSceneFromScenesAsync(targetSceneReference, fromScenes, intermediateSceneReference).AsTask().AsUniTask();
        }

        public UniTask<Scene[]> TransitionToScenesFromAllAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null)
        {
            return _sceneLoaderAsync.TransitionToScenesFromAllAsync(targetScenes, setIndexActive, intermediateSceneReference).AsTask().AsUniTask();
        }

        public UniTask<Scene> TransitionToSceneFromAllAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = null)
        {
            return _sceneLoaderAsync.TransitionToSceneFromAllAsync(targetSceneReference, intermediateSceneReference).AsTask().AsUniTask();
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
            return $"Scene Loader [UniTask] with {_sceneLoaderAsync.Manager.GetType().Name}";
        }
    }
}
#endif