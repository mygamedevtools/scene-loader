/**
 * SceneManagerTests.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-21
 */

using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneManagerTests
    {
        ISceneManager _manager;
        int _scenesActivated;
        int _scenesUnloaded;
        int _scenesLoaded;

        [SetUp]
        public void SetUp()
        {
            _manager = new SceneManager();
            _manager.ActiveSceneChanged += ReportSceneActivation;
            _manager.SceneUnloaded += ReportSceneUnloaded;
            _manager.SceneLoaded += ReportSceneLoaded;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            while (_manager.SceneCount > 0)
                yield return new WaitTask(_manager.UnloadSceneAsync(new LoadSceneInfoScene(_manager.GetLastLoadedScene())).AsTask());

            _scenesActivated = 0;
            _scenesUnloaded = 0;
            _scenesLoaded = 0;

            Assert.Zero(_manager.SceneCount);
            Assert.False(_manager.GetActiveScene().IsValid());
        }

        [Test]
        public void GetActiveScene_Empty()
        {
            Assert.False(_manager.GetActiveScene().IsValid());
        }

        [UnityTest]
        public IEnumerator GetActiveScene_Valid()
        {
            Scene activeScene = default;
            _manager.ActiveSceneChanged += (old, current) => activeScene = current;

            yield return new WaitTask(_manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true).AsTask());

            var managerActiveScene = _manager.GetActiveScene();

            Assert.True(activeScene.IsValid());
            Assert.True(managerActiveScene.IsValid());
            Assert.AreEqual(activeScene, managerActiveScene);
        }

        [Test]
        public void GetLoadedSceneByName_Invalid()
        {
            Assert.Throws<ArgumentException>(() => _manager.GetLoadedSceneByName("not-a-real-scene"));
        }

        [UnityTest]
        public IEnumerator GetLoadedSceneByName_Valid()
        {
            yield return new WaitTask(_manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1])).AsTask());

            Assert.True(_manager.GetLoadedSceneByName(SceneBuilder.SceneNames[1]).IsValid());
        }

        [Test]
        public void EmptyState()
        {
            Assert.False(_manager.GetLastLoadedScene().IsValid());
            Assert.False(_manager.GetActiveScene().IsValid());
        }

        [Test]
        public void GetLoadedSceneAt_IndexError()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _manager.GetLoadedSceneAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => _manager.GetLoadedSceneAt(1));
        }

        [UnityTest]
        public IEnumerator LoadScene_NotActive()
        {
            var loadTask = _manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1])).AsTask();

            Scene eventScene = default;
            _manager.SceneLoaded += scene => eventScene = scene;

            yield return new WaitTask(loadTask);

            var loadedScene = loadTask.Result;

            Assert.AreEqual(1, _manager.SceneCount);
            Assert.AreNotEqual(loadedScene, _manager.GetActiveScene());
            Assert.AreEqual(loadedScene, _manager.GetLastLoadedScene());
            Assert.AreEqual(loadedScene, _manager.GetLoadedSceneByName(loadedScene.name));
            Assert.AreEqual(loadedScene, eventScene);
            Assert.AreEqual(1, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(0, _scenesActivated);
        }

        [UnityTest]
        public IEnumerator LoadScene_Active()
        {
            var loadTask = _manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true).AsTask();

            Scene eventScene = default;
            _manager.SceneLoaded += scene => eventScene = scene;

            yield return new WaitTask(loadTask);

            var loadedScene = loadTask.Result;

            Assert.AreEqual(1, _manager.SceneCount);
            Assert.AreEqual(loadedScene, _manager.GetActiveScene());
            Assert.AreEqual(loadedScene, _manager.GetLastLoadedScene());
            Assert.AreEqual(loadedScene, _manager.GetLoadedSceneByName(loadedScene.name));
            Assert.AreEqual(loadedScene, eventScene);
            Assert.AreEqual(1, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(1, _scenesActivated);
        }

        [UnityTest]
        public IEnumerator LoadScene_NotActive_Progress()
        {
            var progress = new SimpleProgress();
            Assert.AreEqual(0, progress.Value);
            yield return new WaitTask(_manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), false, progress).AsTask());
            Assert.AreEqual(1, progress.Value);
        }

        [UnityTest]
        public IEnumerator LoadScene_Active_Progress()
        {
            var progress = new SimpleProgress();
            Assert.AreEqual(0, progress.Value);
            yield return new WaitTask(_manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true, progress).AsTask());
            Assert.AreEqual(1, progress.Value);
        }

        [UnityTest]
        public IEnumerator LoadScene_NotActive_Multiple()
        {
            var loadedScenes = new Scene[3];

            for (int i = 1; i <= loadedScenes.Length; i++)
            {
                var loadTask = _manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[i])).AsTask();
                yield return new WaitTask(loadTask);
                loadedScenes[i - 1] = loadTask.Result;
            }

            Assert.AreEqual(loadedScenes.Length, _manager.SceneCount);
            Assert.AreEqual(loadedScenes[^1], _manager.GetLastLoadedScene());

            for (int i = 0; i < loadedScenes.Length; i++)
                Assert.AreEqual(loadedScenes[i], _manager.GetLoadedSceneAt(i));

            Assert.AreNotEqual(loadedScenes[^1], _manager.GetActiveScene());
            Assert.AreEqual(3, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(0, _scenesActivated);
        }

        [UnityTest]
        public IEnumerator LoadScene_Active_Multiple()
        {
            var loadedScenes = new Scene[3];

            for (int i = 1; i <= loadedScenes.Length; i++)
            {
                var loadTask = _manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[i]), true).AsTask();
                yield return new WaitTask(loadTask);
                loadedScenes[i - 1] = loadTask.Result;
            }

            Assert.AreEqual(loadedScenes.Length, _manager.SceneCount);
            Assert.AreEqual(loadedScenes[^1], _manager.GetLastLoadedScene());

            for (int i = 0; i < loadedScenes.Length; i++)
                Assert.AreEqual(loadedScenes[i], _manager.GetLoadedSceneAt(i));

            Assert.AreEqual(loadedScenes[^1], _manager.GetActiveScene());
            Assert.AreEqual(3, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(3, _scenesActivated);
        }

        [Test]
        public void LoadScene_NotInBuildSettings()
        {
            var sceneName = "not-a-real-scene";
            LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("couldn't be loaded"));
            var wait = new WaitTask(_manager.LoadSceneAsync(new LoadSceneInfoName(sceneName), false).AsTask());
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        [UnityTest]
        public IEnumerator UnloadScene_Active()
        {
            yield return new WaitTask(_manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true).AsTask());

            Scene eventScene = default;
            _manager.SceneUnloaded += scene => eventScene = scene;

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();

            var task = _manager.UnloadSceneAsync(new LoadSceneInfoScene(workingScene)).AsTask();
            yield return new WaitTask(task);
            var loadedScene = task.Result;
            Assert.AreEqual(workingScene, loadedScene);
            Assert.AreEqual(workingScene, eventScene);
            Assert.IsFalse(workingScene.isLoaded);
            Assert.IsFalse(_manager.GetActiveScene().IsValid());
            Assert.AreEqual(0, _manager.SceneCount);
            Assert.AreEqual(1, _scenesLoaded);
            Assert.AreEqual(1, _scenesUnloaded);
            Assert.AreEqual(2, _scenesActivated, "Activated scenes did not match expectation");
        }

        [UnityTest]
        public IEnumerator UnloadScene_NotActive()
        {
            yield return new WaitTask(_manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1])).AsTask());

            Scene eventScene = default;
            _manager.SceneUnloaded += scene => eventScene = scene;

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();

            var task = _manager.UnloadSceneAsync(new LoadSceneInfoScene(workingScene)).AsTask();
            yield return new WaitTask(task);
            var loadedScene = task.Result;
            Assert.AreEqual(workingScene, loadedScene);
            Assert.AreEqual(workingScene, eventScene);
            Assert.IsFalse(workingScene.isLoaded);
            Assert.IsFalse(_manager.GetActiveScene().IsValid());
            Assert.AreEqual(0, _manager.SceneCount);
            Assert.AreEqual(1, _scenesLoaded);
            Assert.AreEqual(1, _scenesUnloaded);
            Assert.AreEqual(0, _scenesActivated);
        }

        [Test]
        public void UnloadScene_NotLoaded()
        {
            var sceneName = "not-a-real-scene";

            var wait = new WaitTask(_manager.UnloadSceneAsync(new LoadSceneInfoName(sceneName)).AsTask());
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        void ReportSceneActivation(Scene previousScene, Scene newScene) => _scenesActivated++;

        void ReportSceneUnloaded(Scene unloadedScene) => _scenesUnloaded++;

        void ReportSceneLoaded(Scene loadedScene) => _scenesLoaded++;
    }

    public class SimpleProgress : IProgress<float>
    {
        public float Value;

        public void Report(float value) => Value = value;
    }
}