/**
 * SceneLoaderCoroutine.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 9/4/2022 (en-US)
 */

using Cysharp.Threading.Tasks;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MyGameDevTools.SceneLoading
{
    public class SceneLoaderCoroutine : ISceneLoaderAsync<Coroutine>
    {
        public ISceneManager Manager => _manager;

        readonly ISceneManager _manager;

        public SceneLoaderCoroutine(ISceneManager manager)
        {
            _manager = manager ?? throw new ArgumentNullException("Cannot create a scene loader with a null Scene Manager");
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => LoadSceneAsync(sceneInfo, setActive);

        public Coroutine TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene) => RoutineBehaviour.Instance.StartCoroutine(intermediateSceneInfo == null ? TransitionDirectlyRoutine(targetSceneInfo, externalOriginScene) : TransitionWithIntermediateRoutine(targetSceneInfo, intermediateSceneInfo, externalOriginScene));

        public Coroutine UnloadSceneAsync(ILoadSceneInfo sceneInfo) => RoutineBehaviour.Instance.StartCoroutine(UnloadRoutine(sceneInfo));

        public Coroutine LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null) => RoutineBehaviour.Instance.StartCoroutine(LoadRoutine(sceneInfo, setActive, progress));

        IEnumerator LoadRoutine(ILoadSceneInfo sceneInfo, bool setActive, IProgress<float> progress)
        {
            yield return new WaitTask(_manager.LoadSceneAsync(sceneInfo, setActive, progress).AsTask());
        }

        IEnumerator UnloadRoutine(ILoadSceneInfo sceneInfo)
        {
            yield return new WaitTask(_manager.UnloadSceneAsync(sceneInfo).AsTask());
        }

        IEnumerator TransitionDirectlyRoutine(ILoadSceneInfo targetSceneInfo, Scene externalOriginScene)
        {
            var externalOrigin = externalOriginScene.IsValid();
            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();
            yield return UnloadCurrentScene(currentScene, externalOrigin);
            yield return LoadRoutine(targetSceneInfo, true, null);
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
            yield return LoadRoutine(targetSceneInfo, true, null);
            UnloadSceneAsync(intermediateSceneInfo);
        }

        IEnumerator UnloadCurrentScene(Scene currentScene, bool externalOrigin)
        {
            if (!currentScene.IsValid())
                yield break;

            if (externalOrigin)
                yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);
            else
                yield return UnloadRoutine(new LoadSceneInfoScene(currentScene));
        }

        public override string ToString()
        {
            return $"Scene Loader [Coroutine] with {_manager.GetType().Name}";
        }
    }
}