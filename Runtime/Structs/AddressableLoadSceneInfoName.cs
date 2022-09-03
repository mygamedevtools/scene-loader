#if ENABLE_ADDRESSABLES
/**
 * AddressableLoadSceneInfoName.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/29/2022 (en-US)
 */

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Struct to manage addressable scene operations with the scene's name.
    /// Implements <see cref="IAddressableLoadSceneInfo"/>.
    /// </summary>
    public readonly struct AddressableLoadSceneInfoName : IAddressableLoadSceneInfo
    {
        readonly string _sceneName;

        /// <summary>
        /// Creates a new <see cref="IAddressableLoadSceneInfo"/> based on the scene's name.
        /// </summary>
        /// <param name="sceneName">The scene's addressable name.</param>
        public AddressableLoadSceneInfoName(string sceneName)
        {
            _sceneName = sceneName;
        }

        public AsyncOperationHandle<SceneInstance> UnloadSceneAsync(IAddressableSceneManager sceneManager, bool autoReleaseHandle) => sceneManager.UnloadSceneAsync(_sceneName, autoReleaseHandle);

        public static implicit operator AddressableLoadSceneInfoName(string sceneName) => new AddressableLoadSceneInfoName(sceneName);
    }
}
#endif