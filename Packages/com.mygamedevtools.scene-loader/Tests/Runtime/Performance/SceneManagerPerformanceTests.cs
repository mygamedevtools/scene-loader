using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests.Performance
{
    /// <summary>
    /// The allocation and timing baseline the v5 rework is measured against. Every case reports
    /// its figures without asserting on them — see <see cref="AllocationProbe"/>.
    /// </summary>
    public class SceneManagerPerformanceTests : SceneTestBase
    {
        static readonly SceneRef _targetScene = SceneBuilder.SceneNames[1];
        static readonly SceneRef _loadingScene = SceneBuilder.SceneNames[0];

        // Four references that resolve to distinct scenes, so the linking layer has real work
        // to do rather than matching a single operation.
        static readonly SceneRef[] _multipleScenes = new SceneRef[]
        {
            SceneBuilder.SceneNames[1],
            2,
            SceneBuilder.ScenePaths[3],
            SceneBuilder.SceneNames[0],
        };

#if ENABLE_ADDRESSABLES
        static readonly SceneRef _addressableTargetScene = SceneRef.Address(SceneBuilder.SceneNames[1]);
        static readonly SceneRef _addressableLoadingScene = SceneRef.Address(SceneBuilder.SceneNames[0]);
#endif

        ISceneManager Manager => SceneTestEnvironment.SceneManagers[0];

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            SceneTestEnvironment.ValidateSceneEnvironment();
        }

        [UnityTest, Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Transition_WithLoadingScreen()
        {
            yield return LoadSourceScene();

            yield return AllocationProbe.Measure(
                nameof(Transition_WithLoadingScreen),
                setup: null,
                operation: () => Manager.TransitionAsync(new SceneParameters(_targetScene, true), _loadingScene).ToWaitTask(),
                teardown: null);
        }

        [UnityTest, Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Transition_Direct()
        {
            yield return LoadSourceScene();

            // With a single loaded scene and no intermediate, this takes the branch that
            // creates and destroys the "temp-transition-scene" every time.
            yield return AllocationProbe.Measure(
                nameof(Transition_Direct),
                setup: null,
                operation: () => Manager.TransitionAsync(new SceneParameters(_targetScene, true)).ToWaitTask(),
                teardown: null);
        }

        [UnityTest, Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Load_Single()
        {
            yield return AllocationProbe.Measure(
                nameof(Load_Single),
                setup: null,
                operation: () => Manager.LoadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                teardown: () => Manager.UnloadAsync(new SceneParameters(_targetScene)).ToWaitTask());
        }

        [UnityTest, Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Load_Multiple()
        {
            yield return AllocationProbe.Measure(
                nameof(Load_Multiple),
                setup: null,
                operation: () => Manager.LoadAsync(new SceneParameters(_multipleScenes)).ToWaitTask(),
                teardown: () => Manager.UnloadAsync(new SceneParameters(_multipleScenes)).ToWaitTask());
        }

        [UnityTest, Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Unload_Single()
        {
            yield return AllocationProbe.Measure(
                nameof(Unload_Single),
                setup: () => Manager.LoadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                operation: () => Manager.UnloadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                teardown: null);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest, Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Load_Single_Addressable()
        {
            yield return AllocationProbe.Measure(
                nameof(Load_Single_Addressable),
                setup: null,
                operation: () => Manager.LoadAsync(new SceneParameters(_addressableTargetScene)).ToWaitTask(),
                teardown: () => Manager.UnloadAsync(new SceneParameters(_addressableTargetScene)).ToWaitTask());
        }

        [UnityTest, Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Transition_WithLoadingScreen_Addressable()
        {
            yield return LoadSourceScene();

            yield return AllocationProbe.Measure(
                nameof(Transition_WithLoadingScreen_Addressable),
                setup: null,
                operation: () => Manager.TransitionAsync(new SceneParameters(_addressableTargetScene, true), _addressableLoadingScene).ToWaitTask(),
                teardown: null);
        }
#endif

        // Loaded outside the measured loop, so every iteration starts from the same steady
        // state: one loaded scene in, one loaded scene out.
        IEnumerator LoadSourceScene()
        {
            yield return Manager.LoadAsync(new SceneParameters(_targetScene, true)).ToWaitTask();
        }
    }
}
