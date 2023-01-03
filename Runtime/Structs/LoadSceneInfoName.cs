/**
 * LoadSceneInfoName.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Struct to manage scene operations with the scene's name. Implements <see cref="ILoadSceneInfo"/>.
    /// </summary>
    public readonly struct LoadSceneInfoName : ILoadSceneInfo
    {
        public object Reference => _sceneName;

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
    }
}