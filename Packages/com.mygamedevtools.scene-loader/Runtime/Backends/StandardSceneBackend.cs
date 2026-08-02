using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Loads and unloads through the Unity <see cref="SceneManager"/>.
    /// </summary>
    /// <remarks>
    /// What this backend cannot do shapes the rest of the design. The Scene Manager will not
    /// tell you which scene an operation produced, which is why
    /// <see cref="TryResolveScene"/> answers <see langword="false"/> and the manager matches
    /// newly-loaded scenes against references instead. It also has no failure surface at all: a
    /// bad scene name logs to the console and <c>isDone</c> still goes true. So a faulted state
    /// is only honestly reachable on the addressable path, and failure here can only be
    /// <i>inferred</i> from "the operation finished and no new scene appeared" — reported
    /// through <see cref="SceneManagerLog"/> rather than dressed up as parity.
    /// </remarks>
    public sealed class StandardSceneBackend : ISceneBackend
    {
        public bool CanHandle(SceneRefKind kind) => kind == SceneRefKind.BuildIndex || kind == SceneRefKind.Scene;

        public SceneBackendHandle Load(SceneRef sceneRef)
        {
            if (sceneRef.Kind != SceneRefKind.BuildIndex)
                throw new ArgumentException($"[{nameof(StandardSceneBackend)}] Cannot load {sceneRef}. Only a resolved build index can start a non-addressable load.", nameof(sceneRef));

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneRef.BuildIndex, LoadSceneMode.Additive);
            if (operation == null)
                throw new ArgumentException($"[{nameof(StandardSceneBackend)}] The Unity Scene Manager refused to load {sceneRef}. Is the scene enabled in the build settings?", nameof(sceneRef));

            return SceneBackendHandle.ForStandard(this, sceneRef, default, operation);
        }

        public SceneBackendHandle Unload(SceneBackendHandle handle)
        {
            if (!handle.Scene.IsValid())
                throw new ArgumentException($"[{nameof(StandardSceneBackend)}] Cannot unload {handle}, because it was never linked to a loaded scene.", nameof(handle));

            AsyncOperation operation = SceneManager.UnloadSceneAsync(handle.Scene);
            if (operation == null)
                throw new ArgumentException($"[{nameof(StandardSceneBackend)}] The Unity Scene Manager refused to unload {handle}.", nameof(handle));

            return handle.WithStandardOperation(operation);
        }

        public float GetProgress(SceneBackendHandle handle)
        {
            // `progress` caps at 0.9 while `allowSceneActivation` is false. The package never
            // sets it, and there is no API to, so the well-known 0.9 stall does not apply here
            // and the value genuinely reaches 1.
            return handle.StandardOperation?.progress ?? 0f;
        }

        public bool IsDone(SceneBackendHandle handle) => handle.StandardOperation == null || handle.StandardOperation.isDone;

        public bool TryResolveScene(SceneBackendHandle handle, out Scene scene)
        {
            // See the type-level remarks: there is no API that answers this, so the manager
            // falls back to matching against newly-loaded scenes.
            scene = default;
            return false;
        }
    }
}
