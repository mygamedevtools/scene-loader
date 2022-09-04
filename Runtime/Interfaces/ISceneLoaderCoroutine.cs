/**
 * ISceneLoaderCoroutine.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 9/4/2022 (en-US)
 */

using UnityEngine;

namespace MyUnityTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize <see cref="Coroutine"/> scene operations.
    /// </summary>
    public interface ISceneLoaderCoroutine : ISceneLoader
    {
        /// <summary>
        /// Triggers a scene transition asynchronously.
        /// It will transition from the current active scene (<see cref="UnityEngine.SceneManagement.SceneManager.GetActiveScene()"/>)
        /// to the target scene (<paramref name="targetSceneInfo"/>), with an optional intermediate loading scene (<paramref name="intermediateSceneInfo"/>).
        /// If the <paramref name="intermediateSceneInfo"/> is not set, the transition will have no intermediate loading scene and will instead simply load the target scene directly.
        /// The complete transition flow is:
        /// <br/><br/>
        /// 1. Load the intermediate scene (if provided).<br/>
        /// 2. Load the target scene.<br/>
        /// 3. Unload the intermediate scene (if provided).<br/>
        /// 4. Unload the previous scene
        /// </summary>
        /// <param name="targetSceneInfo">
        /// The scene that's going to be transitioned to.
        /// Can be the scene's build index (<see cref="LoadSceneInfoIndex"/>) or name (<see cref="LoadSceneInfoName"/>).
        /// </param>
        /// <param name="intermediateSceneInfo">
        /// The scene that's going to be loaded as the transition intermediate (as a loading scene).
        /// Can be the scene's build index (<see cref="LoadSceneInfoIndex"/>) or name (<see cref="LoadSceneInfoName"/>).
        /// If null, the transition will not have an intermediate loading scene.
        /// </param>
        /// <returns>The transition <see cref="Coroutine"/>.</returns>
        Coroutine TransitionToSceneRoutine(ILoadSceneInfo targetSceneInfo, ILoadSceneInfo intermediateSceneInfo = null);

        /// <summary>
        /// Unloads the given scene asynchronously from the current scene stack.
        /// </summary>
        /// <param name="sceneInfo">
        /// Target scene info.
        /// Can be the scene's build index (<see cref="LoadSceneInfoIndex"/>) or name (<see cref="LoadSceneInfoName"/>).
        /// </param>
        /// <returns>The unload <see cref="Coroutine"/>.</returns>
        Coroutine UnloadSceneRoutine(ILoadSceneInfo sceneInfo);

        /// <summary>
        /// Loads a scene additively on top of the current scene stack, optionally marking it as the active scene
        /// (<see cref="UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.Scene)"/>).
        /// </summary>
        /// <param name="sceneInfo">
        /// The scene that's going to be loaded.
        /// Can be the scene's build index (<see cref="LoadSceneInfoIndex"/>) or name (<see cref="LoadSceneInfoName"/>).
        /// </param>
        /// <param name="setActive">Should the loaded scene be marked as active? Equivalent to calling <see cref="UnityEngine.SceneManagement.SceneManager.SetActiveScene(UnityEngine.SceneManagement.Scene)"/>.</param>
        /// <returns>The load awaitable <see cref="Coroutine"/>.</returns>
        Coroutine LoadSceneRoutine(ILoadSceneInfo sceneInfo, bool setActive = false);
    }
}