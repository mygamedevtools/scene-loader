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
    /// Interface to standardize addressable scene operations.
    /// Can be created with either the target scene's <see cref="AssetReference"/> (<see cref="LoadSceneInfoIndex"/>) or the scene's runtime key (<see cref="LoadSceneInfoName"/>).
    /// </summary>
    public interface IAddressableLoadSceneInfo
    {
        /// <summary>
        /// Loads the provided scene asynchronously.
        /// Internally calls <see cref="Addressables.LoadSceneAsync(object, LoadSceneMode, bool, int)"/>.
        /// </summary>
        /// <returns>The load <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        AsyncOperationHandle<SceneInstance> LoadSceneAsync();
    }
}
#endif