#if ENABLE_ADDRESSABLES
/**
 * IAddressableSceneLoaderCoroutine.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 9/4/2022 (en-US)
 */

using UnityEngine;

namespace MyUnityTools.SceneLoading.AddressablesSupport
{
    /// <summary>
    /// Interface to standardize <see cref="Coroutine"/> addressable scene operations.
    /// </summary>
    public interface IAddressableSceneLoaderCoroutine : IAddressableSceneLoader
    {
        /// <summary>
        /// Triggers a scene transition asynchronously.
        /// It will transition from the current active scene (<see cref="IAddressableSceneManager.GetActiveSceneHandle"/>)
        /// to the target scene (<paramref name="targetSceneReference"/>), with an optional intermediate loading scene (<paramref name="intermediateSceneReference"/>).
        /// If the <paramref name="intermediateSceneReference"/> is not set, the transition will have no intermediate loading scene and will instead simply load the target scene directly.
        /// The complete transition flow is:
        /// <br/><br/>
        /// 1. Load the intermediate scene (if provided).<br/>
        /// 2. Load the target scene.<br/>
        /// 3. Unload the intermediate scene (if provided).<br/>
        /// 4. Unload the previous scene
        /// </summary>
        /// <param name="targetSceneReference">
        /// The scene that's going to be transitioned to.
        /// Can be the scene's addressable <see cref="UnityEngine.AddressableAssets.AssetReference"/> (<see cref="AddressableLoadSceneReferenceAsset"/>)
        /// or runtime key (<see cref="AddressableLoadSceneReferenceKey"/>).
        /// </param>
        /// <param name="intermediateSceneReference">
        /// The scene that's going to be loaded as the transition intermediate (as a loading scene).
        /// Can be the scene's addressable <see cref="UnityEngine.AddressableAssets.AssetReference"/> (<see cref="AddressableLoadSceneReferenceAsset"/>)
        /// or runtime key (<see cref="AddressableLoadSceneReferenceKey"/>).
        /// If null, the transition will not have an intermediate loading scene.
        /// </param>
        /// <returns>The transition <see cref="Coroutine"/>.</returns>
        Coroutine TransitionToSceneRoutine(IAddressableLoadSceneReference targetSceneReference, IAddressableLoadSceneReference intermediateSceneReference = null);

        /// <summary>
        /// Loads a scene additively asynchronously on top of the current scene stack, optionally marking it as the active scene
        /// (<see cref="IAddressableSceneManager.SetActiveSceneHandle(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{UnityEngine.ResourceManagement.ResourceProviders.SceneInstance})"/>).
        /// </summary>
        /// <param name="sceneInfo">
        /// The scene that's going to be loaded.
        /// Can be the scene's addressable <see cref="UnityEngine.AddressableAssets.AssetReference"/> (<see cref="AddressableLoadSceneReferenceAsset"/>)
        /// or runtime key (<see cref="AddressableLoadSceneReferenceKey"/>).
        /// </param>
        /// <param name="setActive">
        /// Should the loaded scene be marked as active?
        /// Equivalent to calling <see cref="IAddressableSceneManager.SetActiveSceneHandle(UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{UnityEngine.ResourceManagement.ResourceProviders.SceneInstance})"/>.
        /// </param>
        /// <returns>The load <see cref="Coroutine"/>.</returns>
        Coroutine LoadSceneRoutine(IAddressableLoadSceneReference sceneReference, bool setActive = false);

        /// <summary>
        /// Unloads the given scene asynchronously from the current scene stack.
        /// </summary>
        /// <param name="sceneInfo">
        /// Target scene info.
        /// Can be the scene's addressable <see cref="UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle{TObject}"/> (<see cref="AddressableLoadSceneInfoOperationHandle"/>),
        /// its <see cref="UnityEngine.ResourceManagement.ResourceProviders.SceneInstance"/> (<see cref="AddressableLoadSceneInfoInstance"/>),
        /// or its name (<see cref="AddressableLoadSceneInfoName"/>).
        /// </param>
        /// <returns>The unload awaitable <see cref="Coroutine"/>.</returns>
        Coroutine UnloadSceneRoutine(IAddressableLoadSceneInfo sceneInfo);
    }
}
#endif