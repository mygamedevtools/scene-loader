using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// The conversions that let a scene name stand in for a loading screen, the registry a screen
    /// finds its <see cref="LoadingBehavior"/> through, and the holder scene a screen that is not
    /// a scene lives in.
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

        /// <summary>"No loading screen" must convert to null, not to a screen pointing at nothing.</summary>
        [Test]
        public void Conversion_FromAnEmptySceneRef_IsNull()
        {
            LoadingScreen screen = default(SceneRef);
            Assert.IsNull(screen);
        }

        /// <summary>The other half of why <see cref="LoadingScreen"/> is a class.</summary>
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

        /// <summary>Whatever the transition does, a screen that created something gets it back.</summary>
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

        /// <summary>Why the holder scene exists: Unity cannot have zero loaded scenes.</summary>
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

        /// <summary>A loading scene with no <see cref="LoadingBehavior"/> gates on nothing.</summary>
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
        /// How a prefab screen finds its own behaviour: by hierarchy, through the same registry
        /// the scene lookup uses.
        /// </summary>
        [Test]
        public void Registry_FindsTheBehaviorBeneathARoot()
        {
            GameObject root = new("screen-root");
            GameObject child = new("child");
            child.transform.SetParent(root.transform);
            LoadingBehavior behavior = child.AddComponent<LoadingBehavior>();

            Assert.True(LoadingBehaviorRegistry.TryGet(root, out LoadingBehavior found));
            Assert.AreEqual(behavior, found);

            GameObject unrelated = new("unrelated");
            Assert.False(LoadingBehaviorRegistry.TryGet(unrelated, out _), "A behaviour outside the root is not part of that screen.");

            Object.DestroyImmediate(root);
            Object.DestroyImmediate(unrelated);
        }

        /// <summary>
        /// A prefab screen is instantiated in one scene and adopted into the holder scene, without
        /// its registration being renewed. Nothing may be keyed on the scene it started in.
        /// </summary>
        [UnityTest]
        public IEnumerator Registry_ResolvesABehaviorThatMovedToAnotherScene()
        {
            Scene origin = SceneManager.CreateScene("registry-origin");
            Scene destination = SceneManager.CreateScene("registry-destination");

            GameObject holder = new(nameof(LoadingBehavior));
            SceneManager.MoveGameObjectToScene(holder, origin);
            holder.AddComponent<LoadingBehavior>();

            Assert.True(LoadingBehaviorRegistry.TryGet(origin, out _), "It should resolve in the scene it was created in.");

            SceneManager.MoveGameObjectToScene(holder, destination);

            Assert.True(LoadingBehaviorRegistry.TryGet(destination, out LoadingBehavior moved), "It should resolve in the scene it moved to.");
            Assert.AreEqual(holder, moved.gameObject);
            Assert.False(LoadingBehaviorRegistry.TryGet(origin, out _), "Nothing should be left behind in the scene it came from.");

            Object.DestroyImmediate(holder);
            yield return SceneManager.UnloadSceneAsync(origin);
            yield return SceneManager.UnloadSceneAsync(destination);
        }

        /// <summary>
        /// The test of the whole arrangement: a screen with no <see cref="LoadingBehavior"/> and no
        /// MonoBehaviour anywhere still gates the transition, because gating lives on
        /// <see cref="LoadingProgress"/> rather than on a component.
        /// </summary>
        [UnityTest]
        public IEnumerator CustomScreen_CanGateOnAProgressItOwns()
        {
            yield return Manager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();

            SelfGatingLoadingScreen screen = new();
            SceneOperation operation = Manager.TransitionAsync(SceneBuilder.SceneNames[2], screen);

            yield return new WaitUntil(() => operation.State == SceneOperationState.ScreenIn);
            yield return null;

            Assert.False(operation.IsDone, "The screen holds the show gate, so the transition cannot proceed.");

            screen.Release();
            yield return operation.ToCoroutine();

            Assert.AreEqual(SceneOperationState.Completed, operation.State);
            Assert.Greater(screen.ProgressReports, 0, "The screen should have received progress through its own LoadingProgress.");
        }

        /// <summary>Owns its <see cref="LoadingProgress"/> instead of finding one on a behaviour.</summary>
        class SelfGatingLoadingScreen : LoadingScreen
        {
            public int ProgressReports;

            LoadingProgress _progress;

            public override SceneOperationPump.ConditionAwaiter PrepareAsync(LoadingScreenHost host, SceneOperation operation)
            {
                _progress = new LoadingProgress();
                _progress.Progressed += _ => ProgressReports++;
                _progress.HoldShow(this);

                BindProgress(_progress);

                return SceneOperationPump.Completed(operation);
            }

            public void Release() => _progress.ReleaseShow(this);
        }

        /// <summary>Records what the transition asked of it, and gates on nothing.</summary>
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

            public override void Dispose()
            {
                Disposed = true;
                base.Dispose();
            }
        }
    }
}
