/**
 * AddressableSceneLoaderCoroutineTests.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-14
 */

using MyGameDevTools.SceneLoading.AddressablesSupport;
using NUnit.Framework;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class AddressableSceneLoaderCoroutineTests
    {
        readonly IAddressableSceneManager _sceneManager = new AddressableSceneManager();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return SceneLoaderTestUtilities.UnloadRemainingAddressableScenes(_sceneManager);
            Assert.AreEqual(0, _sceneManager.SceneCount);
        }

        [UnityTest]
        public IEnumerator LoadScene_NotInAddressables_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            var sceneName = "not-a-real-scene";
            LogAssert.Expect(LogType.Error, new Regex("(UnityEngine\\.AddressableAssets\\.InvalidKeyException)"));
            yield return sceneLoader.LoadSceneRoutine(new AddressableLoadSceneReferenceKey(sceneName), false);
        }

        [UnityTest]
        public IEnumerator LoadScene_NotActive_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            yield return sceneLoader.LoadSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[1]), false);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.IsValid());
            Assert.IsTrue(workingScene.isLoaded);
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[1]);
            Assert.AreNotEqual(workingScene, _sceneManager.GetActiveScene().Scene);
        }

        [UnityTest]
        public IEnumerator LoadScene_Active_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            yield return sceneLoader.LoadSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[1]), true);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.IsValid());
            Assert.IsTrue(workingScene.isLoaded);
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[1]);
            Assert.AreEqual(workingScene, _sceneManager.GetActiveScene().Scene);
        }

        [UnityTest]
        public IEnumerator LoadScene_MultipleNotActive_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            for (int i = 1; i <= 3; i++)
                yield return sceneLoader.LoadSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[i]), false);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[3]);
            Assert.AreNotEqual(workingScene, _sceneManager.GetActiveScene().Scene);
            Assert.AreEqual(4, SceneManager.sceneCount);
        }

        [UnityTest]
        public IEnumerator LoadScene_MultipleActive_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            for (int i = 1; i <= 3; i++)
                yield return sceneLoader.LoadSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[i]), true);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[3]);
            Assert.AreEqual(workingScene, _sceneManager.GetActiveScene().Scene);
            Assert.AreEqual(4, SceneManager.sceneCount);
        }

        [UnityTest]
        public IEnumerator UnloadScene_NotLoaded_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            LogAssert.Expect(LogType.Error, new Regex("(System\\.InvalidOperationException)"));
            yield return sceneLoader.UnloadSceneRoutine(new AddressableLoadSceneInfoName(SceneBuilder.SceneNames[1]));
        }

        [UnityTest]
        public IEnumerator UnloadScene_Active_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            yield return sceneLoader.LoadSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[1]), false);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.isLoaded);

            yield return sceneLoader.UnloadSceneRoutine(new AddressableLoadSceneInfoName(SceneBuilder.SceneNames[1]));
            Assert.IsFalse(workingScene.isLoaded);
        }

        [UnityTest]
        public IEnumerator UnloadScene_NotActive_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            yield return sceneLoader.LoadSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[1]), false);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.isLoaded);

            yield return sceneLoader.UnloadSceneRoutine(new AddressableLoadSceneInfoName(SceneBuilder.SceneNames[1]));
            Assert.IsFalse(workingScene.isLoaded);
        }

        [UnityTest]
        public IEnumerator UnloadScene_MultipleActive_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            for (int i = 1; i <= 3; i++)
                yield return sceneLoader.LoadSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[i]), false);

            for (int i = 3; i >= 1; i--)
            {
                var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
                Assert.IsTrue(workingScene.isLoaded);

                yield return sceneLoader.UnloadSceneRoutine(new AddressableLoadSceneInfoName(SceneBuilder.SceneNames[i]));
                Assert.IsFalse(workingScene.isLoaded);
            }
        }

        [UnityTest]
        public IEnumerator UnloadScene_MultipleNotActive_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            for (int i = 1; i <= 3; i++)
                yield return sceneLoader.LoadSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[i]), false);

            for (int i = 3; i >= 1; i--)
            {
                var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
                Assert.IsTrue(workingScene.isLoaded);

                yield return sceneLoader.UnloadSceneRoutine(new AddressableLoadSceneInfoName(SceneBuilder.SceneNames[i]));
                Assert.IsFalse(workingScene.isLoaded);
            }
        }

        [UnityTest]
        public IEnumerator TransitionToOtherScene_WithoutLoading_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            yield return sceneLoader.TransitionToSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[2]), null);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, _sceneManager.GetActiveScene().Scene);
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[2]);
        }

        [UnityTest]
        public IEnumerator TransitionToOtherScene_WithEmptyLoading_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            yield return sceneLoader.TransitionToSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[2]), new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[3]));

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, _sceneManager.GetActiveScene().Scene);
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[2]);
        }

        [UnityTest]
        public IEnumerator TransitionToOtherScene_WithFeedbackLoading_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            yield return sceneLoader.TransitionToSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[2]), new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[0]));

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, _sceneManager.GetActiveScene().Scene);
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[2]);
        }

        [UnityTest]
        public IEnumerator TransitionToSameScene_WithoutLoading_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            yield return sceneLoader.TransitionToSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[1]), null);

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, _sceneManager.GetActiveScene().Scene);
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[1]);
        }

        [UnityTest]
        public IEnumerator TransitionToSameScene_WithEmptyLoading_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            yield return sceneLoader.TransitionToSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[1]), new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[2]));

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, _sceneManager.GetActiveScene().Scene);
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[1]);
        }

        [UnityTest]
        public IEnumerator TransitionToSameScene_WithFeedbackLoading_Test()
        {
            var sceneLoader = new AddressableSceneLoaderCoroutine(_sceneManager);

            yield return sceneLoader.TransitionToSceneRoutine(new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[1]), new AddressableLoadSceneReferenceKey(SceneBuilder.SceneNames[0]));

            var workingScene = SceneLoaderTestUtilities.GetLastLoadedAddressableScene(_sceneManager);
            Assert.IsTrue(workingScene.IsValid());
            Assert.AreEqual(workingScene, _sceneManager.GetActiveScene().Scene);
            Assert.AreEqual(workingScene.name, SceneBuilder.SceneNames[1]);
        }
    }
}