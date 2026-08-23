using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Whatever shows loading UI and gates a transition on it — a scene, a prefab, a UI Toolkit
    /// document, or anything else that can show itself, report progress, hide itself and clean up.
    /// </summary>
    /// <remarks>
    /// <b>An abstract class, not an interface, and not by preference.</b> C# forbids user-defined
    /// conversions to or from an interface type, and <c>TransitionAsync("target", "loading")</c>
    /// has to keep compiling — which needs an implicit <c>string → LoadingScreen</c> conversion,
    /// which needs a class. Subclasses then pass through with no conversion at all.
    /// </remarks>
    public abstract class LoadingScreen : IDisposable
    {
        /// <summary>Brings the screen up. The transition waits here before it unloads anything.</summary>
        public abstract SceneOperationPump.ConditionAwaiter ShowAsync(SceneOperation operation);

        /// <summary>Reports load progress, from 0 to 1.</summary>
        public abstract void ReportProgress(float progress);

        /// <summary>Takes the screen down. Returning is "the loading screen is completely gone".</summary>
        public abstract SceneOperationPump.ConditionAwaiter HideAsync(SceneOperation operation);

        /// <summary>Tears down whatever the screen created. Always called, fault and cancel included.</summary>
        public abstract void Dispose();

        /// <summary>
        /// Builds the screen's content. Separate from construction because it usually cannot
        /// exist until the transition gives it a <paramref name="host"/> to live in.
        /// </summary>
        public abstract SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation);

        public static implicit operator LoadingScreen(string nameOrPathOrAddress) => new SceneLoadingScreen(nameOrPathOrAddress);
        public static implicit operator LoadingScreen(SceneRef sceneRef) => sceneRef.IsValid ? new SceneLoadingScreen(sceneRef) : null;
        public static implicit operator LoadingScreen(int buildIndex) => new SceneLoadingScreen(buildIndex);
        public static implicit operator LoadingScreen(Scene scene) => new SceneLoadingScreen(scene);
#if ENABLE_ADDRESSABLES
        // v4's TransitionAddressableAsync took an AssetReference loading scene, so that kind
        // keeps its conversion too.
        public static implicit operator LoadingScreen(UnityEngine.AddressableAssets.AssetReference assetReference) => new SceneLoadingScreen(SceneRef.FromAssetReference(assetReference));
#endif
    }
}
