#if ENABLE_UNITASK
using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading.UniTaskSupport
{
    public class SceneLoaderUniTask : ISceneLoaderUniTask
    {
        public ISceneManager Manager => _manager;

        readonly ISceneManager _manager;
        readonly CancellationTokenSource _lifetimeTokenSource;

        public SceneLoaderUniTask(ISceneManager manager)
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
            TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneInfo, externalOriginScene).Forget(HandleFireAndForgetException);
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default)
        {
            TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo, externalOriginScene).Forget(HandleFireAndForgetException);
        }

        public void UnloadScenes(ILoadSceneInfo[] sceneInfos)
        {
            UnloadScenesAsync(sceneInfos).Forget(HandleFireAndForgetException);
        }

        public void UnloadScene(ILoadSceneInfo sceneInfo)
        {
            UnloadSceneAsync(sceneInfo).Forget(HandleFireAndForgetException);
        }

        public void LoadScenes(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1)
        {
            LoadScenesAsync(sceneInfos, setIndexActive).Forget(HandleFireAndForgetException);
        }

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false)
        {
            LoadSceneAsync(sceneInfo, setActive).Forget(HandleFireAndForgetException);
        }

        public UniTask<Scene[]> TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null, Scene externalOriginScene = default, CancellationToken token = default)
        {
            CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return intermediateSceneReference == null
                ? TransitionDirectlyAsync(targetScenes, setIndexActive, externalOriginScene, linkedToken.Token).RunAndDisposeToken(linkedToken)
                : TransitionWithIntermediateAsync(targetScenes, setIndexActive, intermediateSceneReference, externalOriginScene, linkedToken.Token).RunAndDisposeToken(linkedToken);
        }

        public async UniTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default, Scene externalOriginScene = default, CancellationToken token = default)
        {
            var result = await TransitionToScenesAsync(new ILoadSceneInfo[] { targetSceneInfo }, 0, intermediateSceneInfo, externalOriginScene);
            return result == null || result.Length == 0 ? default : result[0];
        }

        public async UniTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null, CancellationToken token = default)
        {
            CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return await _manager.LoadScenesAsync(sceneReferences, setIndexActive, progress, linkedToken.Token).RunAndDisposeToken(linkedToken);
        }

        public async UniTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null, CancellationToken token = default)
        {
            CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return await _manager.LoadSceneAsync(sceneInfo, setActive, progress, linkedToken.Token).RunAndDisposeToken(linkedToken);
        }

        public async UniTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneReferences, CancellationToken token = default)
        {
            CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return await _manager.UnloadScenesAsync(sceneReferences, linkedToken.Token).RunAndDisposeToken(linkedToken);
        }

        public async UniTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo, CancellationToken token = default)
        {
            CancellationTokenSource linkedToken = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeTokenSource.Token, token);
            return await _manager.UnloadSceneAsync(sceneInfo, linkedToken.Token).RunAndDisposeToken(linkedToken);
        }

        async UniTask<Scene[]> TransitionDirectlyAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, Scene externalOriginScene, CancellationToken token)
        {
            var externalOrigin = externalOriginScene.IsValid();
            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();
            await UnloadCurrentScene(currentScene, externalOrigin, token);
            return await _manager.LoadScenesAsync(targetScenes, setIndexActive, token: token);
        }

        async UniTask<Scene[]> TransitionWithIntermediateAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, Scene externalOriginScene, CancellationToken token)
        {
            var externalOrigin = externalOriginScene.IsValid();

            Scene loadingScene = default;
            try
            {
                loadingScene = await _manager.LoadSceneAsync(intermediateSceneInfo, token: token);
            }
            catch
            {
                throw;
            }

            intermediateSceneInfo = new LoadSceneInfoScene(loadingScene);

            var currentScene = externalOrigin ? externalOriginScene : _manager.GetActiveScene();

#if UNITY_2023_2_OR_NEWER
            var loadingBehavior = UnityEngine.Object.FindObjectsByType<LoadingBehavior>(FindObjectsSortMode.None).FirstOrDefault(l => l.gameObject.scene == loadingScene);
#else
            var loadingBehavior = UnityEngine.Object.FindObjectsOfType<LoadingBehavior>().FirstOrDefault(l => l.gameObject.scene == loadingScene);
#endif
            return loadingBehavior
                ? await TransitionWithIntermediateLoadingAsync(targetScenes, setIndexActive, intermediateSceneInfo, loadingBehavior, currentScene, externalOrigin, token)
                : await TransitionWithIntermediateNoLoadingAsync(targetScenes, setIndexActive, intermediateSceneInfo, currentScene, externalOrigin, token);
        }

        async UniTask<Scene[]> TransitionWithIntermediateLoadingAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, LoadingBehavior loadingBehavior, Scene currentScene, bool externalOrigin, CancellationToken token)
        {
            var progress = loadingBehavior.Progress;
            await UniTask.WaitUntil(() => progress.State == LoadingState.Loading, cancellationToken: token);

            if (!externalOrigin)
                currentScene = _manager.GetActiveScene();

            await UnloadCurrentScene(currentScene, externalOrigin, token);

            var loadedScenes = await _manager.LoadScenesAsync(targetScenes, setIndexActive, progress, token);
            progress.SetState(LoadingState.TargetSceneLoaded);

            await UniTask.WaitUntil(() => progress.State == LoadingState.TransitionComplete, cancellationToken: token);

            _manager.UnloadSceneAsync(intermediateSceneInfo, token).Forget(HandleFireAndForgetException);
            return loadedScenes;
        }

        async UniTask<Scene[]> TransitionWithIntermediateNoLoadingAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, Scene currentScene, bool externalOrigin, CancellationToken token)
        {
            await UnloadCurrentScene(currentScene, externalOrigin, token);
            var loadedScenes = await _manager.LoadScenesAsync(targetScenes, setIndexActive, token: token);
            _manager.UnloadSceneAsync(intermediateSceneInfo).Forget(HandleFireAndForgetException);
            return loadedScenes;
        }

        async UniTask UnloadCurrentScene(Scene currentScene, bool externalOrigin, CancellationToken token)
        {
            if (!currentScene.IsValid())
                return;

            if (externalOrigin)
            {
                AsyncOperation operation = UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(currentScene);
                if (operation != null)
                    await operation;

                token.ThrowIfCancellationRequested();
            }
            else
                await _manager.UnloadSceneAsync(new LoadSceneInfoScene(currentScene), token);
        }

        void HandleFireAndForgetException(Exception exception)
        {
            Debug.LogWarningFormat("[{0}] An exception was caught during a fire and forget task:\n{1}", nameof(SceneLoaderUniTask), exception);
        }

        public override string ToString()
        {
            return $"Scene Loader [UniTask] with {_manager.GetType().Name}";
        }
    }
}
#endif