#if ENABLE_UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize async scene loading operations.
    /// <typeparamref name="TAsyncScene"/> can be a <see cref="Coroutine"/> or an awaitable type that returns <see cref="Scene"/>, such as
    /// <see cref="System.Threading.Tasks.ValueTask{T}"/>.
    /// The <typeparamref name="TAsyncSceneArray"/> can also be a coroutine or an awaitable type that returns a <see cref="Scene"/> array.
    /// </summary>
    public interface ISceneLoaderAsync<TAsyncScene, TAsyncSceneArray> : ISceneLoader
    {
        /// <summary>
        /// Async version of the <see cref="ISceneLoader.TransitionToScenes(ILoadSceneInfo[], int, ILoadSceneInfo)"/>
        /// </summary>
        /// <param name="targetScenes">
        /// A reference to all scenes that will be transitioned to.
        /// </param>
        /// <param name="setIndexActive">
        /// Index of the scene in the <paramref name="targetScenes"/> to be set as the active scene.
        /// </param>
        /// <param name="intermediateSceneReference">
        /// A reference to the scene that's going to be loaded as the transition intermediate (as a loading scene).
        /// If null, the transition will not have an intermediate loading scene.
        /// </param>
        /// <returns>
        /// The transition operation.
        /// </returns>
        TAsyncSceneArray TransitionToScenesAsync(ILoadSceneInfo[] targetScenes, int setIndexActive, ILoadSceneInfo intermediateSceneReference = default);

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
        /// The transition operation.
        /// </returns>
        TAsyncScene TransitionToSceneAsync(ILoadSceneInfo targetSceneReference, ILoadSceneInfo intermediateSceneReference = default);

        /// <summary>
        /// Async version of the <see cref="ISceneLoader.LoadScenes(ILoadSceneInfo[], int)"/>.
        /// </summary>
        /// <param name="sceneInfos">
        /// Reference to all scenes that will be loaded.
        /// </param>
        /// <param name="setIndexActive">
        /// Which of the scenes should be marked as active? Default is -1.
        /// </param>
        /// <param name="progress">
        /// Optional <see cref="IProgress{T}"/> reference to report the scene group loading progress (ranging from 0 to 1).
        /// </param>
        /// <returns>
        /// The loading operation.
        /// </returns>
        TAsyncSceneArray LoadScenesAsync(ILoadSceneInfo[] sceneReferences, int setIndexActive = -1, IProgress<float> progress = null);

        /// <summary>
        /// Async version of the <see cref="ISceneLoader.LoadScene(ILoadSceneInfo, bool)"/>.
        /// </summary>
        /// <param name="sceneReference">
        /// Reference to the scene that's going to be loaded.
        /// </param>
        /// <param name="setActive">
        /// Should the loaded scene be marked as active? Equivalent to calling <see cref="ISceneManager.SetActiveScene(Scene)"/>.
        /// </param>
        /// <param name="progress">
        /// Optional <see cref="IProgress{T}"/> reference to report the scene loading progress (ranging from 0 to 1).
        /// </param>
        /// <returns>
        /// The loading operation.
        /// </returns>
        TAsyncScene LoadSceneAsync(ILoadSceneInfo sceneReference, bool setActive = false, IProgress<float> progress = null);

        /// <summary>
        /// Async version of the <see cref="ISceneLoader.UnloadScenes(ILoadSceneInfo[])"/>
        /// </summary>
        /// <param name="sceneReferences">
        /// Reference to all scenes to be unloaded.
        /// </param>
        /// <returns>
        /// The unloading operation.
        /// </returns>
        TAsyncSceneArray UnloadScenesAsync(ILoadSceneInfo[] sceneReferences);

        /// <summary>
        /// Async version of the <see cref="ISceneLoader.UnloadScene(ILoadSceneInfo)"/>.
        /// </summary>
        /// <param name="sceneReference">
        /// Reference to the desired scene to be unloaded.
        /// </param>
        /// <returns>
        /// The unloading operation.
        /// </returns>
        TAsyncScene UnloadSceneAsync(ILoadSceneInfo sceneReference);
    }

    /// <summary>
    /// Convenience interface to standardize <see cref="Coroutine"/> async scene loading operations.
    /// You can use the <see cref="WaitTask{T}"/> to yield return inside coroutines.
    /// </summary>
    public interface ISceneLoaderCoroutine : ISceneLoaderAsync<WaitTask<Scene>, WaitTask<Scene[]>> { }

    /// <summary>
    /// Convenience interface to standardize <see cref="System.Threading.Tasks.ValueTask{TResult}"/> async scene loading operations.
    /// </summary>
    public interface ISceneLoaderAsync : ISceneLoaderAsync<ValueTask<Scene>, ValueTask<Scene[]>> { }

#if ENABLE_UNITASK
    /// <summary>
    /// Convenience interface to standardize <see cref="UniTask{T}"/> async scene loading operations.
    /// </summary>
    public interface ISceneLoaderUniTask : ISceneLoaderAsync<UniTask<Scene>, UniTask<Scene[]>> { }
#endif
}