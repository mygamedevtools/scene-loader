using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// A loading screen that is a scene: the transition loads it, holds it up while the target
    /// loads, and unloads it afterwards. The target of every implicit conversion on
    /// <see cref="LoadingScreen"/>, which is why it is core rather than a sample.
    /// </summary>
    /// <remarks>
    /// It gates on the <see cref="LoadingBehavior"/> in that scene, if there is one. A loading
    /// scene without one shows for exactly as long as the load takes.
    /// </remarks>
    public sealed class SceneLoadingScreen : LoadingScreen
    {
        /// <summary>The scene to load as the intermediate.</summary>
        public SceneRef SceneRef => _sceneRef;

        /// <summary>The loaded intermediate scene, once the transition has loaded it.</summary>
        public Scene Scene => _scene;

        readonly SceneRef _sceneRef;

        Scene _scene;

        /// <summary>Uses the scene at <paramref name="sceneRef"/> as the loading screen.</summary>
        public SceneLoadingScreen(SceneRef sceneRef)
        {
            _sceneRef = sceneRef;
        }

        /// <summary>Records the loaded scene and binds its <see cref="LoadingBehavior"/>, if any.</summary>
        internal void SetLoadedScene(Scene scene)
        {
            _scene = scene;
            BindProgress(LoadingBehaviorRegistry.TryGet(scene, out LoadingBehavior behavior) ? behavior.Progress : null);
        }

        /// <summary>
        /// Nothing to build: a scene screen is already where it needs to be, because the transition
        /// loaded it before preparing it.
        /// </summary>
        public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
        {
            return SceneOperationPump.Completed(operation);
        }
    }
}
