using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests.Performance
{
    /// <summary>
    /// The allocation and timing baseline the v5 rework is measured against. Every case reports
    /// its figures without asserting on them — see <see cref="AllocationProbe"/>.
    /// </summary>
    /// <remarks>
    /// <b>Explicit, so a normal run skips it.</b> These take roughly fifty sequential scene
    /// operations to produce numbers nobody reads on an ordinary pull request, and this package
    /// allocates a fraction of a percent of the scene it is loading. Run them deliberately, when
    /// comparing one implementation against another — which is what the v5 rework needs and what
    /// a feature branch does not.
    /// <code>
    /// Unity -batchmode -runTests -testPlatform PlayMode \
    ///   -testFilter "MyGameDevTools.SceneLoading.Tests.Performance.*"
    /// </code>
    /// <c>[Order]</c> is not decoration: the ambient rung has to run before anything that
    /// subtracts it, and a cold first scene load costs several extra frames.
    /// </remarks>
    [Explicit("Allocation figures are for deliberate comparison runs, not every pull request.")]
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

        // A cold first scene operation spans several more frames than a warm one, which would
        // otherwise land entirely on whichever case happened to run first.
        [UnityTest, Order(0), Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Warmup()
        {
            for (int i = 0; i < 3; i++)
            {
                yield return Manager.LoadAsync(new SceneParameters(_targetScene)).ToWaitTask();
                yield return Manager.UnloadAsync(new SceneParameters(_targetScene)).ToWaitTask();
            }
        }

        [UnityTest, Order(1), Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Ambient()
        {
            yield return AllocationProbe.MeasureAmbient(30);
        }

        // The control. Drives the Unity Scene Manager directly, with none of this package
        // involved, so every case below can be read against what is unavoidable — and so a noisy
        // machine shows up as these moving too.
        [UnityTest, Order(2), Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Engine_Load()
        {
            yield return AllocationProbe.Measure(nameof(Engine_Load), null, EngineLoad, EngineUnload);
        }

        [UnityTest, Order(2), Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Engine_Unload()
        {
            yield return AllocationProbe.Measure(nameof(Engine_Unload), EngineLoad, EngineUnload, null);
        }

        [UnityTest, Order(3), Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Transition_WithLoadingScreen()
        {
            yield return LoadSourceScene();

            yield return AllocationProbe.Measure(
                nameof(Transition_WithLoadingScreen),
                setup: null,
                operation: () => Manager.TransitionAsync(new SceneParameters(_targetScene, true), _loadingScene).ToWaitTask(),
                teardown: null);
        }

        [UnityTest, Order(3), Performance, Timeout(AllocationProbe.TestTimeout)]
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

        [UnityTest, Order(3), Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Load_Single()
        {
            yield return AllocationProbe.Measure(
                nameof(Load_Single),
                setup: null,
                operation: () => Manager.LoadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                teardown: () => Manager.UnloadAsync(new SceneParameters(_targetScene)).ToWaitTask());
        }

        [UnityTest, Order(3), Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Load_Multiple()
        {
            yield return AllocationProbe.Measure(
                nameof(Load_Multiple),
                setup: null,
                operation: () => Manager.LoadAsync(new SceneParameters(_multipleScenes)).ToWaitTask(),
                teardown: () => Manager.UnloadAsync(new SceneParameters(_multipleScenes)).ToWaitTask());
        }

        [UnityTest, Order(3), Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Unload_Single()
        {
            yield return AllocationProbe.Measure(
                nameof(Unload_Single),
                setup: () => Manager.LoadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                operation: () => Manager.UnloadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                teardown: null);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest, Order(3), Performance, Timeout(AllocationProbe.TestTimeout)]
        public IEnumerator Load_Single_Addressable()
        {
            yield return AllocationProbe.Measure(
                nameof(Load_Single_Addressable),
                setup: null,
                operation: () => Manager.LoadAsync(new SceneParameters(_addressableTargetScene)).ToWaitTask(),
                teardown: () => Manager.UnloadAsync(new SceneParameters(_addressableTargetScene)).ToWaitTask());
        }

        [UnityTest, Order(3), Performance, Timeout(AllocationProbe.TestTimeout)]
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

        static int BuildIndex => SceneUtility.GetBuildIndexByScenePath(SceneBuilder.ScenePaths[1]);

        static IEnumerator EngineLoad()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(BuildIndex, LoadSceneMode.Additive);
            while (!operation.isDone)
                yield return null;
        }

        static IEnumerator EngineUnload()
        {
            AsyncOperation operation = SceneManager.UnloadSceneAsync(SceneManager.GetSceneByBuildIndex(BuildIndex));
            while (!operation.isDone)
                yield return null;
        }

        // Loaded outside the measured loop, so every iteration starts from the same steady
        // state: one loaded scene in, one loaded scene out.
        IEnumerator LoadSourceScene()
        {
            yield return Manager.LoadAsync(new SceneParameters(_targetScene, true)).ToWaitTask();
        }
    }
}
