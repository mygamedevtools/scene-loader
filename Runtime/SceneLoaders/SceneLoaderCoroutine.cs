/**
 * SceneLoaderCoroutine.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 9/4/2022 (en-US)
 */

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MyGameDevTools.SceneLoading
{
    public class SceneLoaderCoroutine : ISceneLoader<Coroutine, Coroutine, Scene, ILoadSceneInfo>
    {
        public ISceneManager<Scene, ILoadSceneInfo> Manager => _manager;

        readonly ISceneManager<Scene, ILoadSceneInfo> _manager;
        readonly RoutineBehaviour _routineBehaviour;

        public SceneLoaderCoroutine(ISceneManager<Scene, ILoadSceneInfo> manager)
        {
            _manager = manager ?? throw new ArgumentNullException("Cannot create a scene loader with a null Scene Manager");
            _routineBehaviour = RoutineBehaviour.Instance;
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneAsync(sceneInfo);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => LoadSceneAsync(sceneInfo, setActive);

        public Coroutine TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => _routineBehaviour.StartCoroutine(intermediateSceneInfo == null ? TransitionDirectlyRoutine(targetSceneInfo) : TransitionWithIntermediateRoutine(targetSceneInfo, intermediateSceneInfo));

        public Coroutine UnloadSceneAsync(ILoadSceneInfo sceneInfo) => _routineBehaviour.StartCoroutine(UnloadRoutine(sceneInfo));

        public Coroutine LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null) => _routineBehaviour.StartCoroutine(LoadRoutine(sceneInfo, setActive, progress));

        IEnumerator LoadRoutine(ILoadSceneInfo sceneInfo, bool setActive, IProgress<float> progress)
        {
            yield return new WaitTask(_manager.LoadSceneAsync(sceneInfo, setActive, progress).AsTask());
        }

        IEnumerator UnloadRoutine(ILoadSceneInfo sceneInfo)
        {
            yield return new WaitTask(_manager.UnloadSceneAsync(sceneInfo));
        }

        IEnumerator TransitionWithIntermediateRoutine(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo)
        {
            var currentScene = _manager.GetActiveScene();
            yield return new WaitTask(_manager.LoadSceneAsync(intermediateSceneInfo).AsTask());

            var loadingBehavior = Object.FindObjectOfType<LoadingBase>();
            if (loadingBehavior)
            {
                yield return new WaitUntil(() => loadingBehavior.Active);

                if (currentScene.IsValid())
                    yield return UnloadSceneAsync(new LoadSceneInfoScene(currentScene));

                yield return new WaitTask(_manager.LoadSceneAsync(targetSceneInfo, true, loadingBehavior).AsTask());
                loadingBehavior.CompleteLoading();

                yield return new WaitWhile(() => loadingBehavior.Active);

                UnloadSceneAsync(intermediateSceneInfo);
            }
            else
            {
                if (currentScene.IsValid())
                    yield return UnloadSceneAsync(new LoadSceneInfoScene(currentScene));
                yield return LoadRoutine(targetSceneInfo, true, null);
                UnloadSceneAsync(intermediateSceneInfo);
            }
        }

        IEnumerator TransitionDirectlyRoutine(ILoadSceneInfo targetSceneInfo)
        {
            var currentScene = _manager.GetActiveScene();
            if (currentScene.IsValid())
                yield return UnloadSceneAsync(new LoadSceneInfoScene(currentScene));
            yield return LoadRoutine(targetSceneInfo, true, null);
        }
    }
}