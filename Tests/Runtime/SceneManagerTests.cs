/**
 * SceneManagerTests.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-21
 */

using NUnit.Framework;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneManagerTests
    {
        static readonly ILoadSceneInfo[][] _loadSceneInfos_multiple = new ILoadSceneInfo[][]
        {
            new ILoadSceneInfo[]
            {
#if UNITY_EDITOR
                new LoadSceneInfoIndex(1),
                new LoadSceneInfoIndex(2),
                new LoadSceneInfoIndex(3)
#else
                new LoadSceneInfoIndex(2),
                new LoadSceneInfoIndex(3),
                new LoadSceneInfoIndex(4)
#endif
            },
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[2]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[3]),
            },
            new ILoadSceneInfo[]
            {
#if UNITY_EDITOR
                new LoadSceneInfoIndex(1),
                new LoadSceneInfoIndex(1),
                new LoadSceneInfoIndex(1)
#else
                new LoadSceneInfoIndex(2),
                new LoadSceneInfoIndex(2),
                new LoadSceneInfoIndex(2)
#endif
            },
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            }
        };
        static readonly ILoadSceneInfo[] _loadSceneInfos_single = new ILoadSceneInfo[]
        {
#if UNITY_EDITOR
            new LoadSceneInfoIndex(1),
#else
            new LoadSceneInfoIndex(2),
#endif
            new LoadSceneInfoName(SceneBuilder.SceneNames[1])
        };
        static readonly bool[] _setActiveValues = new bool[] { true, false };

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

        [UnityTest]
        public IEnumerator SetActive_NotThroughManager()
        {
            Scene loadedScene = default;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += assignLoadedScene;

            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            yield return new WaitUntil(() => loadedScene.IsValid());

            Assert.Throws<InvalidOperationException>(() => _manager.SetActiveScene(loadedScene));

            yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(SceneBuilder.SceneNames[1]);

            void assignLoadedScene(Scene scene, LoadSceneMode loadSceneMode)
            {
                UnityEngine.SceneManagement.SceneManager.sceneLoaded -= assignLoadedScene;
                loadedScene = scene;
            }
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
        public IEnumerator LoadScene([ValueSource(nameof(_loadSceneInfos_single))] ILoadSceneInfo sceneInfo, [ValueSource(nameof(_setActiveValues))] bool setActive)
        {
            var loadTask = _manager.LoadSceneAsync(sceneInfo, setActive).AsTask();

            Scene eventScene = default;
            _manager.SceneLoaded += scene => eventScene = scene;

            yield return new WaitTask(loadTask);

            var loadedScene = loadTask.Result;

            Assert.AreEqual(1, _manager.SceneCount);
            Assert.That(setActive ? loadedScene == _manager.GetActiveScene() : loadedScene != _manager.GetActiveScene());
            Assert.AreEqual(loadedScene, _manager.GetLastLoadedScene());
            Assert.AreEqual(loadedScene, _manager.GetLoadedSceneByName(loadedScene.name));
            Assert.AreEqual(loadedScene, eventScene);
            Assert.AreEqual(1, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setActive ? 1 : 0, _scenesActivated);
        }

        [UnityTest]
        public IEnumerator LoadScene_Progress([ValueSource(nameof(_loadSceneInfos_single))] ILoadSceneInfo sceneInfo, [ValueSource(nameof(_setActiveValues))] bool setActive)
        {
            var progress = new SimpleProgress();
            Assert.AreEqual(0, progress.Value);
            yield return new WaitTask(_manager.LoadSceneAsync(sceneInfo, setActive, progress).AsTask());
            Assert.AreEqual(1, progress.Value);
        }

        [UnityTest]
        public IEnumerator LoadScene_Multiple([ValueSource(nameof(_loadSceneInfos_multiple))] ILoadSceneInfo[] sceneInfos, [ValueSource(nameof(_setActiveValues))] bool setActive)
        {
            var length = sceneInfos.Length;
            var loadedScenes = new Scene[length];

            for (int i = 0; i < length; i++)
            {
                var loadTask = _manager.LoadSceneAsync(sceneInfos[i], setActive).AsTask();
                yield return new WaitTask(loadTask);
                loadedScenes[i] = loadTask.Result;
            }

            Assert.AreEqual(length, _manager.SceneCount);
            Assert.AreEqual(loadedScenes[^1], _manager.GetLastLoadedScene());

            for (int i = 0; i < length; i++)
                Assert.AreEqual(loadedScenes[i], _manager.GetLoadedSceneAt(i));

            Assert.That(setActive ? loadedScenes[^1] == _manager.GetActiveScene() : loadedScenes[^1] != _manager.GetActiveScene());
            Assert.AreEqual(3, _scenesLoaded);
            Assert.AreEqual(0, _scenesUnloaded);
            Assert.AreEqual(setActive ? 3 : 0, _scenesActivated);
        }

        [Test]
        public void LoadScene_NotInBuildSettings()
        {
            var sceneName = "not-a-real-scene";
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("couldn't be loaded"));
            var wait = new WaitTask(_manager.LoadSceneAsync(new LoadSceneInfoName(sceneName), false).AsTask());
            Assert.Throws<AggregateException>(() => wait.MoveNext());
        }

        [UnityTest]
        public IEnumerator UnloadScene([ValueSource(nameof(_loadSceneInfos_single))] ILoadSceneInfo sceneInfo, [ValueSource(nameof(_setActiveValues))] bool setActive)
        {
            yield return new WaitTask(_manager.LoadSceneAsync(sceneInfo, setActive).AsTask());

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
            Assert.AreEqual(setActive ? 2 : 0, _scenesActivated, "Activated scenes did not match expectation");
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