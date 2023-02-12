/**
 * SceneLoaderTests.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-07
 */

#if ENABLE_UNITASK
using MyGameDevTools.SceneLoading.UniTaskSupport;
#endif
using NUnit.Framework;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneLoaderTests : SceneTestEnvironment
    {
        static ISceneManager[] _managers = new ISceneManager[]
        {
            new SceneManager(),
#if ENABLE_ADDRESSABLES
            new SceneManagerAddressable()
#endif
        };

        static ILoadSceneInfo[] _targetSceneInfos = new ILoadSceneInfo[]
        {
            new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[2])
        };
        static ILoadSceneInfo[] _loadingSceneInfos = new ILoadSceneInfo[]
        {
            null,
            new LoadSceneInfoName(SceneBuilder.SceneNames[3]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[0])
        };
        static ISceneLoader[] _sceneLoaders = new ISceneLoader[]
        {
            new SceneLoaderCoroutine(_managers[0]),
            new SceneLoaderCoroutine(_managers[1]),
            new SceneLoaderAsync(_managers[0]),
            new SceneLoaderAsync(_managers[1]),
#if ENABLE_UNITASK
            new SceneLoaderUniTask(_managers[0]),
            new SceneLoaderUniTask(_managers[1]),
#endif
        };

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var m in _managers)
                yield return SceneLoaderTestUtilities.UnloadManagerScenes(m);
        }

        [UnityTest]
        public IEnumerator LoadScene([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader)
        {
            yield return LoadFirstScene(sceneLoader);
        }

        [UnityTest]
        public IEnumerator UnloadScene([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader)
        {
            yield return LoadFirstScene(sceneLoader);

            var loadedScene = sceneLoader.Manager.GetLastLoadedScene();

            sceneLoader.Manager.SceneUnloaded += sceneUnloaded;

            Scene unloadedScene = default;
            sceneLoader.UnloadScene(new LoadSceneInfoName(SceneBuilder.SceneNames[1]));

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => unloadedScene.IsValid() && !unloadedScene.isLoaded || watch.ElapsedMilliseconds > 1000);
            watch.Stop();

            sceneLoader.Manager.SceneUnloaded -= sceneUnloaded;

            Assert.AreEqual(loadedScene, unloadedScene);
            Assert.IsFalse(unloadedScene.isLoaded);

            void sceneUnloaded(Scene scene)
            {
                unloadedScene = scene;
            }
        }

        [UnityTest]
        public IEnumerator Transition([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneInfos))] ILoadSceneInfo targetScene, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return LoadFirstScene(sceneLoader);

            sceneLoader.Manager.SceneLoaded += sceneLoaded;

            Scene loadedScene = default;
            sceneLoader.TransitionToScene(targetScene, loadingScene);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScene.IsValid() && loadedScene.isLoaded || watch.ElapsedMilliseconds > 1000);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(loadedScene, sceneLoader.Manager.GetActiveScene());
            Assert.AreEqual(loadedScene.name, targetScene.Reference);

            void sceneLoaded(Scene scene)
            {
                if (targetScene.IsReferenceToScene(scene))
                    loadedScene = scene;
            }
        }

        [UnityTest]
        public IEnumerator Transition_FromExternalOrigin([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneInfos))] ILoadSceneInfo targetScene, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName(SceneBuilder.SceneNames[1]);

            sceneLoader.Manager.SceneLoaded += sceneLoaded;

            Scene loadedScene = default;
            sceneLoader.TransitionToScene(targetScene, loadingScene, currentScene);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScene.IsValid() && loadedScene.isLoaded || watch.ElapsedMilliseconds > 1000);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(loadedScene, sceneLoader.Manager.GetActiveScene());
            Assert.AreEqual(loadedScene.name, targetScene.Reference);

            void sceneLoaded(Scene scene)
            {
                if (targetScene.IsReferenceToScene(scene))
                    loadedScene = scene;
            }
        }

        /// <summary>
        /// Required to test transition scenarios, otherwise the initial (test) scene would be unloaded and stop the tests.
        /// </summary>
        IEnumerator LoadFirstScene(ISceneLoader sceneLoader)
        {
            sceneLoader.Manager.SceneLoaded += sceneLoaded;
            sceneLoader.LoadScene(new LoadSceneInfoName(SceneBuilder.SceneNames[1]), true);
            Scene loadedScene = default;

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScene.IsValid() && loadedScene.isLoaded || watch.ElapsedMilliseconds > 1000);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(loadedScene.name, SceneBuilder.SceneNames[1]);
            Assert.AreEqual(loadedScene, UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            void sceneLoaded(Scene scene)
            {
                loadedScene = scene;
            }
        }
    }
}