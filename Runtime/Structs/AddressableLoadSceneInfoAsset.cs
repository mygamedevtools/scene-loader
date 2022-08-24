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
    /// Struct to manage addressable scene operations with the scene's <see cref="AssetReference"/>.
    /// Implements <see cref="IAddressableLoadSceneInfo"/>.
    /// </summary>
    public readonly struct AddressableLoadSceneInfoAsset : IAddressableLoadSceneInfo
    {
        readonly AssetReference _sceneReference;

        /// <summary>
        /// Creates a new <see cref="IAddressableLoadSceneInfo"/> based on the scene's <see cref="AssetReference"/>.
        /// </summary>
        /// <param name="sceneReference">The scene's addressable <see cref="AssetReference"/>.</param>
        public AddressableLoadSceneInfoAsset(AssetReference sceneReference)
        {
            _sceneReference = sceneReference;
        }

        public AsyncOperationHandle<SceneInstance> LoadSceneAsync() => _sceneReference.LoadSceneAsync(LoadSceneMode.Additive);

        public static implicit operator AddressableLoadSceneInfoAsset(AssetReference sceneReference) => new AddressableLoadSceneInfoAsset(sceneReference);
    }
}
#endif