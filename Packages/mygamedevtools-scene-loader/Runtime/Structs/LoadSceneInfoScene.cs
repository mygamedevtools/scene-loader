using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Struct to manage scene operations with the scene's own reference. Can only be used to unload the referenced scene. Implements <see cref="ILoadSceneInfo"/>.
    /// </summary>
    public readonly struct LoadSceneInfoScene : ILoadSceneInfo
    {
        public readonly LoadSceneInfoType Type => LoadSceneInfoType.SceneHandle;

        public readonly object Reference => _scene;

        readonly Scene _scene;

        /// <summary>
        /// Creates a new <see cref="ILoadSceneInfo"/> based on the scene's struct.
        /// This <see cref="ILoadSceneInfo"/> can only be used to unload the referenced scene.
        /// </summary>
        public LoadSceneInfoScene(Scene scene)
        {
            if (!scene.IsValid())
                throw new ArgumentException("Cannot create a LoadSceneInfoScene from an invalid scene.", nameof(scene));
            _scene = scene;
        }

        public bool CanBeReferenceToScene(Scene scene) => scene == _scene;

        public override string ToString()
        {
            return $"Scene '{_scene.name}' ({_scene.handle})";
        }

        public bool Equals(ILoadSceneInfo other)
        {
            return Type == other.Type && Reference.Equals(other.Reference);
        }
    }
}