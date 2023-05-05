/**
 * SceneLoaderCoroutine.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 9/4/2022 (en-US)
 */

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

        public void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null, Scene externalOriginScene = default) => TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneInfo, externalOriginScene);

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene);

        public void UnloadScenes(ILoadSceneInfo[] sceneInfos) => UnloadScenesAsync(sceneInfos);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo);

        public void LoadScenes(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1) => LoadScenesAsync(sceneInfos, setIndexActive);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false) => LoadSceneAsync(sceneInfo, setActive);

        public Coroutine TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null, Scene externalOriginScene = default)
        {
            throw new NotImplementedException();
        }

        public Coroutine TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default) => RoutineBehaviour.Instance.StartCoroutine(intermediateSceneInfo == null ? TransitionDirectlyRoutine(targetSceneInfo, externalOriginScene) : TransitionWithIntermediateRoutine(targetSceneInfo, intermediateSceneInfo, externalOriginScene));

        public Coroutine UnloadScenesAsync(ILoadSceneInfo[] sceneReferences) => RoutineBehaviour.Instance.StartCoroutine(UnloadScenesRoutine(sceneReferences));

        public Coroutine UnloadSceneAsync(ILoadSceneInfo sceneInfo) => RoutineBehaviour.Instance.StartCoroutine(UnloadSceneRoutine(sceneInfo));

        public Coroutine LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null) => RoutineBehaviour.Instance.StartCoroutine(LoadScenesRoutine(sceneReferences, setIndexActive, progress));

        public Coroutine LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null) => RoutineBehaviour.Instance.StartCoroutine(LoadSceneRoutine(sceneInfo, setActive, progress));

        IEnumerator LoadScenesRoutine(ILoadSceneInfo[] sceneReferences, int setIndexActive, IProgress<float> progress)
        {
            yield return new WaitTask(_manager.LoadScenesAsync(sceneReferences, setIndexActive, progress).AsTask());
        }

        IEnumerator LoadSceneRoutine(ILoadSceneInfo sceneInfo, bool setActive, IProgress<float> progress)
        {
            yield return new WaitTask(_manager.LoadSceneAsync(sceneInfo, setActive, progress).AsTask());
        }

        IEnumerator UnloadScenesRoutine(ILoadSceneInfo[] sceneReferences)
        {
            yield return new WaitTask(_manager.UnloadScenesAsync(sceneReferences).AsTask());
        }

        IEnumerator UnloadSceneRoutine(ILoadSceneInfo sceneInfo)
        {
            yield return new WaitTask(_manager.UnloadSceneAsync(sceneInfo).AsTask());
        }

        IEnumerator TransitionDirectlyRoutine(ILoadSceneInfo targetSceneInfo, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();
            yield return UnloadCurrentScene(currentScene, externalOrigin);
            yield return LoadSceneRoutine(targetSceneInfo, true, null);
        }

        IEnumerator TransitionWithIntermediateRoutine(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            
            var task = _manager.LoadSceneAsync(intermediateSceneInfo).AsTask();
            yield return new WaitTask(task);
            var loadingScene = task.Result;
            intermediateSceneInfo = new LoadSceneInfoScene(loadingScene);

            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();

            var loadingBehavior = Object.FindObjectsOfType<LoadingBehavior>().FirstOrDefault(l => l.gameObject.scene == loadingScene);
            yield return loadingBehavior
                ? TransitionWithIntermediateLoadingAsync(targetSceneInfo, intermediateSceneInfo, loadingBehavior, currentScene, externalOrigin)
                : TransitionWithIntermediateNoLoadingAsync(targetSceneInfo, intermediateSceneInfo, currentScene, externalOrigin);
        }

        IEnumerator TransitionWithIntermediateLoadingAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, LoadingBehavior loadingBehavior, Scene currentScene, bool externalOrigin)
        {
            var progress = loadingBehavior.Progress;
            yield return new WaitUntil(() => progress.State == LoadingState.Loading);

            if (!externalOrigin)
                currentScene = _manager.GetActiveScene();

            yield return UnloadCurrentScene(currentScene, externalOrigin);

            yield return new WaitTask(_manager.LoadSceneAsync(targetSceneInfo, true, progress).AsTask());
            progress.SetState(LoadingState.TargetSceneLoaded);

            yield return new WaitUntil(() => progress.State == LoadingState.TransitionComplete);

            UnloadSceneAsync(intermediateSceneInfo);
        }

        IEnumerator TransitionWithIntermediateNoLoadingAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, Scene currentScene, bool externalOrigin)
        {
            yield return UnloadCurrentScene(currentScene, externalOrigin);
            yield return LoadSceneRoutine(targetSceneInfo, true, null);
            UnloadSceneAsync(intermediateSceneInfo);
        }

        IEnumerator UnloadCurrentScene(Scene currentScene, bool externalOrigin)
        {
            if (!currentScene.IsValid())
                yield break;

            if (externalOrigin)
                yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);
            else
                yield return UnloadSceneRoutine(new LoadSceneInfoScene(currentScene));
        }

        public override string ToString()
        {
            return $"Scene Loader [Coroutine] with {_manager.GetType().Name}";
        }
    }
}