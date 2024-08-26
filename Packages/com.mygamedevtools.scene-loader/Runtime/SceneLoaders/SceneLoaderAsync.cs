using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public readonly struct SceneLoaderAsync : ISceneLoaderAsync
    {
        public ISceneManager Manager => _manager;

        readonly ISceneManager _manager;
        readonly CancellationTokenSource _lifetimeTokenSource;

        public SceneLoaderAsync(ISceneManager manager)
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

        public void TransitionToScenes(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToScenesAsync(targetScenes, setIndexActive, intermediateSceneInfo).AsTask().Forget(HandleFireAndForgetException);
        }

        public void TransitionToScene(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default)
        {
            TransitionToSceneAsync(targetSceneInfo, intermediateSceneInfo).AsTask().Forget(HandleFireAndForgetException);
        }

        public void TransitionToScenesFromScenes(ILoadSceneInfo[] targetScenes, ILoadSceneInfo[] fromScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToScenesFromScenesAsync(targetScenes, fromScenes, setIndexActive, intermediateSceneInfo).Forget(HandleFireAndForgetException);
        }

        public void TransitionToSceneFromScenes(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo[] fromScenes, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToSceneFromScenesAsync(targetSceneInfo, fromScenes, intermediateSceneInfo).Forget(HandleFireAndForgetException);
        }

        public void TransitionToScenesFromAll(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToScenesFromAllAsync(targetScenes, setIndexActive, intermediateSceneInfo).Forget(HandleFireAndForgetException);
        }

        public void TransitionToSceneFromAll(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null)
        {
            TransitionToSceneFromAllAsync(targetSceneInfo, intermediateSceneInfo).Forget(HandleFireAndForgetException);
        }

        public void UnloadScenes(ILoadSceneInfo[] sceneInfos)
        {
            UnloadScenesAsync(sceneInfos).AsTask().Forget(HandleFireAndForgetException);
        }

        public void UnloadScene(ILoadSceneInfo sceneInfo)
        {
            UnloadSceneAsync(sceneInfo).AsTask().Forget(HandleFireAndForgetException);
        }

        public void LoadScenes(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1)
        {
            LoadScenesAsync(sceneInfos, setIndexActive, null).AsTask().Forget(HandleFireAndForgetException);
        }

        public void LoadScene(ILoadSceneInfo sceneInfo, bool setActive = false)
        {
            LoadSceneAsync(sceneInfo, setActive, null).AsTask().Forget(HandleFireAndForgetException);
        }

        public ValueTask<Scene[]> TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null)
        {
            Scene activeScene = _manager.GetActiveScene();
            ILoadSceneInfo[] fromScenes = activeScene.IsValid() ? new ILoadSceneInfo[] { new LoadSceneInfoScene(activeScene) } : null;
            return intermediateSceneReference == null
                ? TransitionDirectlyAsync(targetScenes, fromScenes, setIndexActive, _lifetimeTokenSource.Token)
                : TransitionWithIntermediateAsync(targetScenes, fromScenes, setIndexActive, intermediateSceneReference, _lifetimeTokenSource.Token);
        }

        public async ValueTask<Scene> TransitionToSceneAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = default)
        {
            var result = await TransitionToScenesAsync(new ILoadSceneInfo[] { targetSceneInfo }, 0, intermediateSceneInfo);
            return result == null || result.Length == 0 ? default : result[0];
        }

        public ValueTask<Scene[]> TransitionToScenesFromScenesAsync(ILoadSceneInfo[] targetScenes, ILoadSceneInfo[] fromScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null)
        {
            return intermediateSceneReference == null
                ? TransitionDirectlyAsync(targetScenes, fromScenes, setIndexActive, _lifetimeTokenSource.Token)
                : TransitionWithIntermediateAsync(targetScenes, fromScenes, setIndexActive, intermediateSceneReference, _lifetimeTokenSource.Token);
        }

        public async ValueTask<Scene> TransitionToSceneFromScenesAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo[] fromScenes, ILoadSceneInfo intermediateSceneReference = null)
        {
            var result = await TransitionToScenesFromScenesAsync(new ILoadSceneInfo[] { targetSceneInfo }, fromScenes, 0, intermediateSceneReference);
            return result == null || result.Length == 0 ? default : result[0];
        }

        public ValueTask<Scene[]> TransitionToScenesFromAllAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = null)
        {
            ILoadSceneInfo[] fromScenes = GetAllLoadedSceneInfos();
            return intermediateSceneReference == null
                ? TransitionDirectlyAsync(targetScenes, fromScenes, setIndexActive, _lifetimeTokenSource.Token)
                : TransitionWithIntermediateAsync(targetScenes, fromScenes, setIndexActive, intermediateSceneReference, _lifetimeTokenSource.Token);
        }

        public async ValueTask<Scene> TransitionToSceneFromAllAsync(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneReference = null)
        {
            var result = await TransitionToScenesFromAllAsync(new ILoadSceneInfo[] { targetSceneInfo }, 0, intermediateSceneReference);
            return result == null || result.Length == 0 ? default : result[0];
        }

        public ValueTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null)
        {
            return _manager.LoadScenesAsync(sceneReferences, setIndexActive, progress, _lifetimeTokenSource.Token);
        }

        public ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null)
        {
            return _manager.LoadSceneAsync(sceneInfo, setActive, progress, _lifetimeTokenSource.Token);
        }

        public ValueTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneReferences)
        {
            return _manager.UnloadScenesAsync(sceneReferences, _lifetimeTokenSource.Token);
        }

        public ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo)
        {
            return _manager.UnloadSceneAsync(sceneInfo, _lifetimeTokenSource.Token);
        }

        async ValueTask<Scene[]> TransitionDirectlyAsync(ILoadSceneInfo[] targetScenes, ILoadSceneInfo[] fromScenes, int setIndexActive, CancellationToken token)
        {
            // If only one scene is loaded, we need to create a temporary scene for transition.
            Scene tempScene = default;
            if (SceneManager.sceneCount <= 1)
            {
                tempScene = SceneManager.CreateScene("temp-transition-scene");
            }
            await UnloadSourceScenesAsync(fromScenes, token);

            Scene[] loadedScenes = await _manager.LoadScenesAsync(targetScenes, setIndexActive, token: token);

            if (tempScene.IsValid())
            {
                _ = SceneManager.UnloadSceneAsync(tempScene);
            }
            return loadedScenes;
        }

        async ValueTask<Scene[]> TransitionWithIntermediateAsync(ILoadSceneInfo[] targetScenes, ILoadSceneInfo[] fromScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, CancellationToken token)
        {
            Scene loadingScene;
            try
            {
                loadingScene = await _manager.LoadSceneAsync(intermediateSceneInfo, token: token);
            }
            catch
            {
                throw;
            }

            intermediateSceneInfo = new LoadSceneInfoScene(loadingScene);

#if UNITY_2023_2_OR_NEWER
            LoadingBehavior loadingBehavior = UnityEngine.Object.FindObjectsByType<LoadingBehavior>(FindObjectsSortMode.None).FirstOrDefault(l => l.gameObject.scene == loadingScene);
#else
            LoadingBehavior loadingBehavior = UnityEngine.Object.FindObjectsOfType<LoadingBehavior>().FirstOrDefault(l => l.gameObject.scene == loadingScene);
#endif
            return loadingBehavior
                ? await TransitionWithIntermediateLoadingAsync(targetScenes, fromScenes, setIndexActive, intermediateSceneInfo, loadingBehavior, token)
                : await TransitionWithIntermediateNoLoadingAsync(targetScenes, fromScenes, setIndexActive, intermediateSceneInfo, token);
        }

        async ValueTask<Scene[]> TransitionWithIntermediateLoadingAsync(ILoadSceneInfo[] targetScenes, ILoadSceneInfo[] fromScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, LoadingBehavior loadingBehavior, CancellationToken token)
        {
            LoadingProgress progress = loadingBehavior.Progress;
            while (progress.State != LoadingState.Loading && !token.IsCancellationRequested)
                await Task.Yield();

            token.ThrowIfCancellationRequested();

            await UnloadSourceScenesAsync(fromScenes, token);

            Scene[] loadedScenes = await _manager.LoadScenesAsync(targetScenes, setIndexActive, progress, token);
            progress.SetState(LoadingState.TargetSceneLoaded);

            while (progress.State != LoadingState.TransitionComplete && !token.IsCancellationRequested)
                await Task.Yield();

            token.ThrowIfCancellationRequested();

            _manager.UnloadSceneAsync(intermediateSceneInfo, token).Forget(HandleFireAndForgetException);
            return loadedScenes;
        }

        async ValueTask<Scene[]> TransitionWithIntermediateNoLoadingAsync(ILoadSceneInfo[] targetScenes, ILoadSceneInfo[] fromScenes, int setIndexActive, ILoadSceneInfo intermediateSceneInfo, CancellationToken token)
        {
            await UnloadSourceScenesAsync(fromScenes, token);
            Scene[] loadedScenes = await _manager.LoadScenesAsync(targetScenes, setIndexActive, token: token);
            _manager.UnloadSceneAsync(intermediateSceneInfo, token).Forget(HandleFireAndForgetException);
            return loadedScenes;
        }

        ValueTask<Scene[]> UnloadSourceScenesAsync(ILoadSceneInfo[] fromScenes, CancellationToken token)
        {
            if (fromScenes == null || fromScenes.Length == 0)
                return default;

            return _manager.UnloadScenesAsync(fromScenes, token);
        }

        ILoadSceneInfo[] GetAllLoadedSceneInfos()
        {
            int count = _manager.LoadedSceneCount;
            ILoadSceneInfo[] loadedSceneInfos = new ILoadSceneInfo[count];
            for (int i = 0; i < count; i++)
            {
                loadedSceneInfos[i] = new LoadSceneInfoScene(_manager.GetLoadedSceneAt(i));
            }
            return loadedSceneInfos;
        }

        void HandleFireAndForgetException(Exception exception)
        {
            Debug.LogWarningFormat("[SceneLoader] An exception was caught during a fire and forget task. Usually this can be caused due to internal task cancellation on exiting playmode. If that's not the case, investigate for issues on the async scene operations. Exception:\n{0}", exception);
        }

        public override string ToString()
        {
            return $"Scene Loader [Async] with {_manager.GetType().Name}";
        }
    }
}