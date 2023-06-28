/**
 * SceneLoaderTests.cs
 * Created by: João Borks [joao.borks@gmail.com]
 * Created on: 2022-12-07
 */

#if ENABLE_UNITASK
using Cysharp.Threading.Tasks;
using MyGameDevTools.SceneLoading.UniTaskSupport;
#endif
using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    public class SceneLoaderTests : SceneTestEnvironment
    {
        const int _defaultTimeout = 3000;

        static ISceneManager[] _managers = new ISceneManager[]
        {
            new SceneManager(),
#if ENABLE_ADDRESSABLES
            new SceneManagerAddressable()
#endif
        };

        static ILoadSceneInfo[][] _targetSceneGroups = new ILoadSceneInfo[][]
        {
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[2]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[3]),
            },
            new ILoadSceneInfo[]
            {
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
                new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            }
        };
        static ILoadSceneInfo[] _targetSceneInfos = new ILoadSceneInfo[]
        {
            new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[2])
        };
        static ILoadSceneInfo[] _sameSceneInfos = new ILoadSceneInfo[]
        {
            new LoadSceneInfoName(SceneBuilder.SceneNames[1]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[1])
        };
        static ILoadSceneInfo[] _loadingSceneInfos = new ILoadSceneInfo[]
        {
            null,
            new LoadSceneInfoName(SceneBuilder.SceneNames[3]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[0])
        };
        static ISceneLoader[] _sceneLoaders = new ISceneLoader[]
        {
            new SceneLoaderAsync(_managers[0]),
            new SceneLoaderAsync(_managers[1]),
#if ENABLE_UNITASK
            new SceneLoaderUniTask(_managers[0]),
            new SceneLoaderUniTask(_managers[1]),
#endif
            new SceneLoaderCoroutine(_managers[0]),
            new SceneLoaderCoroutine(_managers[1])
        };

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var m in _managers)
                yield return SceneLoaderTestUtilities.UnloadManagerScenes(m);
        }

        [UnityTest]
        public IEnumerator LoadScenes([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneGroups))] ILoadSceneInfo[] targetScenes)
        {
            int sceneCount = targetScenes.Length;
            var loadedScenes = new List<Scene>(sceneCount);

            sceneLoader.Manager.SceneLoaded += sceneLoaded;
            sceneLoader.LoadScenes(targetScenes, 0);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScenes.Count == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(sceneCount, loadedScenes.Count);
            Assert.AreEqual(getTargetActiveScene(), UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            void sceneLoaded(Scene scene)
            {
                loadedScenes.Add(scene);
            }

            Scene getTargetActiveScene()
            {
                foreach (var loadedScene in loadedScenes)
                    if (targetScenes[0].IsReferenceToScene(loadedScene))
                        return loadedScene;
                return default;
            }
        }

        [UnityTest]
        public IEnumerator LoadScene([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader)
        {
            yield return LoadFirstScene(sceneLoader);
        }

        [UnityTest]
        public IEnumerator UnloadScenes([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneGroups))] ILoadSceneInfo[] targetScenes)
        {
            int sceneCount = targetScenes.Length;
            sceneLoader.LoadScenes(targetScenes);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => sceneLoader.Manager.SceneCount == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            Assert.AreEqual(sceneCount, sceneLoader.Manager.SceneCount);

            var unloadedScenes = new List<Scene>(sceneCount);

            sceneLoader.Manager.SceneUnloaded += sceneUnloaded;
            sceneLoader.UnloadScenes(targetScenes);

            watch.Restart();
            yield return new WaitUntil(() => unloadedScenes.Count == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneUnloaded -= sceneUnloaded;

            Assert.AreEqual(sceneCount, unloadedScenes.Count);
            Assert.AreEqual(0, sceneLoader.Manager.SceneCount);

            void sceneUnloaded(Scene scene)
            {
                unloadedScenes.Add(scene);
            }
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
            yield return new WaitUntil(() => unloadedScene.IsValid() && !unloadedScene.isLoaded || watch.ElapsedMilliseconds > _defaultTimeout);
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
        public IEnumerator TransitionToScenes([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneGroups))] ILoadSceneInfo[] targetScenes, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return LoadFirstScene(sceneLoader);

            int sceneCount = targetScenes.Length;
            if (loadingScene != null)
                sceneCount++;

            var loadedScenes = new List<Scene>(sceneCount);

            sceneLoader.Manager.SceneLoaded += sceneLoaded;
            sceneLoader.TransitionToScenes(targetScenes, 0, loadingScene);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScenes.Count == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(sceneCount, loadedScenes.Count);
            Assert.AreEqual(getTargetActiveScene(), UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            yield return new WaitUntil(() => !((ISceneManagerReporter)sceneLoader.Manager).IsUnloadingScenes);

            void sceneLoaded(Scene scene)
            {
                loadedScenes.Add(scene);
            }

            Scene getTargetActiveScene()
            {
                foreach (var loadedScene in loadedScenes)
                    if (targetScenes[0].IsReferenceToScene(loadedScene))
                        return loadedScene;
                return default;
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
            yield return new WaitUntil(() => loadedScene.IsValid() && loadedScene.isLoaded || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(loadedScene, sceneLoader.Manager.GetActiveScene());
            Assert.AreEqual(loadedScene.name, targetScene.Reference);

            yield return new WaitUntil(() => !((ISceneManagerReporter)sceneLoader.Manager).IsUnloadingScenes);

            void sceneLoaded(Scene scene)
            {
                if (targetScene.IsReferenceToScene(scene))
                    loadedScene = scene;
            }
        }

        [UnityTest]
        public IEnumerator Transition_Stress([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_targetSceneInfos))] ILoadSceneInfo targetScene, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return LoadFirstScene(sceneLoader);

            int type;
            if (sceneLoader is ISceneLoaderAsync)
                type = 1;
            else if (sceneLoader is ISceneLoaderUniTask)
                type = 2;
            else
                yield break; // There is currently no way to correctly get the loaded/unloaded scene in the SceneLoaderCoroutine

            Scene loadedScene = default;
            var watch = new Stopwatch();

            for (int i = 0; i < 20; i++)
            {
                switch (type)
                {
                    case 1:
                        var task = ((ISceneLoaderAsync)sceneLoader).TransitionToSceneAsync(targetScene, loadingScene).AsTask();
                        yield return new WaitTask(task);
                        loadedScene = task.Result;
                        break;
                    case 2:
                        var unitask = ((ISceneLoaderUniTask)sceneLoader).TransitionToSceneAsync(targetScene, loadingScene).AsTask();
                        yield return new WaitTask(unitask);
                        loadedScene = unitask.Result;
                        break;
                }

                Assert.AreEqual(loadedScene.handle, sceneLoader.Manager.GetActiveScene().handle);
                Assert.AreEqual(loadedScene.name, targetScene.Reference);
            }

            yield return new WaitUntil(() => !((ISceneManagerReporter)sceneLoader.Manager).IsUnloadingScenes);
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
            yield return new WaitUntil(() => loadedScene.IsValid() && loadedScene.isLoaded || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(loadedScene, sceneLoader.Manager.GetActiveScene());
            Assert.AreEqual(loadedScene.name, targetScene.Reference);

            yield return new WaitUntil(() => !((ISceneManagerReporter)sceneLoader.Manager).IsUnloadingScenes);

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
            yield return new WaitUntil(() => loadedScene.IsValid() && loadedScene.isLoaded || watch.ElapsedMilliseconds > _defaultTimeout);
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