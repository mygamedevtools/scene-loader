namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// What a <see cref="SceneRef"/> points at. A closed union, not an extension point: the
    /// exhaustive switch over it lives in <see cref="SceneRef"/> and in backend selection.
    /// </summary>
    public enum SceneRefKind
    {
        /// <summary>Points at nothing, which is what <c>default(SceneRef)</c> and an omitted loading screen are.</summary>
        None = 0,
        /// <summary>A bare string — name, path or address — left for <see cref="SceneRefResolver"/> to settle.</summary>
        Key,
        /// <summary>A build index. A resolved <see cref="Key"/> becomes this, keeping its original string.</summary>
        BuildIndex,
        /// <summary>An already-loaded scene. Can only be used to unload.</summary>
        Scene,
        /// <summary>An Addressables <c>AssetReference</c>.</summary>
        AssetReference,
        /// <summary>An address stated explicitly: both the precedence override and the fast path.</summary>
        Address,
    }
}
