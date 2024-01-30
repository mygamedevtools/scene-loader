#if ENABLE_ADDRESSABLES
#if ENABLE_UNITASK && !OVERRIDE_DISABLE_UNITASK
#define USE_UNITASK
#endif
#if USE_UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

namespace MyGameDevTools.SceneLoading
{
    public class SceneManagerAddressable : ISceneManager, ISceneManagerReporter
    {
        public event Action<Scene, Scene> ActiveSceneChanged;
        public event Action<Scene> SceneUnloaded;
        public event Action<Scene> SceneLoaded;

        public bool IsUnloadingScenes => _unloadingScenes.Count > 0;
        public int SceneCount => _loadedScenes.Count;

        readonly List<SceneInstance> _unloadingScenes = new List<SceneInstance>();
        readonly List<SceneInstance> _loadedScenes = new List<SceneInstance>();
        readonly CancellationTokenSource _lifetimeToken = new CancellationTokenSource();

        SceneInstance _activeSceneInstance;

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
            var isSceneLoaded = TryGetInstanceFromScene(scene, out var sceneInstance);
            if (validScene && !isSceneLoaded)
                throw new InvalidOperationException($"[{GetType().Name}] Cannot set active the scene \"{scene.name}\" that has not been loaded through this {GetType().Name}.");

            var previousSceneInstance = _activeSceneInstance;
            _activeSceneInstance = sceneInstance;
            if (validScene)
                UnitySceneManager.SetActiveScene(scene);

            ActiveSceneChanged?.Invoke(previousSceneInstance.Scene, scene);
        }

        public Scene GetActiveScene() => _activeSceneInstance.Scene;

        public Scene GetLastLoadedScene()
        {
            if (SceneCount == 0)
                return default;

            for (int i = SceneCount - 1; i >= 0; i--)
                if (!_unloadingScenes.Contains(_loadedScenes[i]))
                    return _loadedScenes[i].Scene;

            return default;
        }

        public Scene GetLoadedSceneAt(int index) => _loadedScenes[index].Scene;

