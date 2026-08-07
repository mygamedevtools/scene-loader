namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Severity threshold for <see cref="SceneManagerLog"/>. A level enables itself and
    /// everything more severe: <see cref="Warning"/> emits warnings and errors, and drops the rest.
    /// </summary>
    public enum SceneLogLevel
    {
        /// <summary>Emits nothing.</summary>
        Off = 0,
        /// <summary>Link failures, faulted operations, inferred standard-path load failures.</summary>
        Error = 1,
        /// <summary>Double-matched keys, overrun transition gates, unloads of unmanaged scenes.</summary>
        Warning = 2,
        /// <summary>Operation start and completion.</summary>
        Info = 3,
        /// <summary>Every state transition, resolution and scene link. Where the linking layer narrates itself.</summary>
        Verbose = 4,
    }
}
