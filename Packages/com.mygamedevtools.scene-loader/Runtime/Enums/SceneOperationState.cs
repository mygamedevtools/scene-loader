namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Where a <see cref="SceneOperation"/> is in its lifecycle. An operation skips the phases
    /// its kind has no use for — a plain load never reaches <see cref="ScreenIn"/>.
    /// </summary>
    public enum SceneOperationState
    {
        /// <summary>Created, not started.</summary>
        Pending,
        /// <summary>Probing a bare string against the Addressables catalog. A build-settings hit skips this.</summary>
        Resolving,
        /// <summary>Waiting for the loading screen to finish showing.</summary>
        ScreenIn,
        /// <summary>Unloading the source scene of a transition, or the targets of an unload.</summary>
        Unloading,
        /// <summary>The target scenes are loading. What <see cref="SceneOperation.Progress"/> describes.</summary>
        Loading,
        /// <summary>Linking loaded scenes to their references and setting the active one.</summary>
        Activating,
        /// <summary>Waiting for the loading screen to finish hiding — "when is it completely gone?".</summary>
        ScreenOut,
        /// <summary>Finished successfully; <see cref="SceneOperation.Result"/> is populated.</summary>
        Completed,
        /// <summary><see cref="SceneOperation.Cancel"/> was called. The Unity operations still ran to completion.</summary>
        Canceled,
        /// <summary>Finished with an error; <see cref="SceneOperation.Exception"/> is populated.</summary>
        Faulted,
    }
}
