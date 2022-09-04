#if ENABLE_ADDRESSABLES
/**
 * AddressableSceneLoaderCoroutine.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 9/4/2022 (en-US)
 */

using System.Collections;
using UnityEngine;

namespace MyUnityTools.SceneLoading.AddressablesSupport
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

        public void UnloadScene(IAddressableLoadSceneInfo sceneInfo) => UnloadSceneRoutine(sceneInfo);

        public Coroutine TransitionToSceneRoutine(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference) => _routineBehaviour.StartCoroutine(intermediateSceneReference == null ? TransitionDirectlyRoutine(targetSceneReference) : TransitionWithIntermediateRoutine(targetSceneReference, intermediateSceneReference));

        public Coroutine LoadSceneRoutine(IAddressableLoadSceneReference sceneReference, bool setActive) => _routineBehaviour.StartCoroutine(LoadRoutine(sceneReference, setActive));

        public Coroutine UnloadSceneRoutine(IAddressableLoadSceneInfo sceneInfo) => _routineBehaviour.StartCoroutine(UnloadRoutine(sceneInfo));

        IEnumerator LoadRoutine(IAddressableLoadSceneReference sceneReference, bool setActive)
        {
            var operation = sceneReference.LoadSceneAsync(_sceneManager);
            yield return operation;
            if (setActive)
                _sceneManager.SetActiveSceneHandle(operation);
        }

        IEnumerator UnloadRoutine(IAddressableLoadSceneInfo sceneInfo)
        {
            yield return sceneInfo.UnloadSceneAsync(_sceneManager);
        }

        IEnumerator TransitionWithIntermediateRoutine(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference)
        {
            var currentSceneHandle = _sceneManager.GetActiveSceneHandle();
            var loadingSceneHandle = intermediateSceneReference.LoadSceneAsync(_sceneManager);
            yield return loadingSceneHandle;
            _sceneManager.SetActiveSceneHandle(loadingSceneHandle);

            var loadingBehavior = Object.FindObjectOfType<LoadingBase>();
            if (loadingBehavior)
            {
                yield return new WaitUntil(() => loadingBehavior.Active);

                yield return LoadSceneRoutineWithReport(targetSceneReference, loadingBehavior);
                loadingBehavior.CompleteLoading();

                if (currentSceneHandle.IsValid())
                    UnloadSceneRoutine(new AddressableLoadSceneInfoOperationHandle(currentSceneHandle));

                yield return new WaitWhile(() => loadingBehavior.Active);

                UnloadSceneRoutine(new AddressableLoadSceneInfoOperationHandle(loadingSceneHandle));
            }
            else
            {
                yield return LoadSceneRoutine(targetSceneReference, true);
                if (currentSceneHandle.IsValid())
                    UnloadSceneRoutine(new AddressableLoadSceneInfoOperationHandle(currentSceneHandle));
                UnloadSceneRoutine(new AddressableLoadSceneInfoOperationHandle(loadingSceneHandle));
            }
        }

        IEnumerator TransitionDirectlyRoutine(IAddressableLoadSceneReference targetSceneReference)
        {
            var currentSceneHandle = _sceneManager.GetActiveSceneHandle();
            yield return LoadSceneRoutine(targetSceneReference, true);
            UnloadSceneRoutine(new AddressableLoadSceneInfoOperationHandle(currentSceneHandle));
        }

        IEnumerator LoadSceneRoutineWithReport(IAddressableLoadSceneReference targetSceneReference, System.IProgress<float> progress)
        {
            var operation = targetSceneReference.LoadSceneAsync(_sceneManager);
            while (!operation.IsDone)
            {
                progress.Report(operation.PercentComplete);
                yield return null;
            }
            _sceneManager.SetActiveSceneHandle(operation);
        }
    }
}
#endif