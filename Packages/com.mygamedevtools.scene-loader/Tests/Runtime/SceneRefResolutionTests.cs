using System.Collections;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// What a bare string means, and how it is decided — the one v5 change that can make working
    /// code keep compiling and start doing something different. Every test scene name exists in
    /// both the build settings and Addressables, so precedence is exercised everywhere else too.
    /// </summary>
    [PrebuildSetup(typeof(SceneTestEnvironment)), PostBuildCleanup(typeof(SceneTestEnvironment))]
    public class SceneRefResolutionTests
    {
        SceneLogLevel _originalLevel;

        [SetUp]
        public void SetUp()
        {
            _originalLevel = SceneManagerLog.Level;
            // Resolution answers are cached for the session, and these tests care about the
            // probe itself rather than the cache.
            SceneRefResolver.Invalidate();
        }

        [TearDown]
        public void TearDown()
        {
            SceneManagerLog.Level = _originalLevel;
            SceneRefResolver.Invalidate();
        }

        [UnityTest]
        public IEnumerator NameInBuildSettings_ResolvesToTheStandardBackend()
        {
            // Warnings off: this name is also an address, and the double-match warning is the
            // subject of its own test rather than noise in this one.
            SceneManagerLog.Level = SceneLogLevel.Error;

            yield return Resolve(SceneBuilder.SceneNames[1], resolved =>
            {
                Assert.AreEqual(SceneRefKind.BuildIndex, resolved.Kind);
                Assert.AreEqual(SceneBuilder.SceneNames[1], resolved.Key);
            });
        }

        [UnityTest]
        public IEnumerator FullPath_Resolves()
        {
            SceneManagerLog.Level = SceneLogLevel.Error;

            yield return Resolve(SceneBuilder.ScenePaths[1], resolved =>
            {
                Assert.AreEqual(SceneRefKind.BuildIndex, resolved.Kind);
                Assert.AreEqual(SceneBuilder.ScenePaths[1], resolved.Key);
            });
        }

        [Test]
        public void BuildIndex_NeedsNoResolution()
        {
            Assert.True(SceneRefResolver.TryResolveImmediate(3, out SceneRef resolved));
            Assert.AreEqual(SceneRefKind.BuildIndex, resolved.Kind);
            Assert.AreEqual(3, resolved.BuildIndex);
        }

        [Test]
        public void ExplicitAddress_NeedsNoResolution_AndOverridesTheBuildSettings()
        {
            // SceneNames[1] is in the build settings, so a bare string would go standard.
            SceneRef forced = SceneRef.Address(SceneBuilder.SceneNames[1]);

            Assert.True(SceneRefResolver.TryResolveImmediate(forced, out SceneRef resolved));
            Assert.AreEqual(SceneRefKind.Address, resolved.Kind);
        }

        [UnityTest]
        public IEnumerator NameInBoth_ResolvesToTheStandardBackend_AndWarns()
        {
            SceneManagerLog.Level = SceneLogLevel.Warning;
            LogAssert.Expect(LogType.Warning, new Regex("matches both the build settings and an Addressables entry"));

            yield return Resolve(SceneBuilder.SceneNames[1], resolved => Assert.AreEqual(SceneRefKind.BuildIndex, resolved.Kind));
        }

        [UnityTest]
        public IEnumerator SecondResolution_HitsTheCacheAndDoesNotProbeAgain()
        {
            SceneManagerLog.Level = SceneLogLevel.Warning;

            // The double-match warning is emitted once per key, on the resolution that populates
            // the cache. A second warning here would mean the cache was not consulted.
            LogAssert.Expect(LogType.Warning, new Regex("matches both the build settings and an Addressables entry"));

            yield return Resolve(SceneBuilder.SceneNames[1], _ => { });
            yield return Resolve(SceneBuilder.SceneNames[1], _ => { });

            LogAssert.NoUnexpectedReceived();
        }

#if ENABLE_ADDRESSABLES
        [UnityTest]
        public IEnumerator AddressOnly_ResolvesToTheAddressableBackend()
        {
            SceneManagerLog.Level = SceneLogLevel.Error;

            SceneRef sceneRef = SceneRef.Address(SceneBuilder.SceneNames[2]);
            Assert.True(SceneRefResolver.TryResolveImmediate(sceneRef, out SceneRef resolved));
            Assert.AreEqual(SceneRefKind.Address, resolved.Kind);
            yield break;
        }
#endif

        [UnityTest]
        public IEnumerator InNeither_ThrowsNamingBothLookups()
        {
            Task<SceneRef> task = SceneRefResolver.ResolveAsync("not-a-real-scene-anywhere");
            yield return new WaitUntil(() => task.IsCompleted);

            Assert.NotNull(task.Exception);
            string message = task.Exception.InnerException.Message;
            Assert.That(message, Does.Contain("build settings"));
#if ENABLE_ADDRESSABLES
            Assert.That(message, Does.Contain("Addressables"));
#endif
        }

        /// <summary>
        /// Half of the "adding a scene later changes the backend" caveat: the map is derived from
        /// the live build-settings list, not a snapshot. <see cref="Invalidate_ForcesAReProbe"/>
        /// is the other half. Driving it end to end is not possible in play mode — the test
        /// runner owns that list during a run and re-asserts its own entry over any change.
        /// </summary>
        [Test]
        public void BuildSettingsMap_CoversEveryLiveEntryByPathAndByName()
        {
            SceneManagerLog.Level = SceneLogLevel.Error;

            int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
            Assert.NotZero(count, "The test environment should have put its scenes in the build settings.");

            for (int i = 0; i < count; i++)
            {
                string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);

                Assert.True(SceneRefResolver.TryResolveImmediate(path, out SceneRef byPath), $"'{path}' did not resolve by path.");
                Assert.AreEqual(i, byPath.BuildIndex);

                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                Assert.True(SceneRefResolver.TryResolveImmediate(name, out SceneRef byName), $"'{name}' did not resolve by name.");
                Assert.AreEqual(i, byName.BuildIndex);
            }
        }

        /// <summary>
        /// The other half: the cache is droppable, and dropping it re-probes. The double-match
        /// warning is the signal — it fires on the resolution that populates the cache, never on a hit.
        /// </summary>
        [UnityTest]
        public IEnumerator Invalidate_ForcesAReProbe()
        {
            SceneManagerLog.Level = SceneLogLevel.Warning;
            LogAssert.Expect(LogType.Warning, new Regex("matches both the build settings and an Addressables entry"));

            yield return Resolve(SceneBuilder.SceneNames[1], _ => { });

            SceneRefResolver.Invalidate();

            LogAssert.Expect(LogType.Warning, new Regex("matches both the build settings and an Addressables entry"));
            yield return Resolve(SceneBuilder.SceneNames[1], _ => { });
        }

        static IEnumerator Resolve(SceneRef sceneRef, System.Action<SceneRef> assert)
        {
            Task<SceneRef> task = SceneRefResolver.ResolveAsync(sceneRef);
            yield return new WaitUntil(() => task.IsCompleted);

            if (task.Exception != null)
                throw task.Exception;

            assert(task.Result);
        }
    }
}
