using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Interface to standardize scene loading information.
    /// </summary>
    public interface ILoadSceneInfo : IEquatable<ILoadSceneInfo>
    {
        LoadSceneInfoType Type { get; }

        /// <summary>
        /// Exposes a reference to a scene, such as build index, name or addressable asset address.
        /// </summary>
        object Reference { get; }

        /// <summary>
        /// Returns whether the <see cref="ILoadSceneInfo"/> can be a reference to a scene through the <see cref="Scene"/> struct.
        /// Depending on the type of the <see cref="Reference"/> used to load the scene, it cannot directly match a given <see cref="Scene"/> without its load <see cref="IAsyncSceneOperation"/>.
        /// Therefore, this method should only be used for contexts where the <see cref="IAsyncSceneOperation.HasDirectReferenceToScene"/> is <see cref="false"/>.
        /// </summary>
        bool CanBeReferenceToScene(Scene scene);
    }
}