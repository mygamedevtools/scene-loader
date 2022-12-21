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
        ISceneManager<Scene, ILoadSceneInfo> _manager;

        [SetUp]
        public void SetUp()
        {
            _manager = new SceneManager();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            while (_manager.SceneCount > 0)
                yield return new WaitTask(_manager.UnloadSceneAsync(new LoadSceneInfoScene(_manager.GetLastLoadedScene())));

            Assert.Zero(_manager.SceneCount);
            Assert.False(_manager.GetActiveScene().IsValid());
        }

        [UnityTest]
        public IEnumerator LoadScene_NotActive()
        {
            var loadTask = _manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1])).AsTask();
            yield return new WaitTask(loadTask);

            var loadedScene = loadTask.Result;

            Assert.AreEqual(1, _manager.SceneCount);
            Assert.AreNotEqual(loadedScene, _manager.GetActiveScene());
            Assert.AreEqual(loadedScene, _manager.GetLastLoadedScene());
            Assert.AreEqual(loadedScene, _manager.GetLoadedSceneByName(loadedScene.name));
        }

        [UnityTest]
        public IEnumerator LoadScene_Active()
        {
            var loadTask = _manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true).AsTask();
            yield return new WaitTask(loadTask);

            var loadedScene = loadTask.Result;

            Assert.AreEqual(1, _manager.SceneCount);
            Assert.AreEqual(loadedScene, _manager.GetActiveScene());
            Assert.AreEqual(loadedScene, _manager.GetLastLoadedScene());
            Assert.AreEqual(loadedScene, _manager.GetLoadedSceneByName(loadedScene.name));
        }

        [UnityTest]
        public IEnumerator LoadScene_NotActive_Progress()
        {
            var progress = new SimpleProgress();
            yield return new WaitTask(_manager.LoadSceneAsync(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), false, progress).AsTask());
            Assert.AreEqual(1, progress.Value);
        }

        [UnityTest]
        public IEnumerator LoadScene_Active_Progress()
        {
            var progress = new SimpleProgress();
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
        }
    }

    public class SimpleProgress : IProgress<float>
    {
        public float Value;

        public void Report(float value) => Value = value;
    }
}