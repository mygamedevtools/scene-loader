#if ENABLE_ADDRESSABLES
/**
 * IAddressableSceneManager.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/29/2022 (en-US)
 */

using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyGameDevTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Interface to standardize addressable scene management operations.
    /// </summary>
    public interface IAddressableSceneManager
    {
        int SceneCount { get; }

        /// <summary>
        /// Sets the target scene as the active scene in the current addressable scene stack.
        /// It's required in order to make the scene transition work like the non-addressable system.
        /// Works similarly to the <see cref="UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.Scene)"/>.
        /// </summary>
        /// <param name="sceneHandle">The loaded scene's <see cref="AsyncOperationHandle{TObject}"/>.</param>
        void SetActiveScene(SceneInstance scene);

        /// <summary>
        /// Loads the provided scene asynchronously.
        /// Internally calls <see cref="AssetReference.LoadAssetAsync{TObject}"/>.
        /// </summary>
        /// <param name="sceneReference">The scene's addressable <see cref="AssetReference"/>.</param>
        /// <returns>The load <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        ValueTask<SceneInstance> LoadSceneAsync(IAddressableLoadSceneReference sceneReference, bool setActive = false, IProgress<float> progress = null);

        /// <summary>
        /// Unloads the provided scene asynchronously.
        /// Internally calls <see cref="Addressables.UnloadSceneAsync(AsyncOperationHandle, bool)"/>.
        /// </summary>
        /// <param name="sceneHandle">The loaded scene's <see cref="AsyncOperationHandle{TObject}"/>.</param>
        /// <param name="autoReleaseHandle">Should the <see cref="AsyncOperationHandle{TObject}"/> be released automatically? Defaults to true.</param>
        /// <returns>The unload <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        ValueTask UnloadSceneAsync(IAddressableLoadSceneReference sceneInfo);

        /// <summary>
        /// Gets the active scene in the current addressable scene stack.
        /// A scene can be set active through <see cref="SetActiveSceneHandle(AsyncOperationHandle{SceneInstance})"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="AsyncOperationHandle{TObject}"/> of the current active scene, or an invalid handle if no scene is marked as active.
        /// You can check if the handle is valid through <see cref="AsyncOperationHandle{TObject}.IsValid()"/>.
        /// </returns>
        SceneInstance GetActiveScene();

        SceneInstance GetLoadedSceneAt(int index);

        SceneInstance GetLoadedSceneByName(string sceneName);
    }
}
#endif