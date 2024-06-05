#if ENABLE_ADDRESSABLES
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public struct SceneDataAddressable : ISceneData
    {
        public readonly IAsyncSceneOperation AsyncOperation => _asyncSceneOperation;

        public readonly ILoadSceneInfo LoadSceneInfo => _loadSceneInfo;

        public readonly Scene SceneReference => _sceneReference;

        readonly ILoadSceneInfo _loadSceneInfo;

        AsyncSceneOperationAddressable _asyncSceneOperation;
        Scene _sceneReference;

        public SceneDataAddressable(ILoadSceneInfo loadSceneInfo)
        {
            _loadSceneInfo = loadSceneInfo;
            _asyncSceneOperation = default;
            _sceneReference = default;
        }

        public void SetSceneReferenceManually(Scene scene)
        {
            Debug.LogWarning($"[{GetType().Name}] This type of scene data should not have its scene set manually. Instead, it is expected to set it by calling {nameof(ISceneData.UpdateSceneReference)}.");
            _sceneReference = scene;
        }

        public void UpdateSceneReference()
        {
            if (!AsyncOperation.IsDone)
                throw new System.Exception($"[{GetType().Name}] Cannot update the scene reference before the scene has been loaded.");

            _sceneReference = AsyncOperation.GetResult();
        }

        public IAsyncSceneOperation LoadSceneAsync()
        {
            switch (_loadSceneInfo.Type)
            {
                case LoadSceneInfoType.AssetReference:
                case LoadSceneInfoType.Name:
                    _asyncSceneOperation = new AsyncSceneOperationAddressable(Addressables.LoadSceneAsync(_loadSceneInfo.Reference, LoadSceneMode.Additive));
                    return _asyncSceneOperation;
                default:
                    Debug.LogWarning($"[{GetType().Name}] Unexpected {nameof(ILoadSceneInfo.Reference)} type: {_loadSceneInfo.Reference}");
                    return default;
            }
        }

        public IAsyncSceneOperation UnloadSceneAsync()
        {
            _asyncSceneOperation = new AsyncSceneOperationAddressable(Addressables.UnloadSceneAsync(_asyncSceneOperation.AsyncOperationHandle));
            return _asyncSceneOperation;
        }
    }
}
#endif