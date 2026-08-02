using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// The loading-screen abstraction: the conversions that keep v4's call sites compiling, the
    /// registry that replaced the scan, and the holder scene that replaced the temp scene.
    /// </summary>
    public class LoadingScreenTests : SceneTestBase
    {
        ISceneManager Manager => SceneTestEnvironment.SceneManagers[0];

        [OneTimeSetUp]
        public void OneTimeSetup() => SceneTestEnvironment.ValidateSceneEnvironment();

        [Test]
        public void Conversions_ProduceASceneLoadingScreen()
        {
            AssertSceneScreen(SceneBuilder.SceneNames[0]);
            AssertSceneScreen(0);
            AssertSceneScreen((SceneRef)SceneBuilder.SceneNames[0]);

            static void AssertSceneScreen(LoadingScreen screen) => Assert.IsInstanceOf<SceneLoadingScreen>(screen);
        }

        /// <summary>
        /// <c>default(SceneRef)</c> is how "no loading screen" is spelled, so it must convert to
        /// null rather than to a screen pointing at nothing.
        /// </summary>
        [Test]
        public void Conversion_FromAnEmptySceneRef_IsNull()
        {
            LoadingScreen screen = default(SceneRef);
            Assert.IsNull(screen);
        }

        /// <summary>
        /// The other half of why <see cref="LoadingScreen"/> is a class: a subclass needs no
        /// conversion at all.
        /// </summary>
        [UnityTest]
        public IEnumerator CustomScreen_PassesThroughWithoutConversion()
        {
            yield return Manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();

            RecordingLoadingScreen screen = new();
            yield return Manager.TransitionAsync(SceneBuilder.SceneNames[2], screen).ToCoroutine();

            Assert.True(screen.Prepared, "The screen should have been prepared.");
            Assert.True(screen.Shown, "The screen should have been shown.");
            Assert.True(screen.Hidden, "The screen should have been hidden.");
            Assert.True(screen.Disposed, "The screen should have been disposed.");
            Assert.Greater(screen.ProgressReports, 0, "The screen should have received progress.");
        }

        /// <summary>
        /// Whatever the transition does, a screen that created something has to get it back.
        /// </summary>
        [UnityTest]
        public IEnumerator CustomScreen_IsDisposed_EvenWhenTheTransitionFaults()
        {
            yield return Manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();

            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("faulted during"));

            RecordingLoadingScreen screen = new();
            SceneOperation operation = Manager.TransitionAsync("not-a-real-scene", screen);

            yield return new WaitUntil(() => operation.IsDone);

            Assert.AreEqual(SceneOperationState.Faulted, operation.State);
            Assert.True(screen.Disposed, "A faulted transition must still dispose the screen.");
        }

        [UnityTest]
        public IEnumerator Registry_FindsTheBehaviorForItsOwnScene()
        {
            yield return Manager.LoadAsync(SceneBuilder.SceneNames[0]).ToCoroutine();
            Scene loadingScene = Manager.GetLastLoadedScene();

            Assert.True(LoadingBehaviorRegistry.TryGet(loadingScene, out LoadingBehavior behavior));
            Assert.AreEqual(loadingScene, behavior.gameObject.scene);

            // sceneA has no LoadingBehavior in it.
            yield return Manager.LoadAsync(SceneBuilder.SceneNames[1]).ToCoroutine();
            Assert.False(LoadingBehaviorRegistry.TryGet(Manager.GetLastLoadedScene(), out _));
        }

        [Test]
        public void Registry_IsClearedBetweenPlaySessions()
        {
            LoadingBehaviorRegistry.ResetStatics();

            Assert.False(LoadingBehaviorRegistry.TryGet(SceneManager.GetActiveScene(), out _));
        }

        /// <summary>
        /// The reason the holder scene exists at all: Unity cannot have zero loaded scenes, and a
        /// transition from a single scene unloads the only one there is before loading the next.
        /// </summary>
        [UnityTest]
        public IEnumerator Transition_FromASingleScene_NeverDropsToZeroLoadedScenes()
        {
            yield return Manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();

            int lowest = SceneManager.sceneCount;
            SceneOperation operation = Manager.TransitionAsync(SceneBuilder.SceneNames[2]);

            while (!operation.IsDone)
            {
                lowest = Mathf.Min(lowest, SceneManager.sceneCount);
                yield return null;
            }

            Assert.Greater(lowest, 0, "The engine must always have at least one loaded scene.");
        }

        [UnityTest]
        public IEnumerator HolderScene_IsGoneOnceTheTransitionFinishes()
        {
            yield return Manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();
            yield return Manager.TransitionAsync(SceneBuilder.SceneNames[2], SceneBuilder.SceneNames[0]).ToCoroutine();

            yield return new WaitUntil(() => !IsHolderSceneLoaded());
            Assert.False(IsHolderSceneLoaded());

            static bool IsHolderSceneLoaded()
            {
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    if (SceneManager.GetSceneAt(i).name == LoadingScreenHost.SceneName)
                        return true;
                return false;
            }
        }

        /// <summary>
        /// A loading scene with no <see cref="LoadingBehavior"/> gates on nothing and the
        /// transition still completes — the v4 <c>TransitionWithIntermediateNoLoadingAsync</c> path.
        /// </summary>
        [UnityTest]
        public IEnumerator Transition_WithALoadingSceneThatHasNoBehavior_StillCompletes()
        {
            yield return Manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();

            SceneOperation operation = Manager.TransitionAsync(SceneBuilder.SceneNames[2], SceneBuilder.SceneNames[3]);
            yield return operation.ToCoroutine();

            Assert.AreEqual(SceneOperationState.Completed, operation.State);
            Assert.AreEqual(1, Manager.LoadedSceneCount);
        }

        /// <summary>
        /// A screen that records what the transition asked of it, and gates on nothing.
        /// </summary>
        class RecordingLoadingScreen : LoadingScreen
        {
            public bool Prepared;
            public bool Shown;
            public bool Hidden;
            public bool Disposed;
            public int ProgressReports;

            public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
            {
                Prepared = true;
                Assert.True(host.Scene.IsValid(), "The screen should be given a valid host scene.");
                return SceneOperationPump.Completed(operation);
            }

            public override SceneOperationPump.ConditionAwaiter ShowAsync(SceneOperation operation)
            {
                Shown = true;
                return SceneOperationPump.Completed(operation);
            }

            public override void ReportProgress(float progress) => ProgressReports++;

            public override SceneOperationPump.ConditionAwaiter HideAsync(SceneOperation operation)
            {
                Hidden = true;
                return SceneOperationPump.Completed(operation);
            }

            public override void Dispose() => Disposed = true;
        }
    }
}
