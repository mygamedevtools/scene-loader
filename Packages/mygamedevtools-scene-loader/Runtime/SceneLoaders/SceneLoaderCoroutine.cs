using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MyGameDevTools.SceneLoading
{
    public class SceneLoaderCoroutine : ISceneLoaderCoroutine
    {
        public ISceneManager Manager => _manager;

        readonly ISceneManager _manager;

        public SceneLoaderCoroutine(ISceneManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException("Cannot create a scene loader with a null Scene Manager");
        }

        public void Dispose()
        {
            _manager.Dispose();
        }

        public void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default) => TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneInfo, externalOriginScene);

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene);

        public void UnloadScenes(ILoadSceneInfo[] sceneInfos) => UnloadScenesAsync(sceneInfos);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo);

        public void LoadScenes(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1) => LoadScenesAsync(sceneInfos, setIndexActive);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false) => LoadSceneAsync(sceneInfo, setActive);

        public Coroutine TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null, Scene externalOriginScene = default) => RoutineBehaviour.Instance.StartCoroutine(intermediateSceneReference == null ? TransitionDirectlyRoutine(targetScenes, setIndexActive, externalOriginScene) : TransitionWithIntermediateRoutine(targetScenes, setIndexActive, intermediateSceneReference, externalOriginScene));

        public Coroutine TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default) => TransitionToScenesAsync(new ILoadSceneInfo[] { targetSceneInfo }, 0, intermediateSceneInfo, externalOriginScene);

        public Coroutine UnloadScenesAsync(ILoadSceneInfo[] sceneReferences) => RoutineBehaviour.Instance.StartCoroutine(UnloadScenesRoutine(sceneReferences));

        public Coroutine UnloadSceneAsync(ILoadSceneInfo sceneInfo) => UnloadScenesAsync(new ILoadSceneInfo[] { sceneInfo });

        public Coroutine LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null) => RoutineBehaviour.Instance.StartCoroutine(LoadScenesRoutine(sceneReferences, setIndexActive, progress));

        public Coroutine LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null) => LoadScenesAsync(new ILoadSceneInfo[] { sceneInfo }, setActive ? 0 : -1, progress);

        WaitTask GetLoadScenesWaitTask(ILoadSceneInfo[] sceneReferences, int setIndexActive, IProgress<float> progress)
        {
            return new WaitTask(_manager.LoadScenesAsync(sceneReferences, setIndexActive, progress).AsTask());
        }

        WaitTask GetUnloadScenesWaitTask(ILoadSceneInfo[] sceneReferences)
        {
            return new WaitTask(_manager.UnloadScenesAsync(sceneReferences).AsTask());
        }

        IEnumerator LoadScenesRoutine(ILoadSceneInfo[] sceneReferences, int setIndexActive, IProgress<float> progress)
        {
            yield return GetLoadScenesWaitTask(sceneReferences, setIndexActive, progress);
        }

        IEnumerator UnloadScenesRoutine(ILoadSceneInfo[] sceneReferences)
        {
            yield return GetUnloadScenesWaitTask(sceneReferences);
        }

        IEnumerator TransitionDirectlyRoutine(ILoadSceneInfo[] targetScenes, int setIndexActive, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();
            
            var unloadWait = new WaitCurrentSceneUnload(this, currentScene, externalOrigin);
            yield return unloadWait;
            if (unloadWait.IsTaskCanceled)
                yield break;

            yield return LoadScenesRoutine(targetScenes, setIndexActive, null);
        }

        IEnumerator TransitionWithIntermediateRoutine(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            
            var task = _manager.LoadSceneAsync(intermediateSceneInfo).AsTask();
            yield return new WaitTask(task);
            if (task.IsCanceled)
                yield break;

            var loadingScene = task.Result;
            intermediateSceneInfo = new LoadSceneInfoScene(loadingScene);

            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();

#if UNITY_2023_2_OR_NEWER
            var loadingBehavior = Object.FindObjectsByType<LoadingBehavior>(FindObjectsSortMode.None).FirstOrDefault(l => l.gameObject.scene == loadingScene);
#else
            var loadingBehavior = Object.FindObjectsOfType<LoadingBehavior>().FirstOrDefault(l => l.gameObject.scene == loadingScene);
#endif
            yield return loadingBehavior
                ? TransitionWithIntermediateLoadingAsync(targetScenes, setIndexActive, intermediateSceneInfo, loadingBehavior, currentScene, externalOrigin)
                : TransitionWithIntermediateNoLoadingAsync(targetScenes, setIndexActive, intermediateSceneInfo, currentScene, externalOrigin);
        }

        IEnumerator TransitionWithIntermediateLoadingAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, LoadingBehavior loadingBehavior, Scene currentScene, bool externalOrigin)
        {
            var progress = loadingBehavior.Progress;
            yield return new WaitUntil(() => progress.State == LoadingState.Loading);

            if (!externalOrigin)
                currentScene = _manager.GetActiveScene();

            var unloadWait = new WaitCurrentSceneUnload(this, currentScene, externalOrigin);
            yield return unloadWait;
            if (unloadWait.IsTaskCanceled)
                yield break;

            var loadWaitTask = GetLoadScenesWaitTask(targetScenes, setIndexActive, null);
            yield return loadWaitTask;
            if (loadWaitTask.IsTaskCanceled)
                yield break;

            progress.SetState(LoadingState.TargetSceneLoaded);

            yield return new WaitUntil(() => progress.State == LoadingState.TransitionComplete);

            UnloadSceneAsync(intermediateSceneInfo);
        }

        IEnumerator TransitionWithIntermediateNoLoadingAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, Scene currentScene, bool externalOrigin)
        {
            var unloadWait = new WaitCurrentSceneUnload(this, currentScene, externalOrigin);
            yield return unloadWait;
            if (unloadWait.IsTaskCanceled)
                yield break;

            var loadWaitTask = GetLoadScenesWaitTask(targetScenes, setIndexActive, null);
            yield return loadWaitTask;
            if (loadWaitTask.IsTaskCanceled)
                yield break;
            UnloadSceneAsync(intermediateSceneInfo);
        }

        public override string ToString()
        {
            return $"Scene Loader [Coroutine] with {_manager.GetType().Name}";
        }

        readonly struct WaitCurrentSceneUnload : IEnumerator
        {
            public object Current => null;
            public bool IsTaskCanceled => !_externalOrigin && _unloadWaitTask.IsTaskCanceled;

            readonly AsyncOperation _unloadOperation;
            readonly WaitTask _unloadWaitTask;
            readonly bool _externalOrigin;
            readonly bool _validScene;

            public WaitCurrentSceneUnload(SceneLoaderCoroutine sceneLoader, Scene currentScene, bool externalOrigin)
            {
                _validScene = currentScene.IsValid();
                _externalOrigin = externalOrigin;
                if (!_validScene)
                {
                    _unloadOperation = default;
                    _unloadWaitTask = default;
                }
                else
                {
                    if (externalOrigin)
                    {
                        _unloadOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);
                        _unloadWaitTask = default;
                    }
                    else
                    {
                        _unloadWaitTask = sceneLoader.GetUnloadScenesWaitTask(new ILoadSceneInfo[] { new LoadSceneInfoScene(currentScene) });
                        _unloadOperation = default;
                    }
                }
            }

            public bool MoveNext()
            {
                if (!_validScene)
                    return true;

                if (_externalOrigin)
                {
                    return _unloadOperation != null && !_unloadOperation.isDone;
                }
                else
                {
                    return _unloadWaitTask.MoveNext();
                }
            }

            public void Reset() {}
        }
    }
}