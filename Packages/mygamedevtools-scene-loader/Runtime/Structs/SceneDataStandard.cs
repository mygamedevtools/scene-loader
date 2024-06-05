using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public struct SceneDataStandard : ISceneData
    {
        public readonly ILoadSceneOperation LoadOperation => _loadSceneOperation;

        public readonly ILoadSceneInfo LoadSceneInfo => _loadSceneInfo;

        public readonly Scene LoadedScene => _loadedScene;

        readonly ILoadSceneInfo _loadSceneInfo;

        ILoadSceneOperation _loadSceneOperation;
        Scene _loadedScene;

        public SceneDataStandard(ILoadSceneInfo loadSceneInfo)
        {
            _loadSceneInfo = loadSceneInfo;
            _loadSceneOperation = default;
            _loadedScene = default;
        }

        public void SetSceneReferenceManually(Scene scene)
        {
            if (!LoadOperation.IsDone)
                throw new System.Exception($"[{GetType().Name}] Cannot update the scene reference before the scene has been loaded.");

            _loadedScene = scene;
        }

        public void UpdateSceneReference()
        {
            Debug.LogWarning($"[{GetType().Name}] This type of scene data should not have its scene set automatically. Instead, it is expected to set it by calling {nameof(ISceneData.SetSceneReferenceManually)}.");
        }

        public ILoadSceneOperation LoadSceneAsync()
        {
            switch (_loadSceneInfo.Type)
            {
                case LoadSceneInfoType.BuildIndex:
                    _loadSceneOperation = new LoadSceneOperationStandard(UnityEngine.SceneManagement.SceneManager.LoadSceneAsync((int)_loadSceneInfo.Reference, LoadSceneMode.Additive));
                    break;
                case LoadSceneInfoType.Name:
                    _loadSceneOperation = new LoadSceneOperationStandard(UnityEngine.SceneManagement.SceneManager.LoadSceneAsync((string)_loadSceneInfo.Reference, LoadSceneMode.Additive));
                    break;
                default:
                    Debug.LogWarning($"[{GetType().Name}] Unexpected {nameof(ILoadSceneInfo.Reference)} type: {_loadSceneInfo.Reference}");
                    return default;
            }
            return _loadSceneOperation;
        }
    }
}