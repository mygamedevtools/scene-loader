using System;
using UnityEngine;
#if ENABLE_ADDRESSABLES
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// A scene the manager is tracking: which backend owns it, what reference asked for it, the
    /// in-flight operation, and — once linking has run — the scene itself.
    /// <br/><br/>
    /// The engine-specific operations are held in typed fields rather than behind a common
    /// interface, so nothing boxes: <c>AsyncOperation</c> is already a class, and Addressables'
    /// <c>AsyncOperationHandle&lt;SceneInstance&gt;</c> is a struct that would have to be boxed
    /// to sit behind one. Only the backend that created a handle ever reads its own field, which
    /// is what makes that safe.
    /// <br/><br/>
    /// Immutable, like <see cref="SceneRef"/>. v4's <c>SceneDataStandard</c> and
    /// <c>SceneDataAddressable</c> were <i>mutable structs implementing an interface</i>, stored
    /// in <c>List&lt;ISceneData&gt;</c> — every creation boxed, and every mutation mutated the
    /// boxed copy. It worked only because callers always reached them through the box. The
    /// category is gone here: <see cref="WithScene"/> returns a new value.
    /// </summary>
    public readonly struct SceneBackendHandle : IEquatable<SceneBackendHandle>
    {
        /// <summary>
        /// The backend that owns this handle, or <see langword="null"/> for a default handle.
        /// </summary>
        public readonly ISceneBackend Backend => _backend;

        /// <summary>
        /// The reference the scene was requested by, already resolved.
        /// </summary>
        public readonly SceneRef SceneRef => _sceneRef;

        /// <summary>
        /// The loaded scene, once it has been linked. Invalid before that.
        /// </summary>
        public readonly Scene Scene => _scene;

        /// <summary>
        /// Whether this handle refers to anything at all.
        /// </summary>
        public readonly bool IsValid => _backend != null;

        /// <summary>
        /// The non-addressable operation. Only <see cref="StandardSceneBackend"/> reads this.
        /// </summary>
        internal readonly AsyncOperation StandardOperation => _standardOperation;

#if ENABLE_ADDRESSABLES
        /// <summary>
        /// The addressable operation. Only <see cref="AddressablesSceneBackend"/> reads this.
        /// </summary>
        internal readonly AsyncOperationHandle<SceneInstance> AddressableOperation => _addressableOperation;
#endif

        readonly ISceneBackend _backend;
        readonly SceneRef _sceneRef;
        readonly Scene _scene;
        readonly AsyncOperation _standardOperation;
#if ENABLE_ADDRESSABLES
        readonly AsyncOperationHandle<SceneInstance> _addressableOperation;
#endif

        SceneBackendHandle(ISceneBackend backend, SceneRef sceneRef, Scene scene, AsyncOperation standardOperation
#if ENABLE_ADDRESSABLES
            , AsyncOperationHandle<SceneInstance> addressableOperation
#endif
            )
        {
            _backend = backend;
            _sceneRef = sceneRef;
            _scene = scene;
            _standardOperation = standardOperation;
#if ENABLE_ADDRESSABLES
            _addressableOperation = addressableOperation;
#endif
        }

        /// <summary>
        /// A handle over a non-addressable <see cref="AsyncOperation"/>.
        /// </summary>
        internal static SceneBackendHandle ForStandard(ISceneBackend backend, SceneRef sceneRef, Scene scene, AsyncOperation operation)
        {
            return new SceneBackendHandle(backend, sceneRef, scene, operation
#if ENABLE_ADDRESSABLES
                , default
#endif
                );
        }

#if ENABLE_ADDRESSABLES
        /// <summary>
        /// A handle over an Addressables scene operation.
        /// </summary>
        internal static SceneBackendHandle ForAddressable(ISceneBackend backend, SceneRef sceneRef, Scene scene, AsyncOperationHandle<SceneInstance> operation)
        {
            return new SceneBackendHandle(backend, sceneRef, scene, null, operation);
        }
#endif

        /// <summary>
        /// The same handle with its scene filled in, which is what linking produces.
        /// </summary>
        public readonly SceneBackendHandle WithScene(Scene scene)
        {
            return new SceneBackendHandle(_backend, _sceneRef, scene, _standardOperation
#if ENABLE_ADDRESSABLES
                , _addressableOperation
#endif
                );
        }

        /// <summary>
        /// The same handle carrying a different operation, which is what an unload produces.
        /// </summary>
        internal readonly SceneBackendHandle WithStandardOperation(AsyncOperation operation)
        {
            return new SceneBackendHandle(_backend, _sceneRef, _scene, operation
#if ENABLE_ADDRESSABLES
                , default
#endif
                );
        }

#if ENABLE_ADDRESSABLES
        /// <summary>
        /// The same handle carrying a different addressable operation.
        /// </summary>
        internal readonly SceneBackendHandle WithAddressableOperation(AsyncOperationHandle<SceneInstance> operation)
        {
            return new SceneBackendHandle(_backend, _sceneRef, _scene, null, operation);
        }
#endif

        /// <summary>
        /// Identity is the tracked scene where there is one, and the operation otherwise — two
        /// handles built from the same reference are still two separate loads.
        /// </summary>
        public readonly bool Equals(SceneBackendHandle other)
        {
            if (_backend != other._backend)
                return false;

            if (_scene.IsValid() || other._scene.IsValid())
                return _scene == other._scene;

            if (_standardOperation != null || other._standardOperation != null)
                return ReferenceEquals(_standardOperation, other._standardOperation);

#if ENABLE_ADDRESSABLES
            return _addressableOperation.Equals(other._addressableOperation);
#else
            return true;
#endif
        }

        public override readonly bool Equals(object obj) => obj is SceneBackendHandle other && Equals(other);

        public override readonly int GetHashCode()
        {
            if (_scene.IsValid())
                return _scene.GetHashCode();

            return _standardOperation != null ? _standardOperation.GetHashCode() : _sceneRef.GetHashCode();
        }

        public static bool operator ==(SceneBackendHandle left, SceneBackendHandle right) => left.Equals(right);
        public static bool operator !=(SceneBackendHandle left, SceneBackendHandle right) => !left.Equals(right);

        public override readonly string ToString()
        {
            return _scene.IsValid()
                ? $"{_sceneRef} → '{_scene.name}' ({_scene.handle})"
                : $"{_sceneRef} (not linked yet)";
        }
    }
}
