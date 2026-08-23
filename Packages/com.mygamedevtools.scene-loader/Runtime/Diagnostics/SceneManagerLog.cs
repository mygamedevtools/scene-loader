using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// One configurable, routable sink for every diagnostic the scene manager emits.
    /// Filtering is entirely runtime, through <see cref="Level"/>.
    /// </summary>
    // `partial` because Unity 6.5's [OnExitingPlayMode] source generator requires it on any
    // type declaring a lifecycle method — the same reason MySceneManager carries it.
    public static partial class SceneManagerLog
    {
        /// <summary>
        /// The severity threshold: <see cref="SceneLogLevel.Warning"/> in development builds,
        /// <see cref="SceneLogLevel.Error"/> in release. Settable at runtime on purpose —
        /// raising it inside a shipped build is when this is worth having.
        /// </summary>
        public static SceneLogLevel Level { get; set; } = DefaultLevel;

        /// <summary>
        /// Where messages go; the Unity console by default. Assigning <see langword="null"/>
        /// restores the default rather than silencing — use <see cref="SceneLogLevel.Off"/> for that.
        /// </summary>
        public static ILogHandler Handler
        {
            get => _handler;
            set => _handler = value ?? Debug.unityLogger.logHandler;
        }

        static ILogHandler _handler = Debug.unityLogger.logHandler;

        static SceneLogLevel DefaultLevel => Debug.isDebugBuild ? SceneLogLevel.Warning : SceneLogLevel.Error;

        /// <summary>Logs at <see cref="SceneLogLevel.Error"/>.</summary>
        public static void Error(string message) => Log(SceneLogLevel.Error, LogType.Error, message);

        /// <summary>Logs at <see cref="SceneLogLevel.Warning"/>.</summary>
        public static void Warning(string message) => Log(SceneLogLevel.Warning, LogType.Warning, message);

        /// <summary>Logs at <see cref="SceneLogLevel.Info"/>.</summary>
        public static void Info(string message) => Log(SceneLogLevel.Info, LogType.Log, message);

        /// <summary>Logs at <see cref="SceneLogLevel.Verbose"/>.</summary>
        public static void Verbose(string message) => Log(SceneLogLevel.Verbose, LogType.Log, message);

        static void Log(SceneLogLevel level, LogType logType, string message)
        {
            if (Level < level)
                return;

            ILogHandler handler = _handler;

            try
            {
                // "{0}" rather than passing the message as the format string: a message that
                // happens to contain braces — a scene path, a serialized value — would otherwise
                // be parsed as a format specifier and throw.
                handler.LogFormat(logType, null, "{0}", $"[{nameof(MySceneManager)}] {message}");
            }
            catch (System.Exception exception)
            {
                // An assigned handler is someone else's code — an in-game console, an analytics
                // sink — and it is reached from error paths that are already containing a
                // failure. Letting it throw there would escape that containment and strand the
                // caller. Fall back to the console it replaced, which is the one handler that
                // cannot be broken from outside.
                ILogHandler fallback = Debug.unityLogger.logHandler;
                if (ReferenceEquals(handler, fallback))
                    return;

                fallback.LogFormat(LogType.Error, null, "{0}", $"[{nameof(MySceneManager)}] The assigned {nameof(Handler)} threw, so this went to the console instead: {message} — {exception}");
            }
        }

        // Statics survive a disabled Domain Reload, so a test that raises the level or swaps
        // the handler would otherwise leak that into the next play session.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        internal static void ResetStatics()
        {
            _handler = Debug.unityLogger.logHandler;
            Level = DefaultLevel;
        }
    }
}
