#if !DISABLE_STATIC_SCENE_MANAGER
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// The package's entry point: a static mirror of <see cref="ISceneManager"/> over a
    /// process-wide default instance, so the headline call stays a one-liner.
    /// <code>
    /// MySceneManager.TransitionAsync("target", "loading");
    /// </code>
    /// </summary>
    public static partial class MySceneManager
    {
        /// <summary>
        /// The instance every static member forwards to. Settable, so tests and DI setups can
        /// substitute their own rather than finding the static class to be a dead end.
        /// </summary>
        public static ISceneManager Default
        {
            get
            {
                if (_instance == null)
                    throw new InvalidOperationException($"[{nameof(MySceneManager)}] The static Scene Manager instance is not available before the first scene is fully loaded. Try moving the call to `Start()`, or ask {nameof(TryGetDefault)} first.");
                return _instance;
            }
            set => _instance = value;
        }

        /// <summary>
        /// The manager, if there is one yet. The only way to ask without risking the exception
        /// <see cref="Default"/> throws.
        /// </summary>
        /// <remarks>
        /// There are two windows where there is no manager, and both are ordinary rather than
        /// exceptional: before the first scene has finished loading — every <c>Awake</c> and
        /// <c>OnEnable</c> in it — and after play mode has torn the statics down, which is where
        /// an <c>OnDestroy</c> unsubscribing from manager events can land.
        /// <code>
        /// if (MySceneManager.TryGetDefault(out ISceneManager manager))
        ///     manager.OperationStarted -= OnOperationStarted;
        /// </code>
        /// It hands back the manager rather than a bare <see langword="bool"/> because every
        /// reason to ask is followed by wanting to use it.
        /// </remarks>
        /// <returns>Whether a manager was available.</returns>
        public static bool TryGetDefault(out ISceneManager manager)
        {
            manager = _instance;
            return manager != null;
        }

        internal static ISceneManager Instance => Default;

        static ISceneManager _instance;

        // Statics survive a disabled Domain Reload, so the previous session's manager would linger
        // until Initialize runs after the first scene loads. Clearing the field makes this reentrant.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_6000_5_OR_NEWER
        [OnExitingPlayMode]
#endif
        static void ResetStatics()
        {
            _instance?.Dispose();
            _instance = null;
        }

        [RuntimeInitializeOnLoadMethod]
        internal static void Initialize()
        {
            _instance = new CoreSceneManager(true);
        }

        /// <summary>
        /// Reports that the active scene has changed, passing the <b>previous</b> and <b>current</b> active scene as parameters.
        /// <br/>
        /// In some scenarios, the previous or the current scene might be invalid <i>(you can check it through <see cref="Scene.IsValid()"/>)</i>, but never both at the same time.
        /// <br/>
        /// This can occur when the first active scene is being set (there was no previous active scene) or when the last scene gets unloaded (leaving no other scene to be activated).
        /// </summary>
        public static event Action<Scene, Scene> ActiveSceneChanged
        {
            add => Default.ActiveSceneChanged += value;
            remove => Default.ActiveSceneChanged -= value;
        }
        /// <summary>
        /// Reports when a scene gets unloaded.
        /// </summary>
        public static event Action<Scene> SceneUnloaded
        {
            add => Default.SceneUnloaded += value;
            remove => Default.SceneUnloaded -= value;
        }
        /// <summary>
        /// Reports when a scene gets loaded.
        /// </summary>
        public static event Action<Scene> SceneLoaded
        {
            add => Default.SceneLoaded += value;
            remove => Default.SceneLoaded -= value;
        }
        /// <summary>
        /// Reports every operation started through the static manager, before it runs.
        /// </summary>
        public static event Action<SceneOperation> OperationStarted
        {
            add => Default.OperationStarted += value;
            remove => Default.OperationStarted -= value;
        }

        /// <summary>
        /// The amount of scenes loaded through the <see cref="MySceneManager"/>.
        /// To get the total amount of loaded scenes, check <see cref="SceneManager.sceneCount"/>.
        /// </summary>
        public static int LoadedSceneCount => Default.LoadedSceneCount;
        /// <summary>
        /// The amount of scenes managed by the <see cref="MySceneManager"/>.
        /// This includes scenes that are being unloaded.
        /// </summary>
        public static int TotalSceneCount => Default.TotalSceneCount;

        /// <summary>
        /// Sets the target <paramref name="scene"/> as the active scene.
        /// Internally calls <see cref="SceneManager.SetActiveScene(Scene)"/>.
        /// </summary>
        /// <param name="scene">Scene to be enabled as the active scene.</param>
        public static void SetActiveScene(Scene scene) => Default.SetActiveScene(scene);

        /// <summary>
        /// Loads the target scene or group of scenes. Takes a name, path or address, a build
        /// index, a <see cref="Scene"/>, an <c>AssetReference</c>, an array of any of those, or
        /// a <see cref="SceneParameters"/> when you also need to say which becomes active.
        /// </summary>
        /// <param name="sceneParameters">The scene or scenes to load, and optionally which to activate.</param>
        /// <returns>A <see cref="SceneOperation"/> handle on the load.</returns>
        public static SceneOperation LoadAsync(SceneParameters sceneParameters) => Default.LoadAsync(sceneParameters);

        /// <summary>Unloads the target scene or group of scenes.</summary>
        /// <param name="sceneParameters">The scene or scenes to unload.</param>
        /// <returns>
        /// A <see cref="SceneOperation"/> handle on the unload, whose result is the unloaded scenes.
        /// <br/>
        /// Note that in some cases, the returned scenes might no longer have a reference to its native representation, which means its <see cref="Scene.handle"/> will not point anywhere and you won't be able to perform equal comparisons between scenes.
        /// </returns>
        public static SceneOperation UnloadAsync(SceneParameters sceneParameters) => Default.UnloadAsync(sceneParameters);

        /// <summary>
        /// Transitions from the active scene to the target scene or group, optionally showing a
        /// loading screen. Strings resolve themselves, so an addressable transition looks exactly
        /// like a non-addressable one.
        /// </summary>
        /// <param name="sceneParameters">The scene or scenes to transition to. One of them must be marked active.</param>
        /// <param name="loadingScreen">What to show while the transition runs: a scene name, path or address, a build index, a <see cref="Scene"/>, or your own <see cref="LoadingScreen"/>. Leave it unset for no loading screen.</param>
        /// <returns>A <see cref="SceneOperation"/> handle on the transition.</returns>
        public static SceneOperation TransitionAsync(SceneParameters sceneParameters, LoadingScreen loadingScreen = null) => Default.TransitionAsync(sceneParameters, loadingScreen);

        /// <summary>Reloads the active scene, optionally showing a loading screen.</summary>
        /// <param name="loadingScreen">What to show while the reload runs. Leave it unset for no loading screen.</param>
        /// <returns>A <see cref="SceneOperation"/> handle on the reload.</returns>
        public static SceneOperation ReloadActiveSceneAsync(LoadingScreen loadingScreen = null) => Default.ReloadActiveSceneAsync(loadingScreen);

        /// <summary>Gets the current active scene.</summary>
        /// <returns>The current active scene, or an invalid scene if none of the loaded scenes are enabled as the active scene.</returns>
        public static Scene GetActiveScene() => Default.GetActiveScene();

        /// <summary>
        /// Gets the loaded scene at the <paramref name="index"/> of the loaded scenes list,
        /// reporting whether there is one.
        /// </summary>
        /// <remarks>
        /// <see cref="LoadedSceneCount"/> is the range, and it moves: a scene loading or unloading
        /// elsewhere changes it between the moment you read it and the moment you index with it.
        /// Answering rather than throwing is what makes that safe to walk.
        /// </remarks>
        /// <param name="index">Index of the target scene in the loaded scenes list.</param>
        /// <param name="scene">The loaded scene at that index, or an invalid scene if the index is out of range.</param>
        /// <returns>Whether a loaded scene exists at <paramref name="index"/>.</returns>
        public static bool TryGetLoadedSceneAt(int index, out Scene scene) => Default.TryGetLoadedSceneAt(index, out scene);

        /// <summary>Gets the last loaded scene.</summary>
        /// <returns>The last loaded scene, or an invalid scene if there are no loaded scenes.</returns>
        public static Scene GetLastLoadedScene() => Default.GetLastLoadedScene();

        /// <summary>
        /// Gets a loaded scene by its <paramref name="name"/>, reporting whether there is one.
        /// </summary>
        /// <remarks>
        /// This looks only at scenes the manager has finished loading. A scene still being loaded
        /// is not one of them, so this is not a guard against starting a second load of the same
        /// scene — for that, keep the handle the first <see cref="LoadAsync"/> returned.
        /// </remarks>
        /// <param name="name">Name of the loaded scene to be found.</param>
        /// <param name="scene">The loaded scene with that name, or an invalid scene if there is none.</param>
        /// <returns>Whether a loaded scene with the given <paramref name="name"/> was found.</returns>
        public static bool TryGetLoadedSceneByName(string name, out Scene scene) => Default.TryGetLoadedSceneByName(name, out scene);
    }
}
#endif
