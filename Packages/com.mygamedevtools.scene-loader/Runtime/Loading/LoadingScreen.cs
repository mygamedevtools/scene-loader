using System;
using UnityEngine.SceneManagement;

namespace MyGameDevTools.SceneLoading
{
    /// <summary>
    /// Whatever shows loading UI and gates a transition on it — not necessarily a scene.
    /// <br/><br/>
    /// v4 required a loading screen to <i>be</i> a scene, and found its behaviour by scanning
    /// every loaded object on every transition. This is the contract that decouples the two:
    /// a screen can be a scene, a prefab, a UI Toolkit document, or anything else, as long as it
    /// can show itself, report progress, hide itself, and clean up after.
    /// </summary>
    /// <remarks>
    /// <b>An abstract class, not an interface, and not by preference.</b> C# forbids
    /// user-defined conversions to or from an interface type, and
    /// <c>TransitionAsync("target", "loading")</c> has to keep compiling — which needs an
    /// implicit <c>string → LoadingScreen</c> conversion, which needs a class. The same rule is
    /// why v4's <c>ILoadSceneInfo</c> never had conversions either.
    /// <br/><br/>
    /// Subclasses pass through with no conversion at all, which is the other half of why a base
    /// class wins here.
    /// </remarks>
    public abstract class LoadingScreen : IDisposable
    {
        /// <summary>
        /// Brings the screen up, and does not return until it is fully shown. The transition
        /// waits here before it unloads anything.
        /// </summary>
        /// <param name="operation">The transition being gated, so a stalled wait can name it.</param>
        public abstract SceneOperationPump.ConditionAwaiter ShowAsync(SceneOperation operation);

        /// <summary>
        /// Reports load progress, from 0 to 1.
        /// </summary>
        public abstract void ReportProgress(float progress);

        /// <summary>
        /// Takes the screen down, and does not return until it is fully hidden. Reaching this
        /// point is the answer to "when is the loading screen completely gone?".
        /// </summary>
        /// <param name="operation">The transition being gated, so a stalled wait can name it.</param>
        public abstract SceneOperationPump.ConditionAwaiter HideAsync(SceneOperation operation);

        /// <summary>
        /// Tears down whatever the screen created. Always called, including when the transition
        /// faults or is cancelled.
        /// </summary>
        public abstract void Dispose();

        /// <summary>
        /// Called once the screen's content exists, so it can find whatever reports progress.
        /// <br/>
        /// Separate from construction because a screen's content usually does not exist until
        /// the transition puts it somewhere — see <see cref="LoadingScreenHost"/>.
        /// </summary>
        /// <param name="host">The scene the screen may instantiate content into.</param>
        public abstract SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation);

        public static implicit operator LoadingScreen(string nameOrPathOrAddress) => new SceneLoadingScreen(nameOrPathOrAddress);
        public static implicit operator LoadingScreen(SceneRef sceneRef) => sceneRef.IsValid ? new SceneLoadingScreen(sceneRef) : null;
        public static implicit operator LoadingScreen(int buildIndex) => new SceneLoadingScreen(buildIndex);
        public static implicit operator LoadingScreen(Scene scene) => new SceneLoadingScreen(scene);
#if ENABLE_ADDRESSABLES
        // Not in #77's list, but v4's TransitionAddressableAsync took an AssetReference loading
        // scene, and there is no reason for that one reference kind to lose the conversion.
        public static implicit operator LoadingScreen(UnityEngine.AddressableAssets.AssetReference assetReference) => new SceneLoadingScreen(SceneRef.FromAssetReference(assetReference));
#endif
    }
}
