#if ENABLE_ADDRESSABLES
/**
 * AddressableSceneLoaderCoroutine.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 9/4/2022 (en-US)
 */

using System;
using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MyGameDevTools.SceneLoading.AddressablesSupport
{
    public class AddressableSceneLoaderCoroutine : IAddressableSceneLoaderCoroutine
    {
        public IAddressableSceneManager SceneManager => _sceneManager;

        readonly IAddressableSceneManager _sceneManager;
        readonly RoutineBehaviour _routineBehaviour;

        public AddressableSceneLoaderCoroutine(IAddressableSceneManager sceneManager)
        {
            _sceneManager = sceneManager;
            _routineBehaviour = RoutineBehaviour.Instance;
        }

        public void TransitionToScene(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference) => TransitionToSceneRoutine(targetSceneReference, intermediateSceneReference);

        public void LoadScene(IAddressableLoadSceneReference sceneReference, bool setActive) => LoadSceneRoutine(sceneReference, setActive);

        public void UnloadScene(IAddressableLoadSceneReference sceneInfo) => UnloadSceneRoutine(sceneInfo);

        public Coroutine TransitionToSceneRoutine(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference) => _routineBehaviour.StartCoroutine(intermediateSceneReference == null ? TransitionDirectlyRoutine(targetSceneReference) : TransitionWithIntermediateRoutine(targetSceneReference, intermediateSceneReference));

        public Coroutine LoadSceneRoutine(IAddressableLoadSceneReference sceneReference, bool setActive = false, IProgress<float> progress = null) => _routineBehaviour.StartCoroutine(LoadRoutine(sceneReference, setActive, progress));

        public Coroutine UnloadSceneRoutine(IAddressableLoadSceneReference sceneInfo) => _routineBehaviour.StartCoroutine(UnloadRoutine(sceneInfo));

        IEnumerator LoadRoutine(IAddressableLoadSceneReference sceneReference, bool setActive, IProgress<float> progress)
        {
            yield return new WaitTask(_sceneManager.LoadSceneAsync(sceneReference, setActive, progress).AsTask());
        }

        IEnumerator UnloadRoutine(IAddressableLoadSceneReference sceneInfo)
        {
            yield return new WaitTask(_sceneManager.UnloadSceneAsync(sceneInfo));
        }

        IEnumerator TransitionWithIntermediateRoutine(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference)
        {
            var currentScene = _sceneManager.GetActiveScene();

            var sceneLoadTask = _sceneManager.LoadSceneAsync(intermediateSceneReference);
            yield return new WaitUntil(() => sceneLoadTask.IsCompleted);
            var loadingScene = sceneLoadTask.Result;

            var loadingBehavior = Object.FindObjectOfType<LoadingBase>();
            if (loadingBehavior)
            {
                yield return new WaitUntil(() => loadingBehavior.Active);

                if (currentScene.Scene.IsValid())
                    yield return UnloadSceneRoutine(new AddressableLoadSceneInfoInstance(currentScene));

                sceneLoadTask = _sceneManager.LoadSceneAsync(targetSceneReference, true, loadingBehavior);
                yield return new WaitUntil(() => sceneLoadTask.IsCompleted);
                loadingBehavior.CompleteLoading();

                yield return new WaitWhile(() => loadingBehavior.Active);

                UnloadSceneRoutine(new AddressableLoadSceneInfoInstance(loadingScene));
            }
            else
            {
                if (currentScene.Scene.IsValid())
                    yield return UnloadSceneRoutine(new AddressableLoadSceneInfoInstance(currentScene));
                yield return LoadSceneRoutine(targetSceneReference, true);
                UnloadSceneRoutine(new AddressableLoadSceneInfoInstance(loadingScene));
            }
        }

        IEnumerator TransitionDirectlyRoutine(IAddressableLoadSceneReference targetSceneReference)
        {
            var currentScene = _sceneManager.GetActiveScene();
            if (currentScene.Scene.IsValid())
                yield return UnloadSceneRoutine(new AddressableLoadSceneInfoInstance(currentScene));
            yield return LoadSceneRoutine(targetSceneReference, true);
        }
    }
}
#endif