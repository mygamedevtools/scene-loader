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
    /// Two needs, one object: a screen that is not a scene needs somewhere to live that survives
    /// the outgoing scene's unload, and Unity cannot have zero loaded scenes. v4's
    /// <c>temp-transition-scene</c> special case disappears into this.
    /// <br/><br/>
    /// A dedicated scene rather than <c>DontDestroyOnLoad</c>: cleaner to tear down, and it does
    /// not leak if a transition faults. Created lazily, so a transition through a loading
    /// <i>scene</i> still costs no extra scene, exactly as in v4.
    /// </remarks>
    public sealed class LoadingScreenHost : IDisposable
    {
        /// <summary>Visible in the hierarchy during a transition.</summary>
        public const string SceneName = "loading-screen-host";

        /// <summary>The holder scene, valid until <see cref="Dispose"/>.</summary>
        public Scene Scene => _scene;

        Scene _scene;
        bool _disposed;

        /// <summary>
        /// Creates the holder scene if it does not exist yet — called by a transition that would
        /// otherwise leave the engine with zero loaded scenes.
        /// </summary>
        public void EnsureCreated()
        {
            if (_disposed)
                throw new InvalidOperationException($"[{nameof(LoadingScreenHost)}] The host scene has already been torn down.");
            if (_scene.IsValid())
                return;

            _scene = SceneManager.CreateScene(SceneName);

            SceneManagerLog.Verbose($"Created the loading screen host scene ({_scene.handle}).");
        }

        /// <summary>
        /// Moves an object into the holder scene so it outlives the scene it was created in,
        /// creating that scene if this is the first thing to need it.
        /// </summary>
        public void Adopt(GameObject gameObject)
        {
            if (gameObject == null)
                throw new ArgumentNullException(nameof(gameObject));

            EnsureCreated();
            SceneManager.MoveGameObjectToScene(gameObject, _scene);
        }

        /// <summary>
        /// Starts destroying the holder scene. The transition waits on the result: leaving one
        /// half-unloaded makes the next unload of it a double unload, which the engine asserts on.
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

        /// <summary>Destroys the holder scene without waiting. Prefer <see cref="BeginDispose"/>.</summary>
        public void Dispose() => BeginDispose();
    }
}
