using UnityEngine;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// The package's logging layer: one configurable, routable, off-by-default sink for every
    /// diagnostic the scene manager emits.
    /// <br/><br/>
    /// <b>Always guard the call site.</b> String interpolation happens <i>before</i> the call,
    /// so a disabled level saves nothing unless the caller checks first:
    /// <code>
    /// if (SceneManagerLog.IsEnabled(SceneLogLevel.Verbose))
    ///     SceneManagerLog.Verbose($"Linked {scene.name} ({scene.handle}) to {sceneRef}");
    /// </code>
    /// Every method below repeats this, because it is the one convention that keeps the
    /// logging layer from eating the package's allocation budget.
    /// <br/><br/>
    /// Defining <c>MSM_DISABLE_LOGGING</c> strips the implementation entirely: the methods
    /// keep their signatures so user code still compiles, but their bodies are empty and
    /// <see cref="IsEnabled"/> is constantly <see langword="false"/>.
    /// </summary>
    /// <remarks>
    /// The strip is done with <c>#if</c> and empty bodies rather than
    /// <see cref="System.Diagnostics.ConditionalAttribute"/>, because <c>[Conditional]</c>
    /// only has a positive form — it can include calls when a symbol is defined, never
    /// exclude them. Expressing "strip when <c>MSM_DISABLE_LOGGING</c> is set" through it
    /// would mean inverting the default and making logging opt-in, which is not the intent.
    /// </remarks>
    // `partial` because Unity 6.5's [OnExitingPlayMode] source generator requires it on any
    // type declaring a lifecycle method — the same reason MySceneManager carries it.
    public static partial class SceneManagerLog
    {
#if !MSM_DISABLE_LOGGING
        /// <summary>
        /// The severity threshold. Anything at or above this level is emitted.
        /// <br/>
        /// Defaults to <see cref="SceneLogLevel.Warning"/> in development builds and the
        /// editor, and <see cref="SceneLogLevel.Error"/> in release builds. It is settable at
        /// runtime on purpose: raising the level inside an already-shipped build is exactly
        /// when this is worth having, which a compile-time-only switch could not offer.
        /// </summary>
        public static SceneLogLevel Level { get; set; } = DefaultLevel;

        /// <summary>
        /// Where messages go. Defaults to <see cref="Debug.unityLogger"/>'s handler, which is
        /// the Unity console. Substitute one to route into an in-game console, an analytics
        /// pipeline, or a test capture. Assigning <see langword="null"/> restores the default
        /// rather than silencing the layer — use <see cref="SceneLogLevel.Off"/> for that.
        /// </summary>
        public static ILogHandler Handler
        {
            get => _handler;
            set => _handler = value ?? Debug.unityLogger.logHandler;
        }

        static ILogHandler _handler = Debug.unityLogger.logHandler;

        static SceneLogLevel DefaultLevel => Debug.isDebugBuild ? SceneLogLevel.Warning : SceneLogLevel.Error;

        /// <summary>
        /// Whether <paramref name="level"/> would currently be emitted.
        /// <br/>
        /// Call this before building a message. See the type-level remarks: the guard is the
        /// whole point, since the interpolated string is allocated before the log call runs.
        /// </summary>
        public static bool IsEnabled(SceneLogLevel level)
        {
            return level != SceneLogLevel.Off && Level >= level;
        }

        /// <summary>
        /// Logs at <see cref="SceneLogLevel.Error"/>. Guard the call site with
        /// <see cref="IsEnabled"/> before interpolating the message.
        /// </summary>
        public static void Error(string message) => Log(SceneLogLevel.Error, LogType.Error, message);

        /// <summary>
        /// Logs at <see cref="SceneLogLevel.Warning"/>. Guard the call site with
        /// <see cref="IsEnabled"/> before interpolating the message.
        /// </summary>
        public static void Warning(string message) => Log(SceneLogLevel.Warning, LogType.Warning, message);

        /// <summary>
        /// Logs at <see cref="SceneLogLevel.Info"/>. Guard the call site with
        /// <see cref="IsEnabled"/> before interpolating the message.
        /// </summary>
        public static void Info(string message) => Log(SceneLogLevel.Info, LogType.Log, message);

        /// <summary>
        /// Logs at <see cref="SceneLogLevel.Verbose"/>. Guard the call site with
        /// <see cref="IsEnabled"/> before interpolating the message.
        /// </summary>
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
        /// <summary>
        /// Always <see cref="SceneLogLevel.Off"/>: the layer is stripped by
        /// <c>MSM_DISABLE_LOGGING</c>. The setter is kept so call sites still compile.
        /// </summary>
        public static SceneLogLevel Level
        {
            get => SceneLogLevel.Off;
            set { }
        }

        /// <summary>
        /// Always <see langword="null"/>: the layer is stripped by <c>MSM_DISABLE_LOGGING</c>.
        /// The setter is kept so call sites still compile.
        /// </summary>
        public static ILogHandler Handler
        {
            get => null;
            set { }
        }

        /// <summary>
        /// Always <see langword="false"/>: the layer is stripped by <c>MSM_DISABLE_LOGGING</c>.
        /// Guarded call sites therefore never build their messages.
        /// </summary>
        public static bool IsEnabled(SceneLogLevel level) => false;

        /// <summary>Stripped by <c>MSM_DISABLE_LOGGING</c>.</summary>
        public static void Error(string message) { }

        /// <summary>Stripped by <c>MSM_DISABLE_LOGGING</c>.</summary>
        public static void Warning(string message) { }

        /// <summary>Stripped by <c>MSM_DISABLE_LOGGING</c>.</summary>
        public static void Info(string message) { }

        /// <summary>Stripped by <c>MSM_DISABLE_LOGGING</c>.</summary>
        public static void Verbose(string message) { }

        internal static void ResetStatics() { }
#endif
    }
}
