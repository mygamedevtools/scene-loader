using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize scene loading information.
    /// </summary>
    public interface ILoadSceneInfo
    {
        LoadSceneInfoType Type { get; }

        /// <summary>
        /// Exposes a reference to a scene, such as build index, name or addressable asset address.
        /// </summary>
        object Reference { get; }

        /// <summary>
        /// You should be able to use the <see cref="IsReferenceToScene(Scene)"/> to check whether this instance of <see cref="ILoadSceneInfo"/> references a loaded scene.
        /// </summary>
        /// <param name="scene">Loaded scene to be used on the reference validation.</param>
        /// <returns>Whether this <see cref="ILoadSceneInfo"/> references the loaded <paramref name="scene"/>.</returns>
        bool IsReferenceToScene(Scene scene);
    }
}