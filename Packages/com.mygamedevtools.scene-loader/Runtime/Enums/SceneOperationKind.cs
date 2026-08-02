namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Which of the four operations a <see cref="SceneOperation"/> is running.
    /// </summary>
    public enum SceneOperationKind
    {
        /// <summary>
        /// Loading one or more scenes additively.
        /// </summary>
        Load,
        /// <summary>
        /// Unloading one or more scenes.
        /// </summary>
        Unload,
        /// <summary>
        /// Moving from the active scene to another group, optionally through a loading screen.
        /// </summary>
        Transition,
        /// <summary>
        /// A <see cref="Transition"/> whose target is the scene it started from.
        /// </summary>
        Reload,
        /// <summary>
        /// A combinator over other operations — <see cref="SceneOperation.WhenAll"/> or
        /// <see cref="SceneOperation.WhenAny"/> — rather than an operation of its own.
        /// </summary>
        Composite,
    }
}
