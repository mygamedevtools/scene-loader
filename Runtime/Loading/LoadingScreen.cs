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
    /// <br/><br/>
    /// <see cref="PrepareAsync"/> is the only member a screen has to write, plus
    /// <see cref="Dispose"/> if it built anything. Showing, hiding and reporting are driven by the
    /// <see cref="LoadingProgress"/> the screen binds while preparing, so every screen gates the
    /// same way rather than reimplementing it — and a screen that binds nothing gates on nothing.
    /// </remarks>
    public abstract class LoadingScreen : IDisposable
    {
        /// <summary>
        /// The progress and gates this screen drives, or <see langword="null"/> if it binds none.
        /// </summary>
        protected LoadingProgress Progress { get; private set; }

        /// <summary>
        /// Adopts the progress this screen gates on, from <see cref="PrepareAsync"/> — either one
        /// found on a <see cref="LoadingBehavior"/>, or one the screen creates for itself. Passing
        /// <see langword="null"/> means the screen holds the transition up for nothing.
        /// </summary>
        protected void BindProgress(LoadingProgress progress) => Progress = progress;

        /// <summary>
        /// Builds the screen's content. Separate from construction because it usually cannot
        /// exist until the transition gives it a <paramref name="host"/> to live in.
        /// </summary>
        public abstract SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation);

        /// <summary>Brings the screen up. The transition waits here before it unloads anything.</summary>
        public virtual SceneOperationPump.ConditionAwaiter ShowAsync(SceneOperation operation)
        {
            return Progress == null ? SceneOperationPump.Completed(operation) : Progress.WaitForShowAsync(operation);
        }

        /// <summary>Reports load progress, from 0 to 1.</summary>
        public virtual void ReportProgress(float progress)
        {
            Progress?.Report(progress);
        }

        /// <summary>Takes the screen down. Returning is "the loading screen is completely gone".</summary>
        public virtual SceneOperationPump.ConditionAwaiter HideAsync(SceneOperation operation)
        {
            if (Progress == null)
                return SceneOperationPump.Completed(operation);

            Progress.SetLoadingCompleted();
            return Progress.WaitForHideAsync(operation);
        }

        /// <summary>
        /// Tears down whatever the screen created. Always called, fault and cancel included, so
        /// overrides should call <c>base.Dispose()</c>.
        /// </summary>
        public virtual void Dispose()
        {
            Progress = null;
        }

        public static implicit operator LoadingScreen(string nameOrPathOrAddress) => new SceneLoadingScreen(nameOrPathOrAddress);
        public static implicit operator LoadingScreen(SceneRef sceneRef) => sceneRef.IsValid ? new SceneLoadingScreen(sceneRef) : null;
        public static implicit operator LoadingScreen(int buildIndex) => new SceneLoadingScreen(buildIndex);
        public static implicit operator LoadingScreen(Scene scene) => new SceneLoadingScreen(scene);
#if ENABLE_ADDRESSABLES
        // An addressable loading screen is addressed like any other scene, so the same conversion
        // has to reach an AssetReference.
        public static implicit operator LoadingScreen(UnityEngine.AddressableAssets.AssetReference assetReference) => new SceneLoadingScreen(SceneRef.FromAssetReference(assetReference));
#endif
    }
}
