#if ENABLE_UNITASK
using Cysharp.Threading.Tasks;
#endif
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MyGameDevTools.SceneLoading.Tests
{
    enum SceneLoaderType
    {
        Async,
#if ENABLE_UNITASK
        UniTask,
#endif
        Coroutine,
    }

    public class SceneLoaderTests : SceneTestEnvironment
    {
        const int _defaultTimeout = 3000;

        static readonly ILoadSceneInfo[] _loadingSceneInfos = new ILoadSceneInfo[]
        {
            null,
            new LoadSceneInfoName(SceneBuilder.SceneNames[3]),
            new LoadSceneInfoName(SceneBuilder.SceneNames[0]),
        };

        static readonly ISceneLoader[] _sceneLoaders = new ISceneLoader[]
        {
            new SceneLoaderAsync(_sceneManagers[0]),
#if ENABLE_UNITASK
            new SceneLoaderUniTask(_sceneManagers[0]),
#endif
            new SceneLoaderCoroutine(_sceneManagers[0]),
        };

        // Note: These functions must create brand new scene managers to correctly test the dispose flow
        static readonly Func<ISceneLoader>[] _sceneLoaderCreateFuncs = new Func<ISceneLoader>[]
        {
            () => new SceneLoaderAsync(new AdvancedSceneManager()),
#if ENABLE_UNITASK
            () => new SceneLoaderUniTask(new AdvancedSceneManager()),
#endif
            () => new SceneLoaderCoroutine(new AdvancedSceneManager()),
        };

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            foreach (var m in _sceneManagers)
                yield return SceneTestUtilities.UnloadManagerScenes(m);

            yield return SceneTestUtilities.UnloadRemainingScenes();
            Assert.AreEqual(1, SceneManager.loadedSceneCount);
        }

        [UnityTest]
        public IEnumerator LoadScenes([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_multipleLoadSceneInfoList))] ILoadSceneInfo[] targetScenes)
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

            void sceneLoaded(Scene scene)
            {
                loadedScenes.Add(scene);
            }
        }

        [UnityTest]
        public IEnumerator LoadScene([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader)
        {
            yield return LoadFirstScene(sceneLoader);
        }

        [UnityTest]
        public IEnumerator UnloadScenes([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_multipleLoadSceneInfoList))] ILoadSceneInfo[] targetScenes)
        {
            int sceneCount = targetScenes.Length;
            sceneLoader.LoadScenes(targetScenes);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => sceneLoader.Manager.LoadedSceneCount == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            Assert.AreEqual(sceneCount, sceneLoader.Manager.LoadedSceneCount);

            var unloadedScenes = new List<Scene>(sceneCount);

            sceneLoader.Manager.SceneUnloaded += sceneUnloaded;
            sceneLoader.UnloadScenes(targetScenes);

            watch.Restart();
            yield return new WaitUntil(() => unloadedScenes.Count == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneUnloaded -= sceneUnloaded;

            Assert.AreEqual(sceneCount, unloadedScenes.Count);
            Assert.AreEqual(0, sceneLoader.Manager.LoadedSceneCount);

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
            yield return new WaitUntil(() => unloadedScene.handle != 0 && !unloadedScene.isLoaded || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneUnloaded -= sceneUnloaded;

            Assert.Less(watch.ElapsedMilliseconds, _defaultTimeout);
            Assert.AreEqual(loadedScene, unloadedScene);
            Assert.IsFalse(unloadedScene.isLoaded);

            void sceneUnloaded(Scene scene)
            {
                unloadedScene = scene;
            }
        }

        [UnityTest]
        public IEnumerator TransitionToScenes([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_multipleLoadSceneInfoList))] ILoadSceneInfo[] targetScenes, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
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

            yield return new WaitUntil(() => sceneLoader.Manager.TotalSceneCount == sceneCount);

            void sceneLoaded(Scene scene)
            {
                loadedScenes.Add(scene);
            }
        }

        [UnityTest]
        public IEnumerator Transition([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_singleLoadSceneInfoList))] ILoadSceneInfo targetScene, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return LoadFirstScene(sceneLoader);

            List<Scene> loadedScenes = new();
            int expectedScenes = loadingScene == null ? 1 : 2;
            sceneLoader.Manager.SceneLoaded += sceneLoaded;

            sceneLoader.TransitionToScene(targetScene, loadingScene);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScenes.Count == expectedScenes || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(loadedScenes[expectedScenes - 1], sceneLoader.Manager.GetActiveScene());

            yield return new WaitUntil(() => sceneLoader.Manager.TotalSceneCount == 1);

            void sceneLoaded(Scene scene)
            {
                loadedScenes.Add(scene);
            }
        }

        [UnityTest]
        public IEnumerator Transition_FromExternalOrigin([ValueSource(nameof(_sceneLoaders))] ISceneLoader sceneLoader, [ValueSource(nameof(_singleLoadSceneInfoList))] ILoadSceneInfo targetScene, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            yield return SceneManager.LoadSceneAsync(SceneBuilder.SceneNames[1], LoadSceneMode.Additive);
            var currentScene = SceneManager.GetSceneByName(SceneBuilder.SceneNames[1]);

            List<Scene> loadedScenes = new();
            int expectedScenes = loadingScene == null ? 1 : 2;
            sceneLoader.Manager.SceneLoaded += sceneLoaded;

            sceneLoader.TransitionToScene(targetScene, loadingScene, currentScene);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loadedScenes.Count == expectedScenes || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            sceneLoader.Manager.SceneLoaded -= sceneLoaded;

            Assert.AreEqual(loadedScenes[expectedScenes - 1], sceneLoader.Manager.GetActiveScene());

            yield return new WaitUntil(() => sceneLoader.Manager.TotalSceneCount == 1);

            void sceneLoaded(Scene scene)
            {
                loadedScenes.Add(scene);
            }
        }

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_Simple([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc)
        {
            ISceneLoader loader = loaderCreateFunc();
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringLoadScene([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc)
        {
            ISceneLoader loader = loaderCreateFunc();
            loader.LoadScene(new LoadSceneInfoName(SceneBuilder.SceneNames[1]));
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringLoadScenes([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_multipleLoadSceneInfoList))] ILoadSceneInfo[] targetScenes)
        {
            ISceneLoader loader = loaderCreateFunc();
            loader.LoadScenes(targetScenes);
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringUnloadScene([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc)
        {
            ISceneLoader loader = loaderCreateFunc();
            yield return LoadFirstScene(loader);

            loader.UnloadScene(new LoadSceneInfoScene(loader.Manager.GetLastLoadedScene()));
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringUnloadScenes([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_multipleLoadSceneInfoList))] ILoadSceneInfo[] targetScenes)
        {
            ISceneLoader loader = loaderCreateFunc();
            int sceneCount = targetScenes.Length;
            loader.LoadScenes(targetScenes);

            var watch = new Stopwatch();
            watch.Start();
            yield return new WaitUntil(() => loader.Manager.LoadedSceneCount == sceneCount || watch.ElapsedMilliseconds > _defaultTimeout);
            watch.Stop();

            loader.UnloadScenes(targetScenes);
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringTransitionToScene([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_singleLoadSceneInfoList))] ILoadSceneInfo targetScene, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            ISceneLoader loader = loaderCreateFunc();
            yield return LoadFirstScene(loader);

            loader.TransitionToScene(targetScene, loadingScene);
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringTransitionToScenes([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_multipleLoadSceneInfoList))] ILoadSceneInfo[] targetScenes, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene)
        {
            ISceneLoader loader = loaderCreateFunc();
            yield return LoadFirstScene(loader);

            loader.TransitionToScenes(targetScenes, 0, loadingScene);
            Assert.DoesNotThrow(loader.Dispose);
            yield return null;
        }

#if ENABLE_UNITASK
        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringLoadSceneAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_singleLoadSceneInfoList))] ILoadSceneInfo targetScene) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            SceneLoaderType type = GetSceneLoaderType(loader);

            UniTask<Scene> task = default;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).LoadSceneAsync(targetScene).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).LoadSceneAsync(targetScene);
                    break;
                case SceneLoaderType.Coroutine:
                    task = ((ISceneLoaderCoroutine)loader).LoadSceneAsync(targetScene).Task.AsUniTask();
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringLoadScenesAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_multipleLoadSceneInfoList))] ILoadSceneInfo[] targetScenes) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            SceneLoaderType type = GetSceneLoaderType(loader);

            UniTask<Scene[]> task = default;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).LoadScenesAsync(targetScenes).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).LoadScenesAsync(targetScenes);
                    break;
                case SceneLoaderType.Coroutine:
                    task = ((ISceneLoaderCoroutine)loader).LoadScenesAsync(targetScenes).Task.AsUniTask();
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringUnloadSceneAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            await LoadFirstScene(loader);

            SceneLoaderType type = GetSceneLoaderType(loader);
            ILoadSceneInfo sceneInfo = new LoadSceneInfoScene(loader.Manager.GetLastLoadedScene());

            UniTask<Scene> task = default;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).UnloadSceneAsync(sceneInfo).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).UnloadSceneAsync(sceneInfo);
                    break;
                case SceneLoaderType.Coroutine:
                    task = ((ISceneLoaderCoroutine)loader).UnloadSceneAsync(sceneInfo).Task.AsUniTask();
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringUnloadScenesAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_multipleLoadSceneInfoList))] ILoadSceneInfo[] targetScenes) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            int sceneCount = targetScenes.Length;
            loader.LoadScenes(targetScenes);

            await UniTask.WaitUntil(() => loader.Manager.LoadedSceneCount == sceneCount).TimeoutWithoutException(TimeSpan.FromMilliseconds(_defaultTimeout));

            SceneLoaderType type = GetSceneLoaderType(loader);

            UniTask<Scene[]> task = default;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).UnloadScenesAsync(targetScenes).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).UnloadScenesAsync(targetScenes);
                    break;
                case SceneLoaderType.Coroutine:
                    task = ((ISceneLoaderCoroutine)loader).UnloadScenesAsync(targetScenes).Task.AsUniTask();
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringTransitionToSceneAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_singleLoadSceneInfoList))] ILoadSceneInfo targetScene, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            await LoadFirstScene(loader);

            SceneLoaderType type = GetSceneLoaderType(loader);

            UniTask<Scene> task = default;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).TransitionToSceneAsync(targetScene, loadingScene).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).TransitionToSceneAsync(targetScene, loadingScene);
                    break;
                case SceneLoaderType.Coroutine:
                    task = ((ISceneLoaderCoroutine)loader).TransitionToSceneAsync(targetScene, loadingScene).Task.AsUniTask();
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });

        [UnityTest]
        [Category(_disposeCategoryName)]
        public IEnumerator Dispose_DuringTransitionToScenesAsync([ValueSource(nameof(_sceneLoaderCreateFuncs))] Func<ISceneLoader> loaderCreateFunc, [ValueSource(nameof(_multipleLoadSceneInfoList))] ILoadSceneInfo[] targetScenes, [ValueSource(nameof(_loadingSceneInfos))] ILoadSceneInfo loadingScene) => UniTask.ToCoroutine(async () =>
        {
            ISceneLoader loader = loaderCreateFunc();
            await LoadFirstScene(loader);

            SceneLoaderType type = GetSceneLoaderType(loader);

            UniTask<Scene[]> task = default;
            switch (type)
            {
                case SceneLoaderType.Async:
                    task = ((ISceneLoaderAsync)loader).TransitionToScenesAsync(targetScenes, 0, loadingScene).AsTask().AsUniTask();
                    break;
                case SceneLoaderType.UniTask:
                    task = ((ISceneLoaderUniTask)loader).TransitionToScenesAsync(targetScenes, 0, loadingScene);
                    break;
                case SceneLoaderType.Coroutine:
                    task = ((ISceneLoaderCoroutine)loader).TransitionToScenesAsync(targetScenes, 0, loadingScene).Task.AsUniTask();
                    break;
                default: throw new NotImplementedException($"Type {type} was not implemented");
            }
            loader.Dispose();

            bool canceled = false;
            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
                canceled = true;
            }
            Assert.True(canceled);
        });
#endif

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
            Assert.AreEqual(loadedScene, SceneManager.GetActiveScene());

            void sceneLoaded(Scene scene)
            {
                loadedScene = scene;
            }
        }

        SceneLoaderType GetSceneLoaderType(ISceneLoader loader)
        {
            if (loader is SceneLoaderAsync)
                return SceneLoaderType.Async;
#if ENABLE_UNITASK
            else if (loader is SceneLoaderUniTask)
                return SceneLoaderType.UniTask;
#endif
            else if (loader is SceneLoaderCoroutine)
                return SceneLoaderType.Coroutine;

            throw new Exception("Unexpected ISceneLoader type");
        }
    }
}