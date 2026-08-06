using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// One configurable, routable sink for every diagnostic the scene manager emits.
    /// <br/><br/>
    /// <b>Always guard the call site.</b> Interpolation happens before the call, so a disabled
    /// level only saves anything if the caller checks first:
    /// <code>
    /// if (SceneManagerLog.IsEnabled(SceneLogLevel.Verbose))
    ///     SceneManagerLog.Verbose($"Linked {scene.name} to {sceneRef}");
    /// </code>
    /// Defining <c>MSM_DISABLE_LOGGING</c> strips the implementation; the signatures stay so
    /// user code still compiles.
    /// </summary>
    /// <remarks>
    /// Stripped with <c>#if</c> and empty bodies rather than
    /// <see cref="System.Diagnostics.ConditionalAttribute"/>, which only has a positive form —
    /// using it would mean inverting the default and making logging opt-in.
    /// </remarks>
    // `partial` because Unity 6.5's [OnExitingPlayMode] source generator requires it on any
    // type declaring a lifecycle method — the same reason MySceneManager carries it.
    public static partial class SceneManagerLog
    {
#if !MSM_DISABLE_LOGGING
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

        /// <summary>
        /// Whether <paramref name="level"/> would be emitted. Call this before building a message.
        /// </summary>
        public static bool IsEnabled(SceneLogLevel level)
        {
            return level != SceneLogLevel.Off && Level >= level;
        }

        /// <summary>Logs at <see cref="SceneLogLevel.Error"/>. Guard the call site with <see cref="IsEnabled"/>.</summary>
        public static void Error(string message) => Log(SceneLogLevel.Error, LogType.Error, message);

        /// <summary>Logs at <see cref="SceneLogLevel.Warning"/>. Guard the call site with <see cref="IsEnabled"/>.</summary>
        public static void Warning(string message) => Log(SceneLogLevel.Warning, LogType.Warning, message);

        /// <summary>Logs at <see cref="SceneLogLevel.Info"/>. Guard the call site with <see cref="IsEnabled"/>.</summary>
        public static void Info(string message) => Log(SceneLogLevel.Info, LogType.Log, message);

        /// <summary>Logs at <see cref="SceneLogLevel.Verbose"/>. Guard the call site with <see cref="IsEnabled"/>.</summary>
        public static void Verbose(string message) => Log(SceneLogLevel.Verbose, LogType.Log, message);

        static void Log(SceneLogLevel level, LogType logType, string message)
        {
            // Re-checked here so an unguarded call still respects the level. The call-site
            // guard is about the cost of building the message, not about correctness.
            if (!IsEnabled(level))
                return;

            // "{0}" rather than passing the message as the format string: a message that
            // happens to contain braces — a scene path, a serialized value — would otherwise
            // be parsed as a format specifier and throw.
            _handler.LogFormat(logType, null, "{0}", $"[{nameof(MySceneManager)}] {message}");
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
#else
        /// <summary>Always <see cref="SceneLogLevel.Off"/>; the layer is stripped.</summary>
        public static SceneLogLevel Level
        {
            get => SceneLogLevel.Off;
            set { }
        }

        /// <summary>Always <see langword="null"/>; the layer is stripped.</summary>
        public static ILogHandler Handler
        {
            get => null;
            set { }
        }

        /// <summary>Always <see langword="false"/>, so guarded call sites never build their messages.</summary>
        public static bool IsEnabled(SceneLogLevel level) => false;

        public static void Error(string message) { }

        public static void Warning(string message) { }

        public static void Info(string message) { }

        public static void Verbose(string message) { }

        internal static void ResetStatics() { }
#endif
    }
}
