#if ENABLE_ADDRESSABLES
/**
 * AddressableSceneLoader.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Struct to manage addressable scene operations with the scene's runtime key.
    /// Implements <see cref="IAddressableLoadSceneInfo"/>.
    /// </summary>
    public readonly struct AddressableLoadSceneInfoKey : IAddressableLoadSceneInfo
    {
        readonly string _runtimeKey;

        /// <summary>
        /// Creates a new <see cref="IAddressableLoadSceneInfo"/> based on the scene's runtime key.
        /// </summary>
        /// <param name="sceneRuntimeKey">The scene's addressable runtime key.</param>
        public AddressableLoadSceneInfoKey(string sceneRuntimeKey)
        {
            _runtimeKey = sceneRuntimeKey;
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync() => Addressables.LoadSceneAsync(_runtimeKey, LoadSceneMode.Additive);

        public static implicit operator AddressableLoadSceneInfoKey(string runtimeKey) => new AddressableLoadSceneInfoKey(runtimeKey);
    }
}
#endif