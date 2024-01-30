#if ENABLE_UNITASK && !OVERRIDE_DISABLE_UNITASK
#define USE_UNITASK
#endif
#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

[assembly: InternalsVisibleTo("MyGameDevTools.SceneLoading.Tests")]

namespace MyGameDevTools.SceneLoading
{
    public class SceneManager : ISceneManager, ISceneManagerReporter
    {
        public event Action<Scene, Scene> ActiveSceneChanged;
        public event Action<Scene> SceneUnloaded;
        public event Action<Scene> SceneLoaded;

        public bool IsUnloadingScenes => _unloadingScenes.Count > 0;
        public int SceneCount => _loadedScenes.Count;

        readonly List<Scene> _unloadingScenes = new List<Scene>();
        readonly List<Scene> _loadedScenes = new List<Scene>();
        readonly CancellationTokenSource _lifetimeToken = new CancellationTokenSource();

        Scene _activeScene;

        public void Dispose()
        {
            _lifetimeToken.Cancel();
            _lifetimeToken.Dispose();

            _unloadingScenes.Clear();
            _loadedScenes.Clear();
        }

        public void SetActiveScene(Scene scene)
        {
            var validScene = scene.IsValid();
            if (validScene && !_loadedScenes.Contains(scene))
                throw new InvalidOperationException($"[{GetType().Name}] Cannot set active the scene \"{scene.name}\" that has not been loaded through this {GetType().Name}.");

            var previousScene = _activeScene;
            _activeScene = scene;
            if (validScene)
                UnitySceneManager.SetActiveScene(scene);

            ActiveSceneChanged?.Invoke(previousScene, scene);
        }

        public Scene GetActiveScene() => _activeScene;

        public Scene GetLastLoadedScene()
        {
            if (SceneCount == 0)
                return default;

            for (int i = SceneCount - 1; i >= 0; i--)
                if (!_unloadingScenes.Contains(_loadedScenes[i]) && _loadedScenes[i].isLoaded)
                    return _loadedScenes[i];

            return default;
        }

        public Scene GetLoadedSceneAt(int index) => _loadedScenes[index];

        public Scene GetLoadedSceneByName(string name)
        {
            foreach (var scene in _loadedScenes)
                if (scene.name == name)
                    return scene;
            throw new ArgumentException($"[{GetType().Name}] Could not find any loaded scene with the name '{name}'.", nameof(name));
        }

        public async ValueTask<Scene[]> LoadScenesAsync(ILoadSceneInfo[] sceneInfos, int setIndexActive = -1, IProgress<float> progress = null, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken.Token, token);
            try
            {
                return await LoadScenesAsync_Internal(sceneInfos, setIndexActive, progress, linkedSource.Token);
            }
            catch (OperationCanceledException cancelException)
            {
                Debug.LogWarningFormat("[{0}] LoadScenesAsync was canceled. Exception:\n{1}", GetType().Name, cancelException);
                throw;
            }
            finally
            {
                linkedSource.Dispose();
            }
        }

        public async ValueTask<Scene> LoadSceneAsync(ILoadSceneInfo sceneInfo, bool setActive = false, IProgress<float> progress = null, CancellationToken token = default)
        {
            sceneInfo = sceneInfo ?? throw new NullReferenceException($"[{GetType().Name}] Provided scene info is null.");

            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken.Token, token);
            Scene[] loadedScenes = null;
            try
            {
                loadedScenes = await LoadScenesAsync_Internal(new ILoadSceneInfo[] { sceneInfo }, setActive ? 0 : -1, progress, linkedSource.Token);
            }
            catch (OperationCanceledException cancelException)
            {
                Debug.LogWarningFormat("[{0}] LoadSceneAsync was canceled. Exception:\n{1}", GetType().Name, cancelException);
                throw;
            }
            finally
            {
                linkedSource.Dispose();
            }

