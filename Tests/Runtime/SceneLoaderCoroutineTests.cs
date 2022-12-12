/**
 * SceneLoaderCoroutineTests.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-07
 */

using NUnit.Framework;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneLoaderCoroutineTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return SceneLoaderTestUtilities.UnloadRemainingScenes();
        }

        [UnityTest]
        public IEnumerator LoadScene_NotInBuildSettings_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            var sceneName = "not-a-real-scene";
            LogAssert.Expect(UnityEngine.LogType.Error, $"Scene '{sceneName}' couldn't be loaded because it has not been added to the build settings or the AssetBundle has not been loaded.\nTo add a scene to the build settings use the menu File->Build Settings...");
            yield return sceneLoader.LoadSceneRoutine(new LoadSceneInfoName(sceneName), false);
        }

        [UnityTest]
        public IEnumerator LoadScene_NotActive_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            yield return sceneLoader.LoadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), false);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[1]);
            Assert.AreNotEqual(workingScene, SceneManager.GetActiveScene());
        }

        [UnityTest]
        public IEnumerator LoadScene_Active_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            yield return sceneLoader.LoadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[1]);
            Assert.AreEqual(workingScene, SceneManager.GetActiveScene());
        }

        [UnityTest]
        public IEnumerator LoadScene_MultipleNotActive_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            for (int i = 1; i <= 3; i++)
                yield return sceneLoader.LoadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[i]), false);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[3]);
            Assert.AreNotEqual(workingScene, SceneManager.GetActiveScene());
            Assert.AreEqual(4, SceneManager.sceneCount);
        }

        [UnityTest]
        public IEnumerator LoadScene_MultipleActive_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            for (int i = 1; i <= 3; i++)
                yield return sceneLoader.LoadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[i]), true);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[3]);
            Assert.AreEqual(workingScene, SceneManager.GetActiveScene());
            Assert.AreEqual(4, SceneManager.sceneCount);
        }

        [UnityTest]
        public IEnumerator UnloadScene_NotLoaded_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            LogAssert.Expect(UnityEngine.LogType.Exception, "ArgumentException: Scene to unload is invalid");
            yield return sceneLoader.UnloadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[1]));
        }

        [UnityTest]
        public IEnumerator UnloadScene_Active_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            yield return sceneLoader.LoadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.isLoaded);

            yield return sceneLoader.UnloadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[1]));
            Assert.IsFalse(workingScene.isLoaded);
        }

        [UnityTest]
        public IEnumerator UnloadScene_NotActive_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            yield return sceneLoader.LoadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), false);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.isLoaded);

            yield return sceneLoader.UnloadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[1]));
            Assert.IsFalse(workingScene.isLoaded);
        }

        [UnityTest]
        public IEnumerator UnloadScene_MultipleActive_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            for (int i = 1; i <= 3; i++)
                yield return sceneLoader.LoadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[i]), true);

            for (int i = 3; i >= 1; i--)
            {
                var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
                Assert.IsTrue(workingScene.isLoaded);

                yield return sceneLoader.UnloadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[i]));
                Assert.IsFalse(workingScene.isLoaded);
            }
        }

        [UnityTest]
        public IEnumerator UnloadScene_MultipleNotActive_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            for (int i = 1; i <= 3; i++)
                yield return sceneLoader.LoadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[i]), false);

            for (int i = 3; i >= 1; i--)
            {
                var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
                Assert.IsTrue(workingScene.isLoaded);

                yield return sceneLoader.UnloadSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[i]));
                Assert.IsFalse(workingScene.isLoaded);
            }
        }

        [UnityTest]
        public IEnumerator TransitionToOtherScene_WithoutLoading_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            yield return LoadFirstScene(sceneLoader, SceneBuilder.SceneNames[1]);

            yield return sceneLoader.TransitionToSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[2]), null);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, SceneManager.GetActiveScene());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[2]);
        }

        [UnityTest]
        public IEnumerator TransitionToOtherScene_WithEmptyLoading_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            yield return LoadFirstScene(sceneLoader, SceneBuilder.SceneNames[1]);

            yield return sceneLoader.TransitionToSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[2]), new LoadSceneInfoName(SceneBuilder.SceneNames[3]));

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, SceneManager.GetActiveScene());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[2]);
        }

        [UnityTest]
        public IEnumerator TransitionToOtherScene_WithFeedbackLoading_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            yield return LoadFirstScene(sceneLoader, SceneBuilder.SceneNames[1]);

            yield return sceneLoader.TransitionToSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[2]), new LoadSceneInfoName(SceneBuilder.SceneNames[0]));

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, SceneManager.GetActiveScene());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[2]);
        }

        [UnityTest]
        public IEnumerator TransitionToSameScene_WithoutLoading_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            yield return LoadFirstScene(sceneLoader, SceneBuilder.SceneNames[1]);

            yield return sceneLoader.TransitionToSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), null);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, SceneManager.GetActiveScene());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[1]);
        }

        [UnityTest]
        public IEnumerator TransitionToSameScene_WithEmptyLoading_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            yield return LoadFirstScene(sceneLoader, SceneBuilder.SceneNames[1]);

            yield return sceneLoader.TransitionToSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), new LoadSceneInfoName(SceneBuilder.SceneNames[2]));

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, SceneManager.GetActiveScene());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[1]);
        }

        [UnityTest]
        public IEnumerator TransitionToSameScene_WithFeedbackLoading_Test()
        {
            var sceneLoader = new SceneLoaderCoroutine();

            yield return LoadFirstScene(sceneLoader, SceneBuilder.SceneNames[1]);

            yield return sceneLoader.TransitionToSceneRoutine(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), new LoadSceneInfoName(SceneBuilder.SceneNames[0]));

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, SceneManager.GetActiveScene());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[1]);
        }

        /// <summary>
        /// Required to test transition scenarios, otherwise the initial (test) scene would be unloaded and stop the tests.
        /// </summary>
        IEnumerator LoadFirstScene(ISceneLoaderCoroutine sceneLoader, string targetSceneName)
        {
            yield return sceneLoader.LoadSceneRoutine(new LoadSceneInfoName(targetSceneName), true);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedScene();
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, SceneManager.GetActiveScene());
            Assert.AreEqual(workingScene.name, targetSceneName);
        }
    }
}