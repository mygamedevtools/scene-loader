#if ENABLE_ADDRESSABLES
/**
 * AddressableLoadSceneInfoInstance.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/29/2022 (en-US)
 */

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyGameDevTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Struct to manage addressable scene operations with the scene's <see cref="SceneInstance"/>.
    /// Implements <see cref="IAddressableLoadSceneInfo"/>.
    /// </summary>
    public readonly struct AddressableLoadSceneInfoInstance : IAddressableLoadSceneInfo
    {
        public object Info => _sceneInstance;

        readonly SceneInstance _sceneInstance;

        /// <summary>
        /// Creates a new <see cref="IAddressableLoadSceneInfo"/> based on the scene's <see cref="SceneInstance"/>.
        /// </summary>
        /// <param name="sceneInstance">The scene's <see cref="SceneInstance"/>, that can be retrieved when the scene is loaded through its <see cref="AsyncOperationHandle{TObject}"/>.</param>
        public AddressableLoadSceneInfoInstance(SceneInstance sceneInstance)
        {
            _sceneInstance = sceneInstance;
        }

        public static implicit operator AddressableLoadSceneInfoInstance(SceneInstance scene) => new AddressableLoadSceneInfoInstance(scene);
    }
}
#endif