            return loadedScenes != null && loadedScenes.Length > 0 ? loadedScenes[0] : default;
        }

        public async ValueTask<Scene[]> UnloadScenesAsync(ILoadSceneInfo[] sceneInfos, CancellationToken token = default)
        {
            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken.Token, token);
            try
            {
                return await UnloadScenesAsync_Internal(sceneInfos, linkedSource.Token);
            }
            catch (OperationCanceledException cancelException)
            {
                Debug.LogWarningFormat("[{0}] UnloadScenesAsync was canceled. Exception:\n{1}", GetType().Name, cancelException);
                throw;
            }
            finally
            {
                linkedSource.Dispose();
            }
        }

        public async ValueTask<Scene> UnloadSceneAsync(ILoadSceneInfo sceneInfo, CancellationToken token = default)
        {
            sceneInfo = sceneInfo ?? throw new ArgumentNullException(nameof(sceneInfo), $"[{GetType().Name}] Provided scene info is null.");

            CancellationTokenSource linkedSource = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeToken.Token, token);
            Scene[] unloadedScenes = null;
            try
            {
                unloadedScenes = await UnloadScenesAsync_Internal(new ILoadSceneInfo[] { sceneInfo }, linkedSource.Token);
            }
            catch (OperationCanceledException cancelException)
            {
                Debug.LogWarningFormat("[{0}] UnloadSceneAsync was canceled. Exception:\n{1}", GetType().Name, cancelException);
                throw;
            }
            finally
            {
                linkedSource.Dispose();
            }

            return unloadedScenes != null && unloadedScenes.Length > 0 ? unloadedScenes[0] : default;
        }

        async ValueTask<Scene[]> LoadScenesAsync_Internal(ILoadSceneInfo[] sceneInfos, int setIndexActive, IProgress<float> progress, CancellationToken token)
        {
            if (sceneInfos == null || sceneInfos.Length == 0)
                throw new ArgumentException(nameof(sceneInfos), $"[{GetType().Name}] Provided scene group is null or empty.");
            if (setIndexActive >= sceneInfos.Length)
                throw new ArgumentException(nameof(setIndexActive), $"[{GetType().Name}] Provided index to set active is bigger than the provided scene group size.");

            var operationGroup = GetLoadSceneOperations(sceneInfos, ref setIndexActive);
            if (operationGroup.Operations.Count == 0)
                return Array.Empty<Scene>();

            while (!operationGroup.IsDone && !token.IsCancellationRequested)
            {
#if USE_UNITASK
                await UniTask.Yield(token);
#else
                await Task.Yield();
#endif
                progress?.Report(operationGroup.Progress);
            }

            token.ThrowIfCancellationRequested();

            var loadedScenes = GetLastUnityLoadedScenesByInfos(sceneInfos, ref setIndexActive);

            _loadedScenes.AddRange(loadedScenes);
            foreach (var scene in loadedScenes)
                SceneLoaded?.Invoke(scene);

            if (setIndexActive >= 0)
                SetActiveScene(loadedScenes[setIndexActive]);

            return loadedScenes;
        }

        async ValueTask<Scene[]> UnloadScenesAsync_Internal(ILoadSceneInfo[] sceneInfos, CancellationToken token)
        {
            if (sceneInfos == null || sceneInfos.Length == 0)
                throw new ArgumentException($"[{GetType().Name}] Provided scene group is null or empty.", nameof(sceneInfos));

            var loadedScenes = GetLastLoadedScenesByInfos(sceneInfos, out var unloadingIndexes);
            if (loadedScenes.Count == 0)
                return Array.Empty<Scene>();

            int unloadingLength = unloadingIndexes.Length;
            var unloadingScenes = new Scene[unloadingLength];
            int i;
            for (i = 0; i < unloadingLength; i++)
                unloadingScenes[i] = loadedScenes[unloadingIndexes[i]];

            for (i = 0; i < unloadingLength; i++)
                loadedScenes.Remove(unloadingScenes[i]);

            var operationGroup = new AsyncOperationGroup(loadedScenes.Count);
            foreach (var scene in loadedScenes)
            {
                operationGroup.Operations.Add(UnitySceneManager.UnloadSceneAsync(scene));
                _loadedScenes.Remove(scene);
                _unloadingScenes.Add(scene);
            }

            while (!operationGroup.IsDone && !token.IsCancellationRequested)
            {
#if USE_UNITASK
                await UniTask.Yield(token);
#else
                await Task.Yield();
#endif
            }

            token.ThrowIfCancellationRequested();

            foreach (var scene in loadedScenes)
            {
                _unloadingScenes.Remove(scene);
                SceneUnloaded?.Invoke(scene);
                if (_activeScene == scene)
                    SetActiveScene(GetLastLoadedScene());
            }

            var tasks = new Task[unloadingLength];
            for (i = 0; i < unloadingLength; i++)
                tasks[i] = WaitForSceneUnload(unloadingScenes[i], token).AsTask();

            await Task.WhenAll(tasks);

            foreach (var scene in unloadingScenes)
                loadedScenes.Add(scene);
            return loadedScenes.ToArray();
        }

        async ValueTask<Scene> WaitForSceneUnload(Scene scene, CancellationToken token)
        {
#if USE_UNITASK
            await UniTask.WaitUntil(() => !_unloadingScenes.Contains(scene), cancellationToken: token);
#else
            while (_unloadingScenes.Contains(scene) && !token.IsCancellationRequested)
                await Task.Yield();
#endif
            token.ThrowIfCancellationRequested();

            return scene;
        }

        AsyncOperationGroup GetLoadSceneOperations(ILoadSceneInfo[] sceneInfos, ref int setIndexActive)
        {
            var sceneLength = sceneInfos.Length;
            var operationGroup = new AsyncOperationGroup(sceneLength);
            for (int i = 0; i < sceneLength; i++)
            {
                if (TryGetLoadSceneOperation(sceneInfos[i], out var operation))
                    operationGroup.Operations.Add(operation);
                else if (i == setIndexActive)
                    setIndexActive = -1;
            }
            return operationGroup;
        }

        List<Scene> GetLastLoadedScenesByInfos(ILoadSceneInfo[] sceneInfos, out int[] unloadingIndexes)
        {
            var unloadingIndexesList = new List<int>();
            var sceneInfosList = new List<ILoadSceneInfo>(sceneInfos);
            var scenes = new List<Scene>(sceneInfos.Length);

            int sceneCount = SceneCount;
            int i;
            for (i = sceneCount - 1; i >= 0 && sceneInfosList.Count > 0; i--)
                tryValidateSceneReference(_loadedScenes[i], out _);

            sceneCount = _unloadingScenes.Count;
            for (i = 0; i < sceneCount; i++)
                if (tryValidateSceneReference(_unloadingScenes[i], out int index))
                    unloadingIndexesList.Add(index);

            if (sceneInfosList.Count > 0)
            {
                var builder = new StringBuilder($"[{GetType().Name}] Some of the scenes could not be found loaded in the Unity Scene Manager:\n");
                for (i = 0; i < sceneInfosList.Count; i++)
                    builder.AppendLine($" ({i}): {sceneInfosList[i]}");

                Debug.LogWarning(builder.ToString());
            }

            unloadingIndexes = unloadingIndexesList.ToArray();
            return scenes;

            bool tryValidateSceneReference(Scene scene, out int index)
            {
                foreach (var info in sceneInfosList)
                {
                    if (info.IsReferenceToScene(scene))
                    {
                        sceneInfosList.Remove(info);
                        scenes.Add(scene);
                        index = scenes.Count - 1;
                        return true;
                    }
                }
                index = -1;
                return false;
            }
        }

        Scene[] GetLastUnityLoadedScenesByInfos(ILoadSceneInfo[] sceneInfos, ref int setIndexActive)
        {
            var sceneInfosList = new List<ILoadSceneInfo>(sceneInfos);
            var scenes = new List<Scene>(sceneInfos.Length);

            var sceneCount = UnitySceneManager.sceneCount;
            for (int i = sceneCount - 1; i >= 0 && sceneInfosList.Count > 0; i--)
            {
                var scene = UnitySceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                    validateSceneReference(scene, ref setIndexActive);
            }

            if (sceneInfosList.Count > 0)
            {
                var builder = new StringBuilder($"[{GetType().Name}] Some of the scenes could not be found loaded in the Unity Scene Manager:\n");
                for (int i = 0; i < sceneInfosList.Count; i++)
                    builder.AppendLine($" ({i}): {sceneInfosList[i]}");

                Debug.LogWarning(builder.ToString());

                if (setIndexActive >= 0 && sceneInfosList.Contains(sceneInfos[setIndexActive]))
                    setIndexActive = -1;
            }

            return scenes.ToArray();

            void validateSceneReference(Scene scene, ref int setIndexActive)
            {
                foreach (var info in sceneInfosList)
                {
                    if (info.IsReferenceToScene(scene))
                    {
                        sceneInfosList.Remove(info);
                        scenes.Add(scene);
                        if (setIndexActive >= 0 && sceneInfos[setIndexActive] == info)
                            setIndexActive = scenes.Count - 1;
                        return;
                    }
                }
            }
        }

        bool TryGetLoadSceneOperation(ILoadSceneInfo sceneInfo, out AsyncOperation operation)
        {
            operation = null;
            if (sceneInfo.Reference is int index)
                operation = UnitySceneManager.LoadSceneAsync(index, LoadSceneMode.Additive);
            else if (sceneInfo.Reference is string name)
                operation = UnitySceneManager.LoadSceneAsync(name, LoadSceneMode.Additive);
            else if (sceneInfo.Reference is Scene scene)
                operation = UnitySceneManager.LoadSceneAsync(scene.buildIndex, LoadSceneMode.Additive);
            else
                Debug.LogWarning($"[{GetType().Name}] Unexpected {nameof(ILoadSceneInfo.Reference)} type.");

            return operation != null;
        }
    }
}