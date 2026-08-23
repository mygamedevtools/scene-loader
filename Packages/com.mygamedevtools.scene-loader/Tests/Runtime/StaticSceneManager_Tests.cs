using System;
using System.Collections;
using System.Collections.Generic;
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
            return Load_Template(() => MySceneManager.LoadAsync(sceneParameters), sceneParameters.Length, sceneParameters.GetIndexToActivate());
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

        [UnityTest]
        public IEnumerator Reload([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleSceneRefList))] SceneRef sceneRef)
        {
            yield return Reload_Template(sceneRef, () => MySceneManager.ReloadActiveSceneAsync());
        }

        public IEnumerator Load_Template(Func<SceneOperation> loadOperation, int sceneCount, int setIndexActive)
        {
            var reportedScenes = new List<Scene>(sceneCount);
            MySceneManager.SceneLoaded += reportSceneLoaded;

            SceneOperation operation = loadOperation();

            var progress = new SimpleProgress();
            operation.Progressed += progress.Report;
            Assert.AreEqual(0, progress.Value);

            yield return operation.ToCoroutine();

            MySceneManager.SceneLoaded -= reportSceneLoaded;
            Scene[] loadedScenes = operation.Result;

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

        public IEnumerator Reload_Template(SceneRef sceneRef, Func<SceneOperation> reloadOperation)
        {
            yield return MySceneManager.LoadAsync(new SceneParameters(sceneRef, true)).ToCoroutine();
            string activeScene = MySceneManager.GetActiveScene().name;

            SceneOperation operation = reloadOperation();
            yield return operation.ToCoroutine();

            Scene loadedScene = operation.Result;
            Assert.AreEqual(MySceneManager.GetActiveScene(), loadedScene);
            Assert.AreEqual(activeScene, loadedScene.name);

            yield return SceneManagerTests.WaitForTotalSceneCount(MySceneManager.Default, 2);
        }

        public IEnumerator Transition_Template(Func<SceneOperation> transitionOperation, int sceneCount, int setIndexActive)
        {
            yield return LoadFirstScene();

            SceneOperation operation = transitionOperation();
            yield return operation.ToCoroutine();

            Scene[] loadedScenes = operation.Result;
            Assert.AreEqual(sceneCount, loadedScenes.Length);
            Assert.AreEqual(loadedScenes[setIndexActive], MySceneManager.GetActiveScene());

            yield return SceneManagerTests.WaitForTotalSceneCount(MySceneManager.Default, sceneCount + 1);
        }

        public IEnumerator Unload_Template(Func<SceneOperation> loadOperation, Func<SceneOperation> unloadOperation, int sceneCount)
        {
            var load = loadOperation();
            yield return load.ToCoroutine();
            var loadedScenes = load.Result.GetScenes();

            var reportedScenes = new List<Scene>(sceneCount);
            MySceneManager.SceneUnloaded += reportSceneUnloaded;

            var unload = unloadOperation();
            yield return unload.ToCoroutine();

            MySceneManager.SceneUnloaded -= reportSceneUnloaded;
            Scene[] unloadedScenes = unload.Result;

            Assert.AreEqual(sceneCount, unloadedScenes.Length);
            Assert.AreEqual(sceneCount, reportedScenes.Count);
            Assert.AreEqual(1, MySceneManager.LoadedSceneCount);
            Assert.AreEqual(sceneCount, _scenesLoaded);
            Assert.AreEqual(sceneCount, _scenesUnloaded);

            for (int i = 0; i < sceneCount; i++)
                Assert.True(hasReference(loadedScenes[i], reportedScenes));

            void reportSceneUnloaded(Scene loadedScene) => reportedScenes.Add(loadedScene);

            bool hasReference(Scene expectedScene, List<Scene> scenes)
            {
                foreach (var scene in scenes)
                    if (scene.handle == expectedScene.handle)
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
        public static IEnumerator LoadFirstScene() => MySceneManager.LoadAsync(new SceneParameters((SceneRef)SceneBuilder.SceneNames[1], true)).ToCoroutine();

        public static IEnumerator UnloadManagerScenes()
        {
            var lastScene = MySceneManager.GetLastLoadedScene();
            // MySceneManager registers the init scene as one of its managed scenes
            while (MySceneManager.LoadedSceneCount > 1 && lastScene.IsValid())
            {
                yield return MySceneManager.UnloadAsync(lastScene).ToCoroutine();
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
