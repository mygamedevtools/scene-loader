using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    [PrebuildSetup(typeof(SceneTestEnvironment)), PostBuildCleanup(typeof(SceneTestEnvironment))]
    public partial class SceneManagerTests : SceneTestBase
    {
        static readonly bool[] _setActiveParameterValues = new bool[] { true, false };

        static readonly int[] _setIndexActiveParameterValues = new int[] { -1, 1 };

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
        public IEnumerator SetActive_NotThroughManager([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            Scene loadedScene = default;
            SceneManager.sceneLoaded += assignLoadedScene;

            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            yield return new WaitUntil(() => loadedScene.IsValid());

            Assert.Throws<InvalidOperationException>(() => manager.SetActiveScene(loadedScene));

            yield return SceneManager.UnloadSceneAsync(SceneBuilder.SceneNames[1]);

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
            var loadTask = manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true).AsTask();

            yield return new WaitTask<Scene>(loadTask);

            var loadedScene = loadTask.Result;
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
            yield return new WaitTask<Scene>(manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1])).AsTask());

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
        public IEnumerator LoadScene([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo sceneInfo, [ValueSource(nameof(_setActiveParameterValues))] bool setActive)
        {
            var loadTask = manager.LoadSceneAsync(sceneInfo, setActive).AsTask();

            Scene eventScene = default;
            manager.SceneLoaded += setEventScene;

            yield return new WaitTask<Scene>(loadTask);

            manager.SceneLoaded -= setEventScene;
            var loadedScene = loadTask.Result;

            Assert.AreEqual(1, manager.LoadedSceneCount);
            Assert.That(setActive ? loadedScene == manager.GetActiveScene() : loadedScene != manager.GetActiveScene());
            Assert.AreEqual(loadedScene, manager.GetLastLoadedScene());
            Assert.AreEqual(loadedScene, manager.GetLoadedSceneByName(loadedScene.name));
            Assert.AreEqual(loadedScene, eventScene);
            Assert.AreEqual(1, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setActive ? 1 : 0, _scenesActivated);

            void setEventScene(Scene scene) => eventScene = scene;
        }

        [UnityTest]
        public IEnumerator LoadScenes([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.MultipleLoadSceneInfoList))] ILoadSceneInfo[] sceneInfos, [ValueSource(nameof(_setIndexActiveParameterValues))] int setIndexActive)
        {
            int scenesToLoad = sceneInfos.Length;
            var reportedScenes = new List<Scene>(scenesToLoad);
            manager.SceneLoaded += reportSceneLoaded;

            var progress = new SimpleProgress();
            var loadTask = manager.LoadScenesAsync(sceneInfos, setIndexActive, progress).AsTask();

            Assert.AreEqual(0, progress.Value);

            yield return new WaitTask<Scene[]>(loadTask);

            manager.SceneLoaded -= reportSceneLoaded;
            var loadedScenes = loadTask.Result;

            Assert.AreEqual(1, progress.Value);
            Assert.AreEqual(scenesToLoad, loadedScenes.Length);
            Assert.AreEqual(scenesToLoad, reportedScenes.Count);
            Assert.AreEqual(scenesToLoad, manager.LoadedSceneCount);
            if (setIndexActive >= 0)
                Assert.AreEqual(manager.GetActiveScene(), loadedScenes[setIndexActive]);
            Assert.AreEqual(scenesToLoad, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setIndexActive >= 0 ? 1 : 0, _scenesActivated);

            void reportSceneLoaded(Scene loadedScene) => reportedScenes.Add(loadedScene);
        }

        [UnityTest]
        public IEnumerator LoadScene_Progress([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo sceneInfo, [ValueSource(nameof(_setActiveParameterValues))] bool setActive)
        {
            var progress = new SimpleProgress();
            Assert.AreEqual(0, progress.Value);
            yield return new WaitTask<Scene>(manager.LoadSceneAsync(sceneInfo, setActive, progress).AsTask());
            Assert.AreEqual(1, progress.Value);
        }

        [UnityTest]
        public IEnumerator LoadScene_Multiple([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.MultipleLoadSceneInfoList))] ILoadSceneInfo[] sceneInfos, [ValueSource(nameof(_setActiveParameterValues))] bool setActive)
        {
            var length = sceneInfos.Length;
            var loadedScenes = new Scene[length];

            for (int i = 0; i < length; i++)
            {
                var loadTask = manager.LoadSceneAsync(sceneInfos[i], setActive).AsTask();
                yield return new WaitTask<Scene>(loadTask);
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
        public void LoadScene_NotInBuild([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var sceneName = "not-a-real-scene";
            if (manager is SceneManager || manager is AdvancedSceneManager)
                LogAssert.Expect(LogType.Error, new Regex("'not-a-real-scene' couldn't be loaded"));
            var wait = new WaitTask<Scene>(manager.LoadSceneAsync(new LoadSceneInfoName(sceneName), false).AsTask());
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        [UnityTest]
        public IEnumerator UnloadScene([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo sceneInfo, [ValueSource(nameof(_setActiveParameterValues))] bool setActive)
        {
            yield return new WaitTask<Scene>(manager.LoadSceneAsync(sceneInfo, setActive).AsTask());

            Scene eventScene = default;
            manager.SceneUnloaded += setEventScene;

            var workingScene = manager.GetLastLoadedScene();

            var task = manager.UnloadSceneAsync(new LoadSceneInfoScene(workingScene)).AsTask();
            yield return new WaitTask<Scene>(task);

            manager.SceneUnloaded -= setEventScene;
            var unloadedScene = task.Result;

            Assert.AreEqual(workingScene, unloadedScene);
            Assert.AreEqual(workingScene, eventScene);
            Assert.IsFalse(workingScene.isLoaded);
            Assert.IsFalse(manager.GetActiveScene().IsValid());
            Assert.AreEqual(0, manager.LoadedSceneCount);
            Assert.AreEqual(1, _scenesLoaded);
            Assert.AreEqual(1, _scenesUnloaded);
            Assert.AreEqual(setActive ? 2 : 0, _scenesActivated, "Activated scenes did not match expectation");

            void setEventScene(Scene scene) => eventScene = scene;
        }

        [UnityTest]
        public IEnumerator UnloadScenes([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.MultipleLoadSceneInfoList))] ILoadSceneInfo[] sceneInfos, [ValueSource(nameof(_setIndexActiveParameterValues))] int setIndexActive)
        {
            var loadTask = manager.LoadScenesAsync(sceneInfos, setIndexActive).AsTask();
            yield return new WaitTask<Scene[]>(loadTask);
            var loadedSceneHandles = loadTask.Result.Select(s => s.handle).ToArray();

            int scenesToUnload = sceneInfos.Length;
            var reportedScenes = new List<Scene>(scenesToUnload);
            manager.SceneUnloaded += reportSceneUnloaded;

            var task = manager.UnloadScenesAsync(sceneInfos).AsTask();
            yield return new WaitTask<Scene[]>(task);

            manager.SceneUnloaded -= reportSceneUnloaded;
            var unloadedScenes = task.Result;

            Assert.AreEqual(scenesToUnload, unloadedScenes.Length);
            Assert.AreEqual(scenesToUnload, reportedScenes.Count);
            Assert.AreEqual(0, manager.LoadedSceneCount);
            Assert.AreEqual(scenesToUnload, _scenesLoaded);
            Assert.AreEqual(scenesToUnload, _scenesUnloaded);
            Assert.AreEqual(setIndexActive >= 0 ? 2 : 0, _scenesActivated, "Activated scenes did not match expectation");

            for (int i = 0; i < scenesToUnload; i++)
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

        [Test]
        public void UnloadScene_NotLoaded([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager)
        {
            var sceneName = "not-a-real-scene";
            if (manager is not AdvancedSceneManager)
                LogAssert.Expect(LogType.Warning, new Regex("Some of the scenes could not be found loaded"));
            var wait = new WaitTask<Scene>(manager.UnloadSceneAsync(new LoadSceneInfoName(sceneName)).AsTask());
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        [UnityTest]
        public IEnumerator LoadByInfo_UnloadByScene([ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SceneManagers))] ISceneManager manager, [ValueSource(typeof(SceneTestEnvironment), nameof(SceneTestEnvironment.SingleLoadSceneInfoList))] ILoadSceneInfo sceneInfo)
        {
            var task = manager.LoadSceneAsync(sceneInfo).AsTask();

            yield return new WaitTask<Scene>(task);

            var scene = task.Result;

            task = manager.UnloadSceneAsync(new LoadSceneInfoScene(scene)).AsTask();

            yield return new WaitTask<Scene>(task);

            Assert.Zero(manager.LoadedSceneCount);
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

    public class SimpleProgress : IProgress<float>
    {
        public float Value;

        public void Report(float value) => Value = value;
    }
}