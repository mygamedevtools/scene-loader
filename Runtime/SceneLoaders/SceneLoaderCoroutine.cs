/**
 * SceneLoaderCoroutine.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 9/4/2022 (en-US)
 */

using System;
using System.Collections;
using UnityEngine;
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

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => LoadSceneAsync(sceneInfo, setActive);

        public Coroutine TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => RoutineBehaviour.Instance.StartCoroutine(intermediateSceneInfo == null ? TransitionDirectlyRoutine(targetSceneInfo) : TransitionWithIntermediateRoutine(targetSceneInfo, intermediateSceneInfo));

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

        IEnumerator TransitionWithIntermediateRoutine(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo)
        {
            var currentScene = _manager.GetActiveScene();
            yield return new WaitTask(_manager.LoadSceneAsync(intermediateSceneInfo).AsTask());

            var loadingBehavior = Object.FindObjectOfType<LoadingBehavior>();
            if (loadingBehavior)
            {
                var progress = loadingBehavior.Progress;
                yield return new WaitUntil(() => progress.State == LoadingState.Loading);

                if (currentScene.IsValid())
                    yield return UnloadRoutine(new LoadSceneInfoScene(currentScene));

                yield return new WaitTask(_manager.LoadSceneAsync(targetSceneInfo, true, progress).AsTask());
                progress.SetState(LoadingState.TargetSceneLoaded);

                yield return new WaitUntil(() => progress.State == LoadingState.TransitionComplete);

                UnloadSceneAsync(intermediateSceneInfo);
            }
            else
            {
                if (currentScene.IsValid())
                    yield return UnloadRoutine(new LoadSceneInfoScene(currentScene));
                yield return LoadRoutine(targetSceneInfo, true, null);
                UnloadSceneAsync(intermediateSceneInfo);
            }
        }

        IEnumerator TransitionDirectlyRoutine(ILoadSceneInfo targetSceneInfo)
        {
            var currentScene = _manager.GetActiveScene();
            if (currentScene.IsValid())
                yield return UnloadRoutine(new LoadSceneInfoScene(currentScene));
            yield return LoadRoutine(targetSceneInfo, true, null);
        }
    }
}