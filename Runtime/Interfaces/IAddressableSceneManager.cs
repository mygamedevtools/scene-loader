#if ENABLE_ADDRESSABLES
/**
 * IAddressableSceneManager.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/29/2022 (en-US)
 */

using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Interface to standardize addressable scene management operations.
    /// </summary>
    public interface IAddressableSceneManager
    {
        /// <summary>
        /// Sets the target scene as the active scene in the current addressable scene stack.
        /// It's required in order to make the scene transition work like the non-addressable system.
        /// Works similarly to the <see cref="UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.Scene)"/>.
        /// </summary>
        /// <param name="sceneHandle">The loaded scene's <see cref="AsyncOperationHandle{TObject}"/>.</param>
        void SetActiveSceneHandle(AsyncOperationHandle<SceneInstance> sceneHandle);

        /// <summary>
        /// Gets the active scene in the current addressable scene stack.
        /// A scene can be set active through <see cref="SetActiveSceneHandle(AsyncOperationHandle{SceneInstance})"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="AsyncOperationHandle{TObject}"/> of the current active scene, or an invalid handle if no scene is marked as active.
        /// You can check if the handle is valid through <see cref="AsyncOperationHandle{TObject}.IsValid()"/>.
        /// </returns>
        AsyncOperationHandle<SceneInstance> GetActiveSceneHandle();

        /// <summary>
        /// Loads the provided scene asynchronously.
        /// Internally calls <see cref="AssetReference.LoadAssetAsync{TObject}"/>.
        /// </summary>
        /// <param name="sceneReference">The scene's addressable <see cref="AssetReference"/>.</param>
        /// <returns>The load <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        AsyncOperationHandle<SceneInstance> LoadSceneAsync(AssetReference sceneReference);
        /// <summary>
        /// Loads the provided scene asynchronously.
        /// Internally calls <see cref="AssetReference.LoadAssetAsync{TObject}"/>.
        /// </summary>
        /// <param name="runtimeKey">The scene's addressable runtime key.</param>
        /// <returns>The load <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        AsyncOperationHandle<SceneInstance> LoadSceneAsync(string runtimeKey);

        /// <summary>
        /// Unloads the provided scene asynchronously.
        /// Internally calls <see cref="Addressables.UnloadSceneAsync(AsyncOperationHandle, bool)"/>.
        /// </summary>
        /// <param name="sceneHandle">The loaded scene's <see cref="AsyncOperationHandle{TObject}"/>.</param>
        /// <param name="autoReleaseHandle">Should the <see cref="AsyncOperationHandle{TObject}"/> be released automatically? Defaults to true.</param>
        /// <returns>The unload <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(AsyncOperationHandle<SceneInstance> sceneHandle, bool autoReleaseHandle = true);
        /// <summary>
        /// Unloads the provided scene asynchronously.
        /// Internally calls <see cref="Addressables.UnloadSceneAsync(AsyncOperationHandle, bool)"/>.
        /// </summary>
        /// <param name="scene">The scene's <see cref="SceneInstance"/>.</param>
        /// <param name="autoReleaseHandle">Should the <see cref="AsyncOperationHandle{TObject}"/> be released automatically? Defaults to true.</param>
        /// <returns>The unload <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle = true);
        /// <summary>
        /// Unloads the provided scene asynchronously.
        /// Internally calls <see cref="Addressables.UnloadSceneAsync(AsyncOperationHandle, bool)"/>.
        /// </summary>
        /// <param name="sceneName">The scene's name.</param>
        /// <param name="autoReleaseHandle">Should the <see cref="AsyncOperationHandle{TObject}"/> be released automatically? Defaults to true.</param>
        /// <returns>The unload <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        AsyncOperationHandle<SceneInstance> UnloadSceneAsync(string sceneName, bool autoReleaseHandle = true);

        /// <summary>
        /// Gets the <see cref="AsyncOperationHandle{TObject}"/> of the loaded scene by its <see cref="SceneInstance"/>.
        /// </summary>
        /// <param name="sceneInstance">The scene's <see cref="SceneInstance"/> value.</param>
        /// <returns>The loaded scene's <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(SceneInstance sceneInstance);
        /// <summary>
        /// Gets the <see cref="AsyncOperationHandle{TObject}"/> of the loaded scene by its name.
        /// </summary>
        /// <param name="sceneName">The scene's name.</param>
        /// <returns>The loaded scene's <see cref="AsyncOperationHandle{TObject}"/>.</returns>
        AsyncOperationHandle<SceneInstance> GetLoadedSceneHandle(string sceneName);
    }
}
#endif