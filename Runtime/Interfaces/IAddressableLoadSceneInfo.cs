#if ENABLE_ADDRESSABLES
/**
 * IAddressableUnloadSceneInfo.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Interface to standardize addressable scene unload info.
    /// Can be created with either the target scene's <see cref="AsyncOperationHandle{TObject}"/> (<see cref="AddressableLoadSceneInfoOperationHandle"/>),
    /// the <see cref="SceneInstance"/> (<see cref="AddressableLoadSceneInfoInstance"/>) or its name (<see cref="AddressableLoadSceneInfoName"/>).
    /// </summary>
    public interface IAddressableLoadSceneInfo
    {
        /// <summary>
        /// Unloads the provided scene asynchronously.
        /// Internally calls <see cref="UnityEngine.AddressableAssets.Addressables.UnloadSceneAsync(AsyncOperationHandle, bool)"/>.
        /// </summary>
        /// <param name="sceneManager">The reference to the <see cref="IAddressableSceneManager"/> that keeps track of the active scenes.</param>
        /// <param name="autoReleaseHandle">Should the <see cref="AsyncOperationHandle{TObject}"/> be released automatically?</param>
        /// <returns>The unload <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(IAddressableSceneManager sceneManager, bool autoReleaseHandle = true);
    }
}
#endif