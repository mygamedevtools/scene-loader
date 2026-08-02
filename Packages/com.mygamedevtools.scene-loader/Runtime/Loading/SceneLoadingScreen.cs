using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// A loading screen that is a scene, which is what v4 always meant by "loading screen".
    /// <br/><br/>
    /// It is the target of every implicit conversion on <see cref="LoadingScreen"/>, so
    /// <c>TransitionAsync("target", "loading")</c> still means exactly what it used to. That is
    /// also why it lives in the package rather than in the samples.
    /// </summary>
    public sealed class SceneLoadingScreen : LoadingScreen
    {
        /// <summary>
        /// The scene to load as the intermediate.
        /// </summary>
        public SceneRef SceneRef => _sceneRef;

        /// <summary>
        /// The loaded intermediate scene, once the transition has loaded it.
        /// </summary>
        public Scene Scene => _scene;

        readonly SceneRef _sceneRef;

        Scene _scene;
        LoadingProgress _progress;

        /// <summary>
        /// Uses the scene at <paramref name="sceneRef"/> as the loading screen.
        /// </summary>
        public SceneLoadingScreen(SceneRef sceneRef)
        {
            _sceneRef = sceneRef;
        }

        /// <summary>
        /// Records the scene the transition loaded for this screen, and finds the
        /// <see cref="LoadingBehavior"/> in it, if there is one.
        /// </summary>
        internal void SetLoadedScene(Scene scene)
        {
            _scene = scene;
            _progress = LoadingBehaviorRegistry.TryGet(scene, out LoadingBehavior behavior) ? behavior.Progress : null;
        }

        public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
        {
            // A scene screen is already where it needs to be — the transition loaded it — so it
            // has nothing to instantiate into the host.
            return SceneOperationPump.Completed(operation);
        }

        /// <summary>
        /// Waits for the scene's <see cref="LoadingBehavior"/> to report itself fully shown.
        /// A loading scene without one gates on nothing, which is the v4 behaviour for that case.
        /// </summary>
        public override SceneOperationPump.ConditionAwaiter ShowAsync(SceneOperation operation)
        {
            return _progress == null ? SceneOperationPump.Completed(operation) : _progress.WaitForShowAsync(operation);
        }

        public override void ReportProgress(float progress)
        {
            _progress?.Report(progress);
        }

        public override SceneOperationPump.ConditionAwaiter HideAsync(SceneOperation operation)
        {
            if (_progress == null)
                return SceneOperationPump.Completed(operation);

            _progress.SetLoadingCompleted();
            return _progress.WaitForHideAsync(operation);
        }

        /// <summary>
        /// Nothing to tear down: the intermediate scene is the transition's to unload, since it
        /// is the transition that loaded it.
        /// </summary>
        public override void Dispose()
        {
            _progress = null;
        }
    }
}
