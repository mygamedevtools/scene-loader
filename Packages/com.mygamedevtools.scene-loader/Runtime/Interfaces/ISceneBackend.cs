using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// One backend, one contract, every method meaningful on every implementation. The branch
    /// happens exactly once, when <see cref="SceneBackendRegistry.GetBackend"/> picks an
    /// implementation from an already-resolved <see cref="SceneRefKind"/>.
    /// <br/><br/>
    /// <c>WaitForCompletion()</c> and <c>GetDownloadStatus()</c> are deliberately absent: they
    /// exist only on the Addressables path, and adding either is how v4's
    /// half-implemented-interface problem comes back.
    /// </summary>
    public interface ISceneBackend
    {
        /// <summary>
        /// Whether this backend handles a <b>resolved</b> kind. <see cref="SceneRefKind.Key"/> is
        /// never asked about — the resolver settles it before selection happens.
        /// </summary>
        bool CanHandle(SceneRefKind kind);

        /// <summary>Starts loading the scene. No options: the package always loads additively.</summary>
        SceneBackendHandle Load(SceneRef sceneRef);

        /// <summary>Starts unloading the handle's scene, returning a handle for the unload.</summary>
        SceneBackendHandle Unload(SceneBackendHandle handle);

        /// <summary>
        /// Progress, normalized 0..1. The two backends measure different work — Addressables
        /// includes download time — so a mixed group advances unevenly. Documented, not
        /// corrected: rescaling would invent a number neither backend reports.
        /// </summary>
        float GetProgress(SceneBackendHandle handle);

        /// <summary>Whether the handle's operation has finished.</summary>
        bool IsDone(SceneBackendHandle handle);

        /// <summary>
        /// Names the scene the operation produced, if the backend can. Addressables answers
        /// <see langword="true"/>; the Unity Scene Manager gives no such thing, so the standard
        /// backend answers <see langword="false"/> and the manager matches newly-loaded scenes
        /// instead. One branch, one place.
        /// </summary>
        bool TryResolveScene(SceneBackendHandle handle, out Scene scene);
    }
}
