/**
 * LoadSceneInfoName.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyUnityTools.SceneLoading
{
    /// <summary>
    /// Struct to manage scene operations with the scene's name. Implements <see cref="ILoadSceneInfo"/>.
    /// </summary>
    public readonly struct LoadSceneInfoName : ILoadSceneInfo
    {
        readonly string _sceneName;

        /// <summary>
        /// Creates a new <see cref="ILoadSceneInfo"/> based on the scene's name.
        /// The scene must be added to the Build Settings in order to be loaded.
        /// </summary>
        /// <param name="sceneName">`The scene's asset name, as displayed in the Build Settings window.</param>
        public LoadSceneInfoName(string sceneName)
        {
            _sceneName = sceneName;
        }

        public AsyncOperation UnloadSceneAsync() => SceneManager.UnloadSceneAsync(_sceneName);

        public AsyncOperation LoadSceneAsync() => SceneManager.LoadSceneAsync(_sceneName, LoadSceneMode.Additive);

        public Scene GetScene() => SceneManager.GetSceneByName(_sceneName);

        public static implicit operator LoadSceneInfoName(string sceneName) => new LoadSceneInfoName(sceneName);
    }
}