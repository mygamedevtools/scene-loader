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
        /// <summary>An operation failed, or state the manager depends on is inconsistent.</summary>
        Error = 1,
        /// <summary>Something recoverable, or an API used in a way that will not do what the caller expects.</summary>
        Warning = 2,
        /// <summary>Coarse progress through an operation.</summary>
        Info = 3,
        /// <summary>Step-by-step detail, for diagnosing a specific failure.</summary>
        Verbose = 4,
    }
}
