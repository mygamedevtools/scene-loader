using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Struct to manage the link between non-addressable scene operations, its <see cref="SceneRef"/> and resulting loaded scene.
    /// </summary>
    public struct SceneDataStandard : ISceneData
    {
        public readonly IAsyncSceneOperation AsyncOperation => _asyncSceneOperation;

        public readonly SceneRef SceneRef => _sceneRef;

        public readonly Scene SceneReference => _sceneReference;

        readonly SceneRef _sceneRef;

        IAsyncSceneOperation _asyncSceneOperation;
        Scene _sceneReference;

        /// <summary>
        /// Creates a new <see cref="SceneDataStandard"/> with the provided <see cref="SceneRef"/>.
        /// The reference must already be resolved, which for the standard path means
        /// <see cref="SceneRefKind.BuildIndex"/> — <see cref="SceneRefResolver"/> turns names and
        /// paths into that before the data is built.
        /// </summary>
        public SceneDataStandard(SceneRef sceneRef)
        {
            if (sceneRef.Kind != SceneRefKind.BuildIndex)
            {
                throw new ArgumentException($"Cannot create a {nameof(SceneDataStandard)} with a {nameof(SceneRef)} of kind '{sceneRef.Kind}'. It only supports {nameof(SceneRefKind.BuildIndex)}.", nameof(sceneRef));
            }

            _sceneRef = sceneRef;
            _asyncSceneOperation = default;
            _sceneReference = default;
        }

        /// <summary>
        /// Creates a new <see cref="SceneDataStandard"/> with an already loaded <see cref="Scene"/>.
        /// This will create an <see cref="ISceneData"/> without a load <see cref="IAsyncSceneOperation"/>.
        /// </summary>
        public SceneDataStandard(Scene loadedScene)
        {
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                throw new ArgumentException($"Cannot create a {nameof(SceneDataStandard)} with an invalid or not loaded scene: {loadedScene.name} ({loadedScene.handle})");
            }

            _sceneRef = SceneRef.FromScene(loadedScene);
            _sceneReference = loadedScene;
            _asyncSceneOperation = default;
        }

        public void SetSceneReferenceManually(Scene scene)
        {
            if (!AsyncOperation.IsDone)
                throw new Exception($"[{nameof(SceneDataStandard)}] Cannot update the scene reference before the scene has been loaded.");

            _sceneReference = scene;
        }

        public void UpdateSceneReference()
        {
            SceneManagerLog.Warning($"[{nameof(SceneDataStandard)}] This type of scene data should not have its scene set automatically. Instead, it is expected to set it by calling {nameof(ISceneData.SetSceneReferenceManually)}.");
        }

        public readonly bool Matches(SceneRef sceneRef)
        {
            return sceneRef.CanBeReferenceToScene(_sceneReference);
        }

        public IAsyncSceneOperation LoadSceneAsync()
        {
            _asyncSceneOperation = new AsyncSceneOperationStandard(SceneManager.LoadSceneAsync(_sceneRef.BuildIndex, LoadSceneMode.Additive));
            return _asyncSceneOperation;
        }

        public IAsyncSceneOperation UnloadSceneAsync()
        {
            _asyncSceneOperation = new AsyncSceneOperationStandard(SceneManager.UnloadSceneAsync(_sceneReference));
            return _asyncSceneOperation;
        }
    }
}
