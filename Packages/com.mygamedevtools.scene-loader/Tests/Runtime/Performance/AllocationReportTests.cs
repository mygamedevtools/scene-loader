#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests.Performance
{
    /// <summary>
    /// The cases the allocation report covers. These assert nothing — they measure, attribute and
    /// record, and CI diffs the result against the committed baseline.
    /// </summary>
    /// <remarks>
    /// Every case lives in one class with explicit <c>[Order]</c>, because the ambient rung has to
    /// run before anything that subtracts it and NUnit orders neither methods nor classes for you.
    /// </remarks>
    public class AllocationReportTests : SceneTestBase
    {
        static readonly ILoadSceneInfo _targetScene = new LoadSceneInfoName(SceneBuilder.SceneNames[1]);
        static readonly ILoadSceneInfo _loadingScene = new LoadSceneInfoName(SceneBuilder.SceneNames[0]);

        // Four references that resolve to distinct scenes, so the linking layer has real work to
        // do rather than matching a single operation.
        static readonly ILoadSceneInfo[] _multipleScenes = new ILoadSceneInfo[]
        {
            new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            new LoadSceneInfoIndex(2),
            new LoadSceneInfoName(SceneBuilder.ScenePaths[3]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[0]),
        };

#if ENABLE_ADDRESSABLES
        static readonly ILoadSceneInfo _addressableTargetScene = new LoadSceneInfoAddress(SceneBuilder.SceneNames[1]);
#endif

        ISceneManager Manager => SceneTestEnvironment.SceneManagers[0];

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            SceneTestEnvironment.ValidateSceneEnvironment();
        }

        // A cold first scene operation costs markedly more frames than a warm one, and the
        // ambient rung has to run before anything that subtracts it — neither is something
        // NUnit's default ordering guarantees, hence [Order].
        [UnityTest, Order(0), Timeout(AllocationReport.TestTimeout)]
        public IEnumerator A00_Warmup()
        {
            for (int i = 0; i < 3; i++)
            {
                yield return Manager.LoadAsync(new SceneParameters(_targetScene)).ToWaitTask();
                yield return Manager.UnloadAsync(new SceneParameters(_targetScene)).ToWaitTask();
            }
        }

        // Runs before every case that subtracts it.
        [UnityTest, Order(1), Timeout(AllocationReport.TestTimeout)]
        public IEnumerator A0_Ambient()
        {
            yield return AllocationReport.MeasureAmbient(30);
        }

        [UnityTest, Order(2), Timeout(AllocationReport.TestTimeout)]
        public IEnumerator A_Load_Single()
        {
            yield return AllocationReport.Measure(
                "Load_Single",
                null,
                () => Manager.LoadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                () => Manager.UnloadAsync(new SceneParameters(_targetScene)).ToWaitTask());
        }

        [UnityTest, Order(3), Timeout(AllocationReport.TestTimeout)]
        public IEnumerator B_Load_Multiple()
        {
            yield return AllocationReport.Measure(
                "Load_Multiple",
                null,
                () => Manager.LoadAsync(new SceneParameters(_multipleScenes)).ToWaitTask(),
                () => Manager.UnloadAsync(new SceneParameters(_multipleScenes)).ToWaitTask());
        }

        [UnityTest, Order(4), Timeout(AllocationReport.TestTimeout)]
        public IEnumerator C_Unload_Single()
        {
            yield return AllocationReport.Measure(
                "Unload_Single",
                () => Manager.LoadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                () => Manager.UnloadAsync(new SceneParameters(_targetScene)).ToWaitTask(),
                null);
        }

        [UnityTest, Order(5), Timeout(AllocationReport.TestTimeout)]
        public IEnumerator D_Transition_Direct()
        {
            yield return LoadSourceScene();

            // With a single loaded scene and no intermediate, this takes the branch that creates
            // and destroys the temporary transition scene every time.
            yield return AllocationReport.Measure(
                "Transition_Direct",
                null,
                () => Manager.TransitionAsync(new SceneParameters(_targetScene, true)).ToWaitTask(),
                null);
        }

        [UnityTest, Order(6), Timeout(AllocationReport.TestTimeout)]
        public IEnumerator E_Transition_WithLoadingScreen()
        {
            yield return LoadSourceScene();

            yield return AllocationReport.Measure(
                "Transition_WithLoadingScreen",
                null,
                () => Manager.TransitionAsync(new SceneParameters(_targetScene, true), _loadingScene).ToWaitTask(),
                null);
        }

#if ENABLE_ADDRESSABLES
        [UnityTest, Order(7), Timeout(AllocationReport.TestTimeout)]
        public IEnumerator F_Load_Single_Addressable()
        {
            yield return AllocationReport.Measure(
                "Load_Single_Addressable",
                null,
                () => Manager.LoadAsync(new SceneParameters(_addressableTargetScene)).ToWaitTask(),
                () => Manager.UnloadAsync(new SceneParameters(_addressableTargetScene)).ToWaitTask());
        }
#endif

        [Test, Order(100)]
        public void Z_Write()
        {
            AllocationReport.Write(Application.unityVersion, AddressablesVersion);
        }

        static string AddressablesVersion
        {
            get
            {
#if ENABLE_ADDRESSABLES
                UnityEditor.PackageManager.PackageInfo package =
                    UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(UnityEngine.AddressableAssets.Addressables).Assembly);
                return package != null ? package.version : "unknown";
#else
                return "none";
#endif
            }
        }

        // Loaded outside the measured window, so every iteration starts from the same steady
        // state: one loaded scene in, one loaded scene out.
        IEnumerator LoadSourceScene()
        {
            yield return Manager.LoadAsync(new SceneParameters(_targetScene, true)).ToWaitTask();
        }
    }
}
#endif
