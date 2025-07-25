using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public partial class SceneManagerTests : SceneTestBase
    {
        public static readonly ILoadSceneInfo[] LoadingSceneInfos = new ILoadSceneInfo[]
        {
            null,
            new LoadSceneInfoName(SceneBuilder.SceneNames[3]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[0]),
        };

        static readonly bool[] _setActiveParameterValues = new[] { false, true };
        static readonly int[] _setIndexActiveParameterValues = new[] { -1, 1 };

        int _scenesActivated;
        int _scenesUnloaded;
        int _scenesLoaded;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            SceneTestEnvironment.ValidateSceneEnvironment();

            ISceneManager[] sceneManagers = SceneTestEnvironment.SceneManagers;
            for (int i = 0; i < sceneManagers.Length; i++)
            {
                var manager = sceneManagers[i];
                manager.ActiveSceneChanged += ReportSceneActivation;
                manager.SceneUnloaded += ReportSceneUnloaded;
                manager.SceneLoaded += ReportSceneLoaded;
            }
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            ISceneManager[] sceneManagers = SceneTestEnvironment.SceneManagers;
            for (int i = 0; i < sceneManagers.Length; i++)
            {
                var manager = sceneManagers[i];
                manager.ActiveSceneChanged -= ReportSceneActivation;
                manager.SceneUnloaded -= ReportSceneUnloaded;
                manager.SceneLoaded -= ReportSceneLoaded;
            }
        }

        [SetUp]
        public void SetUp()
        {
            _scenesActivated = 0;
            _scenesUnloaded = 0;
            _scenesLoaded = 0;
        }

        [UnityTest]
        public IEnumerator Constructor_AddLoadedScenes()
        {
            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);

            ISceneManager sceneManager = new CoreSceneManager(true);

            Assert.AreEqual(2, sceneManager.LoadedSceneCount);
            Assert.AreEqual(sceneManager.TotalSceneCount, sceneManager.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator Constructor_InitializationScenes()
        {
            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            ISceneManager sceneManager = new CoreSceneManager(new Scene[] { loadedScene });

            Assert.AreEqual(1, sceneManager.LoadedSceneCount);
            Assert.AreEqual(sceneManager.TotalSceneCount, sceneManager.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator InitializationScene_Unload()
        {
            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            Scene loadedScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1);

            ISceneManager sceneManager = new CoreSceneManager(new Scene[] { loadedScene });

            WaitTask<SceneResult> waitTask = default;
            Assert.DoesNotThrow(() => waitTask = sceneManager.UnloadAsync(new SceneParameters(new LoadSceneInfoScene(loadedScene))).ToWaitTask());

            yield return waitTask;
        }

        [UnityTest]
        public IEnumerator SetActive_NotThroughmanager([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Scene loadedScene = default;
            SceneManager.sceneLoaded += assignLoadedScene;

            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            yield return new WaitUntil(() => loadedScene.IsValid());

            Assert.Throws<InvalidOperationException>(() => manager.SetActiveScene(loadedScene));

            yield return SceneManager.UnloadSceneAsync(loadedScene);

            void assignLoadedScene(Scene scene, LoadSceneMode loadSceneMode)
            {
                SceneManager.sceneLoaded -= assignLoadedScene;
                loadedScene = scene;
            }
        }

        [Test]
        public void GetActiveScene_Empty([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Assert.False(manager.GetActiveScene().IsValid());
        }

        [UnityTest]
        public IEnumerator GetActiveScene_Valid([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var loadTask = manager.LoadAsync(new SceneParameters(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true));

            yield return loadTask.ToWaitTask();

            Scene loadedScene = loadTask.Result;
            var managerActiveScene = manager.GetActiveScene();

            Assert.True(loadedScene.IsValid());
            Assert.True(managerActiveScene.IsValid());
            Assert.AreEqual(loadedScene, managerActiveScene);
        }

        [Test]
        public void GetLoadedSceneByName_Invalid([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Assert.Throws<ArgumentException>(() => manager.GetLoadedSceneByName("not-a-real-scene"));
        }

        [UnityTest]
        public IEnumerator GetLoadedSceneByName_Valid([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            yield return manager.LoadAsync(new SceneParameters(new LoadSceneInfoName(SceneBuilder.SceneNames[1]))).ToWaitTask();

            Assert.True(manager.GetLoadedSceneByName(SceneBuilder.SceneNames[1]).IsValid());
        }

        [Test]
        public void EmptyState([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Assert.False(manager.GetLastLoadedScene().IsValid());
            Assert.False(manager.GetActiveScene().IsValid());
        }

        [Test]
        public void GetLoadedSceneAt_IndexError([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.GetLoadedSceneAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => manager.GetLoadedSceneAt(1));
        }

        [UnityTest]
        public IEnumerator Load([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            var progress = new SimpleProgress();
            return Load_Template(manager, () => manager.LoadAsync(sceneParameters, progress), progress, sceneParameters.Length, sceneParameters.GetIndexToActivate());
        }

        [UnityTest]
        public IEnumerator Load_Progress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            var progress = new SimpleProgress();
            Assert.AreEqual(0, progress.Value);
            yield return manager.LoadAsync(sceneParameters, progress).ToWaitTask();
            Assert.AreEqual(1, progress.Value);
        }

        [UnityTest]
        public IEnumerator Load_Stress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            ILoadSceneInfo[] sceneInfos = sceneParameters.GetLoadSceneInfos();
            int length = sceneInfos.Length;
            bool setActive = sceneParameters.GetIndexToActivate() == 1;

            var loadedScenes = new Scene[length];

            for (int i = 0; i < length; i++)
            {
                var loadTask = manager.LoadAsync(new SceneParameters(sceneInfos[i], setActive));
                yield return loadTask.ToWaitTask();
                loadedScenes[i] = loadTask.Result;
            }

            Assert.AreEqual(length, manager.LoadedSceneCount);
            Assert.AreEqual(loadedScenes[^1], manager.GetLastLoadedScene());

            for (int i = 0; i < length; i++)
                Assert.AreEqual(loadedScenes[i], manager.GetLoadedSceneAt(i));

            Assert.That(setActive ? loadedScenes[^1] == manager.GetActiveScene() : loadedScenes[^1] != manager.GetActiveScene());
            Assert.AreEqual(length, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setActive ? length : 0, _scenesActivated);
        }

        [Test]
        public void Load_NotInBuild([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var sceneName = "not-a-real-scene";
            LogAssert.Expect(LogType.Error, new Regex("'not-a-real-scene' couldn't be loaded"));
            var wait = manager.LoadAsync(sceneName).ToWaitTask();
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        [UnityTest]
        public IEnumerator Unload([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            yield return Unload_Template(manager, () => manager.LoadAsync(sceneParameters), () => manager.UnloadAsync(sceneParameters), sceneParameters.Length);
        }

        [Test]
        public void Unload_NotLoaded([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var sceneName = "not-a-real-scene";
            if (manager is not SceneManager)
                LogAssert.Expect(LogType.Warning, new Regex("Unable to get an ISceneData with the load scene info"));
            var wait = manager.UnloadAsync(sceneName).ToWaitTask();
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        [UnityTest]
        public IEnumerator Reload([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo loadSceneInfo, [ValueSource(nameof(LoadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return Reload_Template(manager, loadSceneInfo, () => manager.ReloadActiveSceneAsync(loadingScene));
        }

        [UnityTest]
        public IEnumerator Transition([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.
        TransitionSceneParametersList))] SceneParameters sceneParameters, [ValueSource(nameof(LoadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return Transition_Template(manager, () => manager.TransitionAsync(sceneParameters, loadingScene), sceneParameters.Length, sceneParameters.GetIndexToActivate());
        }

        [UnityTest]
        public IEnumerator Transition_NoSourceScene([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo targetScene, [ValueSource(nameof(LoadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            int expectedLoadedScenes = loadingScene == null ? 1 : 2;
            // If there's no loading scene, the scene manager will create a temporary scene
            // for the transition, and will unload it after the transition is complete.
            int expectedUnloadedScenes = 1;

            int unloadedScenesCount = 0;

            // The temporary scene unload does not go through the ISceneManager
            SceneManager.sceneUnloaded += sceneUnloaded;

            var task = manager.TransitionAsync(new SceneParameters(targetScene, true), loadingScene);

            yield return task.ToWaitTask();

            Scene loadedScene = task.Result;

            SceneManager.sceneUnloaded -= sceneUnloaded;

            Assert.AreEqual(loadedScene, manager.GetActiveScene());
            Assert.AreEqual(expectedLoadedScenes, _scenesLoaded);
            Assert.AreEqual(expectedUnloadedScenes, unloadedScenesCount);

            yield return new WaitUntil(() => manager.TotalSceneCount == 1);

            void sceneUnloaded(Scene scene)
            {
                unloadedScenesCount++;
            }
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByScene([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo sceneInfo)
        {
            var task = manager.LoadAsync(new SceneParameters(sceneInfo));

            yield return task.ToWaitTask();

            Scene scene = task.Result;

            task = manager.UnloadAsync(scene);

            yield return task.ToWaitTask();

            Assert.Zero(manager.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByName([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList_NoAddressable))] ILoadSceneInfo sceneInfo)
        {
            var task = manager.LoadAsync(new SceneParameters(sceneInfo));

            yield return task.ToWaitTask();

            task = manager.UnloadAsync(SceneBuilder.SceneNames[1]);

            yield return task.ToWaitTask();

            Assert.Zero(manager.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByPath([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList_NoAddressable))] ILoadSceneInfo sceneInfo)
        {
            var task = manager.LoadAsync(new SceneParameters(sceneInfo));

            yield return task.ToWaitTask();

            task = manager.UnloadAsync(SceneBuilder.ScenePaths[1]);

            yield return task.ToWaitTask();

            Assert.Zero(manager.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByIndex([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList_NoAddressable))] ILoadSceneInfo sceneInfo)
        {
            var task = manager.LoadAsync(new SceneParameters(sceneInfo));

            yield return task.ToWaitTask();

#if UNITY_EDITOR
            task = manager.UnloadAsync(1);
#else
            task = manager.UnloadAsync(2);
#endif

            yield return task.ToWaitTask();

            Assert.Zero(manager.LoadedSceneCount);
        }

        public IEnumerator Load_Template(ISceneManager manager, Func<Task<SceneResult>> loadTask, SimpleProgress progress, int sceneCount, int setIndexActive)
        {
            var reportedScenes = new List<Scene>(sceneCount);
            manager.SceneLoaded += reportSceneLoaded;

            var task = loadTask();

            Assert.AreEqual(0, progress.Value);

            yield return task.ToWaitTask();

            manager.SceneLoaded -= reportSceneLoaded;
            Scene[] loadedScenes = task.Result;

            Assert.AreEqual(1, progress.Value);
            Assert.AreEqual(sceneCount, loadedScenes.Length);
            Assert.AreEqual(sceneCount, reportedScenes.Count);
            Assert.AreEqual(sceneCount, manager.LoadedSceneCount);
            if (setIndexActive >= 0)
                Assert.AreEqual(manager.GetActiveScene(), loadedScenes[setIndexActive]);
            Assert.AreEqual(sceneCount, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setIndexActive >= 0 ? 1 : 0, _scenesActivated);

            void reportSceneLoaded(Scene loadedScene) => reportedScenes.Add(loadedScene);
        }

        public IEnumerator Reload_Template(ISceneManager manager, ILoadSceneInfo loadSceneInfo, Func<Task<SceneResult>> reloadTask)
        {
            yield return manager.LoadAsync(new SceneParameters(loadSceneInfo, true)).ToWaitTask();
            string activeScene = manager.GetActiveScene().name;

            var task = reloadTask();
            yield return task.ToWaitTask();

            Scene loadedScene = task.Result;
            Assert.AreEqual(manager.GetActiveScene(), loadedScene);
            Assert.AreEqual(activeScene, loadedScene.name);

            yield return new WaitUntil(() => manager.TotalSceneCount == 1);
        }

        public IEnumerator Transition_Template(ISceneManager manager, Func<Task<SceneResult>> transitionTask, int sceneCount, int setIndexActive)
        {
            yield return LoadFirstScene(manager);

            var task = transitionTask();
            yield return task.ToWaitTask();

            Scene[] loadedScenes = task.Result;
            Assert.AreEqual(sceneCount, loadedScenes.Length);
            Assert.AreEqual(loadedScenes[setIndexActive], manager.GetActiveScene());

            yield return new WaitUntil(() => manager.TotalSceneCount == sceneCount);
        }

        public IEnumerator Unload_Template(ISceneManager manager, Func<Task<SceneResult>> loadTask, Func<Task<SceneResult>> unloadTask, int sceneCount)
        {
            var load = loadTask();
            yield return load.ToWaitTask();
            var loadedSceneHandles = load.Result.GetScenes().Select(s => s.handle).ToArray();

            var reportedScenes = new List<Scene>(sceneCount);
            manager.SceneUnloaded += reportSceneUnloaded;

            var unload = unloadTask();
            yield return unload.ToWaitTask();

            manager.SceneUnloaded -= reportSceneUnloaded;
            Scene[] unloadedScenes = unload.Result;

            Assert.AreEqual(sceneCount, unloadedScenes.Length);
            Assert.AreEqual(sceneCount, reportedScenes.Count);
            Assert.AreEqual(0, manager.LoadedSceneCount);
            Assert.AreEqual(sceneCount, _scenesLoaded);
            Assert.AreEqual(sceneCount, _scenesUnloaded);

            for (int i = 0; i < sceneCount; i++)
                Assert.True(hasReference(loadedSceneHandles[i], reportedScenes));

            void reportSceneUnloaded(Scene loadedScene) => reportedScenes.Add(loadedScene);

            bool hasReference(int handle, List<Scene> scenes)
            {
                foreach (var scene in scenes)
                    if (scene.handle == handle)
                    {
                        scenes.Remove(scene);
                        return true;
                    }
                return false;
            }
        }

        /// <summary>
        /// Required to test some transition scenarios.
        /// </summary>
        public static WaitTask<SceneResult> LoadFirstScene(ISceneManager sceneManager) => sceneManager.LoadAsync(SceneBuilder.SceneNames[1], true).ToWaitTask();

        void ReportSceneActivation(Scene previousScene, Scene newScene)
        {
            _scenesActivated++;
        }

        void ReportSceneUnloaded(Scene unloadedScene)
        {
            _scenesUnloaded++;
        }

        void ReportSceneLoaded(Scene loadedScene)
        {
            _scenesLoaded++;
        }
    }

    public class SimpleProgress : IProgress<float>
    {
        public float Value;

        public void Report(float value) => Value = value;
    }
}