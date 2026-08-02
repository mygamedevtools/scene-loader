#if ENABLE_ADDRESSABLES
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Loads and unloads through Addressables.
    /// </summary>
    /// <remarks>
    /// Only the two-argument <c>LoadSceneAsync(key, LoadSceneMode)</c> overload is used, because
    /// the CI matrix resolves three different Addressables majors — 1.19.19, 2.8.0 and 2.9.1 —
    /// behind a single <c>ENABLE_ADDRESSABLES</c> define that does not distinguish them. Anything
    /// newer, <c>SceneReleaseMode</c> in particular, would need its own version define in the
    /// asmdef before it could be touched.
    /// </remarks>
    public sealed class AddressablesSceneBackend : ISceneBackend
    {
        public bool CanHandle(SceneRefKind kind) => kind == SceneRefKind.Address || kind == SceneRefKind.AssetReference;

        public SceneBackendHandle Load(SceneRef sceneRef)
        {
            object key = sceneRef.Kind switch
            {
                SceneRefKind.Address => sceneRef.Key,
                SceneRefKind.AssetReference => sceneRef.AssetReference,
                _ => throw new ArgumentException($"[{nameof(AddressablesSceneBackend)}] Cannot load {sceneRef}. Only an address or an asset reference can start an addressable load.", nameof(sceneRef)),
            };

            return SceneBackendHandle.ForAddressable(this, sceneRef, default, Addressables.LoadSceneAsync(key, LoadSceneMode.Additive));
        }

        public SceneBackendHandle Unload(SceneBackendHandle handle)
        {
            if (!handle.AddressableOperation.IsValid())
                throw new ArgumentException($"[{nameof(AddressablesSceneBackend)}] Cannot unload {handle}, because its addressable operation is no longer valid.", nameof(handle));

            return handle.WithAddressableOperation(Addressables.UnloadSceneAsync(handle.AddressableOperation));
        }

        public float GetProgress(SceneBackendHandle handle)
        {
            // Spans download, load and activation, so it measures strictly more work than the
            // standard backend's `progress`. See ISceneBackend.GetProgress.
            return handle.AddressableOperation.IsValid() ? handle.AddressableOperation.PercentComplete : 0f;
        }

        public bool IsDone(SceneBackendHandle handle) => !handle.AddressableOperation.IsValid() || handle.AddressableOperation.IsDone;

        public bool TryResolveScene(SceneBackendHandle handle, out Scene scene)
        {
            AsyncOperationHandle<UnityEngine.ResourceManagement.ResourceProviders.SceneInstance> operation = handle.AddressableOperation;

            if (!operation.IsValid() || !operation.IsDone)
            {
                scene = default;
                return false;
            }

            if (operation.Status == AsyncOperationStatus.Failed)
                throw operation.OperationException;

            scene = operation.Result.Scene;
            return true;
        }
    }
}
#endif
