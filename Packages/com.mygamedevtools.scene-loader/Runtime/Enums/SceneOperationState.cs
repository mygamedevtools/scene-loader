namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Where a <see cref="SceneOperation"/> is in its lifecycle.
    /// <br/><br/>
    /// States only ever move forward, and an operation skips the ones its kind has no use for:
    /// a plain load never reaches <see cref="ScreenIn"/>, and a transition without a loading
    /// screen skips both screen phases.
    /// <br/><br/>
    /// This is what makes the transition observable at all. In v4 there was no way to ask what
    /// phase a transition was in, which is why knowing "the loading screen has finished fading
    /// out" meant reaching into a publicly exposed <c>TaskCompletionSource</c>. Now it is
    /// <see cref="ScreenOut"/>.
    /// </summary>
    public enum SceneOperationState
    {
        /// <summary>
        /// Created, not started.
        /// </summary>
        Pending,
        /// <summary>
        /// Working out what the given references mean. Only reached when a bare string has to be
        /// probed against the Addressables catalog; a build-settings hit never gets here.
        /// </summary>
        Resolving,
        /// <summary>
        /// The loading screen is showing itself, and the transition is waiting for it.
        /// </summary>
        ScreenIn,
        /// <summary>
        /// Unloading — the source scene of a transition, or the targets of an unload.
        /// </summary>
        Unloading,
        /// <summary>
        /// The target scenes are loading. This is the phase <see cref="SceneOperation.Progress"/>
        /// describes.
        /// </summary>
        Loading,
        /// <summary>
        /// Loading finished; linking loaded scenes to their references and setting the active one.
        /// </summary>
        Activating,
        /// <summary>
        /// The loading screen is hiding itself, and the transition is waiting for it. Reaching
        /// this state is the answer to "when is the loading screen completely gone?".
        /// </summary>
        ScreenOut,
        /// <summary>
        /// Finished successfully. <see cref="SceneOperation.Result"/> is populated.
        /// </summary>
        Completed,
        /// <summary>
        /// <see cref="SceneOperation.Cancel"/> was called. Note that the underlying Unity scene
        /// operations cannot be aborted and ran to completion regardless.
        /// </summary>
        Canceled,
        /// <summary>
        /// Finished with an error. <see cref="SceneOperation.Exception"/> is populated.
        /// </summary>
        Faulted,
    }
}
