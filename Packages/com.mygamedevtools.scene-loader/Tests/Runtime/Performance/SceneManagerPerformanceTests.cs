using System.Collections;
using NUnit.Framework;
using Unity.PerformanceTesting;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests.Performance
{
    /// <summary>
    /// The allocation and timing baseline the v5 rework is measured against. Non-addressable
    /// cases assert against a ceiling; addressable ones report only — see <see cref="AllocationGate"/>.
    /// </summary>
    public class SceneManagerPerformanceTests : SceneTestBase
    {
        static readonly ILoadSceneInfo _targetScene = new LoadSceneInfoName(SceneBuilder.SceneNames[1]);
        static readonly ILoadSceneInfo _loadingScene = new LoadSceneInfoName(SceneBuilder.SceneNames[0]);

        // Four references that resolve to distinct scenes, so the linking layer has real work
        // to do rather than matching a single operation.
        static readonly ILoadSceneInfo[] _multipleScenes = new ILoadSceneInfo[]
        {
            new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            new LoadSceneInfoIndex(2),
            new LoadSceneInfoName(SceneBuilder.ScenePaths[3]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[0]),
        };

#if ENABLE_ADDRESSABLES
        static readonly ILoadSceneInfo _addressableTargetScene = new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]);
        static readonly ILoadSceneInfo _addressableLoadingScene = new LoadSceneInfoAddress(SceneBuilder.SceneNames[0]);
#endif

        ISceneManager Manager => SceneTestEnvironment.SceneManagers[0];

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            SceneTestEnvironment.ValidateSceneEnvironment();
        }

        [UnityTest, Performance, Timeout(AllocationGate.TestTimeout)]
        public IEnumerator Transition_WithLoadingScreen()
        {
            yield return LoadSourceScene();

            yield return AllocationGate.Measure(
                nameof(Transition_WithLoadingScreen),
                setup: null,
                operation: () => Manager.TransitionAsync(new SceneParameters(_targetScene, true), _loadingScene).ToWaitTask(),
                teardown: null,
                AllocationGate.TransitionWithLoadingScreen);
        }

        [UnityTest, Performance, Timeout(AllocationGate.TestTimeout)]
        public IEnumerator Transition_Direct()
        {
            yield return LoadSourceScene();

            // With a single loaded scene and no intermediate, this takes the branch that
            // creates and destroys the "temp-transition-scene" every time.
            yield return AllocationGate.Measure(
                nameof(Transition_Direct),
                setup: null,
                operation: () => Manager.TransitionAsync(new SceneParameters(_targetScene, true)).ToWaitTask(),
                teardown: null,
                AllocationGate.TransitionDirect);
        }

        [UnityTest, Performance, Timeout(AllocationGate.TestTimeout)]
        public IEnumerator Load_Single()
        {
            yield return AllocationGate.Measure(
                nameof(Load_Single),
                setup: null,
                operation: () => Manager.LoadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                teardown: () => Manager.UnloadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                AllocationGate.LoadSingle);
        }

        [UnityTest, Performance, Timeout(AllocationGate.TestTimeout)]
        public IEnumerator Load_Multiple()
        {
            yield return AllocationGate.Measure(
                nameof(Load_Multiple),
                setup: null,
                operation: () => Manager.LoadAsync(new SceneParameters(_multipleScenes)).ToWaitTask(),
                teardown: () => Manager.UnloadAsync(new SceneParameters(_multipleScenes)).ToWaitTask(),
                AllocationGate.LoadMultiple);
        }

        [UnityTest, Performance, Timeout(AllocationGate.TestTimeout)]
        public IEnumerator Unload_Single()
        {
            yield return AllocationGate.Measure(
                nameof(Unload_Single),
                setup: () => Manager.LoadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                operation: () => Manager.UnloadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                teardown: null,
                AllocationGate.UnloadSingle);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest, Performance, Timeout(AllocationGate.TestTimeout)]
        public IEnumerator Load_Single_Addressable()
        {
            yield return AllocationGate.Measure(
                nameof(Load_Single_Addressable),
                setup: null,
                operation: () => Manager.LoadAsync(new SceneParameters(_addressableTargetScene)).ToWaitTask(),
                teardown: () => Manager.UnloadAsync(new SceneParameters(_addressableTargetScene)).ToWaitTask(),
                AllocationGate.NotGated);
        }

        [UnityTest, Performance, Timeout(AllocationGate.TestTimeout)]
        public IEnumerator Transition_WithLoadingScreen_Addressable()
        {
            yield return LoadSourceScene();

            yield return AllocationGate.Measure(
                nameof(Transition_WithLoadingScreen_Addressable),
                setup: null,
                operation: () => Manager.TransitionAsync(new SceneParameters(_addressableTargetScene, true), _addressableLoadingScene).ToWaitTask(),
                teardown: null,
                AllocationGate.NotGated);
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
