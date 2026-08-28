using System.Collections;
using NUnit.Framework;
using Unity.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    /// <summary>
    /// The shapes of <see cref="SceneRef"/> and the <see cref="SceneParameters"/> conversions.
    /// <c>SceneManager_ConversionTests</c> proves they reach the right operation.
    /// </summary>
    public class SceneRefConversionTests
    {
        [Test]
        public void Default_PointsAtNothing()
        {
            SceneRef sceneRef = default;

            Assert.AreEqual(SceneRefKind.None, sceneRef.Kind);
            Assert.False(sceneRef.IsValid);
        }

        [Test]
        public void String_ProducesAnUnresolvedKey()
        {
            SceneRef sceneRef = "sceneA";

            Assert.AreEqual(SceneRefKind.Key, sceneRef.Kind);
            Assert.AreEqual("sceneA", sceneRef.Key);
            Assert.True(sceneRef.IsValid);
        }

        [Test]
        public void Address_ProducesAnAddress()
        {
            SceneRef sceneRef = SceneRef.Address("sceneA");

            Assert.AreEqual(SceneRefKind.Address, sceneRef.Kind);
            Assert.AreEqual("sceneA", sceneRef.Key);
        }

        [Test]
        public void Int_ProducesABuildIndex()
        {
            SceneRef sceneRef = 3;

            Assert.AreEqual(SceneRefKind.BuildIndex, sceneRef.Kind);
            Assert.AreEqual(3, sceneRef.BuildIndex);
        }

        [UnityTest]
        public IEnumerator Scene_ProducesAScene()
        {
            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            Scene scene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            SceneRef sceneRef = scene;

            Assert.AreEqual(SceneRefKind.Scene, sceneRef.Kind);
            Assert.AreEqual(scene, sceneRef.Scene);
            Assert.True(sceneRef.CanBeReferenceToScene(scene));

            yield return SceneManager.UnloadSceneAsync(scene);
        }

        [Test]
        public void EmptyString_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => _ = (SceneRef)"");
            Assert.Throws<System.ArgumentException>(() => _ = (SceneRef)"   ");
            Assert.Throws<System.ArgumentException>(() => SceneRef.Address(null));
        }

        [Test]
        public void NegativeBuildIndex_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => _ = (SceneRef)(-1));
        }

        [Test]
        public void InvalidScene_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => _ = (SceneRef)default(Scene));
        }

        [Test]
        public void Equality_HoldsWithinAKindAndNeverAcrossKinds()
        {
            Assert.AreEqual((SceneRef)"sceneA", (SceneRef)"sceneA");
            Assert.AreEqual(SceneRef.Address("sceneA"), SceneRef.Address("sceneA"));
            Assert.AreEqual((SceneRef)1, (SceneRef)1);
            Assert.AreEqual(default(SceneRef), default(SceneRef));

            Assert.AreNotEqual((SceneRef)"sceneA", (SceneRef)"sceneB");
            Assert.AreNotEqual((SceneRef)1, (SceneRef)2);

            // Same string, different meaning: this is the whole point of SceneRef.Address.
            Assert.AreNotEqual((SceneRef)"sceneA", SceneRef.Address("sceneA"));
        }

        [Test]
        public void GetHashCode_AgreesWithEquality()
        {
            Assert.AreEqual(((SceneRef)"sceneA").GetHashCode(), ((SceneRef)"sceneA").GetHashCode());
            Assert.AreEqual(SceneRef.Address("sceneA").GetHashCode(), SceneRef.Address("sceneA").GetHashCode());
            Assert.AreEqual(((SceneRef)7).GetHashCode(), ((SceneRef)7).GetHashCode());
            Assert.AreEqual(default(SceneRef).GetHashCode(), default(SceneRef).GetHashCode());
        }

        [Test]
        public void CanBeReferenceToScene_IsFalseForAddressableKinds()
        {
            // An address says nothing about the resulting scene's name, so the addressable
            // backend hands its Scene back directly rather than being matched after the fact.
            Assert.False(SceneRef.Address("sceneA").CanBeReferenceToScene(default));
            Assert.False(default(SceneRef).CanBeReferenceToScene(default));
        }

        [Test]
        public void SceneParameters_ConvertsFromEverySingleSourceType()
        {
            AssertSingle("sceneA", SceneRefKind.Key);
            AssertSingle(3, SceneRefKind.BuildIndex);
            AssertSingle((SceneRef)"sceneA", SceneRefKind.Key);
            AssertSingle(SceneRef.Address("sceneA"), SceneRefKind.Address);

            static void AssertSingle(SceneParameters parameters, SceneRefKind expectedKind)
            {
                Assert.AreEqual(1, parameters.Length);
                Assert.AreEqual(expectedKind, parameters.GetSceneRef().Kind);
                Assert.False(parameters.ShouldSetActive(), "A bare conversion must not silently activate the scene.");
            }
        }

        [Test]
        public void SceneParameters_ConvertsFromEveryArraySourceType()
        {
            AssertArray(new[] { "sceneA", "sceneB" }, SceneRefKind.Key);
            AssertArray(new[] { 1, 2 }, SceneRefKind.BuildIndex);
            AssertArray(new SceneRef[] { "sceneA", 2 }, SceneRefKind.Key);

            static void AssertArray(SceneParameters parameters, SceneRefKind expectedFirstKind)
            {
                Assert.AreEqual(2, parameters.Length);
                Assert.AreEqual(expectedFirstKind, parameters.GetSceneRefs()[0].Kind);
                Assert.False(parameters.ShouldSetActive());
            }
        }

        [Test]
        public void SceneParameters_ActiveIndexBeyondTheArray_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => _ = new SceneParameters(new SceneRef[] { "sceneA" }, 1));
        }

        [Test]
        public void SceneParameters_EmptyOrNull_Throws()
        {
            Assert.Throws<System.ArgumentException>(() => _ = new SceneParameters(System.Array.Empty<SceneRef>()));
            Assert.Throws<System.ArgumentException>(() => _ = new SceneParameters((SceneRef[])null));
        }

        // Converting a build index in and reading it back out must not box it. That is a claim the
        // API makes, so it gets asserted rather than assumed.
        [UnityTest]
        public IEnumerator SceneRef_RoundTripsWithoutAllocating()
        {
            ProfilerRecorder recorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            if (!recorder.Valid)
            {
                recorder.Dispose();
                Assert.Ignore("The 'GC Allocated In Frame' profiler counter is unavailable on this runtime.");
            }

            // Warm up: first-call costs would otherwise land inside the measured frame.
            RoundTrip(1000);
            yield return null;

            long before = recorder.CurrentValue;
            RoundTrip(100_000);
            yield return null;
            long allocated = recorder.CurrentValue - before;

            recorder.Dispose();

            Assert.Less(allocated, 2048, $"100,000 SceneRef round-trips allocated {allocated:N0} bytes. Something is boxing.");

            static void RoundTrip(int count)
            {
                int sink = 0;
                for (int i = 0; i < count; i++)
                {
                    SceneRef sceneRef = i % 128;
                    sink += sceneRef.BuildIndex + (sceneRef.Equals((SceneRef)(i % 128)) ? 1 : 0);
                }
                Assert.NotZero(sink);
            }
        }
    }
}
