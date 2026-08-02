#if !DISABLE_STATIC_SCENE_MANAGER
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// The package's entry point: a static mirror of <see cref="ISceneManager"/> over a
    /// process-wide default instance.
    /// <br/><br/>
    /// This is a thin forwarding layer by design. v4 spent 735 lines here because the API was a
    /// matrix of operation × arity × reference kind; with <see cref="SceneParameters"/> doing
    /// the conversions, it is one forwarder per operation and the headline call stays a
    /// one-liner:
    /// <code>
    /// MySceneManager.TransitionAsync("target", "loading");
    /// </code>
    /// </summary>
    public static partial class MySceneManager
    {
        /// <summary>
        /// The instance every static member here forwards to.
        /// <br/>
        /// Settable so tests and dependency-injection setups can substitute their own
        /// <see cref="ISceneManager"/> rather than finding the static class to be a dead end.
        /// Assigning <see langword="null"/> restores the package's own instance on next access.
        /// </summary>
        public static ISceneManager Default
        {
            get
            {
                if (_instance == null)
                    throw new NullReferenceException($"[{nameof(MySceneManager)}] The static Scene Manager instance is not available before the first scene is fully loaded. Try moving the call to `Start()`.");
                return _instance;
            }
            set => _instance = value;
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
        /// Loads the target scene or group of scenes.
        /// <br/>
        /// The parameter accepts a scene name, path or Addressables address, a build index, a
        /// <see cref="Scene"/>, an <c>AssetReference</c>, an array of any of those, or a
        /// <see cref="SceneParameters"/> when you also need to say which one becomes active.
        /// </summary>
        /// <param name="sceneParameters">The scene or scenes to load, and optionally which to activate.</param>
        /// <param name="progress">Object to report the loading operations progress to, from 0 to 1.</param>
        /// <param name="token">Optional token to manually cancel the operation. Note that Unity Scene Manager operations cannot be manually canceled and will continue to run.</param>
        /// <returns>A <see cref="Task{TResult}"/> with all scenes loaded.</returns>
        public static Task<SceneResult> LoadAsync(SceneParameters sceneParameters, IProgress<float> progress = null, CancellationToken token = default) => Default.LoadAsync(sceneParameters, progress, token);

        /// <summary>
        /// Unloads the target scene or group of scenes.
        /// </summary>
        /// <param name="sceneParameters">The scene or scenes to unload.</param>
        /// <param name="token">Optional token to manually cancel the operation. Note that Unity Scene Manager operations cannot be manually canceled and will continue to run.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> with all the unloaded scenes.
        /// <br/>
        /// Note that in some cases, the returned scenes might no longer have a reference to its native representation, which means its <see cref="Scene.handle"/> will not point anywhere and you won't be able to perform equal comparisons between scenes.
        /// </returns>
        public static Task<SceneResult> UnloadAsync(SceneParameters sceneParameters, CancellationToken token = default) => Default.UnloadAsync(sceneParameters, token);

        /// <summary>
        /// Transitions from the current active scene to the target scene or group of scenes,
        /// optionally showing a loading scene while it happens.
        /// <br/>
        /// Both arguments take a name, path or Addressables address interchangeably — the
        /// strings resolve themselves, so an addressable transition looks exactly like a
        /// non-addressable one.
        /// </summary>
        /// <param name="sceneParameters">The scene or scenes to transition to. One of them must be marked active.</param>
        /// <param name="loadingScene">The scene to load as the transition intermediate. Leave it unset for a transition with no loading scene.</param>
        /// <param name="token">Optional token to manually cancel the operation. Note that Unity Scene Manager operations cannot be manually canceled and will continue to run.</param>
        /// <returns>A <see cref="Task{TResult}"/> with all scenes loaded.</returns>
        public static Task<SceneResult> TransitionAsync(SceneParameters sceneParameters, SceneRef loadingScene = default, CancellationToken token = default) => Default.TransitionAsync(sceneParameters, loadingScene, token);

        /// <summary>
        /// Reloads the active scene, optionally showing a loading scene while it happens.
        /// </summary>
        /// <param name="loadingScene">The scene to load as the transition intermediate. Leave it unset for a reload with no loading scene.</param>
        /// <param name="token">Optional token to manually cancel the operation. Note that Unity Scene Manager operations cannot be manually canceled and will continue to run.</param>
        /// <returns>A <see cref="Task{TResult}"/> with all scenes reloaded.</returns>
        public static Task<SceneResult> ReloadActiveSceneAsync(SceneRef loadingScene = default, CancellationToken token = default) => Default.ReloadActiveSceneAsync(loadingScene, token);

        /// <summary>
        /// Gets the current active scene.
        /// </summary>
        /// <returns>The current active scene, or an invalid scene if none of the loaded scenes are enabled as the active scene.</returns>
        public static Scene GetActiveScene() => Default.GetActiveScene();

        /// <summary>
        /// Gets the loaded scene at the <paramref name="index"/> of the loaded scenes list.
        /// </summary>
        /// <param name="index">Index of the target scene in the loaded scenes list.</param>
        /// <returns>The loaded scene at the <paramref name="index"/> of the loaded scenes list.</returns>
        public static Scene GetLoadedSceneAt(int index) => Default.GetLoadedSceneAt(index);

        /// <summary>
        /// Gets the last loaded scene.
        /// </summary>
        /// <returns>The last loaded scene, or an invalid scene if there are no loaded scenes.</returns>
        public static Scene GetLastLoadedScene() => Default.GetLastLoadedScene();

        /// <summary>
        /// Gets a loaded scene by its <paramref name="name"/>.
        /// </summary>
        /// <param name="name">Name of the loaded scene to be found.</param>
        /// <returns>A loaded scene with the given <paramref name="name"/>.</returns>
        public static Scene GetLoadedSceneByName(string name) => Default.GetLoadedSceneByName(name);
    }
}
#endif
