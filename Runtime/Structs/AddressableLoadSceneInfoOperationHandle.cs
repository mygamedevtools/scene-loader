#if ENABLE_ADDRESSABLES
/**
 * AddressableLoadSceneInfoOperationHandle.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Struct to manage addressable scene operations with the scene's <see cref="AsyncOperationHandle{TObject}"/>.
    /// Implements <see cref="IAddressableLoadSceneReference"/>.
    /// </summary>
    public readonly struct AddressableLoadSceneInfoOperationHandle : IAddressableLoadSceneInfo
    {
        readonly AsyncOperationHandle<SceneInstance> _sceneHandle;

        /// <summary>
        /// Creates a new <see cref="IAddressableLoadSceneInfo"/> based on the scene's <see cref="AsyncOperationHandle{TObject}"/>.
        /// </summary>
        /// <param name="sceneHandle">The scene's <see cref="AsyncOperationHandle{TObject}"/>, used to load it.</param>
        public AddressableLoadSceneInfoOperationHandle(AsyncOperationHandle<SceneInstance> sceneHandle)
        {
            _sceneHandle = sceneHandle;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(IAddressableSceneManager sceneManager, bool autoReleaseHandle) => sceneManager.UnloadSceneAsync(_sceneHandle, autoReleaseHandle);

        public static implicit operator AddressableLoadSceneInfoOperationHandle(AsyncOperationHandle<SceneInstance> sceneHandle) => new AddressableLoadSceneInfoOperationHandle(sceneHandle);
    }
}
#endif