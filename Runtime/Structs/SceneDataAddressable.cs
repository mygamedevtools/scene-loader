#if ENABLE_ADDRESSABLES
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Struct to manage the link between addressable scene operations, its <see cref="SceneRef"/> and resulting loaded scene.
    /// </summary>
    public struct SceneDataAddressable : ISceneData
    {
        public readonly IAsyncSceneOperation AsyncOperation => _asyncSceneOperation;

        public readonly SceneRef SceneRef => _sceneRef;

        public readonly Scene SceneReference => _sceneReference;

        readonly SceneRef _sceneRef;

        AsyncSceneOperationAddressable _asyncSceneOperation;
        Scene _sceneReference;

        /// <summary>
        /// Creates a new <see cref="SceneDataAddressable"/> with the provided <see cref="SceneRef"/>.
        /// Only supports the addressable kinds, <see cref="SceneRefKind.AssetReference"/> and
        /// <see cref="SceneRefKind.Address"/>.
        /// </summary>
        public SceneDataAddressable(SceneRef sceneRef)
        {
            if (sceneRef.Kind != SceneRefKind.AssetReference && sceneRef.Kind != SceneRefKind.Address)
            {
                throw new ArgumentException($"Cannot create a {nameof(SceneDataAddressable)} with a {nameof(SceneRef)} of kind '{sceneRef.Kind}'. It only supports {nameof(SceneRefKind.AssetReference)} and {nameof(SceneRefKind.Address)}.", nameof(sceneRef));
            }

            _sceneRef = sceneRef;
            _asyncSceneOperation = default;
            _sceneReference = default;
        }

        public void SetSceneReferenceManually(Scene scene)
        {
            SceneManagerLog.Warning($"[{nameof(SceneDataAddressable)}] This type of scene data should not have its scene set manually. Instead, it is expected to set it by calling {nameof(ISceneData.UpdateSceneReference)}.");
            _sceneReference = scene;
        }

        public void UpdateSceneReference()
        {
            if (!AsyncOperation.IsDone)
                throw new Exception($"[{nameof(SceneDataAddressable)}] Cannot update the scene reference before the scene has been loaded.");

            _sceneReference = AsyncOperation.GetResult();
        }

        public readonly bool Matches(SceneRef sceneRef)
        {
            return sceneRef.Kind switch
            {
                SceneRefKind.AssetReference or SceneRefKind.Address => sceneRef.Equals(_sceneRef),
                _ => sceneRef.CanBeReferenceToScene(_sceneReference),
            };
        }

        public IAsyncSceneOperation LoadSceneAsync()
        {
            object key = _sceneRef.Kind == SceneRefKind.AssetReference ? _sceneRef.AssetReference : _sceneRef.Key;
            _asyncSceneOperation = new AsyncSceneOperationAddressable(Addressables.LoadSceneAsync(key, LoadSceneMode.Additive));
            return _asyncSceneOperation;
        }

        public IAsyncSceneOperation UnloadSceneAsync()
        {
            _asyncSceneOperation = new AsyncSceneOperationAddressable(Addressables.UnloadSceneAsync(_asyncSceneOperation.AsyncOperationHandle));
            return _asyncSceneOperation;
        }
    }
}
#endif
