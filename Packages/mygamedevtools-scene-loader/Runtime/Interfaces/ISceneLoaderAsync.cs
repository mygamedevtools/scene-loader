/**
 * ISceneLoaderAsync.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/15/2022 (en-US)
 */

using System;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize async scene loading operations.
    /// <typeparamref name="TAsync"/> can be a <see cref="UnityEngine.Coroutine"/> or an awaitable type that returns <see cref="UnityEngine.SceneManagement.Scene"/>, such as
    /// <see cref="System.Threading.Tasks.ValueTask{T}"/>.
    /// </summary>
    public interface ISceneLoaderAsync<TAsync> : ISceneLoader
    {
        /// <summary>
        /// Async version of the <see cref="ISceneLoader.TransitionToScene(ILoadSceneInfo, ILoadSceneInfo)"/>.
        /// </summary>
        /// <param name="targetSceneReference">
        /// A reference to the scene that's going to be transitioned to.
        /// </param>
        /// <param name="intermediateSceneReference">
        /// A reference to the scene that's going to be loaded as the transition intermediate (as a loading scene).
        /// If null, the transition will not have an intermediate loading scene.
        /// </param>
        /// <returns>
        /// The loading operation.
        /// </returns>
        TAsync TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = default);

        /// <summary>
        /// Async version of the <see cref="ISceneLoader.LoadScene(ILoadSceneInfo, bool)"/>.
        /// </summary>
        /// <param name="sceneReference">
        /// Reference to the scene that's going to be loaded.
        /// </param>
        /// <param name="setActive">
        /// Should the loaded scene be marked as active? Equivalent to calling <see cref="ISceneManager.SetActiveScene(UnityEngine.SceneManagement.Scene)"/>.
        /// </param>
        /// <param name="progress">
        /// Optional <see cref="IProgress{T}"/> reference to report the scene loading progress (ranging from 0 to 1).
        /// </param>
        /// <returns>
        /// The loading operation.
        /// </returns>
        TAsync LoadSceneAsync(ILoadSceneInfo sceneReference, bool setActive = false, IProgress<float> progress = null);

        /// <summary>
        /// Async version of the <see cref="ISceneLoader.UnloadScene(ILoadSceneInfo)"/>.
        /// </summary>
        /// <param name="sceneReference">
        /// Reference to the desired scene to be unloaded.
        /// </param>
        /// <returns>
        /// The unloading operation.
        /// </returns>
        TAsync UnloadSceneAsync(ILoadSceneInfo sceneReference);
    }
}