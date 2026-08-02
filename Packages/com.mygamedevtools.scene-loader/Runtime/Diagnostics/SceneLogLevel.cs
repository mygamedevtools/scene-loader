namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Severity threshold for <see cref="SceneManagerLog"/>.
    /// <br/>
    /// The values are ordered by verbosity, so a level enables itself and everything above it:
    /// <see cref="Warning"/> emits warnings and errors but drops info and verbose.
    /// </summary>
    public enum SceneLogLevel
    {
        /// <summary>
        /// Emits nothing at all.
        /// </summary>
        Off = 0,
        /// <summary>
        /// Scene link failures, faulted operations, and load failures the standard scene
        /// backend can only infer.
        /// </summary>
        Error = 1,
        /// <summary>
        /// Recoverable oddities worth surfacing: a key that matches both build settings and
        /// Addressables, a transition gate that overran its timeout, an unload requested for
        /// a scene this manager does not own.
        /// </summary>
        Warning = 2,
        /// <summary>
        /// Operation start and completion, with kind, targets and duration.
        /// </summary>
        Info = 3,
        /// <summary>
        /// The full narration: every operation state transition, every reference resolution
        /// and cache hit, and which scene got linked to which reference. This is the level
        /// that makes the linking layer — historically the buggiest part of the package —
        /// diagnosable rather than opaque.
        /// </summary>
        Verbose = 4,
    }
}
