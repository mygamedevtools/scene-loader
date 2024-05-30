#if ENABLE_ADDRESSABLES
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public struct SceneDataAddressable : ISceneData
    {
        public readonly ILoadSceneOperation LoadOperation => _loadSceneOperation;

        public readonly ILoadSceneInfo LoadSceneInfo => _loadSceneInfo;

        public readonly Scene LoadedScene => _loadedScene;

        readonly ILoadSceneInfo _loadSceneInfo;

        ILoadSceneOperation _loadSceneOperation;
        Scene _loadedScene;

        public SceneDataAddressable(ILoadSceneInfo loadSceneInfo)
        {
            _loadSceneInfo = loadSceneInfo;
            _loadSceneOperation = default;
            _loadedScene = default;
        }

        public void AssignLoadedScene(Scene scene)
        {
            _loadedScene = scene;
        }

        public ILoadSceneOperation LoadSceneAsync()
        {
            switch (_loadSceneInfo.Type)
            {
                case LoadSceneInfoType.AssetReference:
                case LoadSceneInfoType.Name:
                    _loadSceneOperation = new LoadSceneOperationAddressable(Addressables.LoadSceneAsync(_loadSceneInfo.Reference, LoadSceneMode.Additive));
                    return _loadSceneOperation;
                default:
                    Debug.LogWarning($"[{GetType().Name}] Unexpected {nameof(ILoadSceneInfo.Reference)} type: {_loadSceneInfo.Reference}");
                    return default;
            }
        }
    }
}
#endif