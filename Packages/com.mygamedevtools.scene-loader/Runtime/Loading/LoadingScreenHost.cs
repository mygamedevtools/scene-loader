using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// A package-owned scene that exists for the length of one transition, so a loading screen
    /// has somewhere to live and the engine always has at least one scene loaded.
    /// </summary>
    /// <remarks>
    /// These are the same problem, which is why they are the same object.
    /// <br/><br/>
    /// v4 created a <c>"temp-transition-scene"</c> whenever a transition started from a single
    /// loaded scene, purely because Unity cannot have zero loaded scenes. A prefab or UI Toolkit
    /// screen has no intermediate scene of its own, so it hits that constraint on <i>every</i>
    /// transition — <i>and</i> it needs somewhere to be parented that survives the outgoing
    /// scene being unloaded. One holder scene answers both, and v4's special case disappears
    /// into it.
    /// <br/><br/>
    /// A dedicated scene rather than <c>DontDestroyOnLoad</c>: it is cleaner to tear down, and
    /// it does not leak if a transition faults.
    /// <br/><br/>
    /// The scene is created lazily, on the first <see cref="Adopt"/> or
    /// <see cref="EnsureCreated"/>. A transition through a loading <i>scene</i> needs neither —
    /// the loading scene is already keeping the engine above zero, and a scene-based screen has
    /// nothing to instantiate — so those transitions cost no extra scene at all, exactly as they
    /// did in v4.
    /// </remarks>
    public sealed class LoadingScreenHost : IDisposable
    {
        /// <summary>
        /// The name given to the holder scene. Visible in the hierarchy during a transition.
        /// </summary>
        public const string SceneName = "loading-screen-host";

        /// <summary>
        /// The holder scene, valid until <see cref="Dispose"/>.
        /// </summary>
        public Scene Scene => _scene;

        Scene _scene;
        bool _disposed;

        /// <summary>
        /// Creates the holder scene if it does not exist yet.
        /// <br/>
        /// Called by a transition that would otherwise leave the engine with zero loaded scenes.
        /// </summary>
        public void EnsureCreated()
        {
            if (_disposed)
                throw new InvalidOperationException($"[{nameof(LoadingScreenHost)}] The host scene has already been torn down.");
            if (_scene.IsValid())
                return;

            _scene = SceneManager.CreateScene(SceneName);

            if (SceneManagerLog.IsEnabled(SceneLogLevel.Verbose))
                SceneManagerLog.Verbose($"Created the loading screen host scene ({_scene.handle}).");
        }

        /// <summary>
        /// Moves an object into the holder scene, so it outlives the scene it was created in.
        /// Creates the scene if this is the first thing to need it.
        /// </summary>
        public void Adopt(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            EnsureCreated();
            SceneManager.MoveGameObjectToScene(gameObject, _scene);
        }

        /// <summary>
        /// Starts destroying the holder scene, and hands back the operation to wait on.
        /// <br/>
        /// The transition does wait: leaving a half-unloaded scene behind makes the very next
        /// unload of it — a test teardown, say — a double unload, which the engine asserts on.
        /// </summary>
        /// <returns>The unload operation, or <see langword="null"/> if there was nothing to unload.</returns>
        public AsyncOperation BeginDispose()
        {
            _disposed = true;

            if (!_scene.IsValid())
                return null;

            Scene scene = _scene;
            _scene = default;

            return scene.isLoaded ? SceneManager.UnloadSceneAsync(scene) : null;
        }

        /// <summary>
        /// Destroys the holder scene without waiting for the unload to finish. Prefer
        /// <see cref="BeginDispose"/> where the caller can wait.
        /// </summary>
        public void Dispose() => BeginDispose();
    }
}
