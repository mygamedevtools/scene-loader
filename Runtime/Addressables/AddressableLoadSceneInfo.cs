/**
 * AddressableSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 7/26/2022 (en-US)
 */

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoader.Addressables
{
    public readonly struct AddressableLoadSceneInfo
    {
        delegate AsyncOperationHandle<SceneInstance> AsyncSceneOperationHandleDelegate();

        readonly AsyncSceneOperationHandleDelegate _loadSceneAsyncDelegate;

        public AddressableLoadSceneInfo(AssetReference sceneReference)
        {
            _loadSceneAsyncDelegate = () => sceneReference.LoadSceneAsync(UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }
        public AddressableLoadSceneInfo(string sceneRuntimeKey)
        {
            _loadSceneAsyncDelegate = () => UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(sceneRuntimeKey, UnityEngine.SceneManagement.LoadSceneMode.Additive);
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync() => _loadSceneAsyncDelegate();
    }
}