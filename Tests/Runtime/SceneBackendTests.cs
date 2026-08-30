using System;
using System.Collections;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// The backend contract and the linking layer built on it: that selection routes correctly,
    /// and that the one method whose answer differs per backend —
    /// <see cref="ISceneBackend.TryResolveScene"/> — differs by returning <see langword="false"/>
    /// rather than by warning and handing back a default.
    /// </summary>
    [PrebuildSetup(typeof(SceneTestEnvironment)), PostBuildCleanup(typeof(SceneTestEnvironment))]
    public class SceneBackendTests : SceneTestBase
    {
        SceneLogLevel _originalLevel;

        [SetUp]
        public void SetUp()
        {
            _originalLevel = SceneManagerLog.Level;
            // The test scene names are double matches, and the resolution warning is not the
            // subject of any test here.
            SceneManagerLog.Level = SceneLogLevel.Error;
        }

        [TearDown]
        public void TearDown()
        {
            SceneManagerLog.Level = _originalLevel;
        }

        [Test]
        public void BackendSelection_RoutesEachKindToItsBackend()
        {
            Assert.IsInstanceOf<StandardSceneBackend>(SceneBackendRegistry.GetBackend(SceneRefKind.BuildIndex));
            Assert.IsInstanceOf<StandardSceneBackend>(SceneBackendRegistry.GetBackend(SceneRefKind.Scene));
#if ENABLE_ADDRESSABLES
            Assert.IsInstanceOf<AddressablesSceneBackend>(SceneBackendRegistry.GetBackend(SceneRefKind.Address));
            Assert.IsInstanceOf<AddressablesSceneBackend>(SceneBackendRegistry.GetBackend(SceneRefKind.AssetReference));
#endif
        }

        // Reaching selection with an unresolved key means the resolver was skipped — worth an
        // explicit error rather than a silently-wrong backend.
        [Test]
        public void BackendSelection_RejectsAnUnresolvedKey()
        {
            Assert.Throws<ArgumentException>(() => SceneBackendRegistry.GetBackend(SceneRefKind.Key));
            Assert.Throws<ArgumentException>(() => SceneBackendRegistry.GetBackend(SceneRefKind.None));
        }

        [Test]
        public void Register_TakesPrecedenceOverTheDefaults()
        {
            ISceneBackend custom = new EverythingBackend();
            try
            {
                SceneBackendRegistry.Register(custom);
                Assert.AreSame(custom, SceneBackendRegistry.GetBackend(SceneRefKind.BuildIndex));
            }
            finally
            {
                SceneBackendRegistry.ResetStatics();
            }
        }

        [UnityTest]
        public IEnumerator Standard_CannotNameItsOwnScene_AndReportsNormalizedProgress()
        {
            ISceneBackend backend = SceneBackendRegistry.GetBackend(SceneRefKind.BuildIndex);
            Assert.True(SceneRefResolver.TryResolveImmediate(SceneBuilder.SceneNames[1], out SceneRef sceneRef));

            SceneBackendHandle handle = backend.Load(sceneRef);

            yield return AssertProgressStaysNormalized(backend, handle);

            // The Unity Scene Manager has no API for this, so the honest answer is "no".
            Assert.False(backend.TryResolveScene(handle, out Scene scene));
            Assert.False(scene.IsValid());

            yield return UnloadEverythingLoaded();
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator Addressable_NamesItsOwnScene_AndReportsNormalizedProgress()
        {
            ISceneBackend backend = SceneBackendRegistry.GetBackend(SceneRefKind.Address);
            SceneBackendHandle handle = backend.Load(SceneRef.Address(SceneBuilder.SceneNames[1]));

            yield return AssertProgressStaysNormalized(backend, handle);

            Assert.True(backend.TryResolveScene(handle, out Scene scene));
            Assert.True(scene.IsValid());
            Assert.AreEqual(SceneBuilder.SceneNames[1], scene.name);

            SceneBackendHandle[] unloadHandles = { backend.Unload(handle.WithScene(scene)) };
            while (!SceneLinker.HasCompletedAll(unloadHandles))
                yield return null;
        }
#endif

        /// <summary>Two references to the same source scene must link to two different loaded scenes.</summary>
        [UnityTest]
        public IEnumerator Linking_HandlesTwoReferencesToTheSameSourceScene()
        {
            ISceneManager manager = SceneTestEnvironment.SceneManagers[0];
            SceneRef[] sceneRefs = { SceneBuilder.SceneNames[1], SceneBuilder.ScenePaths[1] };

            yield return manager.LoadAsync(new SceneParameters(sceneRefs)).ToCoroutine();

            Assert.AreEqual(2, manager.LoadedSceneCount);
            Assert.True(manager.TryGetLoadedSceneAt(0, out Scene first));
            Assert.True(manager.TryGetLoadedSceneAt(1, out Scene second));
            Assert.AreNotEqual(first, second);
        }

        /// <summary>A handle that cannot match anything must fail loudly and say what did not link.</summary>
        [Test]
        public void Linking_Failure_ThrowsNamingTheReference()
        {
            ISceneBackend backend = SceneBackendRegistry.GetBackend(SceneRefKind.BuildIndex);
            SceneRef unmatchable = SceneRef.FromBuildIndex(int.MaxValue);
            SceneBackendHandle[] handles = { SceneBackendHandle.ForStandard(backend, unmatchable, default, null) };

            Exception exception = Assert.Throws<Exception>(() => SceneLinker.Link(handles, Array.Empty<SceneBackendHandle>()));
            Assert.That(exception.Message, Does.Contain(int.MaxValue.ToString()));

            // The throw is the only report: logging it here too would duplicate the error that
            // SceneOperation.Fault emits once this exception reaches it.
            LogAssert.NoUnexpectedReceived();
        }

        static IEnumerator AssertProgressStaysNormalized(ISceneBackend backend, SceneBackendHandle handle)
        {
            float progress = backend.GetProgress(handle);
            Assert.GreaterOrEqual(progress, 0f);
            Assert.LessOrEqual(progress, 1f);

            while (!backend.IsDone(handle))
            {
                progress = backend.GetProgress(handle);
                Assert.GreaterOrEqual(progress, 0f, "Progress went below 0.");
                Assert.LessOrEqual(progress, 1f, "Progress went above 1.");
                yield return null;
            }

            Assert.AreEqual(1f, backend.GetProgress(handle), 0.001f, "Progress should reach 1 once the operation is done.");
        }

        // These drive backends directly rather than through a manager, so the scenes they load
        // are nobody's to clean up but theirs.
        static IEnumerator UnloadEverythingLoaded()
        {
            yield return SceneTestUtilities.UnloadRemainingScenes();
        }

        /// <summary>Claims every kind, to prove registration order decides precedence.</summary>
        class EverythingBackend : ISceneBackend
        {
            public bool CanHandle(SceneRefKind kind) => true;
            public SceneBackendHandle Load(SceneRef sceneRef) => throw new NotSupportedException();
            public SceneBackendHandle Unload(SceneBackendHandle handle) => throw new NotSupportedException();
            public float GetProgress(SceneBackendHandle handle) => 0f;
            public bool IsDone(SceneBackendHandle handle) => true;
            public bool TryResolveScene(SceneBackendHandle handle, out Scene scene)
            {
                scene = default;
                return false;
            }
        }
    }
}
