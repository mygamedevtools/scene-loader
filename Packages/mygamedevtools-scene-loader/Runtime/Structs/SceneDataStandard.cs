using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    public struct SceneDataStandard : ISceneData
    {
        public readonly IAsyncSceneOperation AsyncOperation => _asyncSceneOperation;

        public readonly ILoadSceneInfo LoadSceneInfo => _loadSceneInfo;

        public readonly Scene SceneReference => _sceneReference;

        readonly ILoadSceneInfo _loadSceneInfo;

        IAsyncSceneOperation _asyncSceneOperation;
        Scene _sceneReference;

        public SceneDataStandard(ILoadSceneInfo loadSceneInfo)
        {
            _loadSceneInfo = loadSceneInfo;
            _asyncSceneOperation = default;
            _sceneReference = default;
        }

        public void SetSceneReferenceManually(Scene scene)
        {
            if (!AsyncOperation.IsDone)
                throw new System.Exception($"[{GetType().Name}] Cannot update the scene reference before the scene has been loaded.");

            _sceneReference = scene;
        }

        public void UpdateSceneReference()
        {
            Debug.LogWarning($"[{GetType().Name}] This type of scene data should not have its scene set automatically. Instead, it is expected to set it by calling {nameof(ISceneData.SetSceneReferenceManually)}.");
        }

        public IAsyncSceneOperation LoadSceneAsync()
        {
            switch (_loadSceneInfo.Type)
            {
                case LoadSceneInfoType.BuildIndex:
                    _asyncSceneOperation = new AsyncSceneOperationStandard(UnityEngine.SceneManagement.SceneManager.LoadSceneAsync((int)_loadSceneInfo.Reference, LoadSceneMode.Additive));
                    break;
                case LoadSceneInfoType.Name:
                    _asyncSceneOperation = new AsyncSceneOperationStandard(UnityEngine.SceneManagement.SceneManager.LoadSceneAsync((string)_loadSceneInfo.Reference, LoadSceneMode.Additive));
                    break;
                default:
                    Debug.LogWarning($"[{GetType().Name}] Unexpected {nameof(ILoadSceneInfo.Reference)} type: {_loadSceneInfo.Reference}");
                    return default;
            }
            return _asyncSceneOperation;
        }

        public IAsyncSceneOperation UnloadSceneAsync()
        {
            _asyncSceneOperation = new AsyncSceneOperationStandard(UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(_sceneReference));
            return _asyncSceneOperation;
        }
    }
}