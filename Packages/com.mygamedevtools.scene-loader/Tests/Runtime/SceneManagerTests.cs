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
                var director = sceneManagers[i];
                director.ActiveSceneChanged += ReportSceneActivation;
                director.SceneUnloaded += ReportSceneUnloaded;
                director.SceneLoaded += ReportSceneLoaded;
            }
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            ISceneManager[] sceneManagers = SceneTestEnvironment.SceneManagers;
            for (int i = 0; i < sceneManagers.Length; i++)
            {
                var director = sceneManagers[i];
                director.ActiveSceneChanged -= ReportSceneActivation;
                director.SceneUnloaded -= ReportSceneUnloaded;
                director.SceneLoaded -= ReportSceneLoaded;
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
        public IEnumerator SetActive_NotThroughDirector([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director)
        {
            Scene loadedScene = default;
            SceneManager.sceneLoaded += assignLoadedScene;

            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            yield return new WaitUntil(() => loadedScene.IsValid());

            Assert.Throws<InvalidOperationException>(() => director.SetActiveScene(loadedScene));

            yield return SceneManager.UnloadSceneAsync(loadedScene);

            void assignLoadedScene(Scene scene, LoadSceneMode loadSceneMode)
            {
                SceneManager.sceneLoaded -= assignLoadedScene;
                loadedScene = scene;
            }
        }

        [Test]
        public void GetActiveScene_Empty([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director)
        {
            Assert.False(director.GetActiveScene().IsValid());
        }

        [UnityTest]
        public IEnumerator GetActiveScene_Valid([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director)
        {
            var loadTask = director.LoadAsync(new SceneParameters(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true));

            yield return loadTask.ToWaitTask();

            Scene loadedScene = loadTask.Result;
            var managerActiveScene = director.GetActiveScene();

            Assert.True(loadedScene.IsValid());
            Assert.True(managerActiveScene.IsValid());
            Assert.AreEqual(loadedScene, managerActiveScene);
        }

        [Test]
        public void GetLoadedSceneByName_Invalid([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director)
        {
            Assert.Throws<ArgumentException>(() => director.GetLoadedSceneByName("not-a-real-scene"));
        }

        [UnityTest]
        public IEnumerator GetLoadedSceneByName_Valid([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director)
        {
            yield return director.LoadAsync(new SceneParameters(new LoadSceneInfoName(SceneBuilder.SceneNames[1]))).ToWaitTask();

            Assert.True(director.GetLoadedSceneByName(SceneBuilder.SceneNames[1]).IsValid());
        }

        [Test]
        public void EmptyState([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director)
        {
            Assert.False(director.GetLastLoadedScene().IsValid());
            Assert.False(director.GetActiveScene().IsValid());
        }

        [Test]
        public void GetLoadedSceneAt_IndexError([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => director.GetLoadedSceneAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => director.GetLoadedSceneAt(1));
        }

        [UnityTest]
        public IEnumerator Load([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            var progress = new SimpleProgress();
            return Load_Template(director, () => director.LoadAsync(sceneParameters, progress), progress, sceneParameters.Length, sceneParameters.GetIndexToActivate());
        }

        [UnityTest]
        public IEnumerator Load_Progress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            var progress = new SimpleProgress();
            Assert.AreEqual(0, progress.Value);
            yield return director.LoadAsync(sceneParameters, progress).ToWaitTask();
            Assert.AreEqual(1, progress.Value);
        }

        [UnityTest]
        public IEnumerator Load_Stress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            ILoadSceneInfo[] sceneInfos = sceneParameters.GetLoadSceneInfos();
            int length = sceneInfos.Length;
            bool setActive = sceneParameters.GetIndexToActivate() == 1;

            var loadedScenes = new Scene[length];

            for (int i = 0; i < length; i++)
            {
                var loadTask = director.LoadAsync(new SceneParameters(sceneInfos[i], setActive));
                yield return loadTask.ToWaitTask();
                loadedScenes[i] = loadTask.Result;
            }

            Assert.AreEqual(length, director.LoadedSceneCount);
            Assert.AreEqual(loadedScenes[^1], director.GetLastLoadedScene());

            for (int i = 0; i < length; i++)
                Assert.AreEqual(loadedScenes[i], director.GetLoadedSceneAt(i));

            Assert.That(setActive ? loadedScenes[^1] == director.GetActiveScene() : loadedScenes[^1] != director.GetActiveScene());
            Assert.AreEqual(length, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setActive ? length : 0, _scenesActivated);
        }

        [Test]
        public void Load_NotInBuild([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director)
        {
            var sceneName = "not-a-real-scene";
            LogAssert.Expect(LogType.Error, new Regex("'not-a-real-scene' couldn't be loaded"));
            var wait = director.LoadAsync(sceneName).ToWaitTask();
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        [UnityTest]
        public IEnumerator Unload([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            yield return Unload_Template(director, () => director.LoadAsync(sceneParameters), () => director.UnloadAsync(sceneParameters), sceneParameters.Length);
        }

        [Test]
        public void Unload_NotLoaded([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director)
        {
            var sceneName = "not-a-real-scene";
            if (director is not SceneManager)
                LogAssert.Expect(LogType.Warning, new Regex("Some of the scenes could not be found loaded"));
            var wait = director.UnloadAsync(sceneName).ToWaitTask();
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        [UnityTest]
        public IEnumerator Transition([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.
        TransitionSceneParametersList))] SceneParameters sceneParameters, [ValueSource(nameof(LoadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return Transition_Template(director, () => director.TransitionAsync(sceneParameters, loadingScene), sceneParameters.Length, sceneParameters.GetIndexToActivate());
        }

        [UnityTest]
        public IEnumerator Transition_NoSourceScene([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo targetScene, [ValueSource(nameof(LoadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            int expectedLoadedScenes = loadingScene == null ? 1 : 2;
            // If there's no loading scene, the scene manager will create a temporary scene
            // for the transition, and will unload it after the transition is complete.
            int expectedUnloadedScenes = 1;

            int unloadedScenesCount = 0;

            // The temporary scene unload does not go through the ISceneManager
            SceneManager.sceneUnloaded += sceneUnloaded;

            var task = director.TransitionAsync(new SceneParameters(targetScene, true), loadingScene);

            yield return task.ToWaitTask();

            Scene loadedScene = task.Result;

            SceneManager.sceneUnloaded -= sceneUnloaded;

            Assert.AreEqual(loadedScene, director.GetActiveScene());
            Assert.AreEqual(expectedLoadedScenes, _scenesLoaded);
            Assert.AreEqual(expectedUnloadedScenes, unloadedScenesCount);

            yield return new WaitUntil(() => director.TotalSceneCount == 1);

            void sceneUnloaded(Scene scene)
            {
                unloadedScenesCount++;
            }
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByScene([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo sceneInfo)
        {
            var task = director.LoadAsync(new SceneParameters(sceneInfo));

            yield return task.ToWaitTask();

            Scene scene = task.Result;

            task = director.UnloadAsync(scene);

            yield return task.ToWaitTask();

            Assert.Zero(director.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByName([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList_NoAddressable))] ILoadSceneInfo sceneInfo)
        {
            var task = director.LoadAsync(new SceneParameters(sceneInfo));

            yield return task.ToWaitTask();

            task = director.UnloadAsync(SceneBuilder.SceneNames[1]);

            yield return task.ToWaitTask();

            Assert.Zero(director.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByPath([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList_NoAddressable))] ILoadSceneInfo sceneInfo)
        {
            var task = director.LoadAsync(new SceneParameters(sceneInfo));

            yield return task.ToWaitTask();

            task = director.UnloadAsync(SceneBuilder.ScenePaths[1]);

            yield return task.ToWaitTask();

            Assert.Zero(director.LoadedSceneCount);
        }

        [UnityTest]
        public IEnumerator Load_ByInfo_UnloadByIndex([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager director, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList_NoAddressable))] ILoadSceneInfo sceneInfo)
        {
            var task = director.LoadAsync(new SceneParameters(sceneInfo));

            yield return task.ToWaitTask();

#if UNITY_EDITOR
            task = director.UnloadAsync(1);
#else
            task = director.UnloadAsync(2);
#endif

            yield return task.ToWaitTask();

            Assert.Zero(director.LoadedSceneCount);
        }

        public IEnumerator Load_Template(ISceneManager director, Func<Task<SceneResult>> loadTask, SimpleProgress progress, int sceneCount, int setIndexActive)
        {
            var reportedScenes = new List<Scene>(sceneCount);
            director.SceneLoaded += reportSceneLoaded;

            var task = loadTask();

            Assert.AreEqual(0, progress.Value);

            yield return task.ToWaitTask();

            director.SceneLoaded -= reportSceneLoaded;
            Scene[] loadedScenes = task.Result;

            Assert.AreEqual(1, progress.Value);
            Assert.AreEqual(sceneCount, loadedScenes.Length);
            Assert.AreEqual(sceneCount, reportedScenes.Count);
            Assert.AreEqual(sceneCount, director.LoadedSceneCount);
            if (setIndexActive >= 0)
                Assert.AreEqual(director.GetActiveScene(), loadedScenes[setIndexActive]);
            Assert.AreEqual(sceneCount, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setIndexActive >= 0 ? 1 : 0, _scenesActivated);

            void reportSceneLoaded(Scene loadedScene) => reportedScenes.Add(loadedScene);
        }

        public IEnumerator Transition_Template(ISceneManager director, Func<Task<SceneResult>> transitionTask, int sceneCount, int setIndexActive)
        {
            yield return LoadFirstScene(director);

            var task = transitionTask();
            yield return task.ToWaitTask();

            Scene[] loadedScenes = task.Result;
            Assert.AreEqual(sceneCount, loadedScenes.Length);
            Assert.AreEqual(loadedScenes[setIndexActive], director.GetActiveScene());

            yield return new WaitUntil(() => director.TotalSceneCount == sceneCount);
        }

        public IEnumerator Unload_Template(ISceneManager director, Func<Task<SceneResult>> loadTask, Func<Task<SceneResult>> unloadTask, int sceneCount)
        {
            var load = loadTask();
            yield return load.ToWaitTask();
            var loadedSceneHandles = load.Result.GetScenes().Select(s => s.handle).ToArray();

            var reportedScenes = new List<Scene>(sceneCount);
            director.SceneUnloaded += reportSceneUnloaded;

            var unload = unloadTask();
            yield return unload.ToWaitTask();

            director.SceneUnloaded -= reportSceneUnloaded;
            Scene[] unloadedScenes = unload.Result;

            Assert.AreEqual(sceneCount, unloadedScenes.Length);
            Assert.AreEqual(sceneCount, reportedScenes.Count);
            Assert.AreEqual(0, director.LoadedSceneCount);
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