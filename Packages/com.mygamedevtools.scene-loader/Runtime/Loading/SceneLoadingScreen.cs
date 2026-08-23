using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// A loading screen that is a scene — what v4 always meant by "loading screen", and the
    /// target of every implicit conversion on <see cref="LoadingScreen"/>, which is why it is
    /// core rather than a sample.
    /// </summary>
    public sealed class SceneLoadingScreen : LoadingScreen
    {
        /// <summary>The scene to load as the intermediate.</summary>
        public SceneRef SceneRef => _sceneRef;

        /// <summary>The loaded intermediate scene, once the transition has loaded it.</summary>
        public Scene Scene => _scene;

        readonly SceneRef _sceneRef;

        Scene _scene;
        LoadingProgress _progress;

        /// <summary>Uses the scene at <paramref name="sceneRef"/> as the loading screen.</summary>
        public SceneLoadingScreen(SceneRef sceneRef)
        {
            _sceneRef = sceneRef;
        }

        /// <summary>Records the loaded scene and finds its <see cref="LoadingBehavior"/>, if any.</summary>
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
        /// Waits for the scene's <see cref="LoadingBehavior"/> to report itself shown. A scene
        /// without one gates on nothing, as in v4.
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

        /// <summary>Nothing to tear down: the transition loaded the scene, so it unloads it.</summary>
        public override void Dispose()
        {
            _progress = null;
        }
    }
}
