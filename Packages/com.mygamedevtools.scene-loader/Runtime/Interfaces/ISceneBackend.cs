using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// One backend, one contract, every method meaningful on every implementation.
    /// <br/><br/>
    /// This replaces v4's <c>ISceneData</c> and <c>IAsyncSceneOperation</c> pair, which were
    /// half-implemented by design: <c>ISceneData</c> declared both
    /// <c>SetSceneReferenceManually</c> and <c>UpdateSceneReference</c> and each implementation
    /// logged a warning when you called the wrong one; <c>IAsyncSceneOperation.GetResult()</c>
    /// did the same; and <c>HasDirectReferenceToScene</c> existed purely so callers could tell
    /// which half of the interface was real. That is a substitutability violation baked into
    /// the contract, and it forced a <c>LoadSceneInfoType</c> switch at four separate call
    /// sites plus guard-throws in two constructors.
    /// <br/><br/>
    /// Here the branch happens exactly once, when
    /// <see cref="SceneBackendRegistry.GetBackend"/> picks an implementation from an
    /// already-resolved <see cref="SceneRefKind"/>. Everything after that is a virtual call.
    /// <br/><br/>
    /// Two capabilities are deliberately absent. <c>WaitForCompletion()</c> and
    /// <c>GetDownloadStatus()</c> exist only on the Addressables path, and putting either here
    /// is precisely how the half-implemented-interface problem comes back.
    /// </summary>
    public interface ISceneBackend
    {
        /// <summary>
        /// Whether this backend handles a given <b>resolved</b> reference kind.
        /// <see cref="SceneRefKind.Key"/> is never asked about — <see cref="SceneRefResolver"/>
        /// settles it into a concrete kind before backend selection happens.
        /// </summary>
        bool CanHandle(SceneRefKind kind);

        /// <summary>
        /// Starts loading the scene. There is no options parameter: the package always loads
        /// additively, so there is nothing to configure.
        /// </summary>
        SceneBackendHandle Load(SceneRef sceneRef);

        /// <summary>
        /// Starts unloading the scene the handle refers to, returning a handle for the unload
        /// operation.
        /// </summary>
        SceneBackendHandle Unload(SceneBackendHandle handle);

        /// <summary>
        /// Progress of the handle's operation, normalized 0..1.
        /// <br/>
        /// The two backends measure different work — Addressables includes download time, the
        /// standard path does not — so a mixed group's average advances unevenly. That is
        /// documented rather than corrected: rescaling one to match the other would be inventing
        /// a number neither backend reports.
        /// </summary>
        float GetProgress(SceneBackendHandle handle);

        /// <summary>
        /// Whether the handle's operation has finished.
        /// </summary>
        bool IsDone(SceneBackendHandle handle);

        /// <summary>
        /// Names the scene the operation produced, if the backend can.
        /// <br/>
        /// Addressables answers <see langword="true"/> with <c>Result.Scene</c>. The Unity Scene
        /// Manager gives no such thing, so the standard backend answers <see langword="false"/>
        /// and the manager falls back to matching against newly-loaded scenes. One branch, one
        /// place — this single method is what replaced <c>HasDirectReferenceToScene</c>,
        /// <c>UpdateSceneReference</c>, <c>SetSceneReferenceManually</c> and <c>GetResult</c>.
        /// </summary>
        bool TryResolveScene(SceneBackendHandle handle, out Scene scene);
    }
}
