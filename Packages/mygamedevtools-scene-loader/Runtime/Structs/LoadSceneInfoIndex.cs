using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Struct to manage scene operations with the scene's build index. Implements <see cref="ILoadSceneInfo"/>.
    /// </summary>
    public readonly struct LoadSceneInfoIndex : ILoadSceneInfo
    {
        public readonly LoadSceneInfoType Type => LoadSceneInfoType.BuildIndex;

        public readonly object Reference => _buildIndex;

        readonly int _buildIndex;

        /// <summary>
        /// Creates a new <see cref="ILoadSceneInfo"/> based on the scene's build index.
        /// The scene must be added to the Build Settings in order to be loaded.
        /// </summary>
        /// <param name="buildIndex">The scene's build index, displayed in the Build Settings window.</param>
        public LoadSceneInfoIndex(int buildIndex)
        {
            if (buildIndex < 0)
                throw new ArgumentException("Cannot create a LoadSceneInfoIndex with a build index lower than 0.", nameof(buildIndex));
            _buildIndex = buildIndex;
        }

        public bool CanBeReferenceToScene(Scene scene) => scene.buildIndex == _buildIndex;

        public override string ToString()
        {
            return $"Scene with index '{_buildIndex}'";
        }

        public bool Equals(ILoadSceneInfo other)
        {
            return Type == other.Type && Reference.Equals(other.Reference);
        }
    }
}