#if ENABLE_ADDRESSABLES
/**
 * AddressableLoadSceneReferenceAsset.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyGameDevTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Struct to manage addressable scene operations with the scene's <see cref="AssetReference"/>.
    /// Implements <see cref="IAddressableLoadSceneReference"/>.
    /// </summary>
    public readonly struct AddressableLoadSceneReferenceAsset : IAddressableLoadSceneReference
    {
        readonly AssetReference _sceneReference;

        /// <summary>
        /// Creates a new <see cref="IAddressableLoadSceneReference"/> based on the scene's <see cref="AssetReference"/>.
        /// </summary>
        /// <param name="sceneReference">The scene's addressable <see cref="AssetReference"/>.</param>
        public AddressableLoadSceneReferenceAsset(AssetReference sceneReference)
        {
            _sceneReference = sceneReference;
        }

        /// <summary>
        /// Loads the provided scene asynchronously.
        /// Internally calls <see cref="AssetReference.LoadAssetAsync{TObject}"/>.
        /// </summary>
        /// <param name="sceneManager">The reference to the <see cref="IAddressableSceneManager"/> that keeps track of the active scenes.</param>
        /// <returns>The load <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(IAddressableSceneManager sceneManager) => sceneManager.LoadSceneAsync(_sceneReference);

        public static implicit operator AddressableLoadSceneReferenceAsset(AssetReference sceneReference) => new AddressableLoadSceneReferenceAsset(sceneReference);
    }
}
#endif