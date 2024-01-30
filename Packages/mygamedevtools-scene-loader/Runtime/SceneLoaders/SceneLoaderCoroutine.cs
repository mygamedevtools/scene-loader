using System;
using System.Collections;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public class SceneLoaderCoroutine : ISceneLoaderCoroutine
    {
        public ISceneManager Manager => _manager;

        readonly ISceneManager _manager;
        readonly CancellationTokenSource _lifetimeTokenSource;

        public SceneLoaderCoroutine(ISceneManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException("Cannot create a scene loader with a null Scene Manager");
            _lifetimeTokenSource = new CancellationTokenSource();
        }

        public void Dispose()
        {
            _lifetimeTokenSource.Cancel();
            _lifetimeTokenSource.Dispose();
            _manager.Dispose();
        }

        public void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default)
        {
            TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneInfo, externalOriginScene);
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default)
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

        public Coroutine TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null, Scene externalOriginScene = default, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return RoutineBehaviour.Instance.StartCoroutineWithDisposableToken(intermediateSceneReference == null
                ? TransitionDirectlyRoutine(targetScenes, setIndexActive, externalOriginScene, linkedSource.Token)
                : TransitionWithIntermediateRoutine(targetScenes, setIndexActive, intermediateSceneReference, externalOriginScene, linkedSource.Token), linkedSource);
        }

        public Coroutine TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default, CancellationToken token = default)
        {
            return TransitionToScenesAsync(new ILoadSceneInfo[] { targetSceneInfo }, 0, intermediateSceneInfo, externalOriginScene, token);
        }

        public Coroutine UnloadScenesAsync(ILoadSceneInfo[] sceneReferences, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return RoutineBehaviour.Instance.StartCoroutineWithDisposableToken(GetUnloadScenesWaitTask(sceneReferences, linkedSource.Token), linkedSource);
        }

        public Coroutine UnloadSceneAsync(ILoadSceneInfo sceneInfo, CancellationToken token = default)
        {
            return UnloadScenesAsync(new ILoadSceneInfo[] { sceneInfo }, token);
        }

        public Coroutine LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return RoutineBehaviour.Instance.StartCoroutineWithDisposableToken(GetLoadScenesWaitTask(sceneReferences, setIndexActive, progress, linkedSource.Token), linkedSource);
        }

        public Coroutine LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null, CancellationToken token = default)
        {
            return LoadScenesAsync(new ILoadSceneInfo[] { sceneInfo }, setActive ? 0 : -1, progress, token);
        }

        WaitTask GetLoadScenesWaitTask(ILoadSceneInfo[] sceneReferences, int setIndexActive, IProgress<float> progress, CancellationToken token)
        {
            return new WaitTask(_manager.LoadScenesAsync(sceneReferences, setIndexActive, progress, token).AsTask(), false);
        }

        WaitTask GetUnloadScenesWaitTask(ILoadSceneInfo[] sceneReferences, CancellationToken token)
        {
            return new WaitTask(_manager.UnloadScenesAsync(sceneReferences, token).AsTask(), false);
        }

        IEnumerator TransitionDirectlyRoutine(ILoadSceneInfo[] targetScenes, int setIndexActive, Scene externalOriginScene, CancellationToken token)
        {
            var externalOrigin = externalOriginScene.IsValid();
            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();
            
            var unloadWait = new WaitCurrentSceneUnload(this, currentScene, externalOrigin, token);
            yield return unloadWait;
            if (!externalOrigin && IsWaitTaskFaultedOrCanceled(unloadWait.UnloadWaitTask))
                yield break;

            if (token.IsCancellationRequested)
                yield break;
            yield return GetLoadScenesWaitTask(targetScenes, setIndexActive, null, token);
        }

        IEnumerator TransitionWithIntermediateRoutine(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene, CancellationToken token)
        {
            var externalOrigin = externalOriginScene.IsValid();
            
            var task = _manager.LoadSceneAsync(intermediateSceneInfo, token: token).AsTask();
            var waitTask = new WaitTask(task, false);
            yield return waitTask;
            if (IsWaitTaskFaultedOrCanceled(waitTask))
                yield break;

            var loadingScene = task.Result;
            intermediateSceneInfo = new LoadSceneInfoScene(loadingScene);

            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();

#if UNITY_2023_2_OR_NEWER
            var loadingBehavior = UnityEngine.Object.FindObjectsByType<LoadingBehavior>(FindObjectsSortMode.None).FirstOrDefault(l => l.gameObject.scene == loadingScene);
#else
            var loadingBehavior = UnityEngine.Object.FindObjectsOfType<LoadingBehavior>().FirstOrDefault(l => l.gameObject.scene == loadingScene);
#endif
            yield return loadingBehavior
                ? TransitionWithIntermediateLoadingAsync(targetScenes, setIndexActive, intermediateSceneInfo, loadingBehavior, currentScene, externalOrigin, token)
                : TransitionWithIntermediateNoLoadingAsync(targetScenes, setIndexActive, intermediateSceneInfo, currentScene, externalOrigin, token);
        }

        IEnumerator TransitionWithIntermediateLoadingAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, LoadingBehavior loadingBehavior, Scene currentScene, bool externalOrigin, CancellationToken token)
        {
            var progress = loadingBehavior.Progress;
            yield return new WaitUntil(() => progress.State == LoadingState.Loading);

            if (!externalOrigin)
                currentScene = _manager.GetActiveScene();

            var unloadWait = new WaitCurrentSceneUnload(this, currentScene, externalOrigin, token);
            yield return unloadWait;
            if (!externalOrigin && IsWaitTaskFaultedOrCanceled(unloadWait.UnloadWaitTask))
                yield break;

            if (token.IsCancellationRequested)
                yield break;

            WaitTask loadScenesWaitTask = GetLoadScenesWaitTask(targetScenes, setIndexActive, null, token);
            yield return loadScenesWaitTask;
            if (IsWaitTaskFaultedOrCanceled(loadScenesWaitTask))
                yield break;

            progress.SetState(LoadingState.TargetSceneLoaded);

            yield return new WaitUntil(() => progress.State == LoadingState.TransitionComplete);

            UnloadSceneAsync(intermediateSceneInfo);
        }

        IEnumerator TransitionWithIntermediateNoLoadingAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, Scene currentScene, bool externalOrigin, CancellationToken token)
        {
            var unloadWait = new WaitCurrentSceneUnload(this, currentScene, externalOrigin, token);
            yield return unloadWait;
            if (!externalOrigin && IsWaitTaskFaultedOrCanceled(unloadWait.UnloadWaitTask))
                yield break;

            if (token.IsCancellationRequested)
                yield break;

            WaitTask loadScenesWaitTask = GetLoadScenesWaitTask(targetScenes, setIndexActive, null, token);
            yield return loadScenesWaitTask;
            if (IsWaitTaskFaultedOrCanceled(loadScenesWaitTask))
                yield break;

            UnloadSceneAsync(intermediateSceneInfo);
        }

        bool IsWaitTaskFaultedOrCanceled(WaitTask waitTask)
        {
            if (waitTask.Task.IsFaulted)
            {
                Debug.LogException(waitTask.Task.Exception);
                return true;
            }
            else if (waitTask.Task.IsCanceled)
                return true;

            return false;
        }

        public override string ToString()
        {
            return $"Scene Loader [Coroutine] with {_manager.GetType().Name}";
        }

        readonly struct WaitCurrentSceneUnload : IEnumerator
        {
            public object Current => null;

            public readonly WaitTask UnloadWaitTask;

            readonly AsyncOperation _unloadOperation;
            readonly bool _externalOrigin;
            readonly bool _validScene;

            public WaitCurrentSceneUnload(SceneLoaderCoroutine sceneLoader, Scene currentScene, bool externalOrigin, CancellationToken token)
            {
                _validScene = currentScene.IsValid();
                _externalOrigin = externalOrigin;
                if (!_validScene)
                {
                    _unloadOperation = default;
                    UnloadWaitTask = default;
                }
                else
                {
                    if (externalOrigin)
                    {
                        _unloadOperation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);
                        UnloadWaitTask = default;
                    }
                    else
                    {
                        UnloadWaitTask = sceneLoader.GetUnloadScenesWaitTask(new ILoadSceneInfo[] { new LoadSceneInfoScene(currentScene) }, token);
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
                    return UnloadWaitTask.MoveNext();
                }
            }

            public void Reset() {}
        }
    }
}