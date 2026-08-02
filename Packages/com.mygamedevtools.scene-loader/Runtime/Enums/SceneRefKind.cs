namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// What a <see cref="SceneRef"/> points at.
    /// <br/>
    /// This is a closed union, not an extension point: the exhaustive switch over it lives in
    /// <see cref="SceneRef"/> and in backend selection, and nowhere else. That is the whole
    /// reason a value type replaced v4's <c>ILoadSceneInfo</c> hierarchy — data shapes are
    /// exactly where a closed union beats an interface.
    /// </summary>
    public enum SceneRefKind
    {
        /// <summary>
        /// Points at nothing. This is what <c>default(SceneRef)</c> is, which is what an
        /// omitted optional loading-scene argument means.
        /// </summary>
        None = 0,
        /// <summary>
        /// A bare string, deliberately <b>unresolved</b>: it may be a scene name, a scene path
        /// or an Addressables address, and which one it is gets decided by
        /// <see cref="SceneRefResolver"/> when the operation starts.
        /// </summary>
        Key,
        /// <summary>
        /// A build index. Unambiguous, so it needs no resolution. A resolved <see cref="Key"/>
        /// that was found in the build settings also becomes this, keeping its original string
        /// so it can still be matched by name or path.
        /// </summary>
        BuildIndex,
        /// <summary>
        /// An already-loaded scene. Can only be used to unload.
        /// </summary>
        Scene,
        /// <summary>
        /// An Addressables <c>AssetReference</c>. Unambiguous, so it needs no resolution.
        /// </summary>
        AssetReference,
        /// <summary>
        /// An Addressables address, stated explicitly. Skips the build-settings probe entirely,
        /// which makes it both the precedence override and the fast path.
        /// </summary>
        Address,
    }
}
