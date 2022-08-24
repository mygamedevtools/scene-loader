/**
 * ILoadSceneInfo.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize scene information.
    /// Can be created with either the target scene's build index (<see cref="LoadSceneInfoIndex"/>) or the scene's name (<see cref="LoadSceneInfoName"/>).
    /// </summary>
    public interface ILoadSceneInfo
    {
        /// <summary>
        /// Unloads the provided scene asynchronously.
        /// Internally calls <see cref="SceneManager.UnloadSceneAsync(Scene)"/>.
        /// </summary>
        /// <returns>The unload <see cref="AsyncOperation"/>.</returns>
        AsyncOperation UnloadSceneAsync();

        /// <summary>
        /// Loads the provided scene asynchronously.
        /// Internally calls <see cref="SceneManager.LoadSceneAsync(int)"/>.
        /// </summary>
        /// <returns>The load <see cref="AsyncOperation"/>.</returns>
        AsyncOperation LoadSceneAsync();

        /// <summary>
        /// Gets a runtime struct representing the loaded scene.
        /// Used to pass as a parameter when calling <see cref="SceneManager.SetActiveScene(Scene)"/>.
        /// </summary>
        /// <returns>The loaded scene struct, or an invalid scene if it hasn't been loaded.</returns>
        Scene GetScene();
    }
}