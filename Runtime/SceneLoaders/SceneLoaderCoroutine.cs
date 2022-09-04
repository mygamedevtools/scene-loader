/**
 * SceneLoaderCoroutine.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 9/4/2022 (en-US)
 */

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading
{
    public class SceneLoaderCoroutine : ISceneLoaderCoroutine
    {
        readonly RoutineBehaviour _routineBehaviour;

        public SceneLoaderCoroutine()
        {
            _routineBehaviour = RoutineBehaviour.Instance;
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => TransitionToSceneRoutine(targetSceneInfo, intermediateSceneInfo);

        public void UnloadScene(ILoadSceneInfo sceneInfo) => UnloadSceneRoutine(sceneInfo);

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive) => LoadSceneRoutine(sceneInfo, setActive);

        public Coroutine TransitionToSceneRoutine(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo) => _routineBehaviour.StartCoroutine(intermediateSceneInfo == null ? TransitionDirectlyRoutine(targetSceneInfo) : TransitionWithIntermediateRoutine(targetSceneInfo, intermediateSceneInfo));

        public Coroutine UnloadSceneRoutine(ILoadSceneInfo sceneInfo) => _routineBehaviour.StartCoroutine(UnloadRoutine(sceneInfo));

        public Coroutine LoadSceneRoutine(ILoadSceneInfo sceneInfo, bool setActive) => _routineBehaviour.StartCoroutine(LoadRoutine(sceneInfo, setActive));

        IEnumerator LoadRoutine(ILoadSceneInfo sceneInfo, bool setActive)
        {
            yield return sceneInfo.LoadSceneAsync();
            if (setActive)
                SceneManager.SetActiveScene(sceneInfo.GetScene());
        }

        IEnumerator UnloadRoutine(ILoadSceneInfo sceneInfo)
        {
            yield return sceneInfo.UnloadSceneAsync();
        }

        IEnumerator TransitionWithIntermediateRoutine(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfoIndex(SceneManager.GetActiveScene().buildIndex);
            yield return LoadSceneRoutine(intermediateSceneInfo, true);

            var loadingBehavior = Object.FindObjectOfType<LoadingBase>();
            if (loadingBehavior)
            {
                yield return new WaitUntil(() => loadingBehavior.Active);

                yield return LoadSceneRoutineWithReport(targetSceneInfo, loadingBehavior);
                loadingBehavior.CompleteLoading();
                UnloadSceneRoutine(currentSceneInfo);

                yield return new WaitWhile(() => loadingBehavior.Active);

                UnloadSceneRoutine(intermediateSceneInfo);
            }
            else
            {
                yield return LoadSceneRoutine(targetSceneInfo, true);
                UnloadSceneRoutine(currentSceneInfo);
                UnloadSceneRoutine(intermediateSceneInfo);
            }
        }

        IEnumerator TransitionDirectlyRoutine(ILoadSceneInfo targetSceneInfo)
        {
            var currentSceneInfo = new LoadSceneInfoIndex(SceneManager.GetActiveScene().buildIndex);
            yield return LoadSceneRoutine(targetSceneInfo, true);
            UnloadSceneRoutine(currentSceneInfo);
        }

        IEnumerator LoadSceneRoutineWithReport(ILoadSceneInfo loadSceneInfo, System.IProgress<float> progress)
        {
            var operation = loadSceneInfo.LoadSceneAsync();
            while (!operation.isDone)
            {
                progress.Report(operation.progress);
                yield return null;
            }
            SceneManager.SetActiveScene(loadSceneInfo.GetScene());
        }
    }
}