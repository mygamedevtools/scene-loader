using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Struct to manage scene operations with the scene's name. Implements <see cref="ILoadSceneInfo"/>.
    /// </summary>
    public readonly struct LoadSceneInfoName : ILoadSceneInfo
    {
        public readonly LoadSceneInfoType Type => LoadSceneInfoType.Name;

        public readonly object Reference => _sceneName;

        readonly string _sceneName;

        /// <summary>
        /// Creates a new <see cref="ILoadSceneInfo"/> based on the scene's name.
        /// The scene must be added to the Build Settings in order to be loaded.
        /// </summary>
        /// <param name="sceneName">`The scene's asset name, as displayed in the Build Settings window.</param>
        public LoadSceneInfoName(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
                throw new ArgumentException("Cannot create a LoadSceneInfoName from an empty string.", nameof(sceneName));
            _sceneName = sceneName;
        }

        public bool CanBeReferenceToScene(Scene scene) => scene.name == _sceneName;

        public override string ToString()
        {
            return $"Scene with name '{_sceneName}'";
        }

        public bool Equals(ILoadSceneInfo other)
        {
            return Type == other.Type && Reference.Equals(other.Reference);
        }
    }
}