        public Scene GetLoadedSceneByName(string name)
        {
            foreach (var sceneInstance in _loadedScenes)
                if (sceneInstance.Scene.name == name)
                    return sceneInstance.Scene;
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

        async ValueTask<Scene[]> LoadScenesAsync_Internal(ILoadSceneInfo[] sceneInfos, int setIndexActive, IProgress<float> progress, CancellationToken token = default)
        {
            if (sceneInfos == null || sceneInfos.Length == 0)
                throw new ArgumentException(nameof(sceneInfos), $"[{GetType().Name}] Provided scene group is null or empty.");
            if (setIndexActive >= sceneInfos.Length)
                throw new ArgumentException(nameof(setIndexActive), $"[{GetType().Name}] Provided index to set active is bigger than the provided scene group size.");

            var operationGroup = await GetLoadSceneOperations(sceneInfos, setIndexActive, token);
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

            var loadedScenes = operationGroup.GetResult();

            _loadedScenes.AddRange(loadedScenes);
            foreach (var sceneInstance in loadedScenes)
                SceneLoaded?.Invoke(sceneInstance.Scene);

            if (operationGroup.SetIndexActive >= 0)
                SetActiveScene(loadedScenes[operationGroup.SetIndexActive].Scene);

            return ToSceneArray(loadedScenes);
        }

        async ValueTask<Scene[]> UnloadScenesAsync_Internal(ILoadSceneInfo[] sceneInfos, CancellationToken token)
        {
            if (sceneInfos == null || sceneInfos.Length == 0)
                throw new ArgumentException($"[{GetType().Name}] Provided scene group is null or empty.", nameof(sceneInfos));

            var loadedScenes = GetLastLoadedScenesByInfos(sceneInfos, out var unloadingIndexes);
            if (loadedScenes.Count == 0)
                return Array.Empty<Scene>();

            int unloadingLength = unloadingIndexes.Length;
            var unloadingScenes = new SceneInstance[unloadingLength];
            int i;
            for (i = 0; i < unloadingLength; i++)
                unloadingScenes[i] = loadedScenes[unloadingIndexes[i]];

            for (i = 0; i < unloadingLength; i++)
                loadedScenes.Remove(unloadingScenes[i]);

            var operationList = new List<AsyncOperationHandle<SceneInstance>>(loadedScenes.Count);
            foreach (var sceneInstance in loadedScenes)
            {
                operationList.Add(Addressables.UnloadSceneAsync(sceneInstance));
                _loadedScenes.Remove(sceneInstance);
                _unloadingScenes.Add(sceneInstance);
            }

            var operationGroup = new AsyncOperationHandleGroup(operationList);

            while (!operationGroup.IsDone)
            {
#if USE_UNITASK
                await UniTask.Yield(token);
#else
                await Task.Yield();
#endif
            }

            token.ThrowIfCancellationRequested();

            foreach (var sceneInstance in loadedScenes)
            {
                _unloadingScenes.Remove(sceneInstance);
                SceneUnloaded?.Invoke(sceneInstance.Scene);
                if (_activeSceneInstance.Scene == sceneInstance.Scene)
                    SetActiveScene(GetLastLoadedScene());
            }

            var tasks = new Task[unloadingLength];
            for (i = 0; i < unloadingLength; i++)
                tasks[i] = WaitForSceneUnload(unloadingScenes[i], token).AsTask();

            await Task.WhenAll(tasks);

            foreach (var scene in unloadingScenes)
                loadedScenes.Add(scene);

            return ToSceneArray(loadedScenes);
        }

        async ValueTask<Scene> WaitForSceneUnload(SceneInstance sceneInstance, CancellationToken token)
        {
#if USE_UNITASK
            await UniTask.WaitUntil(() => !_unloadingScenes.Contains(sceneInstance), cancellationToken: token);
#else
            while (_unloadingScenes.Contains(sceneInstance) && !token.IsCancellationRequested)
                await Task.Yield();
#endif

            return token.IsCancellationRequested ? default : sceneInstance.Scene;
        }

        async ValueTask<AsyncOperationHandleGroup> GetLoadSceneOperations(ILoadSceneInfo[] sceneInfos, int setIndexActive, CancellationToken token)
        {
            var sceneLength = sceneInfos.Length;
            var operationList = new List<AsyncOperationHandle<SceneInstance>>(sceneLength);
            for (int i = 0; i < sceneLength; i++)
            {
                var operation = await GetLoadSceneOperation(sceneInfos[i], token);
                if (operation.IsValid())
                    operationList.Add(operation);
                else if (i == setIndexActive)
                    setIndexActive = -1;
            }
            return new AsyncOperationHandleGroup(operationList, setIndexActive);
        }

        async ValueTask<AsyncOperationHandle<SceneInstance>> GetLoadSceneOperation(ILoadSceneInfo sceneInfo, CancellationToken token)
        {
            if (sceneInfo.Reference is AssetReference assetReference)
                return assetReference.LoadSceneAsync(LoadSceneMode.Additive);
            else if (sceneInfo.Reference is string name)
            {
                if (await ValidateAssetReference(name, token))
                    return Addressables.LoadSceneAsync(name, LoadSceneMode.Additive);
                else
                {
                    Debug.LogWarning($"[{GetType().Name}] Scene '{name}' couldn't be loaded because its address found no Addressable Assets.");
                    return default;
                }
            }

            Debug.LogWarning($"[{GetType().Name}] Unexpected {nameof(ILoadSceneInfo.Reference)} type: {sceneInfo.Reference}");
            return default;
        }

        async ValueTask<bool> ValidateAssetReference(object reference, CancellationToken token)
        {
            var operation = Addressables.LoadResourceLocationsAsync(reference);
            while (!operation.IsDone && !token.IsCancellationRequested)
                await Task.Yield();

            token.ThrowIfCancellationRequested();
            return operation.Result.Count > 0;
        }

        IList<SceneInstance> GetLastLoadedScenesByInfos(ILoadSceneInfo[] sceneInfos, out int[] unloadingIndexes)
        {
            var unloadingIndexesList = new List<int>();
            var sceneInfosList = new List<ILoadSceneInfo>(sceneInfos);
            var scenes = new List<SceneInstance>(sceneInfos.Length);

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

            bool tryValidateSceneReference(SceneInstance sceneInstance, out int index)
            {
                foreach (var info in sceneInfosList)
                {
                    if (info.IsReferenceToScene(sceneInstance.Scene))
                    {
                        sceneInfosList.Remove(info);
                        scenes.Add(sceneInstance);
                        index = scenes.Count - 1;
                        return true;
                    }
                }
                index = -1;
                return false;
            }
        }

        Scene[] ToSceneArray(IList<SceneInstance> sceneInstances)
        {
            int sceneCount = sceneInstances.Count;
            var sceneArray = new Scene[sceneCount];
            for (int i = 0; i < sceneCount; i++)
                sceneArray[i] = sceneInstances[i].Scene;

            return sceneArray;
        }

        bool TryGetInstanceFromScene(Scene scene, out SceneInstance sceneInstance)
        {
            foreach (var instance in _loadedScenes)
            {
                sceneInstance = instance;
                if (scene == instance.Scene)
                    return true;
            }

            sceneInstance = default;
            return false;
        }
    }
}
#endif