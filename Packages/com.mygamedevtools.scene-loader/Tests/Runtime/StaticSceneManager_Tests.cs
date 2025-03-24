using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    [PrebuildSetup(typeof(SceneTestEnvironment)), PostBuildCleanup(typeof(SceneTestEnvironment))]
    public partial class StaticSceneManager_Tests
    {
        int _scenesActivated;
        int _scenesUnloaded;
        int _scenesLoaded;

        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            SceneTestEnvironment.ValidateSceneEnvironment();

            MySceneManager.ActiveSceneChanged += ReportSceneActivation;
            MySceneManager.SceneUnloaded += ReportSceneUnloaded;
            MySceneManager.SceneLoaded += ReportSceneLoaded;
        }

        [OneTimeTearDown]
        public void OneTimeTeardown()
        {
            MySceneManager.ActiveSceneChanged -= ReportSceneActivation;
            MySceneManager.SceneUnloaded -= ReportSceneUnloaded;
            MySceneManager.SceneLoaded -= ReportSceneLoaded;
        }

        [SetUp]
        public void TestSetup()
        {
            MySceneManager.SetActiveScene(MySceneManager.GetLoadedSceneAt(0));

            _scenesActivated = 0;
            _scenesUnloaded = 0;
            _scenesLoaded = 0;
        }

        [UnityTearDown]
        public IEnumerator UnloadScenesOnTearDown()
        {
            yield return UnloadAllScenes();
            Assert.AreEqual(1, SceneManager.sceneCount);
        }

        [Test]
        public void InitialStateTest()
        {
            int loadedScenes = 0;
            Assert.DoesNotThrow(() => loadedScenes = MySceneManager.LoadedSceneCount);
            Assert.AreEqual(1, loadedScenes);
            Assert.AreEqual(1, MySceneManager.TotalSceneCount);

            Scene activeScene = SceneManager.GetActiveScene();
            Assert.AreEqual(activeScene, MySceneManager.GetActiveScene());
            Assert.AreEqual(activeScene, MySceneManager.GetLastLoadedScene());
            Assert.AreEqual(activeScene, MySceneManager.GetLoadedSceneAt(0));
            Assert.AreEqual(activeScene, MySceneManager.GetLoadedSceneByName(activeScene.name));
        }

        [UnityTest]
        public IEnumerator Load([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            var progress = new SimpleProgress();
            return Load_Template(() => MySceneManager.LoadAsync(sceneParameters, progress), progress, sceneParameters.Length, sceneParameters.GetIndexToActivate());
        }

        [UnityTest]
        public IEnumerator Unload([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneParametersList))] SceneParameters sceneParameters)
        {
            yield return Unload_Template(() => MySceneManager.LoadAsync(sceneParameters), () => MySceneManager.UnloadAsync(sceneParameters), sceneParameters.Length);
        }

        [UnityTest]
        public IEnumerator Transition([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.
        TransitionSceneParametersList))] SceneParameters sceneParameters)
        {
            yield return Transition_Template(() => MySceneManager.TransitionAsync(sceneParameters), sceneParameters.Length, sceneParameters.GetIndexToActivate());
        }

        public IEnumerator Load_Template(Func<Task<SceneResult>> loadTask, SimpleProgress progress, int sceneCount, int setIndexActive)
        {
            var reportedScenes = new List<Scene>(sceneCount);
            MySceneManager.SceneLoaded += reportSceneLoaded;

            var task = loadTask();

            Assert.AreEqual(0, progress.Value);

            yield return task.ToWaitTask();

            MySceneManager.SceneLoaded -= reportSceneLoaded;
            Scene[] loadedScenes = task.Result;

            Assert.AreEqual(1, progress.Value);
            Assert.AreEqual(sceneCount, loadedScenes.Length);
            Assert.AreEqual(sceneCount, reportedScenes.Count);
            Assert.AreEqual(sceneCount + 1, MySceneManager.LoadedSceneCount);
            if (setIndexActive >= 0)
                Assert.AreEqual(MySceneManager.GetActiveScene(), loadedScenes[setIndexActive]);
            Assert.AreEqual(sceneCount, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setIndexActive >= 0 ? 1 : 0, _scenesActivated);

            void reportSceneLoaded(Scene loadedScene) => reportedScenes.Add(loadedScene);
        }

        public IEnumerator Transition_Template(Func<Task<SceneResult>> transitionTask, int sceneCount, int setIndexActive)
        {
            yield return LoadFirstScene();

            var task = transitionTask();
            yield return task.ToWaitTask();

            Scene[] loadedScenes = task.Result;
            Assert.AreEqual(sceneCount, loadedScenes.Length);
            Assert.AreEqual(loadedScenes[setIndexActive], MySceneManager.GetActiveScene());

            yield return new WaitUntil(() => MySceneManager.TotalSceneCount == sceneCount + 1);
        }

        public IEnumerator Unload_Template(Func<Task<SceneResult>> loadTask, Func<Task<SceneResult>> unloadTask, int sceneCount)
        {
            var load = loadTask();
            yield return load.ToWaitTask();
            var loadedSceneHandles = load.Result.GetScenes().Select(s => s.handle).ToArray();

            var reportedScenes = new List<Scene>(sceneCount);
            MySceneManager.SceneUnloaded += reportSceneUnloaded;

            var unload = unloadTask();
            yield return unload.ToWaitTask();

            MySceneManager.SceneUnloaded -= reportSceneUnloaded;
            Scene[] unloadedScenes = unload.Result;

            Assert.AreEqual(sceneCount, unloadedScenes.Length);
            Assert.AreEqual(sceneCount, reportedScenes.Count);
            Assert.AreEqual(1, MySceneManager.LoadedSceneCount);
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
        public static WaitTask<SceneResult> LoadFirstScene() => MySceneManager.LoadAsync(SceneBuilder.SceneNames[1], true).ToWaitTask();

        public static IEnumerator UnloadManagerScenes()
        {
            var lastScene = MySceneManager.GetLastLoadedScene();
            // MySceneManager registers the init scene as one of its managed scenes
            while (MySceneManager.LoadedSceneCount > 1 && lastScene.IsValid())
            {
                yield return MySceneManager.UnloadAsync(lastScene).ToWaitTask();
                lastScene = MySceneManager.GetLastLoadedScene();
            }

            Assert.AreEqual(1, MySceneManager.LoadedSceneCount);
            Assert.True(MySceneManager.GetActiveScene().IsValid());
        }

        public static IEnumerator UnloadAllScenes()
        {
            yield return UnloadManagerScenes();
            yield return SceneTestUtilities.UnloadRemainingScenes();
        }

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
}
