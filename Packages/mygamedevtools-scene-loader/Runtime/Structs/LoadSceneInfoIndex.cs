/**
 * LoadSceneInfoIndex.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 8/24/2022 (en-US)
 */

using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Struct to manage scene operations with the scene's build index. Implements <see cref="ILoadSceneInfo"/>.
    /// </summary>
    public readonly struct LoadSceneInfoIndex : ILoadSceneInfo
    {
        public object Reference => _buildIndex;

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

        public bool IsReferenceToScene(Scene scene) => scene.buildIndex == _buildIndex;
    }
}