#if ENABLE_ADDRESSABLES
/**
 * AddressableLoadSceneReferenceKey.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Struct to manage addressable scene operations with the scene's runtime key.
    /// Implements <see cref="IAddressableLoadSceneReference"/>.
    /// </summary>
    public readonly struct AddressableLoadSceneReferenceKey : IAddressableLoadSceneReference
    {
        readonly string _runtimeKey;

        /// <summary>
        /// Creates a new <see cref="IAddressableLoadSceneReference"/> based on the scene's runtime key.
        /// </summary>
        /// <param name="sceneRuntimeKey">The scene's addressable runtime key.</param>
        public AddressableLoadSceneReferenceKey(string sceneRuntimeKey)
        {
            _runtimeKey = sceneRuntimeKey;
        }

        /// <summary>
        /// Loads the provided scene asynchronously.
        /// Internally calls <see cref="UnityEngine.AddressableAssets.Addressables.LoadSceneAsync(object, UnityEngine.SceneManagement.LoadSceneMode, bool, int)"/>.
        /// </summary>
        /// <param name="sceneManager">The reference to the <see cref="IAddressableSceneManager"/> that keeps track of the active scenes.</param>
        /// <returns>The load <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        public AsyncOperationHandle<SceneInstance> LoadSceneAsync(IAddressableSceneManager sceneManager) => sceneManager.LoadSceneAsync(_runtimeKey);

        public static implicit operator AddressableLoadSceneReferenceKey(string runtimeKey) => new AddressableLoadSceneReferenceKey(runtimeKey);
    }
}
#endif