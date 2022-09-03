#if ENABLE_ADDRESSABLES
/**
 * AddressableSceneManager.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/26/2022 (en-US)
 */

using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    public class AddressableSceneManager : IAddressableSceneManager
    {
        readonly List<AsyncOperationHandle<SceneInstance>> _loadedScenes = new List<AsyncOperationHandle<SceneInstance>>();

        AsyncOperationHandle<SceneInstance> _activeSceneHandle;

        public void SetActiveSceneHandle(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            if (!_loadedScenes.Contains(sceneHandle))
                throw new System.InvalidOperationException($"Cannot set active the scene \"{sceneHandle.Result.Scene.name}\" that has not been loaded through this AddressableSceneManager.");
            _activeSceneHandle = sceneHandle;
        }
        public void SetActiveScene(SceneInstance scene)
        {
            var handle = GetLoadedSceneHandle(scene);
            if (!handle.IsValid())
                throw new System.InvalidOperationException($"Cannot set active the scene \"{scene.Scene.name}\" that has not been loaded through this AddressableSceneManager.");
            _activeSceneHandle = handle;
        }
        public void SetActiveScene(string sceneName)
        {
            var handle = GetLoadedSceneHandle(sceneName);
            if (!handle.IsValid())
                throw new System.InvalidOperationException($"Cannot set active the scene \"{sceneName}\" that has not been loaded through this AddressableSceneManager.");
            _activeSceneHandle = handle;
        }

        public AsyncOperationHandle<SceneInstance> GetActiveSceneHandle() => _activeSceneHandle;

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(AssetReference sceneReference)
        {
            var operation = sceneReference.LoadSceneAsync(LoadSceneMode.Additive);
            _loadedScenes.Add(operation);
            return operation;
        }
        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(string runtimeKey)
        {
            var operation = Addressables.LoadSceneAsync(runtimeKey, LoadSceneMode.Additive);
            _loadedScenes.Add(operation);
            return operation;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle, bool autoReleaseHandle)
        {
            var operation = Addressables.UnloadSceneAsync(sceneHandle, autoReleaseHandle);
            _loadedScenes.Remove(sceneHandle);
            return operation;
        }
        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle)
        {
            var loadedSceneHandle = GetLoadedSceneHandle(scene);
            var operation = Addressables.UnloadSceneAsync(loadedSceneHandle, autoReleaseHandle);
            _loadedScenes.Remove(loadedSceneHandle);
            return operation;
        }
        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(string sceneName, bool autoReleaseHandle)
        {
            var loadedSceneHandle = GetLoadedSceneHandle(sceneName);
            var operation = Addressables.UnloadSceneAsync(loadedSceneHandle, autoReleaseHandle);
            _loadedScenes.Remove(loadedSceneHandle);
            return operation;
        }

        public AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(SceneInstance sceneInstance)
        {
            foreach (var operationHandle in _loadedScenes)
                if (operationHandle.Result.Scene == sceneInstance.Scene)
                    return operationHandle;
            return default;
        }
        public AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(string sceneName)
        {
            foreach (var operationHandle in _loadedScenes)
                if (operationHandle.Result.Scene.name == sceneName)
                    return operationHandle;
            return default;
        }
    }
}
#endif